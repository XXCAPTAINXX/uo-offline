using System;
using System.IO;
using System.Linq;
using Server.Accounting;
using Server.Commands;
using Server.Engines.Shadowguard;
using Server.Engines.Doom;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    // Explicit bootstrap marker only; never automatically generates an existing shard.
    public static class HavenPreviewSetup
    {
        private const string Marker = "HAVEN-PREVIEW-SETUP";
        private const string Log = "preview-world-setup.log";
        private static readonly int[] Stages = { 101,102,103,104,105,106,107,108,109,110,113,114,115,116,117,118,119,120,121,122,123,124,125,126,128 };
        private static PlayerMobile _operator;
        private static int _index;

        public static void Initialize()
        {
            EventSink.ServerStarted += () => {
                if (File.Exists(Marker)) Timer.DelayCall(TimeSpan.FromSeconds(2), Begin);
            };
        }

        private static void Write(string text) { File.AppendAllText(Log, DateTime.UtcNow.ToString("O") + " " + text + Environment.NewLine); }
        private static void Begin()
        {
            try
            {
                if (Accounts.Count > 0) throw new InvalidOperationException("Bootstrap only supports a fresh preview with no accounts.");
                if (!Core.SA || !Core.HS || !Core.TOL) throw new InvalidOperationException("Modern expansion configuration required.");
                if (File.Exists("preview-world-ready.txt"))
                {
                    Validate();
                    Write("RELOAD VALIDATED; no setup commands rerun");
                    if (File.Exists("preview-world-failed.txt")) File.Delete("preview-world-failed.txt");
                    File.Delete(Marker);
                    Core.Kill(false);
                    return;
                }
                // Native Initialize methods create fixtures even before first world save.
                // Saved entity data, not that transient count, distinguishes an existing world.
                if (File.Exists(World.ItemIndexPath) && !File.Exists("preview-world-started.txt")) throw new InvalidOperationException("Refusing to bootstrap an unknown saved world.");
                File.WriteAllText("preview-world-started.txt", "Authorized preview bootstrap");
                Write("BEGIN native staged world generation");
                _operator = new PlayerMobile { Name = "preview setup operator", Player = true, Body = 0x190, AccessLevel = AccessLevel.Owner };
                _operator.MoveToWorld(new Point3D(1496, 1628, 10), Map.Trammel);
                Next();
            }
            catch (Exception ex) { Fail(ex); }
        }

        private static void Next()
        {
            try
            {
                if (_index == Stages.Length)
                {
                    if (CommandSystem.Entries.ContainsKey("CheckFillables".ToLowerInvariant())) CommandSystem.Handle(_operator, CommandSystem.Prefix + "CheckFillables");
                    _operator.Delete(); _operator = null;
                    World.Save(false, false);
                    Validate();
                    File.WriteAllText("preview-world-ready.txt", "Native modern preview world generated " + DateTime.UtcNow.ToString("O"));
                    if (File.Exists("preview-world-failed.txt")) File.Delete("preview-world-failed.txt");
                    Write("COMPLETE; world saved; ready for separate reload verification");
                    Timer.DelayCall(TimeSpan.FromSeconds(1), () => Core.Kill(false));
                    return;
                }
                int id = Stages[_index++];
                var stage = CreateWorld.Commands.First(x => x.CheckID == id);
                if (CreateWorldData.HasGenerated(id)) Write("SKIP saved stage " + stage.Name);
                else
                {
                    Write("START " + stage.Name + " [" + stage.CreateCommand + "]");
                    CreateWorld.DoCommands(new[] { id }, CreateWorld.GumpType.Create, _operator);
                    if (!CreateWorldData.HasGenerated(id)) throw new InvalidOperationException("Stage did not record completion: " + stage.Name);
                    World.Save(false, false);
                    Write("SAVED " + stage.Name + "; items=" + World.Items.Count + "; mobiles=" + World.Mobiles.Count);
                }
                Timer.DelayCall(TimeSpan.FromSeconds(3), Next);
            }
            catch (Exception ex) { Fail(ex); }
        }

        private static void Validate()
        {
            foreach (int id in Stages) if (!CreateWorldData.HasGenerated(id)) throw new Exception("Missing saved setup stage " + id);
            if (ShadowguardController.Instance == null || ShadowguardController.Instance.Instances.Count < 14) throw new Exception("Missing Shadowguard instances");
            var items = World.Items.Values.ToArray();
            int spawners = items.Count(x => x.GetType().Name.Contains("Spawner"));
            int gauntlets = items.Count(x => x is GauntletSpawner);
            // Native Trammel entrance is beneath the castle; Felucca uses the older stairway.
            var links = items.OfType<Teleporter>().ToArray();
            for (int i = 0; i < 5; ++i)
            {
                RequireLink(links, Map.Trammel, new Point3D(1477, 1471 + i, -8), new Point3D(6432, 2677 + i, 0));
                RequireLink(links, Map.Trammel, new Point3D(6440, 2677 + i, 20), new Point3D(1477, 1471 + i, -8));
            }
            for (int i = 0; i < 4; ++i)
                RequireLink(links, Map.Felucca, new Point3D(1517, 1417 + i, i == 3 ? 12 : 9), new Point3D(6440, 2677 + i, 20));
            if (spawners < 1000) throw new Exception("Too few world spawners: " + spawners);
            if (gauntlets < 5) throw new Exception("Missing Doom rooms: " + gauntlets);
            Write("VALIDATED items=" + items.Length + "; mobiles=" + World.Mobiles.Count + "; spawners=" + spawners + "; Doom=" + gauntlets + "; Blackthorn native links=14; Shadowguard instances=" + ShadowguardController.Instance.Instances.Count);
        }
        private static void RequireLink(Teleporter[] links, Map map, Point3D source, Point3D destination)
        {
            if (!links.Any(x => x.Map == map && x.Location == source && x.MapDest == map && x.PointDest == destination))
                throw new Exception("Missing travel link on " + map + ": " + source + " -> " + destination);
        }
        private static void Fail(Exception ex)
        {
            Write("FAILED " + ex);
            if (_operator != null) _operator.Delete();
            File.WriteAllText("preview-world-failed.txt", ex.ToString());
            Timer.DelayCall(TimeSpan.FromSeconds(1), () => Core.Kill(false));
        }
    }
}
