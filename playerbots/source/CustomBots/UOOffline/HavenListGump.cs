using System;
using Server.Gumps;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.UOOffline;

// Keeps the existing purchase/travel handlers, but gives every entry a readable
// text row and bounds the window height regardless of the number of choices.
public sealed class HavenListGump : Gump
{
    private const int PageSize = 6;
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
        AddHtml(28, 76, 480, 28, "<BASEFONT COLOR=#333333>Choose an item below. Prices are shown before purchase.</BASEFONT>");

        for (var row = 0; row < PageSize; row++)
        {
            var index = _page * PageSize + row;
            if (index >= menu.Entries.Length)
            {
                break;
            }
            var y = 114 + row * 45;
            AddButton(28, y + 5, 4005, 4007, index + 1);
            AddHtml(66, y, 440, 42, $"<BASEFONT COLOR=#111111>{menu.Entries[index].Name}</BASEFONT>");
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
        AddButton(220, 428, 4005, 4007, 0);
        AddLabel(258, 430, 0, "Close");
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
        var index = info.ButtonID - 1;
        if (index < 0 || index >= _menu.Entries.Length)
        {
            return;
        }
        _menu.OnResponse(sender, index);
        if (_reopen && from.Map == _anchor.Map && from.InRange(_anchor.Location, 3))
        {
            from.SendGump(new HavenListGump(_anchor, _menu, _page, _reopen));
        }
    }
}
