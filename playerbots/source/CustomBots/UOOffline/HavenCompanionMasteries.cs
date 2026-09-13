using System;
using Server.Items;
using Server.Spells.SkillMasteries;

namespace Server.UOOffline;
public partial class HavenCompanion
{
    private DateTime _nextMasteryDecision;
    private DateTime _nextMasterySwitch;
    private SkillName _bardMastery = SkillName.Alchemy;

    internal SkillName ChooseBardMastery(Mobile enemy)
    {
        if (Skills.Musicianship.Value < 90) { return SkillName.Alchemy; }
        if (Skills.Peacemaking.Value >= 90 && BoundOwner?.Hits < BoundOwner?.HitsMax * 0.7) { return SkillName.Peacemaking; }
        if (!TamingAssistActive && Skills.Discordance.Value >= 90 && BardEnemy(enemy) && enemy.HitsMax >= 500) { return SkillName.Discordance; }
        if (Skills.Provocation.Value >= 90) { return SkillName.Provocation; }
        return Skills.Peacemaking.Value >= 90 ? SkillName.Peacemaking : SkillName.Alchemy;
    }

    internal void ThinkBardMasteries()
    {
        if (Role != HavenCompanionRole.Bard || IsDeadPet || BoundOwner?.Deleted != false || Spell != null || Target != null ||
            Core.Now < _nextMasteryDecision || Mana < 30 || !CanCastAt(BoundOwner, true)) { return; }
        _nextMasteryDecision = Core.Now + TimeSpan.FromSeconds(3);
        var enemy = Combatant as Mobile ?? BoundOwner.Combatant as Mobile;
        var desired = ChooseBardMastery(enemy);
        if (desired == SkillName.Alchemy) { return; }
        if (_bardMastery != desired && (Core.Now >= _nextMasterySwitch || _bardMastery == SkillName.Alchemy || TamingAssistActive && _bardMastery == SkillName.Discordance))
        {
            ClearBardMasteries();
            _bardMastery = desired;
            _nextMasterySwitch = Core.Now + TimeSpan.FromSeconds(30);
            foreach (var patient in _songRecipients) { if (!patient.Deleted) { RemoveSong(patient); } }
            _songRecipients.Clear();
        }
        var lute = Backpack?.FindItemByType<HavenCompanionLute>();
        if (lute == null) { return; }
        lute.UsesRemaining = 100;
        BaseInstrument.SetInstrument(this, lute);
        SkillMasterySpell next = _bardMastery switch
        {
            SkillName.Provocation when !SkillMasterySpell.HasSpell<InspireSpell>(this) => new InspireSpell(this, null),
            SkillName.Provocation when !SkillMasterySpell.HasSpell<InvigorateSpell>(this) => new InvigorateSpell(this, null),
            SkillName.Peacemaking when !SkillMasterySpell.HasSpell<ResilienceSpell>(this) => new ResilienceSpell(this, null),
            SkillName.Peacemaking when !SkillMasterySpell.HasSpell<PerseveranceSpell>(this) => new PerseveranceSpell(this, null),
            SkillName.Discordance when BardEnemy(enemy) && !SkillMasterySpell.HasSpell<TribulationSpell>(this) => new TribulationSpell(this, null),
            SkillName.Discordance when BardEnemy(enemy) && !SkillMasterySpell.HasSpell<DespairSpell>(this) => new DespairSpell(this, null),
            _ => null
        };
        if (next != null) { BeginCompanionSpell(next, next.PartyEffects ? this : enemy, next.PartyEffects); }
    }

    internal void ClearBardMasteries()
    {
        var spells = SkillMasterySpell.GetSpells(this);
        if (spells != null)
        {
            foreach (var spell in spells) { if (spell is BardSpell && spell.Timer != null) { spell.Expire(); } }
        }
        _bardMastery = SkillName.Alchemy;
    }
}
