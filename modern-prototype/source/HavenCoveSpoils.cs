using System;
using Server.Items;
namespace Server.HavenPrototype {
 public static class HavenCoveSpoils {
  // Completion rolls are placed in the existing persistent owner-only parcel.
  // A full backpack cannot lose or reroll a reward.
  public static void Add(Container parcel,int theme){
   var adventure=Adventure(theme,Utility.RandomDouble(),Utility.Random(6));
   if(adventure!=null)parcel.DropItem(adventure);
   var gear=Gear(theme,Utility.RandomDouble(),Utility.Random(2));
   if(gear!=null)parcel.DropItem(gear);
  }
  public static Item Adventure(int theme,double roll,int choice){
   if(roll<0||roll>=0.25)return null;
   choice=Math.Max(0,choice);
   if(theme==0)return new TreasureMap(3+choice%3,Map.Trammel);
   if(theme==1){if(choice%3==0)return new MessageInABottle();if(choice%3==1)return new SpecialFishingNet();return new RuinedShipPlans();}
   return new ScrollOfAlacrity(choice%2==0?SkillName.Magery:SkillName.Spellweaving);
  }
  public static Item Gear(int theme,double roll,int choice){
   if(roll<0||roll>=0.05)return null;
   if(theme==0)return new HavenStormguardShield();
   if(theme==1)return new HavenHooksShield();
   return choice%2==0?(Item)new HavenTidecallerBook():new HavenDrownedGrimoire();
  }
 }
}
