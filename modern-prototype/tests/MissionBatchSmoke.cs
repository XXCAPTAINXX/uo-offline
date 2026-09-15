using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class MissionBatchSmoke
{
 static BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Finish(HavenCompanion c){typeof(HavenCompanion).GetField("_missionDue",Flags).SetValue(c,DateTime.UtcNow.AddSeconds(-1));typeof(HavenCompanion).GetMethod("CompleteDueMission",Flags).Invoke(c,null);}
 static void Roundtrip(Item item){using(var stream=new MemoryStream()){var w=new BinaryFileWriter(stream,true);item.Serialize(w);w.Flush();stream.Position=0;item.Deserialize(new BinaryFileReader(new BinaryReader(stream)));}}
 public static void Run(Action<string> log)
 {
  var p=new PlayerMobile{Player=true,Body=0x190,RawStr=200};p.AddItem(new Backpack());new Account("batch-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=p;p.MoveToWorld(new Point3D(3400,2400,0),Map.Felucca);var c=HavenCompanion.Claim(p);var plan=HavenOfflineMissionPlan.Ensure(c);plan.Enabled=true;
  int[] minutes={5,15,30,60},counts={1,3,6,15},xp={50,165,345,750};for(int i=0;i<4;i++)if(HavenMissionSpoils.GearCount(minutes[i])!=counts[i]||HavenMissionSpoils.Experience(minutes[i])!=xp[i])throw new Exception("Duration bonus table");
  if(!c.StartOfflineMission(60,CompanionMission.Supply))throw new Exception("Supply dispatch");var parcel=HavenMissionSpoils.For(c).Single();var exact=parcel.Items.ToArray();int gold=parcel.ExtraGold+6000;if(exact.Length!=15||gold<15000||gold>22500)throw new Exception("Supply quantities");Roundtrip(parcel);if(parcel.Ready||parcel.ExtraGold+6000!=gold||!parcel.Items.SequenceEqual(exact))throw new Exception("Snapshot persistence");
  c.Backpack.MaxItems=1;Finish(c);if(!parcel.Ready||HavenMissionSpoils.Pending(c)!=15||exact.Any(x=>x.Deleted||x.Parent!=parcel))throw new Exception("Overflow lost snapshot");int again; if(HavenMissionSpoils.Complete(c,out again)!=0||again!=0)throw new Exception("Repeat completion duplicated");Roundtrip(parcel);if(!parcel.Ready||parcel.Owner!=p||!parcel.Items.SequenceEqual(exact))throw new Exception("Pending persistence");
  c.Backpack.MaxItems=0;c.CollectMissionRewards(p);if(HavenMissionSpoils.Pending(c)!=0||exact.Any(x=>x.Parent!=c.Backpack))throw new Exception("Collection changed rolled items");c.CollectMissionRewards(p);if(exact.Any(x=>x.Parent!=c.Backpack))throw new Exception("Repeated collection");c.Recall(p);
  if(!c.StartOfflineMission(5,CompanionMission.Supply))throw new Exception("Cancel dispatch");var cancelled=HavenMissionSpoils.For(c).Single().Items.ToArray();c.Recall(p);if(cancelled.Any(x=>!x.Deleted)||exact.Any(x=>x.Deleted))throw new Exception("Cancel removed earned gear or retained unearned gear");
  c.Skills.Wrestling.Base=50;c.Skills.Tactics.Base=50;c.Skills.AnimalLore.Base=50;
  if(!c.StartOfflineMission(15,CompanionMission.Leather))throw new Exception("Leather dispatch");Finish(c);if(c.Skills.Wrestling.BaseFixedPoint!=533||c.Skills.Tactics.BaseFixedPoint!=533||c.Skills.AnimalLore.BaseFixedPoint!=533)throw new Exception("Leather skills or bonus");c.Recall(p);
  plan.RouteMask=1<<(int)CompanionMission.Mining;plan.Rotate=true;plan.Cursor=0;Roundtrip(plan);if(!plan.Includes(CompanionMission.Mining)||plan.Includes(CompanionMission.Supply)||!plan.IsVirtualItem)throw new Exception("Rotation persistence");plan.Tick();if(!c.OnMission||c.MissionKind!=CompanionMission.Mining)throw new Exception("Rotation ran unselected mission");c.Recall(p);plan.RouteMask=0;plan.Tick();if(c.OnMission)throw new Exception("Empty selection dispatched");
  var protectedParcel=new HavenMissionSpoils(c,5);protectedParcel.Ready=true;var protectedItem=protectedParcel.Items.Single();c.Delete();if(protectedItem.Deleted||!protectedItem.IsChildOf(p.BankBox))throw new Exception("Companion removal lost earned gear");p.Delete();
  log("PASS supply quantities/serials/snapshot and pending serialization, overflow, repeat/cancel, leather training bonuses, selected rotation and recovery");
 }
}
