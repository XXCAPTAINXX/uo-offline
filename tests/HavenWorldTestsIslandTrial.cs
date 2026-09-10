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
    public void ThemesPersistAndGiveMatchingResourcesWithoutChangingDifficulty()
    {
        TileDataRequirement.SkipIfMissing();
        var trial = new HavenIslandTrial();
        try
        {
            for (var theme = 0; theme < 3; theme++)
            {
                HavenTrialTheme.Set(trial, theme);
                Assert.Equal(theme, HavenTrialTheme.Get(trial));
                var boss = new HavenTrialCreature(4, theme);
                var material = HavenTrialTheme.Resource(theme, true);
                try
                {
                    Assert.Equal(theme, HavenTrialTheme.Get(boss));
                    Assert.Equal(450, boss.HitsMax);
                    if (theme == 0) { Assert.IsType<Board>(material); }
                    else if (theme == 1) { Assert.IsType<IronIngot>(material); }
                    else { Assert.IsType<Hides>(material); }
                    Assert.InRange(material.Amount, 150, 350);
                }
                finally { boss.Delete(); material.Delete(); }
            }
            var record = new HavenTrialTheme(2);
            var writer = new BufferWriter(true); record.Serialize(writer);
            var copy = new HavenTrialTheme(World.NewItem);
            try
            {
                copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
                Assert.Equal(2, copy.Theme);
            }
            finally { record.Delete(); copy.Delete(); }
        }
        finally { trial.Delete(); }
    }
    [SkippableFact]
    public void ThreeShortWavesThenBossPayOnceAndCleanUp()
    {
        TileDataRequirement.SkipIfMissing();
        var trial = new HavenIslandTrial(); var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var helper = new PlayerMobile { Player = true, Body = 0x190 }; helper.AddItem(new Backpack());
        owner.AddItem(new Backpack());
        var previous = Mobile.CreateCorpseHandler;
        Mobile.CreateCorpseHandler = Corpse.Mobile_CreateCorpseHandler;
        try
        {
            Assert.True(HavenIslandTrial.FindSite(HavenIslandTrial.Site, out var site));
            trial.MoveToWorld(site, Map.Trammel); owner.MoveToWorld(site, Map.Trammel);
            helper.MoveToWorld(site, Map.Trammel);
            trial.OnDoubleClick(owner); Assert.Equal(1, trial.Stage);
            for (var i = 0; i < 18; i++)
            {
                trial.Tick(); Assert.InRange(trial.Creatures.Count, 1, 3);
                var mob = trial.Creatures[0]; Assert.InRange(mob.HitsMax, 80, 120);
                if (i == 0) { mob.DamageEntries.Add(new DamageEntry(helper) { DamageGiven = 1, LastDamage = Core.Now }); }
                mob.Kill(); mob.Corpse?.Delete();
            }
            Assert.Equal(4, trial.Stage); trial.Tick(); Assert.Single(trial.Creatures);
            var boss = trial.Creatures[0];
            boss.DamageEntries.Add(new DamageEntry(owner) { DamageGiven = 450, LastDamage = Core.Now });
            owner.Backpack.DropItem(new AstralShard(1)); // Unrelated shard drops must not be mistaken for the reward bundle.
            var helperBefore = helper.Backpack.GetAmount(typeof(AstralShard));
            boss.Kill();
            Assert.Equal(0, trial.Stage); Assert.Empty(trial.Creatures);
            var rewards = owner.Backpack; var helperRewards = helper.Backpack;
            foreach (var item in rewards.Items) { Assert.False(item is Bag bag && bag.Name == "island trial rewards"); }
            // The normal monster-drop hook can also award one shard independently of trial completion.
            var shardsAfter = rewards.GetAmount(typeof(AstralShard));
            Assert.InRange(shardsAfter, 6, 7);
            Assert.Equal(helperBefore + 5, helperRewards.GetAmount(typeof(AstralShard)));
            var corpse = Assert.IsAssignableFrom<Container>(boss.Corpse);
            var count = 0; foreach (var scroll in rewards.FindItemsByType<PowerScroll>()) { count++; Assert.InRange(scroll.Value, 105, 110); } Assert.Equal(5, count);
            Assert.Null(corpse.FindItemByType<PowerScroll>());
            Assert.Equal(20, rewards.FindItemByType<HavenMark>().Amount);
            Assert.NotNull(rewards.FindItemByType<ScrollofAlacrity>());
            Assert.InRange(rewards.FindItemByType<ScrollofTranscendence>().Value, 0.5, 2.0);
            Assert.NotNull(helperRewards.FindItemByType<ScrollofAlacrity>());
            Assert.InRange(rewards.FindItemByType<Gold>().Amount, 25000, 40000);
            trial.Defeated(boss); Assert.Equal(shardsAfter, rewards.GetAmount(typeof(AstralShard)));
            trial.OnDoubleClick(owner); Assert.Equal(0, trial.Stage); corpse.Delete();
        }
        finally { Mobile.CreateCorpseHandler = previous; trial.Delete(); owner.Delete(); helper.Delete(); }
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
    public void LegendaryFrostmaneChillsInsteadOfReceivingGenericHealingAndMana()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190, RawInt = 100 };
        var pet = new HavenFrostmane(); var enemy = new Dragon();
        try
        {
            owner.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
            pet.MoveToWorld(owner.Location, owner.Map); enemy.MoveToWorld(owner.Location, owner.Map); pet.SetControlMaster(owner);
            owner.Hits = 1; owner.Mana = 0; pet.Hits = 1;
            HavenPetRarity.Apply(pet,3);var dex=enemy.Dex;
            Assert.True(HavenPetSignatures.Activate(pet,enemy));
            Assert.Equal(1,owner.Hits);Assert.Equal(0,owner.Mana);Assert.True(enemy.Dex<dex);
            Assert.Null(HavenPetTraining.Find(pet));
            Assert.False(HavenPetSignatures.Activate(pet,enemy));
        }
        finally { pet.Delete(); enemy.Delete(); owner.Delete(); }
    }
}
