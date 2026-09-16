using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class ChampionCorpseSmoke
{
 public static void Run(Action<string> log, Action done)
 {
  var wave=new Ogre{IsChampionSpawn=true};var normal=new Ogre();var boss=new Barracoon{IsChampionSpawn=true};
  var spot=new Point3D(3400,2400,Map.Trammel.GetAverageZ(3400,2400));
  wave.MoveToWorld(spot,Map.Trammel);normal.MoveToWorld(spot,Map.Trammel);boss.MoveToWorld(spot,Map.Trammel);
  wave.Kill();normal.Kill();boss.Kill();
  var wc=wave.Corpse as Corpse;var nc=normal.Corpse as Corpse;var bc=boss.Corpse as Corpse;
  if(wc==null||nc==null||bc==null)throw new Exception("Missing corpse fixture");
  wc.BeginDecay(TimeSpan.FromMinutes(1)); // Controller must not extend our deadline.
  Timer.DelayCall(TimeSpan.FromSeconds(32),()=>{
   try {
    if(!wc.Deleted||nc.Deleted||bc.Deleted)throw new Exception("Cleanup scope/timing failed");
    log("PASS native death event deletes wave corpse in 30 seconds despite later controller timer; normal and champion boss corpses remain");
   } catch(Exception e){log("FAIL "+e);}
   finally {wc.Delete();nc.Delete();bc.Delete();wave.Delete();normal.Delete();boss.Delete();done();}
  });
 }
}
