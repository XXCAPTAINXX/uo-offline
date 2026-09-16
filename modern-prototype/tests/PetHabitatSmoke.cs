using System;
using System.IO;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.Items;
using Server.Accounting;
using Server.HavenPrototype;
public static class PetHabitatSmoke
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);File.AppendAllText("pet-habitat-checks.log","PASS "+name+"\n");}
 public static void Initialize(){if(File.Exists("PET-HABITAT-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(12),Run);}
 static void Run(){try{HavenPetHabitats.Ensure();
 File.AppendAllText("pet-habitat-checks.log","Snow="+HavenSnowBearDen.Registry.Count+" Hunt="+HavenAbyssTrial.Registry.Count+" Chelonia="+HavenChelonia.Registry.Count+" Steeds="+World.Items.Values.OfType<HavenVampiricSteedSpawner>().Count()+"\n");
 Check(HavenSnowBearDen.Registry.Any(x=>!x.Deleted),"bear den has valid modern-map floor");var den=HavenSnowBearDen.Registry.First(x=>!x.Deleted);Point3D point;Check(HavenSnowBearDen.TryFloor(den.Location,den.Map,out point),"connected bear roaming floor");den.Tick();Check(den.Bear!=null&&!den.Bear.Deleted,"bear spawns with rarity");
 Check(HavenAbyssTrial.Registry.Any(x=>!x.Deleted),"Ancient Hunt has valid modern-map floor");var trial=HavenAbyssTrial.Registry.First(x=>!x.Deleted);Check(trial.TrySpawnPoint(out point),"guardians have reachable connected floor");
 var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=250};owner.AddItem(new Backpack());var account=new Account("hunt-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"));account[0]=owner;owner.Skills.AnimalTaming.Base=110;owner.Skills.AnimalLore.Base=110;owner.MoveToWorld(HavenAbyssTrial.Landing,Map.TerMur);Check(trial.Start(owner),"qualified player starts Ancient Hunt");Check(trial.Guardians.Count==3,"first wave has three guardians");foreach(var guardian in trial.Guardians.ToArray())guardian.Kill();trial.Tick();Check(trial.Stage==2&&trial.Guardians.Count==3,"second wave has three guardians");foreach(var guardian in trial.Guardians.ToArray())guardian.Kill();var hound=trial.Hound;Check(trial.Stage==3&&hound!=null&&HavenPetDefenses.Tier(hound)>=1,"two waves reveal rare-or-better hellhound");hound.SetControlMaster(owner);trial.Tick();Check(!hound.Deleted&&trial.Hound==null,"tamed hellhound survives encounter cleanup");hound.Delete();owner.Delete();
 Check(HavenChelonia.Registry.Any(x=>!x.Deleted),"Chelonia has valid modern-map floor");var sanctuary=HavenChelonia.Registry.First(x=>!x.Deleted);sanctuary.Tick();Check(sanctuary.Tortoises.Count>0,"tortoise spawn works");
 Check(World.Items.Values.OfType<HavenVampiricSteedSpawner>().Count()==2,"both original steed sites installed");foreach(var spawner in World.Items.Values.OfType<HavenVampiricSteedSpawner>().ToArray()){spawner.Spawn(0);Check(spawner.SpawnCount==1,"native steed spawner creates animal");}int items=World.Items.Count;HavenPetHabitats.Ensure();Check(World.Items.Count==items,"repeated habitat setup does not duplicate fixtures");File.AppendAllText("pet-habitat-checks.log","COMPLETE\n");Core.Kill(false);
 }catch(Exception e){File.AppendAllText("pet-habitat-checks.log","FAIL "+e+"\n");Core.Kill(false);}}
}
