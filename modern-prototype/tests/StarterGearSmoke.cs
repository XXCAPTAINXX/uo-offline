using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class StarterGearSmoke {
 static void Require(bool ok){if(!ok)throw new Exception("Starter progression regression");}
 public static void Run(Action<string,Action> check,bool reload){
  if(reload){check("evolving gear wallet ownership and balances persist",()=>{var p=World.Mobiles.Values.OfType<PlayerMobile>().Single(x=>x.Name=="Starter gear fixture");var restoredBlade=p.Items.OfType<HavenApprenticeBlade>().Single();Require(restoredBlade.Progress.Owner==p && HavenStarterGear.Level(restoredBlade)==20 && restoredBlade.WeaponAttributes.HitLeechMana==100 && HavenWallet.Find(p).Balance==1190 && p.TithingPoints==100000 && HavenStarterGear.Robe(p).Progress.Tier==4);});return;}
  HavenStarterGear.Ensure();var stone=World.Items.Values.OfType<HavenStarterGearStone>().Single();var owner=new PlayerMobile {Player=true,Name="Starter gear fixture",Body=0x190,RawStr=100,RawDex=100,RawInt=100};owner.AddItem(new Backpack());new Account("starter-gear-fixture",Guid.NewGuid().ToString("N"))[0]=owner;owner.MoveToWorld(stone.Location,stone.Map);
  check("starter gear full-pack refusal and single claim",()=>{owner.Backpack.MaxItems=1;Require(!HavenStarterGear.Claim(owner));owner.Backpack.MaxItems=125;Require(HavenStarterGear.Claim(owner) && !HavenStarterGear.Claim(owner) && owner.Backpack.Items.OfType<IHavenStarterGear>().Count()==9);});
  var blade=(HavenApprenticeBlade)owner.Backpack.FindItemByType(typeof(HavenApprenticeBlade));var cape=(HavenEvolvingCape)owner.Backpack.FindItemByType(typeof(HavenEvolvingCape));owner.EquipItem(blade);owner.EquipItem(cape);
  check("credited native monster kill advances equipped gear",()=>{var enemy=new Orc();enemy.SetHits(300);enemy.Karma=-1000;enemy.MoveToWorld(owner.Location,owner.Map);enemy.Damage(enemy.Hits+1,owner);Require(blade.Progress.Experience>0 && cape.Progress.Experience>0);if(!enemy.Deleted)enemy.Delete();});
  check("weapon levels cap at20 and100 percent mana leech with binding",()=>{HavenStarterGear.Gain(blade,owner,100000);Require(HavenStarterGear.Level(blade)==20 && blade.WeaponAttributes.HitLeechMana==100);var stranger=new PlayerMobile();Require(!blade.CanEquip(stranger));int xp=blade.Progress.Experience;HavenStarterGear.Gain(blade,stranger,50);Require(blade.Progress.Experience==xp);stranger.Delete();});
  check("robe upgrades charge exact Marks and reject stale menu",()=>{HavenMarks.Award(owner,10);Require(HavenStarterGear.Upgrade(owner,0) && HavenMarks.Balance(owner)==9 && !HavenStarterGear.Upgrade(owner,0) && HavenMarks.Balance(owner)==9);Require(HavenStarterGear.Upgrade(owner,1)&&HavenStarterGear.Upgrade(owner,2)&&HavenStarterGear.Upgrade(owner,3)&&!HavenStarterGear.Upgrade(owner,4)&&HavenMarks.Balance(owner)==0);});
  var wallet=new HavenWallet(owner);owner.Backpack.DropItem(wallet);owner.Backpack.DropItem(new Gold(1000));owner.Backpack.DropItem(new BankCheck(500));
  check("wallet absorbs gold checks and tithes only room below cap",()=>{Require(wallet.DepositPack(owner)==1500 && wallet.Balance==1500);owner.TithingPoints=99990;Require(wallet.Tithe(owner,1000)&&owner.TithingPoints==100000&&wallet.Balance==1490&&!wallet.Tithe(owner,1000)&&!wallet.Tithe(owner,-1));});
  check("wallet withdrawal preserves funds when full and bank transfers conserve gold",()=>{owner.Backpack.MaxItems=owner.Backpack.TotalItems;Require(!wallet.Withdraw(owner,100)&&wallet.Balance==1490);owner.Backpack.MaxItems=125;Require(wallet.BankDeposit(owner,500)&&wallet.BankWithdraw(owner,200)&&wallet.Balance==1190);});
  check("Haven Luck adds1000 only in original area",()=>{int luck=owner.Luck;Require(HavenNewcomerBonus.Luck(owner)==1000);owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);Require(HavenNewcomerBonus.Luck(owner)==0&&owner.Luck==luck-1000);owner.MoveToWorld(stone.Location,stone.Map);});
  check("native accelerated skill gains stop at100 and respect power-scroll cap",()=>{var skill=owner.Skills.Mining;skill.Cap=120;skill.Base=50;skill.SetLockNoRelay(SkillLock.Up);Server.Misc.SkillCheck.Gain(owner,skill,1);Require(skill.Base==50.5);skill.Base=99.9;Server.Misc.SkillCheck.Gain(owner,skill,1);Require(skill.Base==100);Server.Misc.SkillCheck.Gain(owner,skill,1);Require(skill.Base==100.1);});
  var book=(HavenApprenticeGrimoire)owner.Backpack.FindItemByType(typeof(HavenApprenticeGrimoire));var ears=(HavenFortuneEarrings)owner.Backpack.FindItemByType(typeof(HavenFortuneEarrings));owner.Backpack.DropItem(blade);owner.EquipItem(book);owner.EquipItem(ears);owner.Skills.Magery.Base=120;owner.Hits=10;owner.Mana=owner.ManaMax;new Server.Spells.First.HealSpell(owner,null).Cast();
  Timer.DelayCall(TimeSpan.FromSeconds(3),()=>{check("successful native spell advances equipped grimoire",()=>{Require(owner.Target!=null);owner.Target.Invoke(owner,owner);Require(book.Progress.Experience>0);});owner.Backpack.DropItem(book);owner.EquipItem(blade);owner.Internalize();});
 }
}



