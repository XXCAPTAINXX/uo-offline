using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Server.Gumps;
using Server.Network;

namespace Server.UOOffline;

public sealed class HavenMissionReportGump : Gump
{
    private readonly HavenMissionJournal _journal;
    private readonly int _tab, _page, _history;
    private readonly int _pages;
    private readonly HavenMissionReport _report;
    private const int Ink = 0;

    public HavenMissionReportGump(HavenMissionJournal journal, int tab = 0, int page = 0, int history = 0) : base(45, 45)
    {
        _journal = journal; _tab = Math.Clamp(tab, 0, 3);
        var reports = journal.Reports;
        AddBackground(0, 0, 680, 540, 5054);
        AddBackground(12, 12, 656, 516, 3000);
        Text(28, 23, 620, 27, $"{journal.Companion?.Name ?? "Companion"} - Adventure Report", true);
        if (reports.Count == 0)
        {
            Text(28, 90, 620, 65, "No adventures recorded yet. Send your companion on a mission to begin a report.");
            Button(544, 496, 0, "Close"); return;
        }
        _history = Math.Clamp(history, 0, reports.Count - 1); _report = reports[_history];
        Text(28, 53, 620, 22, $"{_report.Started.ToLocalTime():MMM d, yyyy  h:mm tt}  |  {_report.Outcome}");
        var tabs = new[] { "Overview", "Loot", "Skills", "Gear" };
        for (var i = 0; i < tabs.Length; i++)
        {
            if (i == _tab) { AddBackground(25 + i * 157, 82, 148, 32, 3000); }
            AddButton(32 + i * 157, 88, 4005, 4007, 10 + i);
            Text(68 + i * 157, 89, 100, 22, tabs[i], i == _tab);
        }
        var count = _tab switch
        {
            1 => _report.Loot.Count,
            2 => ChangedSkills().Count,
            3 => _report.After.Gear.Count,
            _ => 1
        };
        var perPage = _tab == 1 ? 6 : _tab == 2 ? 10 : _tab == 3 ? 4 : 1;
        _pages = Math.Max(1, (count + perPage - 1) / perPage);
        _page = Math.Clamp(page, 0, _pages - 1);
        if (_tab == 0) { Overview(); }
        else if (count == 0) { Text(30, 148, 610, 60, "Nothing recorded in this category yet."); }
        else if (_tab == 1) { Loot(); }
        else if (_tab == 2) { Skills(); }
        else { Gear(); }
        if (_page > 0) { Button(28, 463, 1, "Previous page", true); }
        if (_page + 1 < _pages) { Button(520, 463, 2, "Next page"); }
        Text(265, 465, 210, 22, $"Page {_page + 1} of {_pages}");
        if (_history + 1 < reports.Count) { Button(28, 499, 3, "Older report", true); }
        if (_history > 0) { Button(211, 499, 4, "Newer report"); }
        Text(379, 501, 150, 20, $"Report {_history + 1} of {reports.Count}");
        Button(544, 499, 0, "Close");
    }

    private void Text(int x, int y, int width, int height, string value, bool bold = false)
    {
        var safe = WebUtility.HtmlEncode(value);
        AddHtml(x, y, width, height, $"<BASEFONT COLOR=#181818>{(bold ? "<B>" : "")}{safe}{(bold ? "</B>" : "")}</BASEFONT>", false, false);
    }
    private void Button(int x, int y, int id, string label, bool previous = false)
    {
        AddButton(x, y, id == 0 ? 4017 : previous ? 4014 : 4005, id == 0 ? 4019 : previous ? 4016 : 4007, id);
        AddLabel(x + 35, y + 1, Ink, label);
    }
    private void Metric(int x, string label, string value)
    {
        AddBackground(x, 125, 148, 59, 3000);
        Text(x + 10, 134, 130, 20, label); Text(x + 10, 157, 130, 24, value, true);
    }
    private void Heading(int y, string first)
    {
        AddBackground(28, y - 5, 624, 29, 3000);
        Text(40, y, 230, 22, first, true); Text(292, y, 90, 22, "Before", true);
        Text(414, y, 90, 22, "After", true); Text(544, y, 92, 22, "Change", true);
    }
    private void StatRow(int y, string label, double before, double after, bool skill = false)
    {
        var format = skill ? "N1" : "N0";
        Text(40, y, 240, 23, label); Text(292, y, 108, 23, before.ToString(format));
        Text(414, y, 108, 23, after.ToString(format), true);
        var change = after - before;
        var delta = change == 0 ? "-" : (change > 0 ? "+" : "") + change.ToString(format);
        AddHtml(544, y, 99, 23, $"<BASEFONT COLOR={(change > 0 ? "#23551C" : "#181818")}><B>{WebUtility.HtmlEncode(delta)}</B></BASEFONT>", false, false);
    }
    private void Overview()
    {
        var r = _report;
        Metric(28, "Missions", r.Runs.ToString("N0"));
        Metric(186, "Mission time", $"{r.Minutes:N0} min");
        Metric(344, "Skills improved", ChangedSkills().Count(s => s.Value > r.Before.Skills.GetValueOrDefault(s.Key)).ToString("N0"));
        Metric(502, "Gear XP offered", (r.TrainingMinutes * 10L).ToString("N0"));
        Heading(206, "Companion progress");
        StatRow(240, "Level", r.Before.Level, r.After.Level);
        StatRow(272, "Strength", r.Before.Str, r.After.Str);
        StatRow(304, "Dexterity", r.Before.Dex, r.After.Dex);
        StatRow(336, "Intelligence", r.Before.Int, r.After.Int);
        var notes = new List<string>();
        if (r.Missions.Count > 0) { notes.Add("Missions: " + string.Join("; ", r.Missions.Select(m => $"{m.Key}: {m.Value:N0}"))); }
        notes.Add($"Early returns: {r.EarlyReturns:N0}. Gear XP is subject to each item's cap.");
        notes.Add("Progress includes natural training while away. Loot and Gear show the recorded details.");
        if (r.EarlierRuns > 0) { notes.Add($"{r.EarlierRuns} earlier runs predate this journal; their loot was not recorded."); }
        AddHtml(40, 378, 600, 70, "<BASEFONT COLOR=#181818>" + string.Join("<BR>", notes.Select(WebUtility.HtmlEncode)) + "</BASEFONT>", false, true);
    }
    private List<KeyValuePair<string, double>> ChangedSkills() => _report.After.Skills
        .Where(s => Math.Abs(s.Value - _report.Before.Skills.GetValueOrDefault(s.Key)) > .001).OrderBy(s => s.Key).ToList();
    private void Skills()
    {
        Heading(136, "Skill"); var rows = ChangedSkills();
        for (var row = 0; row < 10 && _page * 10 + row < rows.Count; row++)
        {
            var s = rows[_page * 10 + row];
            StatRow(173 + row * 27, s.Key, _report.Before.Skills.GetValueOrDefault(s.Key), s.Value, true);
        }
    }
    private void Loot()
    {
        Text(40, 131, 600, 22, "Earned items and where they were delivered", true);
        for (var row = 0; row < 6 && _page * 6 + row < _report.Loot.Count; row++)
        {
            var index = _page * 6 + row; var item = _report.Loot[index]; var y = 161 + row * 47;
            Text(40, y, 377, 23, item.Name, true); Text(425, y, 100, 23, $"x{item.Amount:N0}", true);
            Text(40, y + 23, 478, 22, $"Stored in: {item.Destination}");
            if (!string.IsNullOrEmpty(item.Stats)) { Button(540, y + 2, 1000 + index, "Stats"); }
        }
    }
    private void Gear()
    {
        Text(40, 131, 600, 22, "Equipment progress - inspect before and after bonuses", true);
        var rows = _report.After.Gear.ToList();
        for (var row = 0; row < 4 && _page * 4 + row < rows.Count; row++)
        {
            var index = _page * 4 + row; var entry = rows[index]; var after = entry.Value;
            var before = _report.Before.Gear.GetValueOrDefault(entry.Key); var y = 165 + row * 71;
            Text(40, y, 475, 23, after.Name, true); Button(540, y + 2, 2000 + index, "Stats");
            Text(40, y + 24, 475, 23, before == null ? $"Newly equipped | Level {after.Level:N0}" : $"Level {before.Level:N0} to {after.Level:N0} | XP {before.Experience:N0} to {after.Experience:N0}");
            if (before != null) { Text(40, y + 46, 475, 22, $"XP change: {after.Experience - before.Experience:+#,0;-#,0;0}"); }
        }
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0 || !_journal.MayShow(sender.Mobile)) { return; }
        var tab = info.ButtonID is >= 10 and <= 13 ? info.ButtonID - 10 : _tab;
        var page = info.ButtonID == 1 ? _page - 1 : info.ButtonID == 2 ? _page + 1 : info.ButtonID >= 1000 ? _page : 0;
        var history = info.ButtonID == 3 ? _history + 1 : info.ButtonID == 4 ? _history - 1 : _history;
        sender.Mobile.SendGump(new HavenMissionReportGump(_journal, tab, page, history));
        if (_report == null) { return; }
        if (_tab == 1 && info.ButtonID >= 1000 && info.ButtonID - 1000 < _report.Loot.Count)
        {
            var item = _report.Loot[info.ButtonID - 1000];
            sender.Mobile.CloseGump<HavenMissionItemReportGump>();
            sender.Mobile.SendGump(new HavenMissionItemReportGump(item.Name, $"Quantity: {item.Amount:N0}\nStored in: {item.Destination}", null, item.Stats));
        }
        else if (_tab == 3 && info.ButtonID >= 2000 && info.ButtonID - 2000 < _report.After.Gear.Count)
        {
            var entry = _report.After.Gear.ElementAt(info.ButtonID - 2000); var before = _report.Before.Gear.GetValueOrDefault(entry.Key);
            sender.Mobile.CloseGump<HavenMissionItemReportGump>();
            sender.Mobile.SendGump(new HavenMissionItemReportGump(entry.Value.Name, "Recorded equipment bonuses", before?.Stats ?? "Not equipped at the start", entry.Value.Stats));
        }
    }
}

public sealed class HavenMissionItemReportGump : Gump
{
    public HavenMissionItemReportGump(string name, string description, string before, string after) : base(85, 85)
    {
        AddBackground(0, 0, 600, 430, 5054); AddBackground(12, 12, 576, 406, 3000);
        AddHtml(28, 25, 540, 45, Ink(name, true), false, false);
        AddHtml(28, 73, 540, 45, Ink(description), false, false);
        if (before != null)
        {
            AddHtml(28, 128, 265, 23, Ink("Before", true), false, false);
            AddHtml(313, 128, 265, 23, Ink("After", true), false, false);
            AddHtml(28, 159, 259, 215, Ink(before.Replace("; ", "\n")), false, true);
            AddHtml(313, 159, 259, 215, Ink(after.Replace("; ", "\n")), false, true);
        }
        else { AddHtml(28, 128, 544, 246, Ink(after.Replace("; ", "\n")), false, true); }
        AddButton(445, 389, 4017, 4019, 0); AddLabel(480, 390, 0, "Back to report");
    }
    private static string Ink(string text, bool bold = false) => "<BASEFONT COLOR=#181818>" + (bold ? "<B>" : "") +
        WebUtility.HtmlEncode(text).Replace("\n", "<BR>") + (bold ? "</B>" : "") + "</BASEFONT>";
}
