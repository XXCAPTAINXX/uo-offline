using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class MissionPreferencesSmoke
{
 public static void Run(Action<string> log)
 {
  var p=new PlayerMobile{Player=true,Body=0x190,RawStr=200};p.AddItem(new Backpack());new Account("prefs-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=p;p.MoveToWorld(new Point3D(3400,2400,0),Map.Felucca);var c=HavenCompanion.Claim(p);HavenOfflineMissionPlan.Ensure(c).Enabled=true;
  HavenMissionPreferences.Remember(c,1,4,30);new CompanionActivityGump(c);if(HavenMissionPreferences.Get(c,"Tab",0)!=1||HavenMissionPreferences.Get(c,"Selection",0)!=4||HavenMissionPreferences.Get(c,"Minutes",0)!=30)throw new Exception("Remembered selection reset");new CompanionActivityGump(c,2,0,5);if(HavenMissionPreferences.Get(c,"Tab",0)!=1)throw new Exception("Role menu overwrote mission preferences");
  c.Skills.Mining.Base=50;c.Skills.Mining.SetLockNoRelay(SkillLock.Locked);
  if(!c.StartOfflineMission(5,CompanionMission.Mining))throw new Exception("Locked mining dispatch");Finish(c);if(c.Skills.Mining.Base!=50)throw new Exception("Locked gathering trained");c.Recall(p);
  if(!HavenMissionPreferences.Repeat(c,p)||c.MissionKind!=CompanionMission.Mining)throw new Exception("Repeat lost route");if(HavenMissionPreferences.Repeat(c,p))throw new Exception("Repeat replaced active trip");c.Recall(p);
  c.Skills.AnimalTaming.Base=110;c.Skills.AnimalLore.Base=110;c.Skills.AnimalTaming.SetLockNoRelay(SkillLock.Locked);c.Skills.AnimalLore.SetLockNoRelay(SkillLock.Down);
  if(!c.StartOfflineMission(5,CompanionMission.TamePackHorse))throw new Exception("Taming dispatch");Finish(c);if(c.Skills.AnimalTaming.Base!=110||c.Skills.AnimalLore.Base!=110)throw new Exception("Locked/down taming trained");c.Recall(p);
  var field=typeof(HavenCompanion).GetField("_pendingGold",BindingFlags.Instance|BindingFlags.NonPublic);field.SetValue(c,1234);c.CollectMissionRewards(p);if((int)field.GetValue(c)!=0)throw new Exception("Collect left deliverable gold");int gold=c.Backpack.Items.OfType<Gold>().Sum(x=>x.Amount);c.CollectMissionRewards(p);if(c.Backpack.Items.OfType<Gold>().Sum(x=>x.Amount)!=gold)throw new Exception("Collect duplicated gold");
  c.Delete();p.Delete();log("PASS saved selection/duration, repeat route and active-trip rejection, gathering/taming locks, idempotent collect all");
 }
 static void Finish(HavenCompanion c){var f=BindingFlags.Instance|BindingFlags.NonPublic;typeof(HavenCompanion).GetField("_missionDue",f).SetValue(c,DateTime.UtcNow.AddSeconds(-1));typeof(HavenCompanion).GetMethod("CompleteDueMission",f).Invoke(c,null);}
}
