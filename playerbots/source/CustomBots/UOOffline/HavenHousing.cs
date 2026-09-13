using Server.Regions;

namespace Server.UOOffline;

public static class HavenHousing
{
    // Foundation walls can bridge shallow dips and cover small bumps without burying the floor.
    public static bool SupportsFoundation(int foundationZ, int landMinZ, int landMaxZ, TileFlag flags) =>
        (flags & (TileFlag.Impassable | TileFlag.Wet)) == 0 &&
        landMinZ >= foundationZ - 4 && landMaxZ <= foundationZ + 2;

    // Lift only Haven's blanket ban. Named shops, houses and special regions keep their restrictions.
    public static bool IsResidentialRegion(Region region, Map map, Point3D point) =>
        HavenNewcomerLuck.IsInArea(map, point) && !IsProtectedSite(point) &&
        (region is NoHousingRegion && region.Name == "Haven Island" ||
         region is TownRegion && region.Name == "New Haven");

    public static bool IsProtectedSite(Point3D point) =>
        point.X >= 3497 && point.X <= 3517 && point.Y >= 2568 && point.Y <= 2586 ||
        point.X >= 3678 && point.X <= 3718 && point.Y >= 2575 && point.Y <= 2615;

    public static bool BlocksFootprint(Region region, Map map, Point3D point) =>
        HavenNewcomerLuck.IsInArea(map, point) &&
        (IsProtectedSite(point) || !IsResidentialRegion(region, map, point) && region.IsPartOf<NoHousingRegion, NoHousingGuardedRegion>());
}
