using System;
using Server;
using Server.Mobiles;
using Server.HavenPrototype;
public static class FollowerCombatSmoke {
 public static void Run(Action<string> log){
  var owner=new PlayerMobile{Player=true};var a=new Dog();var b=new Dog();var c=new HavenCompanion();var enemy=new Ogre();
  try{a.SetControlMaster(owner);c.SetControlMaster(owner);b.SetControlMaster(c);foreach(var m in new Mobile[]{owner,a,b,c,enemy})m.MoveToWorld(new Point3D(3400,2400,0),Map.Felucca);
   foreach(var pet in new BaseCreature[]{a,b,c})foreach(var other in new BaseCreature[]{a,b,c})if(pet!=other){if(pet.CanBeHarmful(other,false))throw new Exception("Friendly attack permitted");int hp=pet.Hits;if(pet.Damage(5,other)!=0||pet.Hits!=hp)throw new Exception("Friendly damage");}
   a.Combatant=b;a.FocusMob=b;a.ControlOrder=OrderType.Attack;a.ControlTarget=b;a.BardProvoked=true;a.BardTarget=b;a.AggressiveAction(b);a.OnThink();
   if(a.Combatant==b||a.FocusMob==b||a.ControlOrder==OrderType.Attack||a.BardProvoked||a.Aggressors.Count!=0)throw new Exception("Existing fight retained");
   int before=a.Hits;if(a.Damage(1,enemy)!=1||a.Hits!=before-1)throw new Exception("Enemy damage blocked");
   log("PASS own pets/companion cannot harm each other; active attack, focus, provocation and aggression cleared; enemy damage remains");
  }finally{a.Delete();b.Delete();c.Delete();enemy.Delete();owner.Delete();}
 }
}
