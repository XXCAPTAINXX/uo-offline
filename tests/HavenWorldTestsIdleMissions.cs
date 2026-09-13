using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsIdleMissions
{
    public HavenWorldTestsIdleMissions() => _ = new HavenWorldTests();
    [Fact]
    public void GoldConsolidationPreservesTotalsLimitsAndSeparatedBags()
    {
        var pack = new Backpack();
        try
        {
            foreach (var amount in new[] { 40000, 30000, 20000, 5000 }) { pack.DropItem(new Gold(amount)); }
            var colored = new Gold(12) { Hue = 123 }; pack.DropItem(colored);
            var bag = new Bag(); var saved = new Gold(99); bag.DropItem(saved); pack.DropItem(bag);
            var before = pack.TotalGold;
            Assert.Equal(2, HavenCompanionGold.Consolidate(pack));
            Assert.Equal(before, pack.TotalGold);
            Assert.Equal(new[] { 35000, 60000 }, pack.Items.OfType<Gold>().Where(g => g.Hue == 0).Select(g => g.Amount).OrderBy(a => a));
            Assert.Same(pack, colored.Parent); Assert.Equal(12, colored.Amount);
            Assert.Same(bag, saved.Parent); Assert.Equal(99, saved.Amount);
            Assert.Equal(0, HavenCompanionGold.Consolidate(pack));
        }
        finally { pack.Delete(); }
    }
    [SkippableFact]
    public void IdleCyclesReturnOnMovementAndNeverTakeOverManualTrips()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var companion = new HavenCompanion { BoundOwner = owner };
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            companion.MoveToWorld(owner.Location, owner.Map);
            owner.Hits = owner.HitsMax; companion.Hits = companion.HitsMax;
            var record = HavenCompanionIdleMissions.Ensure(companion);
            var now = Core.Now;
            record.Tick(now, true);
            record.Tick(now + TimeSpan.FromMinutes(4), true); Assert.Null(companion.Expedition);
            record.Tick(now + TimeSpan.FromMinutes(5), true);
            Assert.NotNull(record.ActiveTrip); Assert.Equal(HavenExpeditionKind.Grind, record.ActiveTrip.Kind);
            var trip = record.ActiveTrip;
            companion.Backpack.DropItem(new Gold(1000));
            companion.Backpack.DropItem(new Gold(2000));
            Assert.True(trip.Return(owner, trip.Due, automatic: true));
            var gold = Assert.Single(companion.Backpack.Items.OfType<Gold>());
            var earned = HavenMissionJournal.Find(companion).Reports[0].Loot.Where(r => r.Icon == 0xEED).Sum(r => r.Amount);
            Assert.True(earned > 0);
            Assert.Equal(3000L + earned, (long)gold.Amount);
            companion.Hits = companion.HitsMax; // Recover after mission stat growth before leaving again.
            record.Tick(now + TimeSpan.FromMinutes(10), true);
            Assert.Equal(HavenExpeditionKind.Ore, record.ActiveTrip.Kind);
            owner.LastMoveTime++;
            record.Tick(now + TimeSpan.FromMinutes(10.1), true);
            Assert.Null(companion.Expedition); Assert.Equal(owner.Map, companion.Map);
            Assert.Null(record.ActiveTrip);
            Assert.True(HavenCompanionExpedition.Start(companion, owner, HavenExpeditionKind.Wood));
            var manual = companion.Expedition;
            owner.LastMoveTime++;
            record.Tick(now + TimeSpan.FromMinutes(11), true);
            Assert.Same(manual, companion.Expedition);
            manual.Return(owner, Core.Now);
            record.Enabled = false; record.Tick(now + TimeSpan.FromMinutes(20), true);
            Assert.Null(companion.Expedition);
        }
        finally { companion.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void HighestPetNeedsBothSkillsAndGatheringIsDeeded()
    {
        TileDataRequirement.SkipIfMissing();
        var companion = new HavenCompanion();
        try
        {
            companion.Skills.AnimalTaming.Base = 120; companion.Skills.AnimalLore.Base = 70;
            Assert.Equal(HavenExpeditionKind.TameFrostmane, HavenCompanionIdleMissions.BestTaming(companion));
            companion.Skills.AnimalLore.Base = 110;
            Assert.Equal(HavenExpeditionKind.TameStormscale, HavenCompanionIdleMissions.BestTaming(companion));
            foreach (var kind in new[] { HavenExpeditionKind.Ore, HavenExpeditionKind.Wood, HavenExpeditionKind.Leather, HavenExpeditionKind.Reagents })
            {
                var bag = HavenCompanionExpedition.CreateLoot(kind, 5);
                try
                {
                    Assert.Equal(kind == HavenExpeditionKind.Reagents ? 8 : 1, bag.Items.Count);
                    foreach (var item in bag.Items)
                    {
                        var deed = Assert.IsType<CommodityDeed>(item);
                        Assert.NotNull(deed.Commodity); Assert.Equal(Map.Internal, deed.Commodity.Map);
                        Assert.Equal(1.0, deed.Weight);
                    }
                }
                finally { bag.Delete(); }
            }
        }
        finally { companion.Delete(); }
    }

    [SkippableFact]
    public void ManualAfkSurvivesActivityAndDisconnectUntilExplicitlyDisabled()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var companion = new HavenCompanion { BoundOwner = owner };
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); companion.MoveToWorld(owner.Location, owner.Map);
            owner.Hits = owner.HitsMax; companion.Hits = companion.HitsMax;
            var record = HavenCompanionIdleMissions.Ensure(companion);
            record.Tick(Core.Now, true); record.Afk = true; record.Tick(Core.Now, true);
            Assert.NotNull(record.ActiveTrip);
            var trip = record.ActiveTrip;
            owner.LastMoveTime++; owner.NextActionTime++; owner.NextSkillTime++;
            owner.Warmode = true; record.Tick(Core.Now, true);
            Assert.True(record.Afk); Assert.Same(trip, companion.Expedition);
            record.Tick(Core.Now, false);
            Assert.True(record.Afk); Assert.Same(trip, companion.Expedition);
            record.Tick(Core.Now, true);
            Assert.True(record.Afk); Assert.Same(trip, companion.Expedition);
            var writer = new BufferWriter(true); record.Serialize(writer);
            var copy = new HavenCompanionIdleMissions(World.NewItem);
            try
            {
                copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
                Assert.True(copy.Afk); Assert.Same(trip, copy.ActiveTrip);
            }
            finally { copy.Delete(); }
            Assert.True(trip.Return(owner, trip.Due, automatic: true));
            companion.Hits = companion.HitsMax;
            var enemy = new Dragon();
            try
            {
                enemy.MoveToWorld(owner.Location, owner.Map); companion.Combatant = enemy;
                record.Tick(trip.Due, true);
                Assert.NotNull(record.ActiveTrip); Assert.NotSame(trip, record.ActiveTrip);
                Assert.Equal(HavenExpeditionKind.Ore, record.ActiveTrip.Kind);
            }
            finally { enemy.Delete(); }
            record.SetAfk(false); record.Tick(Core.Now, true);
            Assert.False(record.Afk); Assert.Null(companion.Expedition);
        }
        finally { companion.Delete(); owner.Delete(); }
    }
}
