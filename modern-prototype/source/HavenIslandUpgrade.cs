using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Server.HavenPrototype
{
    // Explicit operator request only; normal startup never replaces a house.
    public static class HavenIslandUpgrade
    {
        public const string Request = "HAVEN-UPGRADE-COURTYARD";
        internal static bool Applying { get; private set; }

        public static void Initialize()
        {
            EventSink.ServerStarted += () => { if (File.Exists(Request)) Run(); };
        }

        internal static void VerifyAssets()
        {
            var names = new[] { "multi.idx", "multi.mul", "MultiCollection.uop" };
            var hashes = new[] {
                "e25332404b5985839ef308a52fe333e6be53babdccf6f99b5d06d8f7e5c7fe7d",
                "d49244f7170488cc8f026f0fe552adc8c10e7f37ec040eb64e0b320a0f1c473c",
                "7bded48e7159a7d35700dfbe07a4f219b15dcb48489cff2891b33d4b1855b108" };
            for (int i = 0; i < names.Length; i++)
                using (var sha = SHA256.Create())
                using (var stream = File.OpenRead(Core.FindDataFile(names[i])))
                    if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant() != hashes[i])
                        throw new InvalidOperationException("Courtyard asset mismatch: " + names[i]);
        }

        internal static HavenRecoveredHeadquarters Apply(HavenIslandEstate old)
        {
            VerifyAssets();
            if (old == null || old.Map != Map.Trammel || old.Location != new Point3D(4196, 2868, 0))
                throw new InvalidOperationException("Unexpected island estate");
            if (World.Items.Values.OfType<HavenRecoveredHeadquarters>().Any(h => !h.Deleted))
                throw new InvalidOperationException("Custom courtyard already exists");
            Applying = true;
            try
            {
                foreach (var patrol in World.Items.Values.OfType<HavenIslandEncounters>().ToArray()) patrol.EnsurePatrol();
                var camp = World.Items.Values.OfType<HavenCoveEncounter>().Single(c => !c.Deleted);
                if (!World.Items.Values.OfType<HavenCoveApproach>().Any(c => !c.Deleted)) HavenCoveApproach.BuildTest(camp);
                return HavenRecoveredHouseMigration.Rehearse(old);
            }
            finally { Applying = false; }
        }

        static void Run()
        {
            try
            {
                if (!HavenPreview.Enabled) throw new InvalidOperationException("Preview opt-in missing");
                int serial;
                if (!int.TryParse(File.ReadAllText(Request).Trim(), out serial)) throw new InvalidOperationException("Expected estate serial");
                var house = Apply(World.FindItem((Serial)serial) as HavenIslandEstate);
                World.Save(false, false);
                File.Move(Request, Request + ".completed");
                File.AppendAllText("island-upgrade.log", DateTime.UtcNow.ToString("O") + " PASS courtyard=" + house.Serial.Value + " owner=" + house.Owner.Serial.Value + " vault=" + house.Vault.Serial.Value + " fixtures=" + house.CompanyFixtures.Count + " saved\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("island-upgrade.log", DateTime.UtcNow.ToString("O") + " FAIL " + ex + "\nRestore the stopped backup if saving began. Startup stopped; no further autosave.\n");
                Core.Kill(false);
            }
        }
    }
}
