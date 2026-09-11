using System;
using System.Linq;
using Server.Items;
namespace Server.HavenPrototype {
 public class HavenExpeditionMarker:Item {
  private HavenMiniChamp _camp;private int _part;
  private static readonly int[] Art={0xB98,0xBD2,0xE3C,0xA22};
  public static void Ensure(HavenMiniChamp camp){
   if(camp==null || camp.Deleted)return;
   var existing=World.Items.Values.OfType<HavenExpeditionMarker>().Where(x=>!x.Deleted && x._camp==camp).ToArray();
   if(existing.Length==4 && existing.All(x=>x.Map==camp.Map && x.InRange(camp,10)))return;
   Point3D site=Point3D.Zero;bool found=false;
   foreach(var offset in new[]{new Point2D(6,0),new Point2D(-7,0),new Point2D(0,6),new Point2D(0,-7)}){
    int x=camp.X+offset.X,y=camp.Y+offset.Y;var a=new Point3D(x,y,camp.Map.GetAverageZ(x,y));var b=new Point3D(x+1,y+1,camp.Map.GetAverageZ(x+1,y+1));
    if(HavenMiniChamp.SafeSite(camp.Map,a) && HavenMiniChamp.SafeSite(camp.Map,b) && Math.Abs(a.Z-b.Z)<=1){site=a;found=true;break;}
   }
   if(!found)return;
   for(int i=0;i<4;i++){
    var item=existing.FirstOrDefault(part=>part._part==i) ?? new HavenExpeditionMarker(camp,i);
    int x=site.X+(i>=2?1:0),y=site.Y+(i>=2?1:0),z=camp.Map.GetAverageZ(x,y);
    if(i==3)z+=TileData.ItemTable[0xE3C].CalcHeight;
    item.MoveToWorld(new Point3D(x,y,z),camp.Map);
   }
  }
  public static void Remove(HavenMiniChamp camp){foreach(var item in World.Items.Values.OfType<HavenExpeditionMarker>().Where(x=>x._camp==camp).ToArray())item.Delete();}
  private HavenExpeditionMarker(HavenMiniChamp camp,int part):base(Art[part]){_camp=camp;_part=part;Movable=false;Name=part<2?"Corsair expeditions - mini champion":part==2?"expedition supplies":"expedition lantern";if(part==1)Hue=0x8A5;if(part==3)Light=LightType.Circle300;}
  public HavenExpeditionMarker(Serial serial):base(serial){}
  public override void OnDoubleClick(Mobile from){if(_camp!=null && !_camp.Deleted && from.Map==Map && from.InRange(this,3) && from.InLOS(this))_camp.Show(from);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_camp);w.Write(_part);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_camp=r.ReadItem() as HavenMiniChamp;_part=r.ReadInt();if(_camp==null)Timer.DelayCall(TimeSpan.Zero,Delete);}
 }
}


