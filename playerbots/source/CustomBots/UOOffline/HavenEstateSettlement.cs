using System;
using System.Linq;
using Server.Engines.Spawners;
using Server.Items;
using Server.Multis;

namespace Server.UOOffline;

public partial class HavenPirateEstate
{
    // A single migration marker lives in the estate's owned fixture list. Player property is never moved.
    internal void DecorateSettlement()
    {
        if (Deleted || Map == null || Map == Map.Internal || Fixtures.Count == 0 ||
            Fixtures.Any(i => i?.Deleted == false && i.Name == "Corsair settlement plan")) { return; }
        var trees = Fixtures.Where(i => i?.Deleted == false && i.Name == "Corsair's grove").ToArray();
        var grove = new (int X, int Y)[] { (99,44),(106,46),(114,43),(121,48),(102,52),(111,54),(120,56),
            (98,61),(108,64),(117,66),(123,61),(104,69),(113,72),(96,48),(125,52),(116,58),(101,57),(122,70),(96,67),(109,40) };
        for (var i = 0; i < trees.Length; i++)
        {
            var p = grove[i % grove.Length];
            if (ClearSettlementSite(p.X, p.Y)) { trees[i].MoveToWorld(new Point3D(X + p.X, Y + p.Y, 0), Map); }
        }
        // Small cultivated patches west of the landing; paths and the 40x40 housing plot stay open.
        CropPatch(56, 91, nameof(FarmableCarrot), "Carrot patch");
        CropPatch(62, 93, nameof(FarmableCabbage), "Kitchen garden");
        CropPatch(54, 99, nameof(FarmableWheat), "Island grain patch");
        CropPatch(67, 96, nameof(FarmableCotton), "Sailmaker's cotton");
        Settle(new WaterTroughEastAddon { Name = "Collected rainwater" }, 72, 98);
        Settle(new Static(0xE77) { Name = "Rainwater barrel" }, 70, 97);
        Settle(new Static(0xE77) { Name = "Rainwater barrel" }, 71, 99);
        Settle(new Static(0xF39) { Name = "A worn garden shovel" }, 60, 97);
        Settle(new Static(0xE7A) { Name = "A basket for the harvest" }, 65, 92);
        // Orchard and a modest outdoor kitchen, with supplies grouped by use.
        Settle(new AppleTreeAddon(), 91, 104);
        Settle(new PeachTreeAddon(), 96, 109);
        Settle(new AppleTreeAddon(), 99, 102);
        Settle(new StoneOvenEastAddon { Name = "The castaways' oven" }, 96, 122);
        Settle(new Static(0xB90) { Name = "Camp kitchen table" }, 93, 123);
        Settle(new Static(0x9D7) { Name = "Fresh island provisions" }, 93, 123, 6);
        Settle(new Static(0xB2D) { Name = "A stool by the kitchen" }, 92, 124);
        Settle(new Static(0xB2D) { Name = "A stool by the kitchen" }, 94, 124);
        Settle(new Static(0x1BDD) { Name = "Dry firewood" }, 97, 120);
        Settle(new Static(0xE3D) { Name = "Sacks of island grain" }, 97, 124);
        Settle(new Static(0xB20) { Name = "The kitchen lantern", Light = LightType.Circle225 }, 91, 122);
        // Dockside work area: rope, nets, a catch barrel and weathered cargo.
        Settle(new Static(0x14F8) { Name = "Coiled mooring rope" }, 86, 147);
        Settle(new Static(0xDCA) { Name = "A net drying on the dock" }, 88, 154);
        Settle(new Static(0xE77) { Name = "Salted catch barrel" }, 92, 159);
        Settle(new Static(0xE3F) { Name = "Dockside tackle chest" }, 96, 160);
        Settle(new Static(0xDBF) { Name = "Fishing tackle" }, 97, 161);
        foreach (var p in new[] { (74,104),(76,110),(73,116),(98,132),(102,136),(94,141),(66,128),(68,133),(119,110),(125,117) })
        { Settle(new Static((p.Item1 & 1) == 0 ? 0xC8F : 0xCC7) { Name = "Coastal undergrowth" }, p.Item1, p.Item2); }
        var marker = new Static(0x1F14) { Name = "Corsair settlement plan", Visible = false };
        Place(marker, 80, 129); this.MarkDirty();
    }
    private bool ClearSettlementSite(int x, int y)
    {
        var p = new Point3D(X + x, Y + y, 0);
        if (x >= 46 && x <= 90 && y >= 46 && y <= 89 || BaseHouse.FindHouseAt(p, Map, 20) != null ||
            !Map.CanSpawnMobile(p)) { return false; }
        foreach (var item in Map.GetItemsInRange(p, 0))
        { if (!Fixtures.Contains(item) && item.Visible && item is not BaseAddon) { return false; } }
        return true;
    }
    private void Settle(Item item, int x, int y, int z = 0)
    {
        if (z == 0 && !ClearSettlementSite(x, y)) { item.Delete(); return; }
        item.Movable = false; item.MoveToWorld(new Point3D(X + x, Y + y, z), Map); Fixtures.Add(item);
    }
    private void CropPatch(int x, int y, string crop, string name)
    {
        if (!ClearSettlementSite(x, y)) { return; }
        var spawner = new Spawner(4, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(8), 0,
            new Rectangle3D(X + x, Y + y, 0, 3, 3, 10), crop) { Name = name };
        Place(spawner, x, y);
    }
}
