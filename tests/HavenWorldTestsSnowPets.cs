using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsSnowPets
{
    public HavenWorldTestsSnowPets() { _ = new HavenWorldTests(); }
    private static PlayerMobile Player()
    {
        var p = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100, RawDex = 100, RawInt = 100 };
        p.AddItem(new Backpack()); p.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); return p;
    }
    [SkippableFact]
    public void BearUsesRealMountArtRageAndCustomPetProgression()
    {
        TileDataRequirement.SkipIfMissing(); var p = Player(); var bear = new HavenSnowBear(); var foe = new Ogre();
        try
        {
            Assert.Equal(0xD5, bear.Body.BodyID); Assert.Equal(0x3EC5, bear.ItemID);
            Assert.InRange(bear.RawDex, 180, 210); Assert.Equal(bear.Dex, bear.StamMax); Assert.False(bear.StatLossAfterTame);
            Assert.True(HavenTamingMissions.IsCustomPet(bear)); HavenPetRarity.Apply(bear, 3); Assert.Equal(1, bear.ControlSlots);
            Assert.True(bear.SetControlMaster(p)); bear.MoveToWorld(p.Location, p.Map); foe.MoveToWorld(p.Location, p.Map);
            bear.Rider = p; Assert.Same(bear, p.Mount); bear.Rider = null;
            bear.Hits = bear.HitsMax; var damage = 20; bear.AlterMeleeDamageTo(foe, ref damage); Assert.Equal(20, damage);
            bear.Hits = bear.HitsMax / 3; damage = 20; bear.AlterMeleeDamageTo(foe, ref damage);
            Assert.Equal(30, damage); Assert.True(bear.Raging); Assert.False(bear.TryRage());
            Assert.Equal(2, HavenPetPool.Theme(bear));
        }
        finally { bear.Delete(); foe.Delete(); p.Delete(); }
    }
    [SkippableFact]
    public void DenIsOnSnowCreatesOneBearAndNeverDeletesATamedOrReleasedPet()
    {
        TileDataRequirement.SkipIfMissing(); var p = Player(); var den = HavenSnowBearDen.Install();
        Assert.NotNull(den); var bear = den.Bear;
        try
        {
            Assert.Same(den, HavenSnowBearDen.Install()); Assert.NotNull(bear);
            var land = Map.Tokuno.Tiles.GetLandTile(den.X, den.Y);
            Assert.Contains("snow", TileData.LandTable[land.ID].Name.ToLowerInvariant());
            den.Tick(); Assert.Same(bear, den.Bear); Assert.True(bear.InRange(den, 18));
            Assert.True(HavenSnowBearDen.TryFloor(den.Location, den.Map, out var point)); Assert.True(den.Map.CanSpawnMobile(point));
            Assert.Contains(HavenTravelGump.Entries(0, 4), e => e.Name?.StartsWith("Frostbound") == true);
            Assert.True(bear.SetControlMaster(p)); bear.Owners.Add(p); bear.SetControlMaster(null); den.Tick();
            Assert.Null(den.Bear); Assert.False(bear.Deleted); Assert.True(den.NextSpawn > Core.Now); den.Delete(); Assert.False(bear.Deleted);
        }
        finally { den.Delete(); bear?.Delete(); p.Delete(); }
    }
    [SkippableFact]
    public void DyesValidateOwnershipPreserveStatsAndRestoreOriginalAcrossShrinking()
    {
        TileDataRequirement.SkipIfMissing(); var p = Player(); var stranger = Player(); var bear = new HavenSnowBear();
        var dye = new HavenPetDye(); ShrunkenPet token = null;
        try
        {
            p.Backpack.DropItem(dye); bear.MoveToWorld(p.Location, p.Map); bear.Hue = 1175;
            Assert.False(dye.Apply(p, bear, 0)); Assert.False(dye.Deleted);
            Assert.True(bear.SetControlMaster(stranger)); Assert.False(dye.Apply(p, bear, 0));
            Assert.True(bear.SetControlMaster(p)); HavenPetRarity.Apply(bear, 2); var dex = bear.RawDex; var slots = bear.ControlSlots;
            bear.Rider = p; Assert.False(dye.Apply(p, bear, 0)); bear.Rider = null;
            Assert.True(dye.Apply(p, bear, 0)); Assert.True(dye.Deleted); Assert.Equal(HavenPetDye.Hues[0], bear.Hue);
            Assert.Equal(dex, bear.RawDex); Assert.Equal(slots, bear.ControlSlots); Assert.Equal(2, bear.Backpack.FindItemByType<HavenPetRarity>().Tier);
            dye = new HavenPetDye(); p.Backpack.DropItem(dye);
            token = new ShrunkenPet(bear, p); p.Backpack.DropItem(token); bear.SetControlMaster(null); bear.Internalize();
            Assert.True(dye.Apply(p, token, 8)); Assert.Equal(HavenPetAppearance.NaturalHue(bear), bear.Hue); Assert.Equal(bear.Hue, token.Hue);
            Assert.Null(bear.Backpack.FindItemByType<HavenPetDyeRecord>());
            var shop = new HavenTrainingStone.Menu(4); var bought = shop.CreateItem(6); Assert.IsType<HavenPetDye>(bought); bought.Delete();
        }
        finally { token?.Delete(); dye.Delete(); bear.Delete(); p.Delete(); stranger.Delete(); }
    }
}
