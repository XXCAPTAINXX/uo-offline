using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Engines.Shadowguard;
using Server.HavenPrototype;
public static class ArmoryHelpSmoke
{
 private class TestJenna:HavenCompanion {public override bool CanAutoStable {get{return false;}}}
 public static void Run(Action<string> log,Action done)
 {
  var controller=ShadowguardController.Instance;var instance=controller.Instances.First(i=>!i.IsRoof&&!i.InUse);
  var room=new ArmoryEncounter(instance){HasBegun=true};room.Setup();controller.AddEncounter(room);
  foreach(var enemy in room.Spawn.ToArray())enemy.Delete();room.Spawn.Clear();
  var owner=new PlayerMobile{Player=true};owner.AddItem(new Backpack());owner.MoveToWorld(instance.Center,Map.TerMur);room.Participants.Add(owner);
  var c=new TestJenna();typeof(HavenCompanion).GetField("_owner",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(c,owner);c.SetControlMaster(owner);c.MoveToWorld(instance.Center,Map.TerMur);
  var drop=new Phylactery();drop.MoveToWorld(new Point3D(instance.Center.X+2,instance.Center.Y,instance.Center.Z),Map.TerMur);
  int initial=room.Armor.Count(a=>!a.Deleted);var start=c.Location;bool picked=false,purified=false,moved=false;var deadline=DateTime.UtcNow.AddSeconds(40);
  if(!c.RequestArmoryHelp(owner)||!c.ArmoryHelpActive)throw new Exception("Did not start");
  int routes=0;foreach(var target in room.Armor.Concat(room.Items.OfType<PurifyingFlames>())){Point3D stand;if(!c.FindArmoryStand(target,3,out stand))throw new Exception("No reachable approach for "+target.GetType().Name+" at "+target.Location);if(!Map.TerMur.CanFit(stand.X,stand.Y,stand.Z,16,false,false,true)||!Map.TerMur.LineOfSight(new Point3D(stand.X,stand.Y,stand.Z+14),Map.TerMur.GetPoint(target,false)))throw new Exception("Invalid stand");routes++;}log("PASS reachable floor approaches with line of sight for all "+routes+" Armory statues and flames");
  Timer timer=null;timer=Timer.DelayCall(TimeSpan.FromMilliseconds(100),TimeSpan.FromMilliseconds(100),()=>{
   try{
    c.ThinkArmoryHelp();picked|=drop.IsChildOf(c.Backpack);purified|=drop.Purified;moved|=c.Location!=start;
    if(drop.Deleted){
     if(!picked||!purified||!moved||room.Armor.Count(a=>!a.Deleted)!=initial-1)throw new Exception("Incomplete pickup/purify/statue sequence");
     log("PASS Jenna physically walks to a ground phylactery, picks it up, purifies it using native targeting, and consumes it on one native cursed statue");
     owner.MoveToWorld(c.Location,c.Map);if(!c.SetOrder(owner,OrderType.Follow)||c.ArmoryHelpActive)throw new Exception("Follow did not cancel");
     c.RequestArmoryHelp(owner);c.RequestArmoryHelp(owner);if(c.ArmoryHelpActive)throw new Exception("Puzzle toggle did not cancel");
     c.RequestArmoryHelp(owner);room.Completed=true;c.ThinkArmoryHelp();if(c.ArmoryHelpActive)throw new Exception("Completion did not stop");
     log("PASS Follow, Puzzle toggle, and completed room stop assistance");timer.Stop();done();return;
    }
    if(DateTime.UtcNow>deadline)throw new Exception("Timed out; picked="+picked+" purified="+purified+" moved="+moved+" position="+c.Location+" active="+c.ArmoryHelpActive);
   }catch(Exception ex){log("FAIL "+ex);timer.Stop();done();}
  });
 }
}
