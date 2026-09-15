using System;
using Server.Accounting;

namespace Server.UOOffline;

// Wallets remain ordinary saved items. Bank-backed fees may spend only wallets
// in this character's backpack; no other character's or companion's funds.
public static class HavenBankPayments
{
    public static int WalletBalance(Mobile from)
    {
        if (from?.Deleted != false || from.Backpack?.Deleted != false)
        {
            return 0;
        }

        long balance = 0;
        foreach (var wallet in from.Backpack.FindItemsByType<AdventurersWallet>())
        {
            if (!wallet.Deleted)
            {
                balance += Math.Clamp(wallet.Balance, 0L, int.MaxValue);
                if (balance >= int.MaxValue)
                {
                    return int.MaxValue;
                }
            }
        }

        return (int)balance;
    }

    internal static void SpendWalletGold(Mobile from, int amount)
    {
        // Banker preflights every source before entering this commit phase.
        // Balance edits do not change the backpack tree while enumerating it.
        foreach (var wallet in from.Backpack.FindItemsByType<AdventurersWallet>())
        {
            var debit = (int)Math.Min(amount, Math.Max(0L, wallet.Balance));
            if (debit > 0)
            {
                wallet.TrySpend(debit);
                amount -= debit;
            }

            if (amount == 0)
            {
                return;
            }
        }
    }

    public static long AccountBalance(IGoldAccount account) => account == null ? 0 :
        Math.Max(0L, account.TotalGold) + Math.Max(0L, account.TotalPlat) * AccountGold.CurrencyThreshold;

    internal static bool WithdrawAccount(IGoldAccount account, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (account == null || AccountBalance(account) < amount)
        {
            return false;
        }

        var loose = Math.Min(amount, Math.Max(0, account.TotalGold));
        if (loose == amount)
        {
            return account.WithdrawGold(amount);
        }

        // This branch's native account API spends gold and platinum separately.
        // Make exact change without changing the saved currency representation.
        var shortfall = amount - loose;
        var platinum = (int)(((long)shortfall + AccountGold.CurrencyThreshold - 1) / AccountGold.CurrencyThreshold);
        var change = (int)((long)platinum * AccountGold.CurrencyThreshold - shortfall);
        if (!account.WithdrawPlat(platinum))
        {
            return false;
        }

        if (loose > 0 && !account.WithdrawGold(loose))
        {
            account.DepositPlat(platinum);
            return false;
        }

        if (change > 0)
        {
            account.DepositGold(change);
        }

        return true;
    }
}
