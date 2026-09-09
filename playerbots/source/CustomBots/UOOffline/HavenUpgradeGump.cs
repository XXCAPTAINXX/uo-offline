using Server.Gumps;
using Server.Network;

namespace Server.UOOffline;

public sealed class HavenUpgradeGump : Gump
{
    private readonly HavenUpgradeStone _stone;
    private readonly int _tier;

    public HavenUpgradeGump(HavenUpgradeStone stone, Mobile from) : base(30, 30)
    {
        _stone = stone;
        var robe = from.Backpack?.FindItemByType<NewHavenAdventurersRobe>();
        _tier = robe?.UpgradeTier ?? -1;
        AddBackground(0, 0, 540, 490, 5054);
        AddBackground(12, 12, 516, 466, 3000);
        AddItem(465, 30, 0x1F03, 0x59B);
        AddHtml(30, 28, 420, 30, "<B>Upgrade your adventurer's robe</B>");
        if (robe == null)
        {
            AddHtml(30, 80, 420, 75, "Place your New Haven adventurer's robe in your backpack, then reopen this stone.");
        }
        else if (robe.BoundTo != null && robe.BoundTo != from)
        {
            AddHtml(30, 80, 420, 75, "This robe belongs to another character. Bring your own starter robe.");
        }
        else if (_tier >= robe.MaxUpgradeTier)
        {
            AddHtml(30, 80, 420, 75, "Your robe is already fully upgraded.");
        }
        else
        {
            AddHtml(30, 76, 420, 40, $"Current tier: {_tier}. Upgrade to tier {_tier + 1}.");
            AddHtml(30, 120, 420, 60, $"Cost: {_tier + 1} Haven mark(s), or {(_tier + 1) * 5000:N0} gold if you do not have enough marks.");
            AddButton(30, 193, 4005, 4007, 1);
            AddLabel(68, 195, 0, "Buy this upgrade");
        }
        if (robe != null)
        {
            AddHtml(30, 235, 225, 20, "<B>Current stats</B>");
            AddHtml(30, 258, 225, 170, HavenItemPreviewGump.Describe(robe), false, true);
            if (_tier < robe.MaxUpgradeTier)
            {
                var next = new NewHavenAdventurersRobe { UpgradeTier = _tier + 1 };
                try
                {
                    next.ApplyTier();
                    AddHtml(280, 235, 220, 20, "<B>After upgrade</B>");
                    AddHtml(280, 258, 220, 170, HavenItemPreviewGump.Describe(next), false, true);
                }
                finally { next.Delete(); }
            }
        }
        AddButton(210, 448, 4005, 4007, 0);
        AddLabel(248, 450, 0, "Close");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1 && sender.Mobile != null && !_stone.Deleted)
        {
            _stone.ApplyUpgrade(sender.Mobile, _tier);
        }
    }
}
