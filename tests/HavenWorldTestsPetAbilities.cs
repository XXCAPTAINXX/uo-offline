using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsPetAbilities
{
    public HavenWorldTestsPetAbilities() => _ = new HavenWorldTests();
    private static PlayerMobile Owner(BaseCreature pet)
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack()); owner.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
        pet.MoveToWorld(owner.Location, owner.Map); pet.SetControlMaster(owner);
        var training = HavenPetTraining.Get(pet); Assert.True(training.Begin(owner, pet)); training.Progress = 10000;
        return owner;
    }
    [SkippableFact]
    public void ExactCapChoiceConsumesOnlyMatchingScrollAndPreservesSkill()
    {
        TileDataRequirement.SkipIfMissing();
        var pet = new Horse(); var owner = Owner(pet);
        try
        {
            var codex = new ProgressionArchive(); owner.Backpack.DropItem(codex);
            var larger = new PowerScroll(SkillName.Wrestling, 120); codex.DropItem(larger);
            Assert.False(HavenPetSkillCapsGump.Purchase(owner, pet, SkillName.Wrestling, 110));
            Assert.False(larger.Deleted);
            var chosen = new PowerScroll(SkillName.Wrestling, 110); codex.DropItem(chosen);
            var before = pet.Skills.Wrestling.Base;
            Assert.True(HavenPetSkillCapsGump.Purchase(owner, pet, SkillName.Wrestling, 110));
            Assert.True(chosen.Deleted); Assert.False(larger.Deleted);
            Assert.Equal(110, pet.Skills.Wrestling.Cap); Assert.Equal(before, pet.Skills.Wrestling.Base);
            Assert.False(HavenPetSkillCapsGump.Purchase(owner, pet, SkillName.Wrestling, 110));
        }
        finally { pet.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void LearnedAbilitiesPersistAndEnforceCategoryAndTotalLimits()
    {
        TileDataRequirement.SkipIfMissing();
        var pet = new Horse(); var owner = Owner(pet);
        try
        {
            Assert.True(HavenPetAbilities.Learn(owner, pet, 0));
            Assert.False(HavenPetAbilities.Learn(owner, pet, 1));
            Assert.True(HavenPetAbilities.Learn(owner, pet, 6));
            Assert.True(HavenPetAbilities.Learn(owner, pet, 7));
            Assert.False(HavenPetAbilities.Learn(owner, pet, 4));
            Assert.False(HavenPetTraining.Find(pet).LearnHealing(owner, pet));
            Assert.Equal(AIType.AI_Mage, pet.AI);
            Assert.NotNull(HavenPetAbilities.SelectMove(pet));
            var record = HavenPetAbilities.Find(pet);
            var writer = new BufferWriter(true); record.Serialize(writer);
            var copy = new HavenPetAbilities(World.NewItem);
            try
            {
                copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
                Assert.Equal(record.Learned, copy.Learned);
            }
            finally { copy.Delete(); }
        }
        finally { pet.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void LearnedBreathDamagesTargetAndConsumesManaOnlyOncePerCooldown()
    {
        TileDataRequirement.SkipIfMissing();
        var pet = new Horse(); var owner = Owner(pet); var enemy = new Dragon();
        try
        {
            enemy.MoveToWorld(owner.Location, owner.Map);
            Assert.True(HavenPetAbilities.Learn(owner, pet, 5));
            pet.RawInt = 100; pet.Mana = 100; pet.Combatant = enemy;
            var health = enemy.Hits;
            HavenPetAbilities.Think(pet);
            Assert.True(enemy.Hits < health); Assert.Equal(90, pet.Mana);
            HavenPetAbilities.Think(pet); Assert.Equal(90, pet.Mana);
        }
        finally { pet.Delete(); enemy.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void CustomPoolOnlyDamagesEnemiesInsideAndExpiresWithItsVisuals()
    {
        TileDataRequirement.SkipIfMissing();
        var pet = new HavenFrostmane(); var owner = Owner(pet);
        var enemy = new Dragon(); var friend = new Horse(); var pool = new HavenPetPool(pet, owner);
        try
        {
            enemy.MoveToWorld(owner.Location, owner.Map); friend.MoveToWorld(owner.Location, owner.Map); friend.SetControlMaster(owner);
            pool.MoveToWorld(owner.Location, owner.Map);
            Assert.Equal(2, HavenPetPool.Theme(pet));
            var health = enemy.Hits; var friendHealth = friend.Hits; var playerHealth = owner.Hits;
            pool.Tick(); Assert.True(enemy.Hits < health);
            Assert.Equal(friendHealth, friend.Hits); Assert.Equal(playerHealth, owner.Hits);
            enemy.MoveToWorld(new Point3D(owner.X + 3, owner.Y, owner.Z), owner.Map);
            health = enemy.Hits; pool.Tick(); Assert.Equal(health, enemy.Hits);
            for (var i = 0; i < 4; i++) { pool.Tick(); }
            Assert.True(pool.Deleted);
            Assert.Null(pool.Pet); Assert.Null(pool.Owner);
        }
        finally { pool.Delete(); friend.Delete(); enemy.Delete(); pet.Delete(); owner.Delete(); }
    }
}
