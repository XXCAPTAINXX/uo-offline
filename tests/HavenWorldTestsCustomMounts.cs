using Server;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsCustomMounts
{
    public HavenWorldTestsCustomMounts() => _ = new HavenWorldTests();
    [SkippableTheory]
    [InlineData(HavenExpeditionKind.TameFrostmane, 70)]
    [InlineData(HavenExpeditionKind.TameVerdantLlama, 80)]
    [InlineData(HavenExpeditionKind.TameStormhorn, 105)]
    public void CustomMissionPetsAreMountableByMaleAndFemaleOwners(HavenExpeditionKind kind, double requirement)
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var pet = Assert.IsAssignableFrom<BaseMount>(HavenTamingMissions.CreatePet(kind));
        try
        {
            owner.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
            pet.MoveToWorld(owner.Location, owner.Map);
            Assert.True(pet.Tamable);
            Assert.Equal(requirement, pet.MinTameSkill);
            Assert.Equal(requirement, HavenTamingMissions.Requirement(kind));
            Assert.True(pet.SetControlMaster(owner));
            foreach (var female in new[] { false, true })
            {
                owner.Female = female;
                owner.Body = female ? 0x191 : 0x190;
                pet.OnDoubleClick(owner);
                Assert.Same(owner, pet.Rider);
                pet.Rider = null;
            }
        }
        finally { pet.Delete(); owner.Delete(); }
    }
}
