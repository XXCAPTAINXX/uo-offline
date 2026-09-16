using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class MissionDeliverySmoke {
 public static void Run(Action<string> log,Action done){
 var owner=new PlayerMobile();owner.AddItem(new Backpack());var c=new HavenCompanion();
 typeof(HavenCompanion).GetField("_owner",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,owner);
 var bag=new Bag{Name="taming mission bonus supplies"};var potion=new HavenBondingPotion();var leash=new HavenPetLeash();bag.DropItem(potion);bag.DropItem(leash);c.Backpack.DropItem(bag);
 var personal=new Bag{Name="personal bag"};var personalGold=new Gold(7);personal.DropItem(personalGold);c.Backpack.DropItem(personal);
 var loot=new Katana();new HavenDoomMissionLoot(owner,loot);
 var pending=(Dictionary<int,int>)typeof(HavenCompanion).GetField("_pendingResources",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);pending.Add(0,20);
 typeof(HavenCompanion).GetField("_pendingGold",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,150);
 try{
 c.DeliverRewards();
 if(!bag.Deleted||potion.Parent!=c.Backpack||leash.Parent!=c.Backpack||loot.Parent!=c.Backpack||personalGold.Parent!=personal||pending.Count!=0)throw new Exception("Loose delivery failed");
 if(c.Backpack.Items.OfType<Gold>().Sum(g=>g.Amount)!=150||c.Backpack.Items.OfType<IronIngot>().Sum(g=>g.Amount)!=20)throw new Exception("Gold or resources wrong");
 c.DeliverRewards();if(c.Backpack.Items.OfType<Gold>().Sum(g=>g.Amount)!=150)throw new Exception("Duplicate rewards");
 int max=c.Backpack.MaxItems;c.Backpack.MaxItems=c.Backpack.TotalItems;
 var overflow=new Katana();new HavenDoomMissionLoot(owner,overflow);c.DeliverRewards();if(overflow.Parent==c.Backpack||HavenDoomMissionLoot.Pending(owner)!=1)throw new Exception("Full pack lost overflow");
 c.Backpack.MaxItems=max;c.DeliverRewards();if(overflow.Parent!=c.Backpack||HavenDoomMissionLoot.Pending(owner)!=0)throw new Exception("Retry did not deliver");
 pending[0]=100000;c.DeliverRewards();int expectedIron=c.Backpack.Items.OfType<IronIngot>().Sum(g=>g.Amount);if(expectedIron<=20||expectedIron+c.PendingResources!=100020)throw new Exception("Partial delivery accounting failed");
 Timer.DelayCall(TimeSpan.FromSeconds(1),()=>{try{if(c.Backpack.Items.OfType<IronIngot>().Sum(g=>g.Amount)!=expectedIron)throw new Exception("Materials swept into ledger");log("PASS loose bonus supplies, gold, materials and Doom artifacts; personal bag preserved; no duplicate rewards; full-pack retention and retry; materials stay loose");}catch(Exception e){log("FAIL "+e);}finally{c.Delete();owner.Delete();done();}});
 }catch(Exception e){log("FAIL "+e);c.Delete();owner.Delete();done();}
 }
}

