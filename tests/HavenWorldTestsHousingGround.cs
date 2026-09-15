using Server;
using Server.Mobiles;
using Server.Multis;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsHousingGround
{
    public HavenWorldTestsHousingGround() => _ = new HavenWorldTests();
    [Theory]
    [InlineData(16, 22, TileFlag.None, true)]
    [InlineData(15, 20, TileFlag.None, false)]
    [InlineData(20, 23, TileFlag.None, false)]
    [InlineData(20, 20, TileFlag.Wet, false)]
    [InlineData(20, 20, TileFlag.Impassable, false)]
    public void FoundationsBridgeOnlyShallowDryGround(int low, int high, TileFlag flags, bool allowed)
        => Assert.Equal(allowed, HavenHousing.SupportsFoundation(20, low, high, flags));

    [SkippableFact]
    public void RealIslandHouseCanSpanUnevenGround()
    {
        TileDataRequirement.SkipIfMissing();
        var map = Map.Trammel;
        var owner = new PlayerMobile { Player = true };
        var island = new Server.Regions.NoHousingRegion("Haven Island", map, 90, new Rectangle3D(3314, 2345, -128, 500, 750, 256));
        try
        {
            island.Register(); owner.MoveToWorld(new Point3D(3400, 2500, 0), map);
            var components = MultiData.GetComponents(0x64);
            for (var x = 3340; x < 3790; x += 5)
            {
                for (var y = 2490; y < 3070; y += 5)
                {
                    var center = new Point3D(x, y, map.GetAverageZ(x, y));
                    if (HousePlacement.Check(owner, 0x64, center, out _) != HousePlacementResult.Valid) { continue; }
                    foreach (var entry in components.List)
                    {
                        if (entry.OffsetZ == 0 && TileData.ItemTable[entry.ItemId & TileData.MaxItemValue].Wall &&
                            map.GetAverageZ(x + entry.OffsetX, y + entry.OffsetY) != center.Z)
                        { return; }
                    }
                }
            }
            Assert.Fail("No valid uneven-ground placement found on the real Haven map.");
        }
        finally { island.Unregister(); owner.Delete(); }
    }
}
