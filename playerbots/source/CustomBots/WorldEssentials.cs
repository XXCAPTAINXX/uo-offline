// =========================================================================
// WorldEssentials.cs — idempotent first-world bootstrap for UO Offline.
//
// A brand-new ModernUO world does not contain generated doors/teleporters
// until the stock generation commands are run. UO Offline must not require
// the player to know those admin commands, so the essentials are repaired
// automatically on world load:
//
//   * canonical world/dungeon teleporters from Data/teleporters.json
//   * generated doors when the world contains none
//   * native town vendors + New Haven custom services/questers
//
// Safe on existing saves: teleporters are only created when missing and door
// generation is only invoked if there are no dynamic doors in the world.
// =========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using Server.Commands;
using Server.Items;
using Server.Json;

namespace Server.CustomBots
{
    public static class WorldEssentials
    {
        private static readonly string TeleporterDataPath =
            Path.Combine(Core.BaseDirectory, "Data", "teleporters.json");

        public static void Configure()
        {
            CommandSystem.Register("WorldEssentials", AccessLevel.GameMaster, WorldEssentials_OnCommand);
            EventSink.WorldLoad += () => Timer.DelayCall(TimeSpan.FromSeconds(6), EnsureWorld);
        }

        public static void EnsureWorld()
        {
            try
            {
                int teleporters = EnsureTeleporters();

                if (!HasAnyGeneratedDoor())
                {
                    Console.WriteLine("[world-essentials] no generated doors found; running DoorGenerator...");
                    DoorGenerator.Generate();
                }

                int vendorFiles = NativeTownPopulation.ImportNativeVendors();
                NewbiePlayability.EnsureWorld();
                StarterHub.EnsureWorld();

                Console.WriteLine(
                    $"[world-essentials] ready: {teleporters} missing teleporters added; " +
                    $"{vendorFiles} native vendor file(s) refreshed."
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[world-essentials] bootstrap failed: {ex}");
            }
        }

        private static bool HasAnyGeneratedDoor()
        {
            foreach (var item in World.Items.Values)
            {
                if (item is BaseDoor { Deleted: false })
                {
                    return true;
                }
            }

            return false;
        }

        public static int EnsureTeleporters()
        {
            if (!File.Exists(TeleporterDataPath))
            {
                Console.WriteLine($"[world-essentials] teleporter data missing: {TeleporterDataPath}");
                return 0;
            }

            List<TeleporterDefinition> definitions;

            try
            {
                definitions = JsonConfig.Deserialize<List<TeleporterDefinition>>(TeleporterDataPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[world-essentials] could not read teleporters.json: {ex.Message}");
                return 0;
            }

            if (definitions == null)
            {
                return 0;
            }

            int added = 0;

            foreach (var def in definitions)
            {
                if (EnsureTeleporter(def.Source, def.Destination))
                {
                    added++;
                }

                if (def.Back && EnsureTeleporter(def.Destination, def.Source))
                {
                    added++;
                }
            }

            if (added > 0)
            {
                Console.WriteLine($"[world-essentials] generated {added} missing world/dungeon teleporters.");
            }

            return added;
        }

        private static bool EnsureTeleporter(WorldLocation source, WorldLocation destination)
        {
            if (source.Map == null || source.Map == Map.Internal ||
                destination.Map == null || destination.Map == Map.Internal)
            {
                return false;
            }

            foreach (var item in source.Map.GetItemsAt<Teleporter>(source))
            {
                if (item.Deleted || item is KeywordTeleporter or SkillTeleporter)
                {
                    continue;
                }

                if (Math.Abs(item.Z - source.Z) <= 12)
                {
                    return false;
                }
            }

            var teleporter = new Teleporter(destination, destination.Map);
            teleporter.MoveToWorld(source, source.Map);
            return true;
        }

        private static void WorldEssentials_OnCommand(CommandEventArgs e)
        {
            e.Mobile?.SendMessage("Checking doors, dungeon/world teleporters, vendors, and New Haven essentials...");
            EnsureWorld();
            e.Mobile?.SendMessage("World essentials check complete.");
        }
    }
}
