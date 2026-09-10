using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsEncounters
{
    public HavenWorldTestsEncounters() { _ = new HavenWorldTests(); }
    private static PlayerMobile Player()
    {
        var p = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100, RawDex = 100 }; p.AddItem(new Backpack());
        p.MoveToWorld(new Point3D(4196, 2868, 0), Map.Trammel); return p;
    }
    [SkippableFact]
    public void WavesAwardPointsOnceAndClearCreatesProtectedChestAndRaisesDifficulty()
    {
        TileDataRequirement.SkipIfMissing();
        var player = Player(); var outsider = Player(); var helper = Player(); var journal = HavenEncounterJournal.Get(player);
        HavenWanderingEncounter encounter = null; HavenEncounterChest chest = null;
        try
        {
            encounter = HavenWanderingEncounter.Start(journal); Assert.NotNull(encounter);
            var kills = 0;
            while (!encounter.Deleted && kills < 20)
            {
                encounter.Spawn(); Assert.True(encounter.Creatures.Count > 0, $"No mobs at kill {kills}, wave {encounter.Wave}, {encounter.Location}, {encounter.Map}"); var mob = encounter.Creatures[0];
                mob.DamageEntries.Add(new DamageEntry(player) { DamageGiven = mob.HitsMax, LastDamage = Core.Now });
                if (kills == 0) { mob.DamageEntries.Add(new DamageEntry(helper) { DamageGiven = mob.HitsMax, LastDamage = Core.Now }); }
                encounter.Defeated(mob); var points = journal.Points; encounter.Defeated(mob); Assert.Equal(points, journal.Points);
                mob.Delete(); kills++;
            }
            Assert.True(encounter.Deleted); Assert.Equal(10, kills); Assert.Equal(10, journal.Kills);
            Assert.Equal(35, journal.Points); Assert.Equal(35, journal.EarnedPoints);
            Assert.Equal(1, journal.Wins); Assert.Equal(2, journal.Tier); Assert.Equal(2, journal.HighestTier); Assert.Null(journal.Active);
            var helperJournal = HavenEncounterJournal.Get(helper);
            Assert.Equal(26, helperJournal.Points); Assert.Equal(1, helperJournal.Assists); Assert.Equal(0, helperJournal.Wins); Assert.Equal(1, helperJournal.Tier);
            Assert.Contains(journal.History, line => line.Contains("cleared"));
            foreach (var item in player.GetItemsInRange(2)) { if (item is HavenEncounterChest found) { chest = found; break; } }
            Assert.NotNull(chest); Assert.True(chest.CanClaim(player)); Assert.False(chest.CanClaim(outsider));
            Assert.True(chest.FindItemByType<Gold>() != null, "Gold missing: " + string.Join(", ", chest.Items.Select(i => i.GetType().Name + ":" + i.Amount)));
            Assert.True(chest.FindItemByType<AstralShard>() != null, "Shards missing: " + string.Join(", ", chest.Items.Select(i => i.GetType().Name + ":" + i.Amount)));
            var reason = LRReason.Inspecific; Assert.False(chest.CheckLift(outsider, chest.Items[0], ref reason));
            Assert.True(chest.CheckLift(player, chest.Items[0], ref reason));
            encounter.Finish(true, false); Assert.Equal(35, journal.Points); Assert.Equal(1, journal.Wins);
        }
        finally
        {
            encounter?.Delete(); chest?.Delete();
            var pets = new System.Collections.Generic.List<BaseCreature>();
            foreach (var mobile in player.GetMobilesInRange(10))
            { if (mobile is BaseCreature pet && pet.Backpack?.FindItemByType<HavenEncounterPetLease>() != null) { pets.Add(pet); } }
            foreach (var pet in pets) { pet.Delete(); }
            player.Delete(); outsider.Delete(); helper.Delete();
        }
    }
    [SkippableFact]
    public void RetreatCleansActorsAndLowersDifficultyButManualCancelDoesNot()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(); var journal = HavenEncounterJournal.Get(player); journal.Tier = 5;
        var pet = new Dog(); pet.SetControlMaster(player); pet.MoveToWorld(player.Location, player.Map);
        try
        {
            var encounter = HavenWanderingEncounter.Start(journal); Assert.NotNull(encounter); var actors = encounter.Creatures.ToArray();
            player.MoveToWorld(new Point3D(player.X + 30, player.Y, player.Z), player.Map); encounter.Tick();
            Assert.True(encounter.Deleted); Assert.All(actors, m => Assert.True(m.Deleted)); Assert.False(pet.Deleted);
            Assert.Equal(4, journal.Tier); Assert.Equal(1, journal.Failures);
            journal.Outcome(false, false); Assert.Equal(4, journal.Tier); Assert.Equal(1, journal.Failures);
            for (var i = 0; i < 30; i++) { journal.Log("test history bound"); } Assert.Equal(20, journal.History.Count);
        }
        finally { journal.Active?.Delete(); pet.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void PointRewardsRequireOwnershipFundsAndPreserveEarnedTotal()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(); var stranger = Player(); var journal = HavenEncounterJournal.Get(player);
        try
        {
            Assert.False(journal.Redeem(player, 0)); journal.Earn(250);
            Assert.False(journal.Redeem(stranger, 0)); Assert.Equal(250, journal.Points);
            Assert.True(journal.Redeem(player, 0)); Assert.Equal(0, journal.Points); Assert.Equal(250, journal.EarnedPoints);
            Assert.Equal(25, player.Backpack.FindItemByType<AstralShard>().Amount); Assert.False(journal.Redeem(player, 0));
            Assert.Contains(journal.History, s => s.Contains("Redeemed:"));
        }
        finally { player.Delete(); stranger.Delete(); }
    }
    [SkippableFact]
    public void JournalKeepsPointsDifficultyAndHistoryAcrossSerialization()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(); var journal = HavenEncounterJournal.Get(player);
        var copy = new HavenEncounterJournal(World.NewItem);
        try
        {
            journal.KillCredit(10); journal.ClearCredit(10, true); journal.Outcome(true, false);
            var writer = new BufferWriter(true); journal.Serialize(writer);
            copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.Equal(journal.Points, copy.Points); Assert.Equal(journal.EarnedPoints, copy.EarnedPoints);
            Assert.Equal(journal.Tier, copy.Tier); Assert.Equal(journal.HighestTier, copy.HighestTier);
            Assert.Equal(journal.Kills, copy.Kills); Assert.Equal(journal.Assists, copy.Assists); Assert.Equal(journal.History, copy.History);
        }
        finally { copy.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void LegendaryPetIsCustomOneSlotAndLeaseNeverDeletesAnOwnedPet()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player();
        var anchor = new HavenWanderingEncounter { Tier = 10, Journal = HavenEncounterJournal.Get(player) }; anchor.MoveToWorld(player.Location, player.Map);
        BaseCreature pet = null;
        try
        {
            pet = anchor.SpawnPet(3); Assert.NotNull(pet); Assert.True(HavenTamingMissions.IsCustomPet(pet)); Assert.Equal(1, pet.ControlSlots);
            Assert.Equal(3, pet.Backpack.FindItemByType<HavenPetRarity>().Tier);
            var lease = pet.Backpack.FindItemByType<HavenEncounterPetLease>(); Assert.NotNull(lease);
            pet.SetControlMaster(player); lease.Expire(); Assert.False(pet.Deleted); Assert.True(lease.Deleted);
            anchor.Delete(); Assert.False(pet.Deleted);
        }
        finally { anchor.Delete(); pet?.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void UntamedEncounterPetExpiresAndDisabledEncountersCannotStart()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(); var journal = HavenEncounterJournal.Get(player);
        var anchor = new HavenWanderingEncounter { Tier = 1 }; anchor.MoveToWorld(player.Location, player.Map);
        BaseCreature pet = null;
        try
        {
            pet = anchor.SpawnPet(1); Assert.NotNull(pet); pet.Backpack.FindItemByType<HavenEncounterPetLease>().Expire(); Assert.True(pet.Deleted);
            journal.Enabled = false; Assert.Null(HavenWanderingEncounter.Start(journal));
            journal.NextRoll = Core.Now - TimeSpan.FromHours(1); journal.Check(Core.Now, 0); Assert.Null(journal.Active);
        }
        finally { anchor.Delete(); pet?.Delete(); player.Delete(); }
    }
}
