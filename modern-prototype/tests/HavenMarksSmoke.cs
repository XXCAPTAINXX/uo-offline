using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;

public static class HavenMarksSmoke
{
    private static void Require(bool value){if(!value)throw new Exception("Haven Marks regression");}
    public static void Run(Action<string,Action> check,bool reload) {
        if(reload) {
            check("Marks balance and purchased reward survive reload",()=>{
                var restored=World.Mobiles.Values.OfType<PlayerMobile>().Single(x=>x.Name=="Marks fixture");
                Require(HavenMarks.Balance(restored)==HavenMarks.Maximum-80 && restored.Backpack.FindItemByType(typeof(Broadsword))!=null);
            });return;
        }
        var owner=new PlayerMobile {Player=true,Name="Marks fixture",Body=0x190,RawStr=100};owner.AddItem(new Backpack());
        var account=new Account("marks-fixture",Guid.NewGuid().ToString("N"));account[0]=owner;
        owner.MoveToWorld(new Point3D(3506,2570,14),Map.Trammel);
        check("Marks are per character and awards clamp at cap",()=>{
            Require(HavenMarks.Award(owner,100)==100 && HavenMarks.Balance(owner)==100);
            var alt=new PlayerMobile {Player=true};account[1]=alt;Require(HavenMarks.Balance(alt)==0);alt.Delete();
            Require(HavenMarks.Award(owner,-1)==0 && HavenMarks.Award(owner,int.MaxValue)==HavenMarks.Maximum-100);
        });
        check("full pack and invalid reward preserve Marks",()=>{
            owner.Backpack.MaxItems=1;var filler=new Bag();owner.Backpack.DropItem(filler);
            Require(!HavenMarks.Buy(owner,0) && !HavenMarks.Buy(owner,-1) && !HavenMarks.Buy(owner,99) && HavenMarks.Balance(owner)==HavenMarks.Maximum);
            filler.Delete();owner.Backpack.MaxItems=125;
        });
        check("purchase spends exact price and supplies advertised weapon",()=>{
            Require(HavenMarks.Buy(owner,0) && HavenMarks.Balance(owner)==HavenMarks.Maximum-80);
            var sword=owner.Backpack.FindItemByType(typeof(Broadsword)) as Broadsword;
            Require(sword!=null && sword.WeaponAttributes.HitLeechMana==60 && sword.WeaponAttributes.HitLeechHits==30 && sword.Attributes.WeaponDamage==30);
        });
        check("all reward previews create valid items without charging",()=>{
            int before=HavenMarks.Balance(owner);
            for(int i=0;i<HavenMarks.Names.Length;i++){var item=HavenMarks.CreateReward(i);Require(item!=null && item.Name==HavenMarks.Names[i]);item.Delete();}
            Require(HavenMarks.Balance(owner)==before);
        });
    }
}
