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
    public class HavenCodexBrowserGump : HavenMenuGump
    {
        const int PageSize = 7;
        readonly HavenChampionCodex _book;
        readonly int _page, _category;
        readonly string _search, _selected;
        readonly bool _byCount;
        readonly Dictionary<int, string> _rows = new Dictionary<int, string>();
        static readonly string[] Categories = { "All items", "Power scrolls", "Stat caps", "Alacrity", "Transcendence", "Mastery primers", "Skulls / binders" };

        public static int Category(Item item)
        {
            if (item is PowerScroll) return 1;
            if (item is StatCapScroll) return 2;
            if (item is ScrollOfAlacrity) return 3;
            if (item is ScrollOfTranscendence) return 4;
            if (item is SkillMasteryPrimer) return 5;
            return 6;
        }
        public static string Key(Item item)
        {
            var skull = item as ChampionSkull;
            if (skull != null) return skull.Type + " champion skull";
            var primer = item as SkillMasteryPrimer;
            if (primer != null) return SkillInfo.Table[(int)primer.Skill].Name + " - Mastery " + primer.Volume;
            if (item is StatCapScroll) return "Stat cap - " + ((StatCapScroll)item).Value;
            var scroll = item as SpecialScroll;
            if (scroll != null) return SkillInfo.Table[(int)scroll.Skill].Name + " - " +
                (item is PowerScroll ? "Power " : item is ScrollOfAlacrity ? "Alacrity" : "Transcendence +") +
                (item is ScrollOfAlacrity ? "" : scroll.Value.ToString("0.0"));
            return item.Name ?? "Scroll binder deed";
        }
        public static IGrouping<string, Item>[] Groups(HavenChampionCodex book, int category, string search, bool byCount)
        {
            var groups = book.Items.Where(x => !x.Deleted && (category == 0 || Category(x) == category) &&
                Key(x).IndexOf(search ?? "", StringComparison.OrdinalIgnoreCase) >= 0).GroupBy(Key);
            return (byCount ? groups.OrderByDescending(x => x.Count()).ThenBy(x => x.Key) : groups.OrderBy(x => x.Key)).ToArray();
        }
        public HavenCodexBrowserGump(Mobile p, HavenChampionCodex book, int page, int category = 0,
            string search = "", bool byCount = false, string selected = null) : base(40, 40)
        {
            _book = book; _category = Math.Max(0, Math.Min(6, category));
            _search = (search ?? "").Trim(); if (_search.Length > 40) _search = _search.Substring(0, 40);
            _byCount = byCount;
            var groups = Groups(book, _category, _search, byCount);
            int pages = Math.Max(1, (groups.Length + PageSize - 1) / PageSize);
            _page = Math.Max(0, Math.Min(pages - 1, page));
            var visible = groups.Skip(_page * PageSize).Take(PageSize).ToArray();
            var chosen = visible.FirstOrDefault(x => x.Key == selected) ?? visible.FirstOrDefault();
            _selected = chosen == null ? null : chosen.Key;

            AddBackground(0, 0, 740, 550, 0xA28);
            Text(24, 20, 420, 26, "<B>Champion's Codex</B>");
            Text(505, 22, 210, 24, book.Items.Count + " items stored");
            Button(24, 62, 1, "Collect backpack", 160);
            Button(225, 62, 2, "Target item or bag", 185);
            Button(475, 62, 9, "What can I store?", 210);
            Text(24, 103, 160, 24, "<B>Browse</B>");
            for (int i = 0; i < Categories.Length; i++) {
                if (_category == i) Text(24, 139 + i * 34, 163, 28, "<B>" + Categories[i] + "</B>");
                else Button(24, 139 + i * 34, 20 + i, Categories[i], 133);
            }
            Text(24, 401, 156, 83, "Select a row to inspect.<BR>Hover its arrow for full properties.");
            AddBackground(195, 105, 250, 30, 0xBB8);
            AddTextEntry(203, 110, 233, 22, 0, 1, _search);
            Button(455, 109, 5, "Search", 67);
            Button(565, 109, 6, byCount ? "Sort: count" : "Sort: A-Z", 128);
            Text(195, 145, 390, 24, "<B>" + Categories[_category] + "</B>");
            Text(649, 145, 70, 24, "<B>Qty</B>");
            for (int row = 0; row < visible.Length; row++) {
                var group = visible[row]; int y = 179 + row * 28;
                _rows[100 + row] = group.Key;
                ItemArrow(this, p, group.First(), 195, y, 100 + row);
                Text(230, y, 409, 25, group.Key == _selected ? "<B>" + group.Key + "</B>" : group.Key);
                Text(651, y, 62, 24, group.Count().ToString());
            }
            if (chosen == null) Text(195, 182, 506, 95, book.Items.Count == 0 ?
                "Your Codex is empty.<BR>Use Collect backpack or target an item or bag above." :
                "No matching items.<BR>Clear the search or choose another category.");
            else {
                Text(195, 383, 520, 26, "<B>" + chosen.Key + "</B>");
                var power = chosen.First() as PowerScroll;
                string detail = "Withdraw moves one original item to your backpack.";
                if (power != null) {
                    int tier = (int)power.Value, cost = HavenChampionCodex.Cost(tier), split = HavenChampionCodex.Cost(tier - 5);
                    detail = (cost > 0 ? "Combine: " + cost + " of " + tier + " makes one " + (tier + 5) + ". " : "Maximum power-scroll tier. ") +
                        (split > 0 ? "Split: one " + tier + " makes " + split + " of " + (tier - 5) + "." : "");
                    if (cost > 0) {
                        if (chosen.Count() >= cost) Button(356, 465, 7, "Combine " + cost + " into 1", 164);
                        else Text(356, 465, 182, 35, "Need " + (cost - chosen.Count()) + " more to combine");
                    }
                    if (split > 0) Button(558, 465, 8, "Split into " + split, 148);
                }
                Text(195, 415, 518, 44, detail);
                ItemArrow(this, p, chosen.First(), 195, 465, 10);
                Text(228, 465, 123, 24, "Withdraw one");
            }
            Button(24, 507, 0, "Back to table", 132);
            if (_page > 0) Button(195, 507, 3, "Previous", 105);
            Text(361, 507, 173, 24, "Page " + (_page + 1) + " / " + pages);
            if (_page + 1 < pages) Button(565, 507, 4, "Next", 100);
        }
        void Text(int x, int y, int w, int h, string text) { AddHtml(x, y, w, h, "<BASEFONT COLOR=#342B23>" + text + "</BASEFONT>", false, false); }
        void Button(int x, int y, int id, string label, int width) { AddButton(x, y, 0xFA5, 0xFA7, id, GumpButtonType.Reply, 0); Text(x + 33, y, width, 26, label); }
        void Reopen(Mobile p, int page, int category, string search, bool byCount, string selected)
        {
            if (!_book.CanUse(p)) return;
            p.CloseGump(typeof(HavenCodexBrowserGump)); p.SendGump(new HavenCodexBrowserGump(p, _book, page, category, search, byCount, selected));
        }
        public override void OnResponse(NetState state, RelayInfo info)
        {
            var p = state.Mobile; int id = info.ButtonID;
            if (!_book.CanUse(p)) return;
            if (id == 0) { _book.Show(p); return; }
            int page = _page, category = _category; string search = _search, selected = _selected; bool byCount = _byCount;
            if (id == 1) p.SendMessage("Collected " + _book.Collect(p, p.Backpack) + " item(s).");
            else if (id == 2) { p.SendMessage("Target a scroll, item or bag. Unlocked bags and sub-bags are checked."); p.Target = new CollectTarget(this); return; }
            else if (id == 3) page--;
            else if (id == 4) page++;
            else if (id == 5) { search = info.GetTextEntry(1) == null ? "" : info.GetTextEntry(1).Text; page = 0; }
            else if (id == 6) { byCount = !byCount; page = 0; }
            else if (id == 9) p.SendMessage("Stores power scrolls, stat-cap scrolls, Alacrity, Transcendence, mastery primers, champion skulls and scroll binder deeds. It keeps original items. Contents use no extra pack slots; weight still applies.");
            else if (id >= 20 && id <= 26) { category = id - 20; page = 0; }
            else if (_rows.ContainsKey(id)) selected = _rows[id];
            else if (id == 7 || id == 8 || id == 10) {
                var item = _book.Items.FirstOrDefault(x => !x.Deleted && Key(x) == _selected);
                bool ok = item != null && (id == 10 ? _book.Withdraw(p, item) : item is PowerScroll &&
                    _book.Convert(p, ((PowerScroll)item).Skill, (int)((PowerScroll)item).Value, id == 8));
                p.SendMessage(ok ? (id == 10 ? "One item moved to your backpack." : "Scrolls converted inside your Codex.") : "Could not complete: check matching scroll quantities and available storage space. Nothing changed.");
            }
            Reopen(p, page, category, search, byCount, selected);
        }
        class CollectTarget : Target
        {
            readonly HavenCodexBrowserGump _menu;
            public CollectTarget(HavenCodexBrowserGump menu) : base(12, false, TargetFlags.None) { _menu = menu; }
            protected override void OnTarget(Mobile p, object obj) { p.SendMessage("Collected " + _menu._book.Collect(p, obj as Item) + " item(s)."); }
            protected override void OnTargetFinish(Mobile p) { _menu.Reopen(p, _menu._page, _menu._category, _menu._search, _menu._byCount, _menu._selected); }
        }
    }
}
