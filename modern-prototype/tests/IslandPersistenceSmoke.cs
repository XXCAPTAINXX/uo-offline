using System;
using System.IO;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;

public static class IslandPersistenceSmoke
{
    public static void Initialize()
    {
        if (!File.Exists("ISLAND-PERSIST")) return;
        EventSink.ServerStarted += Run;
    }

    static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    static void Run()
    {
        try
        {
            if (!File.Exists("island-persistence.state"))
            {
                HavenIslandFoundation.BuildTest();
                HavenIslandCommons.BuildTest();
                HavenIslandEncounters.BuildTest();
                var owner = new PlayerMobile { Player = true, Body = 0x190, RawStr = 250, Blessed = true };
                owner.AddItem(new Backpack());
                new Account("island-persistence-" + Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"))[0] = owner;
                owner.MoveToWorld(new Point3D(4210, 2928, 0), Map.Trammel);
                var estate = HavenIslandEstate.BuildTest(owner);
                var station = estate.Stations[0];
                owner.MoveToWorld(station.Location, station.Map);
                var item = new Gold(137);
                owner.Backpack.DropItem(item);
                Require(estate.Deposit(owner, station, item), "Initial deposit");
                var cove = HavenCoveEncounter.BuildTest();
                owner.MoveToWorld(cove.Location, cove.Map);
                Require(cove.Begin(owner, 1), "Initial cove start");
                World.Save(false, false);
                File.WriteAllText("island-persistence.state", owner.Serial.Value + "\n" + item.Serial.Value);
                File.AppendAllText("island-persistence.log", "PASS first process saved island, stores, owner, active cove and patrols\n");
            }
            else
            {
                var ids = File.ReadAllLines("island-persistence.state").Select(int.Parse).ToArray();
                var owner = World.FindMobile((Serial)ids[0]);
                var item = World.FindItem((Serial)ids[1]);
                var estate = World.Items.Values.OfType<HavenIslandEstate>().Single();
                Require(owner != null && estate.Owner == owner && owner.Account != null, "Owner/account reload");
                Require(item != null && item.Amount == 137 && item.Parent == estate.Vault, "Vault item reload");
                Require(estate.Stations.Count == 3 && estate.Stations.All(s => s.Estate == estate), "Storage references reload");
                owner.MoveToWorld(estate.Stations[1].Location, estate.Map);
                Require(estate.Withdraw(owner, estate.Stations[1], item), "Withdrawal after process restart");
                Require(!estate.Withdraw(owner, estate.Stations[1], item), "Restart withdrawal replay");
                var foundation = World.Items.Values.OfType<HavenIslandFoundation>().Single();
                var commons = World.Items.Values.OfType<HavenIslandCommons>().Single();
                Require(!foundation.CheckRoutes().Any(x => x.StartsWith("FAIL")), "Foundation routes after reload");
                Require(!commons.CheckRoutes().Any(x => x.StartsWith("FAIL")), "Commons routes after reload");
                var patrols = World.Items.Values.OfType<HavenIslandEncounters>().ToArray();
                Require(patrols.Length == 3 && patrols.All(p => p.Raiders.Count == 3 && p.Raiders.All(m => !m.Deleted)), "Patrol references reload");
                var cove = World.Items.Values.OfType<HavenCoveEncounter>().Single();
                Require(cove.Active && cove.Stage == 0 && cove.Remaining == 5, "Active cove reload");
                Require(HavenMiniChamp.Find().GetType() == typeof(HavenMiniChamp), "Haven lookup after reload");
                File.AppendAllText("island-persistence.log", "PASS second process loaded private stores, native owner/account, routes, three patrols and active cove; withdrawal and replay checks passed\n");
            }
        }
        catch (Exception ex) { File.AppendAllText("island-persistence.log", "FAIL " + ex + "\n"); }
        Core.Kill(false);
    }
}
