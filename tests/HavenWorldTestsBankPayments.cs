using System;
using System.Reflection;
using Server;
using Server.Accounting;
using Server.Accounting.Security;
using Server.Guilds;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.Tests.Network;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsBankPayments
{
    public HavenWorldTestsBankPayments()
    {
        _ = new HavenWorldTests();
    }

    [Theory]
    [InlineData(25000, 0, 0, 25000, true, 0, 0, 0)]
    [InlineData(10000, 4000, 15000, 25000, true, 0, 0, 4000)]
    [InlineData(10000, 4000, 10000, 25000, false, 10000, 4000, 10000)]
    [InlineData(0, 4000, 25000, 25000, true, 0, 0, 4000)]
    public void BankFeesCombineWalletGoldAndChecksWithoutPartialFailure(
        int walletGold, int bankGold, int checkGold, int cost, bool paid,
        int expectedWallet, int expectedBank, int expectedCheck)
    {
        var player = NewPlayer();
        var wallet = new AdventurersWallet { Balance = walletGold };
        player.Backpack.DropItem(wallet);
        if (bankGold > 0)
        {
            player.BankBox.DropItem(new Gold(bankGold));
        }
        if (checkGold > 0)
        {
            player.BankBox.DropItem(new BankCheck(checkGold));
        }
        try
        {
            Assert.Equal(walletGold + bankGold + checkGold, Banker.GetBalance(player));
            Assert.Equal(paid, Banker.Withdraw(player, cost));
            Assert.Equal(expectedWallet, wallet.Balance);
            Assert.Equal(expectedBank, player.BankBox.GetAmount(typeof(Gold)));
            Assert.Equal(expectedCheck, player.BankBox.FindItemByType<BankCheck>()?.Worth ?? 0);
            Assert.Equal(expectedWallet + expectedBank + expectedCheck, Banker.GetBalance(player));
        }
        finally
        {
            player.Delete();
        }
    }

    [Fact]
    public void RegistrationFeeUsesCarriedWalletWithoutRequiringBankBox()
    {
        var player = NewPlayer();
        var wallet = new AdventurersWallet { Balance = Guild.RegistrationFee + 50 };
        player.Backpack.DropItem(wallet);
        try
        {
            Assert.Null(player.FindBankNoCreate());
            Assert.True(Banker.Withdraw(player, Guild.RegistrationFee));
            Assert.Equal(50, wallet.Balance);
            Assert.Null(player.FindBankNoCreate());
        }
        finally
        {
            player.Delete();
        }
    }

    [Fact]
    public void NestedCarriedWalletsCombineButBankedAndForeignWalletsStayUntouched()
    {
        var player = NewPlayer();
        var other = NewPlayer();
        var bag = new Bag();
        var first = new AdventurersWallet { Balance = 80 };
        var second = new AdventurersWallet { Balance = 70 };
        var banked = new AdventurersWallet { Balance = 1000 };
        var foreign = new AdventurersWallet { Balance = 1000 };
        player.Backpack.DropItem(first);
        player.Backpack.DropItem(bag);
        bag.DropItem(second);
        player.BankBox.DropItem(banked);
        other.Backpack.DropItem(foreign);
        try
        {
            Assert.Equal(150, Banker.GetBalance(player));
            Assert.Equal(150, Banker.GetBalance(player, out _, out _));
            Assert.False(Banker.Withdraw(player, 151));
            Assert.Equal(80, first.Balance);
            Assert.Equal(70, second.Balance);
            Assert.True(Banker.Withdraw(player, 150));
            Assert.Equal(0, first.Balance + second.Balance);
            Assert.Equal(1000, banked.Balance);
            Assert.Equal(1000, foreign.Balance);
        }
        finally
        {
            player.Delete();
            other.Delete();
        }
    }

    [Fact]
    public void MerchantPaymentCountsWalletOnceAndKeepsWalletPackBankPriority()
    {
        var player = NewPlayer();
        var wallet = new AdventurersWallet { Balance = 100 };
        player.Backpack.DropItem(wallet);
        player.Backpack.DropItem(new Gold(40));
        player.BankBox.DropItem(new Gold(60));
        try
        {
            Assert.False(HavenEconomy.CanPay(player, 201));
            Assert.False(HavenEconomy.TryPay(player, 201));
            Assert.Equal(100, wallet.Balance);
            Assert.Equal(40, player.Backpack.GetAmount(typeof(Gold)));
            Assert.Equal(60, player.BankBox.GetAmount(typeof(Gold)));
            Assert.True(HavenEconomy.CanPay(player, 170));
            Assert.True(HavenEconomy.TryPay(player, 170));
            Assert.Equal(0, wallet.Balance);
            Assert.Equal(0, player.Backpack.GetAmount(typeof(Gold)));
            Assert.Equal(30, player.BankBox.GetAmount(typeof(Gold)));
        }
        finally
        {
            player.Delete();
        }
    }

    [Fact]
    public void AccountWalletAndPhysicalBankCanFundOneFeeAndFailureChangesNone()
    {
        var enabled = AccountGold.Enabled;
        var converting = AccountGold.ConvertOnBank;
        var account = NewAccount();
        var player = NewPlayer();
        player.Account = account;
        var wallet = new AdventurersWallet { Balance = 100 };
        try
        {
            SetAccountOption(nameof(AccountGold.Enabled), true);
            SetAccountOption(nameof(AccountGold.ConvertOnBank), false);
            account.DepositGold(80);
            player.Backpack.DropItem(wallet);
            player.Backpack.DropItem(new Gold(20));
            player.BankBox.DropItem(new Gold(40));
            player.BankBox.DropItem(new BankCheck(60));
            Assert.Equal(280, Banker.GetBalance(player));
            Assert.False(HavenEconomy.TryPay(player, 301));
            Assert.Equal(100, wallet.Balance);
            Assert.Equal(80, account.TotalGold);
            Assert.Equal(20, player.Backpack.GetAmount(typeof(Gold)));
            Assert.Equal(40, player.BankBox.GetAmount(typeof(Gold)));
            Assert.Equal(60, player.BankBox.FindItemByType<BankCheck>().Worth);
            Assert.True(HavenEconomy.TryPay(player, 250));
            Assert.Equal(0, wallet.Balance);
            Assert.Equal(0, account.TotalGold);
            Assert.Equal(0, player.Backpack.GetAmount(typeof(Gold)));
            Assert.Equal(0, player.BankBox.GetAmount(typeof(Gold)));
            Assert.Equal(50, player.BankBox.FindItemByType<BankCheck>().Worth);
        }
        finally
        {
            SetAccountOption(nameof(AccountGold.Enabled), enabled);
            SetAccountOption(nameof(AccountGold.ConvertOnBank), converting);
            player.Delete();
            account.Delete();
        }
    }

    [Fact]
    public void PlatinumMakesExactChangeAndLargeBalancesDoNotOverflow()
    {
        var enabled = AccountGold.Enabled;
        var account = NewAccount();
        var player = NewPlayer();
        player.Account = account;
        var wallet = new AdventurersWallet { Balance = 25 };
        player.Backpack.DropItem(wallet);
        try
        {
            SetAccountOption(nameof(AccountGold.Enabled), true);
            account.DepositGold(75);
            account.DepositPlat(3);
            Assert.Equal(int.MaxValue, Banker.GetBalance(player));
            var before = HavenBankPayments.AccountBalance(account) + wallet.Balance;
            Assert.True(Banker.Withdraw(player, 200));
            Assert.Equal(0, wallet.Balance);
            Assert.Equal(2, account.TotalPlat);
            Assert.Equal(AccountGold.CurrencyThreshold - 100, account.TotalGold);
            Assert.Equal(before - 200, HavenBankPayments.AccountBalance(account));
            wallet.Balance = long.MaxValue;
            Assert.Equal(int.MaxValue, Banker.GetBalance(player));
            Assert.False(Banker.Withdraw(player, -100));
            Assert.Equal(long.MaxValue, wallet.Balance);
        }
        finally
        {
            SetAccountOption(nameof(AccountGold.Enabled), enabled);
            player.Delete();
            account.Delete();
        }
    }

    [SkippableFact]
    public void NativeVendorPurchaseWorksWhenWalletPlusLooseGoldExceedsIntLimit()
    {
        TileDataRequirement.SkipIfMissing();
        var buyer = NewPlayer();
        buyer.Body = 0x190;
        var account = NewAccount();
        buyer.Account = account;
        var seller = NewPlayer();
        var vendor = new PlayerVendor(seller, null);
        var wallet = new AdventurersWallet { Balance = int.MaxValue };
        var item = new Longsword();
        buyer.Backpack.DropItem(wallet);
        buyer.Backpack.DropItem(new Gold(10));
        vendor.Backpack.DropItem(item);
        var listing = vendor.GetVendorItem(item);
        listing.Price = 100;
        var originalVendorGold = vendor.HoldGold;
        try
        {
            using var state = PacketTestUtilities.CreateTestNetState();
            state.Account = account;
            state.Mobile = buyer;
            buyer.NetState = state;
            buyer.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            vendor.MoveToWorld(buyer.Location, buyer.Map);
            PlayerVendorBuyGump.DisplayTo(buyer, vendor, listing);
            var gump = buyer.FindGump<PlayerVendorBuyGump>();
            Assert.NotNull(gump);
            gump.OnResponse(state, new RelayInfo(1, [], [], [], []));
            Assert.True(item.IsChildOf(buyer.Backpack));
            Assert.Equal((long)int.MaxValue - 90, wallet.Balance);
            Assert.Equal(0, buyer.Backpack.GetAmount(typeof(Gold)));
            Assert.Equal(originalVendorGold + 100, vendor.HoldGold);
            // The old purchase cannot be replayed after the actual item moves.
            gump.OnResponse(state, new RelayInfo(1, [], [], [], []));
            Assert.Equal((long)int.MaxValue - 90, wallet.Balance);
            Assert.Equal(originalVendorGold + 100, vendor.HoldGold);
        }
        finally
        {
            vendor.Delete();
            buyer.Delete();
            account.Delete();
            seller.Delete();
        }
    }

    private static PlayerMobile NewPlayer()
    {
        var player = new PlayerMobile { Player = true };
        player.AddItem(new Backpack());
        return player;
    }

    private static Account NewAccount()
    {
        var algorithm = AccountSecurity.CurrentAlgorithm;
        try
        {
            AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.SHA2;
            return new Account("wallet-bank-test-" + Guid.NewGuid(), "test-only-password");
        }
        finally
        {
            AccountSecurity.CurrentAlgorithm = algorithm;
        }
    }

    private static void SetAccountOption(string name, bool value) =>
        typeof(AccountGold).GetProperty(name, BindingFlags.Static | BindingFlags.Public)!.SetValue(null, value);
}
