using System;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class WarriorSmoke {
 public static void Run(Action<string> log){
 var p=new PlayerMobile{Body=0x190,RawStr=100};p.AddItem(new Backpack());p.MoveToWorld(new Point3D(3400,2400,Map.Trammel.GetAverageZ(3400,2400)),Map.Trammel);
 var c=new HavenCompanion();typeof(HavenCompanion).GetField("_owner",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,p);c.SetControlMaster(p);c.MoveToWorld(p.Location,p.Map);
 var a=new Mongbat{HitsMaxSeed=10000,Hits=10000,Frozen=true};var b=new Mongbat{HitsMaxSeed=10000,Hits=10000,Frozen=true};var neutral=new Horse{Frozen=true};
 a.MoveToWorld(c.Location,c.Map);b.MoveToWorld(c.Location,c.Map);neutral.MoveToWorld(c.Location,c.Map);
 try{
 c.Combatant=a;b.Combatant=p;
 int damage=100;c.AlterMeleeDamageTo(a,ref damage);if(damage!=125)throw new Exception("Melee bonus "+damage);
 damage=100;c.AlterMeleeDamageFrom(a,ref damage);if(damage!=85)throw new Exception("Defense "+damage);
 int before=b.Hits,untouched=neutral.Hits;c.OnGaveMeleeAttack(a);if(b.Hits>=before||neutral.Hits!=untouched)throw new Exception("Sweep targets");
 before=b.Hits;c.OnGaveMeleeAttack(a);if(b.Hits!=before)throw new Exception("Sweep cooldown");
 typeof(HavenCompanion).GetField("_role",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,CompanionRole.Bard);
 damage=100;c.AlterMeleeDamageTo(a,ref damage);if(damage!=100)throw new Exception("Other role buffed");
 log("PASS warrior damage, defense, sweep, cooldown, neutral exclusion, other-role exclusion");
 for(int id=0x20;id<=0x28;id++)log("ART "+id+" "+TileData.ItemTable[id].Name+" height="+TileData.ItemTable[id].Height);
 }finally{c.Delete();a.Delete();b.Delete();neutral.Delete();p.Delete();}
 }
}
