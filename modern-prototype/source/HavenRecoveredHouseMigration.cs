using System;
using System.Linq;
using Server;
using Server.Multis;
namespace Server.HavenPrototype {
 public static class HavenRecoveredHouseMigration {
  // Test-only until preservation, full-save reload and visual review are complete.
  public static HavenRecoveredHeadquarters Rehearse(HavenIslandEstate old, Action beforeCommit=null) {
   if(!System.IO.File.Exists("RECOVERED-HOUSE-TEST-ONLY"))throw new InvalidOperationException("House migration is still under verification");
   if(old==null||old.Deleted||old.Owner==null||old.Owner.Deleted)throw new InvalidOperationException("Missing estate owner");
   var foundation=MultiData.GetComponents(0x18A8);
   if(foundation.Width!=31||foundation.Height!=31)throw new InvalidOperationException("Recovered large-plot data is not installed");
   if(old.LockDowns.Count!=0||old.Secures.Count!=0||old.Addons.Count!=0||old.PlayerVendors.Count!=0||old.InternalizedVendors.Count!=0)
    throw new InvalidOperationException("Existing placed possessions require a preservation plan before migration");
   var nearby=old.Map.GetItemsInRange(old.Location,24);
   try{foreach(Item item in nearby)if(item!=old&&item!=old.Sign&&!(item is Server.Items.BaseDoor)&&!old.Stations.Contains(item as HavenEstateStorage)&&old.IsInside(item.Location,Math.Max(1,item.ItemData.Height)))throw new InvalidOperationException("Placed item requires preservation before migration: "+item.Serial);}
   finally{nearby.Free();}
   var occupants=old.Map.GetMobilesInRange(old.Location,24);
   try{foreach(Mobile m in occupants)if(old.IsInside(m.Location,16))throw new InvalidOperationException("Move occupants outside the estate before migration");}finally{occupants.Free();}
   var location=old.Location;var map=old.Map;
   var stations=old.Stations.ToDictionary(s=>s,s=>s.Location);
   HavenRecoveredHeadquarters house=null;
   try {
    old.Internalize();foreach(var station in stations.Keys)station.Internalize();
    house=new HavenRecoveredHeadquarters(old.Owner);house.MoveToWorld(location,map);
    house.Public=old.Public;house.Friends.AddRange(old.Friends);house.CoOwners.AddRange(old.CoOwners);house.Access.AddRange(old.Access);house.Bans.AddRange(old.Bans);
    house.FurnishRecoveredRooms();house.CheckFloorAccess();
    // Transfer the actual vault only after construction passes. Never clone its contents.
    house.InstallStores(old.Vault);old.Vault=null;house.CheckWalkingRoutes();
    if(beforeCommit!=null)beforeCommit();
    old.Delete();return house;
   }catch{
    if(house!=null){if(house.Vault!=null){old.Vault=house.Vault;house.Vault=null;}house.DiscardUnpublishedFixtures();house.Delete();}
    if(!old.Deleted){old.MoveToWorld(location,map);foreach(var pair in stations)pair.Key.MoveToWorld(pair.Value,map);}
    throw;
   }
  }
 }
}
