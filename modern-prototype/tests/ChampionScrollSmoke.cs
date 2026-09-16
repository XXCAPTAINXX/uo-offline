using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class ChampionScrollSmoke {
 public static void Run(Action<string> log) {
  var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=200};owner.AddItem(new Backpack());
  owner.MoveToWorld(new Point3D(5557,824,50),Map.Felucca);
  var pet=new Dog();pet.SetControlMaster(owner);
  var companion=new HavenCompanion();typeof(HavenCompanion).GetField("_owner",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(companion,owner);companion.SetControlMaster(owner);
  try {
   foreach(Mobile attacker in new Mobile[]{owner,pet,companion}) {
    var boss=new Barracoon();boss.MoveToWorld(owner.Location,owner.Map);boss.AIObject.m_Timer.Stop();
    try {
     boss.RegisterDamage(12000,attacker);
     int before=owner.Backpack.FindItemsByType(typeof(PowerScroll),true).Length;
     boss.GivePowerScrolls();
     int awarded=owner.Backpack.FindItemsByType(typeof(PowerScroll),true).Length-before;
     if(awarded!=Server.Engines.CannedEvil.ChampionSystem.PowerScrollAmount)throw new Exception("Missing scrolls for "+attacker.GetType().Name+": "+awarded);
     log("PASS "+attacker.GetType().Name+" damage awards "+awarded+" power scrolls directly to owner's backpack");
    } finally {boss.Delete();}
   }
  } finally {pet.Delete();companion.Delete();owner.Delete();}
 }
}
