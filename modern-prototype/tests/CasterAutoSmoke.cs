using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Spellweaving;
using Server.HavenPrototype;
public static class CasterAutoSmoke {
 public static void Run(Action<string> log,Action done){
 var p=new PlayerMobile{Body=0x190,RawStr=200};p.AddItem(new Backpack());p.MoveToWorld(new Point3D(3400,2400,Map.Trammel.GetAverageZ(3400,2400)),Map.Trammel);
 var c=new HavenCompanion{RawInt=500};typeof(HavenCompanion).GetField("_owner",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,p);c.SetControlMaster(p);c.MoveToWorld(p.Location,p.Map);c.SetRole(p,CompanionRole.Caster);
 var ai=c.AIObject as HavenCompanionMageAI;ai.m_Timer.Stop();
 var a=new Ogre{HitsMaxSeed=100000,Hits=100000,CantWalk=true,IsChampionSpawn=true};var b=new Ogre{HitsMaxSeed=100000,Hits=100000,CantWalk=true,IsChampionSpawn=true};var sheep=new Sheep{CantWalk=true};
 a.MoveToWorld(new Point3D(p.X+1,p.Y,p.Z),p.Map);b.MoveToWorld(new Point3D(p.X+2,p.Y,p.Z),p.Map);sheep.MoveToWorld(new Point3D(p.X+1,p.Y+1,p.Z),p.Map);
 Action clean=()=>{c.Delete();a.Delete();b.Delete();sheep.Delete();p.Delete();done();};
 try{
 c.Combatant=a;c.ControlTarget=a;c.ControlOrder=OrderType.Attack;c.Mana=500;
 var fire=ai.ChooseSpell(a);if(!(fire is WildfireSpell))throw new Exception("Auto does not start Wildfire");
 if(!fire.AcquireIndirectTargets(c.Location,11).Contains(a)||fire.AcquireIndirectTargets(c.Location,11).Contains(sheep))throw new Exception("Area filtering");
 int before=a.Hits,safe=sheep.Hits;
 if(!fire.Cast())throw new Exception("Wildfire not cast");
 Timer.DelayCall(TimeSpan.FromSeconds(4),()=>{
 try{
 if(a.Hits>=before||b.Hits>=100000||sheep.Hits!=safe)throw new Exception("Wildfire did not damage both champions safely");
 if(ai._nextWildfire<=DateTime.UtcNow)throw new Exception("Wildfire refresh not scheduled");
 a.Hits=100;c.Mana=500;c.NextSpellTime=0;
 var storm=ai.ChooseSpell(a);if(!(storm is ThunderstormSpell))throw new Exception("Finisher interrupts auto rotation");
 if(!(ai.ChooseSpell(a) is ThunderstormSpell))throw new Exception("Artificial Thunderstorm cooldown");
 int health=a.Hits;if(!storm.Cast())throw new Exception("Thunderstorm not cast");
 Timer.DelayCall(TimeSpan.FromSeconds(2),()=>{
 try{
 if(a.Hits>=health||sheep.Hits!=safe)throw new Exception("Thunderstorm did not land safely");
 ai._nextWildfire=DateTime.UtcNow.AddSeconds(-1);c.Mana=500;if(!(ai.ChooseSpell(b) is WildfireSpell))throw new Exception("Field refresh missing");
 c.Mana=40;if(ai.ChooseSpell(b)!=null)throw new Exception("Mana reserve ignored");
 log("PASS self Wildfire damages champion group; safe targets excluded; Thunderstorm between refreshes without six-second throttle; low-health target does not force Word; refresh and mana reserve");
 }catch(Exception e){log("FAIL "+e);}finally{clean();}
 });
 }catch(Exception e){log("FAIL "+e);clean();}
 });
 }catch(Exception e){log("FAIL "+e);clean();}
 }
}
