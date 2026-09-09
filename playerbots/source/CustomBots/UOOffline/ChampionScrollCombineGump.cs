using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.UOOffline;

public sealed class ChampionScrollCombineGump : Gump
{
    private readonly ProgressionArchive _codex;
    private readonly int _page;
    private readonly Dictionary<int, (SkillName Skill, int Tier)> _choices = new();
    internal static int Cost(int tier) => tier switch { 105 => 8, 110 => 12, 115 => 10, _ => 0 };

    internal static bool Combine(Mobile from, ProgressionArchive codex, SkillName skill, int tier)
    {
        var cost = Cost(tier);
        if (cost == 0 || !ChampionCodexGump.CanUse(from, codex)) { return false; }
        var inputs = new List<PowerScroll>();
        foreach (var item in codex.Items)
        {
            if (item is PowerScroll scroll && !scroll.Deleted && scroll.Skill == skill && scroll.Value == tier)
            {
                inputs.Add(scroll);
                if (inputs.Count == cost) { break; }
            }
        }
        if (inputs.Count < cost)
        {
            from.SendMessage($"You need {cost} matching {tier} scrolls in this codex.");
            return false;
        }
        // The replacement occupies less space than the consumed scrolls; keep it in the codex.
        var output = new PowerScroll(skill, tier + 5);
        foreach (var input in inputs) { input.Delete(); }
        codex.DropItem(output);
        from.SendMessage($"Combined {cost} scrolls into one {tier + 5} power scroll.");
        return true;
    }

    public static void DisplayTo(Mobile from, ProgressionArchive codex, int page = 0)
    {
        if (!ChampionCodexGump.CanUse(from, codex)) { return; }
        from.CloseGump<ChampionScrollCombineGump>();
        from.SendGump(new ChampionScrollCombineGump(codex, page));
    }

    private ChampionScrollCombineGump(ProgressionArchive codex, int page) : base(30, 30)
    {
        _codex = codex;
        var groups = new Dictionary<(SkillName Skill, int Tier), int>();
        foreach (var item in codex.Items)
        {
            if (item is PowerScroll scroll && !scroll.Deleted && Cost((int)scroll.Value) > 0 && scroll.Value == (int)scroll.Value)
            {
                var key = (scroll.Skill, (int)scroll.Value);
                groups.TryGetValue(key, out var count);
                groups[key] = count + 1;
            }
        }
        var keys = new List<(SkillName Skill, int Tier)>(groups.Keys);
        keys.Sort((a, b) => a.Skill == b.Skill ? a.Tier.CompareTo(b.Tier) : string.CompareOrdinal(SkillInfo.Table[(int)a.Skill].Name, SkillInfo.Table[(int)b.Skill].Name));
        var pages = System.Math.Max(1, (keys.Count + 11) / 12);
        _page = System.Math.Clamp(page, 0, pages - 1);
        AddBackground(0, 0, 600, 460, 0xA28);
        AddLabel(25, 20, 0, "CHAMPION'S CODEX - COMBINE");
        AddLabel(25, 48, 0, "8 x 105 = 110     12 x 110 = 115     10 x 115 = 120");
        AddLabel(25, 72, 0, "Same skill only. Combining consumes the listed scrolls.");
        for (var row = 0; row < 12 && _page * 12 + row < keys.Count; row++)
        {
            var key = keys[_page * 12 + row];
            var y = 105 + row * 25;
            AddLabel(25, y, 0, $"{SkillInfo.Table[(int)key.Skill].Name}: {key.Tier} -> {key.Tier + 5}");
            AddLabel(320, y, 0, $"{groups[key]} / {Cost(key.Tier)}");
            if (groups[key] >= Cost(key.Tier))
            {
                _choices[100 + row] = key;
                AddButton(430, y, 4005, 4007, 100 + row);
                AddLabel(465, y, 0, "Combine");
            }
        }
        if (keys.Count == 0) { AddLabel(25, 110, 0, "Store 105, 110 or 115 power scrolls in the codex first."); }
        AddButton(25, 420, 4005, 4007, 1); AddLabel(60, 420, 0, "Back");
        if (_page > 0) { AddButton(180, 420, 4014, 4016, 2); }
        AddLabel(240, 420, 0, $"Page {_page + 1} / {pages}");
        if (_page + 1 < pages) { AddButton(380, 420, 4005, 4007, 3); }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (info.ButtonID == 0 || !ChampionCodexGump.CanUse(from, _codex)) { return; }
        if (info.ButtonID == 1) { ChampionCodexGump.DisplayTo(from, _codex); return; }
        if (_choices.TryGetValue(info.ButtonID, out var key)) { Combine(from, _codex, key.Skill, key.Tier); }
        DisplayTo(from, _codex, _page + (info.ButtonID == 2 ? -1 : info.ButtonID == 3 ? 1 : 0));
    }
}
