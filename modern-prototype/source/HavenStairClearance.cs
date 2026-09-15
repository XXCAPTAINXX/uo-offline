using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace Server.HavenPrototype {
 public static class HavenStairClearance {
  public static void Initialize(){EventSink.ServerStarted+=()=>{
   if(!HavenPreview.Enabled||!File.Exists("HAVEN-STAIR-CLEARANCE"))return;
   foreach(var house in World.Items.Values.OfType<HavenRecoveredHeadquarters>().Where(h=>!h.Deleted).ToArray())Apply(house);
   World.Save(false,false);File.Delete("HAVEN-STAIR-CLEARANCE");Console.WriteLine("Stair clearance migration saved.");
  };}
  public static int Apply(HavenRecoveredHeadquarters house){
   string[] names={"Receiving chest","Export cargo awaiting shipment","Coiled dock rope"};
   var oldSites=new[]{new Point3D(1,6,7),new Point3D(2,5,7),new Point3D(3,5,7)};
   var moves=new Dictionary<Item,Point3D>();var originals=new Dictionary<Item,Point3D>();
   for(int i=0;i<names.Length;i++){
    var old=new Point3D(house.X+oldSites[i].X,house.Y+oldSites[i].Y,house.Z+oldSites[i].Z);
    var item=house.CompanyFixtures.FirstOrDefault(x=>!x.Deleted&&x.Name==names[i]&&x.Location==old);
    if(item==null)continue;
    var to=new Point3D(house.X+4,house.Y+8+i,house.Z+7);
    if(!house.IsInside(to,16)||!house.Map.CanFit(to,16,false,false))throw new InvalidOperationException("New storage site blocked: "+to);
    var nearby=house.Map.GetItemsInRange(to,0);
    try{foreach(Item other in nearby)if(other!=house&&other.Visible&&Math.Abs(other.Z-to.Z)<16)throw new InvalidOperationException("Storage destination occupied: "+to);}finally{nearby.Free();}
    moves[item]=to;originals[item]=item.Location;
   }
   try{
    foreach(var move in moves)move.Key.MoveToWorld(move.Value,house.Map);
    Check(house);house.CheckWalkingRoutes(true);
   }catch{foreach(var original in originals)original.Key.MoveToWorld(original.Value,house.Map);throw;}
   foreach(var move in moves)Console.WriteLine("Stair clearance: "+move.Key.Serial+" "+move.Key.Name+" -> "+move.Value);
   return moves.Count;
  }
  public static void Check(HavenRecoveredHeadquarters house){
   var probe=new Mobile{Body=0x190};
   try{
    for(int lane=1;lane<=2;lane++){
     var at=new Point3D(house.X+lane,house.Y+9,house.Z+7);probe.MoveToWorld(at,house.Map);
     for(int y=8;y>=3;y--){int z;if(!Server.Movement.Movement.CheckMovement(probe,house.Map,at,Direction.North,out z)||z!=house.Z+7)throw new InvalidOperationException("Stair-side lane blocked at "+at);at=new Point3D(at.X,house.Y+y,z);}
    }
    var stair=new Point3D(house.X-1,house.Y+7,house.Z+7);probe.MoveToWorld(stair,house.Map);
    for(int y=6;y>=3;y--){int z;if(!Server.Movement.Movement.CheckMovement(probe,house.Map,stair,Direction.North,out z))throw new InvalidOperationException("Stair ascent blocked at "+stair);stair=new Point3D(stair.X,house.Y+y,z);}
    if(stair.Z<=house.Z+7)throw new InvalidOperationException("Stair did not rise");
   }finally{probe.Delete();}
  }
 }
}
