using System;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Fourth;
using Server.Spells.Eighth;
using Server.Targeting;

namespace Server.CustomBots;

// One bounded cast owned by the party behavior, using native spell/target checks.
internal sealed class HavenPartySupport
{
    private PlayerBot _bot;
    private Mobile _patient;
    private Spell _spell;
    private Timer _timer;
    private DateTime _deadline, _nextAttempt;
    internal static bool Ally(PlayerBot bot, Mobile patient)
    {
        if (bot?.Deleted != false || patient?.Deleted != false || Party.Get(bot) is not { } party) { return false; }
        var owner = patient is BaseCreature pet ? pet.GetMaster() : patient;
        return owner != null && Party.Get(owner) == party;
    }
    internal static int Need(PlayerBot bot, Mobile patient)
    {
        if (!Ally(bot, patient) || patient.Map != bot.Map || !bot.InRange(patient, 12) || !bot.InLOS(patient) || !bot.CanBeBeneficial(patient, false, true)) { return 0; }
        if (!patient.Alive || patient is BaseCreature { IsDeadPet: true }) { return bot.Combatant == null ? 500 : 0; }
        var hp = patient.HitsMax <= 0 ? 100 : patient.Hits * 100 / patient.HitsMax;
        if (hp < 40) { return 1000 - hp; }
        if (patient.Poisoned) { return 850; }
        return hp < 80 ? 800 - hp : 0;
    }
    internal static Spell ChooseSpell(PlayerBot bot, Mobile patient)
    {
        if (Need(bot, patient) == 0 || patient is BaseCreature { IsDeadPet: true }) { return null; }
        var magery = bot.Skills.Magery.Value;
        if (!patient.Alive) { return magery >= 90 && bot.Mana >= 50 ? new ResurrectionSpell(bot) : null; }
        if (patient.Poisoned) { return magery >= 30 && bot.Mana >= 12 ? new CureSpell(bot) : null; }
        if (patient.Hits < patient.HitsMax * .8)
        {
            if (magery >= 65 && bot.Mana >= 17) { return new GreaterHealSpell(bot); }
            if (magery >= 15 && bot.Mana >= 10) { return new HealSpell(bot); }
        }
        return null;
    }
    internal bool Tick(PlayerBot bot)
    {
        if (_spell != null) { return true; }
        if (bot.Deleted || !bot.Alive || bot.Spell != null || bot.Target != null || Core.Now < _nextAttempt || bot.Map == null || bot.Map == Map.Internal) { return false; }
        Mobile best = null; Spell selected = null; var score = 0;
        foreach (var patient in bot.GetMobilesInRange(12))
        {
            var need = Need(bot, patient); if (need <= score) { continue; }
            var spell = ChooseSpell(bot, patient);
            if (spell == null && !CanBandage(bot, patient)) { continue; }
            best = patient; selected = spell; score = need;
        }
        if (best == null) { return false; }
        _nextAttempt = Core.Now + TimeSpan.FromSeconds(2);
        if (selected == null)
        {
            var bandage = bot.Backpack.FindItemByType<Bandage>();
            Bandage.BandageTargetRequest(bot, bandage, best); return false;
        }
        if (!selected.Cast()) { return false; }
        _bot = bot; _patient = best; _spell = selected; _deadline = Core.Now + TimeSpan.FromSeconds(8);
        _timer = Timer.DelayCall(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200), Poll);
        return true;
    }
    internal static bool CanBandage(PlayerBot bot, Mobile patient)
    {
        if (!Ally(bot, patient) || !bot.InRange(patient, 2) || bot.Backpack?.FindItemByType<Bandage>() == null || BandageContext.GetContext(bot) != null) { return false; }
        var veterinary = patient is BaseCreature;
        var skill = veterinary ? bot.Skills.Veterinary.Value : bot.Skills.Healing.Value;
        var knowledge = veterinary ? bot.Skills.AnimalLore.Value : bot.Skills.Anatomy.Value;
        return patient.Alive && patient is not BaseCreature { IsDeadPet: true } ? skill >= 40 : skill >= 80 && knowledge >= 80;
    }
    private void Poll()
    {
        if (_bot?.Deleted != false || !_bot.Alive || _patient?.Deleted != false || !Ally(_bot, _patient) || _patient.Map != _bot.Map ||
            !_bot.InRange(_patient, 12) || !_bot.InLOS(_patient) || Core.Now >= _deadline) { Cancel(); return; }
        if (_bot.Spell != _spell) { Finish(); return; }
        if (_bot.Target is { } target)
        { target.Invoke(_bot, _patient); Finish(); }
    }
    internal void Cancel()
    {
        if (_bot?.Deleted == false && _bot.Spell == _spell && _spell != null)
        { _bot.Target?.Cancel(_bot, TargetCancelType.Canceled); _spell.Disturb(DisturbType.EquipRequest); }
        Finish();
    }
    private void Finish()
    { _timer?.Stop(); _timer = null; _bot = null; _patient = null; _spell = null; _nextAttempt = Core.Now + TimeSpan.FromSeconds(2); }
}
