using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server.Accounting;
using Server.Commands;
using Server.Mobiles;
using Server.Saves;

namespace Server.UOOffline;

// Local release automation only. Disabled unless the process owner explicitly
// supplies an absolute inbox directory in its environment. No network endpoint,
// arbitrary command execution, background game-state access or saved world item.
public static class HavenReleaseOperations
{
    private static Timer _timer;
    private static string _inbox;
    public static void Initialize()
    {
        var path = Environment.GetEnvironmentVariable("HAVEN_RELEASE_INBOX");
        if (string.IsNullOrWhiteSpace(path) || !Path.IsPathFullyQualified(path)) { return; }
        _inbox = Path.GetFullPath(path);
        Directory.CreateDirectory(_inbox);
        _timer = Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), Poll);
        EventSink.Shutdown += () => { _timer?.Stop(); _timer = null; };
    }
    private static void Poll()
    {
        var requestPath = Path.Combine(_inbox, "request.json");
        if (!File.Exists(requestPath)) { return; }
        var processing = Path.Combine(_inbox, "processing.json");
        string id = "unknown";
        try
        {
            if (new FileInfo(requestPath).Length > 4096) { throw new InvalidOperationException("Release request exceeds 4 KB."); }
            File.Move(requestPath, processing, true);
            using var request = JsonDocument.Parse(File.ReadAllText(processing));
            id = request.RootElement.GetProperty("id").GetString();
            var action = request.RootElement.GetProperty("action").GetString();
            switch (action)
            {
                case "status": break;
                case "companion-gear-grind":
                    var grindOwner=World.FindMobile((Serial)request.RootElement.GetProperty("ownerSerial").GetUInt32());
                    HavenCompanionGearAssignment.Begin(HavenCompanionGearAssignment.Find(grindOwner),Core.Now);
                    break;
                case "original-dungeons": HavenOriginalDungeons.Migrate(); break;
                case "save": AutoSave.Save(); break;
                case "frontiers":
                    if (!HavenFrontierHub.Install()) { throw new InvalidOperationException($"Frontier setup refused: {HavenFrontierHub.PreflightReason() ?? "partial installation"}. No automatic world purge is allowed."); }
                    break;
                case "install":
                    var serial = request.RootElement.GetProperty("ownerSerial").GetUInt32();
                    if (World.FindMobile((Serial)serial) is not PlayerMobile owner || owner.Deleted || owner.Account == null)
                    { throw new InvalidOperationException("An existing account character is required as island owner."); }
                    Install(owner);
                    break;
                default: throw new InvalidOperationException("Allowed release actions: status, install, frontiers, original-dungeons, companion-gear-grind, save.");
            }
            Write(id, true, null);
        }
        catch (Exception error) { Write(id, false, error.ToString()); }
        finally { if (File.Exists(processing)) { File.Delete(processing); } }
    }
    private static void Install(PlayerMobile owner)
    {
        // Existing setup commands own their fixtures and preserve an existing setup.
        var previous = owner.AccessLevel;
        try
        {
            owner.AccessLevel = AccessLevel.Owner;
            foreach (var command in new[] { "HavenIslandsBuild", "HavenDoomSetup", "HavenAbyssSetup", "HavenAbyssRestore", "HavenSnowSetup" })
            {
                if (!CommandSystem.Handle(owner, $"[{command}")) { throw new InvalidOperationException($"Setup command unavailable: {command}"); }
            }
        }
        finally { owner.AccessLevel = previous; }
        if (HavenCommunityCenter.Registry.Count != 1 || HavenPirateEstate.Registry.Count != 1 ||
            HavenDoom.Controllers().Count != 6 || HavenAbyssTrial.Registry.Count != 1 ||
            HavenAbyssExpedition.Registry.Count != 1 || HavenSnowBearDen.Registry.Count != 1)
        { throw new InvalidOperationException("Setup is partial. Inspect the release status before saving or retrying."); }
    }
    private static void Write(string id, bool success, string error)
    {
        var characters = new List<object>();
        var companions = new List<object>();
        foreach (var account in Accounts.GetAccounts())
        {
            for (var i = 0; i < account.Length; i++)
            {
                if (account[i] is PlayerMobile player && !player.Deleted)
                {
                    var companion=HavenCompanionGearAssignment.Find(player);
                    if(companion!=null)
                    {
                        var assignment=companion.Backpack?.FindItemByType<HavenCompanionGearAssignment>();
                        var journal=HavenMissionJournal.Find(companion);
                        companions.Add(new { OwnerSerial=player.Serial.Value,Serial=companion.Serial.Value,companion.Name,
                            Level=companion.TrainingLevel,companion.RawStr,companion.RawDex,companion.RawInt,
                            PackItems=companion.Backpack?.TotalItems,Expedition=companion.Expedition?.Status,
                            GearGrind=assignment==null ? null : new { assignment.Running,assignment.Completed,assignment.NextReward,assignment.Status },
                            MissionJournal=journal==null ? null : new { journal.Active,journal.Unread,Reports=journal.Reports.Count,Latest=journal.Reports[0] } });
                    }
                    characters.Add(new { Serial = player.Serial.Value, player.Name, Access = player.AccessLevel.ToString(),
                        Map = player.Map?.Name, player.X, player.Y, player.Z, player.Alive });
                }
            }
        }
        var expeditions = new List<object>();
        foreach (var board in HavenAbyssExpedition.Registry)
        { expeditions.Add(new { Serial = board.Serial.Value, Sites = board.Sites.Count, Fixtures = board.Fixtures.Count }); }
        var estates = new List<object>();
        foreach (var estate in HavenPirateEstate.Registry)
        { estates.Add(new { Owner = estate.Owner?.Name, OwnerSerial = (estate.Owner?.Serial ?? Serial.Zero).Value, Fixtures = estate.Fixtures.Count }); }
        var market = new List<object>();
        var dungeonCrews = new List<object>();
        var seenCrews = new HashSet<HavenDungeonCrew>();
        foreach (var crew in HavenDungeonCrew.Registry.Values)
        {
            if (crew.Deleted || !seenCrews.Add(crew)) { continue; }
            var workers = new List<object>();
            foreach (var worker in crew.Workers)
            { if (worker?.Deleted == false) { workers.Add(new { worker.Name, Serial = worker.Serial.Value, Map = worker.Map?.Name, worker.X, worker.Y, worker.Z, worker.Alive, worker.Hits, worker.HitsMax, Enemy = (worker.Combatant as Mobile)?.Name, Behavior = worker.Behavior?.SerializableName }); } }
            dungeonCrews.Add(new { Route = HavenMarketExpedition.RouteName(crew.Route), crew.Room, crew.Started, crew.Ends, Workers = workers });
        }
        foreach (var stall in HavenMarketStall.Registry)
        {
            if (stall.Deleted) { continue; }
            HavenMarketExpedition job = null;
            foreach (var item in stall.Items) { if (item is HavenMarketExpedition found) { job = found; break; } }
            var stock = new List<object>();
            for (var i = 0; i < stall.Stock.Count && i < stall.Prices.Count; i++)
            {
                var item = stall.Stock[i];
                if (item?.Deleted == false && item.Parent == stall)
                { stock.Add(new { Serial = item.Serial.Value, Name = HavenMarketDirectory.Describe(item), Price = stall.Prices[i], Source = HavenMarketProvenance.Describe(item) }); }
            }
            market.Add(new { Serial = stall.Serial.Value, Trade = stall.Trade.ToString(), Artisan = stall.Artisan?.Name, Stock = stock,
                Job = job == null ? null : new { Route = HavenMarketExpedition.RouteName(job.Route), job.Progress, job.Completed, job.Failed, job.Credits }, stall.NextWork });
        }
        var data = new { Id = id, Success = success, Error = error, Time = Core.Now, Market = market, DungeonCrews = dungeonCrews,
            Mobiles = World.Mobiles.Count, Items = World.Items.Count, Characters = characters, Companions=companions,
            Commons = HavenCommunityCenter.Registry.Count, Estates = estates, DoomControllers = HavenDoom.Controllers().Count,
            AncientHunts = HavenAbyssTrial.Registry.Count, AbyssExpeditions = expeditions, SnowDens = HavenSnowBearDen.Registry.Count,
            FrontierHubs = HavenFrontierHub.Registry.Count, ShadowRooms = HavenShadowChamber.Registry.Count,
            FrontierBattles = HavenFrontierBattle.Registry.Count, Chelonia = HavenChelonia.Registry.Count,
            OriginalDungeons = HavenOriginalDungeons.Installed };
        File.WriteAllText(Path.Combine(_inbox, "response.json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }
}
