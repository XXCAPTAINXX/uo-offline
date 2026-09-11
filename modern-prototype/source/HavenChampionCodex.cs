using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Accounting;
using Server.Commands;
using Server.Items;
using Server.Gumps;
using Server.Network;
using Server.Targeting;
using Server.Spells.SkillMasteries;
namespace Server.HavenPrototype {
 public class HavenChampionCodex:Bag {
  public override int DefaultMaxItems{get{return 0;}}
  public override int GetTotal(TotalType type){return type==TotalType.Items?0:base.GetTotal(type);}
  public override void UpdateTotal(Item sender,TotalType type,int delta){if(type!=TotalType.Items)base.UpdateTotal(sender,type,delta);}
  public HavenChampionCodex(){ItemID=0x2259;Hue=0x489;Weight=1;Name="Champion's Codex";LootType=LootType.Blessed;}
  public HavenChampionCodex(Serial serial):base(serial){}
  public bool CanUse(Mobile p){return p!=null&&p.Backpack!=null&&IsChildOf(p.Backpack)&&HavenResources.Accessible(p,this);}
  public static bool Accepts(Item item){return item!=null&&!item.Deleted&&!(item is Container)&&(item is PowerScroll||item is StatCapScroll||item is ScrollOfAlacrity||item is ScrollOfTranscendence||item is ChampionSkull||item.GetType().Name=="SkillMasteryPrimer"||item.GetType().Name=="ScrollBinderDeed");}
  public override bool CheckHold(Mobile p,Item item,bool message,bool checkItems,int plusItems,int plusWeight){if(!Accepts(item))return false;if(checkItems&&MaxItems>0&&Items.Count+plusItems+(item.Parent==this?0:1)>MaxItems)return false;return base.CheckHold(p,item,message,false,plusItems,plusWeight);}
  public override bool OnDragDrop(Mobile p,Item item){return CanUse(p)&&base.OnDragDrop(p,item);}
  public override bool OnDragDropInto(Mobile p,Item item,Point3D loc){return CanUse(p)&&base.OnDragDropInto(p,item,loc);}
  public int Collect(Mobile p,Item source){if(!CanUse(p)||source==null||source==this||!HavenResources.Accessible(p,source)||source.IsChildOf(this))return 0;int count=0;var bag=source as Container;if(bag!=null){foreach(var child in bag.Items.ToArray())if(!(child is HavenChampionCodex))count+=Collect(p,child);}else if(Accepts(source)&&TryDropItem(p,source,false))count++;return count;}
  public bool Withdraw(Mobile p,Item item){return CanUse(p)&&item!=null&&!item.Deleted&&item.Parent==this&&p.Backpack.TryDropItem(p,item,false);}
  public static int Cost(int tier){return tier==105?8:tier==110?12:tier==115?10:0;}
  public bool Convert(Mobile p,SkillName skill,int tier,bool split){if(!CanUse(p))return false;int cost=Cost(split?tier-5:tier);if(cost==0)return false;var input=Items.OfType<PowerScroll>().Where(x=>!x.Deleted&&x.Skill==skill&&x.Value==tier).Take(split?1:cost).ToArray();if(input.Length!=(split?1:cost))return false;var outputs=new List<PowerScroll>();for(int i=0;i<(split?cost:1);i++){var output=new PowerScroll(skill,tier+(split?-5:5));if(!CheckHold(p,output,false,true,outputs.Count-input.Length,outputs.Sum(x=>x.PileWeight+x.TotalWeight)-input.Sum(x=>x.PileWeight+x.TotalWeight))){output.Delete();foreach(var added in outputs)added.Delete();return false;}outputs.Add(output);}foreach(var item in input)item.Delete();foreach(var item in outputs)DropItem(item);return true;}
  public override void OnDoubleClick(Mobile p){if(CanUse(p)){Collect(p,p.Backpack);Show(p);}}
  public void Show(Mobile p,int page=0){if(CanUse(p)){p.CloseGump(typeof(HavenCodexGump));p.SendGump(new HavenCodexGump(p,this,page));}}
  public static void Initialize(){CommandSystem.Register("codex",AccessLevel.Player,e=>OpenCodex(e.Mobile));}
  public static void OpenCodex(Mobile p){if(!HavenMarks.CanUse(p)||p.Backpack==null)return;var codex=p.Backpack.FindItemByType(typeof(HavenChampionCodex),true) as HavenChampionCodex;var account=p.Account as Account;if(codex==null&&account!=null){string key="Haven.Codex:"+p.Serial.Value;if(account.GetTag(key)!=null){p.SendMessage("Your free Codex was already claimed. Find it or buy a replacement at the Arcane stone.");return;}codex=new HavenChampionCodex();if(!p.Backpack.TryDropItem(p,codex,false)){codex.Delete();return;}account.SetTag(key,"claimed");}if(codex!=null)codex.OnDoubleClick(p);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Archives scrolls, champion skulls, mastery primers and binders");list.Add("Contents use no backpack slots; weight still applies");list.Add("Collect nested bags; withdraw, combine or split scrolls");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenCodexGump:HavenMenuGump {
  readonly HavenChampionCodex _book;readonly int _page;readonly Dictionary<int,Item> _rows=new Dictionary<int,Item>();
  public HavenCodexGump(Mobile p,HavenChampionCodex book,int page):base(40,40){_book=book;var groups=book.Items.Where(x=>!x.Deleted).GroupBy(Key).OrderBy(x=>x.Key).ToArray();int pages=Math.Max(1,(groups.Length+11)/12);_page=Math.Max(0,Math.Min(pages-1,page));AddBackground(0,0,700,495,0xA28);AddLabel(24,20,0,"Champion's Codex");AddHtml(24,52,650,42,"<BASEFONT COLOR=#342B23>Actual scrolls, sorted by skill and tier. Hover arrows for properties. Each click withdraws one item.</BASEFONT>",false,false);AddLabel(24,96,0,"Withdraw / item");AddLabel(410,96,0,"Count");AddLabel(480,96,0,"Combine / split");for(int row=0;row<12&&_page*12+row<groups.Length;row++){var group=groups[_page*12+row];var item=group.First();_rows[row]=item;int y=126+row*23;HavenMenuGump.ItemArrow(this,p,item,24,y,100+row);AddLabelCropped(57,y,345,23,0,group.Key);AddLabel(410,y,0,group.Count().ToString());var power=item as PowerScroll;if(power!=null){if(HavenChampionCodex.Cost((int)power.Value)>0){Button(480,y,200+row,"Up");}if(HavenChampionCodex.Cost((int)power.Value-5)>0){Button(570,y,300+row,"Split");}}}if(groups.Length==0)AddLabel(24,126,0,"No progression items stored yet.");AddLabel(24,408,0,"Combine: 8 x 105 -> 110; 12 x 110 -> 115; 10 x 115 -> 120. Split reverses it.");Button(24,438,1,"Collect pack");Button(175,438,2,"Target bag/item");Button(350,438,3,"Previous");Button(475,438,4,"Next");Button(590,438,0,"Close");AddLabel(24,468,0,"Page "+(_page+1)+" / "+pages);}
  public static string Key(Item item){var primer=item as SkillMasteryPrimer;if(primer!=null)return SkillInfo.Table[(int)primer.Skill].Name+" - Mastery primer "+primer.Volume;if(item is StatCapScroll)return "Stat cap - "+((StatCapScroll)item).Value;var scroll=(item is PowerScroll||item is ScrollOfAlacrity||item is ScrollOfTranscendence)?item as SpecialScroll:null;return scroll!=null?SkillInfo.Table[(int)scroll.Skill].Name+" - "+(item is PowerScroll?"Power":item is ScrollOfAlacrity?"Alacrity":"Transcendence")+" "+scroll.Value.ToString("0.0"):item.Name??item.GetType().Name;}
  void Button(int x,int y,int id,string label){AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);AddLabel(x+33,y,0,label);}
  public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(info.ButtonID==0||!_book.CanUse(p))return;int page=_page;if(info.ButtonID==1)_book.Collect(p,p.Backpack);else if(info.ButtonID==2){p.Target=new CollectTarget(_book);return;}else if(info.ButtonID==3)page--;else if(info.ButtonID==4)page++;else{int row=info.ButtonID%100;Item item;if(_rows.TryGetValue(row,out item)&&!item.Deleted&&item.Parent==_book){bool ok=info.ButtonID<200?_book.Withdraw(p,item):item is PowerScroll&&_book.Convert(p,((PowerScroll)item).Skill,(int)((PowerScroll)item).Value,info.ButtonID>=300);if(!ok)p.SendMessage("Not enough matching scrolls or storage capacity. Nothing changed.");}}_book.Show(p,page);}
  class CollectTarget:Target{readonly HavenChampionCodex _book;public CollectTarget(HavenChampionCodex b):base(12,false,TargetFlags.None){_book=b;}protected override void OnTarget(Mobile p,object obj){p.SendMessage("Collected "+_book.Collect(p,obj as Item)+" item(s).");_book.Show(p);}}
 }
}
