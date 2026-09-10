using System;
using System.Linq;
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
    private static string _operation;
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
            _operation = null;
            var action = request.RootElement.GetProperty("action").GetString();
            switch (action)
            {
                case "status": break;
                case "companion-gold":
                    var goldOwner = World.FindMobile((Serial)request.RootElement.GetProperty("ownerSerial").GetUInt32());
                    var goldCompanion = HavenCompanionGearAssignment.Find(goldOwner);
                    if (goldCompanion?.BoundOwner != goldOwner || goldCompanion == null) { throw new InvalidOperationException("Owner has no companion."); }
                    _operation = $"Combined companion gold; freed {HavenCompanionGold.Consolidate(goldCompanion.Backpack)} pack slots.";
                    break;
                case "companion-ledger":
                    var ledgerOwner = World.FindMobile((Serial)request.RootElement.GetProperty("ownerSerial").GetUInt32());
                    _operation = $"Stored {HavenResourceLedger.StoreCompanionPack(ledgerOwner)} supported companion deeds.";
                    break;
                case "guild-castle":
                    var castleOwner = World.FindMobile((Serial)request.RootElement.GetProperty("ownerSerial").GetUInt32());
                    var castle = HavenPirateHeadquarters.Install(castleOwner);
                    HavenIslandDecoration.Apply(castle.Estate, castle);
                    _operation = $"Rare Export Company headquarters ready at {castle.Location}, with {castle.CompanyFixtures.Count} furnishings and linked guild storage.";
                    break;
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
                default: throw new InvalidOperationException("Allowed release actions: status, install, frontiers, original-dungeons, companion-gear-grind, companion-ledger, companion-gold, guild-castle, save.");
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
                        var pendingDeeds = 0; var ledgerCount = 0; long ledgerUnits = 0;
                        foreach (var deed in companion.Backpack.FindItemsByType<Server.Items.CommodityDeed>()) { if (!deed.Deleted) { pendingDeeds++; } }
                        foreach (var book in companion.Backpack.FindItemsByType<HavenResourceLedger>())
                        { ledgerCount++; foreach (var amount in book.Balances) { ledgerUnits += amount; } }
                        companions.Add(new { OwnerSerial=player.Serial.Value,Serial=companion.Serial.Value,companion.Name,
                            Role = companion.Role.ToString(), Order = companion.ControlOrder.ToString(), companion.Criminal,
                            PendingDeeds = pendingDeeds, LedgerCount = ledgerCount, LedgerUnits = ledgerUnits,
                            Level=companion.TrainingLevel,companion.RawStr,companion.RawDex,companion.RawInt,
                            PackItems=companion.Backpack?.TotalItems,Expedition=companion.Expedition?.Status,
                            GearGrind=assignment==null ? null : new { assignment.Running,assignment.Completed,assignment.NextReward,assignment.Status },
                            MissionJournal=journal==null ? null : new { journal.Active,journal.Unread,Reports=journal.Reports.Count,Latest=journal.Reports[0] } });
                    }
                    characters.Add(new { Serial = player.Serial.Value, player.Name, Access = player.AccessLevel.ToString(),
                        Guild=player.Guild?.Name, Map = player.Map?.Name, player.X, player.Y, player.Z, player.Alive, player.Criminal,
                        HomeTravel = HavenPirateEstate.HomeStatus(player) });
                }
            }
        }
        var expeditions = new List<object>();
        foreach (var board in HavenAbyssExpedition.Registry)
        { expeditions.Add(new { Serial = board.Serial.Value, Sites = board.Sites.Count, Fixtures = board.Fixtures.Count }); }
        var estates = new List<object>();
        foreach (var estate in HavenPirateEstate.Registry)
        {
            HavenPirateHeadquarters house = null;
            foreach (var candidate in HavenPirateHeadquarters.Registry) { if (!candidate.Deleted && candidate.Owner == estate.Owner) { house = candidate; break; } }
            estates.Add(new { Owner = estate.Owner?.Name, OwnerSerial = (estate.Owner?.Serial ?? Serial.Zero).Value, Fixtures = estate.Fixtures.Count,
                Decoration = HavenIslandDecoration.Snapshot(estate, house),
                PatrolBoard = estate.HomePatrol == null ? null : new { Serial = estate.HomePatrol.Serial.Value, estate.HomePatrol.X, estate.HomePatrol.Y, estate.HomePatrol.Z },
                MiniChampion = estate.HomeTrial == null ? null : new { Serial = estate.HomeTrial.Serial.Value, estate.HomeTrial.Name, estate.HomeTrial.X, estate.HomeTrial.Y, estate.HomeTrial.Stage },
                Headquarters = house == null ? null : new { Serial = house.Serial.Value, house.Name, house.X, house.Y, house.Z,
                    Customizable = true, Width = house.Components.Width, Height = house.Components.Height, DesignTiles = house.Components.List.Length, StorageItems = house.Secures.Sum(s => s.Item.TotalItems), Fixtures = house.CompanyFixtures.Count, MasterChest = house.MasterStorage?.Serial.Value, Stores = house.MasterStorage?.FindLinked().Count } });
        }
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
        var bosses = new List<object>();
        foreach (var hunt in HavenScalisHunt.Registry)
        { if (!hunt.Deleted) { bosses.Add(new { Kind = "Scalis", Map = hunt.Map?.Name, Serial = hunt.Boss?.Serial.Value, hunt.Boss?.Alive, hunt.Boss?.Hits, hunt.Boss?.X, hunt.Boss?.Y, hunt.Boss?.Z, hunt.NextSpawn }); } }
        foreach (var lair in HavenBossLair.Registry)
        { if (!lair.Deleted) { bosses.Add(new { Kind = lair.Kind == 0 ? "Cora" : "Corgul", Map = lair.Map?.Name, Serial = lair.Boss?.Serial.Value, lair.Boss?.Alive, lair.Boss?.Hits, lair.Boss?.X, lair.Boss?.Y, lair.Boss?.Z, lair.NextSpawn, Arrival = lair.Arrival.ToString() }); } }
        var fleets = new List<object>();
        foreach (var fleet in HavenFishingFleet.Registry)
        {
            if (fleet.Deleted) { continue; }
            var passenger = fleet.Passenger(); var crew = new List<object>();
            foreach (var sailor in fleet.Sailors)
            { if (sailor?.Deleted == false) { crew.Add(new { sailor.Name, sailor.X, sailor.Y, sailor.Z, sailor.Alive, sailor.Hits, PlayerOwner = HavenBotLoot.PlayerOwner(sailor)?.Serial.Value, Retained = HavenGuildCrew.Retained(sailor) }); } }
            fleets.Add(new { Ship = fleet.Boat?.ShipName, Serial = fleet.Boat?.Serial.Value, fleet.Boat?.X, fleet.Boat?.Y, fleet.Boat?.Z, fleet.Status, Work = fleet.Work.ToString(), fleet.Trips, fleet.Catches, fleet.Wrecks, fleet.Nets, fleet.Sold, Cargo = fleet.Cargo.Count, Crew = crew,
                Passenger = passenger == null ? null : new { passenger.Name, Serial = passenger.Serial.Value, passenger.Z } });
        }
        var data = new { Operation = _operation, FishingFleet = fleets, Bosses = bosses, Id = id, Success = success, Error = error, Time = Core.Now, Market = market, DungeonCrews = dungeonCrews,
            RareExportGuild = Server.Guilds.BaseGuild.FindByName("Rare Export Company")?.Name, Mobiles = World.Mobiles.Count, Items = World.Items.Count, Characters = characters, Companions=companions,
            Commons = HavenCommunityCenter.Registry.Count, Estates = estates, DoomControllers = HavenDoom.Controllers().Count,
            AncientHunts = HavenAbyssTrial.Registry.Count, AbyssExpeditions = expeditions, SnowDens = HavenSnowBearDen.Registry.Count,
            FrontierHubs = HavenFrontierHub.Registry.Count, ShadowRooms = HavenShadowChamber.Registry.Count,
            FrontierBattles = HavenFrontierBattle.Registry.Count, Chelonia = HavenChelonia.Registry.Count,
            OriginalDungeons = HavenOriginalDungeons.Installed };
        File.WriteAllText(Path.Combine(_inbox, "response.json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }
}
