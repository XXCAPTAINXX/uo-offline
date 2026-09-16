using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class HouseStorageBrowserSmoke
{
 public static void Run(PlayerMobile owner,Action<string> log)
 {
  var station=World.Items.Values.OfType<HavenEstateStorage>().First(s=>!s.Deleted&&s.Store!=null);
  var old=owner.Location;var map=owner.Map;var vault=station.Store.StorageVault;int max=vault.MaxItems;
  var c=new HavenCompanion();var bag=new Bag();var loot=new Item(0xF0E){Name="Test loot"};var sword=new Longsword();var ledger=new HavenResourceLedger();var bandages=new Bandage(100);var other=new PlayerMobile{Player=true};
  try {
   owner.MoveToWorld(new Point3D(station.X,station.Y+1,station.Z),station.Map);
   if(!station.Store.CanAccessStores(owner,station))throw new Exception("Fixture storage access");
   typeof(HavenCompanion).GetField("_owner",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,owner);c.SetControlMaster(owner);c.MoveToWorld(owner.Location,owner.Map);
   c.Backpack.DropItem(bag);bag.DropItem(loot);bag.DropItem(sword);bag.DropItem(ledger);bag.DropItem(bandages);
   if(!HavenStorageTools.DepositCandidates(c).Contains(loot)||HavenStorageTools.DepositCandidates(c).Any(i=>i==sword||i==ledger||i==bandages))throw new Exception("Protection selection");
   if(HavenStorageTools.Deposit(other,c,station)!=-1)throw new Exception("Foreign deposit");
   vault.MaxItems=Math.Max(1,vault.TotalItems);if(vault.TotalItems==0)vault.DropItem(new Item(1){Name="capacity fixture"});
   HavenStorageTools.Deposit(owner,c,station);if(!loot.IsChildOf(c.Backpack))throw new Exception("Full storage lost item");
   vault.MaxItems=max;
   HavenStorageTools.Deposit(owner,c,station);if(!loot.IsChildOf(vault)||!sword.IsChildOf(c.Backpack)||!ledger.IsChildOf(c.Backpack)||!bandages.IsChildOf(c.Backpack))throw new Exception("Deposit/keep mismatch");
   var nested=new Bag();vault.DropItem(nested);nested.DropItem(loot);
   if(!HavenStorageTools.Contents(vault,true).Contains(loot)||!HavenStorageTools.Withdraw(owner,station,loot)||!loot.IsChildOf(owner.Backpack))throw new Exception("Nested search/withdraw");nested.Delete();
   if(HavenStorageTools.Withdraw(owner,station,loot))throw new Exception("Stale withdrawal replay");
   var gold=new Gold(1000){Name="quantity test gold"};vault.DropItem(gold);if(!HavenStorageTools.WithdrawAmount(owner,station,gold,100)||gold.Amount!=100||!gold.IsChildOf(owner.Backpack))throw new Exception("Partial withdrawal");var remainder=vault.Items.Single(i=>i is Gold&&i.Name=="quantity test gold");if(remainder.Amount!=900)throw new Exception("Partial remainder");int packMax=owner.Backpack.MaxItems;gold.Delete();owner.Backpack.MaxItems=owner.Backpack.TotalItems;try{if(HavenStorageTools.WithdrawAmount(owner,station,remainder,10)||remainder.Amount!=900)throw new Exception("Failed partial rollback");}finally{owner.Backpack.MaxItems=packMax;remainder.Delete();}
   var supplies=new Bag();var changed=new Gold(10);var stable=new Item(1){Name="stable snapshot loot"};var fresh=new Item(1){Name="new loot"};c.Backpack.DropItem(supplies);supplies.DropItem(changed);supplies.DropItem(stable);
   var snapshot=HavenStorageTools.DepositCandidates(c).ToDictionary(i=>i,i=>i.Amount);changed.Amount=20;supplies.DropItem(fresh);
   if(HavenStorageTools.DepositSnapshot(other,c,station,snapshot)!=-1)throw new Exception("Foreign snapshot accepted");
   if(HavenStorageTools.DepositSnapshot(owner,c,station,snapshot)!=1||!stable.IsChildOf(vault)||!changed.IsChildOf(c.Backpack)||!fresh.IsChildOf(c.Backpack))throw new Exception("Snapshot swept changed/new loot");
   if(HavenStorageTools.DepositSnapshot(owner,c,station,snapshot)!=0)throw new Exception("Snapshot replay");
   snapshot=HavenStorageTools.DepositCandidates(c).ToDictionary(i=>i,i=>i.Amount);supplies.LootType=LootType.Blessed;
   if(HavenStorageTools.DepositSnapshot(owner,c,station,snapshot)!=0)throw new Exception("Newly protected parent swept");
   stable.Delete();supplies.Delete();
   var filterBag=new Bag();vault.DropItem(filterBag);var blade=new Longsword{Name="Cyclone blade",Weight=9};var shield=new WoodenShield{Name="Cyclone shield"};var book=new Spellbook{Name="Cyclone spellbook"};filterBag.DropItem(blade);filterBag.DropItem(shield);filterBag.DropItem(book);
   if(!HavenStorageTools.Query(vault,vault,"blade cyclone",2,0,1).SequenceEqual(new Item[]{blade}))throw new Exception("Multiword weapon search");
   if(!HavenStorageTools.Query(vault,vault,"cyclone",2,0,3).SequenceEqual(new Item[]{shield}))throw new Exception("Shield filter");
   if(!HavenStorageTools.Query(vault,vault,"cyclone",2,0,6).SequenceEqual(new Item[]{book}))throw new Exception("Spellbook filter");
   if(HavenStorageTools.Query(vault,vault,"cyclone",2,4,0).First()!=book)throw new Exception("Descending sort");
   filterBag.Delete();
   if(new HavenStorageDepositGump(owner,c,station).Entries.Count==0)throw new Exception("Empty preview");
   foreach(int sort in new[]{0,1,2,3,4}){var g=new HavenEstateStorageGump(station,999,null,"",0,sort);if(g.Entries.Count==0)throw new Exception("Empty gump");}
   if(new HavenEstateStorageGump(station,0,null,"no-such-search",2,0).Entries.Count==0)throw new Exception("Empty search gump");
   log("PASS protected nested loot deposit, foreign access rejection, full-storage retention, nested withdrawal, stale replay, browser sort/empty/page construction; multiword gear filters; snapshot changed/new/protected/replay safety");
  } finally {vault.MaxItems=max;loot.Delete();sword.Delete();ledger.Delete();bandages.Delete();bag.Delete();c.Delete();other.Delete();owner.MoveToWorld(old,map);}
 }
}
