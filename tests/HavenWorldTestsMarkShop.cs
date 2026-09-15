using System;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsMarkShop
{
    public HavenWorldTestsMarkShop() => _ = new HavenWorldTests();

    [Fact]
    public void EveryOfferConstructsAnIndependentItemAndDeletesItsOwnedContents()
    {
        var offers = HavenMarkRewards.Groups.SelectMany(group => group.Offers).ToArray();
        Assert.Equal(54, offers.Length);
        Assert.Equal(34, offers.Count(offer => offer.LegacyIndex < 0));
        foreach (var offer in offers)
        {
            Item first = null;
            Item second = null;
            try
            {
                first = offer.Create();
                second = offer.Create();
                Assert.NotNull(first);
                Assert.NotNull(second);
                Assert.NotSame(first, second);
                Assert.False(first.Deleted);
                Assert.Null(first.Parent);
                Assert.True(offer.Price > 0);
                Assert.False(string.IsNullOrWhiteSpace(offer.Name));
                Assert.False(string.IsNullOrWhiteSpace(offer.Description));
                var children = first.Items.ToArray();
                var commodity = (first as CommodityDeed)?.Commodity;
                first.Delete();
                Assert.True(first.Deleted);
                Assert.All(children, child => Assert.True(child.Deleted));
                if (commodity != null) { Assert.True(commodity.Deleted); }
                Assert.False(second.Deleted);
            }
            finally
            {
                first?.Delete();
                second?.Delete();
            }
        }
    }

    [SkippableFact]
    public void ResourcePurchasesDeliverExactDeedsThatTheLedgerAbsorbsWithoutLoss()
    {
        TileDataRequirement.SkipIfMissing();
        var player = NewPlayer();
        var wallet = new AdventurersWallet { HavenMarks = 1000 };
        var ledger = new HavenResourceLedger();
        player.Backpack.DropItem(wallet);
        player.Backpack.DropItem(ledger);
        var expected = new (Type Type, int Amount)[]
        {
            (typeof(IronIngot), 5000),
            (typeof(ValoriteIngot), 1000),
            (typeof(OakBoard), 2500),
            (typeof(HeartwoodBoard), 1000),
            (typeof(SpinedLeather), 2500),
            (typeof(BarbedLeather), 1000)
        };
        var group = Group("Resource deeds");
        try
        {
            Assert.Equal(expected.Length, HavenMarkRewards.Groups[group].Offers.Length);
            var spent = 0;
            for (var index = 0; index < expected.Length; index++)
            {
                Assert.True(HavenMarkRewards.Buy(player, group, index));
                spent += HavenMarkRewards.Groups[group].Offers[index].Price;
                var deed = Assert.Single(player.Backpack.Items.OfType<CommodityDeed>());
                var material = deed.Commodity;
                Assert.NotNull(material);
                Assert.Equal(expected[index].Type, material.GetType());
                Assert.Equal(expected[index].Amount, material.Amount);
                var resource = HavenResourceCatalog.Index(material);
                Assert.True(resource >= 0);
                Assert.True(ledger.Absorb(player, deed));
                Assert.True(deed.Deleted);
                Assert.True(material.Deleted);
                Assert.Equal((long)expected[index].Amount, ledger.Balance(resource));
                Assert.Equal(1000L - spent, wallet.HavenMarks);
            }
            Assert.Empty(player.Backpack.Items.OfType<CommodityDeed>());
        }
        finally { player.Delete(); }
    }

    [SkippableFact]
    public void PurchasesSpendWalletBeforeNestedLooseMarksAndAvailableBalanceSaturates()
    {
        TileDataRequirement.SkipIfMissing();
        var player = NewPlayer();
        var wallet = new AdventurersWallet { HavenMarks = 7, Balance = 12345 };
        var bag = new Bag();
        player.Backpack.DropItem(bag);
        bag.DropItem(wallet);
        bag.DropItem(new HavenMark(8));
        var group = Group("Pet supplies");
        var index = Offer(group, "Pet dye");
        try
        {
            Assert.Equal(15L, HavenMarkRewards.AvailableMarks(player));
            Assert.True(HavenMarkRewards.Buy(player, group, index));
            Assert.Equal(0L, wallet.HavenMarks);
            Assert.Equal(5, player.Backpack.GetAmount(typeof(HavenMark)));
            Assert.Single(player.Backpack.Items.OfType<HavenPetDye>());

            wallet.HavenMarks = 20;
            Assert.True(HavenMarkRewards.Buy(player, group, index));
            Assert.Equal(10L, wallet.HavenMarks);
            Assert.Equal(5, player.Backpack.GetAmount(typeof(HavenMark)));
            Assert.Equal(2, player.Backpack.Items.OfType<HavenPetDye>().Count());
            Assert.Equal(12345L, wallet.Balance);

            wallet.HavenMarks = long.MaxValue;
            Assert.Equal(long.MaxValue, HavenMarkRewards.AvailableMarks(player));
            wallet.HavenMarks = -1;
            Assert.Equal(5L, HavenMarkRewards.AvailableMarks(player));
            Assert.Equal(0L, HavenMarkRewards.AvailableMarks(null));
        }
        finally { player.Delete(); }
    }

    [SkippableFact]
    public void InsufficientMarksAndFullBackpacksPreserveAllCurrenciesAndDeliverNothing()
    {
        TileDataRequirement.SkipIfMissing();
        var player = NewPlayer();
        var wallet = new AdventurersWallet { HavenMarks = 6, Balance = 50000 };
        player.Backpack.DropItem(wallet);
        player.Backpack.DropItem(new HavenMark(3));
        player.Backpack.DropItem(new Gold(5000));
        var group = Group("Pet supplies");
        var index = Offer(group, "Pet dye");
        try
        {
            Assert.False(HavenMarkRewards.Buy(player, group, index));
            Assert.Equal(6L, wallet.HavenMarks);
            Assert.Equal(3, player.Backpack.GetAmount(typeof(HavenMark)));
            Assert.Empty(player.Backpack.Items.OfType<HavenPetDye>());

            wallet.HavenMarks = 10;
            player.Backpack.MaxItems = player.Backpack.TotalItems;
            Assert.False(HavenMarkRewards.Buy(player, group, index));
            Assert.Equal(10L, wallet.HavenMarks);
            Assert.Equal(3, player.Backpack.GetAmount(typeof(HavenMark)));
            Assert.Equal(50000L, wallet.Balance);
            Assert.Equal(5000, player.Backpack.GetAmount(typeof(Gold)));
            Assert.Empty(player.Backpack.Items.OfType<HavenPetDye>());
        }
        finally { player.Delete(); }
    }

    [SkippableFact]
    public void EveryNewOfferRequiresMarksEvenWhenBothGoldSourcesCanAffordIt()
    {
        TileDataRequirement.SkipIfMissing();
        var player = NewPlayer();
        var wallet = new AdventurersWallet { Balance = 50000 };
        player.Backpack.DropItem(wallet);
        player.Backpack.DropItem(new Gold(50000));
        try
        {
            for (var group = 0; group < HavenMarkRewards.Groups.Length; group++)
            {
                for (var index = 0; index < HavenMarkRewards.Groups[group].Offers.Length; index++)
                {
                    if (HavenMarkRewards.Groups[group].Offers[index].LegacyIndex >= 0) { continue; }
                    Assert.False(HavenMarkRewards.Buy(player, group, index));
                    Assert.Equal(0L, wallet.HavenMarks);
                    Assert.Equal(50000L, wallet.Balance);
                    Assert.Equal(50000, player.Backpack.GetAmount(typeof(Gold)));
                    Assert.Equal(2, player.Backpack.Items.Count);
                }
            }
        }
        finally { player.Delete(); }
    }

    [SkippableFact]
    public void LegacyBraceletsRetainGoldFallbackAndPreferMarksWhenEnoughAreAvailable()
    {
        TileDataRequirement.SkipIfMissing();
        var player = NewPlayer();
        var wallet = new AdventurersWallet { HavenMarks = 5 };
        player.Backpack.DropItem(wallet);
        player.Backpack.DropItem(new Gold(50000));
        var group = Group("Bracelets");
        try
        {
            Assert.True(HavenMarkRewards.Buy(player, group, 0));
            Assert.Equal(5L, wallet.HavenMarks);
            Assert.Equal(25000, player.Backpack.GetAmount(typeof(Gold)));
            Assert.Single(player.Backpack.Items.OfType<BraceletOfTheVanguard>());

            wallet.HavenMarks = 15;
            Assert.True(HavenMarkRewards.Buy(player, group, 1));
            Assert.Equal(0L, wallet.HavenMarks);
            Assert.Equal(25000, player.Backpack.GetAmount(typeof(Gold)));
            Assert.Single(player.Backpack.Items.OfType<BraceletOfArcaneFocus>());
        }
        finally { player.Delete(); }
    }

    [SkippableFact]
    public void InvalidSelectionsNeverChargeOrCreateAReward()
    {
        TileDataRequirement.SkipIfMissing();
        var player = NewPlayer();
        var wallet = new AdventurersWallet { HavenMarks = 1000, Balance = 50000 };
        player.Backpack.DropItem(wallet);
        try
        {
            Assert.False(HavenMarkRewards.Buy(player, -1, 0));
            Assert.False(HavenMarkRewards.Buy(player, HavenMarkRewards.Groups.Length, 0));
            Assert.False(HavenMarkRewards.Buy(player, 0, -1));
            Assert.False(HavenMarkRewards.Buy(player, 0, HavenMarkRewards.Groups[0].Offers.Length));
            Assert.False(HavenMarkRewards.Buy(null, 0, 0));
            Assert.Equal(1000L, wallet.HavenMarks);
            Assert.Equal(50000L, wallet.Balance);
            Assert.Same(wallet, Assert.Single(player.Backpack.Items));
        }
        finally { player.Delete(); }
    }

    [SkippableFact]
    public void PreviewRechecksWalletOwnershipCurrentFundsAndStoneRangeBeforePurchasing()
    {
        TileDataRequirement.SkipIfMissing();
        var player = NewPlayer();
        var other = NewPlayer();
        var wallet = new AdventurersWallet { HavenMarks = 100 };
        var stone = new SpecialRewardStone();
        player.Backpack.DropItem(wallet);
        player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
        other.MoveToWorld(player.Location, player.Map);
        stone.MoveToWorld(player.Location, player.Map);
        var group = Group("Pet supplies");
        var index = Offer(group, "Pet dye");
        var menu = new HavenMarkRewards.Menu(group);
        try
        {
            using var state = PacketTestUtilities.CreateTestNetState();
            state.Mobile = player;
            player.NetState = state;
            var transferred = new HavenMarkRewardPreview(wallet, menu, index, 0, player);
            other.Backpack.DropItem(wallet);
            transferred.OnResponse(state, new RelayInfo(1, [], [], [], []));
            Assert.Equal(100L, wallet.HavenMarks);
            Assert.Empty(player.Backpack.Items.OfType<HavenPetDye>());

            player.Backpack.DropItem(wallet);
            var staleFunds = new HavenMarkRewardPreview(wallet, menu, index, 0, player);
            wallet.HavenMarks = 0;
            staleFunds.OnResponse(state, new RelayInfo(1, [], [], [], []));
            Assert.Equal(0L, wallet.HavenMarks);
            Assert.Empty(player.Backpack.Items.OfType<HavenPetDye>());

            wallet.HavenMarks = 100;
            var remote = new HavenMarkRewardPreview(stone, menu, index, 0, player);
            player.MoveToWorld(new Point3D(stone.X + 20, stone.Y, stone.Z), stone.Map);
            remote.OnResponse(state, new RelayInfo(1, [], [], [], []));
            Assert.Equal(100L, wallet.HavenMarks);
            Assert.Empty(player.Backpack.Items.OfType<HavenPetDye>());

            player.MoveToWorld(stone.Location, player.Map);
            stone.MoveToWorld(stone.Location, Map.Felucca);
            remote.OnResponse(state, new RelayInfo(1, [], [], [], []));
            Assert.Equal(100L, wallet.HavenMarks);
            Assert.Empty(player.Backpack.Items.OfType<HavenPetDye>());

            stone.MoveToWorld(player.Location, player.Map);
            var deletedAnchor = new HavenMarkRewardPreview(stone, menu, index, 0, player);
            stone.Delete();
            deletedAnchor.OnResponse(state, new RelayInfo(1, [], [], [], []));
            Assert.Equal(100L, wallet.HavenMarks);
            Assert.Empty(player.Backpack.Items.OfType<HavenPetDye>());

            var valid = new HavenMarkRewardPreview(wallet, menu, index, 0, player);
            valid.OnResponse(state, new RelayInfo(1, [], [], [], []));
            Assert.Equal(90L, wallet.HavenMarks);
            Assert.Single(player.Backpack.Items.OfType<HavenPetDye>());
        }
        finally
        {
            stone.Delete();
            wallet.Delete();
            player.Delete();
            other.Delete();
        }
    }

    private static PlayerMobile NewPlayer()
    {
        var player = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100 };
        player.AddItem(new Backpack());
        player.Hits = player.HitsMax;
        return player;
    }

    private static int Group(string name) =>
        Array.FindIndex(HavenMarkRewards.Groups, group => group.Name == name);

    private static int Offer(int group, string name) =>
        Array.FindIndex(HavenMarkRewards.Groups[group].Offers, offer => offer.Name == name);
}
