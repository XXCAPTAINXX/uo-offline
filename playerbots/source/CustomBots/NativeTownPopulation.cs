// =========================================================================
// NativeTownPopulation.cs
//
// Hybrid modern-world population:
//   * UORespawn owns dynamic ecology/wildlife/ordinary monster population.
//   * ModernUO's native vendor JSON owns banks, shops, guildmasters, healers,
//     stablemasters and other fixed town services.
//
// We intentionally import ONLY Vendors.json files. Do not broad-import the
// native dungeon/wildlife files here or they will layer static monsters over
// UORespawn.
//
// New Haven quest instructors are handled by NewbiePlayability because they
// are ML quest givers rather than ordinary vendor JSON entries.
// =========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Commands;
using Server.Engines.Spawners;

namespace Server.CustomBots
{
    public static class NativeTownPopulation
    {
        private static readonly string[] VendorFiles =
        {
            "Data/Spawns/post-uoml/felucca/Vendors.json",
            "Data/Spawns/shared/felucca/Vendors.json",

            "Data/Spawns/post-uoml/trammel/Vendors.json",
            "Data/Spawns/shared/trammel/Vendors.json",

            "Data/Spawns/shared/ilshenar/Vendors.json",

            "Data/Spawns/post-uoml/malas/Vendors.json",
            "Data/Spawns/shared/malas/Vendors.json",

            "Data/Spawns/shared/tokuno/Vendors.json",

            "Data/Spawns/post-uoml/termur/Vendors.json"
        };

        public static void Configure()
        {
            CommandSystem.Register("RefreshVendors", AccessLevel.GameMaster, Refresh_OnCommand);
            EventSink.WorldLoad += OnWorldLoad;
        }

        private static void OnWorldLoad()
        {
            if (!Core.ML)
            {
                return;
            }

            Timer.DelayCall(TimeSpan.FromSeconds(3), ImportNativeVendors);
        }

        public static int ImportNativeVendors()
        {
            if (!Core.ML)
            {
                return 0;
            }

            var allSpawners = new Dictionary<Guid, ISpawner>();

            foreach (var item in World.Items.Values)
            {
                if (item is ISpawner spawner)
                {
                    allSpawners[spawner.Guid] = spawner;
                }
            }

            int files = 0;

            foreach (var relative in VendorFiles)
            {
                var path = Path.Combine(
                    Core.BaseDirectory,
                    relative.Replace('/', Path.DirectorySeparatorChar)
                );

                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    ImportSpawnersCommand.ImportFile(new FileInfo(path), allSpawners);
                    files++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[native-vendors] failed {relative}: {ex.Message}");
                }
            }

            Console.WriteLine(
                $"[native-vendors] refreshed {files} vendor spawn file(s); UORespawn remains ecology-only."
            );

            return files;
        }

        private static void Refresh_OnCommand(CommandEventArgs e)
        {
            int files = ImportNativeVendors();
            NewbiePlayability.EnsureWorld();

            e.Mobile?.SendMessage(
                $"Refreshed {files} native vendor file(s) and New Haven quest instructors."
            );
        }
    }
}
