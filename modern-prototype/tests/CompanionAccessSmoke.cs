using System;
using Server;
using Server.Network;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class CompanionAccessSmoke {
 static void Require(bool ok){if(!ok)throw new Exception("Companion range regression");}
 public static void Run(Action<string,Action> check,bool reload){
  if(reload)return;
  var owner=new PlayerMobile {Player=true,Body=0x190,RawStr=100};owner.AddItem(new Backpack());new Account("range-fixture",Guid.NewGuid().ToString("N"))[0]=owner;
  var camp=HavenMiniChamp.Find();owner.MoveToWorld(camp.Location,camp.Map);var c=HavenCompanion.Claim(owner);c.SetOrder(owner,OrderType.Stay);
  var home=owner.Location;bool placed=false;
  foreach(int dx in new[]{-12,0,12})foreach(int dy in new[]{-12,0,12}){if(placed || (dx==0&&dy==0))continue;var p=new Point3D(home.X+dx,home.Y+dy,camp.Map.GetAverageZ(home.X+dx,home.Y+dy));if(!camp.Map.CanFit(p,16,false,false))continue;owner.MoveToWorld(p,camp.Map);if(owner.InLOS(c))placed=true;}
  check("owner finds clear twelve-tile test position",()=>Require(placed && owner.InRange(c,12) && !owner.InRange(c,11)));
  var bag=new Bag();var ruby=new Ruby();bag.DropItem(ruby);c.Backpack.DropItem(bag);
  check("twelve-tile pack nested inventory and ledger access",()=>{Require(c.CanOpenPack(owner) && owner.GetInventoryAccessRange(ruby)==12 && HavenResources.Accessible(owner,c.Backpack));bool rejected;LRReason reason;owner.Lift(ruby,1,out rejected,out reason);Require(!rejected && owner.Holding==ruby);Require(ruby.DropToItem(owner,bag,new Point3D(-1,-1,0)));owner.Holding=null;Require(ruby.Parent==bag);});
  var stock=new Bandage(2);c.Backpack.DropItem(stock);c.Skills[SkillName.Healing].Base=120;c.Skills[SkillName.Anatomy].Base=120;owner.Hits=10;
  check("companion bandages owner at twelve tiles and completes range check",()=>{Require(HavenCompanionAccess.BandageRange(c,owner)==12 && HavenCompanionAccess.BandageRange(owner,c)==12 && c.TryBandage(owner));BandageContext.GetContext(c).EndHeal();Require(owner.Hits>10 && stock.Amount==1);});
  check("strangers and unrelated bags retain ordinary range",()=>{var stranger=new PlayerMobile();Require(stranger.GetInventoryAccessRange(ruby)==2 && HavenCompanionAccess.BandageRange(stranger,c)==Bandage.Range && owner.GetInventoryAccessRange(owner.Backpack)==2);stranger.Delete();});
  owner.MoveToWorld(new Point3D(home.X+13,home.Y,home.Z),camp.Map);
  check("thirteen tiles rejects pack ledger and bandage",()=>{Require(!c.CanOpenPack(owner) && owner.GetInventoryAccessRange(ruby)==2 && !HavenResources.Accessible(owner,bag));owner.Hits=10;Require(!c.TryBandage(owner) && BandageContext.BeginHeal(owner,c)==null);});
  c.Delete();owner.Delete();
 }
}

