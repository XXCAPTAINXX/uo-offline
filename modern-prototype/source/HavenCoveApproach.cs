using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Multis;
namespace Server.HavenPrototype {
 public class HavenCoveApproach:Item {
  public readonly List<Item> Fixtures=new List<Item>();
  public HavenCoveApproach():base(1){Movable=false;Visible=false;Name="Blackwake cove approach";}
  public HavenCoveApproach(Serial serial):base(serial){}
  public static HavenCoveApproach BuildTest(HavenCoveEncounter camp) {
   if(!File.Exists("RECOVERED-HOUSE-TEST-ONLY")||camp==null||camp.Deleted)throw new InvalidOperationException("Cove approach requires isolated review");
   if(World.Items.Values.OfType<HavenCoveApproach>().Any(i=>!i.Deleted))throw new InvalidOperationException("Cove approach already exists");
   var start=new Point3D(4210,2928,0);var end=new Point3D(camp.X,camp.Y-1,camp.Z);
   var path=new MovementPath(start,end,camp.Map);if(!path.Success)throw new InvalidOperationException("No walking connection from harbor to cove");
   var approach=new HavenCoveApproach();approach.MoveToWorld(start,camp.Map);
   try {
    var at=start;var points=new List<Point3D>{start};
    foreach(var direction in path.Directions) {
     int x=at.X,y=at.Y,z;if(!Server.Movement.Movement.CheckMovement(at,camp.Map,at,direction,out z))throw new InvalidOperationException("Cove approach step blocked");
     Server.Movement.Movement.Offset(direction,ref x,ref y);at=new Point3D(x,y,z);
     if(BaseHouse.FindHouseAt(at,camp.Map,16)!=null)throw new InvalidOperationException("Cove approach crosses a house");points.Add(at);
    }
    foreach(var point in points) {
     var nearby=camp.Map.GetItemsInRange(point,0);bool paved=false;
     try{foreach(Item item in nearby)if(item.Z==point.Z&&item.ItemData.Height<=1&&item.ItemData.Surface)paved=true;}finally{nearby.Free();}
     if(!paved){var tile=new Static(0x519){Name="Blackwake cove trail",Movable=false};approach.Fixtures.Add(tile);tile.MoveToWorld(point,camp.Map);}
    }
    var boardPoint=new Point3D(camp.X,camp.Y-2,camp.Z);
    if(!camp.Map.CanFit(boardPoint,16,false,false)||BaseHouse.FindHouseAt(boardPoint,camp.Map,16)!=null)throw new InvalidOperationException("Cove expedition board site blocked");
    var board=new HavenCoveBoard(camp);approach.Fixtures.Add(board);board.MoveToWorld(boardPoint,camp.Map);
    return approach;
   }catch{approach.Delete();throw;}
  }
  public override void OnDelete(){foreach(var item in Fixtures)if(!item.Deleted)item.Delete();base.OnDelete();}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Fixtures.Count);foreach(var item in Fixtures)w.Write(item);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();int n=r.ReadInt();for(int i=0;i<n;i++){var item=r.ReadItem();if(item!=null)Fixtures.Add(item);}}
 }
 public class HavenCoveBoard:Item {
  public HavenCoveEncounter Camp;
  public HavenCoveBoard(HavenCoveEncounter camp):base(0x1E5E){Camp=camp;Name="BLACKWAKE COVE - Pirate mini-champion expedition";Movable=false;}
  public HavenCoveBoard(Serial serial):base(serial){}
  public override void OnDoubleClick(Mobile p){if(Camp!=null&&!Camp.Deleted&&p.Alive&&p.Map==Map&&p.InRange(this,3)&&p.InLOS(this))Camp.Show(p);else p.SendMessage("Stand beside the expedition board to read it.");}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Three pirate waves, then Admiral Blackwake");list.Add("Double-click to start or collect expedition rewards");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Camp);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Camp=r.ReadItem() as HavenCoveEncounter;}
 }
}
