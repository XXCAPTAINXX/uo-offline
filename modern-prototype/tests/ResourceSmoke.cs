using System;
using System.IO;
using System.Linq;
using Server;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;

public static class ResourceSmoke
{
    private static void Require(bool value) { if (!value) throw new Exception("Resource conservation or access assertion failed"); }
    public static void Run(Action<string,Action> check,bool reload)
    {
        if (reload)
        {
            check("ledger and deed balances survive world restart",()=> {
                var ids=File.ReadAllLines("resource-fixtures.txt");
                var restoredLedger=World.FindItem((Serial)Int32.Parse(ids[0])) as HavenResourceLedger;
                var deed=World.FindItem((Serial)Int32.Parse(ids[1])) as HavenResourceDeed;
                Require(restoredLedger!=null && restoredLedger.Balance(0)==1250 && restoredLedger.Balance(1)==250 && restoredLedger.TotalItems==0);
                Require(deed!=null && deed.ResourceId==0 && deed.Units==50);
            });
            return;
        }
        var owner=new PlayerMobile { Name="ledger fixture",Player=true,Body=0x190 };
        owner.RawStr=100; owner.AddItem(new Backpack()); owner.MoveToWorld(new Point3D(3506,2570,14),Map.Trammel);
        var ledger=new HavenResourceLedger(); owner.Backpack.DropItem(ledger);
        check("every catalog resource constructs with correct amount",()=> {
            Require(HavenResources.Types.Length==HavenResources.Names.Length);
            for(int i=0;i<HavenResources.Types.Length;i++) { var item=HavenResources.Create(i,123); Require(item!=null && item.Amount==123 && item.GetType()==HavenResources.Types[i]); item.Delete(); }
        });
        check("nested bags absorb supported resources and retain other items",()=> {
            var bag=new Bag(); var sub=new Bag(); var sword=new Dagger(); sub.DropItem(new IronIngot(1000)); sub.DropItem(sword); bag.DropItem(sub); owner.Backpack.DropItem(bag);
            Require(ledger.Absorb(owner,bag)==1 && ledger.Balance(0)==1000 && !sword.Deleted && !sub.Deleted && ledger.TotalItems==0);
        });
        check("native commodity deeds deposit exactly once",()=> {
            var material=new DullCopperIngot(250); var deed=new CommodityDeed(); Require(deed.SetCommodity(material)); owner.Backpack.DropItem(deed);
            Require(ledger.Absorb(owner,deed)==1 && ledger.Balance(1)==250 && deed.Deleted && material.Deleted);
            Require(ledger.Absorb(owner,deed)==0 && ledger.Balance(1)==250);
        });
        check("withdraw deed and reabsorb conserves resources",()=> {
            Require(ledger.Withdraw(owner,0,100,true) && ledger.Balance(0)==900);
            var deed=owner.Backpack.FindItemByType(typeof(HavenResourceDeed)) as HavenResourceDeed;
            Require(deed!=null && deed.Units==100 && ledger.Absorb(owner,deed)==1 && ledger.Balance(0)==1000);
        });
        check("deed redemption produces native stack once",()=> {
            Require(ledger.Withdraw(owner,0,100,true));
            var deed=owner.Backpack.FindItemByType(typeof(HavenResourceDeed)) as HavenResourceDeed;
            Require(deed.Redeem(owner) && !deed.Redeem(owner));
            Require(owner.Backpack.GetAmount(typeof(IronIngot))==100 && ledger.Absorb(owner,owner.Backpack)==1 && ledger.Balance(0)==1000);
        });
        check("invalid amounts and insufficient balances cannot withdraw",()=> {
            Require(!ledger.Withdraw(owner,0,-1,true) && !ledger.Withdraw(owner,0,60001,true) && !ledger.Withdraw(owner,0,1001,true) && ledger.Balance(0)==1000);
        });
        check("foreign inventory and locked bags cannot be consumed",()=> {
            var other=new PlayerMobile(); other.AddItem(new Backpack()); var iron=new IronIngot(100); other.Backpack.DropItem(iron);
            var box=new MetalBox { Locked=true }; box.DropItem(new IronIngot(100)); owner.Backpack.DropItem(box);
            Require(ledger.Absorb(owner,iron)==0 && ledger.Absorb(owner,box)==0 && !iron.Deleted && ledger.Balance(0)==1000);
            box.Delete(); other.Delete();
        });
        check("full pack withdrawal is atomic",()=> {
            while(owner.Backpack.TotalItems<125) owner.Backpack.DropItem(new Dagger());
            Require(!ledger.Withdraw(owner,0,50,true) && ledger.Balance(0)==1000);
            foreach(var item in owner.Backpack.Items.OfType<Dagger>().ToArray()) item.Delete();
        });
        check("ledger transfer is atomic on target overflow",()=> {
            var target=new HavenResourceLedger(); owner.Backpack.DropItem(target); target.Credit(0,HavenResourceLedger.MaxBalance);
            Require(!ledger.TransferAll(owner,target) && ledger.Balance(0)==1000 && ledger.Balance(1)==250 && target.Balance(1)==0);
            target.Delete();
        });
        check("ledger transfer preserves each resource and rejects replay",()=> {
            var source=new HavenResourceLedger(); owner.Backpack.DropItem(source); source.Credit(0,300);
            Require(source.TransferAll(owner,ledger) && source.Balance(0)==0 && ledger.Balance(0)==1300);
            Require(source.TransferAll(owner,ledger) && ledger.Balance(0)==1300);
            source.Delete();
        });
        check("persist resource fixtures",()=> {
            Require(ledger.Withdraw(owner,0,50,true));
            var deed=owner.Backpack.FindItemByType(typeof(HavenResourceDeed));
            File.WriteAllLines("resource-fixtures.txt",new[]{ledger.Serial.Value.ToString(),deed.Serial.Value.ToString()});
        });
    }
}
