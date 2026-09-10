using System;
using Server.Mobiles;

namespace Server.UOOffline;

public static class HavenPetAppearance
{
    // Existing client hues, checked against 7.0.23.1/hues.mul. Each family goes
    // from a dark coat through saturated color to a lighter Legendary tone.
    private static readonly int[][] Palettes =
    [
        [],
        [0x21E, 0x1BB, 0x02D, 0x02E], // Emberwing: copper/ember
        [0x1FB, 0x198, 0x00A, 0x137], // Moonfang: lunar violet
        [0x24B, 0x1E8, 0x05A, 0x05B], // Frostmane: glacier cyan
        [0x237, 0x1D4, 0x046, 0x047], // Verdant: forest green
        [0x255, 0x1F2, 0x064, 0x191], // Stormscale: lightning blue
        [0x200, 0x19D, 0x00F, 0x074], // Stormhorn: arcane orchid
        [0x250, 0x1ED, 0x05F, 0x0C4], // Frostbound bear: winter blue
        [0x219, 0x1B6, 0x028, 0x029], // Ancient hellhound: volcanic red
        [0x214, 0x1B1, 0x023, 0x0EC], // Vampiric steed: blood crimson
        [0x237, 0x1D4, 0x046, 0x047]  // Chelonian: olive to jade
    ];

    public static int NaturalHue(BaseCreature pet)
    {
        var kind = HavenPetSignatures.Kind(pet);
        return kind == 0 ? pet.Hue : Palettes[kind][HavenPetSignatures.Tier(pet)];
    }

    public static void Refresh(BaseCreature pet)
    {
        if (pet?.Deleted != false || HavenPetSignatures.Kind(pet) == 0) { return; }
        var hue = NaturalHue(pet);
        var dye = pet.Backpack?.FindItemByType<HavenPetDyeRecord>();
        if (dye != null)
        {
            if (dye.OriginalHue != hue) { dye.OriginalHue = hue; }
        }
        else if (pet.Hue != hue) { pet.Hue = hue; }
    }

    internal static bool Shimmer(BaseCreature pet)
    {
        if (HavenPetSignatures.Tier(pet) != 3 || !HavenPetSignatures.Active(pet) ||
            pet.Hidden || pet.ControlMaster.Hidden || pet.Combatant != null) { return false; }
        var state = HavenPetSignatures.Ensure(pet);
        if (Core.Now < state.NextShimmer) { return false; }
        state.NextShimmer = Core.Now + TimeSpan.FromSeconds(45);
        pet.FixedEffect(0x373A, 10, 12, NaturalHue(pet), 0);
        return true;
    }
}
