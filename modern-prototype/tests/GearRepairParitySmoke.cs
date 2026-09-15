using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class GearRepairParitySmoke
{
 public static void Run(Action<string> log)
 {
  var owner=new PlayerMobile{Player=true};owner.AddItem(new Backpack());var nested=new Bag();owner.Backpack.DropItem(nested);
  var weapon=new Longsword();var armor=new PlateChest();var robe=new Robe();var ring=new GoldRing();var foreign=new Longsword();
  owner.AddItem(weapon);owner.Backpack.DropItem(armor);nested.DropItem(robe);nested.DropItem(ring);
  weapon.MaxHitPoints=100;weapon.HitPoints=30;armor.MaxHitPoints=100;armor.HitPoints=20;robe.MaxHitPoints=100;robe.HitPoints=10;ring.MaxHitPoints=100;ring.HitPoints=40;foreign.MaxHitPoints=100;foreign.HitPoints=5;
  if(HavenStarterHub.Repair(owner)!=4||weapon.HitPoints!=100||armor.HitPoints!=100||robe.HitPoints!=100||ring.HitPoints!=100||foreign.HitPoints!=5)throw new Exception("Bulk repair scope");
  if(HavenStarterHub.Repair(owner)!=0)throw new Exception("Repeat repair");
  log("PASS equipped and nested weapons, armor, clothing and jewelry repaired; unrelated gear untouched; repeat is a no-op");
  weapon.MaxHitPoints=1;weapon.HitPoints=1;weapon.Attributes.Luck=123;weapon.Name="Keep my name";int count;int cost=HavenGearRepair.Quote(owner,out count);
  if(cost!=250||count!=1)throw new Exception("Quote "+cost+" / "+count);
  if(HavenGearRepair.Restore(owner,cost)||weapon.MaxHitPoints!=1)throw new Exception("Unfunded repair mutated gear");
  owner.BankBox.DropItem(new Gold(1000));int before=Banker.GetBalance(owner);
  if(HavenGearRepair.Restore(owner,0)||Banker.GetBalance(owner)!=before)throw new Exception("Exceeded quoted maximum");
  if(!HavenGearRepair.Restore(owner,cost)||Banker.GetBalance(owner)!=before-250||weapon.MaxHitPoints<=1||weapon.HitPoints!=weapon.MaxHitPoints||weapon.Attributes.Luck!=123||weapon.Name!="Keep my name")throw new Exception("Paid restore");
  log("PASS 250-gold quote, insufficient-funds rollback, stale-price rejection, exact charge and preserved properties");
  var starter=new HavenApprenticeBlade();nested.DropItem(starter);starter.MaxHitPoints=100;starter.HitPoints=50;
  if(HavenGearRepair.Quote(owner,out count)!=0||count!=1||!HavenGearRepair.Restore(owner,0)||starter.MaxHitPoints!=255||starter.HitPoints!=255)throw new Exception("Free starter restoration");
  log("PASS evolving starter gear restores to 255 maximum for free");
  owner.Delete();foreign.Delete();
 }
}
