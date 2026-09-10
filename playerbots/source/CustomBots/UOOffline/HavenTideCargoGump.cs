using System;
using Server.Gumps;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

// Mounted creatures are internalized. Transfer controls avoid unreachable-container packets.
public sealed class HavenTideCargoGump : Gump
{
    private readonly HavenTideSteed _steed;
    private readonly Item[] _items;
    private readonly int _page;
    public HavenTideCargoGump(HavenTideSteed steed, int page = 0) : base(70, 70)
    {
        _steed = steed; _items = steed.Backpack.Items.ToArray(); _page = Math.Clamp(page, 0, Math.Max(0, (_items.Length - 1) / 8));
        AddBackground(0, 0, 460, 390, 9270); AddLabel(25, 20, 1152, "Sea horse cargo — select an item to withdraw");
        for (var row = 0; row < 8 && _page * 8 + row < _items.Length; row++)
        {
            var item = _items[_page * 8 + row]; var y = 58 + row * 32;
            AddButton(25, y, 4005, 4007, 100 + row); AddItem(65, y, item.ItemID, item.Hue); AddItemProperty(item.Serial);
            AddLabelCropped(108, y, 325, 24, 1152, item.Name ?? item.GetType().Name);
        }
        AddButton(25, 330, 4005, 4007, 1); AddLabel(60, 330, 1152, "Deposit...");
        AddButton(180, 330, 4014, 4016, 2); AddLabel(215, 330, 1152, "Prev");
        AddButton(290, 330, 4005, 4007, 3); AddLabel(325, 330, 1152, "Next");
        AddButton(330, 360, 4017, 4019, 0); AddLabel(365, 360, 1152, "Close");
    }
    internal static bool Withdraw(HavenTideSteed steed, Mobile from, Item item)
    {
        if (!steed.CanManage(from) || item?.Deleted != false || item.Parent != steed.Backpack || !item.Movable ||
            from.Backpack == null || !from.Backpack.CheckHold(from, item, false, true)) { return false; }
        from.Backpack.DropItem(item); return true;
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;
        if (!_steed.CanManage(from) || info.ButtonID == 0) { return; }
        var page = _page;
        if (info.ButtonID == 1) { from.Target = new DepositTarget(_steed); }
        else if (info.ButtonID == 2) { page--; }
        else if (info.ButtonID == 3) { page++; }
        else if (info.ButtonID >= 100 && info.ButtonID < 108 && _page * 8 + info.ButtonID - 100 < _items.Length)
        { if (!Withdraw(_steed, from, _items[_page * 8 + info.ButtonID - 100])) { from.SendMessage("Cannot withdraw that item; check space and ownership."); } }
        from.SendGump(new HavenTideCargoGump(_steed, page));
    }
    private sealed class DepositTarget : Target
    {
        private readonly HavenTideSteed _steed;
        public DepositTarget(HavenTideSteed steed) : base(-1, false, TargetFlags.None) { _steed = steed; }
        protected override void OnTarget(Mobile from, object target)
        {
            if (!_steed.CanManage(from) || target is not Item item || !item.Movable || !item.IsChildOf(from.Backpack)) { return; }
            _steed.Backpack.TryDropItem(from, item, true); from.CloseGump<HavenTideCargoGump>(); from.SendGump(new HavenTideCargoGump(_steed));
        }
    }
}
