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
public class HavenWorldTestsCompanionLedger
{
    public HavenWorldTestsCompanionLedger() { _ = new HavenWorldTests(); }

    [SkippableTheory]
    [InlineData(HavenExpeditionKind.Ore, false)]
    [InlineData(HavenExpeditionKind.Wood, true)]
    [InlineData(HavenExpeditionKind.Leather, false)]
    [InlineData(HavenExpeditionKind.Reagents, true)]
    [InlineData(HavenExpeditionKind.MalasReagents, false)]
    [InlineData(HavenExpeditionKind.DoomBones, true)]
    [InlineData(HavenExpeditionKind.AbyssEssences, false)]
    [InlineData(HavenExpeditionKind.AbyssIngredients, true)]
    public void MissionResourcesDepositOnceAndRetainExactReceipts(HavenExpeditionKind kind, bool offline)
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 400 }; owner.AddItem(new Backpack());
        var companion = new HavenCompanion { BoundOwner = owner };
        var nested = new Bag(); companion.Backpack.DropItem(nested);
        var book = new HavenResourceLedger(); nested.DropItem(book);
        var copy = new HavenResourceLedger(World.NewItem);
        owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); companion.MoveToWorld(owner.Location, owner.Map);
        companion.Skills.Tactics.Base = 120; companion.Skills.MagicResist.Base = 120;
        try
        {
            if (offline)
            {
                HavenMissionRoute.Get(companion).Focus = (int)kind;
                var job = HavenCompanionGearAssignment.Begin(companion, Core.Now);
                var due = job.NextReward;
                job.Tick(due, false); job.Tick(due, false);
                Assert.Equal(1, job.Completed);
            }
            else
            {
                Assert.True(HavenCompanionExpedition.Start(companion, owner, kind));
                var trip = companion.Expedition;
                Assert.True(trip.Return(owner, trip.Due)); Assert.False(trip.Return(owner, trip.Due));
            }
            var report = Assert.Single(HavenMissionJournal.Find(companion).Reports);
            Assert.Equal(1, report.Runs); Assert.NotEmpty(report.Loot);
            Assert.All(report.Loot, receipt => Assert.Equal("Companion Resource Ledger", receipt.Destination));
            Assert.Null(companion.Backpack.FindItemByType<CommodityDeed>());
            Assert.Equal(report.Loot.Sum(x => x.Amount), book.Balances.Sum());
            var writer = new BufferWriter(true); book.Serialize(writer);
            copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.Equal(book.Balances, copy.Balances);
            var index = Enumerable.Range(0, book.Balances.Count).First(i => book.Balance(i) > 0);
            var total = book.Balance(index);
            Assert.True(book.Withdraw(owner, index, 1)); Assert.Equal(total - 1, book.Balance(index));
            Assert.Equal(1, owner.Backpack.FindItemByType<CommodityDeed>().Commodity.Amount);
        }
        finally { copy.Delete(); companion.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void CompanionLedgerRequiresNearbyOwnerAndUsesItsCarriersPack()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile(); owner.AddItem(new Backpack());
        var stranger = new PlayerMobile(); stranger.AddItem(new Backpack());
        var companion = new HavenCompanion { BoundOwner = owner };
        var book = new HavenResourceLedger(); companion.Backpack.DropItem(book);
        var deed = new CommodityDeed(); Assert.True(deed.SetCommodity(new IronIngot(50))); companion.Backpack.DropItem(deed);
        owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); stranger.MoveToWorld(owner.Location, owner.Map);
        companion.MoveToWorld(owner.Location, owner.Map);
        var index = HavenResourceCatalog.Index(deed.Commodity);
        try
        {
            Assert.False(book.CanUse(stranger)); Assert.False(book.Absorb(stranger, deed));
            Assert.True(book.CanUse(owner)); Assert.Equal(1, book.AbsorbPack(owner)); Assert.True(deed.Deleted);
            Assert.Equal(50, book.Balance(index)); Assert.False(book.Withdraw(stranger, index, 1));
            companion.Internalize(); Assert.False(book.CanUse(owner)); Assert.False(book.Withdraw(owner, index, 1));
            companion.MoveToWorld(new Point3D(owner.X + 30, owner.Y, owner.Z), owner.Map);
            Assert.False(book.CanUse(owner));
            companion.MoveToWorld(owner.Location, owner.Map);
            owner.Backpack.DropItem(new Bag());
            owner.Backpack.MaxItems = owner.Backpack.TotalItems;
            Assert.False(book.Withdraw(owner, index, 1)); Assert.Equal(50, book.Balance(index));
            owner.Backpack.MaxItems = 125;
            Assert.True(book.Withdraw(owner, index, 20)); Assert.Equal(30, book.Balance(index));
            Assert.Equal(20, owner.Backpack.FindItemByType<CommodityDeed>().Commodity.Amount);
        }
        finally { companion.Delete(); owner.Delete(); stranger.Delete(); }
    }

    [Fact]
    public void FullLedgerFallsBackWithoutDeletingOrReportingUnstoredRewards()
    {
        var owner = new PlayerMobile(); var companion = new HavenCompanion { BoundOwner = owner };
        var book = new HavenResourceLedger(); companion.Backpack.DropItem(book);
        var deed = new CommodityDeed(); Assert.True(deed.SetCommodity(new IronIngot(10)));
        var index = HavenResourceCatalog.Index(deed.Commodity);
        while (book.Balances.Count <= index) { book.Balances.Add(0); }
        book.Balances[index] = long.MaxValue;
        var journal = HavenMissionJournal.Begin(companion, Core.Now);
        try
        {
            Assert.False(HavenResourceLedger.StoreMissionReward(companion, deed, journal));
            Assert.False(deed.Deleted); Assert.Equal(10, deed.Commodity.Amount); Assert.Empty(journal.Reports[0].Loot);
            var second = new HavenResourceLedger(); companion.Backpack.DropItem(second);
            Assert.True(HavenResourceLedger.StoreMissionReward(companion, deed, journal));
            Assert.True(deed.Deleted); Assert.Equal(10, second.Balance(index)); Assert.Single(journal.Reports[0].Loot);
            Assert.False(HavenResourceLedger.StoreMissionReward(companion, deed, journal));
            Assert.Equal(10, second.Balance(index));
        }
        finally { deed.Delete(); companion.Delete(); owner.Delete(); }
    }
}
