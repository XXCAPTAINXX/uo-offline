using System;
using System.Collections.Generic;
using System.Linq;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.UOOffline;

public sealed class ChampionCodexGump : Gump
{
    internal const int Rows = 18;
    private readonly ProgressionArchive _codex;
    private readonly int _page;
    private readonly bool _other;
    private readonly bool _ownedOnly;
    private readonly Dictionary<int, Serial> _withdrawals = new();

    internal static bool CanUse(Mobile from, ProgressionArchive codex) =>
        from?.Deleted == false && from.Backpack != null && codex?.Deleted == false && codex.IsChildOf(from.Backpack);
    public static void DisplayTo(Mobile from, ProgressionArchive codex, int page = 0, bool other = false, bool ownedOnly = false)
    {
        if (!CanUse(from, codex)) { return; }
        from.CloseGump<ChampionCodexGump>();
        from.SendGump(new ChampionCodexGump(codex, page, other, ownedOnly));
    }
    internal ChampionCodexGump(ProgressionArchive codex, int page = 0, bool other = false, bool ownedOnly = false) : base(30, 30)
    {
        _codex = codex; _other = other; _ownedOnly = ownedOnly;
        AddBackground(0, 0, 760, 580, 0xA28);
        AddLabel(26, 20, 0, "CHAMPION'S CODEX");
        AddLabel(26, 43, 0, "Your scrolls and champion treasures. Select an arrow to withdraw an item.");
        Button(26, 71, 1, "Skill scrolls");
        Button(173, 71, 2, "Other treasures");
        Button(352, 71, 3, ownedOnly ? "Show all skills" : "Show owned skills");
        Button(554, 71, 4, "Collect backpack");

        var items = codex.Items.Where(i => !i.Deleted).ToArray();
        int pages;
        if (other)
        {
            var treasures = items.Where(i => Column(i) < 0).OrderBy(i => i.Name ?? i.DefaultName).ToArray();
            pages = Math.Max(1, (treasures.Length + Rows - 1) / Rows);
            _page = Math.Clamp(page, 0, pages - 1);
            AddLabel(27, 109, 0, "TREASURE"); AddLabel(584, 109, 0, "AMOUNT");
            for (var row = 0; row < Rows && _page * Rows + row < treasures.Length; row++)
            {
                var item = treasures[_page * Rows + row];
                var y = 139 + row * 21;
                var id = 100 + row;
                _withdrawals[id] = item.Serial;
                Button(26, y, id, item.Name ?? item.DefaultName);
                AddTooltip(1042971, "Withdraw this item or stack to your backpack.");
                AddLabel(600, y + 2, 0, item.Amount.ToString("N0"));
            }
            if (treasures.Length == 0) { AddLabel(27, 150, 0, "No other treasures stored yet."); }
        }
        else
        {
            var skills = Enum.GetValues<SkillName>().Where(skill => !ownedOnly || items.Any(i => i is SpecialScroll scroll && scroll.Skill == skill && Column(i) >= 0))
                .OrderBy(skill => SkillInfo.Table[(int)skill].Name).ToArray();
            pages = Math.Max(1, (skills.Length + Rows - 1) / Rows);
            _page = Math.Clamp(page, 0, pages - 1);
            AddLabel(27, 109, 0, "SKILL");
            var headers = new[] { "105", "110", "115", "120", "Alacrity", "Transcend." };
            for (var column = 0; column < headers.Length; column++) { AddLabel(214 + column * 87, 109, 0, headers[column]); }
            for (var row = 0; row < Rows && _page * Rows + row < skills.Length; row++)
            {
                var skill = skills[_page * Rows + row];
                var y = 139 + row * 21;
                AddLabel(27, y + 2, 0, SkillInfo.Table[(int)skill].Name);
                for (var column = 0; column < 6; column++)
                {
                    var matches = items.Where(i => i is SpecialScroll scroll && scroll.Skill == skill && Column(i) == column).ToArray();
                    var x = 200 + column * 87;
                    if (matches.Length == 0) { AddLabel(x + 32, y + 2, 0x3B2, "--"); continue; }
                    var id = 100 + row * 6 + column;
                    _withdrawals[id] = matches[0].Serial;
                    AddButton(x, y, 4005, 4007, id);
                    AddTooltip(1042971, column == 5 ? $"{matches.Length} scrolls; {matches.Cast<SpecialScroll>().Sum(s => s.Value):F1} total skill points. Withdraw one scroll." : $"Withdraw one {headers[column]} scroll for {SkillInfo.Table[(int)skill].Name}.");
                    AddLabel(x + 32, y + 2, 0, matches.Length.ToString("N0"));
                }
            }
            if (skills.Length == 0) { AddLabel(27, 150, 0, "No skill scrolls stored yet. Collect backpack or drag scrolls onto the codex."); }
        }
        AddLabel(26, 527, 0, "Withdrawals preserve the original scroll. Transcendence totals appear on hover.");
        if (_page > 0) { Button(26, 550, 5, "Previous"); }
        AddLabel(216, 552, 0, $"Page {_page + 1} / {pages}");
        if (_page + 1 < pages) { Button(350, 550, 6, "Next"); }
        Button(476, 550, 7, "Open bag");
        Button(644, 550, 0, "Close");
    }
    internal static int Column(Item item) => item switch
    {
        PowerScroll { Value: 105 } => 0,
        PowerScroll { Value: 110 } => 1,
        PowerScroll { Value: 115 } => 2,
        PowerScroll { Value: 120 } => 3,
        ScrollofAlacrity => 4,
        ScrollofTranscendence => 5,
        _ => -1
    };
    private void Button(int x, int y, int id, string label)
    { AddButton(x, y, 4005, 4007, id); AddLabel(x + 32, y + 2, 0, label); }
    internal bool Withdraw(Mobile from, int button)
    {
        if (!CanUse(from, _codex) || !_withdrawals.TryGetValue(button, out var serial) ||
            World.FindItem(serial) is not Item item || item.Deleted || item.Parent != _codex) { return false; }
        if (!from.Backpack.TryDropItem(from, item, false)) { from.SendMessage("Make room in your backpack first."); return false; }
        return true;
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (info.ButtonID == 0 || !CanUse(from, _codex)) { return; }
        switch (info.ButtonID)
        {
            case 1: DisplayTo(from, _codex, 0, false, _ownedOnly); return;
            case 2: DisplayTo(from, _codex, 0, true, _ownedOnly); return;
            case 3: DisplayTo(from, _codex, 0, _other, !_ownedOnly); return;
            case 4: _codex.CollectAll(from); break;
            case 5: DisplayTo(from, _codex, _page - 1, _other, _ownedOnly); return;
            case 6: DisplayTo(from, _codex, _page + 1, _other, _ownedOnly); return;
            case 7: _codex.DisplayTo(from); break;
            default: Withdraw(from, info.ButtonID); break;
        }
        DisplayTo(from, _codex, _page, _other, _ownedOnly);
    }
}
