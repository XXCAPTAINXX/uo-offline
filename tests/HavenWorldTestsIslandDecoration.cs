using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsIslandDecoration
{
    public HavenWorldTestsIslandDecoration() { _ = new HavenWorldTestsMarket(); BaseHouse.Configure(); }

    [SkippableFact]
    public void IslandDecorationPreservesPossessionsAndDoesNotDuplicateOnSecondPass()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Body = 400 }; owner.AddItem(new Backpack());
        var estate = new HavenPirateEstate { Owner = owner };
        estate.MoveToWorld(HavenPirateEstate.Site, Map.Trammel);
        var trunk = new Static(0xCCA) { Name = "Corsair's grove" };
        trunk.MoveToWorld(new Point3D(estate.X+108,estate.Y+48,0),estate.Map);estate.Fixtures.Add(trunk);
        var treasure = new WoodenChest();var gold = new Gold(1234);treasure.DropItem(gold);
        var treasureSite = new Point3D(estate.X+80,estate.Y+110,0);treasure.MoveToWorld(treasureSite,estate.Map);
        try
        {
            HavenIslandDecoration.Apply(estate,null);
            Assert.True(HavenIslandDecoration.Complete(estate.Fixtures));
            Assert.True(estate.Fixtures.Count>900,$"Only {estate.Fixtures.Count} fixtures placed");
            Assert.False(treasure.Deleted);Assert.Equal(treasureSite,treasure.Location);Assert.Same(treasure,gold.Parent);Assert.Equal(1234,gold.Amount);
            Assert.DoesNotContain(estate.Fixtures,i=>i.Location==treasureSite);
            Assert.Equal(0xCCC,trunk.ItemID);
            Assert.Single(estate.Fixtures,i=>i.ItemID==0xCCE && i.Location==trunk.Location);
            var count=estate.Fixtures.Count;HavenIslandDecoration.Apply(estate,null);Assert.Equal(count,estate.Fixtures.Count);
            foreach(var p in new[] {new Point3D(80,128,0),new Point3D(78,124,0),new Point3D(87,136,0),new Point3D(70,36,0)})
            { Assert.True(estate.Map.CanFit(new Point3D(estate.X+p.X,estate.Y+p.Y,p.Z),16,checkMobiles:false),$"Blocked key site {p}"); }
        }
        finally { treasure.Delete();estate.Delete();owner.Delete(); }
    }

    [SkippableFact]
    public void FurnishedCompoundKeepsEveryLadderAndStoreReachable()
    {
        TileDataRequirement.SkipIfMissing();
        var owner=new PlayerMobile { Body=400 };owner.AddItem(new Backpack());
        var estate=new HavenPirateEstate { Owner=owner };estate.MoveToWorld(HavenPirateEstate.Site,Map.Trammel);estate.Fixtures.Add(new Item(1));
        var old=new HavenGuildCastle(owner) { Estate=estate };old.MoveToWorld(new Point3D(4196,2868,0),Map.Trammel);old.Furnish();
        HavenPirateHeadquarters house=null;
        try
        {
            house=HavenPirateHeadquarters.Replace(old);
            var design=house.Components.List.ToArray();var stores=house.MasterStorage.FindLinked().ToArray();
            var fixtures=house.CompanyFixtures.ToDictionary(i=>i,i=>i.Location);
            HavenIslandDecoration.Apply(estate,house);
            Assert.True(HavenIslandDecoration.Complete(house.CompanyFixtures));
            Assert.True(house.CompanyFixtures.Count>=fixtures.Count+15,$"Only {house.CompanyFixtures.Count-fixtures.Count} house details added");
            Assert.Equal(design,house.Components.List);
            Assert.All(fixtures,p=>{ Assert.False(p.Key.Deleted);Assert.Equal(p.Value,p.Key.Location); });
            Assert.Equal(stores,house.MasterStorage.FindLinked().ToArray());
            foreach(var site in HavenPirateHeadquarters.DirectLadderSites)
            { Assert.True(house.Map.CanFit(new Point3D(house.X+site.X,house.Y+site.Y+1,site.Z),16,checkMobiles:false),$"Blocked ladder {site}"); }
            foreach(var site in HavenPirateHeadquarters.DirectLadderLandings)
            { Assert.True(house.Map.CanFit(new Point3D(house.X+site.X,house.Y+site.Y,site.Z),16,checkMobiles:false),$"Blocked landing {site}"); }
            foreach(var store in stores.Cast<Container>().Append(house.MasterStorage))
            {
                var accessible=false;
                for(var dx=-1;dx<=1;dx++)
                { for(var dy=-1;dy<=1;dy++)
                  { if(house.Map.CanFit(new Point3D(store.X+dx,store.Y+dy,store.Z),16,checkMobiles:false)) { accessible=true; } } }
                Assert.True(accessible,$"Blocked storage {store.Name}");
            }
            var count=house.CompanyFixtures.Count;HavenIslandDecoration.Apply(estate,house);Assert.Equal(count,house.CompanyFixtures.Count);
            File.WriteAllText("E:/(Offline UO)/uo-offline-haven-rc4/artifacts/island-decoration-staged.json",JsonSerializer.Serialize(HavenIslandDecoration.Snapshot(estate,house)));
        }
        finally
        {
            if(house!=null) { foreach(var item in house.CompanyFixtures.ToArray()) { item.Delete(); }house.Delete(); }
            if(!old.Deleted) { foreach(var item in old.CompanyFixtures.ToArray()) { item.Delete(); }old.Delete(); }
            estate.Delete();owner.Delete();
        }
    }
}
