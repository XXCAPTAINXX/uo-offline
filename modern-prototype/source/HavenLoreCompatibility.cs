using System;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.SkillHandlers;
namespace Server.HavenPrototype {
 internal static class HavenLoreCompatibility {
  public static int Clamp(int n,int low,int high){return Math.Max(low,Math.Min(high,n));}
  public static bool Owned(Mobile p,BaseCreature pet){return pet.ControlMaster==p;}
  public static int MaxSlots(BaseCreature pet){return pet.ControlSlotsMax==0?pet.ControlSlots:pet.ControlSlotsMax;}
  public sealed class TrainingView {
   readonly TrainingProfile _profile;readonly BaseCreature _pet;
   public TrainingView(BaseCreature pet,TrainingProfile profile){_pet=pet;_profile=profile;}
   public int Progress {get{return (int)Math.Round(_profile.TrainingProgressPercentile*10000);}}
   public double Points {get{return _profile.TrainingPoints;}}
   public int Healing {get{return _pet.HealChance>0?1:0;}}
   public string Status(BaseCreature pet){return _profile.CanApplyOptions?"Ready to choose training upgrades":_profile.HasBegunTraining?"Combat training in progress":"Training not started";}
  }
  public static TrainingView Training(BaseCreature pet){var profile=PetTrainingHelper.GetTrainingProfile(pet);return profile==null?null:new TrainingView(pet,profile);}
  public static void OpenTraining(Mobile p,BaseCreature pet){if(!HavenAnimalLoreGump.CanInspect(p,pet)||!Owned(p,pet))return;if(PetTrainingHelper.Enabled&&p is PlayerMobile)HavenPetTrainingMenu.Show(p,pet);else p.SendGump(new AnimalLoreGump(pet));}
  public static string Defenses(BaseCreature pet){var element=HavenPetDefenses.Element(pet);int tier=HavenPetDefenses.Tier(pet);if(element.HasValue)return tier==3?"Innate "+element+" immunity (100%); other elements and armor-ignoring damage still work.":"Innate "+element+" defense: at least "+HavenPetDefenses.Resistance(pet,element.Value,0)+"%. Legendary gains immunity.";return pet is HavenSnowBear?"Thick winter hide: at least "+(65+tier*5)+"% physical and "+(75+tier*5)+"% cold resistance.":"";}
  public static string RarityDescription(int tier){switch(tier){case 1:return "+10% starting stats/HP; +5 combat skills, minimum 105 caps; strengthened species signature, no training cost";case 2:return "+20% starting stats/HP; +10 combat skills, minimum 110 caps; stronger signature and species-specific secondary effects, no training cost";case 3:return "Found with 1 follower slot; +30% starting stats/HP; +15 combat skills, minimum 120 caps; 50% chance of 1-3 existing skills at 125-150 with matching caps; strongest species signature, no training cost";default:return "Standard species stats and abilities";}}
  public static void Button(Gump g,int x,int y,int width,int id,string label){for(int offset=0;offset<width;offset+=19)g.AddButton(x+Math.Min(offset,width-19),y,210,210,id,GumpButtonType.Reply,0);g.AddImageTiled(x,y,width,19,5058);g.AddHtml(x,y+1,width,18,"<CENTER><BASEFONT COLOR=#FFFFFF>"+System.Net.WebUtility.HtmlEncode(label)+"</BASEFONT></CENTER>",false,false);}
 }
}
