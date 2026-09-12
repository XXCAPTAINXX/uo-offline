using System;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenCompanionRolesGump:HavenMenuGump {
  readonly HavenCompanion _companion;readonly int _selected;
  static readonly string[] Names={"Warrior","Caster","Archer","Bard","Healer"};
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
   AddBackground(0,0,680,425,3000);
   Text(24,20,630,28,"<B>"+System.Security.SecurityElement.Escape(c.Name)+" - combat roles</B>");
   Text(24,53,630,24,"Active role: "+Names[(int)c.Role]+" | All roles can heal");
   AddBackground(16,90,224,271,3000);AddBackground(250,90,414,271,3000);
   Text(28,105,200,25,"<B>1. Choose a role</B>");
   for(int i=0;i<5;i++)FlatButton(28,145+i*42,200,10+i,Names[i]+(i==(int)c.Role?" [active]":i==_selected?" [selected]":""));
   Text(266,105,380,25,"<B>"+Names[_selected]+"</B>");
   Text(266,138,380,25,Summaries[_selected]);
   Text(266,177,380,140,Details[_selected]);
   if(_selected==(int)c.Role)Text(266,326,380,24,"This role is active.");
   else FlatButton(266,326,380,1,"Use "+Names[_selected]+" role");
   Text(24,369,630,24,"Change roles nearby, outside combat. Equipment and trained skills are kept.");
   FlatButton(24,398,110,0,"Back");
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
