using System;
using ModernUO.Serialization;
using Server.Spells.SkillMasteries;
namespace Server.Items;
[SerializationGenerator(0)]
public partial class SkillMasteryPrimer : Item
{
    [SerializableField(0)] private SkillName _skill;
    [SerializableField(1)] private int _volume;
    public override string DefaultName => $"{Skill} Mastery Primer - Volume {Volume}";
    [Constructible]
    public SkillMasteryPrimer(SkillName skill = SkillName.Magery, int volume = 1) : base(0x1F4D)
    { Skill = skill; Volume = Math.Clamp(volume, 1, 3); Weight = 1; Hue = 0x489; }
    public override void OnDoubleClick(Mobile from) => Learn(from);
    internal bool Learn(Mobile from)
    {
        if (Deleted || from?.Deleted != false || !from.Alive || from.Backpack == null || !IsChildOf(from.Backpack)) { return false; }
        if (from.Skills[Skill].Base < 90) { from.SendMessage("You need 90 base skill to learn this mastery."); return false; }
        if (!MasteryInfo.LearnMastery(from, Skill, Volume)) { from.SendMessage("You already know this mastery volume or higher."); return false; }
        from.SendMessage($"Learned {Skill} mastery volume {Volume}. Open your Book of Masteries to select it.");
        Delete(); return true;
    }
    public static SkillMasteryPrimer GetRandom() => new(MasteryInfo.Skills[Utility.Random(MasteryInfo.Skills.Length)], Utility.RandomMinMax(1, 3));
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Requires 90 base skill. Higher volumes can be learned directly; earlier volumes are not required."}");
    }
}
