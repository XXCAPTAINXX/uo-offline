using System;
using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Spells.SkillMasteries;
namespace Server.HavenPrototype {
 public sealed class HavenShopEntry {
  public string Name,Details,FreeClaim; public int Gold,Marks,Shards; public Func<Item> Create;
  public HavenShopEntry(string n,int gold,Func<Item> create,string details,int marks=0,int shards=0){Name=n;Gold=gold;Create=create;Details=details;Marks=marks;Shards=shards;}
 }
 public static class HavenSupplyShops {
  public static readonly List<HavenShopEntry>[] Catalogs={Arcane(),Training(),Rewards()};
  public static readonly string[] Names={"Arcane supplies","Training supplies","Special rewards"};
  public static void Initialize(){CommandSystem.Register("arcane",AccessLevel.Player,e=>Show(e.Mobile,0));CommandSystem.Register("training",AccessLevel.Player,e=>Show(e.Mobile,1));CommandSystem.Register("rewards",AccessLevel.Player,e=>Show(e.Mobile,2));}
  public static void Show(Mobile p,int kind){if(HavenMarks.CanUse(p))p.SendGump(new HavenSupplyShopGump(p,kind,0));}
  public static bool Near(Mobile p){return HavenMarks.CanUse(p)&&World.Items.Values.OfType<HavenServiceStone>().Any(s=>(s.Service==0||s.Service==1||s.Service==9||s.Service==10)&&HavenStarterHub.CanUse(p,s));}
  public static bool Buy(Mobile p,int kind,int index,Item preview=null){if(kind<0||kind>=Catalogs.Length||index<0||index>=Catalogs[kind].Count||!Near(p)||p.Backpack==null)return false;var entry=Catalogs[kind][index];var item=preview??entry.Create();if(item.Deleted)return false;if(!p.Backpack.CheckHold(p,item,false)){item.Delete();p.SendMessage("Make room in your backpack. Nothing was charged.");return false;}var account=p.Account as Server.Accounting.Account;string claim=entry.FreeClaim==null?null:entry.FreeClaim+":"+p.Serial.Value;bool free=claim!=null&&account!=null&&account.GetTag(claim)==null;var wallet=HavenWallet.Find(p);bool paid=free||(entry.Shards>0?wallet!=null&&wallet.SpendShards(p,entry.Shards):entry.Marks>0&&HavenMarks.Balance(p)>=entry.Marks?HavenMarks.Spend(p,entry.Marks):entry.Gold>0&&HavenWallet.PayGold(p,entry.Gold));if(!paid){item.Delete();p.SendMessage("You do not have enough of the required currency.");return false;}p.Backpack.DropItem(item);HavenAdvancedGear.Attach(item,HavenAdvancedGear.AutoKind(item));if(free)account.SetTag(claim,"claimed");p.SendMessage("Purchased "+entry.Name+".");return true;}
  static Item Full(Spellbook book){book.Content=book.BookCount==64?ulong.MaxValue:(1UL<<book.BookCount)-1;return book;}
  static Bag Reagents(){var bag=new Bag{Name="100 of each magery reagent"};foreach(var item in new Item[]{new BlackPearl(100),new Bloodmoss(100),new Garlic(100),new Ginseng(100),new MandrakeRoot(100),new Nightshade(100),new SulfurousAsh(100),new SpidersSilk(100)})bag.DropItem(item);return bag;}
  static Bag Potions(){var bag=new Bag{Name="10 heal, cure and refresh potions"};for(int i=0;i<10;i++){bag.DropItem(new GreaterHealPotion());bag.DropItem(new GreaterCurePotion());bag.DropItem(new TotalRefreshPotion());}return bag;}
  static List<HavenShopEntry> Arcane(){return new List<HavenShopEntry>{
   new HavenShopEntry("Full Magery spellbook",500,()=>Full(new Spellbook()),"All 64 Magery spells. Normal casting requirements apply."),
   new HavenShopEntry("Full Necromancy book",1500,()=>Full(new NecromancerSpellbook()),"All native Necromancy spells."),
   new HavenShopEntry("Full Chivalry book",1000,()=>Full(new BookOfChivalry()),"All native Chivalry abilities. Requires tithing points."),
   new HavenShopEntry("Full Spellweaving book",2000,()=>Full(new SpellweavingBook()),"All native Spellweaving spells. Normal quest requirements apply."),
   new HavenShopEntry("Full Bushido book",1000,()=>Full(new BookOfBushido()),"All native Bushido abilities."),
   new HavenShopEntry("Full Ninjitsu book",1000,()=>Full(new BookOfNinjitsu()),"All native Ninjitsu abilities."),
   new HavenShopEntry("Runebook",500,()=>new Runebook(),"Holds marked recall runes."),
   new HavenShopEntry("Runic atlas",5000,()=>new RunicAtlas(),"Native atlas holds 48 marked locations."),
   new HavenShopEntry("Bag of sending: 30 charges",2500,()=>new BagOfSending{Charges=30},"Sends eligible items to your bank using charges."),
   new HavenShopEntry("Translocation powder: 10",1000,()=>new PowderOfTranslocation(10),"Recharges a bag of sending."),
   new HavenShopEntry("Fortification powder: 10 uses",5000,()=>new PowderOfTemperament(10),"Restores maximum durability on eligible equipment under native limits."),
   new HavenShopEntry("Clothing bless deed",10000,()=>new ClothingBlessDeed(),"Blesses an eligible clothing item."),
   new HavenShopEntry("Blank recall rune",50,()=>new RecallRune(),"Cast Mark on it to save a location."),
   new HavenShopEntry("Reagents: 100 each",3000,()=>Reagents(),"100 each of the eight Magery reagents."),
   new HavenShopEntry("Bandages: 500",500,()=>new Bandage(500),"Healing and Veterinary bandages."),
   new HavenShopEntry("Arrows: 500",1000,()=>new Arrow(500),"Bow ammunition."),
   new HavenShopEntry("Bolts: 500",1000,()=>new Bolt(500),"Crossbow ammunition."),
   new HavenShopEntry("Recall scrolls: 50",2000,()=>new RecallScroll(50),"A stack of native Recall scrolls."),
   new HavenShopEntry("Potion bundle",1500,()=>Potions(),"10 Greater Heal, 10 Greater Cure and 10 Total Refresh potions."),
   new HavenShopEntry("Resource Ledger",1000,()=>new HavenResourceLedger(),"Combines supported resource deeds into balances."),
   new HavenShopEntry("Fabled fishing net",25000,()=>new FabledFishingNet(),"Native white fishing net. Use in deep water; may attract dangerous sea creatures."),
   new HavenShopEntry("Gatherer's resource pouch",15000,()=>new HavenResourceSatchel(),"Keeps mining, lumber, fishing and skinning resources as real items with 90% less weight. Collect from items or nested bags. Includes gems and rare harvesting materials."),
   new HavenShopEntry("Champion's Codex",1000,()=>new HavenChampionCodex(),"One free with [codex; replacement copy. Stores actual power/stat/skill scrolls, skulls, primers and binders. Combines and splits power scrolls."),
   new HavenShopEntry("World map",500,()=>new WorldMap(),"Native world map.")};}
  static List<HavenShopEntry> Training(){var list=new List<HavenShopEntry>();var skills=new[]{SkillName.Swords,SkillName.Fencing,SkillName.Macing,SkillName.Archery,SkillName.Wrestling,SkillName.Parry,SkillName.Tactics,SkillName.Anatomy,SkillName.Healing,SkillName.MagicResist,SkillName.Magery,SkillName.EvalInt,SkillName.Meditation,SkillName.Focus,SkillName.Spellweaving,SkillName.Necromancy,SkillName.SpiritSpeak,SkillName.Chivalry,SkillName.Musicianship,SkillName.Discordance,SkillName.Peacemaking,SkillName.Provocation,SkillName.AnimalTaming,SkillName.AnimalLore,SkillName.Veterinary,SkillName.Blacksmith,SkillName.Tailoring,SkillName.Carpentry,SkillName.Alchemy,SkillName.Inscribe};foreach(var skill in skills)foreach(int cap in new[]{105,110}){var chosen=skill;int limit=cap;list.Add(new HavenShopEntry(skill+" cap "+cap,cap==105?2500:7500,()=>new PowerScroll(chosen,limit),"Standard Power Scroll: raises this skill's cap to "+cap+". Does not grant trained points. Native player/pet eligibility applies."));}list.Add(new HavenShopEntry("Book of Masteries",1000,()=>new BookOfMasteries(),"Native mastery book. Learn primers and meet the mastery skill requirements."));foreach(var skill in MasteryInfo.Skills)for(int volume=1;volume<=3;volume++){var chosen=skill;int level=volume;list.Add(new HavenShopEntry(skill+" primer "+level,level==1?2500:level==2?7500:15000,()=>new SkillMasteryPrimer(chosen,level),"Native "+chosen+" mastery primer, volume "+level+". Use from your pack to learn it; normal mastery requirements apply."));}return list;}
  static List<HavenShopEntry> Rewards(){var list=new List<HavenShopEntry>();for(int i=0;i<HavenMarks.Names.Length;i++){int index=i;list.Add(new HavenShopEntry(HavenMarks.Names[i],0,()=>HavenMarks.CreateReward(index),HavenMarks.Descriptions[i],HavenMarks.Prices[i]));}
   string[] names={"Vanguard bracelet","Arcane Focus bracelet","Wind bracelet","Beastmaster bracelet","Virtuoso bracelet","Artisan bracelet","Fortune bracelet","Guardian bracelet","Night bracelet"};
   string[] details={"+5 Strength, +5 Hits, +10% Damage Increase.","+5 Intelligence, +8 Mana, +5% Lower Mana Cost.","+5 Dexterity, +8 Stamina, +5% Hit Chance.","+5 Animal Taming, Animal Lore and Veterinary.","+5 Musicianship; +3 Discordance, Peacemaking and Provocation.","+5 Blacksmithing, Tailoring and Tinkering.","+150 Luck.","+10% Defense Chance, +2 Hit Regeneration, +3 all resists.","Night Sight, +2 Hit Regeneration, +10% Lower Reagent Cost, +5 Hits."};
   Func<Item>[] factories={()=>new BraceletOfTheVanguard(),()=>new BraceletOfArcaneFocus(),()=>new BraceletOfTheWind(),()=>new BraceletOfTheBeastmaster(),()=>new BraceletOfTheVirtuoso(),()=>new BraceletOfTheArtisan(),()=>new BraceletOfFortune(),()=>new BraceletOfTheGuardian(),()=>new BraceletOfTheNight()};for(int i=0;i<names.Length;i++)list.Add(new HavenShopEntry(names[i],25000,factories[i],details[i]+" Native attribute caps apply. Equip the matching Haven ring for its set bonus.",15));list.Add(new HavenShopEntry("Champion's pendant",0,()=>new HavenChampionPendant(),"Blessed. +1,000 Luck; +20 Str/Dex/Int; +5 all regeneration; +40% weapon/spell damage; +15% hit/defense chance; +10% swing speed; FC1/FCR3; LMC10%; LRC100%. Native caps apply.",250));for(int i=0;i<HavenJewelrySets.Names.Length;i++){int theme=i;list.Add(new HavenShopEntry(HavenJewelrySets.Names[i]+" ring",0,()=>new HavenSetRing(theme),"Matches its bracelet attributes. Set: "+HavenJewelrySets.Descriptions[i]+". With Concord: +250 Luck, +10% weapon/spell damage.",30));}list.Add(new HavenShopEntry("Concord talisman",0,()=>new HavenConcordTalisman(),"Blessed. 300 Luck; +10 stats; +3 regens; +20% weapon/spell damage; LMC5, LRC20. Matching ring/bracelet adds 250 Luck and 10% damage. Level20 unlocks follower capacity6.",150));list.Add(new HavenShopEntry("Astral weaver's ring",0,()=>new AstralWeaversRing(),"Evolves to level20. Spell damage30, LMC10, mana regen3; +15 Magery/Spellweaving. Paid only with wallet shards.",0,20));list.Add(new HavenShopEntry("Astral guardian's mantle",0,()=>new AstralGuardianMantle(),"Evolves to level20. Hit regen5, defense chance15, Luck300. Paid only with wallet shards.",0,40));list.Add(new HavenShopEntry("Astral fortune earrings",0,()=>new AstralFortuneEarrings(),"Evolves to level20. LRC100, Luck400, mana regen4. Paid only with wallet shards.",0,60));for(int tier=0;tier<3;tier++)for(int piece=0;piece<5;piece++){int t=tier,k=piece;list.Add(new HavenShopEntry(HavenShieldWarriorGear.Tiers[t]+" "+HavenShieldWarriorGear.Pieces[k],t==0?250:0,()=>HavenShieldWarriorGear.Create(t,k),"Evolving shield-warrior equipment. Inspect the arrow for full properties. Shield Bash requires learned, active Parrying mastery.",t==1?40:0,t==2?80:0){FreeClaim=t==0?"Haven.Recruit:"+k:null});}return list;}
 }
 public class HavenShopPreviewHolder:Bag {
  public HavenShopPreviewHolder(){Movable=false;Visible=false;Timer.DelayCall(TimeSpan.FromMinutes(10),Delete);}
  public HavenShopPreviewHolder(Serial serial):base(serial){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Timer.DelayCall(TimeSpan.Zero,Delete);}
 }
 public class HavenSupplyShopGump:Gump {
  readonly int _kind,_selected;const int PageSize=8;
  HavenShopPreviewHolder _holder; Item _preview; Dictionary<int,Item> _items;
  void Cleanup(){if(_holder!=null&&!_holder.Deleted)_holder.Delete();}
  public override void OnServerClose(NetState state){Cleanup();base.OnServerClose(state);}
  public HavenSupplyShopGump(Mobile p,int kind,int selected,HavenShopPreviewHolder holder=null,Dictionary<int,Item> items=null):base(40,40){_kind=kind;var entries=HavenSupplyShops.Catalogs[kind];_selected=Math.Max(0,Math.Min(entries.Count-1,selected));int page=_selected/PageSize;
   AddBackground(0,0,660,470,0xA28);Text(24,22,610,25,"<B>"+HavenSupplyShops.Names[kind]+"</B>");Text(24,54,610,25,"Bank gold: "+Server.Mobiles.Banker.GetBalance(p).ToString("N0")+" | Marks: "+HavenMarks.Balance(p));
   _holder=holder!=null&&!holder.Deleted?holder:new HavenShopPreviewHolder();_items=items??new Dictionary<int,Item>();
   for(int row=0;row<PageSize;row++){int index=page*PageSize+row;if(index>=entries.Count)break;Item item;if(!_items.TryGetValue(index,out item)||item.Deleted){item=entries[index].Create();if(HavenGearDurability.Supported(item))HavenGearDurability.Apply(item);_holder.DropItem(item);_items[index]=item;}HavenMenuGump.ItemArrow(this,p,item,24,94+row*33,100+index);Text(57,94+row*33,275,29,entries[index].Name);}
   var entry=entries[_selected];Text(345,96,285,40,"<B>"+entry.Name+"</B>");Text(345,140,285,40,entry.FreeClaim!=null?"Free first claim; 250 gold replacements":entry.Shards>0?entry.Shards+" Astral shards (wallet: "+(HavenWallet.Find(p)==null?0:HavenWallet.Find(p).AstralShards)+")":entry.Marks>0?entry.Marks+" Marks"+(entry.Gold>0?" or "+entry.Gold.ToString("N0")+" gold":""):entry.Gold.ToString("N0")+" gold");Text(345,267,285,120,entry.Details);
   _preview=_items[_selected];AddItem(370,185,_preview.ItemID,_preview.Hue);_preview.SendPropertiesTo(p);AddItemProperty(_preview.Serial);Text(425,190,195,60,"Hover an arrow or item to inspect its full properties.");HavenMenuGump.ItemArrow(this,p,_preview,345,394,1);Text(378,394,250,29,"Buy selected");
   if(page>0)Button(24,428,2,"Previous",100);Text(148,428,160,25,"Page "+(page+1)+" / "+((entries.Count+7)/8));if((page+1)*PageSize<entries.Count)Button(300,428,3,"Next",100);Button(550,428,0,"Close",65);
  }
  void Text(int x,int y,int w,int h,string text){AddHtml(x,y,w,h,"<BASEFONT COLOR=#342B23>"+text+"</BASEFONT>",false,false);}
  void Button(int x,int y,int id,string text,int width){AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);Text(x+33,y,width,29,text);}
  public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(info.ButtonID==0||!HavenMarks.CanUse(p)){Cleanup();return;}int selected=_selected;if(info.ButtonID==1){if(!HavenSupplyShops.Near(p))p.SendMessage("Stand beside a Haven shop stone to buy.");else HavenSupplyShops.Buy(p,_kind,_selected,_preview);}else if(info.ButtonID==2)selected=Math.Max(0,(_selected/8-1)*8);else if(info.ButtonID==3)selected=(_selected/8+1)*8;else if(info.ButtonID>=100)selected=info.ButtonID-100;if(selected>=0&&selected<HavenSupplyShops.Catalogs[_kind].Count){if(info.ButtonID>=100&&selected/8==_selected/8&&!_holder.Deleted){var holder=_holder;_holder=null;p.SendGump(new HavenSupplyShopGump(p,_kind,selected,holder,_items));}else{Cleanup();p.SendGump(new HavenSupplyShopGump(p,_kind,selected));}}else Cleanup();}
 }
}
