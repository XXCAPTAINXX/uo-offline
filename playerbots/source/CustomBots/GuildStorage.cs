// =========================================================================
// GuildStorage.cs — persistent guild workshop storage for UO Offline.
//
// First implementation slice:
//   * [GuildStorageKit gives every player a one-click testing kit.
//   * The kit expands into a Master Guild Chest plus profession storage.
//   * All storage is normal ModernUO world-item data and survives saves.
//   * The master chest automatically routes dropped items to nearby linked
//     profession chests.
//   * Unknown items go to Unsorted; known items whose destination is absent
//     or unavailable go to Overflow. Nothing is silently deleted.
//   * [GuildStorageLink relinks nearby profession chests to a master chest.
//   * [GuildStorageMissing recreates any missing profession chests.
//   * [GuildStorageSort re-runs sorting on items already in the master.
//   * [GuildStorageStatus reports the linked storage network.
//
// This file is deliberately additive: it does not change existing bot,
// economy, or gatherer behavior yet. Guild-worker deposit/withdraw logic can
// use GuildMasterChest.TryDeposit() in the next phase.
// =========================================================================

using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server;
using Server.Commands;
using Server.Items;
using Server.Targeting;

namespace Server.CustomBots
{
    public enum GuildStorageRole : byte
    {
        Unsorted = 0,
        Overflow,
        Resources,
        Smithing,
        Tailoring,
        Carpentry,
        Tinkering,
        Alchemy,
        Scribing,
        Pantry,
        Taming,
        Armory,
        Treasury
    }

    public static class GuildStorageNames
    {
        public static readonly GuildStorageRole[] AllProfessionRoles =
        {
            GuildStorageRole.Resources,
            GuildStorageRole.Smithing,
            GuildStorageRole.Tailoring,
            GuildStorageRole.Carpentry,
            GuildStorageRole.Tinkering,
            GuildStorageRole.Alchemy,
            GuildStorageRole.Scribing,
            GuildStorageRole.Pantry,
            GuildStorageRole.Taming,
            GuildStorageRole.Armory,
            GuildStorageRole.Treasury,
            GuildStorageRole.Unsorted,
            GuildStorageRole.Overflow
        };

        public static string For(GuildStorageRole role) => role switch
        {
            GuildStorageRole.Resources => "Guild Resource Warehouse",
            GuildStorageRole.Smithing => "Guild Smithy Crate",
            GuildStorageRole.Tailoring => "Guild Tailor Crate",
            GuildStorageRole.Carpentry => "Guild Carpenter Crate",
            GuildStorageRole.Tinkering => "Guild Tinker Crate",
            GuildStorageRole.Alchemy => "Guild Alchemy Cabinet",
            GuildStorageRole.Scribing => "Guild Scribe Cabinet",
            GuildStorageRole.Pantry => "Guild Pantry",
            GuildStorageRole.Taming => "Guild Tamer Supply Chest",
            GuildStorageRole.Armory => "Guild Armory",
            GuildStorageRole.Treasury => "Guild Treasury",
            GuildStorageRole.Unsorted => "Guild Unsorted Storage",
            GuildStorageRole.Overflow => "Guild Overflow Storage",
            _ => "Guild Storage"
        };
    }

    [SerializationGenerator(0)]
    public partial class GuildProfessionChest : MetalChest
    {
        [SerializableField(0, setter: "internal")]
        private GuildStorageRole _role;

        [SerializableField(1, setter: "internal")]
        private GuildMasterChest _masterChest;

        [Constructible(AccessLevel.GameMaster)]
        public GuildProfessionChest() : this(GuildStorageRole.Resources, null)
        {
        }

        public GuildProfessionChest(GuildStorageRole role, GuildMasterChest master) : base()
        {
            _role = role;
            _masterChest = master;
            Name = GuildStorageNames.For(role);
            Weight = 1.0;
        }

        public override int DefaultMaxItems => 0;
        public override int DefaultMaxWeight => 0;

        public bool IsLinkedTo(GuildMasterChest master) =>
            master != null && !master.Deleted && MasterChest == master;

        internal void Relink(GuildMasterChest master)
        {
            MasterChest = master;
            Name = GuildStorageNames.For(Role);
            InvalidateProperties();
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Storage role: {Role}");
            list.Add(MasterChest is { Deleted: false }
                ? $"Linked master: 0x{MasterChest.Serial.Value:X8}"
                : "Linked master: none");
        }
    }

    [SerializationGenerator(0)]
    public partial class GuildMasterChest : MetalChest
    {
        [SerializableField(0)]
        private int _sortRange;

        [Constructible(AccessLevel.GameMaster)]
        public GuildMasterChest() : base()
        {
            Name = "Master Guild Chest";
            Weight = 2.0;
            _sortRange = 24;
        }

        public override int DefaultMaxItems => 0;
        public override int DefaultMaxWeight => 0;

        public int EffectiveSortRange => Math.Max(2, Math.Min(64, SortRange));

        public bool TryDeposit(Mobile from, Item item, bool sendMessage = false)
        {
            if (from == null || item == null || item.Deleted)
            {
                return false;
            }

            if (item is GuildMasterChest or GuildProfessionChest or GuildStorageKit)
            {
                return base.TryDropItem(from, item, sendMessage);
            }

            if (TryRoute(from, item))
            {
                return true;
            }

            return base.TryDropItem(from, item, sendMessage);
        }

        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage) =>
            TryDeposit(from, dropped, sendFullMessage);

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (item == null || item.Deleted)
            {
                return false;
            }

            if (item is not GuildMasterChest and not GuildProfessionChest and not GuildStorageKit &&
                TryRoute(from, item))
            {
                return true;
            }

            return base.OnDragDropInto(from, item, p);
        }

        public int RelinkNearby()
        {
            if (Map == null || Map == Map.Internal)
            {
                return 0;
            }

            int linked = 0;
            foreach (var item in Map.GetItemsInRange(GetWorldLocation(), EffectiveSortRange))
            {
                if (item is not GuildProfessionChest chest || chest.Deleted)
                {
                    continue;
                }

                if (chest.MasterChest == this)
                {
                    continue;
                }

                if (chest.MasterChest == null || chest.MasterChest.Deleted)
                {
                    chest.Relink(this);
                    linked++;
                }
            }

            return linked;
        }

        public int SortAll(Mobile from)
        {
            if (from == null)
            {
                return 0;
            }

            int moved = 0;
            var snapshot = new List<Item>(Items);
            foreach (var item in snapshot)
            {
                if (item == null || item.Deleted || item is GuildMasterChest or GuildProfessionChest or GuildStorageKit)
                {
                    continue;
                }

                if (TryRoute(from, item))
                {
                    moved++;
                }
            }
            return moved;
        }

        public List<GuildProfessionChest> FindLinked(bool worldWide = false)
        {
            var result = new List<GuildProfessionChest>();

            if (worldWide)
            {
                foreach (var item in World.Items.Values)
                {
                    if (item is GuildProfessionChest chest && !chest.Deleted && chest.MasterChest == this)
                    {
                        result.Add(chest);
                    }
                }
                return result;
            }

            if (Map == null || Map == Map.Internal)
            {
                return result;
            }

            foreach (var item in Map.GetItemsInRange(GetWorldLocation(), EffectiveSortRange))
            {
                if (item is GuildProfessionChest chest && !chest.Deleted && chest.MasterChest == this)
                {
                    result.Add(chest);
                }
            }
            return result;
        }

        public int CountLinked(GuildStorageRole role, bool worldWide = false)
        {
            int count = 0;
            foreach (var chest in FindLinked(worldWide))
            {
                if (chest.Role == role)
                {
                    count++;
                }
            }
            return count;
        }

        private bool TryRoute(Mobile from, Item item)
        {
            var role = GuildStorageClassifier.Classify(item);

            if (TryRole(from, item, role))
            {
                return true;
            }

            var fallback = role == GuildStorageRole.Unsorted
                ? GuildStorageRole.Unsorted
                : GuildStorageRole.Overflow;

            if (fallback != role && TryRole(from, item, fallback))
            {
                return true;
            }

            if (role == GuildStorageRole.Unsorted && TryRole(from, item, GuildStorageRole.Overflow))
            {
                return true;
            }

            return false;
        }

        private bool TryRole(Mobile from, Item item, GuildStorageRole role)
        {
            foreach (var chest in FindLinked())
            {
                if (chest.Role != role)
                {
                    continue;
                }

                if (chest.TryDropItem(from, item, false))
                {
                    return true;
                }
            }
            return false;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Auto-sort range: {EffectiveSortRange} tiles");
            list.Add($"Linked storage: {FindLinked().Count}");
        }
    }

    public static class GuildStorageClassifier
    {
        private static readonly HashSet<string> Reagents = new(StringComparer.OrdinalIgnoreCase)
        {
            "BlackPearl", "Bloodmoss", "Garlic", "Ginseng", "MandrakeRoot",
            "Nightshade", "SulfurousAsh", "SpidersSilk", "BatWing", "GraveDust",
            "DaemonBlood", "NoxCrystal", "PigIron", "FertileDirt", "DragonBlood"
        };

        private static readonly HashSet<string> TinkerParts = new(StringComparer.OrdinalIgnoreCase)
        {
            "TinkerTools", "Lockpick", "Gears", "Springs", "Axle", "Hinge",
            "ClockParts", "SextantParts", "AxleGears"
        };

        private static readonly HashSet<string> TamerSupplies = new(StringComparer.OrdinalIgnoreCase)
        {
            "Bandage", "PetLeash", "PetBondingPotion", "PetResurrectionPotion",
            "PetResurrectionStone", "AnimalTrainingToken"
        };

        private static readonly HashSet<string> TreasuryItems = new(StringComparer.OrdinalIgnoreCase)
        {
            "Gold", "BankCheck", "GoldCheck"
        };

        public static GuildStorageRole Classify(Item item)
        {
            if (item == null)
            {
                return GuildStorageRole.Unsorted;
            }

            var name = item.GetType().Name;

            if (TreasuryItems.Contains(name) || IsA(item, "BaseGem"))
            {
                return GuildStorageRole.Treasury;
            }

            if (IsA(item, "BaseOre", "BaseIngot") || NameContains(name, "Ingot", "Ore"))
            {
                return GuildStorageRole.Smithing;
            }

            if (IsA(item, "BaseWeapon", "BaseArmor", "BaseShield", "BaseJewel") || name is "Arrow" or "Bolt")
            {
                return GuildStorageRole.Armory;
            }

            if (IsA(item, "BaseClothing") ||
                NameContains(name, "Leather", "Hide", "Cloth", "Cotton", "Flax", "Thread", "Yarn"))
            {
                return GuildStorageRole.Tailoring;
            }

            if (NameContains(name, "Log", "Board", "Lumber") || IsA(item, "BaseWood"))
            {
                return GuildStorageRole.Carpentry;
            }

            if (TinkerParts.Contains(name) || NameContains(name, "Tinker"))
            {
                return GuildStorageRole.Tinkering;
            }

            if (Reagents.Contains(name) || IsA(item, "BasePotion") || NameContains(name, "Potion", "Keg", "Bottle"))
            {
                return GuildStorageRole.Alchemy;
            }

            if (IsA(item, "SpellScroll", "Spellbook") ||
                NameContains(name, "Scroll", "Spellbook", "Runebook", "RunicAtlas"))
            {
                return GuildStorageRole.Scribing;
            }

            if (TamerSupplies.Contains(name) || NameContains(name, "PetLeash", "Bonding", "PetResurrection"))
            {
                return GuildStorageRole.Taming;
            }

            if (IsA(item, "Food", "CookableFood") ||
                NameContains(name, "Fish", "Meat", "Bread", "Flour", "Fruit", "Vegetable", "Egg", "Cheese"))
            {
                return GuildStorageRole.Pantry;
            }

            if (NameContains(name, "Granite", "Sand", "Scale", "Stone", "Crystal", "Essence"))
            {
                return GuildStorageRole.Resources;
            }

            return GuildStorageRole.Unsorted;
        }

        private static bool IsA(Item item, params string[] baseTypeNames)
        {
            for (var type = item.GetType(); type != null; type = type.BaseType)
            {
                for (int i = 0; i < baseTypeNames.Length; i++)
                {
                    if (string.Equals(type.Name, baseTypeNames[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool NameContains(string name, params string[] fragments)
        {
            for (int i = 0; i < fragments.Length; i++)
            {
                if (name.IndexOf(fragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }
    }

    [SerializationGenerator(0)]
    public partial class GuildStorageKit : Item
    {
        [Constructible]
        public GuildStorageKit() : base(0x14F0)
        {
            Name = "Guild Storage Kit";
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from?.Backpack == null || !IsChildOf(from.Backpack))
            {
                from?.SendMessage("The Guild Storage Kit must be in your backpack to use it.");
                return;
            }

            if (!GuildStorageFactory.GiveFullSet(from))
            {
                from.SendMessage("The storage set could not be placed safely. Make room in your pack and try again.");
                return;
            }

            from.SendMessage("Guild storage created. Place the Master Guild Chest and profession chests in your house.");
            from.SendMessage("The set is pre-linked. Use [GuildStorageLink later to relink nearby replacement chests.");
            Delete();
        }
    }

    public static class GuildStorageFactory
    {
        public static Bag CreateFullSet()
        {
            var bundle = new Bag
            {
                Name = "Complete Guild Storage Set"
            };

            var master = new GuildMasterChest();
            bundle.DropItem(master);

            foreach (var role in GuildStorageNames.AllProfessionRoles)
            {
                bundle.DropItem(new GuildProfessionChest(role, master));
            }

            return bundle;
        }

        public static bool GiveFullSet(Mobile from)
        {
            if (from == null)
            {
                return false;
            }

            var bundle = CreateFullSet();
            if (from.AddToBackpack(bundle))
            {
                return true;
            }

            if (from.Map != null && from.Map != Map.Internal)
            {
                bundle.MoveToWorld(from.Location, from.Map);
                from.SendMessage("Your backpack was full, so the Guild Storage Set was placed at your feet.");
                return true;
            }

            bundle.Delete();
            return false;
        }

        public static Bag CreateMissingSet(GuildMasterChest master)
        {
            if (master == null || master.Deleted)
            {
                return null;
            }

            var bundle = new Bag
            {
                Name = "Missing Guild Storage"
            };

            foreach (var role in GuildStorageNames.AllProfessionRoles)
            {
                if (master.CountLinked(role, worldWide: true) == 0)
                {
                    bundle.DropItem(new GuildProfessionChest(role, master));
                }
            }

            return bundle;
        }
    }

    public static class GuildStorageCommands
    {
        public static void Configure()
        {
            CommandSystem.Register("GuildStorageKit", AccessLevel.Player, Kit_OnCommand);
            CommandSystem.Register("GuildStorageSet", AccessLevel.GameMaster, Set_OnCommand);
            CommandSystem.Register("GuildStorageLink", AccessLevel.Player, Link_OnCommand);
            CommandSystem.Register("GuildStorageMissing", AccessLevel.Player, Missing_OnCommand);
            CommandSystem.Register("GuildStorageSort", AccessLevel.Player, Sort_OnCommand);
            CommandSystem.Register("GuildStorageStatus", AccessLevel.Player, Status_OnCommand);
        }

        [Usage("GuildStorageKit")]
        [Description("Places a one-click Guild Storage Kit in your backpack (testing convenience).")]
        private static void Kit_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            if (from?.Backpack == null)
            {
                return;
            }

            var kit = new GuildStorageKit();
            if (!from.AddToBackpack(kit))
            {
                kit.Delete();
                from.SendMessage("Make room in your backpack first.");
                return;
            }

            from.SendMessage("A Guild Storage Kit has been placed in your backpack. Double-click it to create the full set.");
        }

        [Usage("GuildStorageSet")]
        [Description("Immediately creates a complete pre-linked Guild Storage Set.")]
        private static void Set_OnCommand(CommandEventArgs e)
        {
            if (GuildStorageFactory.GiveFullSet(e.Mobile))
            {
                e.Mobile.SendMessage("Complete Guild Storage Set created.");
            }
        }

        [Usage("GuildStorageLink")]
        [Description("Targets a Master Guild Chest and links nearby unlinked/replacement profession storage.")]
        private static void Link_OnCommand(CommandEventArgs e) =>
            BeginTarget(e.Mobile, GuildStorageTargetAction.Link);

        [Usage("GuildStorageMissing")]
        [Description("Targets a Master Guild Chest and creates replacements for missing profession storage.")]
        private static void Missing_OnCommand(CommandEventArgs e) =>
            BeginTarget(e.Mobile, GuildStorageTargetAction.Missing);

        [Usage("GuildStorageSort")]
        [Description("Targets a Master Guild Chest and sorts items already sitting in it.")]
        private static void Sort_OnCommand(CommandEventArgs e) =>
            BeginTarget(e.Mobile, GuildStorageTargetAction.Sort);

        [Usage("GuildStorageStatus")]
        [Description("Targets a Master Guild Chest and reports linked storage and unsorted/overflow counts.")]
        private static void Status_OnCommand(CommandEventArgs e) =>
            BeginTarget(e.Mobile, GuildStorageTargetAction.Status);

        private static void BeginTarget(Mobile from, GuildStorageTargetAction action)
        {
            if (from == null)
            {
                return;
            }

            from.SendMessage("Target the Master Guild Chest.");
            from.Target = new GuildStorageTarget(action);
        }

        private enum GuildStorageTargetAction : byte
        {
            Link,
            Missing,
            Sort,
            Status
        }

        private sealed class GuildStorageTarget : Target
        {
            private readonly GuildStorageTargetAction _action;

            public GuildStorageTarget(GuildStorageTargetAction action) : base(12, false, TargetFlags.None)
            {
                _action = action;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is not GuildMasterChest master || master.Deleted)
                {
                    from.SendMessage("That is not a Master Guild Chest.");
                    return;
                }

                switch (_action)
                {
                    case GuildStorageTargetAction.Link:
                    {
                        int linked = master.RelinkNearby();
                        int moved = master.SortAll(from);
                        from.SendMessage($"Linked {linked} nearby storage chest(s); sorted {moved} item(s).");
                        break;
                    }
                    case GuildStorageTargetAction.Missing:
                    {
                        var bundle = GuildStorageFactory.CreateMissingSet(master);
                        if (bundle == null)
                        {
                            from.SendMessage("Unable to inspect that storage network.");
                            return;
                        }

                        if (bundle.Items.Count == 0)
                        {
                            bundle.Delete();
                            from.SendMessage("No profession storage is missing.");
                            return;
                        }

                        int count = bundle.Items.Count;
                        if (!from.AddToBackpack(bundle))
                        {
                            if (from.Map != null && from.Map != Map.Internal)
                            {
                                bundle.MoveToWorld(from.Location, from.Map);
                                from.SendMessage($"Created {count} missing chest(s); the bundle was placed at your feet.");
                            }
                            else
                            {
                                bundle.Delete();
                                from.SendMessage("Could not safely place the replacement storage bundle.");
                            }
                            return;
                        }

                        from.SendMessage($"Created {count} missing profession chest(s), already linked to this master.");
                        break;
                    }
                    case GuildStorageTargetAction.Sort:
                    {
                        int moved = master.SortAll(from);
                        from.SendMessage($"Sorted {moved} item(s). Items that could not be routed remain safely stored.");
                        break;
                    }
                    case GuildStorageTargetAction.Status:
                    {
                        ShowStatus(from, master);
                        break;
                    }
                }
            }
        }

        private static void ShowStatus(Mobile from, GuildMasterChest master)
        {
            var all = master.FindLinked(worldWide: true);
            var nearby = master.FindLinked();

            from.SendMessage($"Master Guild Chest 0x{master.Serial.Value:X8}: {nearby.Count} nearby linked / {all.Count} total linked.");
            foreach (var role in GuildStorageNames.AllProfessionRoles)
            {
                int count = 0;
                int items = 0;
                foreach (var chest in all)
                {
                    if (chest.Role == role)
                    {
                        count++;
                        items += chest.TotalItems;
                    }
                }

                from.SendMessage($"{GuildStorageNames.For(role)}: {count} chest(s), {items} item(s).");
            }

            from.SendMessage($"Items still directly in Master Chest: {master.Items.Count}.");
        }
    }
}
