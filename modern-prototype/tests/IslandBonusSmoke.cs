using System;
using Server;
using Server.Mobiles;
using Server.HavenPrototype;
public static class IslandBonusSmoke {
 public static void Run(Action<string> log){
  var p=new PlayerMobile();
  try{
   foreach(var point in new[]{new Point3D(4196,2868,7),new Point3D(4196,2868,47),new Point3D(4213,2938,1),new Point3D(3500,2570,14)}){
    p.MoveToWorld(point,Map.Trammel);if(HavenNewcomerBonus.Luck(p)!=1000)throw new Exception("Missing Luck at "+point);
    p.Skills.Wrestling.Cap=120;p.Skills.Wrestling.Base=99.9;
    if(HavenNewcomerBonus.Chance(p,p.Skills.Wrestling)!=5||HavenNewcomerBonus.Amount(p,p.Skills.Wrestling,1)!=1)throw new Exception("Gain cap boundary failed");
    p.Skills.Wrestling.Base=100;if(HavenNewcomerBonus.Chance(p,p.Skills.Wrestling)!=1)throw new Exception("Bonus above counted limit");
    p.Skills.Lockpicking.Cap=120;p.Skills.Lockpicking.Base=110;if(HavenNewcomerBonus.Amount(p,p.Skills.Lockpicking,1)!=5)throw new Exception("Free skill bonus missing");
   }
   foreach(var point in new[]{new Point3D(4127,2868,0),new Point3D(4304,2868,0),new Point3D(4196,2799,0),new Point3D(4196,2976,0)}){p.MoveToWorld(point,Map.Trammel);if(HavenNewcomerBonus.Luck(p)!=0)throw new Exception("Bonus outside boundary");}
   p.MoveToWorld(new Point3D(4196,2868,7),Map.Felucca);if(HavenNewcomerBonus.Luck(p)!=0)throw new Exception("Wrong facet bonus");
   log("PASS Haven, house floors and dock +1000 Luck; 5x gains with counted/free caps; outside edges and wrong facet excluded");
  }finally{p.Delete();}
 }
}
