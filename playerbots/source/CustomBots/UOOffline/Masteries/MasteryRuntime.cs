using System;
using System.Collections.Generic;
using Server.Collections;
using Server.Items;
namespace Server.Spells.SkillMasteries;
internal static class MasteryDamage
{
    // Reflective mastery damage must not recursively trigger another reflection.
    // All combat runs on the server's main loop.
    private static bool _applying;
    public static void Apply(Mobile target, Mobile source, DamageType type, ref int damage)
    {
        if (_applying || target?.Deleted != false || source?.Deleted != false || damage <= 0)
        {
            return;
        }
        _applying = true;
        try
        {
            if (WhiteTigerFormSpell.CheckEvasion(target)) { damage = 0; return; }
            SkillMasterySpell.OnDamage(target, source, type, ref damage);
            damage = Math.Max(0, damage);
        }
        finally
        {
            _applying = false;
        }
    }
}
internal static class MasteryLifecycle
{
    public static void Clear(Mobile mobile)
    {
        using var affected = Server.Collections.PooledRefList<SkillMasterySpell>.Create();
        foreach (var spell in SkillMasterySpell.EnumerateAllSpells())
        {
            if (spell.Caster == mobile || spell.Target == mobile) { affected.Add(spell); }
        }
        foreach (var spell in affected) { spell.Expire(); }
    }
}
internal static class MasteryCollections
{
    public static void IterateReverse<T>(this IList<T> list, Action<T> action)
    {
        for (var i = list.Count - 1; i >= 0; i--) { if (i < list.Count) { action(list[i]); } }
    }
    public static void IterateReverse<T>(this HashSet<T> set, Action<T> action)
    {
        using var snapshot = PooledRefList<T>.Create(set.Count);
        foreach (var value in set) { snapshot.Add(value); }
        for (var i = snapshot.Count - 1; i >= 0; i--) { action(snapshot[i]); }
    }
}
internal static class MasteryTargeting
{
    public static PooledRefList<Mobile> Acquire(Mobile caster, IPoint3D center, Map map, int range)
    {
        var targets = PooledRefList<Mobile>.Create();
        if (caster?.Deleted != false || center == null || map == null || map == Map.Internal) { return targets; }
        foreach (var mobile in map.GetMobilesInRange<Mobile>(new Point3D(center), range))
        {
            if (mobile != caster && !mobile.Deleted && mobile.Alive && caster.InLOS(mobile) &&
                caster.CanBeHarmful(mobile, false) && SpellHelper.ValidIndirectTarget(caster, mobile)) { targets.Add(mobile); }
        }
        return targets;
    }
    public static int SpellDamageBonus(Mobile caster, Mobile target, SkillName skill, bool pvp)
    {
        var bonus = AosAttributes.GetValue(caster, AosAttribute.SpellDamage);
        return Core.SE && pvp ? Math.Min(15, bonus) : bonus;
    }
}
