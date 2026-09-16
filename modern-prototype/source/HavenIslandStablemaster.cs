using System;
using System.Linq;
using Server.Mobiles;
namespace Server.HavenPrototype {
 public class HavenIslandStablemaster:AnimalTrainer {
  public HavenRecoveredHeadquarters Estate;
  public override bool NoHouseRestrictions { get { return true; } }
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Ensure();};}
  public static void Ensure(){
   foreach(var house in World.Items.Values.OfType<HavenRecoveredHeadquarters>().Where(h=>!h.Deleted&&h.Map!=null&&h.Map!=Map.Internal).ToArray()){
    var keeper=World.Mobiles.Values.OfType<HavenIslandStablemaster>().FirstOrDefault(m=>!m.Deleted&&m.Estate==house);
    if(keeper!=null)continue;
    var site=new Point3D(house.X-8,house.Y+8,house.Z+7);
    if(!house.IsInside(site,16)||!house.Map.CanFit(site,16,true,true)){Console.WriteLine("Island stablemaster: stable position occupied; installation deferred");continue;}
    keeper=new HavenIslandStablemaster(house);keeper.Home=site;keeper.MoveToWorld(site,house.Map);
    Console.WriteLine("Island stablemaster ready: "+keeper.Serial+" at "+site);
   }
  }
  public HavenIslandStablemaster(HavenRecoveredHeadquarters house){Estate=house;Name="Mara";Title="the island stablemaster";Female=true;Body=0x191;Blessed=true;CantWalk=true;RangeHome=0;}
  public HavenIslandStablemaster(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Estate);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Estate=r.ReadItem() as HavenRecoveredHeadquarters;Blessed=true;CantWalk=true;RangeHome=0;}
 }
}

