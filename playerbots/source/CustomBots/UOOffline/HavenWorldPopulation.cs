using System;
using System.Collections.Generic;
using System.IO;
using Server.Commands;
using Server.Engines.Spawners;
using Server.Json;
using Server.Logging;
using Server.Maps;
using Server.Mobiles;

namespace Server.UOOffline;

// Native ML world population, shared by automatic startup and the repair menu.
// Create missing spawners only; never replace a populated NPC or custom spawner.
public static class HavenWorldPopulation
{
    private static readonly ILogger Logger = LogFactory.GetLogger(typeof(HavenWorldPopulation));
    private static Queue<string> _files;
    private static Queue<SpawnerDto> _pending;
    private static readonly List<(Map Map, Point3D Location)> Banks = [];
    private static int _created;
    private static int _existing;
    private static int _failures;
    public static bool IsRunning => _files != null;
    public static string Status { get; private set; } = "World population has not started.";

    public static void Configure()
    {
        CommandSystem.Register("RepairWorld", AccessLevel.GameMaster, e => Start(e.Mobile));
        CommandSystem.Register("WorldStatus", AccessLevel.Player, e => e.Mobile.SendMessage(Status));
    }

    public static void Initialize() => Timer.DelayCall(TimeSpan.FromSeconds(5), () => Start());

    public static void Start(Mobile from = null)
    {
        if (IsRunning)
        {
            from?.SendMessage(Status);
            return;
        }

        var files = FindSpawnFiles(Core.BaseDirectory);
        if (files.Count == 0)
        {
            Status = "Spawn data is missing. Re-run the installer to restore Data/Spawns.";
            Logger.Error(Status);
            from?.SendMessage(Status);
            return;
        }

        Banks.Clear();
        _created = _existing = _failures = 0;
        _pending = new Queue<SpawnerDto>(CreateHavenDefinitions());
        _files = new Queue<string>(files);
        Status = "Restoring town NPCs, wildlife and dungeon creatures. Use Refresh to check progress.";
        from?.SendMessage(Status);
        Timer.DelayCall(TimeSpan.FromMilliseconds(100), ProcessBatch);
    }

    internal static List<string> FindSpawnFiles(string root)
    {
        var result = new List<string>();
        var flags = ExpansionInfo.CoreExpansion.MapSelectionFlags;
        foreach (var (name, flag) in new[]
                 {
                     ("felucca", MapSelectionFlags.Felucca), ("trammel", MapSelectionFlags.Trammel),
                     ("ilshenar", MapSelectionFlags.Ilshenar), ("malas", MapSelectionFlags.Malas),
                     ("tokuno", MapSelectionFlags.Tokuno), ("termur", MapSelectionFlags.TerMur)
                 })
        {
            if (!flags.Includes(flag))
            {
                continue;
            }

            // New Haven replaced old Haven after ML. Use the New Haven layout
            // on Trammel even though this shard retains ML gameplay elsewhere.
            var era = Core.SA || name == "trammel" ? "post-uoml" : "uoml";
            foreach (var folder in new[] { "shared", era })
            {
                var path = Path.Combine(root, "Data", "Spawns", folder, name);
                if (Directory.Exists(path))
                {
                    result.AddRange(Directory.GetFiles(path, "*.json", SearchOption.AllDirectories));
                }
            }
        }

        result.Sort((a, b) =>
        {
            var priority = IsVendorFile(b).CompareTo(IsVendorFile(a));
            return priority != 0 ? priority : StringComparer.Ordinal.Compare(a, b);
        });
        return result;
    }

    private static bool IsVendorFile(string path) =>
        Path.GetFileName(path).Equals("Vendors.json", StringComparison.OrdinalIgnoreCase);

    internal static IEnumerable<SpawnerDto> CreateHavenDefinitions()
    {
        if (!ExpansionInfo.CoreExpansion.MapSelectionFlags.Includes(MapSelectionFlags.Trammel))
        {
            yield break;
        }
        var questers = new[]
        {
            ("Aelorn", new Point3D(3527, 2516, 45)),
            ("Dimethro", new Point3D(3528, 2520, 25)),
            ("Churchill", new Point3D(3531, 2531, 20)),
            ("Robyn", new Point3D(3535, 2531, 20)),
            ("Recaro", new Point3D(3536, 2534, 20)),
            ("AldenArmstrong", new Point3D(3535, 2538, 20)),
            ("Jockles", new Point3D(3535, 2544, 20)),
            ("TylAriadne", new Point3D(3525, 2556, 20)),
            ("Alefian", new Point3D(3473, 2497, 72)),
            ("Gustar", new Point3D(3474, 2492, 91)),
            ("Jillian", new Point3D(3465, 2490, 71)),
            ("Kaelynna", new Point3D(3486, 2491, 52)),
            ("Mithneral", new Point3D(3485, 2491, 71)),
            ("AmeliaYoungstone", new Point3D(3459, 2529, 53)),
            ("AndreasVesalius", new Point3D(3457, 2550, 35)),
            ("Avicenna", new Point3D(3464, 2558, 35)),
            ("SarsmeaSmythe", new Point3D(3492, 2577, 15)),
            ("Ryuichi", new Point3D(3422, 2520, 21)),
            ("Chiyo", new Point3D(3420, 2516, 21)),
            ("Jun", new Point3D(3422, 2516, 21)),
            ("Walker", new Point3D(3429, 2518, 19)),
            ("Hamato", new Point3D(3493, 2414, 55)),
            ("Mulcivikh", new Point3D(3548, 2456, 15)),
            ("Morganna", new Point3D(3547, 2463, 15)),
            ("JacobWaltz", new Point3D(3504, 2741, 0)),
            ("GeorgeHephaestus", new Point3D(3471, 2542, 36))
        };
        foreach (var (name, location) in questers)
        {
            yield return TrainingSpawn("Haven instructor: " + name, location, 1, 0, name);
        }
        yield return TrainingSpawn("Haven training sentinel", new Point3D(3564, 2585, 0), 1, 0,
            "HavenTrainingSentinel");
        yield return TrainingSpawn("Haven meadow animals", new Point3D(3450, 2605, 10), 10, 18,
            "Rabbit", "Hind", "GreatHart", "Cow", "Goat", "Sheep");
        yield return TrainingSpawn("Haven beginner mounts", new Point3D(3506, 2640, 0), 8, 18,
            "Horse", "RidableLlama", "ForestOstard");
        yield return TrainingSpawn("Haven practice creatures", new Point3D(3560, 2585, 0), 10, 18,
            "GiantRat", "Mongbat", "HeadlessOne", "Slime");
    }

    private static SpawnerDto TrainingSpawn(string name, Point3D location, int count, int range, params string[] types)
    {
        var entries = new List<SpawnerEntry>();
        foreach (var type in types)
        {
            entries.Add(new SpawnerEntry(type, 100, count));
        }
        return new SpawnerDataDto
        {
            Guid = Guid.NewGuid(), Name = name, Map = Map.Trammel, Location = location,
            Count = count, HomeRange = range, WalkingRange = Math.Max(2, range),
            MinDelay = TimeSpan.FromSeconds(30), MaxDelay = TimeSpan.FromSeconds(60), Entries = entries
        };
    }

    private static void ProcessBatch()
    {
        try
        {
            // One modest JSON file per tick, followed by at most 20 world edits.
            // All world access stays on the game loop and yields between batches.
            if (_pending.Count == 0 && _files.Count > 0)
            {
                var file = _files.Dequeue();
                try
                {
                    var definitions = JsonConfig.Deserialize<List<SpawnerDto>>(file, SpawnerJsonSerializer.Options);
                    if (definitions != null)
                    {
                        foreach (var dto in definitions)
                        {
                            _pending.Enqueue(dto);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _failures++;
                    Logger.Error(ex, "Could not read spawn file {File}", file);
                }
            }

            for (var i = 0; i < 20 && _pending.Count > 0; i++)
            {
                var dto = _pending.Dequeue();
                try
                {
                    if (dto.Map == null || dto.Map == Map.Internal)
                    {
                        _failures++;
                        continue;
                    }

                    if (EnsureSpawner(dto))
                    {
                        _created++;
                    }
                    else
                    {
                        _existing++;
                    }

                    foreach (var entry in dto.Entries)
                    {
                        var type = AssemblyHandler.FindTypeByName(entry.SpawnedName);
                        if (type != null && typeof(Banker).IsAssignableFrom(type))
                        {
                            EnsureBank(dto.Map, dto.Location);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _failures++;
                    Logger.Error(ex, "Could not restore spawn {Guid} at {Location}", dto.Guid, dto.Location);
                }
            }

            Status = $"World population: {_created} added, {_existing} existing, {_failures} errors; {_files.Count} files remaining.";
            if (_files.Count == 0 && _pending.Count == 0)
            {
                _files = null;
                _pending = null;
                Status = $"World ready: {_created} spawners added, {_existing} preserved, {Banks.Count} banks supplied, {_failures} errors.";
                Logger.Information(Status);
                return;
            }
        }
        catch (Exception ex)
        {
            _files = null;
            _pending = null;
            Status = "World repair stopped after an error. Check the server log, then use Repair World again.";
            Logger.Error(ex, Status);
            return;
        }

        Timer.DelayCall(TimeSpan.FromMilliseconds(100), ProcessBatch);
    }

    internal static bool EnsureSpawner(SpawnerDto dto)
    {
        foreach (var existing in dto.Map.GetItemsAt<BaseSpawner>(dto.Location))
        {
            if (existing.Deleted || existing.Z != dto.Location.Z)
            {
                continue;
            }

            // Preserve custom entries at this location too. Repair should never
            // replace NPCs or multiply an existing population on every restart.
            existing.Running = true;
            if (existing.IsEmpty)
            {
                existing.Respawn();
            }
            return false;
        }

        var spawner = dto.ToSpawner();
        try
        {
            spawner.MoveToWorld(dto.Location, dto.Map);
            spawner.Running = true;
            spawner.Respawn();
            return true;
        }
        catch
        {
            spawner.Delete();
            throw;
        }
    }

    private static void EnsureBank(Map map, Point3D location)
    {
        foreach (var bank in Banks)
        {
            if (bank.Map == map && Utility.InRange(bank.Location, location, 18))
            {
                return;
            }
        }

        HavenContentBootstrap.EnsureBankServices(map, location);
        Banks.Add((map, location));
    }
}
