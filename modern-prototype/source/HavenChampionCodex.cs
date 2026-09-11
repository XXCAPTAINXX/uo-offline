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
}
