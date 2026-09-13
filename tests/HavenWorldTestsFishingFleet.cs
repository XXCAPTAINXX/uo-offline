using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Accounting.Security;
using Server.CustomBots;
using Server.Engines.Harvest;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Spells.SkillMasteries;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsFishingFleet
{
    public HavenWorldTestsFishingFleet() { _ = new HavenWorldTestsMarket(); MasteryInfo.Configure(); }
    private static HavenFishingFleet Fleet()
    {
        var p = HavenFishingFleet.Harbor;
        var boat = new SmallBoat(); Assert.True(boat.CanFit(p, Map.Trammel, boat.NorthID)); boat.MoveToWorld(p, Map.Trammel);
        var crew = new HavenFishingFleet { Boat = boat, Home = p, Work = HavenSeaWork.Fishing, Due = Core.Now + TimeSpan.FromMinutes(3), ReturnBy = Core.Now + TimeSpan.FromMinutes(18) };
        for (var role = 0; role < 2; role++)
        {
            var sailor = new HavenFishingSailor(role) { Fleet = crew }; crew.Sailors.Add(sailor);
            sailor.MoveToWorld(new Point3D(p.X, p.Y + (role == 0 ? 1 : -1), p.Z + 3), Map.Trammel); sailor.Hits = sailor.HitsMax;
        }
        boat.Owner = crew.Captain; crew.Trail.Add(p); return crew;
    }
    private static void Clean(HavenFishingFleet crew)
    {
        var boat = crew.Boat; var sailors = crew.Sailors.ToArray(); crew.Delete();
        foreach (var sailor in sailors) { sailor.Delete(); } boat.Delete();
    }
    [SkippableFact]
    public void NativeBoatMovesCrewAndNeverMovesThroughBlockedWater()
    {
        TileDataRequirement.SkipIfMissing(); var fleet = Fleet();
        try
        {
            var initial = fleet.Captain.Location; Assert.True(fleet.Boat.Contains(fleet.Captain));
            Assert.True(fleet.Sail(new Point3D(fleet.Home.X + 4, fleet.Home.Y, -5)));
            Assert.Equal(initial.X + 4, fleet.Captain.X); Assert.Equal(initial.Y, fleet.Captain.Y);
            Assert.False(HavenSeaNavigation.Clear(fleet.Boat, fleet.Boat.Location, HavenScalisHunt.RoamingWaters));
            Assert.NotNull(fleet.WaterTarget()); Assert.True(fleet.Fish());
        }
        finally { Clean(fleet); }
    }
    [SkippableFact]
    public void IncrementalCourseReachesFishingGroundsAndReturnsAlongActualTrail()
    {
        TileDataRequirement.SkipIfMissing(); var fleet = Fleet();
        try
        {
            var destination = new Point3D(fleet.Home.X + 100, fleet.Home.Y + 40, -5);
            var route = new HavenSeaNavigation(fleet.Boat.Location, destination, 4);
            for (var i = 0; i < 300 && !route.Finished && !route.Failed; i++) { route.Advance(fleet.Boat); }
            Assert.True(route.Finished); Assert.False(route.Failed); Assert.NotEmpty(route.Path);
            foreach (var step in route.Path) { Assert.True(fleet.Sail(step)); }
            Assert.True(Utility.InRange(fleet.Boat.Location, destination, 4));
            fleet.BeginReturn();
            for (var i = 0; i < 100 && fleet.Work != HavenSeaWork.Docked; i++) { fleet.Tick(Core.Now); }
            Assert.Equal(HavenSeaWork.Docked, fleet.Work); Assert.Equal(fleet.Home, fleet.Boat.Location);
        }
        finally { Clean(fleet); }
    }
    [SkippableFact]
    public void NativeFishingDeliveryStowsOnlyEarnedCatchAndListsSameItem()
    {
        TileDataRequirement.SkipIfMissing(); var fleet = Fleet(); var stall = new HavenMarketStall { Trade = HavenMarketTrade.Maritime };
        HavenMarketStall.Registry.Add(stall); var fish = new Fish(5); var personal = new Fish(7); fleet.Captain.Backpack.DropItem(personal);
        try
        {
            Assert.True(Fishing.System.Give(fleet.Captain, fish, false)); Assert.Equal(1, fleet.Catches);
            Assert.True(fish.IsChildOf(fleet.Boat.Hold)); Assert.Contains(fish, fleet.Cargo);
            Assert.True(fleet.Captain.Backpack.GetAmount(typeof(Fish)) >= 7);
            fleet.Unload(); Assert.Contains(fish, stall.Stock); Assert.Same(stall, fish.Parent); Assert.Empty(fleet.Cargo);
            fleet.Unload(); Assert.Single(stall.Stock); Assert.Equal(1, fleet.Sold);
        }
        finally { Clean(fleet); stall.Delete(); fish.Delete(); }
    }
    [SkippableFact]
    public void SosUsesNativeChestRollAndNeverConsumesMapUntilSalvaged()
    {
        TileDataRequirement.SkipIfMissing(); var fleet = Fleet(); var sos = new SOS(Map.Trammel, 4) { TargetLocation = fleet.Captain.Location };
        fleet.Captain.Backpack.DropItem(sos); fleet.Cargo.Add(sos);
        try
        {
            Item chest = null;
            for (var i = 0; i < 100 && !sos.Deleted; i++)
            {
                var loot = Fishing.System.Construct(typeof(Fish), fleet.Captain);
                if (loot is LockableContainer) { chest = loot; } else { loot.Delete(); }
            }
            Assert.True(sos.Deleted); var container = Assert.IsAssignableFrom<LockableContainer>(chest);
            Assert.NotNull(container.FindItemByType<FabledFishingNet>());
            Assert.True(Fishing.System.Give(fleet.Captain, chest, true)); Assert.Equal(1, fleet.Wrecks);
            Assert.True(chest.IsChildOf(fleet.Boat.Hold));
        }
        finally { Clean(fleet); sos.Delete(); }
    }
    [SkippableFact]
    public void PassengerPausesBoatAndRestartDoesNotDuplicateCargo()
    {
        TileDataRequirement.SkipIfMissing(); var fleet = Fleet(); var visitor = new PlayerMobile { Player = true, Body = 400 };
        var copy = new HavenFishingFleet(World.NewItem);
        try
        {
            visitor.MoveToWorld(fleet.Captain.Location, fleet.Boat.Map); fleet.Work = HavenSeaWork.Sailing;
            var before = fleet.Boat.Location; fleet.Tick(Core.Now); Assert.Equal(before, fleet.Boat.Location); Assert.Contains("passenger", fleet.Status);
            var fish = new Fish(2); fleet.Stow(fish); fleet.Catches = 7;
            var writer = new BufferWriter(true); fleet.Serialize(writer);
            copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.Equal(7, copy.Catches); Assert.Same(fish, Assert.Single(copy.Cargo)); Assert.Same(fleet.Boat, copy.Boat);
            copy.Sailors.Clear(); copy.Cargo.Clear(); copy.Boat = null;
        }
        finally { copy.Delete(); visitor.Delete(); Clean(fleet); }
    }
    [SkippableFact]
    public void SeaLifeBelowHullDoesNotPauseVoyageAndBlockedCourseIsRecharted()
    {
        TileDataRequirement.SkipIfMissing(); var fleet = Fleet(); var serpent = new SeaSerpent();
        try
        {
            serpent.MoveToWorld(new Point3D(fleet.Boat.X, fleet.Boat.Y, -5), fleet.Boat.Map);
            Assert.False(fleet.Passengers());
            fleet.Work = HavenSeaWork.Sailing;
            // A stale course into dry land must be abandoned after bounded retries.
            fleet.Course.Add(new Point3D(4196,2868,0));
            for (var i = 0; i < 3; i++) { fleet.Navigate(fleet.Goal,4); }
            Assert.Empty(fleet.Course); Assert.Contains("Recharting", fleet.Status);
            Assert.Equal(fleet.Home,fleet.Boat.Location);
        }
        finally { serpent.Delete(); Clean(fleet); }
    }
    [SkippableFact]
    public void UnlearnedMasteriesAreHiddenAndLearnedVolumesRetainTheirSkills()
    {
        TileDataRequirement.SkipIfMissing(); var p = new PlayerMobile(); var book = new BookOfMasteries(); p.AddItem(new Backpack()); p.Backpack.DropItem(book);
        try
        {
            Assert.Empty(MasterySelectionGump.Learned(p)); MasteryProgress.Get(p).Learn(SkillName.Parry, 3);
            Assert.Equal(new[] { SkillName.Parry }, MasterySelectionGump.Learned(p));
            p.Skills.Parry.Base = 100; Assert.True(BookOfMasteries.Select(p, SkillName.Parry));
            Assert.Equal(SkillName.Parry, MasteryProgress.Current(p)); _ = new MasterySelectionGump(p, book);
        }
        finally { p.Delete(); }
    }
    [SkippableFact]
    public void CompanionLedgerStoresNestedDeedsOnceAndLeavesUnsupportedItems()
    {
        TileDataRequirement.SkipIfMissing(); var p = new PlayerMobile(); p.AddItem(new Backpack()); var companion = new HavenCompanion { BoundOwner = p };
        var algorithm = AccountSecurity.CurrentAlgorithm;
        Account account;
        try { AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.SHA2; account = new Account("fleet-test-" + Guid.NewGuid(), "test-only-password"); }
        finally { AccountSecurity.CurrentAlgorithm = algorithm; }
        p.Account = account; account.SetTag($"HavenCompanion:{p.Serial}", companion.Serial.ToString());
        var ledger = new HavenResourceLedger(); var bag = new Bag(); companion.Backpack.DropItem(ledger); companion.Backpack.DropItem(bag);
        var first = new CommodityDeed(); first.SetCommodity(new IronIngot(321)); bag.DropItem(first);
        var second = new CommodityDeed(); second.SetCommodity(new IronIngot(123)); companion.Backpack.DropItem(second);
        var empty = new CommodityDeed(); bag.DropItem(empty);
        try
        {
            Assert.Equal(2, HavenResourceLedger.StoreCompanionPack(p)); Assert.True(first.Deleted); Assert.True(second.Deleted); Assert.False(empty.Deleted);
            Assert.Equal(444, ledger.Balances.Sum()); Assert.Equal(0, HavenResourceLedger.StoreCompanionPack(p)); Assert.Equal(444, ledger.Balances.Sum());
        }
        finally { companion.Delete(); p.Delete(); account.Delete(); }
    }
    [SkippableFact]
    public void TwoFleetBoatsInstallOnceAndEarnedNetsUseNativeWaterValidation()
    {
        TileDataRequirement.SkipIfMissing(); var island = new HavenChelonia(); HavenChelonia.Registry.Add(island);
        try
        {
            HavenFishingFleet.Ensure(); Assert.Equal(2, HavenFishingFleet.Registry.Count);
            var original = HavenFishingFleet.Registry.Select(f => f.Boat.Serial).OrderBy(s => s.Value).ToArray();
            HavenFishingFleet.Ensure(); Assert.Equal(original, HavenFishingFleet.Registry.Select(f => f.Boat.Serial).OrderBy(s => s.Value).ToArray());
            var fleet = HavenFishingFleet.Registry.First(); var white = new FabledFishingNet(); fleet.Stow(white);
            Assert.False(fleet.TryNet(Core.Now + TimeSpan.FromMinutes(4))); Assert.False(white.Deleted);
            var net = new SpecialFishingNet(); fleet.Stow(net);
            Assert.True(fleet.TryNet(Core.Now + TimeSpan.FromMinutes(4))); Assert.True(net.InUse); Assert.Equal(1, fleet.Nets);
            net.Delete();
        }
        finally { foreach (var fleet in HavenFishingFleet.Registry.ToArray()) { Clean(fleet); } HavenChelonia.Registry.Remove(island); island.Delete(); }
    }
}
