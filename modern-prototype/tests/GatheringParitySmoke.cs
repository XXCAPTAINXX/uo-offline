using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.HavenPrototype;
public static class GatheringParitySmoke {
 public static void Initialize(){if(File.Exists("GATHERING-PARITY-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(2),Run);}
 static void Check(bool value,string label){if(!value)throw new Exception(label);File.AppendAllText("gathering-parity-checks.log","PASS "+label+"\n");}
 static void Run(){HavenCompanion c=null;try{
 c=new HavenCompanion();
 foreach(int minutes in new[]{5,15,30,60})foreach(var kind in new[]{CompanionMission.Mining,CompanionMission.Lumber,CompanionMission.Leather}){
 foreach(var skill in c.Skills)skill.Base=0;
 var rewards=new Dictionary<int,int>();HavenGatheringMissions.Prepare(c,kind,minutes,rewards,0);
 int expected=(kind==CompanionMission.Lumber?40:20)*(minutes==5?5:minutes==15?165:minutes==30?345:750)/(minutes==5?1:10);
 Check(rewards.Count==1&&rewards.Values.Sum()==expected,"base output "+kind+" "+minutes);
 }
 c.Skills.Mining.Base=100;c.Skills.Lumberjacking.Base=100;c.Skills.Wrestling.Base=100;c.Skills.Tactics.Base=100;
 foreach(var kind in new[]{CompanionMission.Mining,CompanionMission.Lumber,CompanionMission.Leather})foreach(double roll in new[]{0.0,0.2,0.4,0.59999,0.6,0.999999}){
 var rewards=new Dictionary<int,int>();HavenGatheringMissions.Prepare(c,kind,60,rewards,roll);
 Check(rewards.Count==2&&rewards.Values.Sum()==(kind==CompanionMission.Lumber?3000:1500),"mixed output "+kind+" roll "+roll);
 foreach(var pair in rewards){var item=HavenResources.Create(pair.Key,pair.Value);Check(item!=null&&item.Stackable,"native resource "+pair.Key);item.Delete();}
 }
 c.Skills.Tactics.Base=0;var leather=new Dictionary<int,int>();HavenGatheringMissions.Prepare(c,CompanionMission.Leather,5,leather,0);Check(leather.Count==1&&leather.ContainsKey(16),"leather requires both combat skills");
 File.AppendAllText("gathering-parity-checks.log","COMPLETE\n");
 }catch(Exception e){File.AppendAllText("gathering-parity-checks.log","FAIL "+e+"\n");}finally{if(c!=null)c.Delete();Core.Kill(false);}}
}
