using System;
using Server.Gumps;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.UOOffline;

// Keeps the existing purchase/travel handlers, but gives every entry a readable
// text row and bounds the window height regardless of the number of choices.
public sealed class HavenListGump : Gump
{
    private const int PageSize = 4;
    private readonly Item _anchor;
    private readonly ItemListMenu _menu;
    private readonly int _page;
    private readonly bool _reopen;

    public HavenListGump(Item anchor, ItemListMenu menu, int page = 0, bool reopen = true) : base(30, 30)
    {
        _anchor = anchor;
        _menu = menu;
        _page = Math.Clamp(page, 0, Math.Max(0, (menu.Entries.Length - 1) / PageSize));
        _reopen = reopen;
        AddPage(0);
        AddBackground(0, 0, 540, 460, 5054);
        AddBackground(12, 12, 516, 436, 3000);
        AddHtml(28, 24, 482, 48, $"<BASEFONT COLOR=#111111><B>{menu.Question}</B></BASEFONT>");
        AddHtml(28, 76, 480, 28, menu is IHavenShop
            ? "<BASEFONT COLOR=#333333>Select an item to inspect its stats before buying.</BASEFONT>"
            : "<BASEFONT COLOR=#333333>Select your destination.</BASEFONT>");

        for (var row = 0; row < PageSize; row++)
        {
            var index = _page * PageSize + row;
            if (index >= menu.Entries.Length)
            {
                break;
            }
            var y = 112 + row * 66;
            AddButton(28, y + 5, 4005, 4007, index + 1);
            if (menu is IHavenShop shop)
            {
                var item = shop.CreateItem(index);
                try { AddItem(65, y, item.ItemID, item.Hue); }
                finally { item.Delete(); }
            }
            else { AddItem(65, y, menu.Entries[index].ItemID, menu.Entries[index].Hue); }
            AddHtml(115, y, 389, 42, $"<BASEFONT COLOR=#111111>{menu.Entries[index].Name}</BASEFONT>");
        }

        if (_page > 0)
        {
            AddButton(28, 395, 4014, 4016, 10001);
            AddLabel(66, 397, 0, "Previous");
        }
        AddLabel(205, 397, 0, $"Page {_page + 1} / {Math.Max(1, (menu.Entries.Length + PageSize - 1) / PageSize)}");
        if ((_page + 1) * PageSize < menu.Entries.Length)
        {
            AddButton(390, 395, 4005, 4007, 10002);
            AddLabel(428, 397, 0, "Next");
        }
        AddButton(390, 428, 4005, 4007, 0);
        AddLabel(428, 430, 0, "Close");
        if (anchor is StarterSupplyStone)
        {
            AddButton(28, 428, 4005, 4007, 10003);
            AddLabel(66, 430, 0, "Claim starter bundle");
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0)
        {
            return;
        }
        var from = sender.Mobile;
        if (from == null || _anchor.Deleted || from.Map != _anchor.Map ||
            !from.InRange(_anchor.GetWorldLocation(), 3))
        {
            from?.SendMessage("Return to the service stone to use this menu.");
            return;
        }
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
