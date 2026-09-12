using System;
using Server;
namespace Server.HavenPrototype {
 public static class HavenCompanionProgression {
  public static int TamingTraining(int trained,int minutes,double roll) {
   int budget=minutes*2*HavenRegionalMissions.Bonus(minutes)/100;
   int fast=Math.Min(Math.Max(0,1000-trained),budget*5);
   int remainder=Math.Max(0,budget-(fast+4)/5);
   return fast+Scaled(trained+fast,remainder,roll);
  }
  public static int Scaled(int trained,int amount,double roll) {
   if(amount<=0)return 0;
   int ordinary=Math.Min(amount,Math.Max(0,1200-trained));
   double slow=(amount-ordinary)/20.0;
   int whole=(int)slow;
   return ordinary+whole+(roll<slow-whole?1:0);
  }
  public static int TrainingMultiplier(Mobile from){return HavenPreview.Enabled&&from is HavenCompanion&&from.Map==Map.Trammel&&from.X>=3314&&from.X<3814&&from.Y>=2345&&from.Y<3095?2:1;}
  public static int Amount(Mobile from,Skill skill,int amount){return from is HavenCompanion?Scaled(skill.BaseFixedPoint,amount*(HavenPreview.Enabled&&skill==from.Skills.Peacemaking?3:TrainingMultiplier(from)),Utility.RandomDouble()):amount;}
 }
}
