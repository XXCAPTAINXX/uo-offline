// Creature statistics and names checked against ServUO pub57 public reference.
// Mechanics use this shard’s native ModernUO AI and ability system.
using ModernUO.Serialization;
using Server.Items;
using Server.UOOffline;
namespace Server.Mobiles;

[SerializationGenerator(0)]
public partial class AcidElementalRenowned : BaseCreature
{
    [Constructible]
    public AcidElementalRenowned() : base(AIType.AI_Mage)
    {
        Name = "Acid Elemental";
        Title = "[Renowned]";
        Body = 0x9E;
        BaseSoundID = 278;
        SetStr(450, 600);
        SetDex(120, 185);
        SetInt(361, 435);
        SetHits(2000, 2400);
        SetDamage(9, 15);
        SetDamageType(ResistanceType.Physical, 25);
        SetDamageType(ResistanceType.Poison, 50);
        SetDamageType(ResistanceType.Energy, 25);
        SetResistance(ResistanceType.Physical, 40, 70);
        SetResistance(ResistanceType.Fire, 30, 50);
        SetResistance(ResistanceType.Cold, 20, 40);
        SetResistance(ResistanceType.Poison, 10, 30);
        SetResistance(ResistanceType.Energy, 20, 50);
        SetSkill(SkillName.EvalInt, 80.1, 100.0);
        SetSkill(SkillName.Magery, 80.1, 100.0);
        SetSkill(SkillName.MagicResist, 65.2, 100.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.Wrestling, 80.1, 100.0);
        Fame = 12500;
        Karma = -12500;
        VirtualArmor = 70;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Acid Elemental [Renowned] corpse";
    public override Poison PoisonImmune => Poison.Lethal;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}

[SerializationGenerator(0)]
public partial class AcidSlug : BaseCreature
{
    [Constructible]
    public AcidSlug() : base(AIType.AI_Melee)
    {
        Name = "an acid slug";
        Body = 51;
        SetStr(213, 294);
        SetDex(80, 82);
        SetInt(18, 22);
        SetHits(333, 370);
        SetDamage(21, 28);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 10, 15);
        SetResistance(ResistanceType.Fire, 0);
        SetResistance(ResistanceType.Cold, 10, 15);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 10, 15);
        SetSkill(SkillName.MagicResist, 25.0);
        SetSkill(SkillName.Tactics, 30.0, 50.0);
        SetSkill(SkillName.Wrestling, 30.0, 80.0);
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "an acid slug corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
    }
}

[SerializationGenerator(0)]
public partial class AncientLichRenowned : BaseCreature
{
    [Constructible]
    public AncientLichRenowned() : base(AIType.AI_Mage)
    {
        Name = "Ancient Lich";
        Title = "[Renowned]";
        Body = 78;
        BaseSoundID = 412;
        SetStr(250, 305);
        SetDex(96, 115);
        SetInt(966, 1045);
        SetHits(2000, 2500);
        SetDamage(15, 27);
        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 40);
        SetDamageType(ResistanceType.Energy, 40);
        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 25, 30);
        SetResistance(ResistanceType.Cold, 50, 60);
        SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 25, 30);
        SetSkill(SkillName.EvalInt, 120.1, 130.0);
        SetSkill(SkillName.Magery, 120.1, 130.0);
        SetSkill(SkillName.Meditation, 100.1, 101.0);
        SetSkill(SkillName.MagicResist, 175.2, 200.0);
        SetSkill(SkillName.Tactics, 90.1, 100.0);
        SetSkill(SkillName.Wrestling, 75.1, 100.0);
        Fame = 23000;
        Karma = -23000;
        VirtualArmor = 60;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Ancient Lich [Renowned] corpse";
    public override Poison PoisonImmune => Poison.Lethal;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
    }
}

[SerializationGenerator(0)]
public partial class ClanCA : BaseCreature
{
    [Constructible]
    public ClanCA() : base(AIType.AI_Archer)
    {
        Name = "Clan Chitter Assistant";
        Body = 0x8E;
        BaseSoundID = 437;
        SetStr(146, 175);
        SetDex(101, 130);
        SetInt(120, 135);
        SetHits(120, 145);
        SetDamage(4, 10);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 23, 35);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 30, 50);
        SetResistance(ResistanceType.Poison, 15, 20);
        SetResistance(ResistanceType.Energy, 10, 20);
        SetSkill(SkillName.Anatomy, 0);
        SetSkill(SkillName.Archery, 80.1, 90.0);
        SetSkill(SkillName.MagicResist, 81.1, 90.0);
        SetSkill(SkillName.Tactics, 53.8, 75.0);
        SetSkill(SkillName.Wrestling, 62.3, 75.0);
        Fame = 6500;
        Karma = -6500;
        VirtualArmor = 56;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a clan chitter assistant corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
    }
}

[SerializationGenerator(0)]
public partial class ClanCT : BaseCreature
{
    [Constructible]
    public ClanCT() : base(AIType.AI_Archer)
    {
        Name = "Clan Scratch Tinkerer";
        Body = 0x8E;
        BaseSoundID = 437;
        SetStr(300, 330);
        SetDex(220, 240);
        SetInt(240, 275);
        SetHits(2025, 2068);
        SetDamage(4, 10);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 20, 30);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 35, 50);
        SetResistance(ResistanceType.Poison, 10, 20);
        SetResistance(ResistanceType.Energy, 10, 20);
        SetSkill(SkillName.Anatomy, 62.5, 82.6);
        SetSkill(SkillName.Archery, 80.1, 90.0);
        SetSkill(SkillName.MagicResist, 76.8, 99.3);
        SetSkill(SkillName.Tactics, 64.2, 84.4);
        SetSkill(SkillName.Wrestling, 62.8, 85.0);
        Fame = 6500;
        Karma = -6500;
        VirtualArmor = 56;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a clan scratch tinkerer corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}

[SerializationGenerator(0)]
public partial class ClanRC : BaseCreature
{
    [Constructible]
    public ClanRC() : base(AIType.AI_Melee)
    {
        Name = "Clan Ribbon Courtier";
        Body = 42;
        Hue = 2207;
        BaseSoundID = 437;
        SetStr(231);
        SetDex(252);
        SetInt(125);
        SetHits(2054, 2100);
        SetDamage(7, 14);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 40);
        SetResistance(ResistanceType.Fire, 10, 12);
        SetResistance(ResistanceType.Cold, 15, 20);
        SetResistance(ResistanceType.Poison, 10, 12);
        SetResistance(ResistanceType.Energy, 10, 12);
        SetSkill(SkillName.MagicResist, 113.5, 115.0);
        SetSkill(SkillName.Tactics, 65.1, 70.0);
        SetSkill(SkillName.Wrestling, 50.5, 55.0);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 48;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a clan ribbon courtier corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.Average);
    }
}

[SerializationGenerator(0)]
public partial class ClanRS : BaseCreature
{
    [Constructible]
    public ClanRS() : base(AIType.AI_Melee)
    {
        Name = "Clan Ribbon Supplicant";
        Body = 42;
        Hue = 2952;
        BaseSoundID = 437;
        SetStr(173);
        SetDex(117);
        SetInt(207);
        SetHits(127);
        SetDamage(7, 14);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 55, 60);
        SetResistance(ResistanceType.Fire, 30, 35);
        SetResistance(ResistanceType.Cold, 80, 85);
        SetResistance(ResistanceType.Poison, 45, 50);
        SetResistance(ResistanceType.Energy, 25, 30);
        SetSkill(SkillName.MagicResist, 78.5, 80.0);
        SetSkill(SkillName.Tactics, 62.1, 65.0);
        SetSkill(SkillName.Wrestling, 56.5, 60.0);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 48;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a clan ribbon supplicant corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 3);
    }
}

[SerializationGenerator(0)]
public partial class ClanRibbonPlagueRat : BaseCreature
{
    [Constructible]
    public ClanRibbonPlagueRat() : base(AIType.AI_Animal)
    {
        Name = "Clan Ribbon Plague Rat";
        Body = 238;
        BaseSoundID = 0xCC;
        SetStr(59);
        SetDex(51);
        SetInt(17);
        SetHits(92);
        SetStam(51);
        SetDamage(4, 8);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 30, 40);
        SetResistance(ResistanceType.Poison, 5, 10);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Energy, 30, 40);
        SetSkill(SkillName.MagicResist, 30.0);
        SetSkill(SkillName.Tactics, 34.0);
        SetSkill(SkillName.Wrestling, 40.0);
        Fame = 150;
        Karma = -150;
        VirtualArmor = 6;
        Hue = 52;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a rat corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Poor);
    }
}

[SerializationGenerator(0)]
public partial class ClanSH : BaseCreature
{
    [Constructible]
    public ClanSH() : base(AIType.AI_Melee)
    {
        Name = "Clan Scratch Henchrat";
        Body = 42;
        BaseSoundID = 437;
        SetStr(227);
        SetDex(183);
        SetInt(93);
        SetHits(2065);
        SetDamage(5, 7);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 26, 30);
        SetResistance(ResistanceType.Fire, 29, 35);
        SetResistance(ResistanceType.Cold, 30, 35);
        SetResistance(ResistanceType.Poison, 15, 20);
        SetResistance(ResistanceType.Energy, 13, 15);
        SetSkill(SkillName.MagicResist, 35.4, 40.0);
        SetSkill(SkillName.Tactics, 61.1, 65.0);
        SetSkill(SkillName.Wrestling, 64.0, 65.0);
        SetSkill(SkillName.Anatomy, 74.0, 75.0);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 48;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a clan scratch henchrat corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 3);
    }
}

[SerializationGenerator(0)]
public partial class ClanSS : BaseCreature
{
    [Constructible]
    public ClanSS() : base(AIType.AI_Archer)
    {
        Name = "Clan Scratch Scrounger";
        Body = 0x8E;
        BaseSoundID = 437;
        SetStr(97, 100);
        SetDex(98, 100);
        SetInt(45, 50);
        SetHits(135);
        SetDamage(4, 5);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 25, 30);
        SetResistance(ResistanceType.Fire, 20, 25);
        SetResistance(ResistanceType.Cold, 49, 55);
        SetResistance(ResistanceType.Poison, 10, 20);
        SetResistance(ResistanceType.Energy, 10, 20);
        SetSkill(SkillName.Anatomy, 51.5, 55.5);
        SetSkill(SkillName.MagicResist, 65.1, 90.0);
        SetSkill(SkillName.Tactics, 59.1, 65.0);
        SetSkill(SkillName.Wrestling, 72.5, 75.0);
        Fame = 6500;
        Karma = -6500;
        VirtualArmor = 56;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a clan scratch scrounger corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 2);
    }
}

[SerializationGenerator(0)]
public partial class ClanSSW : BaseCreature
{
    [Constructible]
    public ClanSSW() : base(AIType.AI_Melee)
    {
        Name = "Clan Scratch Savage Wolf";
        Body = 98;
        Hue = 0x2C;
        BaseSoundID = 229;
        SetStr(170);
        SetDex(244);
        SetInt(57);
        SetHits(65);
        SetDamage(8, 10);
        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Cold, 80);
        SetResistance(ResistanceType.Physical, 30, 35);
        SetResistance(ResistanceType.Cold, 40, 45);
        SetResistance(ResistanceType.Poison, 25, 30);
        SetResistance(ResistanceType.Energy, 20, 25);
        SetSkill(SkillName.Swords, 99.0, 100.0);
        SetSkill(SkillName.MagicResist, 41.5, 42.5);
        SetSkill(SkillName.Tactics, 65.1, 70.0);
        SetSkill(SkillName.Wrestling, 42.3, 45.5);
        Fame = 3400;
        Karma = -3400;
        VirtualArmor = 50;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a clan scratch savage wolf corpse";
    public override WeaponAbility GetWeaponAbility() => WeaponAbility.ParalyzingBlow;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
        AddLoot(LootPack.Meager);
    }
}

[SerializationGenerator(0)]
public partial class ClockworkScorpion : BaseCreature
{
    [Constructible]
    public ClockworkScorpion() : base(AIType.AI_Melee)
    {
        Name = "a clockwork scorpion";
        Body = 717;
        SetStr(225, 245);
        SetDex(80, 100);
        SetInt(30, 40);
        SetHits(151, 210);
        SetDamage(5, 10);
        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Poison, 40);
        SetResistance(ResistanceType.Physical, 80, 100);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 60, 80);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 10, 25);
        SetSkill(SkillName.MagicResist, 30.1, 50.0);
        SetSkill(SkillName.Poisoning, 95.1, 100.0);
        SetSkill(SkillName.Tactics, 70.1, 90.0);
        SetSkill(SkillName.Wrestling, 50.1, 80.0);
        Fame = 3500;
        Karma = -3500;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a clockwork scorpion corpse";
    public override Poison PoisonImmune => Poison.Lethal;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager, 2);
    }
}

[SerializationGenerator(0)]
public partial class DevourerRenowned : BaseCreature
{
    [Constructible]
    public DevourerRenowned() : base(AIType.AI_Mage)
    {
        Name = "Devourer of Souls";
        Title = "[Renowned]";
        Body = 303;
        BaseSoundID = 357;
        SetStr(801, 950);
        SetDex(126, 175);
        SetInt(201, 250);
        SetHits(2000);
        SetDamage(22, 26);
        SetDamageType(ResistanceType.Physical, 60);
        SetDamageType(ResistanceType.Cold, 20);
        SetDamageType(ResistanceType.Energy, 20);
        SetResistance(ResistanceType.Physical, 45, 55);
        SetResistance(ResistanceType.Fire, 25, 35);
        SetResistance(ResistanceType.Cold, 15, 25);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 40, 50);
        SetSkill(SkillName.Necromancy, 90.1, 100.0);
        SetSkill(SkillName.SpiritSpeak, 90.1, 100.0);
        SetSkill(SkillName.EvalInt, 90.1, 100.0);
        SetSkill(SkillName.Magery, 90.1, 100.0);
        SetSkill(SkillName.Meditation, 90.1, 100.0);
        SetSkill(SkillName.MagicResist, 90.1, 105.0);
        SetSkill(SkillName.Tactics, 75.1, 85.0);
        SetSkill(SkillName.Wrestling, 80.1, 100.0);
        Fame = 9500;
        Karma = -9500;
        VirtualArmor = 44;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Devourer of Souls [Renowned] corpse";
    public override Poison PoisonImmune => Poison.Lethal;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
    }
}

[SerializationGenerator(0)]
public partial class EnslavedGoblinKeeper : BaseCreature
{
    [Constructible]
    public EnslavedGoblinKeeper() : base(AIType.AI_Melee)
    {
        Name = "Enslaved Goblin Keeper";
        Body = 334;
        BaseSoundID = 0x600;
        SetStr(297, 297);
        SetDex(80, 80);
        SetInt(118, 118);
        SetHits(174, 174);
        SetStam(80, 80);
        SetMana(118, 118);
        SetDamage(5, 7);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 47, 47);
        SetResistance(ResistanceType.Fire, 37, 37);
        SetResistance(ResistanceType.Cold, 29, 29);
        SetResistance(ResistanceType.Poison, 10, 11);
        SetResistance(ResistanceType.Energy, 19, 19);
        SetSkill(SkillName.MagicResist, 121.6, 122.2);
        SetSkill(SkillName.Tactics, 80.0, 82.8);
        SetSkill(SkillName.Anatomy, 82.0, 84.8);
        SetSkill(SkillName.Wrestling, 99.2, 100.7);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 28;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "an goblin corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}

[SerializationGenerator(0)]
public partial class EnslavedGoblinMage : BaseCreature
{
    [Constructible]
    public EnslavedGoblinMage() : base(AIType.AI_Melee)
    {
        Name = "Enslaved Goblin Mage";
        Body = 334;
        BaseSoundID = 0x600;
        SetStr(297, 297);
        SetDex(94, 94);
        SetInt(510, 510);
        SetHits(174, 174);
        SetStam(94, 94);
        SetMana(510, 510);
        SetDamage(5, 7);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 22, 22);
        SetResistance(ResistanceType.Fire, 36, 37);
        SetResistance(ResistanceType.Cold, 39, 39);
        SetResistance(ResistanceType.Poison, 43, 43);
        SetResistance(ResistanceType.Energy, 14, 14);
        SetSkill(SkillName.MagicResist, 121.6, 149.7);
        SetSkill(SkillName.Tactics, 80.0, 85.2);
        SetSkill(SkillName.Anatomy, 82.0, 86.6);
        SetSkill(SkillName.Wrestling, 99.2, 106.4);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 28;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "an goblin corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}

[SerializationGenerator(0)]
public partial class EnslavedGoblinScout : BaseCreature
{
    [Constructible]
    public EnslavedGoblinScout() : base(AIType.AI_Melee)
    {
        Name = "Enslaved Goblin Scout";
        Body = 334;
        BaseSoundID = 0x600;
        SetStr(320, 320);
        SetDex(74, 74);
        SetInt(112, 112);
        SetHits(182, 182);
        SetStam(74, 74);
        SetMana(112, 112);
        SetDamage(5, 7);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 42, 42);
        SetResistance(ResistanceType.Fire, 33, 33);
        SetResistance(ResistanceType.Cold, 30, 30);
        SetResistance(ResistanceType.Poison, 14, 14);
        SetResistance(ResistanceType.Energy, 18, 18);
        SetSkill(SkillName.MagicResist, 95.0, 95.0);
        SetSkill(SkillName.Tactics, 80.0, 86.9);
        SetSkill(SkillName.Anatomy, 82.0, 89.3);
        SetSkill(SkillName.Wrestling, 99.2, 113.7);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 28;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "an goblin corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}

[SerializationGenerator(0)]
public partial class EnslavedGrayGoblin : BaseCreature
{
    [Constructible]
    public EnslavedGrayGoblin() : base(AIType.AI_Melee)
    {
        Name = "Enslaved Gray Goblin";
        Body = 334;
        BaseSoundID = 0x600;
        SetStr(321, 321);
        SetDex(64, 64);
        SetInt(147, 147);
        SetHits(179, 179);
        SetStam(64, 64);
        SetMana(147, 147);
        SetDamage(5, 7);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 50, 50);
        SetResistance(ResistanceType.Fire, 38, 38);
        SetResistance(ResistanceType.Cold, 32, 32);
        SetResistance(ResistanceType.Poison, 12, 12);
        SetResistance(ResistanceType.Energy, 11, 11);
        SetSkill(SkillName.MagicResist, 121.6, 121.6);
        SetSkill(SkillName.Tactics, 90.0, 90.0);
        SetSkill(SkillName.Anatomy, 82.0, 82.0);
        SetSkill(SkillName.Wrestling, 99.2, 99.2);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 28;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "an goblin corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}

[SerializationGenerator(0)]
public partial class EnslavedGreenGoblin : BaseCreature
{
    [Constructible]
    public EnslavedGreenGoblin() : base(AIType.AI_Melee)
    {
        Name = "Enslaved Green Goblin";
        Body = 334;
        BaseSoundID = 0x600;
        SetStr(326, 326);
        SetDex(71, 71);
        SetInt(126, 126);
        SetHits(184, 184);
        SetStam(71, 71);
        SetMana(126, 126);
        SetDamage(5, 7);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 40, 40);
        SetResistance(ResistanceType.Fire, 38, 39);
        SetResistance(ResistanceType.Cold, 31, 32);
        SetResistance(ResistanceType.Poison, 12, 12);
        SetResistance(ResistanceType.Energy, 10, 11);
        SetSkill(SkillName.MagicResist, 121.6, 122.9);
        SetSkill(SkillName.Tactics, 80.0, 81.2);
        SetSkill(SkillName.Anatomy, 82.0, 83.4);
        SetSkill(SkillName.Wrestling, 99.2, 99.4);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 28;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "an goblin corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}

[SerializationGenerator(0)]
public partial class EnslavedGreenGoblinAlchemist : BaseCreature
{
    [Constructible]
    public EnslavedGreenGoblinAlchemist() : base(AIType.AI_Melee)
    {
        Name = "Green Goblin Alchemist";
        Body = 723;
        BaseSoundID = 0x600;
        SetStr(289, 289);
        SetDex(72, 72);
        SetInt(113, 113);
        SetHits(196, 196);
        SetStam(72, 72);
        SetMana(113, 113);
        SetDamage(5, 7);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 45, 49);
        SetResistance(ResistanceType.Fire, 50, 53);
        SetResistance(ResistanceType.Cold, 25, 30);
        SetResistance(ResistanceType.Poison, 40, 42);
        SetResistance(ResistanceType.Energy, 15, 18);
        SetSkill(SkillName.MagicResist, 124.1, 126.2);
        SetSkill(SkillName.Tactics, 75.3, 83.6);
        SetSkill(SkillName.Anatomy, 0.0, 0.0);
        SetSkill(SkillName.Wrestling, 90.4, 94.7);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 28;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "an goblin corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Meager);
    }
}

[SerializationGenerator(0)]
public partial class FairyDragon : BaseCreature
{
    [Constructible]
    public FairyDragon() : base(AIType.AI_Mage)
    {
        Name = "Fairy Dragon";
        Body = 718;
        BaseSoundID = 362;
        SetStr(512, 558);
        SetDex(95, 105);
        SetInt(455, 501);
        SetHits(398, 403);
        SetDamage(15, 18);
        SetDamageType(ResistanceType.Fire, 20, 25);
        SetDamageType(ResistanceType.Cold, 20, 25);
        SetDamageType(ResistanceType.Poison, 20, 25);
        SetDamageType(ResistanceType.Energy, 20, 25);
        SetResistance(ResistanceType.Physical, 16, 30);
        SetResistance(ResistanceType.Fire, 41, 44);
        SetResistance(ResistanceType.Cold, 40, 49);
        SetResistance(ResistanceType.Poison, 40, 49);
        SetResistance(ResistanceType.Energy, 45, 47);
        SetSkill(SkillName.MagicResist, 99.1, 100.0);
        SetSkill(SkillName.Tactics, 60.6, 68.2);
        SetSkill(SkillName.Wrestling, 90.1, 92.5);
        SetSkill(SkillName.Mysticism, 101.8, 108.3);
        Fame = 15000;
        Karma = -15000;
        VirtualArmor = 39;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a Fairy dragon corpse";
    public override Poison HitPoison => Poison.Greater;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.MedScrolls, 2);
    }
}

[SerializationGenerator(0)]
public partial class FireAnt : BaseCreature
{
    [Constructible]
    public FireAnt() : base(AIType.AI_Melee)
    {
        Name = "a fire ant";
        Body = 738;
        SetStr(225);
        SetDex(108);
        SetInt(25);
        SetHits(299);
        SetDamage(15, 18);
        SetDamageType(ResistanceType.Physical, 40);
        SetDamageType(ResistanceType.Fire, 60);
        SetResistance(ResistanceType.Physical, 52);
        SetResistance(ResistanceType.Fire, 96);
        SetResistance(ResistanceType.Cold, 36);
        SetResistance(ResistanceType.Poison, 40);
        SetResistance(ResistanceType.Energy, 36);
        SetSkill(SkillName.Anatomy, 8.7);
        SetSkill(SkillName.MagicResist, 53.1);
        SetSkill(SkillName.Tactics, 77.2);
        SetSkill(SkillName.Wrestling, 75.4);
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a fire ant corpse";
    private static readonly MonsterAbility[] Abilities = { MonsterAbilities.PoisonGasAreaAttack };
    public override MonsterAbility[] GetMonsterAbilities() => Abilities;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average, 2);
    }
}

[SerializationGenerator(0)]
public partial class FireDaemon : BaseCreature
{
    [Constructible]
    public FireDaemon() : base(AIType.AI_Mage)
    {
        Name = "a fire daemon";
        Body = 9;
        BaseSoundID = 0x47D;
        Hue = 1636;
        SetStr(504, 539);
        SetDex(126, 145);
        SetInt(329, 364);
        SetHits(1026, 1174);
        SetDamage(7, 14);
        SetDamageType(ResistanceType.Physical, 20);
        SetDamageType(ResistanceType.Fire, 80);
        SetResistance(ResistanceType.Physical, 45, 60);
        SetResistance(ResistanceType.Fire, 100);
        SetResistance(ResistanceType.Cold, -10, 0);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 30, 40);
        SetSkill(SkillName.Anatomy, 75.5, 84.9);
        SetSkill(SkillName.MagicResist, 95.7, 109.8);
        SetSkill(SkillName.Tactics, 81.0, 98.6);
        SetSkill(SkillName.Wrestling, 40.2, 78.7);
        SetSkill(SkillName.EvalInt, 91.1, 104.5);
        SetSkill(SkillName.Magery, 91.3, 105.0);
        SetSkill(SkillName.Meditation, 90.1, 103.7);
        SetSkill(SkillName.DetectHidden, 66.0);
        Fame = 15000;
        Karma = -15000;
        VirtualArmor = 58;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a fire daemon corpse";
    public override Poison PoisonImmune => Poison.Regular;
    private static readonly MonsterAbility[] Abilities = { MonsterAbilities.FireBreath, new HavenAbyssAura() };
    public override MonsterAbility[] GetMonsterAbilities() => Abilities;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich);
        AddLoot(LootPack.Rich);
    }
}

[SerializationGenerator(0)]
public partial class FireDaemonRenowned : BaseCreature
{
    [Constructible]
    public FireDaemonRenowned() : base(AIType.AI_Mage)
    {
        Name = "Fire Daemon";
        Title = "[Renowned]";
        Body = 40;
        BaseSoundID = 357;
        Hue = 243;
        SetStr(800, 1199);
        SetDex(200, 250);
        SetInt(202, 336);
        SetHits(1111, 1478);
        SetDamage(22, 29);
        SetDamageType(ResistanceType.Physical, 50);
        SetDamageType(ResistanceType.Fire, 25);
        SetDamageType(ResistanceType.Energy, 25);
        SetResistance(ResistanceType.Physical, 60, 93);
        SetResistance(ResistanceType.Fire, 60, 100);
        SetResistance(ResistanceType.Cold, 40, 70);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 37, 50);
        SetSkill(SkillName.MagicResist, 110.1, 132.6);
        SetSkill(SkillName.Tactics, 86.9, 95.5);
        SetSkill(SkillName.Wrestling, 42.2, 98.8);
        SetSkill(SkillName.Magery, 97.1, 100.8);
        SetSkill(SkillName.EvalInt, 91.1, 91.8);
        SetSkill(SkillName.Meditation, 45.4, 94.1);
        SetSkill(SkillName.Anatomy, 45.4, 74.1);
        Fame = 7000;
        Karma = -10000;
        VirtualArmor = 55;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Fire Daemon [Renowned] corpse";
    public override WeaponAbility GetWeaponAbility() => WeaponAbility.ConcussionBlow;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
    }
}

[SerializationGenerator(0)]
public partial class FireElementalRenowned : BaseCreature
{
    [Constructible]
    public FireElementalRenowned() : base(AIType.AI_Mage)
    {
        Name = "Fire Elemental";
        Title = "[Renowned]";
        Body = 15;
        BaseSoundID = 838;
        Hue = 1161;
        SetStr(450, 500);
        SetDex(200, 250);
        SetInt(300, 350);
        SetHits(1200, 1600);
        SetDamage(7, 9);
        SetDamageType(ResistanceType.Physical, 25);
        SetDamageType(ResistanceType.Fire, 75);
        SetResistance(ResistanceType.Physical, 45, 60);
        SetResistance(ResistanceType.Fire, 70, 80);
        SetResistance(ResistanceType.Cold, 5, 10);
        SetResistance(ResistanceType.Poison, 30, 50);
        SetResistance(ResistanceType.Energy, 40, 60);
        SetSkill(SkillName.EvalInt, 100.1, 110.0);
        SetSkill(SkillName.Magery, 105.1, 110.0);
        SetSkill(SkillName.MagicResist, 110.2, 120.0);
        SetSkill(SkillName.Tactics, 100.1, 105.0);
        SetSkill(SkillName.Wrestling, 90.1, 100.0);
        Fame = 4500;
        Karma = -4500;
        VirtualArmor = 40;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Fire Elemental [Renowned] corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich);
    }
}

[SerializationGenerator(0)]
public partial class ForgottenServant : BaseCreature
{
    [Constructible]
    public ForgottenServant() : base(AIType.AI_Melee)
    {
        Title = "Forgotten Servant";
        Hue = 768;
        SetStr(147, 215);
        SetDex(91, 115);
        SetInt(61, 85);
        SetHits(95, 123);
        SetDamage(4, 14);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 25, 35);
        SetResistance(ResistanceType.Fire, 30, 40);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 30, 40);
        SetResistance(ResistanceType.Energy, 30, 40);
        SetSkill(SkillName.MagicResist, 70.1, 85.0);
        SetSkill(SkillName.Swords, 60.1, 85.0);
        SetSkill(SkillName.Tactics, 75.1, 90.0);
        SetSkill(SkillName.Wrestling, 60.1, 85.0);
        Fame = 2500;
        Karma = -2500;
        Name = "a forgotten servant"; Body = 0x190;
        AddItem(new ShortPants()); AddItem(new FancyShirt()); AddItem(new Boots()); AddItem(new Longsword());
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Average);
    }
}

[SerializationGenerator(0)]
public partial class GrayGoblinMageRenowned : BaseCreature
{
    [Constructible]
    public GrayGoblinMageRenowned() : base(AIType.AI_Mage)
    {
        Name = "Gray Goblin Mage";
        Title = "[Renowned]";
        Body = 723;
        Hue = 1900;
        BaseSoundID = 0x600;
        SetStr(550, 600);
        SetDex(70, 75);
        SetInt(500, 600);
        SetHits(1100, 1300);
        SetDamage(5, 7);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 30, 35);
        SetResistance(ResistanceType.Fire, 45, 50);
        SetResistance(ResistanceType.Cold, 40, 50);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 20, 25);
        SetSkill(SkillName.MagicResist, 120.0, 125.0);
        SetSkill(SkillName.Tactics, 95.0, 100.0);
        SetSkill(SkillName.Wrestling, 100.0, 110.0);
        SetSkill(SkillName.EvalInt, 100.0, 120.0);
        SetSkill(SkillName.Meditation, 100.0, 105.0);
        SetSkill(SkillName.Magery, 100.0, 110.0);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 28;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Gray Goblin Mage [Renowned] corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich);
    }
}

[SerializationGenerator(0)]
public partial class GreenGoblinAlchemistRenowned : BaseCreature
{
    [Constructible]
    public GreenGoblinAlchemistRenowned() : base(AIType.AI_Melee)
    {
        Name = "Green Goblin Alchemist";
        Title = "[Renowned]";
        Body = 723;
        BaseSoundID = 0x600;
        SetStr(600, 650);
        SetDex(50, 70);
        SetInt(100, 250);
        SetHits(1000, 1500);
        SetDamage(5, 7);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 50, 55);
        SetResistance(ResistanceType.Fire, 55, 60);
        SetResistance(ResistanceType.Cold, 40, 50);
        SetResistance(ResistanceType.Poison, 40, 50);
        SetResistance(ResistanceType.Energy, 20, 25);
        SetSkill(SkillName.MagicResist, 120.0, 125.0);
        SetSkill(SkillName.Tactics, 95.0, 100.0);
        SetSkill(SkillName.Wrestling, 100.0, 110.0);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 28;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Green Goblin Alchemist [Renowned] corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 2);
    }
}

[SerializationGenerator(0)]
public partial class LavaElemental : BaseCreature
{
    [Constructible]
    public LavaElemental() : base(AIType.AI_Mage)
    {
        Name = "a lava elemental";
        Body = 720;
        SetStr(446, 510);
        SetDex(160, 190);
        SetInt(360, 430);
        SetHits(270, 290);
        SetDamage(12, 18);
        SetDamageType(ResistanceType.Physical, 10);
        SetDamageType(ResistanceType.Fire, 90);
        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 20, 30);
        SetResistance(ResistanceType.Cold, 20, 30);
        SetResistance(ResistanceType.Poison, 100);
        SetResistance(ResistanceType.Energy, 40, 50);
        SetSkill(SkillName.EvalInt, 84.8, 92.6);
        SetSkill(SkillName.Magery, 80.0, 92.7);
        SetSkill(SkillName.Meditation, 97.8, 120.0);
        SetSkill(SkillName.MagicResist, 101.9, 106.2);
        SetSkill(SkillName.Tactics, 80.3, 94.0);
        SetSkill(SkillName.Wrestling, 71.7, 85.4);
        SetSkill(SkillName.Poisoning, 90.0, 100.0);
        SetSkill(SkillName.DetectHidden, 75.1);
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a lava elemental corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
        AddLoot(LootPack.Gems, 2);
        AddLoot(LootPack.MedScrolls);
    }
}

[SerializationGenerator(0)]
public partial class PitFiend : BaseCreature
{
    [Constructible]
    public PitFiend() : base(AIType.AI_Mage)
    {
        Name = "a Pit fiend";
        Body = 43;
        Hue = 1863;
        BaseSoundID = 357;
        SetStr(376, 405);
        SetDex(176, 195);
        SetInt(201, 225);
        SetHits(226, 243);
        SetDamage(15, 20);
        SetSkill(SkillName.EvalInt, 80.1, 90.0);
        SetSkill(SkillName.Magery, 80.1, 90.0);
        SetSkill(SkillName.MagicResist, 75.1, 85.0);
        SetSkill(SkillName.Tactics, 80.1, 90.0);
        SetSkill(SkillName.Wrestling, 80.1, 100.0);
        SetResistance(ResistanceType.Physical, 55, 65);
        SetResistance(ResistanceType.Fire, 10, 20);
        SetResistance(ResistanceType.Cold, 60, 70);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 30, 40);
        Fame = 18000;
        Karma = -18000;
        VirtualArmor = 60;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "a pit fiend corpse";
    public override Poison PoisonImmune => Poison.Regular;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
        AddLoot(LootPack.Average, 2);
        AddLoot(LootPack.MedScrolls, 2);
    }
}

[SerializationGenerator(0)]
public partial class PixieRenowned : BaseCreature
{
    [Constructible]
    public PixieRenowned() : base(AIType.AI_Mage)
    {
        Name = "Pixie";
        Title = "[Renowned]";
        Body = 128;
        BaseSoundID = 0x467;
        SetStr(350, 380);
        SetDex(450, 600);
        SetInt(700, 850);
        SetHits(9100, 9200);
        SetStam(450, 600);
        SetMana(700, 800);
        SetDamage(9, 15);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 70, 90);
        SetResistance(ResistanceType.Fire, 60, 70);
        SetResistance(ResistanceType.Cold, 70, 80);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 60, 70);
        SetSkill(SkillName.EvalInt, 100.0, 100.0);
        SetSkill(SkillName.Magery, 90.1, 110.0);
        SetSkill(SkillName.Meditation, 100.0, 100.0);
        SetSkill(SkillName.MagicResist, 110.5, 150.0);
        SetSkill(SkillName.Tactics, 100.1, 120.0);
        SetSkill(SkillName.Wrestling, 100.1, 120.0);
        Fame = 7000;
        Karma = 7000;
        VirtualArmor = 100;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Pixie [Renowned] corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 2);
    }
}

[SerializationGenerator(0)]
public partial class RakktaviRenowned : BaseCreature
{
    [Constructible]
    public RakktaviRenowned() : base(AIType.AI_Archer)
    {
        Name = "Rakktavi";
        Title = "[Renowned]";
        Body = 0x8E;
        BaseSoundID = 437;
        SetStr(119);
        SetDex(279);
        SetInt(327);
        SetHits(50000);
        SetMana(327);
        SetStam(279);
        SetDamage(8, 10);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 20, 30);
        SetResistance(ResistanceType.Fire, 10, 25);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 10, 20);
        SetResistance(ResistanceType.Energy, 10, 20);
        SetSkill(SkillName.Anatomy, 0);
        SetSkill(SkillName.Archery, 80.1, 90.0);
        SetSkill(SkillName.MagicResist, 66.0);
        SetSkill(SkillName.Tactics, 68.1);
        SetSkill(SkillName.Wrestling, 85.5);
        Fame = 6500;
        Karma = -6500;
        VirtualArmor = 56;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Rakktavi [Renowned] corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich, 3);
    }
}

[SerializationGenerator(0)]
public partial class SkeletalDragonRenowned : BaseCreature
{
    [Constructible]
    public SkeletalDragonRenowned() : base(AIType.AI_Mage)
    {
        Name = "Skeletal Dragon";
        Title = "[Renowned]";
        Body = 104;
        BaseSoundID = 0x488;
        Hue = 906;
        SetStr(898, 1030);
        SetDex(100, 200);
        SetInt(488, 620);
        SetHits(558, 599);
        SetDamage(29, 35);
        SetDamageType(ResistanceType.Physical, 75);
        SetDamageType(ResistanceType.Fire, 25);
        SetResistance(ResistanceType.Physical, 75, 80);
        SetResistance(ResistanceType.Fire, 40, 60);
        SetResistance(ResistanceType.Cold, 40, 60);
        SetResistance(ResistanceType.Poison, 70, 80);
        SetResistance(ResistanceType.Energy, 40, 60);
        SetSkill(SkillName.EvalInt, 80.1, 100.0);
        SetSkill(SkillName.Magery, 80.1, 100.0);
        SetSkill(SkillName.MagicResist, 100.3, 130.0);
        SetSkill(SkillName.Tactics, 97.6, 100.0);
        SetSkill(SkillName.Wrestling, 97.6, 100.0);
        Fame = 22500;
        Karma = -22500;
        VirtualArmor = 80;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Skeletal Dragon [Renowned] corpse";
    public override Poison PoisonImmune => Poison.Lethal;
    private static readonly MonsterAbility[] Abilities = { MonsterAbilities.FireBreath };
    public override MonsterAbility[] GetMonsterAbilities() => Abilities;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich, 3);
    }
}

[SerializationGenerator(0)]
public partial class SkeletalLich : BaseCreature
{
    [Constructible]
    public SkeletalLich() : base(AIType.AI_Mage)
    {
        Name = "a skeletal lich";
        Body = 309;
        Hue = 1345;
        BaseSoundID = 0x48D;
        SetStr( 301, 350 );
        SetDex( 75 );
        SetInt( 151, 200 );
        SetHits( 1200 );
        SetStam( 150 );
        SetMana( 0 );
        SetDamage( 8, 10 );
        SetDamageType( ResistanceType.Physical, 0 );
        SetDamageType( ResistanceType.Cold, 50 );
        SetDamageType( ResistanceType.Poison, 50 );
        SetResistance( ResistanceType.Physical, 35, 45 );
        SetResistance( ResistanceType.Fire, 20, 30 );
        SetResistance( ResistanceType.Cold, 50, 70 );
        SetResistance( ResistanceType.Poison, 40, 50 );
        SetResistance( ResistanceType.Energy, 20, 30 );
        SetSkill( SkillName.EvalInt, 127.2 );
        SetSkill( SkillName.Magery, 127.2 );
        SetSkill( SkillName.Necromancy, 100.0, 120.0 );
        SetSkill( SkillName.MagicResist, 187.1 );
        SetSkill( SkillName.Tactics, 91.7 );
        SetSkill( SkillName.Wrestling, 98.5 );
        Fame = 6000;
        Karma = -6000;
        VirtualArmor = 40;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override Poison PoisonImmune => Poison.Lethal;
    public override WeaponAbility GetWeaponAbility() => WeaponAbility.Dismount;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.Rich);
    }
}

[SerializationGenerator(0)]
public partial class TikitaviRenowned : BaseCreature
{
    [Constructible]
    public TikitaviRenowned() : base(AIType.AI_Melee)
    {
        Name = "Tikitavi";
        Title = "[Renowned]";
        Body = 42;
        BaseSoundID = 437;
        SetStr(315, 354);
        SetDex(139, 177);
        SetInt(243, 288);
        SetHits(50000);
        SetMana(243, 288);
        SetStam(139, 177);
        SetDamage(7, 9);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 26, 28);
        SetResistance(ResistanceType.Fire, 22, 25);
        SetResistance(ResistanceType.Cold, 30, 38);
        SetResistance(ResistanceType.Poison, 14, 17);
        SetResistance(ResistanceType.Energy, 15, 18);
        SetSkill(SkillName.MagicResist, 40.4);
        SetSkill(SkillName.Tactics, 73.6);
        SetSkill(SkillName.Wrestling, 66.5);
        Fame = 1500;
        Karma = -1500;
        VirtualArmor = 28;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Tikitavi [Renowned] corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
    }
}

[SerializationGenerator(0)]
public partial class VitaviRenowned : BaseCreature
{
    [Constructible]
    public VitaviRenowned() : base(AIType.AI_Mage)
    {
        Name = "Vitavi";
        Title = "[Renowned]";
        Body = 0x8F;
        BaseSoundID = 437;
        SetStr(300, 350);
        SetDex(250, 300);
        SetInt(300, 350);
        SetHits(45000, 50000);
        SetDamage(7, 14);
        SetDamageType(ResistanceType.Physical, 100);
        SetResistance(ResistanceType.Physical, 50, 60);
        SetResistance(ResistanceType.Fire, 30, 50);
        SetResistance(ResistanceType.Cold, 60, 80);
        SetResistance(ResistanceType.Poison, 20, 30);
        SetResistance(ResistanceType.Energy, 30, 40);
        SetSkill(SkillName.EvalInt, 70.1, 80.0);
        SetSkill(SkillName.Magery, 70.1, 80.0);
        SetSkill(SkillName.MagicResist, 75.1, 100.0);
        SetSkill(SkillName.Tactics, 70.1, 75.0);
        SetSkill(SkillName.Wrestling, 50.1, 75.0);
        Fame = 7500;
        Karma = -7500;
        VirtualArmor = 44;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Vitavi [Renowned] corpse";
    public override void GenerateLoot()
    {
        AddLoot(LootPack.FilthyRich, 3);
    }
}

[SerializationGenerator(0)]
public partial class WyvernRenowned : BaseCreature
{
    [Constructible]
    public WyvernRenowned() : base(AIType.AI_Mage)
    {
        Name = "Wyvern";
        Title = "[Renowned]";
        Body = 62;
        Hue = 243;
        BaseSoundID = 362;
        SetStr(1370, 1422);
        SetDex(103, 151);
        SetInt(835, 1002);
        SetHits(2412, 2734);
        SetStam(103, 151);
        SetMana(835, 1002);
        SetDamage(29, 35);
        SetDamageType(ResistanceType.Physical, 75);
        SetDamageType(ResistanceType.Fire, 25);
        SetResistance(ResistanceType.Physical, 60, 70);
        SetResistance(ResistanceType.Fire, 80, 90);
        SetResistance(ResistanceType.Cold, 70, 80);
        SetResistance(ResistanceType.Poison, 60, 70);
        SetResistance(ResistanceType.Energy, 60, 70);
        SetSkill(SkillName.Magery, 107.7, 109.1);
        SetSkill(SkillName.Meditation, 63.9, 78.2);
        SetSkill(SkillName.EvalInt, 106.8, 111.1);
        SetSkill(SkillName.Wrestling, 108.6, 109.4);
        SetSkill(SkillName.MagicResist, 125.8, 127.6);
        SetSkill(SkillName.Tactics, 112.8, 123.7);
        Fame = 24000;
        Karma = -24000;
        VirtualArmor = 70;
        Tamable = false;
        foreach (var skill in Skills) { skill.Cap = System.Math.Max(skill.Cap, skill.Base); }
    }
    public override string CorpseName => "Wyvern [Renowned] corpse";
    public override Poison PoisonImmune => Poison.Deadly;
    public override Poison HitPoison => Poison.Deadly;
    public override void GenerateLoot()
    {
        AddLoot(LootPack.UltraRich);
    }
}

