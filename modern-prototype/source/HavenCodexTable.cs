using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Gumps;
using Server.Network;
using Server.Targeting;
using Server.Spells.SkillMasteries;

namespace Server.HavenPrototype
{
    public class HavenCodexGump : HavenMenuGump
    {
        protected override bool CompactButtons => true;
        const int PageSize = 18;
        readonly HavenChampionCodex _book;
        readonly int _page;
        readonly bool _owned;
        readonly string _search;
        readonly Dictionary<int, Tuple<SkillName, int>> _cells = new Dictionary<int, Tuple<SkillName, int>>();
        static readonly int[] Columns = { 177, 241, 305, 369, 438, 502, 566, 635, 725 };
        static readonly string[] Headings = { "105", "110", "115", "120", "Vol I", "Vol II", "Vol III", "Alacrity", "Trans." };
        public static Item[] Cell(HavenChampionCodex book, SkillName skill, int column)
        {
            return book.Items.Where(item => !item.Deleted &&
                (column < 4 ? item is PowerScroll && ((PowerScroll)item).Skill == skill && ((PowerScroll)item).Value == 105 + column * 5 :
                 column < 7 ? item is SkillMasteryPrimer && ((SkillMasteryPrimer)item).Skill == skill && ((SkillMasteryPrimer)item).Volume == column - 3 :
                 column == 7 ? item is ScrollOfAlacrity && ((ScrollOfAlacrity)item).Skill == skill :
                 column == 8 && item is ScrollOfTranscendence && ((ScrollOfTranscendence)item).Skill == skill)).OrderBy(x => x is ScrollOfTranscendence ? ((ScrollOfTranscendence)x).Value : 0).ToArray();
        }
        public static string Quantity(Item[] items, int column)
        {
            if (items.Length == 0) return "--";
            return column == 8 ? items.Cast<ScrollOfTranscendence>().Sum(x => x.Value).ToString("0.0") : items.Length.ToString();
        }
        public HavenCodexGump(Mobile p, HavenChampionCodex book, int page = 0, bool owned = false, string search = "") : base(30, 30)
        {
            _book = book; _owned = owned; _search = (search ?? "").Trim();
            if (_search.Length > 40) _search = _search.Substring(0, 40);
            var skills = Enum.GetValues(typeof(SkillName)).Cast<SkillName>().Where(s =>
                SkillInfo.Table[(int)s].Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0 &&
                (!owned || Enumerable.Range(0, 9).Any(c => Cell(book, s, c).Length > 0))).OrderBy(s => SkillInfo.Table[(int)s].Name).ToArray();
            int pages = Math.Max(1, (skills.Length + PageSize - 1) / PageSize);
            _page = Math.Max(0, Math.Min(pages - 1, page));
            AddBackground(0, 0, 850, 650, 0xA28);
            Text(24, 20, 410, 28, "<B>Champion's Codex</B>");
            Text(592, 22, 230, 25, book.Items.Count + " items stored");
            Button(24, 60, 1, "Collect backpack", 150);
            Button(230, 60, 2, "Target bag / item", 160);
            Button(465, 60, 3, "Stat caps / skulls / binders", 300);
            AddBackground(24, 96, 227, 29, 0xBB8); AddTextEntry(32, 101, 208, 22, 0, 1, _search);
            Button(263, 101, 4, "Search skill", 120);
            Button(434, 101, 5, owned ? "Show all skills" : "Show stored skills", 190);
            Text(665, 101, 162, 25, "Page " + (_page + 1) + " / " + pages);
            Text(24, 140, 145, 25, "<B>Skill</B>");
            for (int c = 0; c < Columns.Length; c++) Text(Columns[c], 140, c == 8 ? 91 : 68, 24, "<B>" + Headings[c] + "</B>");
            for (int row = 0; row < PageSize && _page * PageSize + row < skills.Length; row++) {
                var skill = skills[_page * PageSize + row]; int y = 170 + row * 23;
                Text(24, y, 149, 23, SkillInfo.Table[(int)skill].Name);
                for (int c = 0; c < Columns.Length; c++) {
                    var items = Cell(book, skill, c); int x = Columns[c];
                    if (items.Length == 0) Text(x + 6, y, 50, 23, "--");
                    else {
                        int id = 100 + row * 9 + c; _cells[id] = Tuple.Create(skill, c);
                        ItemArrow(this, p, items[0], x, y, id);
                        Text(x + 32, y, c == 8 ? 66 : 34, 23, Quantity(items, c));
                    }
                }
            }
            if (skills.Length == 0) Text(24, 180, 780, 45, "No matching skills. Clear the search or show all skills.");
            Text(24, 588, 793, 25, "Click a quantity arrow for withdraw / combine / split. Trans. shows total stored skill points.");
            Button(24, 618, 0, "Close", 90);
            if (_page > 0) Button(258, 618, 6, "Previous", 120);
            if (_page + 1 < pages) Button(447, 618, 7, "Next", 100);
            Text(610, 618, 212, 23, "Binders: " + book.Items.Count(x => x.GetType().Name == "ScrollBinderDeed"));
        }
        internal void Show(Mobile p)
        {
            if (!_book.CanUse(p)) return;
            p.CloseGump(typeof(HavenCodexGump)); p.SendGump(new HavenCodexGump(p, _book, _page, _owned, _search));
        }
        void Text(int x, int y, int w, int h, string text) { AddHtml(x, y, w, h, "<BASEFONT COLOR=#342B23>" + text + "</BASEFONT>", false, false); }
        void Button(int x, int y, int id, string label, int width) { AddButton(x, y, 0xFA5, 0xFA7, id, GumpButtonType.Reply, 0); Text(x + 33, y, width, 24, label); }
        public override void OnResponse(NetState state, RelayInfo info)
        {
            var p = state.Mobile; int id = info.ButtonID;
            if (id == 0 || !_book.CanUse(p)) return;
            if (id == 1) p.SendMessage("Collected " + _book.Collect(p, p.Backpack) + " item(s).");
            else if (id == 2) { p.SendMessage("Target an item or bag; sub-bags are checked too."); p.Target = new CollectTarget(this); return; }
            else if (id == 3) { p.SendGump(new HavenCodexBrowserGump(p, _book, 0, 0)); return; }
            else if (_cells.ContainsKey(id)) {
                var cell = _cells[id]; p.CloseGump(typeof(HavenCodexCellGump));
                p.SendGump(new HavenCodexCellGump(p, _book, this, cell.Item1, cell.Item2)); return;
            }
            int page = id == 6 ? _page - 1 : id == 7 ? _page + 1 : id == 4 || id == 5 ? 0 : _page;
            string search = id == 4 && info.GetTextEntry(1) != null ? info.GetTextEntry(1).Text : _search;
            p.SendGump(new HavenCodexGump(p, _book, page, id == 5 ? !_owned : _owned, search));
        }
        class CollectTarget : Target
        {
            readonly HavenCodexGump _menu;
            public CollectTarget(HavenCodexGump menu) : base(12, false, TargetFlags.None) { _menu = menu; }
            protected override void OnTarget(Mobile p, object target) { p.SendMessage("Collected " + _menu._book.Collect(p, target as Item) + " item(s)."); }
            protected override void OnTargetFinish(Mobile p) { _menu.Show(p); }
        }
    }

    public class HavenCodexCellGump : HavenMenuGump
    {
        protected override bool CompactButtons => true;
        readonly HavenChampionCodex _book; readonly HavenCodexGump _table;
        readonly SkillName _skill; readonly int _column, _page;
        readonly Dictionary<int, Item> _withdraw = new Dictionary<int, Item>();
        public HavenCodexCellGump(Mobile p, HavenChampionCodex book, HavenCodexGump table, SkillName skill, int column, int page = 0) : base(170, 130)
        {
            _book = book; _table = table; _skill = skill; _column = column;
            var items = HavenCodexGump.Cell(book, skill, column);
            var groups = items.GroupBy(HavenCodexBrowserGump.Key).ToArray();
            int pages = Math.Max(1, (groups.Length + 5) / 6); _page = Math.Max(0, Math.Min(pages - 1, page));
            AddBackground(0, 0, 550, 365, 0xA28);
            Text(24, 20, 500, 28, "<B>" + SkillInfo.Table[(int)skill].Name + " - " +
                (column < 4 ? "Power " + (105 + column * 5) : column < 7 ? "Mastery " + (column - 3) : column == 7 ? "Alacrity" : "Transcendence") + "</B>");
            Text(24, 54, 500, 38, column == 8 ? "Total: " + HavenCodexGump.Quantity(items, column) + " skill points. Choose an exact scroll below." : "Stored: " + items.Length + ". Each withdrawal moves one original item.");
            for (int row = 0; row < 6 && _page * 6 + row < groups.Length; row++) {
                var group = groups[_page * 6 + row]; int y = 103 + row * 27;
                _withdraw[100 + row] = group.First(); ItemArrow(this, p, group.First(), 24, y, 100 + row);
                Text(58, y, 461, 25, column == 8 ? "Withdraw +" + ((ScrollOfTranscendence)group.First()).Value.ToString("0.0") + "  (" + group.Count() + " stored)" : "Withdraw one  (" + group.Count() + " stored)");
            }
            if (items.Length == 0) Text(24, 103, 500, 26, "No items remain in this cell.");
            if (column < 4) {
                int tier = 105 + column * 5, cost = HavenChampionCodex.Cost(tier), split = HavenChampionCodex.Cost(tier - 5);
                if (cost > 0) {
                    if (items.Length >= cost) Button(24, 254, 1, "Combine " + cost + " x " + tier + " into one " + (tier + 5), 465);
                    else Text(24, 254, 500, 24, "Combine needs " + cost + " x " + tier + "; " + (cost - items.Length) + " more needed.");
                }
                if (split > 0 && items.Length > 0) Button(24, 283, 2, "Split one " + tier + " into " + split + " x " + (tier - 5), 465);
            }
            Button(24, 325, 0, "Back to table", 160);
            if (_page > 0) Button(238, 325, 3, "Previous", 110);
            if (_page + 1 < pages) Button(407, 325, 4, "Next", 90);
        }
        void Text(int x, int y, int w, int h, string text) { AddHtml(x, y, w, h, "<BASEFONT COLOR=#342B23>" + text + "</BASEFONT>", false, false); }
        void Button(int x, int y, int id, string label, int width) { AddButton(x, y, 0xFA5, 0xFA7, id, GumpButtonType.Reply, 0); Text(x + 33, y, width, 25, label); }
        public override void OnResponse(NetState state, RelayInfo info)
        {
            var p = state.Mobile; if (!_book.CanUse(p)) return;
            int id = info.ButtonID; if (id == 0) { _table.Show(p); return; }
            if (id == 1 || id == 2) p.SendMessage(_column < 4 && _book.Convert(p, _skill, 105 + _column * 5, id == 2) ? "Converted inside your Codex." : "Not enough matching scrolls or storage space. Nothing changed.");
            else if (_withdraw.ContainsKey(id)) p.SendMessage(_book.Withdraw(p, _withdraw[id]) ? "One scroll moved to your backpack." : "Cannot withdraw: check backpack space or refresh the menu.");
            p.SendGump(new HavenCodexCellGump(p, _book, _table, _skill, _column, id == 3 ? _page - 1 : id == 4 ? _page + 1 : _page));
        }
    }
}
