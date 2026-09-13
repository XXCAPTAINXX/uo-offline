using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Server.Items;
using Server.Multis;
namespace Server.HavenPrototype {
 public static class HavenCoveDesignReview {
  public static void Apply(){
   if(!File.Exists("HAVEN-DESIGN-REVIEW"))throw new InvalidOperationException("Review marker required");
   var camp=World.Items.Values.OfType<HavenCoveEncounter>().Single(c=>!c.Deleted);
   if(camp.X!=4214||camp.Y!=2922||camp.Map!=Map.Trammel)throw new InvalidOperationException("Unexpected cove position");
   var foundation=World.Items.Values.OfType<HavenIslandFoundation>().Single(f=>!f.Deleted);
   var approach=World.Items.Values.OfType<HavenCoveApproach>().Single(f=>!f.Deleted);
   var board=approach.Fixtures.OfType<HavenCoveBoard>().Single(b=>!b.Deleted);
   if(approach.Fixtures.Any(i=>!i.Deleted&&i.Name=="Blackwake cove design complete"))return;
   var remove=foundation.Fixtures.Where(i=>i is Static&&!i.Deleted&&i.Name=="R.E.C. connected paving"&&i.ItemID>=0x519&&i.ItemID<=0x51C&&i.Z==0&&i.X>=4208&&i.X<=4220&&i.Y>=2930&&i.Y<=2934&&!Route(i.X,i.Y)).ToArray();
   var targets=new[]{new Point3D(4211,2930,0),new Point3D(4213,2930,0),new Point3D(4217,2930,0)};
   foreach(var p in targets.Concat(new[]{new Point3D(4212,2930,0),new Point3D(4218,2930,0)})){
    if(BaseHouse.FindHouseAt(p,camp.Map,16)!=null||!camp.Map.CanFit(p,16,true,true))throw new InvalidOperationException("Occupied cove entrance "+p);
    var items=camp.Map.GetItemsInRange(p,0);try{foreach(Item item in items)if(item.Visible&&!foundation.Fixtures.Contains(item)&&!approach.Fixtures.Contains(item))throw new InvalidOperationException("Unmanaged property at cove entrance "+p);}finally{items.Free();}
   }
   var oldBoard=board.Location;var added=new List<Item>();var oldPaving=remove.ToDictionary(i=>i,i=>i.Location);
   try{
   foreach(var item in remove)item.Internalize();
   board.MoveToWorld(targets[0],camp.Map);
   for(int n=1;n<3;n++){var light=new Static(0xB20){Name="Blackwake cove entrance lamp",Movable=false,Light=LightType.Circle225};added.Add(light);light.MoveToWorld(targets[n],camp.Map);}
   foreach(int x in new[]{4212,4218}){
    var flag=new Static(0x15AE){Name="Blackwake expedition banner",Hue=0x53D,Movable=false};added.Add(flag);flag.MoveToWorld(new Point3D(x,2930,5),camp.Map);
   }
   var from=new Point3D(4215,2935,0);var to=new Point3D(4214,2929,0);
   if(!new MovementPath(from,to,camp.Map).Success||!new MovementPath(new Point3D(4210,2928,0),to,camp.Map).Success)throw new InvalidOperationException("Cove approach disconnected");
   var marker=new Static(1){Name="Blackwake cove design complete",Visible=false,Movable=false};added.Add(marker);marker.MoveToWorld(approach.Location,camp.Map);
   approach.Fixtures.AddRange(added);
   foreach(var item in remove){foundation.Fixtures.Remove(item);item.Delete();}
   Console.WriteLine("Cove review: removed excess paving="+remove.Length);
   }catch{foreach(var item in added)item.Delete();board.MoveToWorld(oldBoard,camp.Map);foreach(var item in remove)if(!item.Deleted)item.MoveToWorld(oldPaving[item],camp.Map);throw;}
  }
  static bool Route(int x,int y){return (x>=4214&&x<=4216&&y>=2930&&y<=2935)||(x>=4209&&x<=4211&&y>=2928&&y<=2933)||(x>=4209&&x<=4216&&y>=2931&&y<=2933);}
 }
}
