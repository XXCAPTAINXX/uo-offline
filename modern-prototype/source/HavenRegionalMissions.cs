using System;
using System.Linq;
using System.Collections.Generic;
using Server.Items;
namespace Server.HavenPrototype {
 public static class HavenRegionalMissions {
  public static readonly CompanionMission[] Kinds={CompanionMission.Reagents,CompanionMission.MalasReagents,CompanionMission.DoomBones,CompanionMission.AbyssEssences,CompanionMission.AbyssIngredients,CompanionMission.JewelRecovery,CompanionMission.EnchantedTimber,CompanionMission.DragonSalvage,CompanionMission.DoomRecon};
  public static readonly string[] Names={"Magery reagents","Malas: necromantic reagents","Malas: Doom bones","Abyss: essences","Abyss: rare ingredients","Ruins: jewel recovery","Heartwood: enchanted timber","Eodon: dragon salvage","Doom: artifact reconnaissance"};
  public static bool Valid(CompanionMission k){return Array.IndexOf(Kinds,k)>=0;}
  public static int Requirement(CompanionMission k){return k==CompanionMission.DoomRecon?100:k==CompanionMission.JewelRecovery?70:k==CompanionMission.EnchantedTimber?85:k==CompanionMission.DragonSalvage?95:k==CompanionMission.MalasReagents?40:k==CompanionMission.DoomBones?60:k==CompanionMission.AbyssEssences?80:k==CompanionMission.AbyssIngredients?100:0;}
  public static double Rating(HavenCompanion c){return Math.Min(c.Skills.MagicResist.Base,Math.Max(c.Skills.Tactics.Base,Math.Max(c.Skills.Magery.Base,c.Skills.Archery.Base)));}
  public static double Rating(HavenCompanion c,CompanionMission k){return k==CompanionMission.DoomRecon?Math.Max(c.Skills.Tactics.Base,Math.Max(c.Skills.Magery.Base,c.Skills.Archery.Base)):Rating(c);}
  public static bool CanStart(HavenCompanion c,CompanionMission k){return Rating(c,k)>=Requirement(k);}
  public static int Bonus(int minutes){return minutes==60?125:minutes==30?115:minutes==15?110:100;}
  static void Add(Dictionary<int,int> rewards,Type type,int amount){int id=Array.IndexOf(HavenResources.Types,type);if(id<0)throw new InvalidOperationException("Unregistered mission material: "+type.Name);rewards.Add(id,amount);}
  public static void Prepare(HavenCompanion c,CompanionMission k,int minutes,Dictionary<int,int> rewards){if(!CanStart(c,k))throw new InvalidOperationException("Unqualified regional mission");int bonus=Bonus(minutes);if(k==CompanionMission.DoomRecon)return;
   if(Specialist(k)){PrepareSpecialist(k,minutes,rewards);return;}
   if(k==CompanionMission.Reagents){foreach(var t in new[]{typeof(BlackPearl),typeof(Bloodmoss),typeof(Garlic),typeof(Ginseng),typeof(MandrakeRoot),typeof(Nightshade),typeof(SulfurousAsh),typeof(SpidersSilk)})Add(rewards,t,minutes*10*bonus/100);}
   else if(k==CompanionMission.MalasReagents){foreach(var t in new[]{typeof(BatWing),typeof(GraveDust),typeof(DaemonBlood),typeof(NoxCrystal),typeof(PigIron)})Add(rewards,t,minutes*3*bonus/100);}
   else if(k==CompanionMission.DoomBones){Add(rewards,typeof(DaemonBone),minutes*2*bonus/100);Add(rewards,typeof(Bone),minutes*5*bonus/100);}
   else {var pool=k==CompanionMission.AbyssEssences?HavenResources.Types.Where(t=>t.Name.StartsWith("Essence")).ToArray():new[]{typeof(DaemonClaw),typeof(LavaSerpentCrust),typeof(GoblinBlood),typeof(FaeryDust),typeof(FeyWings),typeof(VialOfVitriol),typeof(VoidOrb),typeof(UndyingFlesh),typeof(ReflectiveWolfEye),typeof(CrystallineBlackrock),typeof(ArcanicRuneStone),typeof(SeedOfRenewal),typeof(SpiderCarapace),typeof(BottleIchor),typeof(SilverSnakeSkin),typeof(DelicateScales)};int first=Utility.Random(pool.Length);int amount=Math.Max(1,(minutes*bonus/100)/(k==CompanionMission.AbyssEssences?2:5));for(int i=0;i<3;i++)Add(rewards,pool[(first+i)%pool.Length],amount);}
  }
  public static bool Specialist(CompanionMission k){return k>=CompanionMission.JewelRecovery&&k<=CompanionMission.DragonSalvage;}
  static void PrepareSpecialist(CompanionMission k,int minutes,Dictionary<int,int> rewards){
   int units=minutes*Bonus(minutes)/100;
   if(k==CompanionMission.JewelRecovery){
    var gems=new[]{typeof(BlueDiamond),typeof(DarkSapphire),typeof(EcruCitrine),typeof(FireRuby),typeof(PerfectEmerald),typeof(Turquoise)};
    int first=Utility.Random(gems.Length),second=(first+1+Utility.Random(gems.Length-1))%gems.Length;
    Add(rewards,gems[first],Math.Max(1,units/10));Add(rewards,gems[second],Math.Max(1,units/10));Add(rewards,typeof(Diamond),units);
   }else if(k==CompanionMission.EnchantedTimber){
    var woods=new[]{typeof(HeartwoodLog),typeof(BloodwoodLog),typeof(FrostwoodLog)};
    var supplies=new[]{typeof(BarkFragment),typeof(LuminescentFungi),typeof(SwitchItem),typeof(ParasiticPlant)};
    Add(rewards,woods[Utility.Random(woods.Length)],units*5);Add(rewards,supplies[Utility.Random(supplies.Length)],Math.Max(1,units/5));
   }else{
    var scales=new[]{typeof(RedScales),typeof(YellowScales),typeof(BlackScales),typeof(GreenScales),typeof(WhiteScales),typeof(BlueScales)};
    Add(rewards,scales[Utility.Random(scales.Length)],units*3);Add(rewards,typeof(DragonBlood),units);Add(rewards,typeof(DelicateScales),Math.Max(1,units/5));
   }
  }
  static string SpecialistDescription(CompanionMission k,int minutes){int u=minutes*Bonus(minutes)/100;
   return (k==CompanionMission.JewelRecovery?"Two rare gem types: "+Math.Max(1,u/10)+" each; "+u+" diamonds.":k==CompanionMission.EnchantedTimber?(u*5)+" Heartwood/Bloodwood/Frostwood logs; "+Math.Max(1,u/5)+" rare wood ingredients.":(u*3)+" colored scales, "+u+" dragon blood, "+Math.Max(1,u/5)+" delicate scales.")+" Ledger delivery; rolls fixed at dispatch.";
  }
  public static string Description(CompanionMission k,int minutes){int bonus=Bonus(minutes);if(k==CompanionMission.DoomRecon)return HavenDoomMission.Points(minutes).ToString("N0")+" Doom points, then an artifact roll. Failed rolls retain points; artifacts wait in Doom loot.";if(Specialist(k))return SpecialistDescription(k,minutes);string s=k==CompanionMission.Reagents?(minutes*10*bonus/100)+" of each of eight Magery reagents.":k==CompanionMission.MalasReagents?(minutes*3*bonus/100)+" of each necromantic reagent.":k==CompanionMission.DoomBones?(minutes*2*bonus/100)+" daemon bones and "+(minutes*5*bonus/100)+" ordinary bones.":"Three resource types, "+Math.Max(1,(minutes*bonus/100)/(k==CompanionMission.AbyssEssences?2:5))+" units each, selected when dispatched.";return s+" Stored in the ledger. Includes "+(bonus-100)+"% material completion bonus.";}
 }
}
