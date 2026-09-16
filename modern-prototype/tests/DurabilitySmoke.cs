using System;
using System.IO;
using System.Linq;
using Server;
using Server.Items;
using Server.HavenPrototype;
public static class DurabilitySmoke {
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText("durability-checks.log","PASS "+label+"\n");}
 public static void Initialize(){if(File.Exists("DURABILITY-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(7),Run);}
 static void Run(){try{
  var saved=World.Items.Values.OfType<Longsword>().FirstOrDefault(x=>x.Name=="Durability reload fixture");
  if(saved!=null){HavenGearDurability.Apply(saved);Check(saved.MaxHitPoints==250&&saved.HitPoints==230,"reload never resets subsequent wear or repairs capacity");File.AppendAllText("durability-checks.log","RELOAD COMPLETE\n");Core.Kill(false);return;}
  var sword=new Longsword{Name="Durability reload fixture",MaxHitPoints=60,HitPoints=50};HavenGearDurability.Apply(sword);
  Check(sword.MaxHitPoints==255&&sword.HitPoints==245,"migration preserves ten missing durability points");sword.MaxHitPoints=250;sword.HitPoints=230;HavenGearDurability.Apply(sword);Check(sword.MaxHitPoints==250&&sword.HitPoints==230,"repeat maintenance does not repair wear");
  var fresh=new HavenApprenticeBlade();Check(fresh.MaxHitPoints==255&&fresh.HitPoints==255,"new starter weapon has full durability");fresh.Delete();
  var shield=(BaseArmor)HavenMarks.CreateReward(2);Check(shield.MaxHitPoints==255&&shield.HitPoints==255,"new reward shield has full durability");shield.Delete();
  var ring=new BraceletOfFortune{MaxHitPoints=50,HitPoints=40};HavenGearDurability.Apply(ring);Check(ring.MaxHitPoints==255&&ring.HitPoints==245,"jewelry preserves wear");ring.Delete();
  var stronger=new Longsword{MaxHitPoints=300,HitPoints=290};HavenGearDurability.Apply(stronger);Check(stronger.MaxHitPoints==300&&stronger.HitPoints==290,"higher existing durability is preserved");stronger.Delete();
  sword.Movable=false;sword.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);World.Save();File.AppendAllText("durability-checks.log","FRESH COMPLETE\n");Core.Kill(false);
 }catch(Exception e){File.AppendAllText("durability-checks.log","FAIL "+e+"\n");Core.Kill(false);}}
}
