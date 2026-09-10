using System;
using Server.Mobiles;
using Server.SkillHandlers;
using Server.Targeting;

namespace Server.UOOffline;

public partial class HavenCompanion
{
    private BaseCreature _tamingTarget;
    private bool _calmingAnimal;
    private DateTime _nextTamingPeace;
    internal bool TamingAssistActive => _tamingTarget != null;

    internal bool StartTamingAssist(Mobile owner, BaseCreature animal)
    {
        if (Deleted || IsDeadPet || owner != BoundOwner || Role != HavenCompanionRole.Bard ||
            animal is not { Deleted: false, Alive: true, Tamable: true, Controlled: false, Summoned: false, BardImmune: false } ||
            owner.Map != Map || !InRange(owner, 12) || animal.Map != Map || !InRange(animal, 12) || !InLOS(animal)) { return false; }
        if (Skills.AnimalTaming.Value<animal.MinTameSkill || Skills.AnimalLore.Value<animal.MinTameSkill ||
            Followers+animal.ControlSlots>FollowersMax) { owner.SendMessage("I need higher Taming/Lore or free follower capacity for this animal."); return false; }
        _tamingTarget = animal;
        _nextTamingPeace = Core.Now;
        _nextTamingAttempt = Core.Now;
        Combatant = null;
        ControlTarget = owner;
        ControlOrder = OrderType.Follow;
        return true;
    }

    internal void StopTamingAssist() => _tamingTarget = null;

    internal void ThinkTamingAssist()
    {
        ThinkAssignedPets();
        if (_tamingTarget == null) { return; }
        if (Role != HavenCompanionRole.Bard || IsDeadPet || BoundOwner == null || BoundOwner.Map != Map ||
            !InRange(BoundOwner, 18) || _tamingTarget.Deleted || !_tamingTarget.Alive || _tamingTarget.Controlled ||
            !_tamingTarget.Tamable || _tamingTarget.Map != Map || !InRange(_tamingTarget, 18))
        { StopTamingAssist(); return; }
        Combatant = null;
        ControlTarget = _tamingTarget;
        ControlOrder = OrderType.Follow;
        if (_tamingTarget.BardPacified) { TryAssistedTaming(); return; }
        if ( Core.Now < _nextTamingPeace || !InRange(_tamingTarget, 10) || !InLOS(_tamingTarget)) { return; }
        var lute = Backpack?.FindItemByType<HavenCompanionLute>();
        if (lute == null) { return; }
        _nextTamingPeace = Core.Now + TimeSpan.FromSeconds(12);
        lute.UsesRemaining = 100;
        _calmingAnimal = true;
        try
        {
            Peacemaking.OnPickedInstrument(this, lute);
            Target?.Invoke(this, _tamingTarget);
            if (_tamingTarget?.BardPacified == true) { AwardHelpfulAction(); TryAssistedTaming(); }
        }
        finally { _calmingAnimal = false; }
    }

    internal void RequestTamingAssist(Mobile owner)
    {
        if (owner != BoundOwner) { return; }
        if (TamingAssistActive) { StopTamingAssist(); owner.SendMessage("Taming assistance stopped."); return; }
        if (Role != HavenCompanionRole.Bard) { owner.SendMessage("Choose the Bard role first, then Tame assist."); return; }
        owner.SendMessage("Choose a wild animal. I will calm it, attempt to tame it, and return the actual animal as a claim ticket.");
        owner.Target = new TamingAssistTarget(this);
    }

    private sealed class TamingAssistTarget(HavenCompanion companion) : Target(12, false, TargetFlags.None)
    {
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not BaseCreature animal || !companion.StartTamingAssist(from, animal))
            { from.SendMessage("Choose a nearby, visible, wild tamable animal that can be calmed."); }
            else { from.SendMessage("Taming assistance active. I will peace and tame the animal; Stop assist cancels my attempt."); }
            HavenCompanionGump.DisplayTo(from, companion);
        }
    }
}
