using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsResources
{
    public HavenWorldTestsResources() => _ = new HavenWorldTests();
    public static int TotalDeededAmount(Container pack)
    {
        var total = 0;
        foreach (var deed in pack.FindItemsByType<CommodityDeed>()) { total += deed.Commodity?.Amount ?? 0; }
        return total;
    }
    private static CommodityDeed Filled(Item resource)
    { var deed = new CommodityDeed(); Assert.True(deed.SetCommodity(resource), resource.GetType().Name); return deed; }
    [SkippableFact]
    public void EveryCatalogResourceCombinesWithdrawsAndRedeemsAtItsOriginalTier()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true }; owner.AddItem(new Backpack());
        var book = new HavenResourceLedger(); owner.Backpack.DropItem(book);
        try
        {
            for (var index = 0; index < HavenResourceCatalog.Entries.Length; index++)
            {
                var entry = HavenResourceCatalog.Entries[index];
                var first = Filled(entry.Create(100)); var second = Filled(entry.Create(70));
                owner.Backpack.DropItem(first); owner.Backpack.DropItem(second);
                Assert.Equal(index, HavenResourceCatalog.Index(first.Commodity));
                Assert.True(book.Absorb(owner, first)); Assert.True(book.Absorb(owner, second));
                Assert.False(book.Absorb(owner, first)); Assert.Equal(170, book.Balance(index));
                Assert.True(book.Withdraw(owner, index, 125)); Assert.Equal(45, book.Balance(index));
                var deed = owner.Backpack.FindItemByType<CommodityDeed>();
                var material = deed.Commodity;
                Assert.Equal(index, HavenResourceCatalog.Index(material)); Assert.Equal(125, material.Amount);
                owner.BankBox.DropItem(deed); deed.OnDoubleClick(owner);
                Assert.True(deed.Deleted); Assert.False(material.Deleted); Assert.True(material.IsChildOf(owner.BankBox));
                material.Delete();
            }
            var writer = new BufferWriter(true); book.Serialize(writer);
            var copy = new HavenResourceLedger(World.NewItem);
            try
            {
                copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
                for (var index = 0; index < HavenResourceCatalog.Entries.Length; index++) { Assert.Equal(45, copy.Balance(index)); }
            }
            finally { copy.Delete(); }
        }
        finally { owner.Delete(); }
    }
    [SkippableFact]
    public void LedgerRejectsOtherOwnersOverflowInvalidAmountsAndFullPacksWithoutLoss()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true }; owner.AddItem(new Backpack());
        var stranger = new PlayerMobile { Player = true }; stranger.AddItem(new Backpack());
        var book = new HavenResourceLedger(); owner.Backpack.DropItem(book);
        var deed = Filled(new IronIngot(100)); owner.Backpack.DropItem(deed);
        try
        {
            Assert.False(book.Absorb(stranger, deed)); Assert.False(deed.Deleted);
            book.Balances.Add(long.MaxValue);
            Assert.False(book.Absorb(owner, deed)); Assert.Equal(100, deed.Commodity.Amount);
            book.Balances[0] = 0; Assert.True(book.Absorb(owner, deed));
            Assert.False(book.Withdraw(owner, 0, 0)); Assert.False(book.Withdraw(owner, 0, -1));
            Assert.False(book.Withdraw(owner, 0, 60001)); Assert.False(book.Withdraw(owner, 0, 101));
            Assert.False(book.Withdraw(stranger, 0, 1));
            owner.Backpack.MaxItems = owner.Backpack.TotalItems;
            Assert.False(book.Withdraw(owner, 0, 50)); Assert.Equal(100, book.Balance(0));
            Assert.Null(owner.Backpack.FindItemByType<CommodityDeed>());
        }
        finally { owner.Delete(); stranger.Delete(); }
    }
    [Theory]
    [InlineData(HavenExpeditionKind.Ore, 64, 0.0, 0)]
    [InlineData(HavenExpeditionKind.Ore, 85, 0.0, 5)]
    [InlineData(HavenExpeditionKind.Ore, 99, 0.0, 8)]
    [InlineData(HavenExpeditionKind.Wood, 99, 0.0, 12)]
    [InlineData(HavenExpeditionKind.Wood, 100, 0.0, 13)]
    [InlineData(HavenExpeditionKind.Wood, 100, 0.3, 14)]
    [InlineData(HavenExpeditionKind.Wood, 100, 0.59, 15)]
    [InlineData(HavenExpeditionKind.Leather, 100, 0.0, 26)]
    public void GatheringRespectsSkillThresholds(HavenExpeditionKind kind, double skill, double roll, int expected)
        => Assert.Equal(expected, HavenMissionResources.SelectIndex(kind, skill, roll));

    [SkippableFact]
    public void MissionsDeliverMixedMaterialsDirectlyWithoutRewardBags()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true }; owner.AddItem(new Backpack());
        var companion = new HavenCompanion { BoundOwner = owner };
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); companion.MoveToWorld(owner.Location, owner.Map);
            companion.Skills.Mining.Base = 100;
            Assert.True(HavenCompanionExpedition.Start(companion, owner, HavenExpeditionKind.Ore));
            var trip = companion.Expedition;
            Assert.True(trip.Return(owner, trip.Due));
            Assert.Null(companion.Backpack.FindItemByType<Bag>());
            Assert.Equal(100, TotalDeededAmount(companion.Backpack));
            var tiers = 0;
            foreach (var deed in companion.Backpack.FindItemsByType<CommodityDeed>())
            { Assert.Equal(50, deed.Commodity.Amount); if (HavenResourceCatalog.Index(deed.Commodity) > 0) { tiers++; } }
            Assert.Equal(1, tiers);
            Assert.False(trip.Return(owner, trip.Due)); Assert.Equal(100, TotalDeededAmount(companion.Backpack));
            var sold = new ArcaneSupplyStone.Menu().CreateItem(20);
            try { Assert.IsType<HavenResourceLedger>(sold); } finally { sold.Delete(); }
        }
        finally { companion.Delete(); owner.Delete(); }
    }
}
