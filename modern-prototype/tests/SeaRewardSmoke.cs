using System;
using Server;
using Server.Items;
using Server.Mobiles;
public static class SeaRewardSmoke
{
 public static void Run(Action<string> log)
 {
  var boss=new BaseSeaChampion(null,AIType.AI_Melee,FightMode.Closest);var owner=new PlayerMobile{Player=true};owner.AddItem(new Backpack());var remote=new PlayerMobile{Player=true};remote.AddItem(new Backpack());
  boss.MoveToWorld(new Point3D(3400,2400,0),Map.Felucca);owner.MoveToWorld(boss.Location,boss.Map);remote.MoveToWorld(new Point3D(1000,1000,0),Map.Felucca);
  boss.RegisterDamage(remote,10000);boss.RegisterDamage(owner,1);
  for(int n=0;n<20;n++){var item=new GoldRing();boss.AwardArtifact(item);if(item.Deleted||!item.IsChildOf(owner.Backpack))throw new Exception("Filtered winner / one-damage boundary");item.Delete();}
  owner.Backpack.MaxItems=1;owner.Backpack.DropItem(new Gold(1));var full=new GoldRing();boss.AwardArtifact(full);if(full.Deleted||!full.IsChildOf(owner.BankBox))throw new Exception("Full pack lost artifact");
  log("PASS twenty one-damage draws award the eligible player despite an ineligible high-damage entry; full backpack delivers exact artifact to bank");
  var petBoss=new BaseSeaChampion(null,AIType.AI_Melee,FightMode.Closest);petBoss.MoveToWorld(boss.Location,boss.Map);var pet=new Dog();pet.SetControlMaster(owner);pet.MoveToWorld(owner.Location,owner.Map);((Mobile)petBoss).RegisterDamage(10,pet);petBoss.RegisterDamageTo(petBoss);var petReward=new GoldRing();petBoss.AwardArtifact(petReward);if(petReward.Deleted||!petReward.IsChildOf(owner.BankBox))throw new Exception("Pet owner credit");
  log("PASS native pet damage resolves to owner and preserves bank delivery");
  owner.MoveToWorld(remote.Location,remote.Map);var none=new GoldRing();boss.AwardArtifact(none);if(!none.Deleted)throw new Exception("No eligible recipient should not award");
  pet.Delete();petBoss.Delete();boss.Delete();owner.Delete();remote.Delete();
 }
}
