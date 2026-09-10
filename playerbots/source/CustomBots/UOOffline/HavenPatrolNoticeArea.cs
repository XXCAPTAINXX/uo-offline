using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
namespace Server.UOOffline;
public partial class HavenPirateEstate
{
    internal void RefinePatrolNoticeArea()
    {
        if(HomePatrol==null || Fixtures.Any(i=>!i.Deleted && i.Name=="R.E.C. dockside notice platform")) { return; }
        var old=Fixtures.Where(i=>i is Static && i.Name is "Weathered dispatch shelter" or "Dispatch shelter awning" or "Timber awning post" or "Patrol supplies" or "The watchkeeper's lantern").ToDictionary(i=>i,i=>i.Location);
        var added=new List<Item>();
        void Add(Item item,int x,int y,int z=0)
        { item.Movable=false;added.Add(item);item.MoveToWorld(new Point3D(X+x,Y+y,z),Map); }
        try
        {
            foreach(var item in old.Keys) { item.Internalize(); }
            for(var x=75;x<=79;x++)
            for(var y=123;y<=126;y++)
            {
                var at=new Point3D(X+x,Y+y,0);
                foreach(var item in Map.GetItemsInRange<Item>(at,0))
                { if(item.Visible && item!=HomePatrol && !Fixtures.Contains(item)) { throw new InvalidOperationException("The notice platform is occupied; existing property was preserved."); } }
                Add(new Static(0x4A9) { Name="R.E.C. dockside notice platform" },x,y);
            }
            Add(new Static(0x14F8) { Name="Coiled patrol mooring rope" },76,123);
            Add(new Static(0xE3F) { Name="Patrol dispatch crate" },78,125);
            Add(new Static(0xE77) { Name="Watchkeeper's supply barrel" },79,123);
            Add(new Static(0xA25) { Name="Dockside notice lantern", Light=LightType.Circle225 },79,123,6);
            Add(new Static(0xB2D) { Name="Watchkeeper's chair" },75,125);
            foreach(var entry in old) { Fixtures.Remove(entry.Key);entry.Key.Delete(); }
            Fixtures.AddRange(added);this.MarkDirty();
        }
        catch
        {
            foreach(var item in added) { item.Delete(); }
            foreach(var entry in old) { if(!entry.Key.Deleted) { entry.Key.MoveToWorld(entry.Value,Map); } }
            throw;
        }
    }
}
