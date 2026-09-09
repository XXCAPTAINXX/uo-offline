using System;
using System.Text.RegularExpressions;
using Server.Items;
using Server.Network;
using Server.Spells;
using Server.Spells.SkillMasteries;
namespace Server.Gumps;
public sealed class MasterySelectionGump : Gump
{
    private readonly BookOfMasteries _book;
    public static void DisplayTo(Mobile owner, BookOfMasteries book)
    {
        if (!book.Accessible(owner)) { return; }
        owner.CloseGump<MasterySelectionGump>(); owner.SendGump(new MasterySelectionGump(owner, book));
    }
    public MasterySelectionGump(Mobile owner, BookOfMasteries book) : base(30, 30)
    {
        _book = book;
        AddBackground(0, 0, 630, 545, 5054); AddBackground(12, 12, 606, 521, 3000);
        AddLabel(24, 22, 0, "Book of Masteries");
        var selected = MasteryProgress.Current(owner);
        AddLabel(24, 48, 0, $"Active: {(selected == SkillName.Alchemy ? "None" : owner.Skills[selected].Name)}");
        AddLabel(24, 80, 0, "Select mastery / learned volume");
        for(var i = 0; i < MasteryInfo.Skills.Length; i++)
        {
            var skill = MasteryInfo.Skills[i]; var level = MasteryInfo.GetMasteryLevel(owner, skill);
            AddButton(24, 108 + i * 20, 4005, 4007, i + 1);
            AddLabel(58, 110 + i * 20, level > 0 ? 0 : 0x3B2, $"{owner.Skills[skill].Name} - {(level > 0 ? level.ToString() : "Unlearned")}");
        }
        var row = 0;
        foreach (var info in MasteryInfo.Infos)
        {
            if(info.MasterySkill != selected) { continue; }
            var label = info.SpellType == null ? info.PassiveSpell.ToString() : info.SpellType.Name.Replace("Spell", "");
            label = Regex.Replace(label, "([a-z])([A-Z])", "$1 $2");
            if(info.SpellType != null) { AddButton(320, 108 + row * 75, 4005, 4007, 1000 + info.SpellID); }
            AddHtml(355, 110 + row * 75, 245, 60, label + (info.Passive ? " (passive)" : "")); row++;
        }
        AddHtml(320, 355, 275, 125, "Primers unlock proficiency. Abilities require 90 skill and the matching active mastery. Switching has a ten-minute cooldown.<BR><BR>Haven rules: Conduit spreads necromancy damage; Reaper uses mage attacks. Some effects differ from later-era shards.");
        AddButton(480, 502, 4005, 4007, 0); AddLabel(515, 504, 0, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var owner = state.Mobile;
        if (info.ButtonID == 0 || !_book.Accessible(owner)) { return; }
        if (info.ButtonID > 0 && info.ButtonID <= MasteryInfo.Skills.Length)
        { BookOfMasteries.Select(owner, MasteryInfo.Skills[info.ButtonID - 1]); }
        else if (info.ButtonID >= 1700 && info.ButtonID <= 1744)
        { var id = info.ButtonID - 1000; var move = SpellRegistry.GetSpecialMove(id); if (move != null) { SpecialMove.SetCurrentMove(owner, move); } else { SpellRegistry.NewSpell(id, owner, null)?.Cast(); } }
        DisplayTo(owner, _book);
    }
}
