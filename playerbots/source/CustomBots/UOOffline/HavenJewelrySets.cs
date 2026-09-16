using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenSetRing : GoldRing
{
    [SerializableField(0)] private int _theme;
    [Constructible]
    public HavenSetRing(int theme = 0)
    {
        _theme = Math.Clamp(theme, 0, 8);
        var bracelet = (BaseJewel)SpecialBraceletFactory.Create(_theme);
        Name = $"{HavenJewelrySets.Names[_theme]} ring";
        Hue = bracelet.Hue;
        LootType = LootType.Blessed;
        foreach (var attribute in Enum.GetValues<AosAttribute>()) { Attributes[attribute] = bracelet.Attributes[attribute]; }
        foreach (var element in Enum.GetValues<AosElementAttribute>()) { Resistances[element] = bracelet.Resistances[element]; }
        for (var i = 0; i < 5; i++)
        {
            bracelet.SkillBonuses.GetValues(i, out var skill, out var value);
            if (value > 0) { SkillBonuses.SetValues(i, skill, value); }
        }
        bracelet.Delete();
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Matching bracelet bonus:"} {HavenJewelrySets.Descriptions[Theme]}");
        list.Add($"{"With Haven talisman:"} {"+250 Luck, +10% weapon and spell damage"}");
    }
}

[SerializationGenerator(0)]
public partial class HavenConcordTalisman : BaseTalisman
{
    internal void UnlockFollower(Mobile owner)
    {
        if (owner is not Server.Mobiles.PlayerMobile || owner is Server.CustomBots.PlayerBot || HavenGearExperience.Find(this)?.Level != 20) { return; }
        if (owner.FollowersMax < 6)
        {
            owner.FollowersMax = 6;
            owner.SendMessage("Your level-20 Haven talisman unlocks one permanent follower slot (capacity 6). This does not stack.");
        }
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (Parent == from || from.Backpack != null && IsChildOf(from.Backpack)) { UnlockFollower(from); }
        base.OnDoubleClick(from);
    }
    [Constructible]
    public HavenConcordTalisman() : base(0x2F5A)
    {
        Name = "Haven talisman of concord"; Hue = 0x489; LootType = LootType.Blessed;
        Attributes.Luck = 300;
        Attributes.BonusStr = Attributes.BonusDex = Attributes.BonusInt = 10;
        Attributes.RegenHits = Attributes.RegenStam = Attributes.RegenMana = 3;
        Attributes.WeaponDamage = Attributes.SpellDamage = 20;
        Attributes.LowerManaCost = 5; Attributes.LowerRegCost = 20;
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        HavenGearExperience.AddProperties(this, list);
        list.Add($"{"Level 20 unlock:"} {"+1 permanent follower capacity (6 total, does not stack); double-click to claim"}");
        list.Add($"{"Matching ring and bracelet:"} {"+250 Luck, +10% weapon and spell damage"}");
    }
}

public static class HavenJewelrySets
{
    public static readonly string[] Names = ["Vanguard", "Arcane Focus", "Wind", "Beastmaster", "Virtuoso", "Artisan", "Fortune", "Guardian", "Night"];
    public static readonly string[] Descriptions = ["+20% weapon damage, +2 hit regen", "+15% spell damage, +5% lower mana cost", "+10% swing speed, +3 stamina regen", "+150 Luck, +2 to all regens", "+10% spell damage, +3 mana regen", "+250 Luck, +20% lower reagent cost", "+350 Luck", "+10% defense chance, +3 hit regen", "+3 to all regens"];
    internal static int BraceletTheme(Item item) => item switch
    {
        BraceletOfTheVanguard => 0, BraceletOfArcaneFocus => 1, BraceletOfTheWind => 2,
        BraceletOfTheBeastmaster => 3, BraceletOfTheVirtuoso => 4, BraceletOfTheArtisan => 5,
        BraceletOfFortune => 6, BraceletOfTheGuardian => 7, BraceletOfTheNight => 8, _ => -1
    };
    public static int GetBonus(Mobile wearer, AosAttribute attribute)
    {
        if (wearer.FindItemOnLayer(Layer.Ring) is not HavenSetRing ring ||
            BraceletTheme(wearer.FindItemOnLayer(Layer.Bracelet)) != ring.Theme) { return 0; }
        var value = (ring.Theme, attribute) switch
        {
            (0, AosAttribute.WeaponDamage) => 20, (0, AosAttribute.RegenHits) => 2,
            (1, AosAttribute.SpellDamage) => 15, (1, AosAttribute.LowerManaCost) => 5,
            (2, AosAttribute.WeaponSpeed) => 10, (2, AosAttribute.RegenStam) => 3,
            (3, AosAttribute.Luck) => 150,
            (3, AosAttribute.RegenHits or AosAttribute.RegenStam or AosAttribute.RegenMana) => 2,
            (4, AosAttribute.SpellDamage) => 10, (4, AosAttribute.RegenMana) => 3,
            (5, AosAttribute.Luck) => 250, (5, AosAttribute.LowerRegCost) => 20,
            (6, AosAttribute.Luck) => 350,
            (7, AosAttribute.DefendChance) => 10, (7, AosAttribute.RegenHits) => 3,
            (8, AosAttribute.RegenHits or AosAttribute.RegenStam or AosAttribute.RegenMana) => 3,
            _ => 0
        };
        if (wearer.Talisman is HavenConcordTalisman)
        {
            value += attribute switch { AosAttribute.Luck => 250, AosAttribute.WeaponDamage or AosAttribute.SpellDamage => 10, _ => 0 };
        }
        return value;
    }
}
