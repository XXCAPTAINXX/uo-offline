using System;
using System.Collections.Generic;
using Server.Engines.BuffIcons;
using Server.Items;
using Server.Mobiles;
using Server.SkillHandlers;

namespace Server.UOOffline;

public partial class HavenCompanion
{
    private readonly HashSet<Mobile> _songRecipients = new();
    private DateTime _nextBardCombat;
    internal string SelectSong(Mobile patient)
    {
        if (Skills.Musicianship.Value < 90) { return "Encouragement"; }
        if (patient.Hits < patient.HitsMax * 0.6) { return "Recovery"; }
        if (patient.Mana < patient.ManaMax * 0.5 || patient.Skills.Magery.Value > patient.Skills.Tactics.Value) { return "Arcane"; }
        return patient.Weapon is BaseRanged ? "Battle" : "Valor";
    }
    internal static BuffInfo SongBuff(string song, int strength, int dexterity, int intelligence) => new(
        song == "Recovery" ? BuffIcon.Invigorate : song == "Arcane" ? BuffIcon.Resilience : BuffIcon.Inspire,
        1042971, 1042971, TimeSpan.FromSeconds(60),
        $"Companion {song} song: +{strength} Str, +{dexterity} Dex, +{intelligence} Int");
    internal void BardSupport(Mobile patient)
    {
        if (Role != HavenCompanionRole.Bard || !SongUnlocked || IsDeadPet || !patient.Alive || patient.Map != Map ||
            !InRange(patient, 12) || !InLOS(patient)) { return; }
        var skill = Math.Min(Skills.Musicianship.Value, Skills.Peacemaking.Value);
        var amount = 3 + (int)((skill - 80) / 10) + (int)Math.Log2(1 + Math.Max(0, TrainingMinutes) / 60);
        var song = SelectSong(patient);
        var strength = amount * (song is "Recovery" or "Valor" ? 2 : 1);
        var dexterity = amount * (song == "Battle" ? 2 : 1);
        var intelligence = amount * (song == "Arcane" ? 2 : 1);
        RemoveSong(patient);
        patient.AddStatMod(new StatMod(StatType.Str, "HavenCompanionSongStr", strength, TimeSpan.FromSeconds(60)));
        patient.AddStatMod(new StatMod(StatType.Dex, "HavenCompanionSongDex", dexterity, TimeSpan.FromSeconds(60)));
        patient.AddStatMod(new StatMod(StatType.Int, "HavenCompanionSongInt", intelligence, TimeSpan.FromSeconds(60)));
        (patient as PlayerMobile)?.AddBuff(SongBuff(song, strength, dexterity, intelligence));
        _songRecipients.Add(patient);
    }
    private static void RemoveSong(Mobile patient)
    {
        patient.RemoveStatMod("HavenCompanionSongStr"); patient.RemoveStatMod("HavenCompanionSongDex"); patient.RemoveStatMod("HavenCompanionSongInt");
        if (patient is PlayerMobile player)
        {
            player.RemoveBuff(BuffIcon.Inspire); player.RemoveBuff(BuffIcon.Invigorate); player.RemoveBuff(BuffIcon.Resilience);
        }
    }
    internal void ClearSongs()
    {
        foreach (var patient in _songRecipients) { if (!patient.Deleted) { RemoveSong(patient); } }
        _songRecipients.Clear();
    }
    private bool BardEnemy(Mobile target) => target is BaseCreature { Alive: true, Deleted: false, IsDeadPet: false, Controlled: false, Summoned: false, BardImmune: false } creature &&
        creature.Karma < 0 && target.Map == Map && InRange(target, 10) && InLOS(target) && CanBeHarmful(target, false);
    internal void ThinkBardCombat(Mobile enemy)
    {
        if (Role != HavenCompanionRole.Bard || IsDeadPet || BoundOwner == null || Core.Now < _nextBardCombat || !BardEnemy(enemy)) { return; }
        var lute = Backpack?.FindItemByType<HavenCompanionLute>();
        if (lute == null) { return; }
        _nextBardCombat = Core.Now + TimeSpan.FromSeconds(20);
        lute.UsesRemaining = 100;
        if (Skills.Peacemaking.Value >= 75 && BoundOwner.Hits < BoundOwner.HitsMax * 0.4 && enemy is BaseCreature { BardPacified: false } threatened)
        {
            Peacemaking.OnPickedInstrument(this, lute);
            Target?.Invoke(this, enemy);
            if (threatened.BardPacified) { Combatant = null; ControlTarget = BoundOwner; ControlOrder = OrderType.Follow; AwardHelpfulAction(); }
            return;
        }
        if (Skills.Provocation.Value >= 75 && enemy is BaseCreature { BardProvoked: false, Unprovokable: false })
        {
            foreach (var other in Map.GetMobilesInRange<BaseCreature>(Location, 10))
            {
                if (other == enemy || other.Unprovokable || other.BardProvoked || !BardEnemy(other) ||
                    other.Combatant != BoundOwner && other.Combatant != this) { continue; }
                Provocation.OnPickedInstrument(this, lute);
                var first = Target;
                first?.Invoke(this, enemy);
                if (Target != null && Target != first) { Target.Invoke(this, other); }
                if (((BaseCreature)enemy).BardProvoked) { AwardHelpfulAction(); }
                return;
            }
        }
        TryDiscord(enemy);
    }
}
