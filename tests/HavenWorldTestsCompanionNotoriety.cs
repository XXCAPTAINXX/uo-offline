using System;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Fourth;
using Server.Spells.SkillMasteries;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsCompanionNotoriety
{
    public HavenWorldTestsCompanionNotoriety() => _ = new HavenWorldTests();

    [SkippableFact]
    public void CasterGreaterHealSupportsCriminalOwnerWithoutRestartingTheOriginalTimer()
    {
        TileDataRequirement.SkipIfMissing();
        using var scope = new NativeSupportScope();
        var owner = Player();
        var companion = Companion(owner, HavenCompanionRole.Caster);
        try
        {
            owner.Criminal = true;
            var expiry = CriminalExpiry(owner);
            Assert.Equal(Notoriety.Criminal, Notoriety.Compute(companion, owner));
            owner.Hits = owner.HitsMax / 4;
            Core._now += TimeSpan.FromSeconds(30);
            var spell = Assert.IsType<GreaterHealSpell>(companion.ChooseEmergencySpell(out var patient));
            Assert.Same(owner, patient);
            var hits = owner.Hits;
            CastHeal(spell, patient);

            Assert.True(owner.Hits > hits);
            Assert.True(owner.Criminal); // The original crime is preserved until its timer expires.
            Assert.Equal(expiry, CriminalExpiry(owner));
            Assert.False(companion.Criminal);
        }
        finally { companion.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void InvigorateTicksAtFullHealthDoNotRefreshOwnerOrSameOwnerPetCriminalTimers()
    {
        TileDataRequirement.SkipIfMissing();
        using var scope = new NativeSupportScope();
        var owner = Player();
        var companion = Companion(owner, HavenCompanionRole.Bard);
        var pet = new Horse();
        var song = new InvigorateSpell(companion, null);
        try
        {
            Assert.True(pet.SetControlMaster(owner));
            pet.MoveToWorld(owner.Location, owner.Map);
            foreach (var skill in new[] { SkillName.Provocation, SkillName.Musicianship, SkillName.Peacemaking, SkillName.Discordance })
            {
                companion.Skills[skill].Cap = 150;
                companion.Skills[skill].Base = 130;
            }
            var lute = new Lute();
            companion.Backpack.DropItem(lute);
            BaseInstrument.SetInstrument(companion, lute);
            owner.Criminal = true;
            pet.Criminal = true;
            var ownerExpiry = CriminalExpiry(owner);
            var petExpiry = CriminalExpiry(pet);
            Assert.True(song.CheckCast());
            companion.Spell = song;
            song.State = SpellState.Sequencing;
            song.OnCast();
            Assert.NotNull(song.Timer);
            Assert.Contains(owner, song.PartyList);
            Assert.Contains(pet, song.PartyList);
            owner.Hits = owner.HitsMax;
            pet.Hits = pet.HitsMax;

            for (var tick = 0; tick < 3; tick++)
            {
                Core._now += TimeSpan.FromSeconds(5);
                Assert.True(song.OnTick());
                Assert.Equal(ownerExpiry, CriminalExpiry(owner));
                Assert.Equal(petExpiry, CriminalExpiry(pet));
                Assert.True(owner.Criminal);
                Assert.True(pet.Criminal);
                Assert.False(companion.Criminal);
            }
        }
        finally { song.Expire(); pet.Delete(); companion.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void HelpingUnrelatedCriminalsAndHarmfulCriminalActionsStillFlagTheOwner()
    {
        TileDataRequirement.SkipIfMissing();
        using var scope = new NativeSupportScope();
        var owner = Player();
        var stranger = Player();
        var innocent = Player();
        var companion = Companion(owner, HavenCompanionRole.Caster);
        try
        {
            stranger.Criminal = true;
            stranger.Hits = stranger.HitsMax / 4;
            owner.Criminal = true;
            var oldExpiry = CriminalExpiry(owner);
            Core._now += TimeSpan.FromSeconds(30);
            Assert.True(companion.IsBeneficialCriminal(stranger));
            var hits = stranger.Hits;
            CastHeal(new GreaterHealSpell(companion), stranger);
            Assert.True(stranger.Hits > hits);
            Assert.True(companion.Criminal);
            Assert.True(owner.Criminal);
            Assert.True(CriminalExpiry(owner) > oldExpiry);

            owner.Criminal = false;
            companion.Criminal = false;
            Assert.True(companion.IsHarmfulCriminal(innocent));
            // Exercise the native penalty directly; AI target selection has separate restrictions.
            companion.DoHarmful(innocent);
            Assert.True(companion.Criminal);
            Assert.True(owner.Criminal);
        }
        finally { companion.Delete(); owner.Delete(); stranger.Delete(); innocent.Delete(); }
    }

    [SkippableFact]
    public void BeneficialExemptionRequiresCurrentSharedOwnershipAndDoesNotApplyToOrdinaryPets()
    {
        TileDataRequirement.SkipIfMissing();
        using var scope = new NativeSupportScope();
        var owner = Player();
        var stranger = Player();
        var companion = Companion(owner, HavenCompanionRole.Caster);
        var ownPet = new Horse();
        var otherPet = new Horse();
        var assignedPet = new Cat();
        try
        {
            Assert.True(ownPet.SetControlMaster(owner));
            Assert.True(otherPet.SetControlMaster(stranger));
            Assert.True(assignedPet.SetControlMaster(owner));
            assignedPet.MoveToWorld(owner.Location, owner.Map);
            companion.Skills.AnimalTaming.Base = 100;
            companion.Skills.AnimalLore.Base = 100;
            Assert.True(companion.AcceptAssignedPet(owner, assignedPet));
            owner.Criminal = true;
            stranger.Criminal = true;
            ownPet.Criminal = true;
            otherPet.Criminal = true;
            assignedPet.Criminal = true;
            Assert.False(companion.IsBeneficialCriminal(owner));
            Assert.False(companion.IsBeneficialCriminal(ownPet));
            Assert.True(companion.IsBeneficialCriminal(otherPet));
            Assert.True(ownPet.IsBeneficialCriminal(owner));
            Assert.Same(companion, assignedPet.ControlMaster);
            Assert.False(companion.IsBeneficialCriminal(assignedPet));
            var assignment = companion.Backpack.FindItemByType<HavenCompanionAssignedPet>();
            Assert.NotNull(assignment);
            assignment.Owner = stranger;
            Assert.True(companion.IsBeneficialCriminal(assignedPet));
            assignment.Owner = owner;

            companion.BoundOwner = stranger; // A stale bound-owner reference is not current ownership.
            Assert.True(companion.IsBeneficialCriminal(stranger));
            Assert.True(companion.IsBeneficialCriminal(otherPet));
            companion.BoundOwner = owner;
            ownPet.Controlled = false;
            Assert.True(companion.IsBeneficialCriminal(ownPet));
        }
        finally { assignedPet.Delete(); ownPet.Delete(); otherPet.Delete(); companion.Delete(); owner.Delete(); stranger.Delete(); }
    }

    private static PlayerMobile Player()
    {
        var player = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100, RawInt = 100 };
        player.AddItem(new Backpack());
        player.MoveToWorld(new Point3D(3500, 2570, 20), Map.Trammel);
        player.Hits = player.HitsMax;
        return player;
    }

    private static HavenCompanion Companion(PlayerMobile owner, HavenCompanionRole role)
    {
        var companion = new HavenCompanion { BoundOwner = owner, Role = role, RawInt = 500 };
        Assert.True(companion.SetControlMaster(owner));
        companion.MoveToWorld(owner.Location, owner.Map);
        companion.Skills.Magery.Cap = 150;
        companion.Skills.Magery.Base = 130;
        companion.Hits = companion.HitsMax;
        companion.Mana = companion.ManaMax;
        return companion;
    }

    private static void CastHeal(GreaterHealSpell spell, Mobile patient)
    {
        spell.Caster.Spell = spell;
        spell.State = SpellState.Sequencing;
        try { spell.Target(patient); }
        finally { spell.FinishSequence(); }
    }

    private static DateTime CriminalExpiry(Mobile mobile) =>
        ((TimerExecutionToken)typeof(Mobile).GetField("_expireCriminalTimerToken", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(mobile)).Next;

    private sealed class NativeSupportScope : IDisposable
    {
        private readonly DateTime _now = Core.Now;
        private readonly NotorietyHandler _notoriety = Notoriety.Handler;
        private readonly AllowBeneficialHandler _beneficial = Mobile.AllowBeneficialHandler;
        private readonly SkillCheckLocationHandler _skills = Mobile.SkillCheckLocationHandler;

        internal NativeSupportScope()
        {
            Notoriety.Handler = Server.Misc.NotorietyHandlers.MobileNotoriety;
            Mobile.AllowBeneficialHandler = Server.Misc.NotorietyHandlers.Mobile_AllowBeneficial;
            Mobile.SkillCheckLocationHandler = Server.Misc.SkillCheck.Mobile_SkillCheckLocation;
        }

        public void Dispose()
        {
            Notoriety.Handler = _notoriety;
            Mobile.AllowBeneficialHandler = _beneficial;
            Mobile.SkillCheckLocationHandler = _skills;
            Core._now = _now;
        }
    }
}
