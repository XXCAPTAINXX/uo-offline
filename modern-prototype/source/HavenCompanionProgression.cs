using System;
using Server;
namespace Server.HavenPrototype {
 public static class HavenCompanionProgression {
  // Called only after native Peacemaking rejects invalid and already-calmed targets.
  public static bool CheckPeaceAttempt(Mobile from, Mobile target, double min, double max) {
   if(!HavenPreview.Enabled || !(from is HavenCompanion))
    return from.CheckTargetSkill(SkillName.Peacemaking,target,min,max);
   double value=from.Skills.Peacemaking.Value;
   bool success=value>=max || (value>=min && Utility.Random(100)<=(int)((value-min)/(max-min)*100));
   // Train once per valid attempt, including failures and easy targets. Gain applies
   // locks, caps and the existing 3x multiplier; do not also run the native gain roll.
   if(from.Alive) Server.Misc.SkillCheck.Gain(from,from.Skills.Peacemaking,1);
   EventSink.InvokeSkillCheck(new SkillCheckEventArgs(from,from.Skills.Peacemaking,success));
   return success;
  }
  public static int TamingTraining(int trained,int minutes,double roll) {
   int budget=minutes*2*HavenRegionalMissions.Bonus(minutes)/100;
   int fast=Math.Min(Math.Max(0,1000-trained),budget*5);
   int remainder=Math.Max(0,budget-(fast+4)/5);
   return fast+Scaled(trained+fast,remainder,roll);
  }
  public static void TamingBardPractice(HavenCompanion c,int minutes) {
   // A little supporting practice per completed trip; Peacemaking keeps its 3x rate.
   int units=Math.Max(0,minutes)/5;
   foreach(var name in new[]{SkillName.Peacemaking,SkillName.Musicianship}) {
    var skill=c.Skills[name];
    if(skill.Lock!=SkillLock.Up||skill.BaseFixedPoint>=skill.CapFixedPoint)continue;
    int gain=Scaled(skill.BaseFixedPoint,units*(name==SkillName.Peacemaking?3:1),Utility.RandomDouble());
    skill.BaseFixedPoint=Math.Min(skill.CapFixedPoint,skill.BaseFixedPoint+gain);
   }
  }
  public static int Scaled(int trained,int amount,double roll) {
   if(amount<=0)return 0;
   int ordinary=Math.Min(amount,Math.Max(0,1200-trained));
   double slow=(amount-ordinary)/20.0;
   int whole=(int)slow;
   return ordinary+whole+(roll<slow-whole?1:0);
  }
  public static int TrainingMultiplier(Mobile from){return HavenPreview.Enabled&&from is HavenCompanion&&HavenNewcomerBonus.InBonusArea(from)?2:1;}
  public static int Amount(Mobile from,Skill skill,int amount){return from is HavenCompanion?Scaled(skill.BaseFixedPoint,amount*(HavenPreview.Enabled&&skill==from.Skills.Peacemaking?3:TrainingMultiplier(from)),Utility.RandomDouble()):amount;}
 }
}

