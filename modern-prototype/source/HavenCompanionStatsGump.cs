using System;
using Server;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenCompanionStatsGump:Gump {
  readonly HavenCompanion _c;readonly int _page;readonly double[] _baseline;
  public HavenCompanionStatsGump(HavenCompanion c,int page=0,double[] baseline=null):base(45,45){_c=c;_page=Math.Max(0,Math.Min((c.Skills.Length-1)/12,page));_baseline=baseline??new double[c.Skills.Length];if(baseline==null)for(int i=0;i<c.Skills.Length;i++)_baseline[i]=c.Skills[i].Base;
   AddBackground(0,0,610,540,0x13BE);AddImageTiled(12,12,586,516,2624);Text(24,22,555,25,"<B>"+System.Security.SecurityElement.Escape(c.Name)+" - stats and skills</B>");
   Text(24,56,555,25,"Hits "+c.Hits+"/"+c.HitsMax+" | Mana "+c.Mana+"/"+c.ManaMax+" | Stamina "+c.Stam+"/"+c.StamMax);
   Text(24,85,555,25,"Str "+c.Str+" | Dex "+c.Dex+" | Int "+c.Int+" | "+c.Role+" | "+c.ControlOrder);
   Text(24,119,555,40,"Base = trained; Now = with bonuses. Gain = since opening this screen.<BR>Refresh to see progress. Skills at cap cannot gain further.");
   Text(24,169,190,24,"<B>Skill</B>");Text(220,169,75,24,"<B>Base</B>");Text(302,169,75,24,"<B>Now</B>");Text(384,169,75,24,"<B>Cap</B>");Text(466,169,105,24,"<B>Gain</B>");
   for(int row=0;row<12;row++){int i=_page*12+row;if(i>=c.Skills.Length)break;var sk=c.Skills[i];int y=200+row*23;Text(24,y,190,23,sk.Name);Text(220,y,75,23,sk.Base.ToString("F1"));Text(302,y,75,23,sk.Value.ToString("F1"));Text(384,y,75,23,sk.Cap.ToString("F1"));Text(466,y,105,23,(sk.Base-_baseline[i]).ToString("+0.0;-0.0;0.0"));}
   Button(24,493,1,"Refresh",85);if(_page>0)Button(145,493,2,"Prev",65);Text(245,493,95,25,(_page+1)+" / "+((c.Skills.Length+11)/12));if((_page+1)*12<c.Skills.Length)Button(337,493,3,"Next",70);Button(489,493,0,"Close",70);
  }
  void Text(int x,int y,int w,int h,string text){AddHtml(x,y,w,h,"<BASEFONT COLOR=#FFFFFF>"+text+"</BASEFONT>",false,false);}
  void Button(int x,int y,int id,string text,int w){AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);Text(x+33,y,w,24,text);}
  public override void OnResponse(NetState state,RelayInfo info){if(info.ButtonID==0||_c.Deleted||!_c.IsOwner(state.Mobile))return;state.Mobile.SendGump(new HavenCompanionStatsGump(_c,info.ButtonID==2?_page-1:info.ButtonID==3?_page+1:_page,_baseline));}
 }
}
