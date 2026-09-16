using System;
using Server.Mobiles;

namespace Server.UOOffline;

// Native pet orders, pathfinding and training hooks remain in charge. The drake
// maintains 4-6 tiles and fires natural lightning instead of requiring a bow/ammo.
public class HavenStormscaleAI : MeleeAI
{
    private DateTime _nextBolt;

    public HavenStormscaleAI(BaseCreature pet) : base(pet) { }

    public override bool DoActionCombat()
    {
        if (!Mobile.Controlled)
        {
            return base.DoActionCombat();
        }
        if (!HavenPetSignatures.Active(Mobile) || Mobile.ControlOrder is OrderType.Stay or OrderType.Stop)
        {
            return true;
        }
        var target = Mobile.Combatant;
        if (target?.Deleted != false || !target.Alive || target.Map != Mobile.Map || !Mobile.CanSee(target) || target.IsDeadBondedPet)
        {
            Mobile.Combatant = null;
            Action = ActionType.Guard;
            return true;
        }
        if (!Mobile.InRange(target, Mobile.ChaseLeashRange))
        {
            Mobile.Combatant = null;
            Action = ActionType.Guard;
            return true;
        }
        if (!Mobile.InLOS(target))
        {
            MoveTo(target, false, 1);
            return true;
        }
        WalkMobileRange(target, 1, false, 4, 6);
        Fire(target);
        return true;
    }

    internal bool Fire(Mobile target)
    {
        if (Mobile.Combatant != target || Core.Now < _nextBolt || !HavenPetSignatures.Enemy(Mobile, target) ||
            !Mobile.InRange(target, 6) || Mobile.ControlOrder is OrderType.Stay or OrderType.Stop)
        {
            return false;
        }
        _nextBolt = Core.Now + TimeSpan.FromSeconds(3);
        Mobile.Direction = Mobile.GetDirectionTo(target);
        Mobile.DoHarmful(target);
        target.BoltEffect(0);
        var damage = 16 + HavenPetSignatures.Tier(Mobile) * 4 + (int)(Mobile.Skills.Tactics.Value / 20);
        HavenPetSignatures.Damage(Mobile, target, damage, ResistanceType.Energy);
        Mobile.CheckSkill(SkillName.Tactics, 0, 150);
        HavenPetSignatures.Activate(Mobile, target);
        return true;
    }
}
