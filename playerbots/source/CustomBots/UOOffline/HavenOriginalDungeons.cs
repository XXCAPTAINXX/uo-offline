using System;
using System.Collections.Generic;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.Logging;

namespace Server.UOOffline;

public static class HavenOriginalDungeons
{
    public static void Initialize()
    {
        CommandSystem.Register("HavenOriginalDungeonsSetup", AccessLevel.Administrator, e =>
        {
            var reason = Installed ? null : Preflight();
            if (reason != null) { e.Mobile.SendMessage($"Original dungeon setup refused: {reason}"); return; }
            Migrate();
            e.Mobile.SendMessage("Original Shadowguard and Blackthorn locations are ready; saved progression was preserved.");
        });
    }
    // The current client has a decorative surface at z=26 on the traditional kick tile.
    internal static readonly Point3D ShadowEntrance = new(505, 2192, 26);
    internal static readonly Point3D BlackthornLanding = new(6432, 2677, 0);
    internal static readonly Point3D[] Centers =
    {
        new(96, 2016, -20), new(224, 2016, -20), new(352, 2016, -20),
        new(480, 2016, -20), new(160, 2080, -20), new(64, 2336, 0)
    };
    internal static readonly Point2D[] TreeOffsets =
    {
        new(-10,-11), new(-18,-15), new(-11,-19), new(-17,-10),
        new(-21,10), new(-17,16), new(-13,12), new(-11,18),
        new(10,-20), new(10,-11), new(14,-15), new(17,-10),
        new(10,10), new(9,16), new(13,16), new(15,10)
    };
    internal static bool Installed
    {
        get
        {
            if (HavenShadowChamber.Registry.Count != 6) { return false; }
            foreach (var room in HavenShadowChamber.Registry) { if (!room.Original) { return false; } }
            foreach (var battle in HavenFrontierBattle.Registry) { if (!battle.Pirate) { return battle.Original; } }
            return false;
        }
    }
    internal static Point3D SpawnPoint(Map map, Point3D preferred, Point3D center, bool sameLevel = false)
    {
        for (var radius = 0; radius <= 12; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (radius > 0 && Math.Abs(dx) != radius && Math.Abs(dy) != radius) { continue; }
                    var x = preferred.X + dx; var y = preferred.Y + dy;
                    foreach (var z in new[] { preferred.Z, map.GetAverageZ(x, y) })
                    {
                        if (sameLevel && z != preferred.Z) { continue; }
                        var p = new Point3D(x, y, z);
                        if (map.CanSpawnMobile(p) && map.LineOfSight(new Point3D(center.X,center.Y,center.Z+14),new Point3D(p.X,p.Y,p.Z+14))) { return p; }
                    }
                }
            }
        }
        throw new InvalidOperationException($"No usable spawn near {preferred} on {map}.");
    }
    internal static string Preflight()
    {
        if (Installed) { return null; }
        if (HavenFrontierHub.Registry.Count != 1 || HavenShadowChamber.Registry.Count != 6 || HavenFrontierBattle.Registry.Count != 2)
        { return "Expected the complete existing frontier installation."; }
        foreach (var room in HavenShadowChamber.Registry)
        { if (room.Active || room.Original) { return "A room is active or partially migrated."; } }
        foreach (var battle in HavenFrontierBattle.Registry)
        { if (!battle.Pirate && (battle.Active || battle.Original)) { return "Blackthorn is active or partially migrated."; } }
        foreach (var p in new[] { ShadowEntrance, new Point3D(519,2192,25), new Point3D(56,2328,0) })
        { if (!Map.TerMur.CanFit(p, 16, checkMobiles: false)) { return $"Original Shadowguard terrain blocked at {p}."; } }
        foreach (var p in new[] { BlackthornLanding, new Point3D(1477,1473,-8), new Point3D(6317,2555,0) })
        { if (!Map.Trammel.CanFit(p, 16, checkMobiles: false)) { return $"Original Blackthorn terrain blocked at {p}."; } }
        foreach (var center in Centers)
        {
            foreach (var item in Map.TerMur.GetItemsInRange<Item>(center, 27))
            { if (!item.Deleted && item.Parent == null) { return $"Existing item {item.Serial} occupies a Shadowguard instance."; } }
            foreach (var mobile in Map.TerMur.GetMobilesInRange<Mobile>(center, 27))
            { if (!mobile.Deleted) { return $"Existing mobile {mobile.Serial} occupies a Shadowguard instance."; } }
        }
        for (var i = 0; i < 5; i++)
        {
            var source = new Point3D(1477,1471+i,-8);
            var error = (Map.Trammel.CanFit(source,16,checkMobiles:false) ? PassageConflict(source,new Point3D(6432,2677+i,0)) : null) ??
                PassageConflict(new Point3D(6440,2677+i,20),new Point3D(1477,1473,-8));
            if (error != null) { return error; }
        }
        return null;
    }
    private static string PassageConflict(Point3D source, Point3D target)
    {
        foreach (var existing in Map.Trammel.GetItemsInRange<Teleporter>(source,0))
        {
            if (existing.Z == source.Z && (existing.PointDest != target || existing.MapDest != Map.Trammel) && !StandardExit(existing, source))
            { return $"Different teleporter already occupies {source}."; }
        }
        return null;
    }
    // Existing standard exits may point at the blocked outer edge of the castle stairs.
    // Only that exact stock mapping is eligible for correction; preserve unrelated teleporters.
    private static bool StandardExit(Teleporter existing, Point3D source) =>
        existing.GetType() == typeof(Teleporter) && existing.MapDest == Map.Trammel &&
        source.X == 6440 && source.Y >= 2677 && source.Y <= 2681 && source.Z == 20 &&
        existing.PointDest == new Point3D(1477, source.Y - 1206, -8);
    internal static bool Migrate()
    {
        if (Installed) { return true; }
        var problem = Preflight();
        if (problem != null) { throw new InvalidOperationException(problem); }
        HavenFrontierHub hub = null;
        foreach (var candidate in HavenFrontierHub.Registry) { hub = candidate; }
        var oldShadow = new Point3D(4760,3242,0); var oldRift = new Point3D(4888,3466,0);
        // Finish on the old map before changing any coordinate-dependent cleanup behavior.
        foreach (var room in HavenShadowChamber.Registry)
        {
            room.Finish(false); Rescue(room.Location, 17, ShadowEntrance, Map.TerMur);
            foreach (var fixture in room.Fixtures) { fixture?.Delete(); } room.Fixtures.Clear();
        }
        foreach (var battle in HavenFrontierBattle.Registry)
        {
            if (battle.Pirate) { continue; }
            battle.Cancel(); Rescue(battle.Center, 40, BlackthornLanding, Map.Trammel);
            foreach (var fixture in battle.Fixtures) { fixture?.Delete(); } battle.Fixtures.Clear();
            battle.MoveToWorld(new Point3D(6430,2677,0), Map.Trammel); battle.Build(false);
        }
        Rescue(oldShadow, 8, ShadowEntrance, Map.TerMur); Rescue(oldRift, 8, BlackthornLanding, Map.Trammel);
        foreach (var room in new List<HavenShadowChamber>(HavenShadowChamber.Registry))
        { room.MoveToWorld(Centers[(int)room.Room], Map.TerMur); room.Build(room.Room); }
        foreach (var fixture in hub.Owned)
        {
            if (fixture == null || fixture.Deleted) { continue; }
            if (fixture is HavenShadowEntrance) { fixture.ItemID = 0x468B; fixture.MoveToWorld(new Point3D(501,2192,50), Map.TerMur); }
            else if (fixture is HavenCommonsReturnGate && Utility.InRange(fixture.Location, oldShadow, 6))
            { fixture.MoveToWorld(new Point3D(507,2197,25), Map.TerMur); }
            else if (fixture is AnkhWest && Utility.InRange(fixture.Location, oldShadow, 6))
            { fixture.MoveToWorld(new Point3D(503,2191,25), Map.TerMur); }
            else if (fixture is HavenCommonsReturnGate && Utility.InRange(fixture.Location, oldRift, 6))
            { fixture.MoveToWorld(new Point3D(6430,2682,0), Map.Trammel); }
            else if (fixture is AnkhWest && Utility.InRange(fixture.Location, oldRift, 6))
            { fixture.MoveToWorld(new Point3D(6428,2677,0), Map.Trammel); }
        }
        for (var i = 0; i < 5; i++)
        {
            var source = new Point3D(1477,1471+i,-8); var target = new Point3D(6432,2677+i,0);
            if (Map.Trammel.CanFit(source,16,checkMobiles:false) && Map.Trammel.CanFit(target,16,checkMobiles:false)) { Passage(hub,source,target); }
            source = new Point3D(6440,2677+i,20);
            if (Map.Trammel.CanFit(source,16,checkMobiles:false)) { Passage(hub,source,new Point3D(1477,1473,-8)); }
        }
        foreach (var y in new[] {2188,2189,2192,2193})
        { Place(hub,new MetalDoor(y % 2 == 0 ? DoorFacing.NorthCCW : DoorFacing.SouthCW) { Hue = 1779 },new Point3D(519,y,25),Map.TerMur); }
        foreach (var p in new[] {new Point3D(6409,2695,0),new Point3D(6410,2695,0),new Point3D(6409,2664,0),new Point3D(6410,2664,0)})
        { Place(hub,new MetalDoor(p.X % 2 == 1 ? DoorFacing.WestCW : DoorFacing.EastCCW),p,Map.Trammel); }
        foreach (var p in new[] {new Point3D(6394,2680,0),new Point3D(6394,2679,0),new Point3D(6425,2680,0),new Point3D(6425,2679,0)})
        { Place(hub,new MetalDoor(p.Y % 2 == 0 ? DoorFacing.SouthCW : DoorFacing.NorthCCW),p,Map.Trammel); }
        hub.MarkDirty();
        LogFactory.GetLogger(typeof(HavenOriginalDungeons)).Information("Original Shadowguard and Blackthorn locations installed; character progression retained.");
        return true;
    }
    private static void Place(HavenFrontierHub hub, Item item, Point3D p, Map map)
    { hub.Owned.Add(item); item.MoveToWorld(p,map); }
    private static void Passage(HavenFrontierHub hub, Point3D source, Point3D target)
    {
        foreach (var existing in Map.Trammel.GetItemsInRange<Teleporter>(source,0))
        {
            if (existing.Z == source.Z)
            {
                if (existing.PointDest == target && existing.MapDest == Map.Trammel) { return; }
                if (StandardExit(existing, source)) { existing.PointDest = target; return; }
                throw new InvalidOperationException($"Different teleporter already occupies {source}.");
            }
        }
        Place(hub,new Teleporter(target,Map.Trammel),source,Map.Trammel);
    }
    private static void Rescue(Point3D oldCenter, int radius, Point3D target, Map map)
    {
        var mobiles = new List<Mobile>(); var salvage = new List<Item>();
        foreach (var mobile in Map.Trammel.GetMobilesInRange<Mobile>(oldCenter,radius)) { mobiles.Add(mobile); }
        foreach (var item in Map.Trammel.GetItemsInRange<Item>(oldCenter,radius))
        { if (item.Parent == null && (item.Movable || item is Corpse)) { salvage.Add(item); } }
        foreach (var mobile in mobiles) { HavenFrontierSupport.Return(mobile,target,map); }
        foreach (var item in salvage) { item.MoveToWorld(target,map); }
        foreach (var account in Accounts.GetAccounts())
        {
            for (var i = 0; i < account.Length; i++)
            {
                var player = account[i];
                if (player?.Map == Map.Internal && player.LogoutMap == Map.Trammel && Utility.InRange(player.LogoutLocation,oldCenter,radius))
                { player.LogoutLocation = target; player.LogoutMap = map; }
            }
        }
    }
}
