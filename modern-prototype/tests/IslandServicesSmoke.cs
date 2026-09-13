using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class IslandServicesSmoke {
 class PickProbe:HavenInfiniteLockpick {public void Fail(Mobile p){BrokeLockPickTest(p);}public void Finish(Mobile p,ILockpickable target){EndLockpick(new object[]{target,p});}}
 public static void Run(Action<string> log){
  HavenTrainingServices.Ensure();HavenTrainingServices.Ensure();
  var house=World.Items.Values.OfType<HavenRecoveredHeadquarters>().Single(h=>!h.Deleted);
  var hall=World.Items.Values.OfType<HavenIslandCommons>().Single(h=>!h.Deleted);
  if(house.CompanyFixtures.OfType<HavenHomePracticeChest>().Count()!=1||hall.Fixtures.OfType<HavenPublicPracticeChest>().Count()!=1)throw new Exception("Trainer placement or duplicates");
  house.CheckWalkingRoutes(true);
  var p=new PlayerMobile();p.AddItem(new Backpack());var pick=new PickProbe();p.Backpack.DropItem(pick);
  var chest=new HavenPublicPracticeChest();chest.MoveToWorld(new Point3D(3500,2570,14),Map.Trammel);p.MoveToWorld(new Point3D(3500,2571,14),Map.Trammel);
  try{
   for(int i=0;i<1000;i++)pick.Fail(p);
   if(pick.Deleted||pick.Amount!=1||pick.SkillBonus!=0||pick.IsSkeletonKey)throw new Exception("Infinite pick changed native skill behavior");
   p.Skills.Lockpicking.Base=20;chest.OnDoubleClick(p);if(chest.LockLevel==0)throw new Exception("Unpickable trainer at skill20");
   p.Skills.Lockpicking.Base=0;chest.RequiredSkill=100;pick.Finish(p,chest);if(!chest.Locked)throw new Exception("Skill requirement bypass");
   var hp=p.Hits;chest.OnRemoveTrap(p);if(p.Hits!=hp)throw new Exception("Trainer damaged player");
   log("PASS house and community trainers, no duplicate, routes; 1000 nonbreaking attempts, native skill requirement and harmless trap");
  }finally{pick.Delete();chest.Delete();p.Delete();}
 }
}
