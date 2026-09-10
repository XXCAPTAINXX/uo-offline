using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Server.Items;
using Server.Network;
using Server.Spells;
using Server.Spells.SkillMasteries;
namespace Server.Gumps;
public sealed class MasterySelectionGump : Gump
{
    private readonly BookOfMasteries _book;
    private readonly int _page;
    internal static SkillName[] Learned(Mobile owner) => MasteryInfo.Skills.Where(s => MasteryInfo.GetMasteryLevel(owner, s) > 0).ToArray();
    public static void DisplayTo(Mobile owner, BookOfMasteries book, int page = 0)
    {
        if (!book.Accessible(owner)) { return; }
        owner.CloseGump<MasterySelectionGump>(); owner.SendGump(new MasterySelectionGump(owner, book, page));
    }
    public MasterySelectionGump(Mobile owner, BookOfMasteries book, int page = 0) : base(30, 30)
    {
        _book = book;
        AddBackground(0, 0, 650, 480, 5054); AddBackground(12, 12, 626, 456, 3000);
        Text(24, 22, 600, 24, "Book of Masteries", true);
        var selected = MasteryProgress.Current(owner);
        Text(24, 49, 600, 24, $"Active: {(selected == SkillName.Alchemy ? "None" : owner.Skills[selected].Name)}");
        Text(24, 85, 280, 25, "Learned masteries", true); Text(336, 85, 280, 25, "Active abilities", true);
        var learned = Learned(owner); var pages = Math.Max(1, (learned.Length + 7) / 8);
        _page = Math.Clamp(page, 0, pages - 1);
        for (var row = 0; row < 8 && _page * 8 + row < learned.Length; row++)
        {
            var skill = learned[_page * 8 + row]; var level = MasteryInfo.GetMasteryLevel(owner, skill);
            AddButton(24, 116 + row * 35, 4005, 4007, Array.IndexOf(MasteryInfo.Skills, skill) + 1);
            Text(60, 118 + row * 35, 252, 27, $"{owner.Skills[skill].Name} - Volume {level}", skill == selected);
        }
        if (learned.Length == 0) { Text(24, 123, 278, 100, "No masteries learned yet. Use a mastery primer to learn your first volume. Primers are sold at Training Supplies."); }
        var abilityRow = 0;
        foreach (var info in MasteryInfo.Infos)
        {
            if (info.MasterySkill != selected || !learned.Contains(selected)) { continue; }
            var label = info.SpellType == null ? info.PassiveSpell.ToString() : info.SpellType.Name.Replace("Spell", "");
            label = Regex.Replace(label, "([a-z])([A-Z])", "$1 $2");
            if (info.SpellType != null) { AddButton(336, 116 + abilityRow * 57, 4005, 4007, 1000 + info.SpellID); }
            Text(374, 118 + abilityRow * 57, 235, 49, label + (info.Passive ? " (passive)" : "")); abilityRow++;
        }
        if (abilityRow == 0) { Text(336, 123, 278, 70, "Select one of your learned masteries to see its abilities."); }
        Text(336, 302, 278, 108, "Abilities require 90 skill and the matching active mastery. Switching masteries has a ten-minute cooldown.\n\nOnly learned volumes appear here. Use a higher-volume primer to improve one.");
        if (_page > 0) { AddButton(24, 403, 4014, 4016, 3001); Text(59, 405, 95, 22, "Previous"); }
        if (_page + 1 < pages) { AddButton(188, 403, 4005, 4007, 3002); Text(223, 405, 80, 22, "Next"); }
        Text(24, 443, 280, 22, $"Learned: {learned.Length} | Page {_page + 1} of {pages}");
        AddButton(525, 438, 4017, 4019, 0); Text(560, 440, 65, 22, "Close");
    }
    private void Text(int x, int y, int w, int h, string text, bool bold = false) => AddHtml(x, y, w, h,
        "<BASEFONT COLOR=#181818>" + (bold ? "<B>" : "") + WebUtility.HtmlEncode(text).Replace("\n", "<BR>") + (bold ? "</B>" : "") + "</BASEFONT>", false, false);
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var owner = state.Mobile;
        if (info.ButtonID == 0 || !_book.Accessible(owner)) { return; }
        var page = _page;
        if (info.ButtonID == 3001) { page--; }
        else if (info.ButtonID == 3002) { page++; }
        else if (info.ButtonID > 0 && info.ButtonID <= MasteryInfo.Skills.Length)
        {
            if (!BookOfMasteries.Select(owner, MasteryInfo.Skills[info.ButtonID - 1])) { owner.SendMessage("You need its primer, 90 base skill, and an available mastery switch."); }
        }
        else if (info.ButtonID >= 1700 && info.ButtonID <= 1744)
        { var id = info.ButtonID - 1000; var move = SpellRegistry.GetSpecialMove(id); if (move != null) { SpecialMove.SetCurrentMove(owner, move); } else { SpellRegistry.NewSpell(id, owner, null)?.Cast(); } }
        DisplayTo(owner, _book, page);
    }
}
