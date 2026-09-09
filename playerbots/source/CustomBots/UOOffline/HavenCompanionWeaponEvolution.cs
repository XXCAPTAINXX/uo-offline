using System;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

public static class HavenCompanionWeaponEvolution
{
    internal static int Level(BaseWeapon weapon) => weapon is IEvolvingStarterWeapon starter ? starter.Level : HavenGearExperience.Find(weapon)?.Level ?? 1;
    internal static void Apply(BaseWeapon weapon)
    {
        if (!HavenGearExperience.IsSpecial(weapon)) { return; }
        var level = Math.Clamp(Level(weapon), 1, 20);
        var chance = level / 5 * 10;
        weapon.Attributes.WeaponDamage = Math.Max(weapon.Attributes.WeaponDamage, level * 2);
        weapon.Attributes.WeaponSpeed = Math.Max(weapon.Attributes.WeaponSpeed, level / 5 * 5);
        weapon.WeaponAttributes.HitLowerAttack = Math.Max(weapon.WeaponAttributes.HitLowerAttack, chance);
        weapon.WeaponAttributes.HitLowerDefend = Math.Max(weapon.WeaponAttributes.HitLowerDefend, chance);
        weapon.WeaponAttributes.HitLeechMana = Math.Max(weapon.WeaponAttributes.HitLeechMana, chance);
        weapon.WeaponAttributes.HitLeechHits = Math.Max(weapon.WeaponAttributes.HitLeechHits, chance);
        weapon.WeaponAttributes.HitEnergyArea = Math.Max(weapon.WeaponAttributes.HitEnergyArea, chance);
    }
    public static bool Slays(BaseWeapon weapon, Mobile attacker, Mobile defender) =>
        attacker is HavenCompanion { IsDeadPet: false } && HavenGearExperience.IsSpecial(weapon) && weapon.Parent == attacker && Level(weapon) >= 20 &&
        defender is BaseCreature { Deleted: false, Alive: true, Controlled: false, Summoned: false } && attacker.CanBeHarmful(defender, false);
}
