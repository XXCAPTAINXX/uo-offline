// Clean-room ModernUO-compatible recreation inspired by:
// https://www.servuo.dev/archive/vampiric-steed.1782/
// The ServUO archive source was not accessible without an account, so no source
// from that package is copied here. Only the public description ("Tamable") and
// theme informed this implementation.

using System;
using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class VampiricSteed : BaseMount
{
    public override string DefaultName => "a vampiric steed";

    [Constructible]
    public VampiricSteed() : base(0x74, 0x3EA7, AIType.AI_Mage)
    {
        BaseSoundID = 0xA8;
        Hue = 0x497;

        Body = 116;
        ItemID = 16039;

        SetStr(420, 470);
        SetDex(90, 110);
        SetInt(100, 135);

        SetHits(280, 330);
        SetDamage(14, 20);

        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Cold, 25);
        SetDamageType(ResistanceType.Energy, 25);

        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 25, 35);
        SetResistance(ResistanceType.Cold, 45, 55);
        SetResistance(ResistanceType.Poison, 35, 45);
        SetResistance(ResistanceType.Energy, 35, 45);

        SetSkill(SkillName.EvalInt, 35.0, 55.0);
        SetSkill(SkillName.Magery, 35.0, 55.0);
        SetSkill(SkillName.MagicResist, 80.0, 95.0);
        SetSkill(SkillName.Tactics, 90.0, 100.0);
        SetSkill(SkillName.Wrestling, 82.0, 95.0);

        Fame = 12000;
        Karma = -12000;
        VirtualArmor = 50;

        Tamable = true;
        MinTameSkill = 95.1;
        ControlSlots = 2;
    }

    internal bool IsGentleIslandSteed => !Controlled && !Summoned && Map == Map.Trammel &&
        X >= 3314 && X < 3814 && Y >= 2345 && Y < 3095;

    public override int HitsMax => IsGentleIslandSteed ? Math.Min(120, base.HitsMax) : base.HitsMax;

    internal void UpdateIslandDifficulty()
    {
        if (Controlled && RawDex < 180)
        {
            SetDex(180, 210);
            Stam = StamMax;
        }
        var desiredAI = IsGentleIslandSteed ? AIType.AI_Melee : AIType.AI_Mage;
        if (AI != desiredAI) { AI = desiredAI; }
        if (Hits > HitsMax) { Hits = HitsMax; }
    }

    public override void OnThink()
    {
        UpdateIslandDifficulty();
        base.OnThink();
    }

    public override void AlterMeleeDamageTo(Mobile to, ref int damage)
    {
        base.AlterMeleeDamageTo(to, ref damage);
        if (IsGentleIslandSteed && damage > 0) { damage = Math.Clamp(damage * 2 / 5, 1, 8); }
    }
    public override int StepsMax => 6400;
    public override string CorpseName => "a vampiric steed corpse";
    public override int Meat => 3;
    public override int Hides => 8;
    public override FoodType FavoriteFood => FoodType.Meat;
    public override bool CanAngerOnTame => true;

    public override void OnGaveMeleeAttack(Mobile defender, int damage)
    {
        base.OnGaveMeleeAttack(defender, damage);

        // Its defining trait: a restrained life-drain proc. It is intentionally
        // capped so a tamed steed is useful without replacing dedicated pet healing.
        if (damage > 0 && Hits < HitsMax && Utility.RandomDouble() < 0.30)
        {
            var drained = Math.Clamp(damage / 4, 1, 10);
            Heal(drained);
            FixedParticles(0x376A, 9, 32, 5005, EffectLayer.Waist);
        }
    }

    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.Average);
    }
}
