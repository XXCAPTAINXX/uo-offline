using System;
using ModernUO.Serialization;
using Server.Engines.BuffIcons;
using Server.Mobiles;

namespace Server.UOOffline;

// Haven's adaptation: existing Ancient Hell Hound art and native pet healing.
// No claim is made that these rolls reproduce another shard's private formulas.
[SerializationGenerator(0)]
public partial class HavenAncientHellhound : HellHound
{
    [Constructible]
    public HavenAncientHellhound()
    {
        Name = "an ancient hellhound"; Body = 1069; Hue = 0;
        SetStr(450, 550); SetDex(180, 210); SetInt(180, 220); SetHits(500, 650);
        SetDamage(17, 23);
        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 70, 75);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 45, 55);
        HavenRarePetAbility.Skills(this, 110);
        Skills.Healing.Cap = 120; Skills.Healing.Base = 110;
        Skills.Anatomy.Cap = 120; Skills.Anatomy.Base = 100;
        ControlSlots = 3; MinTameSkill = 110; Tamable = true;
    }
    public override bool CanHeal => Controlled;
    public override bool CanHealOwner => Controlled;
    public override double HealDelay => 2;
    public override double HealOwnerDelay => 2;
    public override double HealInterval => 8;
    public override double HealOwnerInterval => 8;
    public override int HealStartRange => 12;
    public override int HealEndRange => 12;
    public override double HealOwnerTrigger => 1;
    public override bool StatLossAfterTame => false;
    public override void HealStart(Mobile patient)
    {
        if (!Controlled || patient?.Deleted != false || patient != this && patient != ControlMaster || !InLOS(patient)) { return; }
        base.HealStart(patient);
        if (patient is PlayerMobile player)
        {
            player.AddBuff(new BuffInfo(BuffIcon.Healing, 1042971, 1042971, TimeSpan.FromSeconds(2), "Your ancient hellhound is healing you."));
        }
    }
    public override void Heal(Mobile patient)
    {
        if (!Controlled || patient?.Deleted != false || patient != this && patient != ControlMaster || !InLOS(patient)) { StopHeal(); return; }
        base.Heal(patient);
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Abyss companion:"} {"Innate Healing, fire breath; no stat loss on taming"}");
        list.Add($"{"Healing:"} {"Self and owner, 2 seconds, range 12, 8-second interval"}");
        list.Add($"{"Dexterity and stamina:"} {RawDex} {"/"} {StamMax}");
    }
}
