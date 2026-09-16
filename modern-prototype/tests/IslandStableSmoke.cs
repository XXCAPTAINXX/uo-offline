using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class IslandStableSmoke {
 public static void Run(Action<string> log){
  HavenIslandStablemaster.Ensure();HavenIslandStablemaster.Ensure();
  var house=World.Items.Values.OfType<HavenRecoveredHeadquarters>().Single(h=>!h.Deleted);
  var keepers=World.Mobiles.Values.OfType<HavenIslandStablemaster>().Where(m=>!m.Deleted&&m.Estate==house).ToArray();
  if(keepers.Length!=1)throw new Exception("Duplicate or missing stablemaster");var keeper=keepers[0];
  if(!house.IsInside(keeper.Location,16)||keeper.Z!=house.Z+7)throw new Exception("Wrong stable floor: keeper="+keeper.Location+" house="+house.Location+" inside="+house.IsInside(keeper.Location,16));
  var owner=(PlayerMobile)house.Owner;var oldLocation=owner.Location;var oldMap=owner.Map;int oldMax=owner.FollowersMax;owner.FollowersMax=oldMax+5;owner.Backpack.DropItem(new Gold(100));owner.MoveToWorld(new Point3D(keeper.X+1,keeper.Y,keeper.Z),keeper.Map);
  var pet=new Horse();pet.MoveToWorld(owner.Location,owner.Map);pet.SetControlMaster(owner);
  try{
   keeper.EndStable(owner,pet);if(!pet.IsStabled||!owner.Stabled.Contains(pet))throw new Exception("Native stable failed");
   keeper.EndClaimList(owner,pet);if(pet.IsStabled||pet.ControlMaster!=owner||pet.Map!=owner.Map)throw new Exception("Native retrieval failed");
   log("PASS one stablemaster inside ground-floor stable; native pet stabling and retrieval; repeated Ensure has no duplicates");
  }finally{pet.Delete();owner.FollowersMax=oldMax;owner.MoveToWorld(oldLocation,oldMap);}
 }
}


