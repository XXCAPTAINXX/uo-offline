using System;
using System.Collections.Generic;
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
public class HavenWorldTestsMasteryDefense
{
    public HavenWorldTestsMasteryDefense() { _ = new HavenWorldTests(); MasteryInfo.Configure(); }

    [Theory]
    [InlineData(1, 0.10)]
    [InlineData(2, 0.20)]
    [InlineData(3, 0.30)]
    public void SavingThrowRequiresLearnedSelectedMasteryAndUsesBaseTraining(int level, double expected)
    {
        var player = new PlayerMobile();
        try
        {
            var record = MasteryProgress.Get(player); record.Selected = SkillName.Swords;
            player.Skills.Swords.Cap = 500; player.Skills.Swords.Base = 120;
            player.Skills.Tactics.Cap = 500; player.Skills.Tactics.Base = 120;
            Assert.Equal(0, HavenMasteryDefense.DisarmBlockChance(player));
            Assert.True(record.Learn(SkillName.Swords, level));
            Assert.Equal(expected, HavenMasteryDefense.DisarmBlockChance(player), 8);
            player.Skills.Tactics.Base = 60;
            Assert.Equal(expected / 2, HavenMasteryDefense.DisarmBlockChance(player), 8);
            player.Skills.Tactics.Base = 500; player.Skills.Swords.Base = 500;
            Assert.Equal(expected, HavenMasteryDefense.DisarmBlockChance(player), 8);
            player.Skills.Swords.Base = 89; Assert.Equal(0, HavenMasteryDefense.DisarmBlockChance(player));
            player.Skills.Swords.Base = 120; record.Selected = SkillName.Magery;
            Assert.Equal(0, HavenMasteryDefense.DisarmBlockChance(player));
        }
        finally { player.Delete(); }
    }

    [SkippableFact]
    public void RealPairedPeaceSongsProtectOwnerAndPetThroughNativeWoundBleedAndCurse()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, RawStr = 100, RawDex = 100, RawInt = 100 };
        var enemy = new PlayerMobile { RawInt = 120 }; enemy.Skills.Magery.Base = 120; enemy.Skills.EvalInt.Base = 120;
        var bard = new HavenCompanion { BoundOwner = owner, Role = HavenCompanionRole.Bard, RawInt = 500 };
        var pet = new Horse();
        var song = new ResilienceSpell(bard, null); var paired = new PerseveranceSpell(bard, null);
        var oldSkillCheck = Mobile.SkillCheckLocationHandler;
        Mobile.SkillCheckLocationHandler = Server.Misc.SkillCheck.Mobile_SkillCheckLocation;
        try
        {
            owner.MoveToWorld(new Point3D(3500, 2570, 20), Map.Trammel);
            bard.SetControlMaster(owner); bard.MoveToWorld(owner.Location, owner.Map);
            pet.SetControlMaster(owner); pet.MoveToWorld(owner.Location, owner.Map);
            enemy.MoveToWorld(owner.Location, owner.Map);
            foreach (var skill in new[] { SkillName.Musicianship, SkillName.Peacemaking, SkillName.Discordance, SkillName.Provocation })
            { bard.Skills[skill].Cap = 150; bard.Skills[skill].Base = 130; }
            var lute = new Lute(); bard.Backpack.DropItem(lute); BaseInstrument.SetInstrument(bard, lute); bard.Mana = bard.ManaMax;
            foreach (var effect in new SkillMasterySpell[] { song, paired })
            { Assert.True(effect.CheckCast()); bard.Spell = effect; effect.State = SpellState.Sequencing; effect.OnCast(); Assert.NotNull(effect.Timer); }
            Assert.NotNull(SkillMasterySpell.GetSpellForParty(owner, typeof(PerseveranceSpell)));
            Assert.Equal(60, HavenMasteryDefense.ResilienceReduction(owner));
            Assert.Equal(60, HavenMasteryDefense.ResilienceReduction(pet));
            Assert.Equal(TimeSpan.FromSeconds(4), HavenMasteryDefense.ResilientDuration(owner, TimeSpan.FromSeconds(10)));
            MortalStrike.BeginWound(owner, TimeSpan.FromSeconds(10));
            var wounds = (Dictionary<Mobile, TimerExecutionToken>)typeof(MortalStrike).GetField("_table", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.InRange((wounds[owner].Next - Core.Now).TotalSeconds, 3.5, 4.1);
            BleedAttack.BeginBleed(pet, enemy);
            var bleeds = (Dictionary<Mobile, Timer>)typeof(BleedAttack).GetField("_table", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.Equal(2, bleeds[pet].Count);
            var expectedCurse = HavenMasteryDefense.ResilientDuration(owner, SpellHelper.GetDuration(enemy, owner));
            Assert.True(CurseSpell.DoCurse(enemy, owner));
            var curses = (Dictionary<Mobile, Timer>)typeof(CurseSpell).GetField("_table", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            Assert.InRange((curses[owner].Next - Core.Now).TotalSeconds, expectedCurse.TotalSeconds - 0.5, expectedCurse.TotalSeconds + 0.1);
            pet.MoveToWorld(new Point3D(3550, 2570, 20), Map.Trammel);
            Assert.Equal(0, HavenMasteryDefense.ResilienceReduction(pet));
            song.Expire(); Assert.Equal(0, HavenMasteryDefense.ResilienceReduction(owner));
            Assert.Equal(TimeSpan.FromSeconds(10), HavenMasteryDefense.ResilientDuration(owner, TimeSpan.FromSeconds(10)));
            Assert.False(HavenMasteryDefense.ResistsPoison(owner));
            BleedAttack.EndBleed(pet, false); BleedAttack.BeginBleed(pet, enemy); Assert.Equal(5, bleeds[pet].Count);
        }
        finally
        {
            Mobile.SkillCheckLocationHandler = oldSkillCheck;
            MortalStrike.EndWound(owner); BleedAttack.EndBleed(pet, false); CurseSpell.RemoveEffect(owner);
            paired.Expire(); song.Expire(); pet.Delete(); bard.Delete(); enemy.Delete(); owner.Delete();
        }
    }
}
