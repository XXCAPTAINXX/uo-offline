using System;
using Server.Mobiles;

namespace Server.HavenPrototype
{

// Native pet orders, pathfinding and training hooks remain in charge. The drake
// maintains 4-6 tiles and fires natural lightning instead of requiring a bow/ammo.
public class HavenStormscaleAI : MeleeAI
{
    private DateTime _nextBolt;

    public HavenStormscaleAI(BaseCreature pet) : base(pet) { }

    public override bool DoActionCombat()
    {
        if (!m_Mobile.Controlled)
        {
            return base.DoActionCombat();
        }
        if (!HavenPetSignatures.Active(m_Mobile) || (m_Mobile.ControlOrder==OrderType.Stay || m_Mobile.ControlOrder==OrderType.Stop))
        {
            return true;
        }
        var target = m_Mobile.Combatant as Mobile;
        if (target?.Deleted != false || !target.Alive || target.Map != m_Mobile.Map || !m_Mobile.CanSee(target) || target.IsDeadBondedPet)
        {
            m_Mobile.Combatant = null;
            Action = ActionType.Guard;
            return true;
        }
        if (!m_Mobile.InRange(target, m_Mobile.RangePerception*2))
        {
            m_Mobile.Combatant = null;
            Action = ActionType.Guard;
            return true;
        }
        if (!m_Mobile.InLOS(target))
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
        if (m_Mobile.Combatant != target || DateTime.UtcNow < _nextBolt || !HavenPetSignatures.Enemy(m_Mobile, target) ||
            !m_Mobile.InRange(target, 6) || (m_Mobile.ControlOrder==OrderType.Stay || m_Mobile.ControlOrder==OrderType.Stop))
        {
            return false;
        }
        _nextBolt = DateTime.UtcNow + TimeSpan.FromSeconds(3);
        m_Mobile.Direction = m_Mobile.GetDirectionTo(target);
        m_Mobile.DoHarmful(target);
        target.BoltEffect(0);
        var damage = 16 + HavenPetSignatures.Tier(m_Mobile) * 4 + (int)(m_Mobile.Skills.Tactics.Value / 20);
        HavenPetSignatures.Damage(m_Mobile, target, damage, ResistanceType.Energy);
        m_Mobile.CheckSkill(SkillName.Tactics, 0, 150);
        HavenPetSignatures.Activate(m_Mobile, target);
        return true;
    }
}

}
