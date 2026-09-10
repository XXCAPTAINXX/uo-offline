using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

internal interface IHavenMarkEquipment
{
}

[SerializationGenerator(0)]
public partial class HavenBulwarkBoots : Boots, IHavenMarkEquipment
{
    [Constructible]
    public HavenBulwarkBoots()
    {
        Name = "Haven bulwark boots";
        Hue = 0x455;
        LootType = LootType.Blessed;
        SkillBonuses.SetValues(0, SkillName.Parry, 10);
        SkillBonuses.SetValues(1, SkillName.Healing, 5);
        Attributes.DefendChance = 5;
        Attributes.BonusStr = 5;
        Attributes.RegenHits = 2;
    }
}

[SerializationGenerator(0)]
public partial class HavenTideweaverSash : BodySash, IHavenMarkEquipment
{
    [Constructible]
    public HavenTideweaverSash()
    {
        Name = "Haven tideweaver's sash";
        Hue = 0x489;
        LootType = LootType.Blessed;
        SkillBonuses.SetValues(0, SkillName.Spellweaving, 10);
        SkillBonuses.SetValues(1, SkillName.Focus, 10);
        Attributes.LowerManaCost = 5;
        Attributes.SpellDamage = 5;
        Attributes.RegenMana = 2;
    }
}

[SerializationGenerator(0)]
public partial class HavenBeastkeepersDoublet : Doublet, IHavenMarkEquipment
{
    [Constructible]
    public HavenBeastkeepersDoublet()
    {
        Name = "Haven beastkeeper's doublet";
        Hue = 0x59B;
        LootType = LootType.Blessed;
        SkillBonuses.SetValues(0, SkillName.AnimalTaming, 10);
        SkillBonuses.SetValues(1, SkillName.AnimalLore, 10);
        SkillBonuses.SetValues(2, SkillName.Veterinary, 10);
        Attributes.RegenHits = 2;
        Attributes.DefendChance = 5;
    }
}

[SerializationGenerator(0)]
public partial class HavenProspectorsApron : HalfApron, IHavenMarkEquipment
{
    [Constructible]
    public HavenProspectorsApron()
    {
        Name = "Haven prospector's apron";
        Hue = 0x972;
        LootType = LootType.Blessed;
        SkillBonuses.SetValues(0, SkillName.Mining, 10);
        SkillBonuses.SetValues(1, SkillName.Lumberjacking, 10);
        SkillBonuses.SetValues(2, SkillName.Fishing, 10);
        Attributes.Luck = 150;
        Attributes.RegenStam = 2;
        Attributes.BonusStr = 5;
    }
}
