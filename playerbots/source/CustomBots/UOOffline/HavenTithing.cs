using System;
using Server.Commands;
namespace Server.UOOffline;
public static class HavenTithing
{
    public const int Maximum = 100000;
    public static void Initialize() => CommandSystem.Register("Tithe", AccessLevel.Player, e =>
    {
        if (!int.TryParse(e.ArgString.Trim(), out var amount))
        { e.Mobile.SendMessage("Use [tithe 1000 to convert wallet gold into Chivalry tithing points, one for one."); return; }
        Tithe(e.Mobile, amount);
    });
    internal static bool Tithe(Mobile from, int requested)
    {
        if (from?.Deleted != false || !from.Alive || requested <= 0) { return false; }
        var wallet = from.Backpack?.FindItemByType<AdventurersWallet>();
        var amount = Math.Min(requested, Math.Max(0, Maximum - from.TithingPoints));
        if (wallet == null || amount <= 0 || !wallet.TrySpend(amount))
        { from.SendMessage("Keep enough gold in your backpack wallet. Tithing points cannot exceed 100,000."); return false; }
        from.TithingPoints += amount;
        from.PlaySound(0x243);
        from.SendMessage($"Tithed {amount:N0} wallet gold. Chivalry tithing points: {from.TithingPoints:N0}.");
        return true;
    }
}
