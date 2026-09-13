using System;
using System.Linq;
using Server.Engines.CannedEvil;
namespace Server.HavenPrototype {
 public static class HavenDespiseChampion {
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(!HavenPreview.Enabled)return;Ensure();Timer.DelayCall(TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(10),()=>Ensure());};}
  public static int Ensure(){
   int count=0;
   foreach(var spawn in World.Items.Values.OfType<ChampionSpawn>().Where(s=>!s.Deleted&&s.Map==Map.Felucca&&s.SpawnName=="Despise").ToArray()){
    spawn.AutoRestart=true;spawn.RestartDelay=TimeSpan.Zero;
    if(!spawn.Active)spawn.Start();count++;
   }
   return count;
  }
 }
}
