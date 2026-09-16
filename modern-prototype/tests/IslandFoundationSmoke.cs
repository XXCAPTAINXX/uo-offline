using System;
using System.IO;
using System.Linq;
using Server;
using Server.HavenPrototype;
using Server.Multis;
using Server.Mobiles;
using System.Collections;
public static class IslandFoundationSmoke
{
 public static void Initialize(){if(File.Exists("ISLAND-TEST-ONLY")&&!File.Exists("ISLAND-PERSIST"))EventSink.ServerStarted+=()=>{
  try{var island=HavenIslandFoundation.BuildTest();File.AppendAllText("island-foundation.log","Fixtures="+island.Fixtures.Count+"\n");var routes=island.CheckRoutes();foreach(var route in routes)File.AppendAllText("island-foundation.log",route+"\n");if(routes.Any(r=>r.StartsWith("FAIL")))throw new Exception("Disconnected settlement routes");var commons=HavenIslandCommons.BuildTest();foreach(var route in commons.CheckRoutes()){File.AppendAllText("island-foundation.log",route+"\n");if(route.StartsWith("FAIL"))throw new Exception(route);}File.AppendAllText("island-foundation.log","ROUTES COMPLETE\n");foreach(var site in HavenIslandEncounters.BuildTest()){if(site.Raiders.Count!=3)throw new Exception("Patrol count");var before=site.Raiders.ToArray();site.Tick();if(!site.Raiders.SequenceEqual(before))throw new Exception("Duplicate patrol");foreach(var mob in before)mob.Kill();site.Tick();if(site.Raiders.Count!=0||site.NextWave<=DateTime.UtcNow)throw new Exception("Missing patrol cooldown");site.NextWave=DateTime.UtcNow;site.Tick();if(site.Raiders.Count!=3)throw new Exception("Patrol replacement");File.AppendAllText("island-foundation.log","PASS encounter spawn/cap/death/cooldown/replacement "+site.Theme+"\n");}}
  catch(Exception ex){File.AppendAllText("island-foundation.log","FAIL "+ex+"\n");}
  try{
   var player=new PlayerMobile{Player=true};player.MoveToWorld(new Point3D(4210,2928,0),Map.Trammel);ArrayList move;
   var placement=HousePlacement.Check(player,0x7E,new Point3D(4196,2868,0),out move);
   File.AppendAllText("island-foundation.log","Castle placement="+placement+"; displaced="+move.Count+"\n");player.Delete();if(placement!=HousePlacementResult.Valid||move.Count!=0)throw new Exception("House plot blocked");
   var boat=new SmallBoat(Direction.South);bool clear=true;
   for(int y=2960;y<=2980;y++)if(!boat.CanFit(new Point3D(4237,y,-5),Map.Trammel,boat.SouthID)){clear=false;File.AppendAllText("island-foundation.log","Boat blocked y="+y+"\n");break;}
   boat.Delete();File.AppendAllText("island-foundation.log","Boat south corridor="+clear+"\n");if(!clear)throw new Exception("Boat exit blocked");File.AppendAllText("island-foundation.log","FOUNDATION COMPLETE\n");
  }catch(Exception ex){File.AppendAllText("island-foundation.log","Navigation FAIL "+ex+"\n");}
  try{CoveEncounterSmoke.Run();}catch(Exception ex){File.AppendAllText("island-foundation.log","COVE FAIL "+ex+"\n");}
  try{IslandEstateSmoke.Run();}catch(Exception ex){File.AppendAllText("island-foundation.log","ESTATE FAIL "+ex+"\n");}
  Core.Kill(false);
 };}
}
