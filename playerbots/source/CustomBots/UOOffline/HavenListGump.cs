using System;
using Server.Gumps;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.UOOffline;

// Keeps the existing purchase/travel handlers, but gives every entry a readable
// text row and bounds the window height regardless of the number of choices.
public sealed class HavenListGump : Gump
{
    private readonly int _pageSize;
    private readonly Item _anchor;
    private readonly ItemListMenu _menu;
    private readonly int _page;
    private readonly bool _reopen;

    public HavenListGump(Item anchor, ItemListMenu menu, int page = 0, bool reopen = true) : base(30, 30)
    {
        _pageSize = menu is IHavenShop ? 4 : 12;
        _anchor = anchor;
        _menu = menu;
        _page = Math.Clamp(page, 0, Math.Max(0, (menu.Entries.Length - 1) / _pageSize));
        _reopen = reopen;
        AddPage(0);
        AddBackground(0, 0, 540, 460, 5054);
        AddBackground(12, 12, 516, 436, 3000);
        AddHtml(28, 24, 482, 48, $"<BASEFONT COLOR=#111111><B>{menu.Question}</B></BASEFONT>");
        AddHtml(28, 76, 480, 28, menu is IHavenShop
            ? "<BASEFONT COLOR=#333333>Hover over an item for stats; select it for the full preview.</BASEFONT>"
            : "<BASEFONT COLOR=#333333>Select your destination.</BASEFONT>");

        for (var row = 0; row < _pageSize; row++)
        {
            var index = _page * _pageSize + row;
            if (index >= menu.Entries.Length)
            {
                break;
            }
            string tooltip = null;
            string summary = null;
            var y = 112 + row * (menu is IHavenShop ? 66 : 23);
            AddButton(28, y + 5, 4005, 4007, index + 1);
            if (menu is IHavenShop shop)
            {
                var item = shop.CreateItem(index);
                try
                {
                    tooltip = HavenItemPreviewGump.Tooltip(item);
                    summary = HavenItemPreviewGump.ShortStats(item);
                    AddTooltip(1042971, tooltip);
                    AddItem(65, y, item.ItemID, item.Hue);
                    AddTooltip(1042971, tooltip);
                }
                finally { item.Delete(); }
            }

            AddHtml(menu is IHavenShop ? 115 : 66, y, menu is IHavenShop ? 389 : 442, menu is IHavenShop ? 40 : 23, $"<BASEFONT COLOR=#111111>{menu.Entries[index].Name}</BASEFONT>");
            if (tooltip != null) { AddTooltip(1042971, tooltip); }
            if (summary != null) { AddLabelCropped(115, y + 40, 389, 22, 0x3B2, System.Net.WebUtility.HtmlDecode(summary)); AddTooltip(1042971, tooltip); }
        }

        if (_page > 0)
        {
            AddButton(28, 395, 4014, 4016, 10001);
            AddLabel(66, 397, 0, "Previous");
        }
        AddLabel(205, 397, 0, $"Page {_page + 1} / {Math.Max(1, (menu.Entries.Length + _pageSize - 1) / _pageSize)}");
        if ((_page + 1) * _pageSize < menu.Entries.Length)
        {
            AddButton(390, 395, 4005, 4007, 10002);
            AddLabel(428, 397, 0, "Next");
        }
        AddButton(390, 420, 4005, 4007, 0);
        AddLabel(428, 422, 0, "Close");
        if (menu is HavenTrainingStone.Menu) { AddButton(28, 420, 4014, 4016, 10004); AddLabel(66, 422, 0, "Categories"); }
        if (anchor is StarterSupplyStone)
        {
            AddButton(28, 420, 4005, 4007, 10003);
            AddLabel(66, 422, 0, "Claim starter bundle");
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0)
        {
            return;
        }
        var from = sender.Mobile;
        if (!HavenShopAccess.CanUse(from, _anchor))
        {
            from?.SendMessage("Return to the service stone to use this menu.");
            return;
        }
        if (info.ButtonID == 10004 && _anchor is HavenTrainingStone training) { training.OnDoubleClick(from); return; }
        if (info.ButtonID is 10001 or 10002)
        {
            from.SendGump(new HavenListGump(_anchor, _menu, _page + (info.ButtonID == 10001 ? -1 : 1), _reopen));
            return;
        }
        if (info.ButtonID == 10003 && _anchor is StarterSupplyStone)
        {
            StarterBundleClaims.Claim(from);
            from.SendGump(new HavenListGump(_anchor, _menu, _page, _reopen));
            return;
        }
        var index = info.ButtonID - 1;
        if (index < 0 || index >= _menu.Entries.Length)
        {
            return;
        }
        if (_menu is IHavenShop)
        {
            from.SendGump(new HavenItemPreviewGump(_anchor, _menu, index, _page));
            return;
        }
        _menu.OnResponse(sender, index);
        if (_reopen && from.Map == _anchor.Map && from.InRange(_anchor.Location, 3))
        {
            from.SendGump(new HavenListGump(_anchor, _menu, _page, _reopen));
        }
    }
}
