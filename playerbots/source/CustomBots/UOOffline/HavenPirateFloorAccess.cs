using System;
using System.Linq;
using Server.Items;

namespace Server.UOOffline;

public partial class HavenPirateHeadquarters
{
    internal static readonly string[] FloorNames = { "Courtyard and receiving counter", "Guild hall - second floor", "Captain's quarters - third floor", "Workshop loft - maps and alchemy", "Barn loft - crew quarters" };
    internal static readonly Point3D[] FloorDestinations = { new(0,10,7), new(-8,-5,27), new(-8,-5,47), new(9,0,27), new(-9,10,27) };
    internal void RefineFloorAccess()
    {
        if (Deleted || !HasCompound || CompanyFixtures.Any(i => !i.Deleted && i.Name == "R.E.C. marked floor access v3")) { return; }
        if (Customizer != null) { throw new InvalidOperationException("Finish the active house customization before refining access."); }
        var ladders = CompanyFixtures.OfType<HavenPirateStair>().Where(i => !i.Deleted).OrderBy(i => i.Z).ToArray();
        if (ladders.Length != 3) { throw new InvalidOperationException("Expected the three original floor controls; existing furnishings were preserved."); }
        var sites = new[] { new Point3D(-2,1,7), new Point3D(-5,-4,27), new Point3D(-5,-4,47), new Point3D(5,1,7), new Point3D(5,1,27), new Point3D(-5,11,7), new Point3D(-5,11,27) };
        // All seven locations are on real floors, with adjacent walking space. Never hide player property underneath a ladder.
        foreach (var p in sites)
        {
            var point = new Point3D(X+p.X,Y+p.Y,Z+p.Z);
            foreach (var item in Map.GetItemsInRange<Item>(point,0))
            { if (item.Visible && item.Z >= point.Z && item.Z < point.Z+22 && !CompanyFixtures.Contains(item)) { throw new InvalidOperationException("A floor access location is occupied. Property was preserved."); } }
        }
        for (var i=0;i<sites.Length;i++)
        {
            var p=sites[i];
            if (i<3)
            { var ladder=ladders[i]; ladder.ItemID=0x8A5; ladder.Name="R.E.C. rope ladder - choose a floor"; ladder.MoveToWorld(new Point3D(X+p.X,Y+p.Y,Z+p.Z),Map); }
            else { Place(new HavenPirateStair { Headquarters=this },p.X,p.Y,p.Z-1); }
        }
        Place(new Static(1) { Name="R.E.C. marked floor access v3", Visible=false },0,14,6);
        this.MarkDirty();
    }
}
