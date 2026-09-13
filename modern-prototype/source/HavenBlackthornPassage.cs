using System;
using System.Linq;
using Server.Items;

namespace Server.HavenPrototype
{
    public class HavenBlackthornPassage : Teleporter
    {
        public static void Initialize()
        {
            EventSink.ServerStarted += () =>
            {
                if (!HavenPreview.Enabled) return;
                foreach (var map in new[] { Map.Trammel, Map.Felucca })
                {
                    var points = new[] { new Point3D(6409,2679,0), new Point3D(6359,2571,0), new Point3D(6361,2570,0), new Point3D(6411,2679,0) };
                    if (points.Any(p => !map.CanFit(p,16,false,true))) { Console.WriteLine("Blackthorn passage pair blocked: " + map); continue; }
                    Install(map, new Point3D(6409, 2679, 0), new Point3D(6359, 2571, 0), "Blackthorn - captains and lighthouses");
                    Install(map, new Point3D(6361, 2570, 0), new Point3D(6411, 2679, 0), "Return to Blackthorn entrance hall");
                }
            };
        }
        private static void Install(Map map, Point3D source, Point3D destination, string name)
        {
            if (World.Items.Values.OfType<HavenBlackthornPassage>().Any(p => !p.Deleted && p.Map == map && p.Location == source)) return;
            // Do not create an unusable link when terrain or furnishings obstruct either end.
            if (!map.CanFit(source, 16, false, true) || !map.CanFit(destination, 16, false, true))
            { Console.WriteLine("Blackthorn passage blocked: " + map + " " + source); return; }
            var pad = new HavenBlackthornPassage(destination, map) { Name = name };
            if (source.X == 6409) pad.Visible = false; // Use the existing red floor emblem.
            pad.MoveToWorld(source, map);
        }
        public HavenBlackthornPassage(Point3D destination, Map map) : base(destination, map)
        { ItemID = 0x1BC3; Visible = true; Hue = 1153; }
        public HavenBlackthornPassage(Serial serial) : base(serial) { }
        public override bool CanTeleport(Mobile from)
        {
            return base.CanTeleport(from) && MapDest != null && MapDest.CanFit(PointDest, 16, false, true);
        }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); }
        public override void Deserialize(GenericReader reader) { base.Deserialize(reader); reader.ReadInt(); }
    }
}
