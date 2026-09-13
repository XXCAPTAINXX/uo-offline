using System;
using System.Linq;
using Server.Items;
namespace Server.HavenPrototype {
 public class HavenExpeditionMarker:Item {
  private HavenMiniChamp _camp;private int _part;
  private static readonly int[] Art={0xB98,0xBD2,0xE3C,0xA22,0xE77,0xA58,0xA2A,0xA2A,0xDE3};
  public static void Ensure(HavenMiniChamp camp){
   if(camp==null || camp.Deleted)return;
   var existing=World.Items.Values.OfType<HavenExpeditionMarker>().Where(x=>!x.Deleted && x._camp==camp).ToArray();
   if(existing.Length==Art.Length && existing.All(x=>x.Map==camp.Map && x.InRange(camp,10)))return;
   var anchor=existing.FirstOrDefault(x=>x._part==0&&x.Map==camp.Map&&x.InRange(camp,10));Point3D site=anchor==null?Point3D.Zero:anchor.Location;bool found=anchor!=null;
   foreach(var offset in new[]{new Point2D(6,0),new Point2D(-7,0),new Point2D(0,6),new Point2D(0,-7)}){
    if(found)break;int x=camp.X+offset.X,y=camp.Y+offset.Y;var a=new Point3D(x,y,camp.Map.GetAverageZ(x,y));var b=new Point3D(x+1,y+1,camp.Map.GetAverageZ(x+1,y+1));
    if(HavenMiniChamp.SafeSite(camp.Map,a) && HavenMiniChamp.SafeSite(camp.Map,b) && Math.Abs(a.Z-b.Z)<=1){site=a;found=true;break;}
   }
   if(!found)return;
   for(int i=0;i<Art.Length;i++){
    var item=existing.FirstOrDefault(part=>part._part==i) ?? new HavenExpeditionMarker(camp,i);
    var offsets=new[]{new Point2D(0,0),new Point2D(0,0),new Point2D(1,1),new Point2D(1,1),new Point2D(2,1),new Point2D(3,0),new Point2D(1,3),new Point2D(3,3),new Point2D(2,3)};
    int x=site.X+offsets[i].X,y=site.Y+offsets[i].Y,z=camp.Map.GetAverageZ(x,y);
    if(i>=4&&existing.Contains(item)&&item.Map==camp.Map&&item.InRange(camp,10))continue;
    if(i>=4&&!HavenMiniChamp.SafeSite(camp.Map,new Point3D(x,y,z))){if(!existing.Contains(item))item.Delete();continue;}
    if(i==3)z+=TileData.ItemTable[0xE3C].CalcHeight;
    item.MoveToWorld(new Point3D(x,y,z),camp.Map);
   }
  }
  public static bool Nearby(HavenMiniChamp camp,Mobile p){return p!=null&&World.Items.Values.OfType<HavenExpeditionMarker>().Any(x=>!x.Deleted&&x._camp==camp&&x.Map==p.Map&&p.InRange(x,3)&&p.InLOS(x));}
  public static void Remove(HavenMiniChamp camp){foreach(var item in World.Items.Values.OfType<HavenExpeditionMarker>().Where(x=>x._camp==camp).ToArray())item.Delete();}
  private HavenExpeditionMarker(HavenMiniChamp camp,int part):base(Art[part]){_camp=camp;_part=part;Movable=false;Name=part<2?"Corsair expeditions - mini champion":part==2?"expedition supplies":part==3?"expedition lantern":part==4?"fresh water barrel":part==5?"scout bedroll":part==8?"expedition campfire":"camp stool";if(part==1)Hue=0x8A5;if(part==3||part==8)Light=LightType.Circle300;}
  public HavenExpeditionMarker(Serial serial):base(serial){}
  public override void OnDoubleClick(Mobile from){if(_camp!=null && !_camp.Deleted && from.Map==Map && from.InRange(this,3) && from.InLOS(this))_camp.Show(from);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_camp);w.Write(_part);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_camp=r.ReadItem() as HavenMiniChamp;_part=r.ReadInt();if(_camp==null)Timer.DelayCall(TimeSpan.Zero,Delete);}
 }
}


