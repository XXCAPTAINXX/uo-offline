using System;
using System.Linq;
using System.Collections.Generic;
using Server.Items;
namespace Server.HavenPrototype {
 public static class HavenRegionalMissions {
  public static readonly CompanionMission[] Kinds={CompanionMission.Reagents,CompanionMission.MalasReagents,CompanionMission.DoomBones,CompanionMission.AbyssEssences,CompanionMission.AbyssIngredients};
  public static readonly string[] Names={"Magery reagents","Malas: necromantic reagents","Malas: Doom bones","Abyss: essences","Abyss: rare ingredients"};
  public static bool Valid(CompanionMission k){return Array.IndexOf(Kinds,k)>=0;}
  public static int Requirement(CompanionMission k){return k==CompanionMission.MalasReagents?40:k==CompanionMission.DoomBones?60:k==CompanionMission.AbyssEssences?80:k==CompanionMission.AbyssIngredients?100:0;}
  public static double Rating(HavenCompanion c){return Math.Min(c.Skills.MagicResist.Base,Math.Max(c.Skills.Tactics.Base,Math.Max(c.Skills.Magery.Base,c.Skills.Archery.Base)));}
  public static bool CanStart(HavenCompanion c,CompanionMission k){return Rating(c)>=Requirement(k);}
  public static int Bonus(int minutes){return minutes==60?125:minutes==30?115:minutes==15?110:100;}
  static void Add(Dictionary<int,int> rewards,Type type,int amount){int id=Array.IndexOf(HavenResources.Types,type);if(id<0)throw new InvalidOperationException("Unregistered mission material: "+type.Name);rewards.Add(id,amount);}
  public static void Prepare(HavenCompanion c,CompanionMission k,int minutes,Dictionary<int,int> rewards){if(!CanStart(c,k))throw new InvalidOperationException("Unqualified regional mission");int bonus=Bonus(minutes);
   if(k==CompanionMission.Reagents){foreach(var t in new[]{typeof(BlackPearl),typeof(Bloodmoss),typeof(Garlic),typeof(Ginseng),typeof(MandrakeRoot),typeof(Nightshade),typeof(SulfurousAsh),typeof(SpidersSilk)})Add(rewards,t,minutes*10*bonus/100);}
   else if(k==CompanionMission.MalasReagents){foreach(var t in new[]{typeof(BatWing),typeof(GraveDust),typeof(DaemonBlood),typeof(NoxCrystal),typeof(PigIron)})Add(rewards,t,minutes*3*bonus/100);}
   else if(k==CompanionMission.DoomBones){Add(rewards,typeof(DaemonBone),minutes*2*bonus/100);Add(rewards,typeof(Bone),minutes*5*bonus/100);}
   else {var pool=k==CompanionMission.AbyssEssences?HavenResources.Types.Where(t=>t.Name.StartsWith("Essence")).ToArray():new[]{typeof(DaemonClaw),typeof(LavaSerpentCrust),typeof(GoblinBlood),typeof(FaeryDust),typeof(FeyWings),typeof(VialOfVitriol),typeof(VoidOrb),typeof(UndyingFlesh),typeof(ReflectiveWolfEye),typeof(CrystallineBlackrock),typeof(ArcanicRuneStone),typeof(SeedOfRenewal),typeof(SpiderCarapace),typeof(BottleIchor),typeof(SilverSnakeSkin),typeof(DelicateScales)};int first=Utility.Random(pool.Length);int amount=Math.Max(1,(minutes*bonus/100)/(k==CompanionMission.AbyssEssences?2:5));for(int i=0;i<3;i++)Add(rewards,pool[(first+i)%pool.Length],amount);}
  }
  public static string Description(CompanionMission k,int minutes){int bonus=Bonus(minutes);string s=k==CompanionMission.Reagents?(minutes*10*bonus/100)+" of each of eight Magery reagents.":k==CompanionMission.MalasReagents?(minutes*3*bonus/100)+" of each necromantic reagent.":k==CompanionMission.DoomBones?(minutes*2*bonus/100)+" daemon bones and "+(minutes*5*bonus/100)+" ordinary bones.":"Three resource types, "+Math.Max(1,(minutes*bonus/100)/(k==CompanionMission.AbyssEssences?2:5))+" units each, selected when dispatched.";return s+" Stored in the ledger. Includes "+(bonus-100)+"% material completion bonus.";}
 }
}
