using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenSnowBear : BaseMount
{
    private DateTime _rageUntil, _nextRage;
    public const string RageDescription = "Colossal Rage: below half health, melee damage +50% for 10 seconds; 30-second cooldown. Innate, no training slot.";
    [Constructible]
    public HavenSnowBear() : base(0xD5, 0x3EC5, AIType.AI_Melee, FightMode.Aggressor)
    {
        Name = "a frostbound bear"; BaseSoundID = 0xA3;
        SetStr(450, 550); SetDex(180, 210); SetInt(100, 150); SetHits(600, 750);
        SetDamage(17, 23); SetDamageType(ResistanceType.Physical, 50); SetDamageType(ResistanceType.Cold, 50);
        SetResistance(ResistanceType.Physical, 50, 60); SetResistance(ResistanceType.Fire, 25, 35);
        SetResistance(ResistanceType.Cold, 65, 75); SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 45, 55);
        HavenRarePetAbility.Skills(this, 105);
        Skills.Anatomy.Base = 90;
        Tamable = true; MinTameSkill = 100; ControlSlots = 3; Fame = 8000; Karma = 0;
    }
    public override string CorpseName => "a frostbound bear corpse";
    public override FoodType FavoriteFood => FoodType.Meat | FoodType.Fish | FoodType.FruitsAndVeggies;
    public override int Meat => 4;
    public override int Hides => 15;
    public override bool StatLossAfterTame => false;
    internal bool Raging => Core.Now < _rageUntil;
    internal bool TryRage()
    {
        if (Deleted || !Alive || IsDeadPet || Rider != null || Hits <= 0 || Hits * 2 >= HitsMax || Core.Now < _nextRage) { return false; }
        _rageUntil = Core.Now + TimeSpan.FromSeconds(10); _nextRage = Core.Now + TimeSpan.FromSeconds(30);
        PlaySound(0xA6); FixedParticles(0x375A, 10, 15, 5017, EffectLayer.Waist);
        Emote("*roars with colossal rage*"); return true;
    }
    public override void AlterMeleeDamageTo(Mobile to, ref int damage)
    {
        base.AlterMeleeDamageTo(to, ref damage);
        TryRage();
        if (Raging) { damage += damage / 2; }
    }
    public override void AlterMeleeDamageFrom(Mobile from,ref int damage)
    {
        base.AlterMeleeDamageFrom(from,ref damage);
        damage=damage*(100-HavenPetSignatures.GuardPercent(this))/100;
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list); list.Add($"{RageDescription}");
        list.Add($"{"Custom mount: no stat loss on taming; dexterity/stamina"} {RawDex} {"/"} {StamMax}");
    }
}
