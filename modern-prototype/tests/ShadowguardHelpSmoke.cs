using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.HavenPrototype;
using Server.Engines.Shadowguard;
public static class ShadowguardHelpSmoke
{
 public static void Run(Action<string> log)
 {
  var controller=ShadowguardController.Instance;
  if(controller==null)throw new Exception("No Shadowguard controller");
  var instance=controller.Instances.First(i=>!i.IsRoof&&!i.InUse);
  var owner=new PlayerMobile{Player=true};owner.AddItem(new Backpack());
  var companion=new HavenCompanion();typeof(HavenCompanion).GetField("_owner",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(companion,owner);companion.SetControlMaster(owner);
  for(int trial=0;trial<8;trial++)
  {
   var room=new FountainEncounter(instance){HasBegun=true};room.Participants.Add(owner);room.Setup();
   foreach(var spigot in room.ShadowguardCanals.OfType<ShadowguardSpigot>().ToArray())
   {
    owner.MoveToWorld(spigot.Location,Map.TerMur);companion.MoveToWorld(spigot.Location,Map.TerMur);
    var route=HavenFountainHelp.FindRoute(room,spigot);if(route==null)throw new Exception("No route trial "+trial+" at "+spigot.Location);
    int initial=room.ShadowguardCanals.Count;
    HavenFountainHelp.TryHelp(owner,companion);
    if(room.ShadowguardCanals.Count!=initial||room.FlowCheckers.Any(c=>c.Complete)&&spigot.ItemID!=39922&&spigot.ItemID!=39909)throw new Exception("Advanced without materials");
    for(int n=0;n<route.Count-1;n++){var piece=new ShadowguardCanal();room.AddShadowguardCanal(piece);(n%2==0?owner.Backpack:companion.Backpack).DropItem(piece);}
    HavenFountainHelp.TryHelp(owner,companion);
    if(spigot.ItemID!=17294&&spigot.ItemID!=17278)throw new Exception("Native flow did not complete trial "+trial+" at "+spigot.Location+" needed "+(route.Count-1));
   }
   if(room.FlowCheckers.Count(c=>c.Complete)!=4)throw new Exception("Not four connections");
   room.ClearItems();instance.Encounter=null;
  }
  log("PASS 8 randomized native Fountain layouts: all 32 spigots connected using earned pieces from both backpacks; insufficient supplies do not advance");
  var orchard=new OrchardEncounter(instance);orchard.Trees=new System.Collections.Generic.List<ShadowguardCypress>();
  foreach(Server.Engines.Shadowguard.VirtueType virtue in Enum.GetValues(typeof(Server.Engines.Shadowguard.VirtueType))){var tree=new ShadowguardCypress(orchard,virtue);if(!tree.IsOppositeVirtue(HavenOrchardHelp.Opposite(virtue)))throw new Exception("Pair mismatch");var apple=new ShadowguardApple(orchard,tree);if(apple.Lifespan!=120)throw new Exception("Apple lifetime");apple.Delete();tree.Delete();}
  log("PASS all 16 Orchard hints agree with native opposite-virtue matching; apples last 120 seconds");
  instance.Encounter=null;companion.Delete();owner.Delete();
 }
 public static void Orchard(Action<string> log, Action done)
 {
  var instance=ShadowguardController.Instance.Instances.First(i=>!i.IsRoof&&!i.InUse);
  var owner=new PlayerMobile{Player=true};owner.AddItem(new Backpack());
  var companion=new HavenCompanion();typeof(HavenCompanion).GetField("_owner",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(companion,owner);companion.SetControlMaster(owner);
  var orchard=new OrchardEncounter(instance){HasBegun=true};orchard.Participants.Add(owner);orchard.Setup();
  var source=orchard.Trees.First();var target=orchard.Trees.First(t=>t.IsOppositeVirtue(source.VirtueType));
  owner.MoveToWorld(source.Location,Map.TerMur);companion.MoveToWorld(source.Location,Map.TerMur);HavenOrchardHelp.Help(owner,companion);
  if(orchard.Apple==null)throw new Exception("Helper did not pick apple");
  owner.MoveToWorld(target.Location,Map.TerMur);companion.MoveToWorld(target.Location,Map.TerMur);HavenOrchardHelp.Help(owner,companion);
  Timer.DelayCall(TimeSpan.FromSeconds(1),()=>{try{if(!source.Deleted||!target.Deleted)throw new Exception("Helper did not finish native apple pair");log("PASS Jenna picks and throws an Orchard apple through native targeting; both matching trees are removed");}catch(Exception e){log("FAIL "+e);}done();});
 }
}
