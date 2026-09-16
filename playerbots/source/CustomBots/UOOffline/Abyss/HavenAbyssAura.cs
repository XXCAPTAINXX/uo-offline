using System;
using Server.Mobiles;

namespace Server.UOOffline;

public sealed class HavenAbyssAura : AreaEffectMonsterAbility
{
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.CombatAction;
    public override TimeSpan MinTriggerCooldown => TimeSpan.FromSeconds(5);
    public override TimeSpan MaxTriggerCooldown => TimeSpan.FromSeconds(5);
    protected override void DoEffectTarget(BaseCreature source, Mobile defender)
    {
        if (defender.Deleted || !source.InLOS(defender)) { return; }
        source.DoHarmful(defender); AOS.Damage(defender, source, 8, 0, 100, 0, 0, 0);
        defender.SendMessage("The daemon's intense heat burns you.");
    }
}
