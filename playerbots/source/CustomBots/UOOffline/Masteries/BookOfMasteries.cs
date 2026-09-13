using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Spells.SkillMasteries;
namespace Server.Items;
[SerializationGenerator(0)]
public partial class BookOfMasteries : Spellbook
{
    public override SpellbookType SpellbookType => SpellbookType.SkillMasteries;
    public override int BookOffset => 700;
    public override int BookCount => 45;
    public override string DefaultName => "Book of Masteries";
    [Constructible]
    public BookOfMasteries() : base((1ul << 45) - 1, 0x225A) { LootType = LootType.Blessed; }
    public override void OnDoubleClick(Mobile from)
    {
        if (Accessible(from)) { MasterySelectionGump.DisplayTo(from, this); }
    }
    public bool Accessible(Mobile from) => !Deleted && from?.Deleted == false && from.Alive &&
        (Parent == from || from.Backpack != null && IsChildOf(from.Backpack));
    public static bool CheckCooldown(Mobile from) => (MasteryProgress.Find(from)?.NextSwitch ?? DateTime.MinValue) <= Core.Now;
    public static void AddToCooldown(Mobile from) => MasteryProgress.Get(from).NextSwitch = Core.Now + TimeSpan.FromMinutes(10);
    public static bool Select(Mobile from, SkillName skill)
    {
        if (from?.Deleted != false || !from.Alive || !MasteryInfo.HasLearned(from, skill) || from.Skills[skill].Base < 90) { return false; }
        var old = MasteryProgress.Current(from);
        if (old == skill) { return true; }
        if (!CheckCooldown(from)) { from.SendMessage("You must wait ten minutes between changing masteries."); return false; }
        MasteryProgress.Get(from).Selected = skill;
        MasteryInfo.OnMasteryChanged(from, old);
        AddToCooldown(from);
        from.SendMessage($"Active mastery: {from.Skills[skill].Name}, volume {MasteryInfo.GetMasteryLevel(from, skill)}.");
        return true;
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Double-click to select a mastery and use its abilities. Learn volumes I, II or III from primers."}");
    }
}
