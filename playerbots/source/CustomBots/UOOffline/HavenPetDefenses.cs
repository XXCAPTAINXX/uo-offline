using System;
using Server.Mobiles;

namespace Server.UOOffline;

public static class HavenPetDefenses
{
    // Floors apply to the effective resistance, leaving saved training seeds untouched.
    public static int Resistance(BaseCreature pet, ResistanceType type, int normal)
    {
        var element = Element(pet);
        var tier = HavenPetSignatures.Tier(pet);
        if (element == type)
        {
            var floor = tier switch { 3 => 100, 2 => 90, 1 => 80, _ => 75 };
            return Math.Max(normal, floor);
        }
        if (pet is HavenSnowBear)
        {
            var floor = type switch
            {
                ResistanceType.Physical => 65 + tier * 5,
                ResistanceType.Cold => 75 + tier * 5,
                _ => int.MinValue
            };
            return Math.Max(normal, floor);
        }
        return normal;
    }

    private static ResistanceType? Element(BaseCreature pet) => pet switch
    {
        HavenEmberwing => ResistanceType.Fire,
        HavenFrostmane => ResistanceType.Cold,
        HavenStormscale => ResistanceType.Energy,
        _ => null
    };

    // AOS normally inflicts at least one damage even at 100 resistance. Only these
    // species' Legendary immunity bypasses that minimum; direct/armor-ignore remains effective.
    public static bool FullyImmune(Mobile target, int physical, int fire, int cold, int poison, int energy, int direct)
    {
        if (target is not BaseCreature pet || direct > 0 || physical > 0 || poison > 0 || HavenPetSignatures.Tier(pet) != 3)
        {
            return false;
        }
        return Element(pet) switch
        {
            ResistanceType.Fire => fire > 0 && cold == 0 && energy == 0,
            ResistanceType.Cold => cold > 0 && fire == 0 && energy == 0,
            ResistanceType.Energy => energy > 0 && fire == 0 && cold == 0,
            _ => false
        };
    }

    public static string Describe(BaseCreature pet)
    {
        var tier = HavenPetSignatures.Tier(pet);
        if (Element(pet) is ResistanceType element)
        {
            return tier == 3
                ? $"Innate {element} immunity (100%); other elements and armor-ignoring damage still work."
                : $"Innate {element} defense: at least {Resistance(pet, element, 0)}%. Legendary gains immunity.";
        }
        return pet is HavenSnowBear
            ? $"Thick winter hide: at least {65 + tier * 5}% physical and {75 + tier * 5}% cold resistance."
            : "";
    }
}

public partial class HavenEmberwing
{
    public override int GetResistance(ResistanceType type) => HavenPetDefenses.Resistance(this, type, base.GetResistance(type));
}
public partial class HavenFrostmane
{
    public override int GetResistance(ResistanceType type) => HavenPetDefenses.Resistance(this, type, base.GetResistance(type));
}
public partial class HavenStormscale
{
    public override int GetResistance(ResistanceType type) => HavenPetDefenses.Resistance(this, type, base.GetResistance(type));
    // Magery training stores AI_Mage in the creature's existing save data. Honor
    // that choice on both purchase and reload; otherwise keep the innate hunter AI.
    protected override BaseAI ForcedAI => AI == AIType.AI_Mage ? null : new HavenStormscaleAI(this);
}
public partial class HavenSnowBear
{
    public override int GetResistance(ResistanceType type) => HavenPetDefenses.Resistance(this, type, base.GetResistance(type));
}
