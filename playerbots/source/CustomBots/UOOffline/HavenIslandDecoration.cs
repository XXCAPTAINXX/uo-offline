using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Multis;

namespace Server.UOOffline;

// Authored native artwork. A saved marker makes each scope a one-time furnishing pass.
// The original house design, possessions, secured containers and functional fixtures stay in place.
internal static partial class HavenIslandDecoration
{
    internal const string Marker = "R.E.C. lived-in island v1";
    internal readonly record struct Tile(int Id, int X, int Y, int Z);
    internal sealed record Group(string Name, bool House, Tile[] Tiles);

    internal static void Apply(HavenPirateEstate estate, HavenPirateHeadquarters house)
    {
        if (estate?.Deleted != false || estate.Map != Map.Trammel || estate.Fixtures.Count == 0) { return; }
        ApplyScope(estate, house, false);
        RefineChandlery(estate, house);
        if (house?.Deleted == false && house.Estate == estate && house.Customizer == null && house.HasCompound)
        { ApplyScope(estate, house, true); }
        RebuildSettlement(estate, house);
    }

    internal static bool Complete(List<Item> fixtures)
        => fixtures.Any(i => i?.Deleted == false && i.Name == Marker);

    private static void RefineChandlery(HavenPirateEstate estate, HavenPirateHeadquarters house)
    {
        const string refinement = "R.E.C. chandlery planting refinement";
        if (estate.Fixtures.Any(i => i?.Deleted == false && i.Name == refinement)) { return; }
        var plants = estate.Fixtures.Where(i => i?.Deleted == false && i is Static && i.Name == "Coastal undergrowth" &&
            i.X-estate.X is >= 93 and <= 99 && i.Y-estate.Y is >= 138 and <= 143).ToArray();
        var moved = new Dictionary<Item,Point3D>(); var added = new List<Item>();
        try
        {
            foreach (var plant in plants)
            {
                var relocated = false;
                for (var x = 101; x <= 104 && !relocated; x++)
                {
                    for (var y = 138; y <= 142 && !relocated; y++)
                    {
                        var point = new Point3D(estate.X+x,estate.Y+y,0);
                        if (!estate.Map.CanFit(point,16,checkMobiles:true)) { continue; }
                        var occupied = false;
                        foreach (var item in estate.Map.GetItemsInRange<Item>(point,0))
                        { if (item.Visible) { occupied = true; break; } }
                        if (occupied) { continue; }
                        moved.Add(plant,plant.Location);plant.MoveToWorld(point,estate.Map);relocated=true;
                    }
                }
                if (!relocated) { return; }
            }
            foreach (var group in Plan.Where(g => g.Name is "Chandlery shingled awning" or "Chandlery timber deck"))
            {
                foreach (var tile in group.Tiles)
                {
                    var point = new Point3D(estate.X+tile.X,estate.Y+tile.Y,tile.Z);
                    if (estate.Fixtures.Any(i => i?.Deleted == false && i.ItemID==tile.Id && i.Location==point && i.Name==group.Name)) { continue; }
                    if (!CanPlace(group,estate,house)) { continue; }
                    var item = new Static(tile.Id) { Name=group.Name,Movable=false };
                    added.Add(item);item.MoveToWorld(point,estate.Map);estate.Fixtures.Add(item);
                }
            }
            var marker = new Static(0x1F14) { Name=refinement,Movable=false,Visible=false };
            added.Add(marker);marker.MoveToWorld(estate.Location,estate.Map);estate.Fixtures.Add(marker);estate.MarkDirty();
        }
        catch
        {
            foreach (var item in added) { estate.Fixtures.Remove(item);item.Delete(); }
            foreach (var pair in moved) { pair.Key.MoveToWorld(pair.Value,estate.Map); }
            throw;
        }
    }

    private static void ApplyScope(HavenPirateEstate estate, HavenPirateHeadquarters house, bool indoors)
    {
        var fixtures = indoors ? house.CompanyFixtures : estate.Fixtures;
        if (Complete(fixtures)) { return; }
        Item origin = indoors ? house : estate;
        // Preflight before adding anything, so grouped foliage, table settings and roof components
        // may share a planned tile. Occupied sites are omitted, never cleared by deleting property.
        var accepted = Plan.Where(g => g.House == indoors && CanPlace(g, estate, house)).ToArray();
        var added = new List<Item>();
        try
        {
            foreach (var group in accepted)
            {
                foreach (var tile in group.Tiles)
                {
                    var item = new Static(tile.Id) { Name = group.Name, Movable = false };
                    if (tile.Id == 0xA15) { item.Light = LightType.Circle225; }
                    added.Add(item);
                    item.MoveToWorld(new Point3D(origin.X + tile.X, origin.Y + tile.Y, origin.Z + tile.Z), origin.Map);
                    if (indoors) { item.IsLockedDown = true; house.LockDowns.Add(item); }
                    fixtures.Add(item);
                }
            }
            if (!indoors)
            {
                // The original grove used bare-tree artwork. Give only those managed trees their
                // matching crown; player trees and existing fruit-tree addons are untouched.
                foreach (var trunk in fixtures.Where(i => i?.Deleted == false && i.Name == "Corsair's grove" && i.ItemID == 0xCCA).ToArray())
                {
                    var leaves = new Static(0xCCE) { Name = "Corsair's grove canopy", Movable = false };
                    added.Add(leaves); leaves.MoveToWorld(trunk.Location, trunk.Map); fixtures.Add(leaves);
                }
            }
            var marker = new Static(0x1F14) { Name = Marker, Visible = false, Movable = false };
            added.Add(marker); marker.MoveToWorld(origin.Location, origin.Map); fixtures.Add(marker);
            if (!indoors)
            {
                foreach (var trunk in fixtures)
                { if (trunk?.Deleted == false && trunk.Name == "Corsair's grove" && trunk.ItemID == 0xCCA) { trunk.ItemID = 0xCCC; } }
            }
            origin.MarkDirty();
        }
        catch
        {
            foreach (var item in added)
            { fixtures.Remove(item); if (indoors) { house.LockDowns.Remove(item); } item.Delete(); }
            throw;
        }
    }

    internal static bool CanPlace(Group group, HavenPirateEstate estate, HavenPirateHeadquarters house)
    {
        Item origin = group.House ? house : estate;
        if (origin?.Deleted != false || origin.Map == null || origin.Map == Map.Internal) { return false; }
        foreach (var tile in group.Tiles)
        {
            if (!group.House && (tile.X is < 25 or > 150 || tile.Y is < 25 or > 164 ||
                tile.X is >= 46 and <= 90 && tile.Y is >= 46 and <= 85 ||
                Math.Abs(tile.X - 70) <= 14 && Math.Abs(tile.Y - 36) <= 14)) { return false; }
            var floor = group.House ? 7 + Math.Max(0, (tile.Z - 7) / 20) * 20 : 0;
            var point = new Point3D(origin.X + tile.X, origin.Y + tile.Y, origin.Z + floor);
            if (group.House)
            {
                if (!house.IsInside(point, 16)) { return false; }
                foreach (var site in HavenPirateHeadquarters.DirectLadderSites.Concat(HavenPirateHeadquarters.DirectLadderLandings))
                { if (site.Z == floor && Math.Abs(site.X - tile.X) <= 1 && Math.Abs(site.Y - tile.Y) <= 1) { return false; } }
            }
            else if (BaseHouse.FindHouseAt(point, origin.Map, 16) != null) { return false; }
            if (!origin.Map.CanFit(point, 16, checkMobiles: false)) { return false; }
            foreach (var item in origin.Map.GetItemsInRange<Item>(point, 0))
            {
                if (!item.Visible || item == origin || Math.Abs(item.Z - point.Z) > 15) { continue; }
                // A zero-height managed floor is safe under a path or a roof support.
                var managed = estate.Fixtures.Contains(item) || house?.CompanyFixtures.Contains(item) == true;
                if (managed && item is Static && item.ItemData.Height == 0) { continue; }
                return false;
            }
        }
        return true;
    }

    internal static object Snapshot(HavenPirateEstate estate, HavenPirateHeadquarters house)
    {
        // Only estate-owned fixture lists, never world-wide searches or player follower lists.
        var names = Plan.Concat(SettlementPlan).Select(g => g.Name).ToHashSet();
        object[] Tiles(List<Item> items, Item origin)
            => items.Where(i => i?.Deleted == false && (names.Contains(i.Name ?? "") || i.Name == "Corsair's grove canopy"))
                .Select(i => (object)new { Id = i.ItemID, X = i.X-origin.X, Y = i.Y-origin.Y, Z = i.Z-origin.Z, i.Name }).ToArray();
        return new { EstateComplete = Complete(estate.Fixtures), HouseComplete = house != null && Complete(house.CompanyFixtures),
            SettlementComplete = estate.Fixtures.Any(i => i?.Deleted == false && i.Name == SettlementMarker),
            Routes = CheckSettlementRoutes(estate),
            ManagedOutside = estate.Fixtures.Where(i => i?.Deleted == false && i.Visible && i.Map == estate.Map)
                .SelectMany(i => i is BaseAddon addon ? addon.Components.Cast<Item>() : new[] { i })
                .Select(i => new { Id = i.ItemID, X = i.X-estate.X, Y = i.Y-estate.Y, Z = i.Z, i.Name }).ToArray(),
            Outside = Tiles(estate.Fixtures, estate), Inside = house == null ? Array.Empty<object>() : Tiles(house.CompanyFixtures, house) };
    }
}
