using System;
using Server;
using Server.Mobiles;
public static class BarracoonStaySmoke {
 public static void Run(Action<string> log){
 var boss=new Barracoon();var enemy=new PlayerMobile{Body=0x190,RawStr=1000};
 var spot=new Point3D(3400,2400,Map.Trammel.GetAverageZ(3400,2400));enemy.MoveToWorld(spot,Map.Trammel);boss.MoveToWorld(spot,Map.Trammel);
 try{boss.Hits=1;boss.Combatant=enemy;boss.AIObject.Action=ActionType.Combat;
 if(boss.CanFlee)throw new Exception("Can still flee");
 for(int i=0;i<100;i++){boss.AIObject.DoActionCombat();if(boss.AIObject.Action==ActionType.Flee)throw new Exception("Low-health fleeing");}
 boss.AIObject.Action=ActionType.Flee;boss.OnThink();if(boss.AIObject.Action!=ActionType.Combat)throw new Exception("Did not resume fight");
 if(boss.Frozen||boss.CantWalk)throw new Exception("Movement disabled");
 log("PASS one-health Barracoon stays in combat over 100 AI decisions, exits existing flee state, and retains normal movement");
 }finally{boss.Delete();enemy.Delete();}
 }
}
