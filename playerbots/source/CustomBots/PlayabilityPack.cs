// =========================================================================
// PlayabilityPack.cs — first-play travel + New Haven/Old Haven training.
//
// ModernUO already contains the modern public moongate destination tables and
// New Haven skill-training quests. This layer makes those systems easy to
// reach on an offline shard:
//
//   * every character gets a blessed Travel Book (OfflineWallet ensures it)
//   * double-clicking the book opens the standard modern moongate menu
//   * a permanent Newbie Dungeon gate is placed beside New Haven
//   * the first Newbie Dungeon uses Old Haven's canonical training area
//   * weak undead spawners make training dependable even without static
//     world-spawner generation
//   * players inside the training bounds receive +1000 Luck through engine
//     patch 0009-player-newbie-luck.patch
//
// This is intentionally a first playable training area. A bespoke dungeon
// layout can replace/extend it later without changing the travel/wallet API.
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
        // ServUO/OSI New Haven training bounds ("Old Haven Training").
        public const int MinX = 3589;
        public const int MinY = 2443;
        public const int MaxXExclusive = 3704;
        public const int MaxYExclusive = 2543;

        private static readonly Point3D NewHavenGateAnchor = new(3450, 2677, 25);
        private static readonly Point3D TrainingGateAnchor = new(3595, 2492, 0);

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

            EnsureSpawner(
                "UO Offline Newbie Spawn West",
                new Point3D(3615, 2470, 0),
                5,
                "Skeleton",
                "Zombie",
                "Mongbat"
            );

            EnsureSpawner(
                "UO Offline Newbie Spawn Center",
                new Point3D(3650, 2492, 0),
                6,
                "Skeleton",
                "Zombie",
                "HeadlessOne"
            );

            EnsureSpawner(
                "UO Offline Newbie Spawn East",
                new Point3D(3680, 2515, 0),
                5,
                "Skeleton",
                "Zombie",
                "Mongbat"
            );

            EnsureSpawner(
                "UO Offline Newbie Healer",
                new Point3D(3601, 2490, 0),
                1,
                "WanderingHealer"
            );
        }

        private static void EnsureGate<T>(Point3D preferred, Map map) where T : Item, new()
        {
            foreach (var item in World.Items.Values)
            {
                if (item is T && !item.Deleted)
                {
                    return;
                }
            }

            var loc = FindSafe(map, preferred, 8);
            var gate = new T();
            gate.MoveToWorld(loc, map);
            Console.WriteLine($"[newbie] placed {typeof(T).Name} at {map.Name} {loc}");
        }

        private static void EnsureSpawner(string name, Point3D preferred, int amount, params string[] types)
        {
            foreach (var item in World.Items.Values)
            {
                if (item is Spawner sp && !sp.Deleted &&
                    string.Equals(sp.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            var loc = FindSafe(Map.Trammel, preferred, 8);
            var spawner = new Spawner(
                amount,
                TimeSpan.FromSeconds(20),
                TimeSpan.FromSeconds(45),
                0,
                default,
                types
            )
            {
                Name = name
            };

            spawner.MoveToWorld(loc, Map.Trammel);
            spawner.HomeRange = name.Contains("Healer", StringComparison.OrdinalIgnoreCase) ? 5 : 14;
            spawner.Running = true;

            Console.WriteLine($"[newbie] placed {name} at Trammel {loc}");
        }

        public static Point3D FindSafe(Map map, Point3D preferred, int radius)
        {
            if (map == null || map == Map.Internal)
            {
                return preferred;
            }

            int preferredZ = map.GetAverageZ(preferred.X, preferred.Y);
            if (map.CanSpawnMobile(preferred.X, preferred.Y, preferredZ))
            {
                return new Point3D(preferred.X, preferred.Y, preferredZ);
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
                        int z = map.GetAverageZ(x, y);
                        if (map.CanSpawnMobile(x, y, z))
                        {
                            return new Point3D(x, y, z);
                        }
                    }
                }
            }

            return new Point3D(preferred.X, preferred.Y, preferredZ);
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
            e.Mobile?.SendMessage("Newbie travel gates and training spawners checked/repaired.");
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

    public abstract class NewbieGateBase : Item
    {
        protected NewbieGateBase(int hue) : base(0xF6C)
        {
            Movable = false;
            Light = LightType.Circle300;
            Hue = hue;
        }

        protected abstract Point3D Destination { get; }
        protected abstract string DestinationName { get; }

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
        public NewbieDungeonEntrance() : base(0x48D)
        {
            Name = "Newbie Dungeon Gate";
        }

        protected override Point3D Destination => new(3595, 2492, 0);
        protected override string DestinationName => "Newbie Dungeon - Old Haven Training";

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
        public NewbieDungeonExit() : base(0x59B)
        {
            Name = "Return Gate to New Haven";
        }

        protected override Point3D Destination => new(3450, 2677, 25);
        protected override string DestinationName => "New Haven";
    }
}
