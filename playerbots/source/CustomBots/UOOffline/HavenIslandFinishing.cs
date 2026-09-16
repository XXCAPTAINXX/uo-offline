using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;

namespace Server.UOOffline;

internal static partial class HavenIslandDecoration
{
    internal const string GardenMarker = "R.E.C. bordered gardens v1";
    internal static readonly Group[] FinishingPlan = MakeFinishingPlan();

    private static Group[] MakeFinishingPlan()
    {
        var groups = new List<Group>();
        void Add(string name,int id,int x,int y,int z=0)
            => groups.Add(new Group(name,false,new[] {new Tile(id,x,y,z)}));
        // Narrow enclosed beds soften the street without intruding into the walking lanes.
        foreach (var bed in new[] { (X:85,Y:122), (X:85,Y:92) })
        {
            for (var x=bed.X;x<=bed.X+2;x++)
            {
                for (var y=bed.Y;y<=bed.Y+5;y++)
                {
                    Add("R.E.C. border soil",0x31F4+(x+y)%4,x,y);
                    if (x==bed.X || x==bed.X+2 || y==bed.Y+5)
                    { Add("R.E.C. low planted border",0xC8F,x,y); }
                    else if (y!=125 && y!=126)
                    { Add("R.E.C. border flowers",(y&1)==0 ? 0xC83 : 0xC8C,x,y); }
                }
            }
            Add("R.E.C. garden palm",0xC95,bed.X+1,bed.Y);
        }
        // Native ivy is attached to real fences/posts, not suspended over the path.
        Add("R.E.C. supported ivy",0xCEB,75,121);
        Add("R.E.C. supported ivy",0xCEF,79,121);
        Add("R.E.C. supported ivy",0xCEC,90,119);
        Add("R.E.C. supported ivy",0xCEF,95,119);
        Add("R.E.C. supported ivy",0xCEC,99,119);
        return groups.ToArray();
    }

    private static void FinishSettlementGardens(HavenPirateEstate estate)
    {
        if (estate.Fixtures.Any(i=>i?.Deleted==false && i.Name==GardenMarker)) { return; }
        var added=new List<Item>();
        try
        {
            foreach (var group in FinishingPlan)
            {
                var tile=group.Tiles[0];var point=new Point3D(estate.X+tile.X,estate.Y+tile.Y,tile.Z);
                var ivy=group.Name=="R.E.C. supported ivy";
                var supported=false;
                foreach (var item in estate.Map.GetItemsInRange<Item>(point,0))
                {
                    if (!item.Visible || Math.Abs(item.Z-point.Z)>15) { continue; }
                    if (!estate.Fixtures.Contains(item) || item is not Static)
                    { throw new InvalidOperationException($"Garden site is occupied at {point}; existing property preserved."); }
                    if (item.Name is "R.E.C. notice shelter post" or "R.E.C. kitchen garden wall") { supported=true; }
                }
                if (ivy && !supported) { throw new InvalidOperationException($"Missing ivy support at {point}."); }
                if (!ivy && !estate.Map.CanFit(point,16,checkMobiles:false))
                { throw new InvalidOperationException($"Garden site is blocked at {point}."); }
            }
            foreach (var group in FinishingPlan)
            {
                var tile=group.Tiles[0];var item=new Static(tile.Id) { Name=group.Name,Movable=false };
                added.Add(item);item.MoveToWorld(new Point3D(estate.X+tile.X,estate.Y+tile.Y,tile.Z),estate.Map);estate.Fixtures.Add(item);
            }
            if (CheckSettlementRoutes(estate).Any(r=>!r.Reachable))
            { throw new InvalidOperationException("A planted border obstructed a settlement route."); }
            var marker=new Static(0x1F14) { Name=GardenMarker,Movable=false,Visible=false };
            added.Add(marker);marker.MoveToWorld(estate.Location,estate.Map);estate.Fixtures.Add(marker);estate.MarkDirty();
        }
        catch
        {
            foreach (var item in added) { estate.Fixtures.Remove(item);item.Delete(); }
            throw;
        }
    }
}
