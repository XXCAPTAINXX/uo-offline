using System;
using Server.Items;
namespace Server.HavenPrototype {
 public static class HavenEncounterBossLoot {
  // One gear roll per completed invasion, shared in the protected spoils chest.
  public static double Chance(int tier){return tier<5?0:Math.Min(.30,.12+(tier-5)*.012);}
  public static Item Roll(int tier,double roll,int choice){
   if(roll>=Chance(tier))return null;
   Func<Item>[] pool=tier>=12
    ?new Func<Item>[] {()=>new HavenIronwakeShield(),()=>new HavenTidecastingRing(),()=>new HavenDeepcastingBracelet(),()=>new HavenTidecallerBook(),()=>new HavenDrownedGrimoire()}
    :new Func<Item>[] {()=>new HavenIronwakeShield(),()=>new HavenTidecastingRing(),()=>new HavenDeepcastingBracelet()};
   return pool[Math.Abs(choice%pool.Length)]();
  }
 }
}
