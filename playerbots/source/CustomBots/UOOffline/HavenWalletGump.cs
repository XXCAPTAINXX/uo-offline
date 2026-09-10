using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

public sealed class HavenWalletGump : Gump
{
    private readonly AdventurersWallet _wallet;
    public HavenWalletGump(AdventurersWallet wallet) : base(40, 40)
    {
        _wallet = wallet;
        AddBackground(0, 0, 460, 435, 5054);
        AddBackground(12, 12, 436, 411, 3000);
        AddHtml(30, 25, 400, 30, "<B>Adventurer's wallet</B>");
        AddLabel(30, 65, 0, $"Gold: {wallet.Balance:N0}");
        AddLabel(30, 87, 0, $"Haven marks: {wallet.HavenMarks:N0}");
        AddLabel(30, 109, 0, $"Astral shards: {wallet.AstralShards:N0}");
        AddButton(30, 140, 4005, 4007, 1);
        AddLabel(66, 142, 0, "Deposit backpack gold and marks");
        var available = wallet.RootParent is Mobile owner ? Banker.GetBalance(owner) : 0;
        AddLabel(30, 165, 0, $"Wallet + bank available: {available:N0}");
        AddLabel(30, 184, 0, "Amount (gold / marks / tithe):");
        AddBackground(30, 214, 180, 30, 3000);
        AddTextEntry(38, 219, 162, 22, 0, 0, "1000");
        AddButton(230, 218, 4005, 4007, 2);
        AddLabel(266, 220, 0, "Gold");
        AddButton(330, 218, 4005, 4007, 5);
        AddLabel(366, 220, 0, "Marks");
        AddButton(30, 280, 4005, 4007, 4);
        AddLabel(66, 282, 0, "Haven rewards");
        AddButton(240, 280, 4005, 4007, 3);
        AddLabel(276, 282, 0, "Astral treasures");
        AddHtml(30, 318, 400, 28, "Guild and other bank fees automatically use carried wallet gold.");
        AddButton(30, 350, 4005, 4007, 6);
        AddLabel(66, 352, 0, "Tithe gold for Chivalry (1 gold = 1 point)");
        AddButton(320, 390, 4005, 4007, 0);
        AddLabel(356, 392, 0, "Close");
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (info.ButtonID == 0 || _wallet.Deleted || from.Backpack == null || !_wallet.IsChildOf(from.Backpack)) { return; }
        if (info.ButtonID == 3) { from.SendGump(new HavenListGump(_wallet, new HavenAstralRewards.Menu(_wallet))); return; }
        if (info.ButtonID == 4) { HavenMarkRewards.DisplayTo(from, _wallet); return; }
        if (info.ButtonID == 1) { _wallet.DepositBackpackGold(from); _wallet.DepositBackpackMarks(from); }
        else if (info.ButtonID is 2 or 5 or 6)
        {
            if (int.TryParse(info.GetTextEntry(0), out var amount))
            {
                if (info.ButtonID == 6) { HavenTithing.Tithe(from, amount); }
                else if (info.ButtonID == 2) { _wallet.Withdraw(from, amount); }
                else if (!_wallet.WithdrawMarks(from, amount)) { from.SendMessage("Check your mark balance, withdrawal amount and backpack capacity."); }
            }
            else { from.SendMessage("Enter a whole-number amount."); }
        }
        from.SendGump(new HavenWalletGump(_wallet));
    }
}
