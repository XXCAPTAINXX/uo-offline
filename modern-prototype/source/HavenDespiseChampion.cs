using System;
using System.Linq;
using Server.Engines.CannedEvil;
using Server.Mobiles;
namespace Server.HavenPrototype {
 public static class HavenDespiseChampion {
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(!HavenPreview.Enabled)return;Ensure();Timer.DelayCall(TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(10),()=>Ensure());};}
  public static int Ensure(){
   int count=0;
   foreach(var spawn in World.Items.Values.OfType<ChampionSpawn>().Where(s=>!s.Deleted&&s.Map==Map.Felucca&&s.SpawnName=="Despise").ToArray()){
    spawn.AutoRestart=true;spawn.RestartDelay=TimeSpan.Zero;
    // Keep the encounter on the central Despise platform.
    spawn.SpawnRadius=10;spawn.ConfinedRoaming=true;
    var bounds=new Rectangle2D(spawn.X-10,spawn.Y-10,20,20);
    if(!spawn.SpawnArea.Equals(bounds))spawn.SpawnArea=bounds;
    foreach(var mob in spawn.Creatures.OfType<BaseCreature>().Where(m=>!m.Deleted&&!m.Controlled&&!m.Summoned).ToArray()){
     mob.Home=spawn.Location;mob.RangeHome=10;
     if(mob.Map!=spawn.Map||!bounds.Contains(mob.Location))mob.MoveToWorld(spawn.GetSpawnLocation(bounds,10),spawn.Map);
    }
    if(!spawn.Active)spawn.Start();count++;
   }
   return count;
  }
 }
}

