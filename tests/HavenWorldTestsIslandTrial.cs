using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsIslandTrial
{
    public HavenWorldTestsIslandTrial() => _ = new HavenWorldTests();
    [SkippableFact]
    public void ThreeShortWavesThenBossPayOnceAndCleanUp()
    {
        TileDataRequirement.SkipIfMissing();
        var trial = new HavenIslandTrial(); var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        var previous = Mobile.CreateCorpseHandler;
        Mobile.CreateCorpseHandler = Corpse.Mobile_CreateCorpseHandler;
        try
        {
            Assert.True(HavenIslandTrial.FindSite(HavenIslandTrial.Site, out var site));
            trial.MoveToWorld(site, Map.Trammel); owner.MoveToWorld(site, Map.Trammel);
            trial.OnDoubleClick(owner); Assert.Equal(1, trial.Stage);
            for (var i = 0; i < 18; i++)
            {
                trial.Tick(); Assert.InRange(trial.Creatures.Count, 1, 3);
                var mob = trial.Creatures[0]; Assert.InRange(mob.HitsMax, 80, 120);
                mob.Kill(); mob.Corpse?.Delete();
            }
            Assert.Equal(4, trial.Stage); trial.Tick(); Assert.Single(trial.Creatures);
            var boss = trial.Creatures[0];
            boss.DamageEntries.Add(new DamageEntry(owner) { DamageGiven = 450, LastDamage = Core.Now });
            boss.Kill();
            Assert.Equal(0, trial.Stage); Assert.Empty(trial.Creatures);
            Assert.Equal(5, owner.Backpack.FindItemByType<AstralShard>().Amount);
            var corpse = Assert.IsAssignableFrom<Container>(boss.Corpse);
            var count = 0; foreach (var scroll in corpse.FindItemsByType<PowerScroll>()) { count++; Assert.InRange(scroll.Value, 105, 110); } Assert.Equal(5, count);
            Assert.Equal(20, corpse.FindItemByType<HavenMark>().Amount);
            trial.Defeated(boss); Assert.Equal(5, owner.Backpack.FindItemByType<AstralShard>().Amount);
            trial.OnDoubleClick(owner); Assert.Equal(0, trial.Stage); corpse.Delete();
        }
        finally { Mobile.CreateCorpseHandler = previous; trial.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void AstralGrowthAndGearFollowerUnlockAreIdempotent()
    {
        TileDataRequirement.SkipIfMissing();
        var ring = new AstralWeaversRing(); var mantle = new AstralGuardianMantle(); var earrings = new AstralFortuneEarrings();
        var talisman = new HavenConcordTalisman(); var owner = new PlayerMobile();
        try
        {
            foreach (var gear in new Item[] { ring, mantle, earrings, talisman }) { HavenGearExperience.Gain(gear, 1900); }
            Assert.Equal(68, ring.Attributes.SpellDamage); Assert.Equal(38, mantle.Attributes.BonusHits); Assert.Equal(780, earrings.Attributes.Luck);
            HavenGearExperience.Gain(earrings, 100); Assert.Equal(780, earrings.Attributes.Luck);
            talisman.UnlockFollower(owner); Assert.Equal(6, owner.FollowersMax);
            talisman.UnlockFollower(owner); Assert.Equal(6, owner.FollowersMax);
            owner.FollowersMax = 8; talisman.UnlockFollower(owner); Assert.Equal(8, owner.FollowersMax);
        }
        finally { ring.Delete(); mantle.Delete(); earrings.Delete(); talisman.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void HavenQuestRewardsGrowButOrdinaryEquipmentDoesNot()
    {
        TileDataRequirement.SkipIfMissing();
        var sword = new JocklesQuicksword(); var book = new HallowedSpellbook(); var ordinary = new Longsword();
        try
        {
            var damage = sword.Attributes.WeaponDamage; var spell = book.Attributes.SpellDamage;
            HavenGearExperience.Gain(sword, 1900); HavenGearExperience.Gain(book, 1900); HavenGearExperience.Gain(ordinary, 1900);
            Assert.Equal(damage + 38, sword.Attributes.WeaponDamage); Assert.Equal(spell + 19, book.Attributes.SpellDamage);
            Assert.Null(HavenGearExperience.Find(ordinary));
        }
        finally { sword.Delete(); book.Delete(); ordinary.Delete(); }
    }
    [SkippableFact]
    public void SkilledPlayersClaimQuestRewardsOnlyOnce()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 }; owner.AddItem(new Backpack());
        var quest = new Server.Engines.MLQuests.Definitions.TheWayOfTheBlade();
        try
        {
            owner.Skills.Swords.Base = 49.9;
            Assert.False(HavenTrainingRewards.ClaimQuest(owner, quest));
            owner.Skills.Swords.Base = 100;
            Assert.True(HavenTrainingRewards.ClaimQuest(owner, quest));
            Assert.NotNull(owner.Backpack.FindItemByType<JocklesQuicksword>());
            Assert.False(HavenTrainingRewards.ClaimQuest(owner, quest));
        }
        finally { Server.Engines.MLQuests.MLQuestSystem.Contexts.Remove(owner); owner.Delete(); }
    }
    [SkippableFact]
    public void ProvisionerUsesMarkedShopAndIsNotDuplicated()
    {
        TileDataRequirement.SkipIfMissing();
        HavenProvisioner.Ensure(); HavenProvisioner.Ensure();
        var count = 0; Provisioner found = null;
        foreach (var vendor in Map.Trammel.GetMobilesInRange<Provisioner>(HavenProvisioner.Site, 5))
        {
            count++; found = vendor; Assert.Equal("Mara Wren", vendor.Name); Assert.InRange(vendor.Z, 19, 23);
        }
        found?.Delete();
        Assert.Equal(1, count);
    }
    [SkippableFact]
    public void LegendaryInnatePowersIncludeHealingAndManaWithoutTraining()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190, RawInt = 100 };
        var pet = new HavenFrostmane(); var enemy = new Dragon();
        try
        {
            owner.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
            pet.MoveToWorld(owner.Location, owner.Map); enemy.MoveToWorld(owner.Location, owner.Map); pet.SetControlMaster(owner);
            owner.Hits = 1; owner.Mana = 0; pet.Hits = 1;
            var next = DateTime.MinValue;
            Assert.True(HavenRarePetAbility.Activate(pet, enemy, 3, ref next, cumulative: true));
            Assert.True(owner.Hits > 1); Assert.Equal(8, owner.Mana); Assert.Equal(11, pet.Hits);
            Assert.Null(HavenPetTraining.Find(pet));
            Assert.False(HavenRarePetAbility.Activate(pet, enemy, 3, ref next, cumulative: true));
        }
        finally { pet.Delete(); enemy.Delete(); owner.Delete(); }
    }
}
