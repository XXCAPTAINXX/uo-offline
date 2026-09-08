// =========================================================================
// PlayabilityPack.cs — first-play travel + New Haven newbie dungeon.
//
// ModernUO already contains the modern public moongate destination tables and
// New Haven quest definitions. This layer makes them usable on an offline
// shard where broad native world-spawner generation is intentionally avoided:
//
//   * every character gets a blessed Travel Book (OfflineWallet ensures it)
//   * a DISTINCT orange dungeon portal sits beside New Haven bank
//   * the portal leads to a real Trammel Despise dungeon interior
//   * the newbie room has controlled weak spawns and a healer
//   * UORespawn is suppressed while a player is inside the newbie bounds
//   * +1000 Luck and 5x player/pet skill gain apply only in that dungeon
//   * New Haven quest instructors are explicitly restored at canonical points
//
// Native town/vendor population is restored separately by NativeTownPopulation.
// =========================================================================

using System;
using ModernUO.Serialization;
using Server;
using Server.Commands;
using Server.Engines.Spawners;
using Server.Items;
using Server.Maps;
using Server.Mobiles;
using Server.Spells;

namespace Server.CustomBots
{
    public static class NewbiePlayability
    {
        // Dedicated newbie section inside the real Trammel Despise dungeon.
        // These coordinates are within ModernUO's Despise dungeon region.
        public const int MinX = 5445;
        public const int MinY = 515;
        public const int MaxXExclusive = 5525;
        public const int MaxYExclusive = 620;

        // New Haven bank is centered on the native bankers at 3484,2570/2576.
        private static readonly Point3D NewHavenGateAnchor = new(3489, 2573, 20);

        // Real Despise interior: ModernUO's region GoLocation is 5501,570,59.
        private static readonly Point3D TrainingGateAnchor = new(5501, 570, 59);

        public static void Configure()
        {
            CommandSystem.Register("NewbieSetup", AccessLevel.GameMaster, NewbieSetup_OnCommand);
            CommandSystem.Register("TravelBook", AccessLevel.Player, TravelBook_OnCommand);

            EventSink.WorldLoad += OnWorldLoad;
        }

        private static void OnWorldLoad()
        {
            Timer.DelayCall(TimeSpan.FromSeconds(1), EnsureWorld);
        }

        public static bool IsInNewbieTraining(Mobile m)
        {
            if (m?.Map != Map.Trammel)
            {
                return false;
            }

            return m.X >= MinX && m.X < MaxXExclusive &&
                   m.Y >= MinY && m.Y < MaxYExclusive;
        }

        public static int LuckBonus(PlayerMobile pm) =>
            pm != null && IsInNewbieTraining(pm) ? 1000 : 0;

        public static bool FastGainEligible(Mobile from, Skill skill)
        {
            if (from == null || skill == null || skill.Base >= 100.0 ||
                !IsInNewbieTraining(from))
            {
                return false;
            }

            // Real characters and their controlled pets train here. Ambient
            // population PlayerBots are intentionally excluded so the zone
            // doesn't silently power-level the simulated shard population.
            return from is PlayerMobile and not PlayerBot ||
                   from is BaseCreature { Controlled: true, IsDeadPet: false };
        }

        public const int FastGainMultiplier = 5;

        public static void EnsureWorld()
        {
            if (!Core.ML ||
                !ExpansionInfo.CoreExpansion.MapSelectionFlags.Includes(MapSelectionFlags.Trammel))
            {
                return;
            }

            EnsureGate<NewbieDungeonEntrance>(NewHavenGateAnchor, Map.Trammel);
            EnsureGate<NewbieDungeonExit>(TrainingGateAnchor, Map.Trammel);

            // Known walkable Despise spawn points from ModernUO's own data.
            EnsureSpawner(
                "UO Offline Newbie Spawn NorthWest",
                new Point3D(5460, 526, 60),
                5,
                homeRange: 12,
                walkingRange: 20,
                "Skeleton",
                "Zombie",
                "Mongbat"
            );

            EnsureSpawner(
                "UO Offline Newbie Spawn NorthEast",
                new Point3D(5503, 529, 60),
                6,
                homeRange: 12,
                walkingRange: 20,
                "Skeleton",
                "Zombie",
                "HeadlessOne"
            );

            EnsureSpawner(
                "UO Offline Newbie Spawn SouthWest",
                new Point3D(5464, 600, 45),
                5,
                homeRange: 12,
                walkingRange: 20,
                "Skeleton",
                "Zombie",
                "Mongbat"
            );

            EnsureSpawner(
                "UO Offline Newbie Spawn SouthEast",
                new Point3D(5504, 597, 45),
                6,
                homeRange: 12,
                walkingRange: 20,
                "Skeleton",
                "Zombie",
                "HeadlessOne"
            );

            EnsureSpawner(
                "UO Offline Newbie Healer",
                new Point3D(5498, 566, 59),
                1,
                homeRange: 4,
                walkingRange: 4,
                "WanderingHealer"
            );

            EnsureNewHavenQuesters();
        }

        private static void EnsureGate<T>(Point3D preferred, Map map) where T : Item, new()
        {
            var loc = FindSafe(map, preferred, 8);

            foreach (var item in World.Items.Values)
            {
                if (item is T existing && !existing.Deleted)
                {
                    if (existing is NewbieGateBase gateBase)
                    {
                        gateBase.RefreshVisual();
                    }

                    if (existing.Map != map || !existing.Location.InRange(loc, 2))
                    {
                        existing.MoveToWorld(loc, map);
                        Console.WriteLine($"[newbie] moved {typeof(T).Name} to {map.Name} {loc}");
                    }

                    return;
                }
            }

            var gate = new T();
            if (gate is NewbieGateBase gateBaseNew)
            {
                gateBaseNew.RefreshVisual();
            }

            gate.MoveToWorld(loc, map);
            Console.WriteLine($"[newbie] placed {typeof(T).Name} at {map.Name} {loc}");
        }

        private static void EnsureSpawner(
            string name,
            Point3D preferred,
            int amount,
            int homeRange = 0,
            int walkingRange = 2,
            params string[] types
        )
        {
            var loc = FindSafe(Map.Trammel, preferred, 8);

            foreach (var item in World.Items.Values)
            {
                if (item is not Spawner sp || sp.Deleted ||
                    !string.Equals(sp.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Migration: old versions placed these in Old Haven. Rebuild
                // named newbie spawners if the desired location changed.
                if (sp.Map == Map.Trammel && sp.Location.InRange(loc, 3))
                {
                    return;
                }

                sp.Delete();
                break;
            }

            var spawner = new Spawner(
                amount,
                TimeSpan.FromSeconds(20),
                TimeSpan.FromSeconds(45),
                0,
                default,
                types
            )
            {
                Name = name,
                WalkingRange = walkingRange
            };

            spawner.MoveToWorld(loc, Map.Trammel);
            spawner.HomeRange = homeRange;
            spawner.Running = true;
            spawner.Respawn();

            Console.WriteLine($"[newbie] placed {name} at Trammel {loc}");
        }

        private static void EnsureNewHavenQuesters()
        {
            // Canonical New Haven ML instructor positions recovered from the
            // original quest Generate() data (RunUO/CorexUO lineage).
            EnsureQuestSpawner("Aelorn",          new Point3D(3527, 2516, 45));
            EnsureQuestSpawner("Dimethro",        new Point3D(3528, 2520, 25));
            EnsureQuestSpawner("Churchill",       new Point3D(3531, 2531, 20));
            EnsureQuestSpawner("Robyn",           new Point3D(3535, 2531, 20));
            EnsureQuestSpawner("Recaro",          new Point3D(3536, 2534, 20));
            EnsureQuestSpawner("AldenArmstrong",  new Point3D(3535, 2538, 20));
            EnsureQuestSpawner("Jockles",         new Point3D(3535, 2544, 20));
            EnsureQuestSpawner("TylAriadne",      new Point3D(3525, 2556, 20));
            EnsureQuestSpawner("Alefian",         new Point3D(3473, 2497, 72));
            EnsureQuestSpawner("Gustar",          new Point3D(3474, 2492, 91));
            EnsureQuestSpawner("Jillian",         new Point3D(3465, 2490, 71));
            EnsureQuestSpawner("Kaelynna",        new Point3D(3486, 2491, 52));
            EnsureQuestSpawner("Mithneral",       new Point3D(3485, 2491, 71));
            EnsureQuestSpawner("AmeliaYoungstone",new Point3D(3459, 2529, 53));
            EnsureQuestSpawner("AndreasVesalius", new Point3D(3457, 2550, 35));
            EnsureQuestSpawner("Avicenna",        new Point3D(3464, 2558, 35));
            EnsureQuestSpawner("SarsmeaSmythe",   new Point3D(3492, 2577, 15));
            EnsureQuestSpawner("Ryuichi",         new Point3D(3422, 2520, 21));
            EnsureQuestSpawner("Chiyo",           new Point3D(3420, 2516, 21));
            EnsureQuestSpawner("Jun",             new Point3D(3422, 2516, 21));
            EnsureQuestSpawner("Walker",          new Point3D(3429, 2518, 19));
            EnsureQuestSpawner("Hamato",          new Point3D(3493, 2414, 55));
            EnsureQuestSpawner("Mulcivikh",       new Point3D(3548, 2456, 15));
            EnsureQuestSpawner("Morganna",        new Point3D(3547, 2463, 15));
            EnsureQuestSpawner("JacobWaltz",      new Point3D(3504, 2741, 0));
            EnsureQuestSpawner("GeorgeHephaestus",new Point3D(3471, 2542, 36));
        }

        private static void EnsureQuestSpawner(string type, Point3D location)
        {
            EnsureSpawner(
                $"UO Offline New Haven Quest - {type}",
                location,
                1,
                homeRange: 0,
                walkingRange: 2,
                type
            );
        }

        public static Point3D FindSafe(Map map, Point3D preferred, int radius)
        {
            if (map == null || map == Map.Internal)
            {
                return preferred;
            }

            // First trust the supplied Z. Dungeon interiors often have
            // stacked floors where GetAverageZ points at the wrong level.
            if (map.CanSpawnMobile(preferred.X, preferred.Y, preferred.Z))
            {
                return preferred;
            }

            int preferredAverageZ = map.GetAverageZ(preferred.X, preferred.Y);
            if (map.CanSpawnMobile(preferred.X, preferred.Y, preferredAverageZ))
            {
                return new Point3D(preferred.X, preferred.Y, preferredAverageZ);
            }

            for (int r = 1; r <= radius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                        {
                            continue;
                        }

                        int x = preferred.X + dx;
                        int y = preferred.Y + dy;

                        if (map.CanSpawnMobile(x, y, preferred.Z))
                        {
                            return new Point3D(x, y, preferred.Z);
                        }

                        int z = map.GetAverageZ(x, y);
                        if (map.CanSpawnMobile(x, y, z))
                        {
                            return new Point3D(x, y, z);
                        }
                    }
                }
            }

            return new Point3D(preferred.X, preferred.Y, preferredAverageZ);
        }

        private static void TravelBook_OnCommand(CommandEventArgs e)
        {
            OfflineWalletSystem.EnsureStarterItems(e.Mobile);

            var book = e.Mobile?.Backpack?.FindItemByType<OfflineTravelBook>();
            if (book == null)
            {
                e.Mobile?.SendMessage("No Travel Book could be created.");
                return;
            }

            book.Open(e.Mobile);
        }

        private static void NewbieSetup_OnCommand(CommandEventArgs e)
        {
            EnsureWorld();
            e.Mobile?.SendMessage("New Haven questers, bank portal, and real newbie dungeon checked/repaired.");
        }
    }

    [SerializationGenerator(0)]
    public partial class OfflineTravelBook : Item
    {
        [Constructible]
        public OfflineTravelBook() : base(0x22C5)
        {
            Name = "Blessed Travel Book";
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public override void OnDoubleClick(Mobile from) => Open(from);

        public void Open(Mobile from)
        {
            if (from?.Backpack == null || !IsChildOf(from.Backpack))
            {
                from?.SendMessage("The Travel Book must be in your backpack.");
                return;
            }

            if (!from.CheckAlive())
            {
                return;
            }

            if (from.Criminal)
            {
                from.SendMessage("You cannot use the Travel Book while criminal.");
                return;
            }

            if (SpellHelper.CheckCombat(from))
            {
                from.SendMessage("You cannot use the Travel Book during combat.");
                return;
            }

            if (from.Spell != null)
            {
                from.SendMessage("You are too busy to travel.");
                return;
            }

            // MoongateGump accepts any Item as its anchor. Because this book
            // is inside the player's backpack, its world location/map follow
            // the player and the stock proximity/security checks still apply.
            MoongateGump.DisplayTo(from, this);
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Double-click: open modern travel destinations");
            list.Add("Includes New Haven and enabled expansion facets");
        }
    }

    [SerializationGenerator(0)]
    public abstract partial class NewbieGateBase : Item
    {
        protected NewbieGateBase(int itemID, int hue) : base(itemID)
        {
            Movable = false;
            Light = LightType.Circle300;
            Hue = hue;
        }

        protected abstract int GateItemID { get; }
        protected abstract int GateHue { get; }
        protected abstract Point3D Destination { get; }
        protected abstract string DestinationName { get; }

        public void RefreshVisual()
        {
            ItemID = GateItemID;
            Hue = GateHue;
            Movable = false;
            Light = LightType.Circle300;
        }

        public override bool HandlesOnMovement => true;

        public override void OnDoubleClick(Mobile from)
        {
            if (from != null && from.InRange(GetWorldLocation(), 1))
            {
                Travel(from);
            }
            else
            {
                from?.SendMessage("That gate is too far away.");
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m?.Player == true)
            {
                Travel(m);
            }

            return true;
        }

        protected virtual void Travel(Mobile from)
        {
            if (from == null || from.Deleted || !from.Alive)
            {
                return;
            }

            var dest = NewbiePlayability.FindSafe(Map.Trammel, Destination, 10);
            BaseCreature.TeleportPets(from, dest, Map.Trammel);

            from.Combatant = null;
            from.Warmode = false;
            from.MoveToWorld(dest, Map.Trammel);

            Effects.PlaySound(dest, Map.Trammel, 0x1FE);
            from.SendMessage(0x35, $"Travelled to {DestinationName}.");
        }
    }

    [SerializationGenerator(0)]
    public partial class NewbieDungeonEntrance : NewbieGateBase
    {
        [Constructible]
        public NewbieDungeonEntrance() : base(0x1822, 0x489)
        {
            Name = "Newbie Dungeon Portal";
        }

        protected override int GateItemID => 0x1822;
        protected override int GateHue => 0x489;
        protected override Point3D Destination => new(5501, 570, 59);
        protected override string DestinationName => "Newbie Dungeon";

        protected override void Travel(Mobile from)
        {
            base.Travel(from);
            from?.SendMessage(
                0x35,
                "Newbie Dungeon: +1000 Luck, 5x player/pet skill gain to 100.0, and Sovereigns from kills."
            );
        }
    }

    [SerializationGenerator(0)]
    public partial class NewbieDungeonExit : NewbieGateBase
    {
        [Constructible]
        public NewbieDungeonExit() : base(0x1822, 0x59B)
        {
            Name = "Return Portal to New Haven Bank";
        }

        protected override int GateItemID => 0x1822;
        protected override int GateHue => 0x59B;
        protected override Point3D Destination => new(3489, 2573, 20);
        protected override string DestinationName => "New Haven Bank";
    }
}
