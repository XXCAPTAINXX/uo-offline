using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Engines.CannedEvil;
using Server.HavenPrototype;

public static class CodexMenuSmoke
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); File.AppendAllText("codex-menu-checks.log", "PASS " + name + "\n"); }
    static void Click(HavenCodexBrowserGump menu, Mobile player, int id)
    {
        var state = (NetState)FormatterServices.GetUninitializedObject(typeof(NetState)); state.Mobile = player;
        menu.OnResponse(state, new RelayInfo(id, new int[0], new TextRelay[0]));
    }
    public static void Initialize() { if (File.Exists("CODEX-MENU-TEST-ONLY")) EventSink.ServerStarted += () => Timer.DelayCall(TimeSpan.FromSeconds(4), Run); }
    static void Run()
    {
        try {
            var p = new PlayerMobile { Name = "Codex menu fixture", RawStr = 200 }; p.AddItem(new Backpack());
            p.Skills.Magery.Base = 0;
            Server.Spells.Spell[] travel = { new Server.Spells.Fourth.RecallSpell(p, null), new Server.Spells.Sixth.MarkSpell(p, null), new Server.Spells.Seventh.GateTravelSpell(p, null) };
            foreach (var spell in travel) { double min, max; spell.GetCastSkills(out min, out max); Check(min == 0 && max == 0 && spell.CheckFizzle(), "zero Magery succeeds for " + spell.GetType().Name); Check(spell.GetMana() > 0, "travel retains mana cost"); }
            double combatMin, combatMax; new Server.Spells.Seventh.FlameStrikeSpell(p, null).GetCastSkills(out combatMin, out combatMax); Check(combatMin > 0, "combat spells retain skill requirements");
            var book = new HavenChampionCodex(); p.Backpack.DropItem(book);
            for (int i = 0; i < 8; i++) book.DropItem(new PowerScroll(SkillName.Mining, 105));
            book.DropItem(new ScrollOfTranscendence(SkillName.Mining, 2.3));
            book.DropItem(new ScrollOfTranscendence(SkillName.Mining, 2.4));
            foreach (ChampionSkullType type in Enum.GetValues(typeof(ChampionSkullType))) book.DropItem(new ChampionSkull(type));
            Check(HavenCodexBrowserGump.Groups(book, 6, "", false).Length == Enum.GetValues(typeof(ChampionSkullType)).Length, "different champion skulls stay in distinct groups");
            Check(HavenCodexBrowserGump.Groups(book, 4, "minING", false).Length == 2, "case insensitive search preserves exact transcendence values");
            Check(HavenCodexBrowserGump.Groups(book, 1, "", false).Single().Count() == 8, "power category excludes other scrolls");
            Check(HavenCodexBrowserGump.Groups(book, 0, "", true).First().Count() == 8, "quantity sort puts largest stack first");
            var menu = new HavenCodexBrowserGump(p, book, 0, 1);
            int count = book.Items.Count;
            Click(menu, p, 100); Check(book.Items.Count == count, "selecting a row never withdraws or converts");
            Check(menu.Entries.OfType<GumpButton>().Any(b => b.ButtonID == 7), "combine available with sufficient scrolls");
            Click(menu, p, 7); Check(book.Items.OfType<PowerScroll>().Single().Value == 110, "explicit combine action consumes correct inputs");
            menu = new HavenCodexBrowserGump(p, book, 0, 1);
            Check(!menu.Entries.OfType<GumpButton>().Any(b => b.ButtonID == 7), "insufficient quantity does not offer combine button");
            Click(menu, p, 8); Check(book.Items.OfType<PowerScroll>().Count() == 8, "explicit split restores eight 105 scrolls");
            menu = new HavenCodexBrowserGump(p, book, 0, 1); Click(menu, p, 10);
            Check(book.Items.OfType<PowerScroll>().Count() == 7 && p.Backpack.Items.OfType<PowerScroll>().Count() == 1, "withdraw moves exactly one item");
            for (int i = 0; i < menu.Entries.Count; i++) {
                var arrow = menu.Entries[i] as GumpButton;
                if (arrow != null && (arrow.ButtonID >= 100 || arrow.ButtonID == 10)) Check(menu.Entries[i + 1] is GumpItemProperty, "browse and withdraw arrows have item properties");
            }
            var table = new HavenCodexGump(p, book, 0, true, "Mining");
            Check(HavenCodexGump.Quantity(HavenCodexGump.Cell(book, SkillName.Mining, 8), 8) == "4.7", "table shows sum of exact transcendence points");
            var detail = new HavenCodexCellGump(p, book, table, SkillName.Mining, 8);
            Check(detail.Entries.OfType<GumpButton>().Count(b => b.ButtonID >= 100) == 2, "detail offers each distinct transcendence value");
            var state = (NetState)FormatterServices.GetUninitializedObject(typeof(NetState)); state.Mobile = p;
            detail.OnResponse(state, new RelayInfo(100, new int[0], new TextRelay[0]));
            Check(p.Backpack.Items.OfType<ScrollOfTranscendence>().Single().Value == 2.3 && HavenCodexGump.Quantity(HavenCodexGump.Cell(book, SkillName.Mining, 8), 8) == "2.4", "table withdrawal preserves exact scroll and remaining points");
            var companion = new HavenCompanion(); var bar = new CompanionCombatBarGump(companion);
            Check(!bar.Closable && bar.Entries.OfType<GumpButton>().Any(b => b.ButtonID == 9), "combat bar prevents right click dismissal and has explicit close");
            companion.Delete();
            p.Delete(); File.AppendAllText("codex-menu-checks.log", "COMPLETE\n"); Core.Kill(false);
        } catch (Exception e) { File.AppendAllText("codex-menu-checks.log", "FAIL " + e + "\n"); Core.Kill(false); }
    }
}
