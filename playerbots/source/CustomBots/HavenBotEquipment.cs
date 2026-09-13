using System;
using Server.Items;

namespace Server.CustomBots;

// ML item properties are valuable independently of the pre-AOS magic labels.
public static class HavenBotEquipment
{
    public static bool EquipIfBetter(PlayerBot bot, Item item)
    {
        if (item is not BaseWeapon weapon || item.RootParent != bot || bot.Backpack == null || bot.Spell != null) { return false; }
        if (bot.Skills[weapon.Skill].Base < 70 || bot.Skills[weapon.Skill].Base < bot.Skills.Magery.Base && bot.Class == BotClass.Mage) { return false; }
        if (bot.Weapon is not BaseWeapon current || current is Fists || PropertyValue(weapon) <= PropertyValue(current)) { return false; }
        // Do not strip a shield or an unrelated layer to fit a new loadout.
        var occupied = bot.FindItemOnLayer(weapon.Layer);
        if (occupied != null && occupied != current) { return false; }
        bot.Backpack.DropItem(current);
        if (bot.EquipItem(weapon)) { return true; }
        bot.EquipItem(current); return false;
    }

    public static int SpellWeight(Mobile target, string spell, int baseWeight)
    {
        if (!Core.AOS) { return baseWeight; }
        var element = spell.Contains("EnergyBolt", StringComparison.Ordinal) || spell.Contains("Lightning", StringComparison.Ordinal)
            ? ResistanceType.Energy : spell.Contains("Harm", StringComparison.Ordinal) || spell.Contains("MindBlast", StringComparison.Ordinal)
                ? ResistanceType.Cold : ResistanceType.Fire;
        return Math.Max(1, baseWeight * (100 - Math.Clamp(target.GetResistance(element), 0, 95)) / 20);
    }
    public static int PropertyValue(Item item)
    {
        if (!Core.AOS || item is not IAosItem gear) { return 0; }
        long score = 0;
        foreach (var attribute in Enum.GetValues<AosAttribute>())
        {
            var value = Math.Max(0, gear.Attributes[attribute]);
            var weight = attribute switch
            {
                AosAttribute.WeaponSpeed or AosAttribute.LowerManaCost => 180,
                AosAttribute.CastRecovery or AosAttribute.CastSpeed => 1200,
                AosAttribute.RegenMana or AosAttribute.RegenHits => 400,
                AosAttribute.Luck => 12,
                _ => 70
            };
            score += (long)value * weight;
        }
        if (item is BaseWeapon weapon)
        {
            foreach (var attribute in Enum.GetValues<AosWeaponAttribute>())
            { score += (long)Math.Max(0, weapon.WeaponAttributes[attribute]) * 65; }
            if (weapon.Slayer != SlayerName.None) { score += 2500; }
            if (weapon.Slayer2 != SlayerName.None) { score += 2500; }
        }
        if (item is BaseArmor armor)
        {
            foreach (var attribute in Enum.GetValues<AosArmorAttribute>()) { score += (long)Math.Max(0, armor.ArmorAttributes[attribute]) * 150; }
            score += (long)Math.Max(0, armor.PhysicalBonus + armor.FireBonus + armor.ColdBonus + armor.PoisonBonus + armor.EnergyBonus) * 160;
        }
        if (item is BaseJewel jewel)
        { for (var i = 0; i < 5; i++) { score += (long)(Math.Max(0, jewel.SkillBonuses.GetBonus(i)) * 300); } }
        return (int)Math.Min(score, 250000);
    }

    public static void RollMagic(Item item)
    {
        switch (item)
        {
            case BaseWeapon weapon: BaseRunicTool.ApplyAttributesTo(weapon, false, 0, Utility.RandomMinMax(3, 5), 35, 75); break;
            case BaseArmor armor: BaseRunicTool.ApplyAttributesTo(armor, false, 0, Utility.RandomMinMax(3, 5), 35, 75); break;
            case BaseJewel jewel: BaseRunicTool.ApplyAttributesTo(jewel, false, 0, Utility.RandomMinMax(3, 5), 35, 75); break;
            case Spellbook book: BaseRunicTool.ApplyAttributesTo(book, false, 0, Utility.RandomMinMax(2, 4), 35, 75); break;
        }
    }
}
