using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsPetTraining
{
    public HavenWorldTestsPetTraining() => _ = new HavenWorldTests();
    private static PlayerMobile Owner(BaseCreature pet)
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        owner.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
        pet.MoveToWorld(owner.Location, owner.Map); pet.SetControlMaster(owner); return owner;
    }
    [SkippableFact]
    public void CombatRequiresTrainingAndMultipleTargetsThenSpendingAddsExactlyOneSlot()
    {
        TileDataRequirement.SkipIfMissing();
        var pet = new Horse(); var enemy = new Dragon(); var owner = Owner(pet); var other = new PlayerMobile();
        try
        {
            var record = HavenPetTraining.Get(pet);
            enemy.OnDamage(200, pet, false); Assert.Equal(0, record.Progress);
            Assert.True(record.BeginWithFeedback(owner, pet));
            Assert.True(record.Active);
            Assert.Contains("Training active", record.Status(pet));
            Assert.False(record.BeginWithFeedback(owner, pet));
            for (var i = 0; i < 60; i++) { enemy.OnDamage(200, pet, false); }
            Assert.Equal(5000, record.Progress);
            Assert.False(record.Upgrade(owner, pet, 0, 1));
            record.Progress = 10000;
            var before = record.PointsTenths;
            Assert.False(record.Upgrade(other, pet, 0, 1));
            var mana = pet.ManaMax;
            Assert.True(record.Upgrade(owner, pet, 2, 1)); Assert.Equal(mana, pet.ManaMax);
            Assert.Equal(2, pet.ControlSlots); Assert.Equal(2, owner.Followers);
            Assert.Equal(before - 5, record.PointsTenths);
            Assert.True(record.Upgrade(owner, pet, 2, 1)); Assert.Equal(2, pet.ControlSlots);
            Assert.True(record.Finish(owner, pet)); Assert.Equal(5, owner.FollowersMax);
            Assert.False(record.Finish(owner, pet));
        }
        finally { pet.Delete(); enemy.Delete(); owner.Delete(); other.Delete(); }
    }
    [SkippableFact]
    public void FinalStagePreservesPlayerCapacityAndHealingWorks()
    {
        TileDataRequirement.SkipIfMissing();
        var pet = new HavenFrostmane { ControlSlots = 4 }; var owner = Owner(pet);
        try
        {
            var record = HavenPetTraining.Get(pet); Assert.True(record.Begin(owner, pet)); record.Progress = 10000;
            Assert.True(record.LearnHealing(owner, pet)); Assert.True(pet.CanHealOwner); Assert.Equal(2.0, pet.HealOwnerDelay);
            Assert.Equal(5, pet.ControlSlots); Assert.Equal(5, owner.FollowersMax);
            Assert.False(record.LearnHealing(owner, pet));
            pet.Skills.Healing.Base = 100; pet.Skills.Anatomy.Base = 100; owner.Hits = 1;
            pet.Heal(owner); Assert.True(owner.Hits > 1);
            Assert.True(record.Finish(owner, pet)); Assert.Equal(5, owner.FollowersMax);
            Assert.False(record.Finish(owner, pet)); Assert.False(record.Begin(owner, pet)); Assert.Equal(5, owner.FollowersMax);
            var writer = new BufferWriter(true); record.Serialize(writer);
            var copy = new HavenPetTraining(World.NewItem);
            try
            {
                copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
                Assert.Equal(record.Progress, copy.Progress); Assert.Equal(record.Healing, copy.Healing); Assert.False(copy.Active);
            }
            finally { copy.Delete(); }
        }
        finally { pet.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void StatCapsAndScrollCostsPreventFreeOrRepeatedUpgrades()
    {
        TileDataRequirement.SkipIfMissing();
        var pet = new Horse(); var owner = Owner(pet);
        var scroll = new PowerScroll(SkillName.Wrestling, 110);
        try
        {
            owner.Backpack.DropItem(scroll);
            var record = HavenPetTraining.Get(pet); record.Begin(owner, pet); record.Progress = 10000;
            pet.RawDex = 210;
            var points = record.PointsTenths;
            Assert.False(record.Upgrade(owner, pet, 1, 1)); Assert.Equal(210, pet.RawDex); Assert.Equal(points, record.PointsTenths);
            var skill = pet.Skills.Wrestling.Base;
            Assert.True(record.RaiseCap(owner, pet, SkillName.Wrestling, scroll));
            Assert.Equal(110, pet.Skills.Wrestling.Cap); Assert.Equal(skill, pet.Skills.Wrestling.Base);
            Assert.True(scroll.Deleted); Assert.False(record.RaiseCap(owner, pet, SkillName.Wrestling, scroll));
        }
        finally { scroll.Delete(); pet.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void StandardPetsNeverRollRarityOrReceiveRarityBonuses()
    {
        TileDataRequirement.SkipIfMissing();
        var loot = HavenCompanionExpedition.CreateLoot(HavenExpeditionKind.TameHorse, 5, null, 0.999);
        var horse = new Horse();
        try
        {
            Assert.Equal(0, loot.FindItemByType<HavenExpeditionPetClaim>().Rarity);
            var strength = horse.RawStr; HavenPetRarity.Apply(horse, 3); Assert.Equal(strength, horse.RawStr);
            Assert.Null(horse.Backpack?.FindItemByType<HavenPetRarity>());
        }
        finally { loot.Delete(); horse.Delete(); }
    }
    [SkippableFact]
    public void StableChargesWalletAndIgnoresHiddenTrainingRecords()
    {
        TileDataRequirement.SkipIfMissing();
        var pet = new PackHorse(); var owner = Owner(pet); var trainer = new AnimalTrainer();
        var wallet = new AdventurersWallet();
        try
        {
            owner.Backpack.DropItem(wallet); wallet.Deposit(30);
            trainer.MoveToWorld(owner.Location, owner.Map);
            HavenPetTraining.Get(pet);
            Assert.False(HavenEconomy.HasStableCargo(pet));
            trainer.BeginStable(owner); Assert.NotNull(owner.Target); Assert.Equal(30, wallet.Balance);
            trainer.EndStable(owner, pet);
            Assert.True(pet.IsStabled); Assert.Equal(0, wallet.Balance); Assert.Equal(0, owner.Followers);
            Assert.NotNull(HavenPetTraining.Find(pet));
        }
        finally { trainer.Delete(); pet.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void TithingChargesWalletOnlyForAvailablePointCapacity()
    {
        TileDataRequirement.SkipIfMissing();
        var pet = new Horse(); var owner = Owner(pet); var wallet = new AdventurersWallet();
        try
        {
            owner.Backpack.DropItem(wallet); wallet.Deposit(5000);
            Assert.True(HavenTithing.Tithe(owner, 1000)); Assert.Equal(1000, owner.TithingPoints); Assert.Equal(4000, wallet.Balance);
            owner.TithingPoints = 99950;
            Assert.True(HavenTithing.Tithe(owner, 1000)); Assert.Equal(100000, owner.TithingPoints); Assert.Equal(3950, wallet.Balance);
            Assert.False(HavenTithing.Tithe(owner, 1000)); Assert.False(HavenTithing.Tithe(owner, -1)); Assert.Equal(3950, wallet.Balance);
        }
        finally { pet.Delete(); owner.Delete(); }
    }
}
