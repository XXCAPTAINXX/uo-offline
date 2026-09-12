using System;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenCompanionStatsGump:HavenMenuGump {
  readonly HavenCompanion _c;readonly int _page,_filter,_sort;readonly double[] _baseline;
  public static SkillName AttackSkill(HavenCompanion c){var weapon=c.Weapon as BaseWeapon;return weapon==null?SkillName.Wrestling:weapon.GetUsedSkill(c,true);}
  public static bool Used(HavenCompanion c,SkillName skill){
   if(skill==AttackSkill(c))return true;
   switch(skill){
    case SkillName.Swords:return c.Role==CompanionRole.Warrior;
    case SkillName.Parry:return c.FindItemOnLayer(Layer.TwoHanded) is BaseShield;
    case SkillName.Archery:return c.Role==CompanionRole.Archer;
    case SkillName.Musicianship:case SkillName.Peacemaking:case SkillName.Discordance:case SkillName.Provocation:return c.Role==CompanionRole.Bard;
    case SkillName.AnimalTaming:case SkillName.AnimalLore:case SkillName.Veterinary:return true;
    case SkillName.Spellweaving:case SkillName.Necromancy:case SkillName.SpiritSpeak:case SkillName.EvalInt:return c.Role==CompanionRole.Caster;
    case SkillName.Wrestling:case SkillName.Tactics:case SkillName.Anatomy:case SkillName.MagicResist:case SkillName.Magery:case SkillName.Meditation:case SkillName.Focus:case SkillName.Healing:case SkillName.Mining:case SkillName.Lumberjacking:return true;
    default:return false;
   }
  }

  public static int[] Rows(HavenCompanion c,int filter,int sort,double[] baseline){var rows=Enumerable.Range(0,c.Skills.Length).Where(i=>filter==2||Used(c,(SkillName)i)&&(filter==1||c.Skills[i].Base<c.Skills[i].Cap&&(c.Skills[i].Lock==SkillLock.Up||i==(int)SkillName.Mining||i==(int)SkillName.Lumberjacking||i==(int)SkillName.AnimalLore)));return (sort==1?rows.OrderByDescending(i=>c.Skills[i].Base).ThenBy(i=>c.Skills[i].Name):sort==2?rows.OrderByDescending(i=>c.Skills[i].Base-baseline[i]).ThenBy(i=>c.Skills[i].Name):rows.OrderBy(i=>c.Skills[i].Name)).ToArray();}
  public HavenCompanionStatsGump(HavenCompanion c,int page=0,double[] baseline=null,int filter=2,int sort=0):base(45,45){_c=c;_filter=filter;_sort=sort;_baseline=baseline??new double[c.Skills.Length];if(baseline==null)for(int i=0;i<c.Skills.Length;i++)_baseline[i]=c.Skills[i].Base;var rows=Rows(c,filter,sort,_baseline);int pages=Math.Max(1,(rows.Length+11)/12);_page=Math.Max(0,Math.Min(pages-1,page));
   AddBackground(0,0,610,580,0xA28);Text(24,22,555,25,"<B>"+System.Security.SecurityElement.Escape(c.Name)+" - stats and skills</B>");
   Text(24,56,555,25,"Hits "+c.Hits+"/"+c.HitsMax+" | Mana "+c.Mana+"/"+c.ManaMax+" | Stamina "+c.Stam+"/"+c.StamMax);
   Text(24,85,555,25,"Str "+c.Str+" | Dex "+c.Dex+" | Int "+c.Int+" | "+c.Role+" | "+c.ControlOrder);
   Text(24,111,555,24,"Current weapon skill: "+c.Skills[AttackSkill(c)].Name);
   Text(24,139,555,24,"Base = trained; Now = with bonuses; Gain = since opening.");
   Button(24,169,4,new[]{"Show: Trainable","Show: Used skills","Show: All skills"}[filter]+" ("+rows.Length+")",220);Button(320,169,5,new[]{"Sort: Name","Sort: Highest base","Sort: Biggest gain"}[sort],240);
   Text(24,207,190,24,"<B>Skill</B>");Text(220,207,75,24,"<B>Base</B>");Text(302,207,75,24,"<B>Now</B>");Text(384,207,75,24,"<B>Cap</B>");Text(466,207,105,24,"<B>Gain</B>");
   for(int row=0;row<12;row++){int index=_page*12+row;if(index>=rows.Length)break;int i=rows[index];var sk=c.Skills[i];int y=238+row*23;Text(24,y,190,23,sk.Name);Text(220,y,75,23,sk.Base.ToString("F1"));Text(302,y,75,23,sk.Value.ToString("F1"));Text(384,y,75,23,sk.Cap.ToString("F1"));Text(466,y,105,23,(sk.Base-_baseline[i]).ToString("+0.0;-0.0;0.0"));}
   if(rows.Length==0)Text(24,248,555,60,"No trainable skills below cap in this view. Switch to Used skills or All skills to inspect caps.");
   Button(24,533,1,"Refresh",85);if(_page>0)Button(145,533,2,"Prev",65);Text(245,533,95,25,(_page+1)+" / "+pages);if(_page+1<pages)Button(337,533,3,"Next",70);Button(489,533,0,"Close",70);
  }
  void Text(int x,int y,int w,int h,string text){AddHtml(x,y,w,h,"<BASEFONT COLOR=#342B23>"+text+"</BASEFONT>",false,false);}
  void Button(int x,int y,int id,string text,int w){FlatButton(x,y,w+15,id,text);}
  public override void OnResponse(NetState state,RelayInfo info){if(info.ButtonID==0||_c.Deleted||!_c.IsOwner(state.Mobile))return;state.Mobile.SendGump(new HavenCompanionStatsGump(_c,info.ButtonID==4||info.ButtonID==5?0:info.ButtonID==2?_page-1:info.ButtonID==3?_page+1:_page,_baseline,info.ButtonID==4?(_filter+1)%3:_filter,info.ButtonID==5?(_sort+1)%3:_sort));}
 }
}
