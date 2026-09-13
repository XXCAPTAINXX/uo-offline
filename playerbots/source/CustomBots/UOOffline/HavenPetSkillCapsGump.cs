using System;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

public sealed class HavenPetSkillCapsGump : Gump
{
    private readonly BaseCreature _pet;
    private readonly bool _magic;
    private readonly int _page;
    internal static readonly SkillName[] Combat = [SkillName.Wrestling, SkillName.Tactics, SkillName.MagicResist,
        SkillName.Anatomy, SkillName.Healing, SkillName.Poisoning, SkillName.Parry, SkillName.Hiding, SkillName.DetectHidden];
    internal static readonly SkillName[] Magic = [SkillName.Magery, SkillName.EvalInt, SkillName.Meditation, SkillName.Focus,
        SkillName.Necromancy, SkillName.SpiritSpeak, SkillName.Spellweaving, SkillName.Mysticism, SkillName.Chivalry,
        SkillName.Bushido, SkillName.Ninjitsu, SkillName.Musicianship, SkillName.Discordance, SkillName.Peacemaking, SkillName.Provocation];

    internal static Item FindScroll(Mobile owner, SkillName skill, int cap)
    {
        if (owner.Backpack == null || cap is not (105 or 110 or 115 or 120)) { return null; }
        foreach (var item in owner.Backpack.FindItemsByType<Item>())
        {
            if (!item.Deleted && (item is PowerScroll p && p.Skill == skill && p.Value == cap ||
                item is HavenPetPowerScroll petScroll && petScroll.Skill == skill && petScroll.Cap == cap)) { return item; }
        }
        return null;
    }
    internal static bool Purchase(Mobile owner, BaseCreature pet, SkillName skill, int cap)
    {
        if (!HavenPetTraining.Owned(owner, pet)) { return false; }
        var scroll = FindScroll(owner, skill, cap);
        if (scroll == null) { owner.SendMessage($"You need a matching {cap} {SkillInfo.Table[(int)skill].Name} power scroll in your backpack or codex."); return false; }
        var record = HavenPetTraining.Find(pet);
        return record != null && record.RaiseCap(owner, pet, skill, scroll);
    }
    public static void DisplayTo(Mobile owner, BaseCreature pet, bool magic, int page = 0)
    {
        if (!HavenPetTraining.Owned(owner, pet)) { return; }
        owner.CloseGump<HavenPetSkillCapsGump>();
        owner.SendGump(new HavenPetSkillCapsGump(owner, pet, magic, page));
    }
    internal HavenPetSkillCapsGump(Mobile owner, BaseCreature pet, bool magic, int page) : base(25, 30)
    {
        _pet = pet; _magic = magic;
        var skills = magic ? Magic : Combat;
        var pages = (skills.Length + 5) / 6;
        _page = Math.Clamp(page, 0, pages - 1);
        AddBackground(0, 0, 720, 460, 5054); AddImageTiled(12, 12, 696, 436, 2624);
        AddLabel(25, 20, 53, magic ? "MAGIC SKILL CAPS" : "COMBAT SKILL CAPS");
        AddLabel(25, 48, 1152, "Choose a cap. Consumes one scroll of that exact skill and level.");
        AddLabel(25, 73, 1152, "Raises the cap only; the pet still gains skill through use.");
        var record = HavenPetTraining.Get(pet);
        AddLabel(25, 98, 53, $"Training {record.Progress / 100.0:F1}%    Points {record.Points:F1}");
        for (var row = 0; row < 6 && _page * 6 + row < skills.Length; row++)
        {
            var skill = skills[_page * 6 + row]; var y = 140 + row * 42;
            AddLabel(25, y, 1152, $"{pet.Skills[skill].Name} {pet.Skills[skill].Base:F1}/{pet.Skills[skill].Cap:F1}");
            for (var col = 0; col < 4; col++)
            {
                var cap = 105 + col * 5; var x = 310 + col * 95;
                var unlocked = pet.Skills[skill].Cap >= cap;
                AddLabel(x + 30, y, unlocked ? 53 : 1152, $"{cap}");
                if (!unlocked)
                {
                    AddButton(x, y, 4005, 4007, 100 + row * 4 + col);
                    var weight = skill is SkillName.Wrestling or SkillName.Tactics or SkillName.EvalInt ? 10 : skill == SkillName.Magery ? 5 : 1;
                    var points = Math.Ceiling((cap - pet.Skills[skill].Cap) * 10 * weight) / 10;
                    var availability = FindScroll(owner, skill, cap) == null ? "missing" : "available";
                    AddTooltip(1042971, $"Costs {points:F1} training points and one {cap} scroll. Matching scroll: {availability}.");
                }
            }
        }
        AddLabel(25, 395, 1152, "Gold caps are already unlocked. Hover on arrows to check your scrolls.");
        AddButton(25, 424, 4005, 4007, 1); AddLabel(60, 424, 1152, "Back");
        if (_page > 0) { AddButton(210, 424, 4014, 4016, 2); }
        AddLabel(285, 424, 1152, $"Page {_page + 1}/{pages}");
        if (_page + 1 < pages) { AddButton(430, 424, 4005, 4007, 3); }
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var owner = sender.Mobile;
        if (info.ButtonID == 0 || !HavenPetTraining.Owned(owner, _pet)) { return; }
        if (info.ButtonID == 1) { HavenPetTrainingGump.DisplayTo(owner, _pet); return; }
        var skills = _magic ? Magic : Combat;
        if (info.ButtonID is >= 100 and < 124)
        {
            var index = _page * 6 + (info.ButtonID - 100) / 4;
            if (index < skills.Length && !Purchase(owner, _pet, skills[index], 105 + (info.ButtonID - 100) % 4 * 5))
            { owner.SendMessage("Upgrade not applied. Complete combat training and check points, current cap, scroll and follower space."); }
        }
        DisplayTo(owner, _pet, _magic, _page + (info.ButtonID == 2 ? -1 : info.ButtonID == 3 ? 1 : 0));
    }
}
