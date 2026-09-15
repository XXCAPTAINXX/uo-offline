using System;
using System.Linq;
using Server.Items;
namespace Server.HavenPrototype {
 public class HavenPublicPracticeChest:HavenHomePracticeChest {
  public HavenPublicPracticeChest(){Name="Community Lockpicking and Remove Trap trainer";}
  public HavenPublicPracticeChest(Serial s):base(s){}
  protected override bool CanPractice(Mobile from){return from!=null&&from.Alive&&from.Map==Map&&from.InRange(this,2)&&from.InLOS(this);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public static class HavenTrainingServices {
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Ensure();};}
  public static void Ensure(){
   foreach(var house in World.Items.Values.OfType<HavenRecoveredHeadquarters>().Where(h=>!h.Deleted).ToArray()){
    if(house.CompanyFixtures.OfType<HavenHomePracticeChest>().Any(c=>!c.Deleted))continue;
    var p=new Point3D(house.X+10,house.Y,house.Z+7);
    if(!house.Map.CanFit(p,16,true,true))continue;
    var chest=new HavenHomePracticeChest();house.CompanyFixtures.Add(chest);chest.MoveToWorld(p,house.Map);
   }
   foreach(var hall in World.Items.Values.OfType<HavenIslandCommons>().Where(h=>!h.Deleted).ToArray()){
    if(hall.Fixtures.OfType<HavenPublicPracticeChest>().Any(c=>!c.Deleted))continue;
    var p=new Point3D(hall.X+10,hall.Y+27,hall.Z);
    if(!hall.Map.CanFit(p,16,true,true))continue;
    var chest=new HavenPublicPracticeChest();hall.Fixtures.Add(chest);chest.MoveToWorld(p,hall.Map);
   }
  }
 }
 public class HavenInfiniteLockpick:Lockpick {
  [Constructable]public HavenInfiniteLockpick(){Name="Unbreakable lockpick";Stackable=false;Weight=0.1;Hue=0x8A5;}
  public HavenInfiniteLockpick(Serial s):base(s){}
  protected override void BrokeLockPickTest(Mobile from){}
  public override void OnDoubleClick(Mobile from){if(from.Alive&&IsChildOf(from.Backpack))base.OnDoubleClick(from);else from.SendMessage("Keep the lockpick in your backpack to use it.");}
  protected override void BeginLockpick(Mobile from,ILockpickable target){if(!Deleted&&from.Alive&&IsChildOf(from.Backpack))base.BeginLockpick(from,target);}
  protected override void EndLockpick(object state){var args=state as object[];var from=args==null?null:args[1] as Mobile;if(from!=null&&!Deleted&&from.Alive&&IsChildOf(from.Backpack))base.EndLockpick(state);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Unlimited uses; normal Lockpicking skill checks and gains");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
}
