using System;
using Server;
namespace Server.HavenPrototype {
 public static class HavenCompanionProgression {
  public static int Scaled(int trained,int amount,double roll) {
   if(amount<=0)return 0;
   int ordinary=Math.Min(amount,Math.Max(0,1200-trained));
   double slow=(amount-ordinary)/20.0;
   int whole=(int)slow;
   return ordinary+whole+(roll<slow-whole?1:0);
  }
  public static int Amount(Mobile from,Skill skill,int amount){return from is HavenCompanion?Scaled(skill.BaseFixedPoint,amount,Utility.RandomDouble()):amount;}
 }
}
