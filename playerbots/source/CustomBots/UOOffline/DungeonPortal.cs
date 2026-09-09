using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Menus.ItemLists;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class UOOfflineDungeonPortal : Item
{
    public override string DefaultName => "dungeon portal";

    [Constructible]
    public UOOfflineDungeonPortal() : base(0xF6C)
    {
        Movable = false;
        Hue = 0x482;
        Light = LightType.Circle300;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.Player)
        {
            return;
        }

        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.SendMessage("You are too far away from the dungeon portal.");
            return;
        }

        from.CloseGump<HavenListGump>();
        from.SendGump(new HavenListGump(this, new DungeonMenu(this), reopen: false));
    }

    private sealed class DungeonDestination
    {
        public string Name { get; }
        public Point3D Location { get; }
        public Map Map { get; }

        public DungeonDestination(string name, Point3D location, Map map)
        {
            Name = name;
            Location = location;
            Map = map;
        }
    }

    private static DungeonDestination[] BuildDestinations()
    {
        var baseEntries = new (string Name, Point3D Location)[]
        {
            ("Blighted Grove", new Point3D(586, 1643, -5)),
            ("Covetous", new Point3D(2499, 919, 0)),
            ("Deceit", new Point3D(4111, 432, 5)),
            ("Despise", new Point3D(1298, 1080, 0)),
            ("Destard", new Point3D(1176, 2637, 0)),
            ("Hythloth", new Point3D(4721, 3822, 0)),
            ("Shame", new Point3D(514, 1561, 0)),
            ("Wrong", new Point3D(2043, 238, 10)),
            ("Terathan Keep", new Point3D(5451, 3143, -60)),
            ("Fire", new Point3D(5760, 2908, 15)),
            ("Ice", new Point3D(5210, 2322, 30)),
            ("Orc Cave", new Point3D(1019, 1431, 0)),
            ("Painted Caves", new Point3D(1716, 2993, 0)),
            ("Palace of Paroxysmus", new Point3D(5576, 3018, 25)),
            ("Prism of Light", new Point3D(3784, 1097, 14)),
            ("Sanctuary", new Point3D(764, 1646, 0))
        };

        var destinations = new DungeonDestination[baseEntries.Length * 2];
        var index = 0;

        foreach (var entry in baseEntries)
        {
            destinations[index++] = new DungeonDestination(
                $"{entry.Name} [Felucca]",
                entry.Location,
                Map.Felucca
            );

            destinations[index++] = new DungeonDestination(
                $"{entry.Name} [Trammel]",
                entry.Location,
                Map.Trammel
            );
        }

        return destinations;
    }

    private static readonly DungeonDestination[] Destinations = BuildDestinations();

    private sealed class DungeonMenu : ItemListMenu
    {
        private readonly UOOfflineDungeonPortal _portal;

        private static ItemListEntry[] BuildEntries()
        {
            var entries = new ItemListEntry[Destinations.Length];

            for (var i = 0; i < Destinations.Length; i++)
            {
                var d = Destinations[i];
                entries[i] = new ItemListEntry(
                    d.Name,
                    0xF6C,
                    d.Map == Map.Felucca ? 0x489 : 0x482
                );
            }

            return entries;
        }

        public DungeonMenu(UOOfflineDungeonPortal portal)
            : base("Choose a dungeon and facet", BuildEntries()) =>
            _portal = portal;

        public override void OnResponse(NetState state, int index)
        {
            var from = state.Mobile;

            if (from == null || _portal?.Deleted != false ||
                !from.InRange(_portal.GetWorldLocation(), 3) ||
                index < 0 || index >= Destinations.Length)
            {
                return;
            }

            if (!from.CheckAlive())
            {
                return;
            }

            if (from.Spell != null)
            {
                from.SendMessage("You are too busy casting a spell to travel.");
                return;
            }

            var destination = Destinations[index];

            BaseCreature.TeleportPets(from, destination.Location, destination.Map);
            from.MoveToWorld(destination.Location, destination.Map);
            from.PlaySound(0x1FE);
        }
    }
}
