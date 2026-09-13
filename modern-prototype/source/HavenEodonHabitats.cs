using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Multis;

namespace Server.HavenPrototype
{
    public class HavenEodonPetSpawner : Spawner
    {
        public bool Tiger;
        public HavenEodonPetSpawner(bool tiger) : base(1, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 0, 3,
            new List<string> { tiger ? "HavenSunfangTiger" : "HavenStonehornTriceratops" }) { Tiger = tiger; }
        public HavenEodonPetSpawner(Serial serial) : base(serial) { }
        public override void Spawn()
        {
            foreach (var pet in GetSpawn().OfType<BaseCreature>().ToArray())
                if (pet.Controlled || pet.Owners.Count > 0 || pet.Map == Map.Internal) RemoveSpawn(pet);
            Defrag(); var before = GetSpawn().ToArray(); base.Spawn();
            foreach (var pet in GetSpawn().Except(before).OfType<BaseCreature>())
                HavenPetMissions.ApplyRarity(pet, HavenPetHabitats.SteedRarity(Utility.RandomDouble()));
        }
        public override void Serialize(GenericWriter writer) { base.Serialize(writer); writer.Write(0); writer.Write(Tiger); }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader); reader.ReadInt(); Tiger = reader.ReadBool();
            MinDelay = TimeSpan.FromSeconds(10); MaxDelay = TimeSpan.FromSeconds(15); NextSpawn = TimeSpan.FromSeconds(10);
        }
    }

    public static class HavenEodonHabitats
    {
        public static void Initialize()
        {
            EventSink.ServerStarted += () => { if (HavenPreview.Enabled && File.Exists("HAVEN-EODON-PETS")) Timer.DelayCall(TimeSpan.FromSeconds(8), Ensure); };
        }
        public static void Ensure()
        {
            for (int kind = 0; kind < 2; kind++)
            {
                bool tiger = kind == 1;
                if (World.Items.Values.OfType<HavenEodonPetSpawner>().Any(s => !s.Deleted && s.Tiger == tiger)) continue;
                string native = tiger ? "SabertoothedTiger" : "Triceratops";
                var anchors = World.Items.Values.OfType<Spawner>().Where(s => !s.Deleted && s.Map == Map.TerMur && s.Running && s.SpawnObjects.Any(o => o.SpawnName.Equals(native, StringComparison.OrdinalIgnoreCase))).OrderBy(s => s.Serial.Value).ToArray();
                bool installed = false;
                foreach (var anchor in anchors)
                {
                    for (int radius = 4; radius <= 8 && !installed; radius++)
                        for (int dx = -radius; dx <= radius && !installed; dx++)
                            for (int dy = -radius; dy <= radius && !installed; dy++)
                            {
                                int x = anchor.X + dx, y = anchor.Y + dy; var p = new Point3D(x, y, anchor.Map.GetAverageZ(x, y));
                                if (!anchor.Map.CanSpawnMobile(p) || BaseHouse.FindHouseAt(p, anchor.Map, 20) != null || !new MovementPath(anchor.Location, p, anchor.Map).Success) continue;
                                var spawner = new HavenEodonPetSpawner(tiger); spawner.MoveToWorld(p, anchor.Map); spawner.Spawn();
                                if (!spawner.GetSpawn().Any()) { spawner.Delete(); continue; }
                                File.AppendAllText("eodon-pets.log", DateTime.UtcNow.ToString("O") + " Installed " + native + " habitat at " + p + " " + anchor.Map + "\n"); installed = true;
                            }
                    if (installed) break;
                }
                if (!installed) installed = InstallSurveyed(tiger);
                if (!installed) File.AppendAllText("eodon-pets.log", DateTime.UtcNow.ToString("O") + " No safe habitat found for " + native + "\n");
            }
        }
        static bool InstallSurveyed(bool tiger)
        {
            var map = Map.TerMur;
            int centerX = 650, centerY = tiger ? 2250 : 2100;
            for (int radius = 0; radius <= 20; radius++)
                for (int dx = -radius; dx <= radius; dx++)
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius) continue;
                        int x = centerX + dx, y = centerY + dy;
                        var point = new Point3D(x, y, map.GetAverageZ(x, y));
                        if (!map.CanSpawnMobile(point) || BaseHouse.FindHouseAt(point, map, 20) != null) continue;
                        bool clear = true;
                        foreach (var offset in new[] { new Point2D(-3, -3), new Point2D(3, -3), new Point2D(3, 3), new Point2D(-3, 3) })
                        {
                            var edge = new Point3D(x + offset.X, y + offset.Y, map.GetAverageZ(x + offset.X, y + offset.Y));
                            if (!map.CanSpawnMobile(edge) || BaseHouse.FindHouseAt(edge, map, 20) != null || !new MovementPath(point, edge, map).Success) { clear = false; break; }
                        }
                        if (!clear) continue;
                        var site = new HavenEodonPetSpawner(tiger); site.MoveToWorld(point, map); site.Spawn();
                        if (!site.GetSpawn().Any()) { site.Delete(); continue; }
                        File.AppendAllText("eodon-pets.log", DateTime.UtcNow.ToString("O") + " Installed surveyed " + (tiger ? "tiger" : "triceratops") + " habitat at " + point + " " + map + "\n");
                        return true;
                    }
            return false;
        }
    }
}
