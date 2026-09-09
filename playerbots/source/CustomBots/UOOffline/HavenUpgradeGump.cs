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
        AddBackground(0, 0, 490, 290, 5054);
        AddBackground(12, 12, 466, 266, 3000);
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
        AddButton(180, 242, 4005, 4007, 0);
        AddLabel(218, 244, 0, "Close");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 1 && sender.Mobile != null && !_stone.Deleted)
        {
            _stone.ApplyUpgrade(sender.Mobile, _tier);
        }
    }
}
