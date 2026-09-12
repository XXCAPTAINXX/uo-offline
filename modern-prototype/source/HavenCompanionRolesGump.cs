using System;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenCompanionRolesGump:HavenMenuGump {
  readonly HavenCompanion _companion;readonly int _selected;
  static readonly string[] Names={"Warrior","Caster","Archer","Bard","Healer"};
  static readonly SkillName[][] RoleSkills={
   new[]{SkillName.Swords,SkillName.Tactics,SkillName.Anatomy,SkillName.Parry,SkillName.Wrestling,SkillName.Healing,SkillName.MagicResist},
   new[]{SkillName.Magery,SkillName.EvalInt,SkillName.Spellweaving,SkillName.Necromancy,SkillName.SpiritSpeak,SkillName.Meditation,SkillName.Focus,SkillName.Healing,SkillName.MagicResist},
   new[]{SkillName.Archery,SkillName.Tactics,SkillName.Anatomy,SkillName.Healing,SkillName.MagicResist},
   new[]{SkillName.Musicianship,SkillName.Peacemaking,SkillName.Discordance,SkillName.Provocation,SkillName.AnimalTaming,SkillName.AnimalLore,SkillName.Meditation,SkillName.Healing,SkillName.MagicResist},
   new[]{SkillName.Healing,SkillName.Anatomy,SkillName.Magery,SkillName.EvalInt,SkillName.Meditation,SkillName.Focus,SkillName.MagicResist}
  };
  static readonly string[] Summaries={"Sword and shield","Magery and Spellweaving","Ranged weapon combat","Songs and crowd control","Healing and group support"};
  static readonly string[] Details={
   "Fights up close with a sword and shield.<BR><BR>Uses weapon skills and Parrying to attack and defend.",
   "Attacks with Magery and Spellweaving.<BR><BR>Uses Wraith Form and automatic Arcane Focus to support spellcasting.",
   "Fights with a bow from range.<BR><BR>Uses Archery for weapon attacks and keeps distance from its target.",
   "Uses Peacemaking, Discordance and Provocation.<BR><BR>Mastery songs become available at 90 skill. Join the party to share them. This role also supports taming assistance.",
   "Provides stronger direct heals, cures and resurrection, prioritizing urgent patients.<BR><BR>Emergency group recovery costs 30 mana, has a 20-second cooldown, and reaches allies within six tiles."
  };
  void Text(int x,int y,int w,int h,string text){AddHtml(x,y,w,h,"<BASEFONT COLOR=#342B23>"+text+"</BASEFONT>",false,false);}
  public HavenCompanionRolesGump(HavenCompanion c,int selected=-1):base(50,50){
   _companion=c;_selected=Math.Max(0,Math.Min(4,selected<0?(int)c.Role:selected));
   AddBackground(0,0,680,660,3000);
   Text(24,20,630,28,"<B>"+HavenMenuText.Encode(c.Name)+" - combat roles</B>");
   Text(24,53,630,24,"Active role: "+Names[(int)c.Role]+" | All roles can heal");
   AddBackground(16,90,224,496,3000);AddBackground(250,90,414,496,3000);
   Text(28,105,200,25,"<B>1. Choose a role</B>");
   for(int i=0;i<5;i++)FlatButton(28,145+i*42,200,10+i,Names[i]+(i==(int)c.Role?" [active]":i==_selected?" [selected]":""));
   Text(266,105,380,25,"<B>"+Names[_selected]+"</B>");
   Text(266,138,380,25,Summaries[_selected]);
   Text(266,177,380,98,Details[_selected]);
   Text(266,280,190,24,"<B>Relevant skills</B>");Text(453,280,58,24,"Base");Text(515,280,58,24,"Now");Text(581,280,62,24,"Cap");
   var skills=RoleSkills[_selected];for(int i=0;i<skills.Length;i++){var skill=c.Skills[skills[i]];int y=307+i*23;Text(266,y,182,23,HavenMenuText.Encode(skill.Name));Text(453,y,58,23,skill.Base.ToString("F1"));Text(515,y,58,23,skill.Value.ToString("F1"));Text(581,y,62,23,skill.Cap.ToString("F1"));}
   Text(28,377,196,104,"Base = trained skill.<BR>Now = current bonuses.<BR>Cap = training limit.<BR><BR>Now reflects equipped gear.");
   if(_selected==3)Text(28,483,196,100,HavenMenuText.Encode(c.BardStatus()));
   if(_selected==(int)c.Role)Text(266,552,380,24,"This role is active.");
   else FlatButton(266,552,380,1,"Use "+Names[_selected]+" role");
   Text(24,595,630,24,"Change roles nearby, outside combat. Equipment and trained skills are kept.");
   FlatButton(24,630,110,0,"Back");
  }
  public override void OnResponse(NetState state,RelayInfo info){
   var owner=state.Mobile;if(_companion.Deleted||!_companion.IsOwner(owner))return;
   if(info.ButtonID==0){_companion.Show(owner,true);return;}
   if(_companion.ShowAwayTimer(owner))return;
   int selected=_selected;
   if(info.ButtonID>=10&&info.ButtonID<15)selected=info.ButtonID-10;
   else if(info.ButtonID==1&&!_companion.SetRole(owner,(CompanionRole)_selected))owner.SendMessage("Stand nearby and finish combat before changing roles.");
   owner.CloseGump(typeof(HavenCompanionRolesGump));owner.SendGump(new HavenCompanionRolesGump(_companion,selected));
  }
 }
}
