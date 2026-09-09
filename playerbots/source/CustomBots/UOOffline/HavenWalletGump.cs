using Server.Gumps;
using Server.Network;

namespace Server.UOOffline;

public sealed class HavenWalletGump : Gump
{
    private readonly AdventurersWallet _wallet;
    public HavenWalletGump(AdventurersWallet wallet) : base(40, 40)
    {
        _wallet = wallet;
        AddBackground(0, 0, 460, 330, 5054);
        AddBackground(12, 12, 436, 306, 3000);
        AddHtml(30, 25, 400, 30, "<B>Adventurer's wallet</B>");
        AddLabel(30, 65, 0, $"Balance: {wallet.Balance:N0} gold");
        AddButton(30, 110, 4005, 4007, 1);
        AddLabel(66, 112, 0, "Deposit all backpack gold");
        AddLabel(30, 160, 0, "Withdraw gold to your backpack:");
        AddBackground(30, 192, 180, 30, 3000);
        AddTextEntry(38, 197, 162, 22, 0, 0, "1000");
        AddButton(230, 195, 4005, 4007, 2);
        AddLabel(266, 197, 0, "Withdraw");
        AddLabel(30, 238, 0, "1 to 60,000 gold per withdrawal; capacity applies.");
        AddButton(320, 280, 4005, 4007, 0);
        AddLabel(356, 282, 0, "Close");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (info.ButtonID == 0 || _wallet.Deleted || from.Backpack == null || !_wallet.IsChildOf(from.Backpack)) { return; }
        if (info.ButtonID == 1) { _wallet.DepositBackpackGold(from); }
        else if (info.ButtonID == 2)
        {
            if (int.TryParse(info.GetTextEntry(0), out var amount)) { _wallet.Withdraw(from, amount); }
            else { from.SendMessage("Enter a whole number of gold coins."); }
        }
        from.SendGump(new HavenWalletGump(_wallet));
    }
}
