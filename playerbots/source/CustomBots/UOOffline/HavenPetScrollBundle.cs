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
        if (from.Backpack == null || !IsChildOf(from.Backpack)) { return; }
        from.SendMessage("Choose your living pet within three tiles. This raises a skill cap, not the current skill or abilities.");
        from.Target = new PetTarget(this);
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
        list.Add($"{"Pet skill cap:"} {Skill} {Cap}");
        list.Add($"{"Use on your living pet; does not grant skill points or new abilities."}");
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
        Name = $"Pet training scroll bundle ({cap})"; Hue = 0x489;
        foreach (var skill in Skills) { DropItem(new HavenPetPowerScroll(skill, cap)); }
    }
}

