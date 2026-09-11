using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class SupplyShopSmoke {
 static void Require(bool ok){if(!ok)throw new Exception("Supply shop regression");}
 public static void Run(Action<string,Action> check,bool reload){
  check("companion native skill gain and paginated stats are available",()=>{var c=new HavenCompanion();c.MoveToWorld(new Point3D(3500,2575,Map.Trammel.GetAverageZ(3500,2575)),Map.Trammel);double before=c.Skills.Swords.Base;Server.Misc.SkillCheck.Gain(c,c.Skills.Swords,1);Require(c.Skills.Swords.Base>before);for(int page=0;page<(c.Skills.Length+11)/12;page++)new HavenCompanionStatsGump(c,page);c.Delete();});
  if(reload){check("shop purchases keep native properties and bank charge after reload",()=>{var restored=World.Mobiles.Values.OfType<PlayerMobile>().Single(x=>x.Name=="Shop fixture");Require(restored.Backpack.FindItemByType(typeof(PowerScroll))!=null&&restored.Backpack.FindItemByType(typeof(BraceletOfTheVanguard))!=null&&Banker.GetBalance(restored)==7500&&HavenMarks.Balance(restored)==0);});return;}
  var p=new PlayerMobile{Player=true,Name="Shop fixture",Body=0x190,RawStr=100};p.AddItem(new Backpack());new Account("shop-fixture",Guid.NewGuid().ToString("N"))[0]=p;var sign=World.Items.Values.OfType<HavenServiceStone>().Single(x=>x.Service==0);p.MoveToWorld(sign.Location,sign.Map);
  check("all shop entries create real items and catalog prices are valid",()=>{foreach(var catalog in HavenSupplyShops.Catalogs)foreach(var e in catalog){var item=e.Create();Require(item!=null&&!item.Deleted&&(e.Gold>0||e.Marks>0));item.Delete();}Require(HavenSupplyShops.Catalogs[0].Count==22&&HavenSupplyShops.Catalogs[1].Count>100&&HavenSupplyShops.Catalogs[2].Count==15);});
  Banker.Deposit(p,10000);HavenMarks.Award(p,15);
  check("shops deny remote invalid and full-pack purchases without charging",()=>{p.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);Require(!HavenSupplyShops.Buy(p,0,0));p.MoveToWorld(sign.Location,sign.Map);Require(!HavenSupplyShops.Buy(p,-1,0)&&!HavenSupplyShops.Buy(p,0,999));p.Backpack.MaxItems=1;p.Backpack.DropItem(new Dagger());Require(!HavenSupplyShops.Buy(p,0,0)&&Banker.GetBalance(p)==10000);foreach(var i in p.Backpack.Items.ToArray())i.Delete();p.Backpack.MaxItems=125;});
  check("standard scroll and original bracelet charge correct currencies",()=>{Require(HavenSupplyShops.Buy(p,1,0)&&Banker.GetBalance(p)==7500);var scroll=(PowerScroll)p.Backpack.FindItemByType(typeof(PowerScroll));Require(scroll.Value==105&&scroll.Skill==SkillName.Swords);Require(HavenSupplyShops.Buy(p,2,6)&&HavenMarks.Balance(p)==0&&Banker.GetBalance(p)==7500);var b=(BraceletOfTheVanguard)p.Backpack.FindItemByType(typeof(BraceletOfTheVanguard));Require(b.Attributes.BonusStr==5&&b.Attributes.WeaponDamage==10);});
  check("mini champion gives five power scrolls and both training scrolls",()=>{var prize=new HavenMiniPrize(p,1);Require(prize.Items.OfType<PowerScroll>().Count()==5&&prize.Items.OfType<PowerScroll>().All(x=>x.Value==105||x.Value==110)&&prize.Items.OfType<ScrollOfAlacrity>().Count()==1&&prize.Items.OfType<ScrollOfTranscendence>().Count()==1);p.Backpack.MaxItems=p.Backpack.TotalItems;Require(!prize.Deliver(p)&&prize.Items.OfType<PowerScroll>().Count()==5);p.Backpack.MaxItems=125;Require(prize.Deliver(p)&&p.Backpack.Items.OfType<PowerScroll>().Count()==6);Require(!prize.Deliver(p));});
 }
}
