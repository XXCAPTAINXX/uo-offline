using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.SkillMasteries;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsMasteries
{
    public HavenWorldTestsMasteries()
    {
        _ = new HavenWorldTests();
        MasteryInfo.Configure();
    }

    [SkippableFact]
    public void BookAndEveryPrimerAreAvailableAndLearningPersists()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true };
        owner.AddItem(new Backpack());
        try
        {
            var book = Assert.IsType<BookOfMasteries>(HavenTrainingStone.Menu.CreateMastery(0));
            owner.Backpack.DropItem(book);
            Assert.Equal(45, book.BookCount);
            Assert.Equal(1000, HavenTrainingStone.Menu.MasteryPrice(0));
            Assert.Equal(2500, HavenTrainingStone.Menu.MasteryPrice(1));
            Assert.Equal(7500, HavenTrainingStone.Menu.MasteryPrice(2));
            Assert.Equal(15000, HavenTrainingStone.Menu.MasteryPrice(3));
            Assert.Equal(SpellbookType.SkillMasteries, Spellbook.GetTypeForSpell(700));
            Assert.Equal(SpellbookType.SkillMasteries, Spellbook.GetTypeForSpell(744));
            for (var i = 0; i < MasteryInfo.Skills.Length; i++)
            {
                var skill = MasteryInfo.Skills[i];
                owner.Skills[skill].Base = 100;
                for (var volume = 1; volume <= 3; volume++)
                {
                    var primer = Assert.IsType<SkillMasteryPrimer>(HavenTrainingStone.Menu.CreateMastery(1 + i * 3 + volume - 1));
                    owner.Backpack.DropItem(primer);
                    Assert.Equal(skill, primer.Skill);
                    Assert.Equal(volume, primer.Volume);
                    Assert.True(primer.Learn(owner));
                    Assert.True(primer.Deleted);
                    Assert.Equal(volume, MasteryInfo.GetMasteryLevel(owner, skill));
                }
            }
            Assert.True(BookOfMasteries.Select(owner, SkillName.Spellweaving));
            Assert.False(BookOfMasteries.Select(owner, SkillName.Magery));
            var record = MasteryProgress.Find(owner);
            var writer = new BufferWriter(true);
            record.Serialize(writer);
            var copy = new MasteryProgress(World.NewItem);
            try
            {
                copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
                Assert.Equal(SkillName.Spellweaving, copy.Selected);
                foreach (var skill in MasteryInfo.Skills) { Assert.Equal(3, copy.Level(skill)); }
            }
            finally { copy.Delete(); }
        }
        finally { owner.Delete(); }
    }

    private sealed class TestShield : ManaShieldSpell
    {
        public TestShield(Mobile owner) : base(owner, null) { }
        public void Activate() { Chance = 1; BeginTimer(); }
    }

    private sealed class TestSong : SkillMasterySpell
    {
        public TestSong(Mobile owner) : base(owner, null, new SpellInfo("Test song", "", -1, 0)) { }
        public override bool PartyEffects => true;
        public override SkillName CastSkill => SkillName.Provocation;
        public override void OnCast() { }
        public void Activate() { BeginTimer(); UpdateParty(); }
    }

    [SkippableFact]
    public void PartySongsIncludeOwnedPetsAndStopAtRangeOrMembershipBoundary()
    {
        TileDataRequirement.SkipIfMissing();
        var bard = new PlayerMobile(); var owner = new PlayerMobile(); var pet = new Horse();
        var party = new Server.Engines.PartySystem.Party(bard);
        var song = new TestSong(bard);
        try
        {
            bard.Party = party;
            party.Add(owner);
            bard.MoveToWorld(new Point3D(3500, 2570, 0), Map.Trammel);
            owner.MoveToWorld(new Point3D(3501, 2570, 0), Map.Trammel);
            pet.MoveToWorld(new Point3D(3502, 2570, 0), Map.Trammel);
            pet.SetControlMaster(owner);
            song.Activate();
            Assert.Contains(owner, song.PartyList);
            Assert.Contains(pet, song.PartyList);
            Assert.Same(song, SkillMasterySpell.GetSpellForParty(pet, typeof(TestSong)));
            pet.MoveToWorld(new Point3D(3550, 2570, 0), Map.Trammel);
            song.UpdateParty();
            Assert.DoesNotContain(pet, song.PartyList);
            Assert.Null(SkillMasterySpell.GetSpellForParty(pet, typeof(TestSong)));
            party.Remove(owner);
            song.UpdateParty();
            Assert.DoesNotContain(owner, song.PartyList);
        }
        finally { song.Expire(); party.Disband(); pet.Delete(); owner.Delete(); bard.Delete(); }
    }

    [SkippableFact]
    public void BardCompanionChoosesRecoveryOrSupportAccordingToNeed()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { RawStr = 100 };
        var bard = new HavenCompanion { BoundOwner = owner };
        try
        {
            bard.Role = HavenCompanionRole.Bard;
            bard.Skills.Musicianship.Base = 100;
            bard.Skills.Peacemaking.Base = 100;
            bard.Skills.Provocation.Base = 100;
            owner.Hits = owner.HitsMax;
            Assert.Equal(SkillName.Provocation, bard.ChooseBardMastery(null));
            Assert.Equal(2, MasteryInfo.GetMasteryLevel(bard, SkillName.Provocation));
            owner.Hits = 10;
            Assert.Equal(SkillName.Peacemaking, bard.ChooseBardMastery(null));
        }
        finally { bard.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void NativeDamageActuallyUsesManaShieldAndExpiresCleanly()
    {
        TileDataRequirement.SkipIfMissing();
        var victim = new PlayerMobile { RawStr = 100, RawInt = 100 };
        var attacker = new PlayerMobile();
        var shield = new TestShield(victim);
        try
        {
            victim.Hits = victim.HitsMax;
            victim.Mana = 100;
            shield.Activate();
            var before = victim.Hits;
            AOS.Damage(victim, attacker, 40, true, 100, 0, 0, 0, 0);
            Assert.Equal(before - 20, victim.Hits);
            Assert.Equal(80, victim.Mana);
            shield.Expire();
            before = victim.Hits;
            AOS.Damage(victim, attacker, 20, true, 100, 0, 0, 0, 0);
            Assert.Equal(before - 20, victim.Hits);
        }
        finally { shield.Expire(); victim.Delete(); attacker.Delete(); }
    }

    [SkippableFact]
    public void EveryActiveMasteryConstructsAndUnlearnedPlayersCannotCast()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true };
        owner.AddItem(new Backpack());
        try
        {
            foreach (var info in MasteryInfo.Infos)
            {
                if (info.SpellType == null) { continue; }
                if (typeof(SpecialMove).IsAssignableFrom(info.SpellType))
                {
                    var move = SpellRegistry.GetSpecialMove(info.SpellID); Assert.NotNull(move); Assert.False(move.Validate(owner)); continue;
                }
                var spell = SpellRegistry.NewSpell(info.SpellID, owner, null);
                Assert.NotNull(spell);
                Assert.Equal(info.SpellType, spell.GetType());
                Assert.False(spell.CheckCast());
            }
        }
        finally { owner.Delete(); }
    }
}
