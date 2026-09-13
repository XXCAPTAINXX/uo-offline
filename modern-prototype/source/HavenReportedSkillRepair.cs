using System;
using System.IO;
using System.Linq;
namespace Server.HavenPrototype {
 public static class HavenReportedSkillRepair {
  public static void Initialize(){EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(3),Run);}
  static void Run(){
   if(!HavenPreview.Enabled||!File.Exists("REPAIR-REPORTED-ALDEN-TAMING"))return;
   var candidates=World.Mobiles.Values.OfType<HavenCompanion>().Where(c=>!c.Deleted&&c.Name=="Alden Ashford"&&c.BoundOwner!=null&&c.BoundOwner.Name=="Rictor Quake").ToArray();
   if(candidates.Length!=1){File.AppendAllText("reported-skill-repair.log","Repair deferred: expected one owned Alden, found "+candidates.Length+"\n");return;}
   var skill=candidates[0].Skills.AnimalTaming;int before=skill.BaseFixedPoint;skill.BaseFixedPoint=Math.Max(before,530);
   File.AppendAllText("reported-skill-repair.log",DateTime.UtcNow.ToString("O")+" "+candidates[0].Serial+" Taming "+before+" -> "+skill.BaseFixedPoint+"\n");
   File.Delete("REPAIR-REPORTED-ALDEN-TAMING");
  }
 }
}
