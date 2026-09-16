using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;
namespace Server.HavenPrototype {
 public static class HavenOvercapBenefits {
  public static bool Eligible(Mobile m){var pet=m as BaseCreature;return HavenPreview.Enabled&&pet!=null&&(pet is HavenCompanion||HavenPetRarity.Find(pet)!=null);}
  public static double Excess(Mobile m,SkillName skill){return Eligible(m)?Math.Max(0,Math.Min(30,m.Skills[skill].Value-120)):0;}
  public static double PeaceSeconds(Mobile m,double nativeSeconds){return nativeSeconds*(1+Excess(m,SkillName.Peacemaking)/100);}
  public static int DiscordEffect(Mobile m,int nativeEffect){return nativeEffect-(int)Math.Floor(Excess(m,SkillName.Discordance)/5);}
  public static double DiscordMusic(Mobile m,double nativeMusic){return Eligible(m)?Math.Max(nativeMusic,m.Skills.Musicianship.Value):nativeMusic;}
  public static string Describe(Mobile m){
   var lines=new List<string>();double peace=Excess(m,SkillName.Peacemaking),discord=Excess(m,SkillName.Discordance),music=Excess(m,SkillName.Musicianship);
   if(peace>0)lines.Add("Peacemaking: +"+peace.ToString("0.#")+"% successful calm duration.");
   if(discord>0)lines.Add("Discordance: "+(28+(int)Math.Floor(discord/5))+"% maximum debuff before native target reductions.");
   if(music>0)lines.Add("Musicianship: actual skill above 120 contributes to Discordance difficulty.");
   return string.Join(" ",lines);
  }
 }
}
