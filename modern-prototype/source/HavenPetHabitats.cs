using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
namespace Server.HavenPrototype
{
 public class HavenVampiricSteedSpawner:Spawner
 {
  public HavenVampiricSteedSpawner():base(1,TimeSpan.FromSeconds(10),TimeSpan.FromSeconds(15),0,0,new List<string>{"VampiricSteed"}){}
  // Clear tamed/deleted entries before the native availability check, which otherwise skips cleanup when full.
  public override void Spawn(){foreach(var pet in GetSpawn().OfType<BaseCreature>().ToArray()){if(pet.Owners.Count>0||pet.Map==Map.Internal)RemoveSpawn(pet);}Defrag();base.Spawn();}
  public HavenVampiricSteedSpawner(Serial serial):base(serial){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();MinDelay=TimeSpan.FromSeconds(10);MaxDelay=TimeSpan.FromSeconds(15);NextSpawn=TimeSpan.FromSeconds(10);}
 }
 public static class HavenPetHabitats
 {
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Timer.DelayCall(TimeSpan.FromSeconds(8),Ensure);};}
  public static void Ensure(){
   HavenSnowBearDen.Install();
   if(!HavenAbyssTrial.Registry.Any(x=>!x.Deleted)&&HavenAbyssTrial.TerrainReady()){var trial=new HavenAbyssTrial();trial.MoveToWorld(HavenAbyssTrial.Site,Map.TerMur);trial.Register();}
   if(!HavenChelonia.Registry.Any(x=>!x.Deleted)&&Map.Trammel.CanFit(HavenChelonia.Site,16,false,false)){var sanctuary=new HavenChelonia();sanctuary.MoveToWorld(HavenChelonia.Site,Map.Trammel);sanctuary.Build();}
   Steed(Map.Trammel,3675,2410);Steed(Map.Felucca,1388,1498);
  }
  static void Steed(Map map,int x,int y){if(World.Items.Values.OfType<HavenVampiricSteedSpawner>().Any(s=>!s.Deleted&&s.Map==map&&s.X==x&&s.Y==y))return;var point=new Point3D(x,y,map.GetAverageZ(x,y));if(!map.CanSpawnMobile(point))return;var spawner=new HavenVampiricSteedSpawner();spawner.MoveToWorld(point,map);}
 }
}
