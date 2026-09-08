// Clean-room ModernUO-compatible reward set inspired by:
// https://www.servuo.dev/archive/9-special-bracelets.1608/
// The original archive source was not accessible without an account. These are
// shard-specific designs and do not copy the inaccessible ServUO implementation.

using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class BraceletOfTheVanguard : GoldBracelet
{
    public override string DefaultName => "bracelet of the vanguard";

    [Constructible]
    public BraceletOfTheVanguard()
    {
        Hue = 0x972;
        Attributes.BonusStr = 5;
        Attributes.BonusHits = 5;
        Attributes.WeaponDamage = 10;
    }
}

[SerializationGenerator(0)]
public partial class BraceletOfArcaneFocus : GoldBracelet
{
    public override string DefaultName => "bracelet of arcane focus";

    [Constructible]
    public BraceletOfArcaneFocus()
    {
        Hue = 0x482;
        Attributes.BonusInt = 5;
        Attributes.BonusMana = 8;
        Attributes.LowerManaCost = 5;
    }
}

[SerializationGenerator(0)]
public partial class BraceletOfTheWind : GoldBracelet
{
    public override string DefaultName => "bracelet of the wind";

    [Constructible]
    public BraceletOfTheWind()
    {
        Hue = 0x47F;
        Attributes.BonusDex = 5;
        Attributes.BonusStam = 8;
        Attributes.AttackChance = 5;
    }
}

[SerializationGenerator(0)]
public partial class BraceletOfTheBeastmaster : GoldBracelet
{
    public override string DefaultName => "bracelet of the beastmaster";

    [Constructible]
    public BraceletOfTheBeastmaster()
    {
        Hue = 0x59B;
        SkillBonuses.SetValues(0, SkillName.AnimalTaming, 5.0);
        SkillBonuses.SetValues(1, SkillName.AnimalLore, 5.0);
        SkillBonuses.SetValues(2, SkillName.Veterinary, 5.0);
    }
}

[SerializationGenerator(0)]
public partial class BraceletOfTheVirtuoso : GoldBracelet
{
    public override string DefaultName => "bracelet of the virtuoso";

    [Constructible]
    public BraceletOfTheVirtuoso()
    {
        Hue = 0x489;
        SkillBonuses.SetValues(0, SkillName.Musicianship, 5.0);
        SkillBonuses.SetValues(1, SkillName.Discordance, 3.0);
        SkillBonuses.SetValues(2, SkillName.Peacemaking, 3.0);
        SkillBonuses.SetValues(3, SkillName.Provocation, 3.0);
    }
}

[SerializationGenerator(0)]
public partial class BraceletOfTheArtisan : GoldBracelet
{
    public override string DefaultName => "bracelet of the artisan";

    [Constructible]
    public BraceletOfTheArtisan()
    {
        Hue = 0x96D;
        SkillBonuses.SetValues(0, SkillName.Blacksmith, 5.0);
        SkillBonuses.SetValues(1, SkillName.Tailoring, 5.0);
        SkillBonuses.SetValues(2, SkillName.Tinkering, 5.0);
    }
}

[SerializationGenerator(0)]
public partial class BraceletOfFortune : GoldBracelet
{
    public override string DefaultName => "bracelet of fortune";

    [Constructible]
    public BraceletOfFortune()
    {
        Hue = 0x8A5;
        Attributes.Luck = 150;
    }
}

[SerializationGenerator(0)]
public partial class BraceletOfTheGuardian : GoldBracelet
{
    public override string DefaultName => "bracelet of the guardian";

    [Constructible]
    public BraceletOfTheGuardian()
    {
        Hue = 0x497;
        Attributes.DefendChance = 10;
        Attributes.RegenHits = 2;
        Resistances.Physical = 3;
        Resistances.Fire = 3;
        Resistances.Cold = 3;
        Resistances.Poison = 3;
        Resistances.Energy = 3;
    }
}

[SerializationGenerator(0)]
public partial class BraceletOfTheNight : GoldBracelet
{
    public override string DefaultName => "bracelet of the night";

    [Constructible]
    public BraceletOfTheNight()
    {
        Hue = 0x455;
        Attributes.NightSight = 1;
        Attributes.RegenHits = 2;
        Attributes.LowerRegCost = 10;
        Attributes.BonusHits = 5;
    }
}
