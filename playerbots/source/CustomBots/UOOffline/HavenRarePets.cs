using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.UOOffline;

internal static class HavenRarePetAbility
{
    internal static bool Activate(BaseCreature pet, Mobile target, int tier, ref DateTime next)
    {
        if (Core.Now < next || !pet.Controlled || pet.IsDeadPet || pet.ControlMaster?.Alive != true ||
            target is not BaseCreature { Controlled: false, Summoned: false, Alive: true } ||
            target.Map != pet.Map || !pet.InRange(target, 2) || !pet.InLOS(target) || !pet.CanBeHarmful(target, false)) { return false; }
        next = Core.Now + TimeSpan.FromSeconds(12);
        var owner = pet.ControlMaster;
        if (tier == 1) { AOS.Damage(target, pet, 20, 0, 100, 0, 0, 0); }
        else if (tier == 2)
        {
            pet.Hits = Math.Min(pet.HitsMax, pet.Hits + 10);
            if (owner.Map == pet.Map && pet.InRange(owner, 12) && pet.InLOS(owner)) { owner.Heal(15, pet); }
        }
        else if (tier == 4) { AOS.Damage(target, pet, 25, 0, 0, 100, 0, 0); }
        else
        {
            AOS.Damage(target, pet, 30, 0, 0, 0, 0, 100);
            if (owner.Map == pet.Map && pet.InRange(owner, 12) && pet.InLOS(owner)) { owner.Mana = Math.Min(owner.ManaMax, owner.Mana + 8); }
        }
        return true;
    }
    internal static void Skills(BaseCreature pet, double cap)
    {
        foreach (var name in new[] { SkillName.Wrestling, SkillName.Tactics, SkillName.MagicResist })
        { pet.Skills[name].Cap = cap; pet.Skills[name].Base = cap; }
    }
}

[SerializationGenerator(0)]
public partial class HavenEmberwing : ForestOstard
{
    private DateTime _nextAbility;
    [Constructible]
    public HavenEmberwing()
    {
        Name = "an emberwing ostard"; Hue = 0x489; MinTameSkill = 65; ControlSlots = 2;
        SetStr(180); SetDex(120); SetInt(80); SetHits(220); SetDamage(8, 12);
        HavenRarePetAbility.Skills(this, 100);
    }
    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    { base.OnGaveMeleeAttack(defender, damage); HavenRarePetAbility.Activate(this, defender, 1, ref _nextAbility); }
    public override void GetProperties(IPropertyList list)
    { base.GetProperties(list); list.Add($"{"Custom pet:"} {"Ember strike: extra fire damage, 12-second cooldown while tamed"}"); }
}

[SerializationGenerator(0)]
public partial class HavenMoonfang : DireWolf
{
    private DateTime _nextAbility;
    [Constructible]
    public HavenMoonfang()
    {
        Name = "a moonfang wolf"; Hue = 0x47E; MinTameSkill = 95; ControlSlots = 2;
        SetStr(250); SetDex(150); SetInt(150); SetHits(350); SetDamage(10, 16);
        HavenRarePetAbility.Skills(this, 110);
    }
    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    { base.OnGaveMeleeAttack(defender, damage); HavenRarePetAbility.Activate(this, defender, 2, ref _nextAbility); }
    public override void GetProperties(IPropertyList list)
    { base.GetProperties(list); list.Add($"{"Custom pet:"} {"Moon mend: heals itself and nearby owner in combat, 12-second cooldown"}"); }
}

[SerializationGenerator(0)]
public partial class HavenStormscale : Drake
{
    private DateTime _nextAbility;
    [Constructible]
    public HavenStormscale()
    {
        Name = "a stormscale drake"; Hue = 0x482; MinTameSkill = 110; ControlSlots = 3;
        SetStr(450); SetDex(170); SetInt(250); SetHits(550); SetDamage(14, 20);
        HavenRarePetAbility.Skills(this, 120);
    }
    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    { base.OnGaveMeleeAttack(defender, damage); HavenRarePetAbility.Activate(this, defender, 3, ref _nextAbility); }
    public override void GetProperties(IPropertyList list)
    { base.GetProperties(list); list.Add($"{"Custom pet:"} {"Storm strike: energy damage and nearby owner's mana recovery, 12-second cooldown"}"); }
}
