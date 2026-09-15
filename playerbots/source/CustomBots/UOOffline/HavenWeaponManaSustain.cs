using System;
using Server.Items;

namespace Server.UOOffline;

public static class HavenWeaponManaSustain
{
    internal static int Chance(int level) => 20 + (Math.Clamp(level,1,20)-1)*80/19;
    internal static void Apply(BaseWeapon weapon,int level)
        => weapon.WeaponAttributes.HitLeechMana = Math.Max(weapon.WeaponAttributes.HitLeechMana,Chance(level));
}
