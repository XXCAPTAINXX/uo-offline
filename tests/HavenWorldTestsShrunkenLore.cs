using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsShrunkenLore
{
    public HavenWorldTestsShrunkenLore() => _ = new HavenWorldTests();
    [SkippableFact]
    public void StoredPetInspectionIsExactReadOnlyAndOwnerOnly()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 }; var stranger = new PlayerMobile { Player = true, Body = 0x190 };
        var pet = new HavenFrostmane(); var token = new ShrunkenPet(pet, owner);
        try
        {
            owner.AddItem(new Backpack()); stranger.AddItem(new Backpack());
            owner.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
            stranger.MoveToWorld(owner.Location, owner.Map);
            owner.Backpack.DropItem(token);
            var dex = pet.RawDex; var followers = owner.Followers;
            Assert.Same(pet, token.Inspect(owner)); Assert.Null(token.Inspect(stranger));
            var gump = new HavenAnimalLoreGump(owner, pet);
            Assert.True(gump.CanRefresh(owner)); Assert.False(gump.CanRefresh(stranger));
            Assert.Equal(Map.Internal, pet.Map); Assert.Null(pet.ControlMaster); Assert.Equal(dex, pet.RawDex);
            Assert.Equal(followers, owner.Followers); Assert.False(token.Deleted);
            stranger.Backpack.DropItem(token);
            Assert.Null(token.Inspect(owner)); Assert.Null(token.Inspect(stranger)); Assert.False(gump.CanRefresh(owner));
        }
        finally { token.Pet = null; token.Delete(); pet.Delete(); owner.Delete(); stranger.Delete(); }
    }
}
