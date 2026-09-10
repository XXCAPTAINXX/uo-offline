using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;

namespace Server.UOOffline;

public partial class HavenPirateHeadquarters
{
    internal void FurnishCraftStations()
    {
        if (Deleted || !HasCompound || CompanyFixtures.Any(i=>!i.Deleted && i.Name=="R.E.C. workshop stations v1")) { return; }
        if (Customizer!=null) { throw new InvalidOperationException("Finish house customization before furnishing workshops."); }
        var sites=new[] { new Point3D(9,-11,7),new(9,-6,7),new(6,-7,7),new(6,-12,7),new(6,-3,7),new(9,-11,27),new(-9,-12,7),new(9,-6,27),new(9,-9,7),new(9,-4,7),new(9,-4,27) };
        var tools=new[] {0x13E3,0xF9D,0x1034,0x1022,0x1EB8,0xE9B,0x97F,0xFBF,0xE1F,0x12B3,0x14EB};
        var replaced=CompanyFixtures.Where(i=>i is Static && i.Name is "Shipwright's workbench" or "Tinker's workbench").ToDictionary(i=>i,i=>i.Location);
        var added=new List<Item>();
        void Add(Item item,int x,int y,int z)
        { added.Add(item);Place(item,x,y,z-1); }
        try
        {
            foreach(var item in replaced.Keys) { item.Internalize(); }
            foreach(var site in sites.Concat(new[] { new Point3D(12,4,7),new(12,5,7),new(12,6,7),new(5,7,7) }))
            {
                var at=new Point3D(X+site.X,Y+site.Y,Z+site.Z);
                if(!IsInside(at,16)||!Map.CanFit(at,16,checkMobiles:false)) { throw new InvalidOperationException($"Workshop position is occupied at {at}. Existing furnishings were preserved."); }
            }
            for(var i=0;i<sites.Length;i++)
            {
                var p=sites[i];Add(new HavenCraftStation((HavenCraftStationKind)i),p.X,p.Y,p.Z);
                if(i!=7) { Add(new Static(tools[i]) { Name=HavenCraftStation.Names[i]+" - working tools" },p.X,p.Y,p.Z+6); }
            }
            Add(new MiningCart(MiningCartType.OreSouth) { Name="Company ore cart" },12,5,7);
            Add(new TreeStump(0xE58) { Name="Shipwright's seasoned log supply" },5,7,7);
            foreach(var site in DirectLadderSites)
            {
                var front=new Point3D(X+site.X,Y+site.Y+1,Z+site.Z);
                if(!Map.CanFit(front,16,checkMobiles:false)) { throw new InvalidOperationException("A furnishing blocked a ladder; installation was rolled back."); }
            }
            Add(new Static(1) { Name="R.E.C. workshop stations v1",Visible=false },2,14,7);
            foreach(var item in replaced.Keys) { CompanyFixtures.Remove(item);LockDowns.Remove(item);item.Delete(); }
            this.MarkDirty();
        }
        catch
        {
            foreach(var item in added)
            { CompanyFixtures.Remove(item);LockDowns.Remove(item);if(item is BaseAddon addon) { Addons.Remove(addon); }item.Delete(); }
            foreach(var entry in replaced) { if(!entry.Key.Deleted) { entry.Key.MoveToWorld(entry.Value,Map); } }
            throw;
        }
    }
}
