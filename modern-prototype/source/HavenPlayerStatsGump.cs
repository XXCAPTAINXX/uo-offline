using System;
using System.Linq;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenPlayerStatsGump:HavenMenuGump {
  readonly int _page;
  public static void Initialize(){CommandSystem.Register("mystats",AccessLevel.Player,e=>{if(HavenPreview.Enabled){e.Mobile.CloseGump(typeof(HavenPlayerStatsGump));e.Mobile.SendGump(new HavenPlayerStatsGump(e.Mobile));}});}
  public HavenPlayerStatsGump(Mobile p,int page=0):base(30,30){
   var skills=p.Skills.Cast<Skill>().OrderByDescending(s=>s.Base).ThenBy(s=>s.Name).ToArray();const int rows=23;_page=Math.Max(0,Math.Min(page,(skills.Length-1)/rows));
   AddBackground(0,0,740,725,3000);AddLabel(24,22,0,p.Name+" - character stats");
   AddLabel(24,53,0,"Str "+p.Str+"   Dex "+p.Dex+"   Int "+p.Int+"   |   Raw stat total "+(p.RawStr+p.RawDex+p.RawInt)+" / "+p.StatCap);
   Vital(24,90,"Health",p.Hits,p.HitsMax);Vital(263,90,"Stamina",p.Stam,p.StamMax);Vital(502,90,"Mana",p.Mana,p.ManaMax);
   AddBackground(18,152,294,514,0xBB8);AddBackground(324,152,398,514,0xBB8);
   AddLabel(30,164,0,"DEFENSE & RECOVERY");
   string[] elements={"Physical","Fire","Cold","Poison","Energy"};int[] values={p.PhysicalResistance,p.FireResistance,p.ColdResistance,p.PoisonResistance,p.EnergyResistance};
   for(int i=0;i<5;i++){AddLabel(30,195+i*23,0,elements[i]+" resistance");AddLabel(234,195+i*23,0,values[i]+"%");}
   AddLabel(30,319,0,"Regen: Hits "+((int)RegenRates.HitPointRegen(p))+" / Stam "+((int)RegenRates.StamRegen(p))+" / Mana "+((int)RegenRates.ManaRegen(p)));
   AddLabel(30,353,0,"EQUIPMENT BONUSES");
   string[] labels={"Weapon damage","Spell damage","Hit chance","Defense chance","Swing speed","Lower mana cost","Lower reagent cost"};AosAttribute[] attrs={AosAttribute.WeaponDamage,AosAttribute.SpellDamage,AosAttribute.AttackChance,AosAttribute.DefendChance,AosAttribute.WeaponSpeed,AosAttribute.LowerManaCost,AosAttribute.LowerRegCost};
   for(int i=0;i<labels.Length;i++){AddLabel(30,382+i*23,0,labels[i]);AddLabel(234,382+i*23,0,AosAttributes.GetValue(p,attrs[i])+"%");}
   AddLabel(30,548,0,"Casting: FC "+AosAttributes.GetValue(p,AosAttribute.CastSpeed)+" / FCR "+AosAttributes.GetValue(p,AosAttribute.CastRecovery));
   AddLabel(30,574,0,"Luck "+p.Luck+"   Followers "+p.Followers+" / "+p.FollowersMax);
   AddHtml(30,604,266,45,"Equipment totals shown above; combat caps and situational effects still apply.",false,false);
   AddLabel(338,164,0,"SKILLS - highest first");AddLabel(508,164,0,"Base");AddLabel(575,164,0,"Now");AddLabel(645,164,0,"Cap");
   for(int row=0;row<rows;row++){int i=_page*rows+row;if(i>=skills.Length)break;var skill=skills[i];int y=194+row*19;AddLabel(338,y,0,skill.Name+(HavenPlayerCaps.IsFree(p,skill)?" *":""));AddLabel(508,y,0,skill.Base.ToString("F1"));AddLabel(575,y,0,skill.Value.ToString("F1"));AddLabel(645,y,0,skill.Cap.ToString("F1"));}
   AddLabel(338,635,0,"* Free skill | Counted: "+(HavenPlayerCaps.Counted(p.Skills)/10.0).ToString("F1")+" / "+(p.Skills.Cap/10.0).ToString("F1"));
   FlatButton(24,685,120,1,"Refresh");if(_page>0)FlatButton(168,685,110,2,"Previous");AddLabel(300,685,0,"Skills "+(_page+1)+" / "+((skills.Length+rows-1)/rows));if((_page+1)*rows<skills.Length)FlatButton(446,685,110,3,"Next");FlatButton(590,685,120,0,"Close");
  }
  void Vital(int x,int y,string name,int value,int max){AddLabel(x,y,0,name+"  "+value+" / "+max);AddImageTiled(x,y+27,210,9,9750);if(max>0&&value>0)AddImageTiled(x,y+27,Math.Min(210,(int)(210L*value/max)),9,9752);}
  public override void OnResponse(NetState s,RelayInfo i){if(HavenPreview.Enabled&&i.ButtonID>=1&&i.ButtonID<=3)s.Mobile.SendGump(new HavenPlayerStatsGump(s.Mobile,_page+(i.ButtonID==2?-1:i.ButtonID==3?1:0)));}
 }
}
