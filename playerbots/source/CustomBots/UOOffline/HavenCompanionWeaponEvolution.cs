using System;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

public static class HavenCompanionWeaponEvolution
{
    internal static int Level(BaseWeapon weapon) => weapon is IEvolvingStarterWeapon starter ? starter.Level : HavenGearExperience.Find(weapon)?.Level ?? 1;
    internal static void Apply(BaseWeapon weapon)
    {
        var chance = Math.Clamp(Level(weapon) / 5 * 10, 0, 40);
        weapon.WeaponAttributes.HitEnergyArea = Math.Max(weapon.WeaponAttributes.HitEnergyArea, chance);
    }
    public static bool Slays(BaseWeapon weapon, Mobile attacker, Mobile defender) =>
        attacker is HavenCompanion { IsDeadPet: false } && weapon.Parent == attacker && Level(weapon) >= 20 &&
        defender is BaseCreature { Deleted: false, Alive: true, Controlled: false, Summoned: false } && attacker.CanBeHarmful(defender, false);
}
