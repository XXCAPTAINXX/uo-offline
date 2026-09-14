using System;
using Server;
using Server.Mobiles;
using Server.HavenPrototype;
public static class FollowerMovementSmoke {
 public static void Run(Action<string> log){
  var owner=new PlayerMobile{Player=true,RawDex=100,Stam=0};var stranger=new PlayerMobile{Player=true};var pet=new Dog();var companion=new HavenCompanion();var assigned=new Dog();var wild=new Dog();
  try{pet.SetControlMaster(owner);companion.SetControlMaster(owner);assigned.SetControlMaster(companion);
   foreach(var mobile in new Mobile[]{owner,pet,companion,assigned})mobile.MoveToWorld(new Point3D(3400,2400,0),Map.Felucca);
   foreach(var a in new Mobile[]{owner,pet,companion,assigned})foreach(var b in new Mobile[]{owner,pet,companion,assigned})if(a!=b&&!a.OnMoveOver(b))throw new Exception("Family collision");
   if(owner.Stam!=0||HavenFollowerMovement.CanPass(owner,stranger)||HavenFollowerMovement.CanPass(owner,wild))throw new Exception("Stamina/foreign scope");
   pet.SetControlMaster(stranger);if(HavenFollowerMovement.CanPass(owner,pet))throw new Exception("Transferred pet still exempt");
   log("PASS owner, companion and assigned pets pass both ways at zero stamina; wild, foreign and transferred pets not exempt");
  }finally{assigned.Delete();pet.Delete();companion.Delete();wild.Delete();owner.Delete();stranger.Delete();}
 }
}
