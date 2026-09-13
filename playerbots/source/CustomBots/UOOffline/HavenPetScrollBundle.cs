using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenPetPowerScroll : Item
{
    [SerializableField(0)] private SkillName _skill;
    [SerializableField(1)] private int _cap;
    [Constructible]
    public HavenPetPowerScroll(SkillName skill = SkillName.Wrestling, int cap = 105) : base(0x14F0)
    {
        Skill = skill; Cap = cap; Hue = 0x489; Weight = 0.1;
        Name = $"Pet {skill} power scroll ({cap})";
    }
    public override void OnDoubleClick(Mobile from)
    {
        Exchange(from);
    }
    internal bool Exchange(Mobile from)
    {
        if (Deleted || from?.Backpack == null || !IsChildOf(from.Backpack) || !from.Alive ||
            Cap is not (105 or 110 or 115 or 120) || (int)Skill < 0 || (int)Skill >= from.Skills.Length) { return false; }
        var scroll = new PowerScroll(Skill, Cap);
        if (!from.Backpack.TryDropItem(from, scroll, false))
        { scroll.Delete(); from.SendMessage("Make room for the matching standard Power Scroll. Your original was kept."); return false; }
        Delete(); from.SendMessage("Exchanged for the same skill and level of standard Power Scroll. Players and pet training can both use it.");
        return true;
    }
    internal bool ApplyTo(Mobile from, BaseCreature pet)
    {
        if (Deleted || from.Backpack == null || !IsChildOf(from.Backpack) || !from.Alive || pet == null || pet.Deleted ||
            !pet.Alive || pet.IsDeadPet || !pet.Controlled || pet.ControlMaster != from || pet.Summoned ||
            pet.Map != from.Map || !from.InRange(pet, 3) || !from.InLOS(pet) || Cap is not (105 or 110) ||
            !System.Array.Exists(HavenPetScrollBundle.Skills, skill => skill == Skill)) { return false; }
        var skill = pet.Skills[Skill];
        if (skill.Cap >= Cap) { from.SendMessage("That pet already has this skill cap or higher. Your scroll was kept."); return false; }
        skill.Cap = Cap;
        from.SendMessage($"{pet.Name}: {Skill} cap is now {Cap}. Train the skill normally.");
        Delete();
        return true;
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Power Scroll:"} {Skill} {Cap}");
        list.Add($"{"Double-click to exchange for the matching standard Power Scroll."}");
    }
    private sealed class PetTarget : Target
    {
        private readonly HavenPetPowerScroll _scroll;
        public PetTarget(HavenPetPowerScroll scroll) : base(3, false, TargetFlags.None) { _scroll = scroll; }
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not BaseCreature pet || !_scroll.ApplyTo(from, pet))
            { from.SendMessage("Use this scroll on your own living pet nearby with a lower skill cap."); }
        }
    }
}

[SerializationGenerator(0)]
public partial class HavenPetScrollBundle : Bag
{
    internal static readonly SkillName[] Skills = [SkillName.Wrestling, SkillName.Tactics, SkillName.Anatomy,
        SkillName.MagicResist, SkillName.Meditation, SkillName.Focus];
    [Constructible]
    public HavenPetScrollBundle(int cap = 105)
    {
        Name = $"Power Scroll training bundle ({cap})"; Hue = 0x489;
        foreach (var skill in Skills) { DropItem(new PowerScroll(skill, cap)); }
    }
}

