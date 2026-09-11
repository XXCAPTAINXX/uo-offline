using System;
using System.IO;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class CompanionInventorySmoke {
 public static void Initialize(){if(File.Exists("INVENTORY-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(3),Run);}
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText("inventory-checks.log","PASS "+label+"\n");}
 static void Run(){try{
 var owner=new PlayerMobile{Player=true,Body=0x190};owner.AddItem(new Backpack());new Account("inventory-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);
 var c=HavenCompanion.Claim(owner);var bag=new Bag();var gold=new Gold(200);bag.DropItem(gold);c.Backpack.DropItem(bag);
 Check(c.Backpack.MaxItems==1000&&c.Backpack.MaxWeight==0,"1000 item unlimited-weight companion pack");
 foreach(CompanionRole role in new[]{CompanionRole.Warrior,CompanionRole.Caster,CompanionRole.Archer,CompanionRole.Bard,CompanionRole.Healer}){
 Check(c.SetRole(owner,role),"role "+role);Check(!c.AIObject.DoOrderRelease()&&c.ControlMaster==owner,"native release blocked "+role);c.DropBackpack();Check(bag.Parent==c.Backpack&&gold.Parent==bag,"inventory retained "+role);
 }
 c.MoveToWorld(new Point3D(2000,1500,0),Map.Trammel);Check(c.Recall(owner)&&c.Map==owner.Map&&c.Location==owner.Location,"recall from another facet at any distance");
 c.MoveToWorld(new Point3D(1500,1500,0),owner.Map);Check(c.Recall(owner)&&c.Location==owner.Location,"recall across the same facet");
 Check(c.StartMission(owner,30,CompanionMission.Mining),"mission dispatch");Check(bag.Parent==c.Backpack&&gold.Parent==bag,"nested items retained while away");Check(c.Recall(owner),"early recall");Check(bag.Parent==c.Backpack&&gold.Parent==bag&&gold.Amount==200,"same items return unchanged");
 var horse=new Horse();horse.MoveToWorld(owner.Location,owner.Map);horse.SetControlMaster(owner);Check(horse.AIObject.DoOrderRelease()&&!horse.Controlled,"ordinary pet release unchanged");horse.Delete();c.Delete();owner.Delete();File.AppendAllText("inventory-checks.log","COMPLETE\n");
 }catch(Exception e){File.AppendAllText("inventory-checks.log","FAIL "+e+"\n");}Core.Kill(false);}
}
