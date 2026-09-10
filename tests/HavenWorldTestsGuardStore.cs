using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsGuardStore
{
    public HavenWorldTestsGuardStore() => _ = new HavenWorldTests();
    private static PlayerMobile Owner()
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100 };
        owner.AddItem(new Backpack()); owner.MoveToWorld(new Point3D(4196, 2868, 0), Map.Trammel); return owner;
    }
    private static Orc Enemy(Mobile owner, int x, int y = 0)
    {
        var enemy = new Orc { FightMode = FightMode.Closest };
        enemy.MoveToWorld(new Point3D(owner.X + x, owner.Y + y, owner.Z), owner.Map); return enemy;
    }

    [SkippableFact]
    public void GuardFindsClosestToOwnerAndReturnsAfterAutoTargetDies()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Owner(); var pet = new GreyWolf(); var near = Enemy(owner, 2); var far = Enemy(owner, 5);
        try
        {
            pet.SetControlMaster(owner); pet.MoveToWorld(new Point3D(owner.X + 6, owner.Y, owner.Z), owner.Map);
            pet.ControlOrder = OrderType.Follow;
            Assert.False(HavenGuardPatrol.TryAcquire(pet));
            pet.ControlOrder = OrderType.Guard;
            Assert.Same(near, HavenGuardPatrol.Closest(pet));
            Assert.True(HavenGuardPatrol.TryAcquire(pet));
            Assert.Same(near, pet.ControlTarget); Assert.Equal(OrderType.Attack, pet.ControlOrder);
            Assert.False(HavenGuardPatrol.ReturnIfOutOfRange(pet));
            near.Delete();
            Assert.True(HavenGuardPatrol.ReturnIfOutOfRange(pet));
            Assert.Equal(OrderType.Guard, pet.ControlOrder);
        }
        finally { pet.Delete(); near.Delete(); far.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void GuardExcludesPetsPeacefulAnimalsAndHiddenTargetsAndRespectsManualOrders()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Owner(); var other = Owner(); var pet = new GreyWolf(); var enemy = Enemy(owner, 2); var sheep = new Sheep();
        try
        {
            pet.SetControlMaster(owner); pet.MoveToWorld(owner.Location, owner.Map);
            sheep.MoveToWorld(owner.Location, owner.Map);
            Assert.False(HavenGuardPatrol.Hostile(pet, sheep, 8));
            enemy.SetControlMaster(other); Assert.False(HavenGuardPatrol.Hostile(pet, enemy, 8));
            enemy.SetControlMaster(null); enemy.Owners.Clear();
            enemy.Hidden = true; Assert.False(HavenGuardPatrol.Hostile(pet, enemy, 8)); enemy.Hidden = false;
            enemy.BardPacified = true; Assert.False(HavenGuardPatrol.Hostile(pet, enemy, 8)); enemy.BardPacified = false;
            pet.ControlOrder = OrderType.Guard; Assert.True(HavenGuardPatrol.TryAcquire(pet));
            pet.ControlTarget = enemy; pet.ControlOrder = OrderType.Attack; // Explicit re-issue cancels patrol leash.
            enemy.MoveToWorld(new Point3D(owner.X + 20, owner.Y, owner.Z), owner.Map);
            Assert.False(HavenGuardPatrol.ReturnIfOutOfRange(pet)); Assert.Equal(OrderType.Attack, pet.ControlOrder);
        }
        finally { pet.Delete(); enemy.Delete(); sheep.Delete(); other.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void CompanionGuardLeashesWithoutTurningIntoFollow()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Owner(); var pet = new HavenCompanion(); var enemy = Enemy(owner, 2);
        try
        {
            pet.SetControlMaster(owner); pet.MoveToWorld(owner.Location, owner.Map); pet.ControlOrder = OrderType.Guard;
            Assert.True(HavenGuardPatrol.TryAcquire(pet));
            enemy.MoveToWorld(new Point3D(owner.X + 20, owner.Y, owner.Z), owner.Map);
            Assert.True(HavenGuardPatrol.ReturnIfOutOfRange(pet)); Assert.Equal(OrderType.Guard, pet.ControlOrder);
            pet.ControlOrder = OrderType.Stay; Assert.False(HavenGuardPatrol.TryAcquire(pet));
        }
        finally { pet.Delete(); enemy.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void ProgressAwardsOnlyOnceAndExistingSkillMilestonesCount()
    {
        TileDataRequirement.SkipIfMissing(); var owner = Owner();
        try
        {
            owner.Skills.Magery.Base = 100;
            HavenSovereigns.CheckProgress(owner); var account = HavenSovereignAccount.Get(owner); var balance = account.Balance;
            Assert.True(balance >= 65); Assert.Contains("skill:25:100", account.Achievements);
            HavenSovereigns.CheckProgress(owner); Assert.Equal(balance, account.Balance);
            Assert.True(account.IsVirtualItem); Assert.Equal(0, owner.Backpack.TotalItems);
            Assert.False(account.Award("welcome", "duplicate", 999)); Assert.Equal(balance, account.Balance);
            var writer = new BufferWriter(true); account.Serialize(writer);
            var copy = new HavenSovereignAccount(World.NewItem);
            try
            {
                copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
                Assert.Equal(balance, copy.Balance); Assert.Equal(account.Earned, copy.Earned);
                Assert.Equal(account.Achievements, copy.Achievements); Assert.Equal(account.History, copy.History);
            }
            finally { copy.Delete(); }
        }
        finally { owner.Delete(); }
    }
    [SkippableFact]
    public void VisitsAreOneTimePerNamedRegionAndFacet()
    {
        TileDataRequirement.SkipIfMissing(); var owner = Owner();
        var area = new Rectangle3D(owner.X - 4, owner.Y - 4, -20, 10, 10, 80);
        var town = new Server.Regions.TownRegion("Sovereign test town", Map.Trammel, 150, area);
        var dungeon = new Server.Regions.DungeonRegion("Sovereign test dungeon", Map.Trammel, 160, area);
        try
        {
            town.Register(); owner.MoveToWorld(new Point3D(owner.X + 1, owner.Y, owner.Z), owner.Map);
            HavenSovereigns.CheckProgress(owner); var account = HavenSovereignAccount.Get(owner);
            var balance = account.Balance;
            Assert.Contains("visit:1:town:Sovereign test town", account.Achievements);
            HavenSovereigns.CheckProgress(owner); Assert.Equal(balance, account.Balance);
            dungeon.Register(); owner.MoveToWorld(new Point3D(owner.X + 1, owner.Y, owner.Z), owner.Map);
            HavenSovereigns.CheckProgress(owner); Assert.Equal(balance + 20, account.Balance);
            HavenSovereigns.CheckProgress(owner); Assert.Equal(balance + 20, account.Balance);
            HavenSovereigns.EncounterCleared(owner, 1); Assert.Equal(balance + 57, account.Balance);
            HavenSovereigns.EncounterCleared(owner, 1); Assert.Equal(balance + 69, account.Balance);
        }
        finally { dungeon.Unregister(); town.Unregister(); owner.Delete(); }
    }
    [SkippableFact]
    public void BossCurrencyIsPerParticipantDeduplicatedAndPetsNeverPay()
    {
        TileDataRequirement.SkipIfMissing(); var owner = Owner(); var helper = Owner(); var boss = Enemy(owner, 2);
        try
        {
            boss.SetHits(5000); boss.Karma = -1000;
            HavenSovereigns.OnMonsterKilled(boss, owner); HavenSovereigns.OnMonsterKilled(boss, helper);
            var account = HavenSovereignAccount.Get(owner);
            Assert.Equal(60, account.Balance); Assert.Equal(60, HavenSovereignAccount.Get(helper).Balance);
            HavenSovereigns.OnMonsterKilled(boss, owner); Assert.Equal(60, account.Balance); Assert.Equal(1, account.Kills);
            var tamed = Enemy(owner, 1);
            try { tamed.SetHits(5000); tamed.SetControlMaster(owner); HavenSovereigns.OnMonsterKilled(tamed, owner); Assert.Equal(60, account.Balance); }
            finally { tamed.Delete(); }
        }
        finally { boss.Delete(); helper.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void StoreDebitsOnlyDeliveredItemsAndRejectsForgedOrRepeatedConfirmations()
    {
        TileDataRequirement.SkipIfMissing(); var owner = Owner(); var other = Owner();
        try
        {
            var account = HavenSovereignAccount.Get(owner); account.Award("test", "Test", 2000);
            Assert.False(HavenSovereignStore.Buy(owner, -1)); Assert.False(HavenSovereignStore.Buy(owner, 999));
            owner.Backpack.MaxItems = 1; owner.Backpack.DropItem(new Bag());
            Assert.False(HavenSovereignStore.Buy(owner, 0)); Assert.Equal(2000, account.Balance);
            owner.Backpack.MaxItems = 125;
            var gump = new HavenSovereignStore.StoreGump(owner, 0, 0, 0);
            gump.Respond(other, 500); Assert.Equal(2000, account.Balance);
            gump.Respond(owner, 500); Assert.Equal(1950, account.Balance);
            gump.Respond(owner, 500); Assert.Equal(1950, account.Balance);
            Assert.NotNull(owner.Backpack.FindItemByType<HavenBondingPotion>());
            for (var i = 1; i < HavenSovereignStore.Offers.Length; i++)
            { account.Balance = 2000; Assert.True(HavenSovereignStore.Buy(owner, i)); Assert.Equal(2000 - HavenSovereignStore.Offers[i].Price, account.Balance); }
        }
        finally { other.Delete(); owner.Delete(); }
    }
}
