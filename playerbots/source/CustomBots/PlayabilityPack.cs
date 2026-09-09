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
//   * UORespawn is suppressed only inside the dedicated newbie dungeon
//   * +1000 Luck and 5x player/pet skill gain apply in the dungeon AND Old Haven
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
using Server.Network;
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

        // Canonical Old Haven training bounds used by the original New Haven
        // skill quests. Keep this outdoor area useful even though the shard
        // now also has a dedicated newbie dungeon.
        public const int OldHavenMinX = 3589;
        public const int OldHavenMinY = 2443;
        public const int OldHavenMaxXExclusive = 3704;
        public const int OldHavenMaxYExclusive = 2543;

        // New Haven bank is centered on the native bankers at 3484,2570/2576.
        private static readonly Point3D NewHavenGateAnchor = new(3489, 2573, 20);

        // Real Despise interior: ModernUO's region GoLocation is 5501,570,59.
        private static readonly Point3D TrainingGateAnchor = new(5501, 570, 59);

        public static void Configure()
        {
            CommandSystem.Register("NewbieSetup", AccessLevel.Player, NewbieSetup_OnCommand);
            CommandSystem.Register("NewbieStatus", AccessLevel.Player, NewbieStatus_OnCommand);
            CommandSystem.Register("TravelBook", AccessLevel.Player, TravelBook_OnCommand);

            EventSink.WorldLoad += OnWorldLoad;
        }

        private static void OnWorldLoad()
        {
            Timer.DelayCall(TimeSpan.FromSeconds(1), EnsureWorld);
        }

        public static bool IsInNewbieDungeon(Mobile m) =>
            m != null && IsInNewbieDungeon(m.Map, m.Location);

        public static bool IsInNewbieDungeon(Map map, Point3D location)
        {
            if (map != Map.Trammel)
            {
                return false;
            }

            return location.X >= MinX && location.X < MaxXExclusive &&
                   location.Y >= MinY && location.Y < MaxYExclusive;
        }

        public static bool IsInOldHavenTraining(Mobile m) =>
            m != null && IsInOldHavenTraining(m.Map, m.Location);

        public static bool IsInOldHavenTraining(Map map, Point3D location)
        {
            if (map != Map.Trammel)
            {
                return false;
            }

            return location.X >= OldHavenMinX && location.X < OldHavenMaxXExclusive &&
                   location.Y >= OldHavenMinY && location.Y < OldHavenMaxYExclusive;
        }

        public static bool IsInNewbieTraining(Mobile m) =>
            m != null && IsInNewbieTraining(m.Map, m.Location);

        public static bool IsInNewbieTraining(Map map, Point3D location) =>
            IsInNewbieDungeon(map, location) || IsInOldHavenTraining(map, location);

        public static void RefreshLuckStatus(PlayerMobile pm, Map oldMap, Point3D oldLocation)
        {
            if (pm == null || pm.Deleted)
            {
                return;
            }

            bool wasTraining = IsInNewbieTraining(oldMap, oldLocation);
            bool isTraining = IsInNewbieTraining(pm);

            if (wasTraining == isTraining)
            {
                return;
            }

            // Luck is dynamic by location. Crossing the boundary does not
            // otherwise dirty a stat, so explicitly refresh the AOS/ML status
            // packet or TazUO will continue displaying the pre-zone value.
            pm.NetState?.SendMobileStatus(pm);

            if (isTraining)
            {
                pm.SendMessage(
                    0x35,
                    $"Newbie training active: +1000 Luck and {FastGainMultiplier}x player/pet skill gain to 100.0."
                );
            }
            else
            {
                pm.SendMessage("Newbie training bonuses have ended.");
            }
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

            PrepareNewbieDungeon();

            // Known walkable Despise spawn points from ModernUO's own data.
            EnsureSpawner(
                "UO Offline Newbie Spawn NorthWest",
                new Point3D(5460, 526, 60),
                5,
                12,
                20,
                "Skeleton",
                "Zombie",
                "Mongbat"
            );

            EnsureSpawner(
                "UO Offline Newbie Spawn NorthEast",
                new Point3D(5503, 529, 60),
                6,
                12,
                20,
                "Skeleton",
                "Zombie",
                "HeadlessOne"
            );

            EnsureSpawner(
                "UO Offline Newbie Spawn SouthWest",
                new Point3D(5464, 600, 45),
                5,
                12,
                20,
                "Skeleton",
                "Zombie",
                "Mongbat"
            );

            EnsureSpawner(
                "UO Offline Newbie Spawn SouthEast",
                new Point3D(5504, 597, 45),
                6,
                12,
                20,
                "Skeleton",
                "Zombie",
                "HeadlessOne"
            );

            EnsureSpawner(
                "UO Offline Newbie Healer",
                new Point3D(5498, 566, 59),
                1,
                4,
                4,
                "WanderingHealer"
            );

            // New Haven should feel alive the moment a new character steps
            // outside. These are intentionally gentle, tameable, and useful
            // beginner creatures rather than end-game ecology.
            EnsureSpawner(
                "UO Offline New Haven Meadow West",
                new Point3D(3450, 2605, 10),
                10,
                18,
                24,
                "Rabbit",
                "Hind",
                "GreatHart",
                "Cow",
                "Goat",
                "Sheep"
            );

            EnsureSpawner(
                "UO Offline New Haven Mount Meadow",
                new Point3D(3506, 2640, 0),
                8,
                18,
                24,
                "Horse",
                "RidableLlama",
                "ForestOstard"
            );

            EnsureSpawner(
                "UO Offline New Haven Practice Creatures",
                new Point3D(3560, 2585, 0),
                10,
                18,
                24,
                "GiantRat",
                "Mongbat",
                "HeadlessOne",
                "Slime"
            );

            // Old Haven is the outdoor bridge between tutorial play and the
            // real world: plentiful low-tier targets, some tameables/mounts,
            // plus one forgiving named boss with starter-quality rewards.
            EnsureSpawner(
                "UO Offline Old Haven Training West",
                new Point3D(3610, 2470, 0),
                10,
                20,
                26,
                "Skeleton",
                "Zombie",
                "HeadlessOne",
                "Mongbat",
                "GiantRat"
            );

            EnsureSpawner(
                "UO Offline Old Haven Training East",
                new Point3D(3682, 2510, 0),
                9,
                20,
                26,
                "Orc",
                "Lizardman",
                "Skeleton",
                "Zombie"
            );

            EnsureSpawner(
                "UO Offline Old Haven Taming Meadow",
                new Point3D(3650, 2525, 0),
                7,
                18,
                24,
                "Horse",
                "Hind",
                "GreatHart",
                "ForestOstard"
            );

            EnsureSpawner(
                "UO Offline Old Haven Warden",
                new Point3D(3645, 2478, 0),
                1,
                TimeSpan.FromMinutes(8),
                TimeSpan.FromMinutes(12),
                8,
                10,
                "OldHavenWarden"
            );

            EnsureNewHavenQuesters();
        }

        private static void PrepareNewbieDungeon()
        {
            // This section of Despise is intentionally reserved for beginner
            // training. If someone previously generated the stock Despise
            // spawners, remove only those that physically sit inside our
            // reserved rectangle. Never touch the rest of the dungeon.
            var spawnersToDelete = new System.Collections.Generic.List<Item>();

            foreach (var item in World.Items.Values)
            {
                if (item is not ISpawner || item.Deleted || item.Map != Map.Trammel)
                {
                    continue;
                }

                if (item.X < MinX || item.X >= MaxXExclusive ||
                    item.Y < MinY || item.Y >= MaxYExclusive)
                {
                    continue;
                }

                if (item.Name?.StartsWith("UO Offline Newbie", StringComparison.OrdinalIgnoreCase) == true)
                {
                    continue;
                }

                spawnersToDelete.Add(item);
            }

            foreach (var item in spawnersToDelete)
            {
                item.Delete();
            }

            if (spawnersToDelete.Count > 0)
            {
                Console.WriteLine(
                    $"[newbie] removed {spawnersToDelete.Count} non-newbie spawner(s) from reserved dungeon section"
                );
            }
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

                    if (existing.Map != map || !Utility.InRange(existing.Location, loc, 2))
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
        ) =>
            EnsureSpawner(
                name,
                preferred,
                amount,
                TimeSpan.FromSeconds(20),
                TimeSpan.FromSeconds(45),
                homeRange,
                walkingRange,
                types
            );

        private static void EnsureSpawner(
            string name,
            Point3D preferred,
            int amount,
            TimeSpan minDelay,
            TimeSpan maxDelay,
            int homeRange,
            int walkingRange,
            params string[] types
        )
        {
            var loc = FindSafe(Map.Trammel, preferred, 12);

            foreach (var item in World.Items.Values)
            {
                if (item is not Spawner sp || sp.Deleted ||
                    !string.Equals(sp.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Migration: move/rebuild named UO Offline spawners when the
                // authored starter layout changes between builds.
                if (sp.Map == Map.Trammel && Utility.InRange(sp.Location, loc, 3))
                {
                    return;
                }

                sp.Delete();
                break;
            }

            var spawner = new Spawner(
                amount,
                minDelay,
                maxDelay,
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
                0,
                2,
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

        private static void NewbieStatus_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is not PlayerMobile pm)
            {
                return;
            }

            pm.NetState?.SendMobileStatus(pm);

            string zone = IsInNewbieDungeon(pm)
                ? "Newbie Dungeon"
                : IsInOldHavenTraining(pm)
                    ? "Old Haven"
                    : "None";

            pm.SendMessage(0x35, "=== Newbie Training Status ===");
            pm.SendMessage($"Zone: {zone}");
            pm.SendMessage($"Current Luck: {pm.Luck:N0}");
            pm.SendMessage(
                IsInNewbieTraining(pm)
                    ? $"+1000 zone Luck ACTIVE; {FastGainMultiplier}x player/pet skill gain to 100.0 ACTIVE."
                    : "No newbie training bonus is currently active."
            );
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
