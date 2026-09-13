using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsAbyss
{
    public HavenWorldTestsAbyss() { _ = new HavenWorldTests(); }
    private static PlayerMobile Player()
    {
        var player = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100, RawDex = 100, RawInt = 100 };
        player.AddItem(new Backpack()); player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
        return player;
    }
    [SkippableFact]
    public void HomeFindsOnlyTheOwnedIslandAndBringsPetsButHonorsTravelRestrictions()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Player(); var stranger = Player(); var estate = new HavenPirateEstate(); var pet = new Dog();
        try
        {
            estate.MoveToWorld(HavenPirateEstate.Site, Map.Trammel); estate.Build(owner);
            pet.SetControlMaster(owner); pet.ControlOrder = OrderType.Follow; pet.MoveToWorld(owner.Location, owner.Map);
            Assert.False(HavenPirateEstate.GoHome(stranger));
            owner.Criminal = true; Assert.False(HavenPirateEstate.GoHome(owner)); owner.Criminal = false;
            Assert.True(HavenPirateEstate.GoHome(owner));
            Assert.Equal(new Point3D(4208, 2928, 0), owner.Location);
            Assert.Equal(owner.Location, pet.Location);
        }
        finally { pet.Delete(); estate.Delete(); owner.Delete(); stranger.Delete(); }
    }
    [SkippableFact]
    public void AncientHellhoundKeepsItsHighStatsAndUsesNativeOwnerHealingWithRarityAndTraining()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Player(); var hound = new HavenAncientHellhound();
        try
        {
            Assert.Equal(1069, hound.Body.BodyID); Assert.InRange(hound.RawDex, 180, 210);
            Assert.Equal(hound.Dex, hound.StamMax); Assert.False(hound.StatLossAfterTame);
            Assert.Equal(110, hound.Skills.Healing.Base); Assert.False(hound.CanHeal);
            HavenPetRarity.Apply(hound, 3);
            Assert.Equal(1, hound.ControlSlots); Assert.True(HavenTamingMissions.IsCustomPet(hound)); Assert.Equal(5, HavenPetTraining.MaxSlots(hound));
            var dex = hound.RawDex;
            Assert.True(hound.SetControlMaster(owner)); Assert.Equal(dex, hound.RawDex); Assert.Equal(hound.Dex, hound.StamMax);
            Assert.True(hound.CanHealOwner); Assert.Equal(2, hound.HealOwnerDelay); Assert.Equal(12, hound.HealEndRange);
            Assert.True(HavenPetAbilities.Knows(hound, 4)); Assert.Equal(0, HavenPetTraining.HealingRank(hound));
            Assert.True(HavenPetAbilities.HasRoom(hound, 5));
            owner.MoveToWorld(new Point3D(4196, 2868, 0), Map.Trammel); hound.MoveToWorld(owner.Location, owner.Map);
            owner.Hits = 20; hound.Heal(owner); Assert.True(owner.Hits > 20);
            owner.Hits = 20; hound.MoveToWorld(new Point3D(owner.X + 20, owner.Y, owner.Z), owner.Map);
            hound.Heal(owner); Assert.Equal(20, owner.Hits);
            hound.HealStart(owner); hound.Delete(); Assert.True(hound.Deleted);
        }
        finally { hound.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void AbyssTrialHasConnectedSpawnFloorRequiresSkillsAndAwardsExactlyOneWildPet()
    {
        TileDataRequirement.SkipIfMissing();
        Assert.True(HavenAbyssTrial.TerrainReady());
        var player = Player(); var trial = new HavenAbyssTrial();
        try
        {
            trial.MoveToWorld(HavenAbyssTrial.Site, Map.TerMur); trial.Register();
            Assert.True(trial.TrySpawnPoint(out var point)); Assert.InRange(point.Z, -94, -90);
            Assert.True(HavenAbyssTrial.Go(player)); Assert.Equal(HavenAbyssTrial.Landing, player.Location);
            Assert.False(trial.Start(player));
            player.Skills.AnimalTaming.Cap = 120; player.Skills.AnimalLore.Cap = 120;
            player.Skills.AnimalTaming.Base = 110; player.Skills.AnimalLore.Base = 110;
            Assert.Equal(110, player.Skills.AnimalTaming.Base); Assert.Equal(110, player.Skills.AnimalLore.Base);
            Assert.True(player.InLOS(trial), "Brazier must be visible from arrival point");
            Assert.True(trial.Start(player)); Assert.False(trial.Start(player)); Assert.Equal(3, trial.Guardians.Count);
            HavenAbyssGuardian last = null;
            for (var wave = 1; wave <= 2; wave++)
            {
                Assert.Equal(wave, trial.Stage); trial.Spawn();
                foreach (var guard in trial.Guardians.ToArray())
                { last = guard; trial.Defeated(guard); guard.Delete(); }
            }
            Assert.Equal(3, trial.Stage); Assert.NotNull(trial.Hound);
            var hound = trial.Hound;
            Assert.True(hound.Tamable); Assert.False(hound.Controlled); Assert.InRange(hound.Z, -94, -90);
            trial.Defeated(last); trial.Spawn(); Assert.Same(hound, trial.Hound);
            Assert.True(hound.SetControlMaster(player));
            trial.Tick(); Assert.Equal(0, trial.Stage); Assert.Null(trial.Hound); Assert.False(hound.Deleted);
            Assert.False(trial.Start(player)); trial.Delete(); Assert.False(hound.Deleted); hound.Delete();
        }
        finally { trial.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void TrialCancellationDeletesOnlyItsWildActorsAndPreservesPreviouslyTamedPets()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(); var trial = new HavenAbyssTrial(); var hound = new HavenAncientHellhound();
        try
        {
            trial.MoveToWorld(HavenAbyssTrial.Site, Map.TerMur); trial.Register();
            trial.Hound = hound; trial.Stage = 3;
            hound.Owners.Add(player); trial.Finish(); Assert.False(hound.Deleted);
            var wild = new HavenAncientHellhound(); trial.Hound = wild; trial.Stage = 3;
            trial.Finish(); Assert.True(wild.Deleted);
            Assert.Equal(1, HavenAbyssTrial.RollRarity(.49)); Assert.Equal(2, HavenAbyssTrial.RollRarity(.50)); Assert.Equal(3, HavenAbyssTrial.RollRarity(.85));
        }
        finally { hound.Delete(); trial.Delete(); player.Delete(); }
    }
}
