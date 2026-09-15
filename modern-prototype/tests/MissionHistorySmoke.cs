using System.Linq;
using System;
using System.IO;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class MissionHistorySmoke
{
 public static void Run(Action<string> log)
 {
  var p=new PlayerMobile{Player=true,Body=0x190,RawStr=200};p.AddItem(new Backpack());new Account("history-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=p;p.MoveToWorld(new Point3D(3400,2400,0),Map.Felucca);
  var c=HavenCompanion.Claim(p);HavenOfflineMissionPlan.Ensure(c).Enabled=true;
  c.EnsureEvolvingEquipment();var gear=c.Items.Select(x=>HavenEquipmentEvolution.Find(x)).First(x=>x!=null);int xpBefore=gear.Experience;c.Skills.Mining.Base=50;
  if(!c.StartOfflineMission(5,CompanionMission.Mining))throw new Exception("Mission dispatch: "+c.MissionStartError(p,5,CompanionMission.Supply,true));
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  typeof(HavenCompanion).GetField("_missionDue",flags).SetValue(c,DateTime.UtcNow.AddSeconds(-1));
  var finish=typeof(HavenCompanion).GetMethod("CompleteDueMission",flags);finish.Invoke(c,null);finish.Invoke(c,null);
  var h=HavenMissionHistory.Find(c);if(h==null||h.Entries.Count!=1||!h.Entries[0].Contains("gold earned"))throw new Exception("Completion history missing or duplicated");
  if(gear.Experience!=xpBefore+50||!h.Entries[0].Contains("Mining +")||!h.Entries[0].Contains("Gear:"))throw new Exception("Mission gear XP or gain report");
  c.Recall(p);if(!c.StartOfflineMission(5,CompanionMission.Supply))throw new Exception("Second dispatch");var due=c.MissionDue;
  if(HavenMissionRecallGump.Confirm(c,p,due.AddSeconds(-1))||!c.OnMission)throw new Exception("Stale confirmation cancelled trip");
  var stranger=new PlayerMobile{Player=true};if(HavenMissionRecallGump.Confirm(c,stranger,due)||!c.OnMission)throw new Exception("Stranger cancelled trip");stranger.Delete();
  if(!HavenMissionRecallGump.Confirm(c,p,due)||c.OnMission||h.Entries.Count!=2||!h.Entries[0].Contains("recalled early"))throw new Exception("Confirmed recall or journal failed");
  if(gear.Experience!=xpBefore+50)throw new Exception("Early recall awarded gear XP");
  for(int i=0;i<25;i++)HavenMissionHistory.Record(c,"entry "+i);
  if(h.Entries.Count!=20||!h.Entries[0].EndsWith("entry 24")||!h.Entries[19].EndsWith("entry 5"))throw new Exception("History retention order");
  using(var stream=new MemoryStream()){var writer=new BinaryFileWriter(stream,true);h.Serialize(writer);writer.Flush();h.Entries.Clear();stream.Position=0;h.Deserialize(new BinaryFileReader(new BinaryReader(stream)));}
  if(h.Companion!=c||h.Entries.Count!=20||!h.Entries[0].EndsWith("entry 24")||h.Parent!=null||h.Map!=Map.Internal)throw new Exception("History serialization");
  h.Delete();c.Delete();p.Delete();log("PASS mission completion and recall history, 20-entry order, serialization, stale/foreign recall protection");
 }
}
