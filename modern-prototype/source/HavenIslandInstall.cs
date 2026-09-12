using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Server.Accounting;
using Server.Commands;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public static class HavenIslandInstall
    {
        const string Request = "HAVEN-INSTALL-ISLAND";
        const string MapHash = "5e8f232f803a1f4fe080df3ca33b75e333e0b53484ec2582e2921f488b32a48f";
        static bool _installing;
        public static bool CanBuild { get { return _installing || File.Exists("ISLAND-TEST-ONLY"); } }
        public static bool Installed { get { return World.Items.Values.OfType<HavenIslandEstate>().Any(x => !x.Deleted) && World.Items.Values.OfType<HavenIslandFoundation>().Any(x => !x.Deleted) && World.Items.Values.OfType<HavenIslandCommons>().Any(x => !x.Deleted) && World.Items.Values.OfType<HavenCoveEncounter>().Any(x => !x.Deleted) && World.Items.Values.OfType<HavenIslandEncounters>().Count(x => !x.Deleted) == 3; } }

        public static void Initialize()
        {
            CommandSystem.Register("island", AccessLevel.Player, e => {
                int index = Array.FindIndex(HavenPreview.Destinations, d => d.Name == "Corsair island estate");
                if (!Installed || !HavenPreview.Travel(e.Mobile, index)) e.Mobile.SendMessage("Island travel is unavailable. Leave combat and try again.");
            });
            EventSink.ServerStarted += () => { if (File.Exists(Request)) Install(); };
        }

        static void VerifyMap(string name)
        {
            string path = Core.FindDataFile(name);
            if (path == null) throw new InvalidOperationException("Missing island map: " + name);
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != MapHash)
                    throw new InvalidOperationException("Island map hash mismatch: " + name);
        }

        static void Install()
        {
            var created = new List<Item>();
            bool saved = false;
            bool saveStarted = false;
            try
            {
                if (!HavenPreview.Enabled) throw new InvalidOperationException("Haven server opt-in missing");
                VerifyMap("map1.mul"); VerifyMap("map1x.mul");
                if (Core.FindDataFile("map1LegacyMUL.uop") != null || Core.FindDataFile("map1xLegacyMUL.uop") != null)
                    throw new InvalidOperationException("Trammel UOP could override installed MUL terrain");
                var request = File.ReadAllLines(Request);
                if (request.Length != 2) throw new InvalidOperationException("Expected owner account and character name");
                var owners = World.Mobiles.Values.OfType<PlayerMobile>().Where(p => !p.Deleted && p.Name == request[1] && (p.Account as Account)?.Username == request[0]).ToArray();
                if (owners.Length != 1) throw new InvalidOperationException("Island owner must resolve uniquely");
                if (World.Items.Values.Any(i => i is HavenIslandFoundation || i is HavenIslandCommons || i is HavenIslandEstate || i is HavenCoveEncounter || i is HavenIslandEncounters))
                    throw new InvalidOperationException("Island entities already exist; refusing duplicate installation");
                _installing = true;
                var foundation = HavenIslandFoundation.BuildTest(); created.Add(foundation);
                var commons = HavenIslandCommons.BuildTest(); created.Add(commons);
                created.Add(HavenIslandEstate.BuildTest(owners[0]));
                created.AddRange(HavenIslandEncounters.BuildTest());
                created.Add(HavenCoveEncounter.BuildTest());
                if (foundation.CheckRoutes().Concat(commons.CheckRoutes()).Any(r => r.StartsWith("FAIL")))
                    throw new InvalidOperationException("Island routes failed");
                saveStarted = true;
                World.Save(false, false);
                saved = true;
                File.Move(Request, Request + ".installed");
                File.AppendAllText("island-install.log", DateTime.UtcNow.ToString("O") + " PASS installed and saved foundation, commons, owner castle, 3 patrols and cove; map hashes/routes verified\n");
            }
            catch (Exception ex)
            {
                if (saveStarted && !saved)
                {
                    File.AppendAllText("island-install.log", DateTime.UtcNow.ToString("O") + " FATAL save failed; restore the complete preinstallation backup before restart. " + ex + "\n");
                    Core.Kill(false);
                    return;
                }
                if (!saved) foreach (var item in created.AsEnumerable().Reverse()) if (!item.Deleted) item.Delete();
                File.AppendAllText("island-install.log", DateTime.UtcNow.ToString("O") + " FAIL " + ex + "\n");
                if (File.Exists(Request)) File.Move(Request, Request + ".failed");
            }
            finally { _installing = false; }
        }
    }
}
