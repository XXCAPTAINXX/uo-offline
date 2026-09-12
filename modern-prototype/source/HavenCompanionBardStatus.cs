using System;
using System.Linq;
using Server.Spells.SkillMasteries;
using BardParty=Server.Engines.PartySystem.Party;
namespace Server.HavenPrototype {
 public partial class HavenCompanion {
  internal string BardStatus(){
   if(Role!=CompanionRole.Bard)return "Bard role is inactive.";
   if(Skills.Musicianship.Value<90)return "Masteries need 90 Musicianship (now "+Skills.Musicianship.Value.ToString("F1")+").";
   if(Skills.Peacemaking.Value<90&&Skills.Discordance.Value<90&&Skills.Provocation.Value<90)return "Masteries need 90 in a bard skill.";
   if(!CanSupportRole(BoundOwner))return "Stay alive and within 12 tiles, with a clear view of your companion.";
   if(BardParty.Get(this)==null||!BardParty.Get(this).Contains(BoundOwner))return "Join your companion's party to receive mastery buffs.";
   var songs=SkillMasterySpell.GetSpells(this);
   var active=songs==null?new string[0]:songs.Where(s=>s is BardSpell&&s.Timer!=null&&s.PartyEffects&&s.PartyList!=null&&s.PartyList.Contains(BoundOwner)).Select(s=>s.GetType().Name.Replace("Spell","")).ToArray();
   if(active.Length>0)return "Shared buffs: "+String.Join(", ",active)+". Mana "+Mana+" / "+ManaMax+".";
   if(TamingAssistActive&&Skills.Peacemaking.Value<90)return "Taming: defensive songs need 90 Peacemaking.";
   if(Mana<30)return "Mastery songs need 30 mana to restart. Mana "+Mana+" / "+ManaMax+".";
   if(_bardMastery==SkillName.Discordance)return "Discordance songs target the enemy; they do not show as buffs on you.";
   return "Preparing mastery songs. Casts can fail or be interrupted. Mana "+Mana+" / "+ManaMax+".";
  }
 }
}
