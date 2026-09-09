using System;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Sixth;
using Server.Spells.Second;
using Server.Spells.Spellweaving;
using Server.Targeting;

namespace Server.UOOffline;

public partial class HavenCompanion
{
    private DateTime _nextCasterSpell = Core.Now;
    private DateTime _nextResourceRecovery = Core.Now;
    private DateTime _nextLifeGift = Core.Now;
    private Spell _companionSpell;
    private Mobile _companionSpellTarget;
    private bool _beneficialSpell;

    public override int ManaMax => Role == HavenCompanionRole.Caster
        ? base.ManaMax + 140 + (int)Math.Min(100000, TrainingLevel * 2) : base.ManaMax;

    internal bool CanCastAt(Mobile target, bool beneficial) => Role == HavenCompanionRole.Caster && !IsDeadPet &&
        Alive && target?.Deleted == false && target.Alive && target.Map == Map && InRange(target, 10) && InLOS(target) &&
        (beneficial ? target == BoundOwner || target == this : CanBeHarmful(target, false));

    internal Spell ChooseAttackSpell(Mobile enemy)
    {
        if (!CanCastAt(enemy, false)) { return null; }
        if (Skills.Spellweaving.Value >= 80 && Mana >= 70 && enemy.Hits < enemy.HitsMax * 0.30)
        {
            return new WordOfDeathSpell(this);
        }
        if (Skills.Spellweaving.Value >= 10 && Mana >= 52 && NearbyCasterEnemies() >= 2) { return new ThunderstormSpell(this); }
        if (Skills.Magery.Value >= 70 && Mana >= 30) { return new EnergyBoltSpell(this); }
        if (Skills.Magery.Value >= 45 && Mana >= 21) { return new LightningSpell(this); }
        return Mana >= 14 ? new MagicArrowSpell(this) : null;
    }
    internal int NearbyCasterEnemies()
    {
        var count = 0;
        foreach (var enemy in Map.GetMobilesInRange<Server.Mobiles.BaseCreature>(Location, 8))
        {
            if (enemy != this && enemy.Alive && !enemy.Controlled && !enemy.Summoned && enemy is not Server.Mobiles.BaseVendor &&
                CanCastAt(enemy, false) && SpellHelper.ValidIndirectTarget(this, enemy))
            { if (++count >= 2) { break; } }
        }
        return count;
    }
    internal Spell ChooseEmergencySpell(out Mobile patient)
    {
        patient = BoundOwner;
        if (CanCastAt(BoundOwner, true) && BoundOwner.Poisoned && Mana >= 11) { return new CureSpell(this); }
        if (Poisoned && Mana >= 11) { patient = this; return new CureSpell(this); }
        if (CanCastAt(BoundOwner, true) && BoundOwner.Hits < BoundOwner.HitsMax * 0.65 && Mana >= 21) { return new GreaterHealSpell(this); }
        if (Hits < HitsMax * 0.50 && Mana >= 21) { patient = this; return new GreaterHealSpell(this); }
        return null;
    }

    private bool BeginCompanionSpell(Spell spell, Mobile target, bool beneficial)
    {
        if (spell == null || Spell != null || Target != null || !CanCastAt(target, beneficial) || !spell.Cast()) { return false; }
        _companionSpell = spell;
        _companionSpellTarget = target;
        _beneficialSpell = beneficial;
        _nextCasterSpell = Core.Now + TimeSpan.FromSeconds(0.5);
        return true;
    }

    private void ProcessCompanionSpell()
    {
        if (_companionSpell == null) { return; }
        if (BoundOwner?.NetState == null || !CanCastAt(_companionSpellTarget, _beneficialSpell))
        {
            CancelCompanionSpell();
            return;
        }
        if (Target != null)
        {
            var target = _companionSpellTarget;
            _companionSpell = null;
            _companionSpellTarget = null;
            Target.Invoke(this, target);
        }
        else if (Spell != _companionSpell)
        {
            _companionSpell = null;
            _companionSpellTarget = null;
        }
    }

    private void CancelCompanionSpell()
    {
        if (_companionSpell != null)
        {
            Target?.Cancel(this, TargetCancelType.Canceled);
            _companionSpell.Disturb(DisturbType.Kill);
        }
        _companionSpell = null;
        _companionSpellTarget = null;
    }

    internal void RecoverResources(DateTime now)
    {
        if (IsDeadPet || !Alive || now < _nextResourceRecovery) { return; }
        _nextResourceRecovery = now + TimeSpan.FromSeconds(3);
        var growth = (int)Math.Min(40, TrainingLevel / 5);
        if (!Poisoned) { Hits = Math.Min(HitsMax, Hits + 4 + growth); }
        Stam = Math.Min(StamMax, Stam + 12 + growth);
        Mana = Math.Min(ManaMax, Mana + 8 + growth);
    }
    private void ThinkAsCaster()
    {

        if (Role != HavenCompanionRole.Caster) { return; }
        if (Spell != null || Target != null || Core.Now < _nextCasterSpell || Core.TickCount - NextSpellTime < 0) { return; }
        var emergency = ChooseEmergencySpell(out var patient);
        if (emergency != null && BeginCompanionSpell(emergency, patient, true)) { return; }
        if (BoundOwner.Alive && BoundOwner.Hits < BoundOwner.HitsMax * 0.85 && Mana >= 34 &&
            CanBeginAction<GiftOfRenewalSpell>() && BeginCompanionSpell(new GiftOfRenewalSpell(this), BoundOwner, true)) { return; }
        if (BoundOwner.Alive && BoundOwner.Combatant == null && Skills.Spellweaving.Value >= 80 &&
            Core.Now >= _nextLifeGift && Mana >= 90 && BeginCompanionSpell(new GiftOfLifeSpell(this), BoundOwner, true))
        {
            _nextLifeGift = Core.Now + TimeSpan.FromMinutes(2);
            return;
        }
        var enemy = Combatant as Mobile ?? BoundOwner.Combatant as Mobile;
        if (enemy != null) { BeginCompanionSpell(ChooseAttackSpell(enemy), enemy, false); }
    }
}
