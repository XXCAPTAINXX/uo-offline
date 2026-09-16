using System;
using System.Collections.Generic;
using Server.Items;
using Server.Multis;
using Server.Regions;

namespace Server.HavenPrototype {

public static class HavenTreasureSites
{
    // Wilderness extents cross-checked against ServUO's published facet layout.
    // Candidate positions are deterministic, bounded and checked against this shard's actual terrain.
    private static readonly Rectangle2D[] MalasAreas = new Rectangle2D[]{new Rectangle2D(611,67,1862,705),new Rectangle2D(1540,852,286,182),new Rectangle2D(602,784,546,746),new Rectangle2D(1160,1035,1299,871)};
    private static readonly Rectangle2D[] TerMurAreas = new Rectangle2D[]{new Rectangle2D(535,2895,85,117),new Rectangle2D(525,3085,115,70),new Rectangle2D(755,2860,400,270),new Rectangle2D(1025,3280,190,100),new Rectangle2D(305,3445,175,255),new Rectangle2D(480,3540,90,110),new Rectangle2D(605,3880,200,170),new Rectangle2D(750,3830,80,80)};
    private static readonly Dictionary<Map, Point2D[]> Cache = new Dictionary<Map, Point2D[]>();
    public static bool Supported(Map map) => map == Map.Trammel || map == Map.Felucca || map == Map.Malas || map == Map.TerMur;
    public static string FacetName(Map map) => map == Map.TerMur ? "Ter Mur" : map?.Name ?? "Unknown facet";
    internal static IReadOnlyList<Point2D> Sites(Map map)
    {
        if (Cache.TryGetValue(map, out var existing)) { return existing; }
        var areas = map == Map.Malas ? MalasAreas : map == Map.TerMur ? TerMurAreas : Array.Empty<Rectangle2D>();
        var found = new List<Point2D>();
        foreach (var area in areas)
        {
            var count = 0;
            for (var y = area.Start.Y + 8; y < area.End.Y - 8 && count < 12; y += 24)
            {
                for (var x = area.Start.X + 8; x < area.End.X - 8 && count < 12; x += 24)
                {
                    if (!Clear(map, new Point2D(x, y))) { continue; }
                    found.Add(new Point2D(x, y)); count++;
                }
            }
        }
        var sites = found.ToArray(); Cache[map] = sites; return sites;
    }
    internal static bool Clear(Map map, Point2D point)
    {
        if (map == null || map == Map.Internal || point.X < 5 || point.Y < 5 || point.X >= map.Width - 5 || point.Y >= map.Height - 5) { return false; }
        var z = map.GetAverageZ(point.X, point.Y);
        var region = Region.Find(new Point3D(point, z), map);
        if (region.IsPartOf(typeof(TownRegion)) || region.IsPartOf(typeof(DungeonRegion)) || region.IsPartOf(typeof(HouseRegion)) ||
            BaseHouse.FindHouseAt(new Point3D(point, z), map, 16) != null) { return false; }
        for (var dx = -2; dx <= 2; dx++)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                var x = point.X + dx; var y = point.Y + dy;
                var tile = map.Tiles.GetLandTile(x, y);
                var flags = TileData.LandTable[tile.ID].Flags;
                if (tile.Ignored || (flags & (TileFlag.Impassable | TileFlag.Wet)) != 0 ||
                    !map.CanFit(x, y, map.GetAverageZ(x, y), 16, true, false) || Math.Abs(map.GetAverageZ(x, y) - z) > 2) { return false; }
            }
        }
        return true;
    }
    public static Point2D RandomLocation(Map map)
    {
        var sites = Sites(map);
        if (sites.Count == 0) { throw new InvalidOperationException($"No usable treasure sites on {FacetName(map)}. Check facet data."); }
        var start = Utility.Random(sites.Count);
        for (var i = 0; i < sites.Count; i++)
        {
            var candidate = sites[(start + i) % sites.Count];
            if (Clear(map, candidate)) { return candidate; }
        }
        // A temporarily occupied site behaves like an existing map: the obstruction must be cleared to dig.
        return sites[start];
    }
}

}
