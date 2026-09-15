using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Sixth;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsHomeTravel
{
    public HavenWorldTestsHomeTravel() => _ = new HavenWorldTests();

    private static PlayerMobile Player()
    {
        var player = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100, RawDex = 100, RawInt = 100 };
        player.AddItem(new Backpack());
        player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
        player.Hits = player.HitsMax;
        return player;
    }

    [SkippableFact]
    public void HomeStatusDistinguishesOwnershipCriminalAndDeathWithoutMovingPlayers()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Player();
        var stranger = Player();
        var estate = new HavenPirateEstate();
        try
        {
            estate.MoveToWorld(HavenPirateEstate.Site, Map.Trammel);
            estate.Build(owner);
            var origin = owner.Location;
            var strangerOrigin = stranger.Location;

            Assert.Null(estate.TravelBlockReason(owner));
            Assert.Equal("Ready", HavenPirateEstate.HomeStatus(owner));
            Assert.Equal(origin, owner.Location);
            Assert.Contains("belongs", estate.TravelBlockReason(stranger));
            Assert.Equal("No island is registered to this character.", HavenPirateEstate.HomeStatus(stranger));
            Assert.False(HavenPirateEstate.GoHome(stranger));
            Assert.Equal(strangerOrigin, stranger.Location);
            Assert.Same(Map.Trammel, stranger.Map);

            owner.Criminal = true;
            Assert.Contains("criminal", estate.TravelBlockReason(owner));
            Assert.Equal(estate.TravelBlockReason(owner), HavenPirateEstate.HomeStatus(owner));
            Assert.False(HavenPirateEstate.GoHome(owner));
            Assert.Equal(origin, owner.Location);
            Assert.True(owner.Criminal);

            owner.Criminal = false;
            owner.Body = 0x192;
            Assert.False(owner.Alive);
            Assert.Contains("alive", estate.TravelBlockReason(owner));
            Assert.Equal(estate.TravelBlockReason(owner), HavenPirateEstate.HomeStatus(owner));
            Assert.False(HavenPirateEstate.GoHome(owner));
            Assert.Equal(origin, owner.Location);
            Assert.Same(Map.Trammel, owner.Map);

            owner.Body = 0x190;
            Assert.Null(estate.TravelBlockReason(owner));
            Assert.Equal("Ready", HavenPirateEstate.HomeStatus(owner));
            Assert.Equal(origin, owner.Location);
        }
        finally
        {
            estate.Delete();
            owner.Delete();
            stranger.Delete();
        }
    }

    [SkippableFact]
    public void PendingMarkBlocksHomeUntilFinishedThenOwnerAndFollowingPetTravel()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Player();
        var estate = new HavenPirateEstate();
        var pet = new Dog();
        var spell = new MarkSpell(owner, null);
        try
        {
            estate.MoveToWorld(HavenPirateEstate.Site, Map.Trammel);
            estate.Build(owner);
            Assert.True(pet.SetControlMaster(owner));
            pet.ControlOrder = OrderType.Follow;
            pet.MoveToWorld(owner.Location, owner.Map);
            var origin = owner.Location;

            owner.Spell = spell;
            Assert.Contains("Finish or cancel", estate.TravelBlockReason(owner));
            Assert.Equal(estate.TravelBlockReason(owner), HavenPirateEstate.HomeStatus(owner));
            Assert.False(HavenPirateEstate.GoHome(owner));
            Assert.Equal(origin, owner.Location);
            Assert.Equal(origin, pet.Location);
            Assert.Same(Map.Trammel, owner.Map);
            Assert.Same(Map.Trammel, pet.Map);
            Assert.Same(spell, owner.Spell);

            spell.FinishSequence();
            Assert.Null(owner.Spell);
            Assert.Null(estate.TravelBlockReason(owner));
            Assert.Equal("Ready", HavenPirateEstate.HomeStatus(owner));
            Assert.Equal(origin, owner.Location);
            Assert.Equal(origin, pet.Location);
            Assert.True(HavenPirateEstate.GoHome(owner));
            Assert.Equal(new Point3D(4208, 2928, 0), owner.Location);
            Assert.Equal(owner.Location, pet.Location);
            Assert.Same(owner.Map, pet.Map);
            Assert.Same(owner, pet.ControlMaster);
        }
        finally
        {
            spell.FinishSequence();
            pet.Delete();
            estate.Delete();
            owner.Delete();
        }
    }

    [SkippableFact]
    public void HomeReportsNativeRecallRestrictionsAndBecomesReadyWhenTheyEnd()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Player();
        var estate = new HavenPirateEstate();
        var restricted = new NoTravelSpellsAllowedRegion("Home travel test restriction", Map.Trammel, 200,
            new Rectangle3D(owner.X - 2, owner.Y - 2, -128, 5, 5, 256));
        try
        {
            estate.MoveToWorld(HavenPirateEstate.Site, Map.Trammel);
            estate.Build(owner);
            var origin = owner.Location;
            restricted.Register();

            Assert.Contains("area", estate.TravelBlockReason(owner));
            Assert.Equal(estate.TravelBlockReason(owner), HavenPirateEstate.HomeStatus(owner));
            Assert.False(HavenPirateEstate.GoHome(owner));
            Assert.Equal(origin, owner.Location);
            Assert.Same(Map.Trammel, owner.Map);

            restricted.Unregister();
            Assert.Null(estate.TravelBlockReason(owner));
            Assert.Equal("Ready", HavenPirateEstate.HomeStatus(owner));
            Assert.Equal(origin, owner.Location);
        }
        finally
        {
            restricted.Unregister();
            estate.Delete();
            owner.Delete();
        }
    }
}
