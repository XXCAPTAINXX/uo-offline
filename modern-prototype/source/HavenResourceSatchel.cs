using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Items;
using Server.Gumps;
using Server.Network;
using Server.Targeting;
namespace Server.HavenPrototype {
 public class HavenResourceSatchel:Bag {
  static readonly HashSet<string> Families=new HashSet<string>{"BaseOre","BaseIngot","BaseLog","BaseWoodBoard","BaseGranite","BaseHides","BaseLeather","BaseScales","BaseFish","BaseHighseasFish","BaseMagicFish"};
  static readonly HashSet<string> Materials=new HashSet<string>{"Sand","Saltpeter","SmallPieceofBlackrock","CrystallineBlackrock","BlueDiamond","DarkSapphire","EcruCitrine","FireRuby","PerfectEmerald","Turquoise","BrilliantAmber","BarkFragment","LuminescentFungi","SwitchItem","ParasiticPlant","CrystalShards","Fish","BigFish","RawFishSteak","FishSteak","WhitePearl","DelicateScales","Bait","Feather","Wool","TaintedWool","RawRibs","RawBird","RawLambLeg","RawRotwormMeat","DragonBlood","Fur","Bone","DaemonBone","MessageInABottle","TreasureMap","SpecialFishingNet","FabledFishingNet","WhiteMIB","ShoalOfFish","CrackedLavaRockEast","CrackedLavaRockSouth","StonePaver"};
  public HavenResourceSatchel(){Name="gatherer's resource pouch";Hue=0x59B;Weight=1;MaxItems=500;LootType=LootType.Blessed;}
  public HavenResourceSatchel(Serial serial):base(serial){}
  public static bool Accepts(Item item){if(item==null||item.Deleted||item is Container)return false;return AcceptsType(item.GetType());}
  public static bool AcceptsType(Type type){if(typeof(Container).IsAssignableFrom(type))return false;if(HavenResources.Types.Any(t=>t.IsAssignableFrom(type)))return true;for(Type t=type;t!=null&&t!=typeof(Item);t=t.BaseType)if(Families.Contains(t.Name)||Materials.Contains(t.Name))return true;return false;}
  public bool CanUse(Mobile p){return HavenMarks.CanUse(p)&&HavenResources.Accessible(p,this);}
  public override int DefaultMaxWeight{get{return 0;}}
  public override int GetTotal(TotalType type){int raw=base.GetTotal(type);return type==TotalType.Weight?(int)Math.Ceiling(raw/10.0):raw;}
  public override void UpdateTotal(Item sender,TotalType type,int delta){base.UpdateTotal(sender,type,delta);if(type==TotalType.Weight&&RootParent is Mobile)((Mobile)RootParent).UpdateTotals();}
  public override bool CheckHold(Mobile p,Item item,bool message,bool checkItems,int plusItems,int plusWeight){if(!Accepts(item)){if(message)p.SendMessage("This pouch holds gathering resources and catches. Use Collect to sort a bag.");return false;}int raw=item.TotalWeight+item.PileWeight;return base.CheckHold(p,item,message,checkItems,plusItems,plusWeight+(int)Math.Ceiling(raw/10.0)-raw);}
  public int Collect(Mobile p,Item source){if(!CanUse(p)||source==null||source.Deleted||source==this||source.IsChildOf(this)||!HavenResources.Accessible(p,source))return 0;var bag=source as Container;var items=bag==null?new[]{source}:bag.FindItemsByType(typeof(Item),true);int moved=0;foreach(var item in items){if(item==this||item.IsChildOf(this)||!Accepts(item)||!HavenResources.Accessible(p,item))continue;if(TryDropItem(p,item,false))moved++;}return moved;}
  public override void OnDoubleClick(Mobile p){if(CanUse(p))p.SendGump(new HavenResourceSatchelGump(this));}
  public void OpenContents(Mobile p){if(CanUse(p))DisplayTo(p);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("90% resource weight reduction; raw items remain usable");list.Add("Mining, chopping, fishing, skinning and gems");list.Add("Double-click: contents, collect, and accepted materials");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenResourceSatchelGump:Gump {
  readonly HavenResourceSatchel _bag;
  public HavenResourceSatchelGump(HavenResourceSatchel bag):base(50,50){_bag=bag;AddBackground(0,0,530,345,0xA28);AddHtml(24,22,480,30,"<BASEFONT COLOR=#342B23><B>Gatherer's resource pouch</B></BASEFONT>",false,false);AddHtml(24,60,480,160,"<BASEFONT COLOR=#342B23>Mining: ores, ingots, granite, sand, saltpeter, gems and rare minerals.<BR>Chopping: logs, boards, bark, fungi, plants, amber and crystal shards.<BR>Fishing: fish, crabs, lobsters, scales, pearls, bait, maps, bottles and nets.<BR>Skinning: hides, leather, scales, feathers, wool, meat, fur and bones.<BR><BR>90% less resource weight. Items stay raw. Collect checks your or your companion's pack and nested bags; equipment stays outside.</BASEFONT>",false,true);Button(24,235,1,"Open contents");Button(270,235,2,"Collect item or bag");Button(24,280,3,"Collect carrier pack");Button(390,300,0,"Close");}
  void Button(int x,int y,int id,string text){AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);AddHtml(x+33,y,210,25,"<BASEFONT COLOR=#342B23>"+text+"</BASEFONT>",false,false);}
  public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(!_bag.CanUse(p)||info.ButtonID==0)return;if(info.ButtonID==1)_bag.OpenContents(p);else if(info.ButtonID==2){p.Target=new GatherTarget(_bag);p.SendMessage("Target a resource or bag in your or your companion's pack.");}else if(info.ButtonID==3){p.SendMessage("Collected "+_bag.Collect(p,(_bag.RootParent as Mobile).Backpack)+" resource stacks/items.");p.SendGump(new HavenResourceSatchelGump(_bag));}}
  class GatherTarget:Target {readonly HavenResourceSatchel _bag;public GatherTarget(HavenResourceSatchel bag):base(12,false,TargetFlags.None){_bag=bag;}protected override void OnTarget(Mobile p,object target){p.SendMessage("Collected "+_bag.Collect(p,target as Item)+" resource stacks/items.");}}
 }
}
