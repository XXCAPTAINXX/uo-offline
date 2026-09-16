using System;
using Server.Items;

namespace Server.UOOffline;

public static class HavenAstralGrowth
{
    // Floors preserve already-earned bonuses and make upgrades to existing rewards idempotent.
    internal static void Apply(Item item, int level)
    {
        var steps = Math.Clamp(level, 1, 20) - 1;
        if (item is not IAosItem gear) { return; }
        var a = gear.Attributes;
        switch (item)
        {
            case AstralWeaversRing:
                a.SpellDamage = Math.Max(a.SpellDamage, 30 + steps * 2);
                a.LowerManaCost = Math.Max(a.LowerManaCost, 10 + steps / 4);
                a.RegenMana = Math.Max(a.RegenMana, 3 + steps / 3);
                a.CastRecovery = Math.Max(a.CastRecovery, steps / 6);
                break;
            case AstralGuardianMantle:
                a.RegenHits = Math.Max(a.RegenHits, 5 + steps / 3);
                a.RegenStam = Math.Max(a.RegenStam, steps / 3);
                a.DefendChance = Math.Max(a.DefendChance, 15 + steps / 2);
                a.BonusHits = Math.Max(a.BonusHits, steps * 2);
                break;
            case AstralFortuneEarrings:
                a.RegenMana = Math.Max(a.RegenMana, 4 + steps / 3);
                a.Luck = Math.Max(a.Luck, 400 + steps * 20);
                a.WeaponDamage = Math.Max(a.WeaponDamage, steps);
                a.SpellDamage = Math.Max(a.SpellDamage, steps);
                break;
        }
    }
}
