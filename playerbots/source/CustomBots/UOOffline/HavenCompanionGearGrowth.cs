using System;
using Server.Items;

namespace Server.UOOffline;

public static class HavenCompanionGearGrowth
{
    internal static int Level(Item item) => Math.Clamp(item switch
    {
        IEvolvingStarterWeapon weapon => weapon.Level,
        HavenLevelingCape cape => cape.Level,
        ApprenticeGrimoire book => book.Level,
        _ => HavenGearExperience.Find(item)?.Level ?? 1
    }, 1, 20);

    public static int GetBonus(Mobile wearer, AosAttribute attribute)
    {
        if (wearer is not HavenCompanion) { return 0; }
        if (attribute is not (AosAttribute.WeaponDamage or AosAttribute.SpellDamage or AosAttribute.RegenHits or
            AosAttribute.RegenStam or AosAttribute.RegenMana or AosAttribute.AttackChance or AosAttribute.DefendChance or AosAttribute.LowerManaCost)) { return 0; }
        var total = 0;
        foreach (var item in wearer.Items)
        {
            if (item is not IAosItem || !HavenGearExperience.IsSpecial(item)) { continue; }
            if (item is BaseWeapon && attribute is AosAttribute.SpellDamage or AosAttribute.LowerManaCost) { continue; }
            if (item is Spellbook && attribute is AosAttribute.WeaponDamage or AosAttribute.AttackChance) { continue; }
            var level = Level(item);
            total += attribute switch
            {
                AosAttribute.WeaponDamage or AosAttribute.SpellDamage => level / 5 * 2,
                AosAttribute.RegenHits or AosAttribute.RegenStam or AosAttribute.RegenMana or
                    AosAttribute.AttackChance or AosAttribute.DefendChance or AosAttribute.LowerManaCost => level / 10,
                _ => 0
            };
        }
        return total;
    }

    public static void AddProperties(Item item, IPropertyList list)
    {
        var level = Level(item);
        if (level < 5 || item is not IAosItem || !HavenGearExperience.IsSpecial(item)) { return; }
        var damage = item is BaseWeapon ? "% weapon damage" : item is Spellbook ? "% spell damage" : "% weapon and spell damage";
        list.Add($"{"Companion gear:"} {"+"}{level / 5 * 2}{damage}");
        if (level >= 10)
        {
            var benefits = item is BaseWeapon ? " HP/stamina/mana regen and hit/defense chance" :
                item is Spellbook ? " HP/stamina/mana regen, defense chance and lower mana cost" : " HP/stamina/mana regen, hit/defense chance and lower mana cost";
            list.Add($"{"Companion gear:"} {"+"}{level / 10}{benefits}");
        }
    }
    internal static void ApplySpellbook(Spellbook book)
    {
        if (!HavenGearExperience.IsSpecial(book)) { return; }
        var level = Level(book);
        book.Attributes.SpellDamage = Math.Max(book.Attributes.SpellDamage, level * 2);
        book.Attributes.CastSpeed = Math.Max(book.Attributes.CastSpeed, level / 10);
        book.Attributes.CastRecovery = Math.Max(book.Attributes.CastRecovery, level / 5);
        book.Attributes.LowerManaCost = Math.Max(book.Attributes.LowerManaCost, level / 2);
        book.Attributes.LowerRegCost = Math.Max(book.Attributes.LowerRegCost, level * 5);
        book.Attributes.RegenMana = Math.Max(book.Attributes.RegenMana, level / 5);
        book.Attributes.BonusMana = Math.Max(book.Attributes.BonusMana, level);
    }
}
