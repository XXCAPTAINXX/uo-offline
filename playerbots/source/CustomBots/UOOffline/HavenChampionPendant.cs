using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenChampionPendant : GoldNecklace
{
    [Constructible]
    public HavenChampionPendant()
    {
        Name = "Haven champion's pendant";
        Hue = 0x489; LootType = LootType.Blessed;
        Attributes.Luck = 500;
        Attributes.BonusStr = Attributes.BonusDex = Attributes.BonusInt = 10;
        Attributes.RegenHits = Attributes.RegenStam = Attributes.RegenMana = 3;
        Attributes.WeaponDamage = Attributes.SpellDamage = 25;
        Attributes.AttackChance = Attributes.DefendChance = 10;
        Attributes.LowerManaCost = 10;
        Attributes.LowerRegCost = 100;
    }
}
