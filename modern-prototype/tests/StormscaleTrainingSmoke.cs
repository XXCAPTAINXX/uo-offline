using System;
using Server;
using Server.Mobiles;
using Server.HavenPrototype;
public static class StormscaleTrainingSmoke {
 public static void Run(Action<string> log){
  var pet=new HavenStormscale{Controlled=true};var drake=new Drake();
  try{
   for(int category=2;category<8;category++)if(HavenPetTrainingMenu.Options(pet,category).Count==0)throw new Exception("Empty training category "+category);
   pet.Skills.Discordance.Base=0;if(!PetTrainingHelper.ValidateTrainingPoint(pet,SkillName.Discordance))throw new Exception("Zero skill blocked");
   if(PetTrainingHelper.ValidateTrainingPoint(drake,SkillName.Discordance))throw new Exception("Normal drake changed");
   var profile=PetTrainingHelper.GetAbilityProfile(pet,true);
   if(!profile.AddAbility(MagicalAbility.Mysticism)||!profile.HasAbility(MagicalAbility.Mysticism))throw new Exception("Cannot learn Mysticism");
   if(pet.AIObject is HavenStormscaleAI)throw new Exception("Learned AI still overridden");
   log("PASS Stormscale skill/ability categories populated; zero-base skill eligibility; learned Mysticism uses native AI; ordinary drake unchanged");
  }finally{pet.Delete();drake.Delete();}
 }
}
