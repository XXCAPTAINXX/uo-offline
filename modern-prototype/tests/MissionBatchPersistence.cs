using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class MissionBatchPersistence
{
 public static void Run(bool read,Action<string> log)
 {
  if(!read){var p=new PlayerMobile{Player=true,Body=0x190};p.AddItem(new Backpack());new Account("restart-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=p;p.MoveToWorld(new Point3D(3400,2400,0),Map.Felucca);var c=HavenCompanion.Claim(p);var plan=HavenOfflineMissionPlan.Ensure(c);plan.Enabled=true;plan.RouteMask=(1<<(int)CompanionMission.Mining)|(1<<(int)CompanionMission.Leather);plan.Rotate=true;if(!c.StartOfflineMission(30,CompanionMission.Supply))throw new Exception("Dispatch");plan.Enabled=false;typeof(HavenCompanion).GetField("_missionDue",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(c,DateTime.UtcNow.AddDays(1));var pending=new HavenMissionSpoils(c,15){Ready=true,ExtraGold=0};var active=HavenMissionSpoils.For(c).Single(x=>!x.Ready);File.WriteAllLines("mission-batch-identities.txt",new[]{c.Serial.Value.ToString(),active.Serial.Value.ToString(),pending.Serial.Value.ToString(),active.ExtraGold.ToString(),String.Join(",",active.Items.Select(i=>i.Serial.Value)),String.Join(",",pending.Items.Select(i=>i.Serial.Value))});World.Save(false,false);log("PASS test world saved with scheduled and earned parcels plus selected rotation");return;}
  var lines=File.ReadAllLines("mission-batch-identities.txt");var companion=World.FindMobile((Serial)int.Parse(lines[0])) as HavenCompanion;var scheduled=World.FindItem((Serial)int.Parse(lines[1])) as HavenMissionSpoils;var earned=World.FindItem((Serial)int.Parse(lines[2])) as HavenMissionSpoils;
  if(companion==null||!companion.OnMission||scheduled==null||scheduled.Ready||scheduled.Companion!=companion||scheduled.ExtraGold!=int.Parse(lines[3])||earned==null||!earned.Ready||earned.Owner!=companion.BoundOwner)throw new Exception("Fresh-world references or state lost");
  if(String.Join(",",scheduled.Items.Select(i=>i.Serial.Value))!=lines[4]||String.Join(",",earned.Items.Select(i=>i.Serial.Value))!=lines[5])throw new Exception("Rolled item identities changed");var restored=HavenOfflineMissionPlan.Find(companion);if(restored==null||restored.Enabled||!restored.Rotate||!restored.Includes(CompanionMission.Mining)||!restored.Includes(CompanionMission.Leather)||restored.Includes(CompanionMission.Supply))throw new Exception("Rotation settings lost");
  var exact=earned.Items.ToArray();companion.CollectMissionRewards(companion.BoundOwner);if(exact.Any(i=>i.Parent!=companion.Backpack)||scheduled.Ready||scheduled.Deleted)throw new Exception("Post-restart collection invalid");log("PASS fresh process reload retains exact scheduled/earned gear, gold snapshot, owner/companion links and selected rotation; collection succeeds");
 }
}
