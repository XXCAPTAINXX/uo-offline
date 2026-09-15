using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Multis;

namespace Server.UOOffline;

internal static partial class HavenIslandDecoration
{
    internal const string SettlementMarker = "R.E.C. connected settlement v2";

    private static void RebuildSettlement(HavenPirateEstate estate, HavenPirateHeadquarters house)
    {
        if (estate.Fixtures.Any(i => i?.Deleted == false && i.Name == SettlementMarker)) { return; }
        // Only replace exact, authored outdoor fixtures. Never touch a container, an addon,
        // a spawner, player possessions or the custom house's design/furnishings.
        var oldTiles = Plan.Where(g => !g.House).SelectMany(g => g.Tiles.Select(t =>
            (g.Name, t.Id, t.X, t.Y, t.Z))).ToHashSet();
        var old = estate.Fixtures.Where(i => i?.Deleted == false && i is Static &&
            (oldTiles.Contains((i.Name, i.ItemID, i.X-estate.X, i.Y-estate.Y, i.Z-estate.Z)) ||
             i.Name is "Coastal undergrowth" or "Coiled patrol mooring rope" or "Patrol dispatch crate" or
                 "Watchkeeper's supply barrel" or "Dockside notice lantern" or "Watchkeeper's chair"))
            .ToDictionary(i => i, i => i.Location);
        var added = new List<Item>();
        try
        {
            foreach (var item in old.Keys) { item.Internalize(); }
            // Floors can go under existing functional objects (oven, garden crops, board).
            // Every structure is preflighted as a whole: no missing posts or patchwork roofs.
            foreach (var group in SettlementPlan)
            {
                foreach (var tile in group.Tiles)
                {
                    var point = new Point3D(estate.X+tile.X, estate.Y+tile.Y, tile.Z);
                    var floor = tile.Z == 0 && TileData.ItemTable[tile.Id].Height == 0;
                    if (tile.X is < 25 or > 150 || tile.Y is < 25 or > 164 ||
                        BaseHouse.FindHouseAt(point, estate.Map, 16) != null)
                    { throw new InvalidOperationException($"Settlement plan enters protected ground at {point}."); }
                    if (floor || tile.Z >= 20) { continue; }
                    if (!estate.Map.CanFit(new Point3D(point.X,point.Y,0),16,checkMobiles:false))
                    { throw new InvalidOperationException($"Settlement structure is obstructed at {point} ({group.Name})."); }
                    foreach (var item in estate.Map.GetItemsInRange<Item>(point,0))
                    {
                        if (!item.Visible || Math.Abs(item.Z-point.Z)>15 ||
                            item is Static && item.ItemData.Height == 0 && estate.Fixtures.Contains(item)) { continue; }
                        throw new InvalidOperationException($"Settlement site contains an existing item at {point} ({group.Name}); property preserved.");
                    }
                }
            }
            foreach (var group in SettlementPlan)
            {
                foreach (var tile in group.Tiles)
                {
                    var item = new Static(tile.Id) { Name = group.Name, Movable = false };
                    if (tile.Id == 0xB20) { item.Light = LightType.Circle225; }
                    added.Add(item);
                    item.MoveToWorld(new Point3D(estate.X+tile.X,estate.Y+tile.Y,tile.Z),estate.Map);
                    estate.Fixtures.Add(item);
                }
            }
            var routes = CheckSettlementRoutes(estate);
            if (routes.Any(r => !r.Reachable))
            { throw new InvalidOperationException("Settlement route failed: " + string.Join(", ",routes.Where(r => !r.Reachable).Select(r => r.Destination))); }
            var marker = new Static(0x1F14) { Name = SettlementMarker, Movable = false, Visible = false };
            added.Add(marker);marker.MoveToWorld(estate.Location,estate.Map);estate.Fixtures.Add(marker);
            foreach (var item in old.Keys) { estate.Fixtures.Remove(item);item.Delete(); }
            estate.MarkDirty();
        }
        catch
        {
            foreach (var item in added) { estate.Fixtures.Remove(item);item.Delete(); }
            foreach (var pair in old) { pair.Key.MoveToWorld(pair.Value,estate.Map); }
            throw;
        }
    }

    internal sealed record SettlementRoute(string Destination, bool Reachable);
    internal static SettlementRoute[] CheckSettlementRoutes(HavenPirateEstate estate)
    {
        // Traverse actual walkable route tiles, not just the authored polyline. This detects
        // gaps, blocking fixtures, and disconnected work areas before committing the migration.
        var floors = estate.Fixtures.Where(i => i?.Deleted == false && i.Map==estate.Map &&
            i.Z==0 && i is Static && i.ItemData.Height<=1 &&
            (i.Name is "R.E.C. connected paving" or "R.E.C. kitchen courtyard" or "R.E.C. dockside notice platform" ||
             i.Name?.EndsWith(" deck") == true || i.ItemID==0x7CD))
            .Select(i => (X:i.X-estate.X,Y:i.Y-estate.Y)).ToHashSet();
        var visited = new HashSet<(int X,int Y)>();var pending = new Queue<(int X,int Y)>();
        pending.Enqueue((82,128));
        while (pending.Count>0)
        {
            var p = pending.Dequeue();
            if (visited.Contains(p) || !floors.Contains(p) ||
                !(estate.Map.CanFit(new Point3D(estate.X+p.X,estate.Y+p.Y,0),16,checkMobiles:false) ||
                  estate.Map.CanFit(new Point3D(estate.X+p.X,estate.Y+p.Y,1),16,checkMobiles:false))) { continue; }
            visited.Add(p);
            pending.Enqueue((p.X-1,p.Y));pending.Enqueue((p.X+1,p.Y));
            pending.Enqueue((p.X,p.Y-1));pending.Enqueue((p.X,p.Y+1));
        }
        return SettlementDestinations.Select(p => new SettlementRoute($"{p.X},{p.Y}",visited.Contains((p.X,p.Y)))).ToArray();
    }
}
