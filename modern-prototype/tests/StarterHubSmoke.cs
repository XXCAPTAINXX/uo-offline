using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;

public static class StarterHubSmoke
{
    private static void Require(bool value) { if(!value) throw new Exception("Starter hub assertion failed"); }
    public static void Run(Action<string,Action> check,bool reload)
    {
        check("Haven service stones are complete and setup is idempotent",()=> {
            HavenStarterHub.Ensure(); HavenStarterHub.Ensure();
            var stones=World.Items.Values.OfType<HavenServiceStone>().ToArray();
            Require(World.Items.Values.OfType<HavenServicePost>().Count()==3);
            Require(new[]{0,3,6}.SelectMany(HavenServiceMenu.Services).Distinct().Count()==9);
            Require(stones.Length==3 && stones.Select(s=>s.Service).Distinct().Count()==3 && stones.All(s=>s.Map==Map.Trammel && !s.Movable));
        });
        if(reload)
        {
            check("starter supply claim persists without replay",()=> {
                var owner=World.Mobiles.Values.OfType<PlayerMobile>().Single(m=>m.Name=="Hub fixture");
                Require(!HavenStarterHub.Claim(owner,0) && !HavenStarterHub.Claim(owner,1));
            });
            check("player checklist status survives reload",()=> {
                var owner=World.Mobiles.Values.OfType<PlayerMobile>().Single(m=>m.Name=="Hub fixture");
                Require(HavenPlaytest.Status(owner,0)==1 && HavenPlaytest.Status(owner,1)==2);
            });
            return;
        }
        var player=new PlayerMobile {Name="Hub fixture",Player=true,Body=0x190};
        player.RawStr=100; player.RawDex=50; player.RawInt=50; player.AddItem(new Backpack());
        new Account("hub-fixture",Guid.NewGuid().ToString("N"))[0]=player;
        var stone=World.Items.Values.OfType<HavenServiceStone>().First();
        player.MoveToWorld(stone.Location,stone.Map);
        check("service actions reject distant players",()=> {
            Require(HavenStarterHub.CanUse(player,stone));
            player.MoveToWorld(new Point3D(100,100,0),Map.Trammel); Require(!HavenStarterHub.CanUse(player,stone));
            player.MoveToWorld(stone.Location,stone.Map);
        });
        check("full pack preserves unclaimed starter supplies",()=> {
            for(int i=0;i<125;i++) player.Backpack.DropItem(new Dagger());
            Require(!HavenStarterHub.Claim(player,0) && player.Backpack.TotalItems==125);
            foreach(var item in player.Backpack.Items.ToArray()) item.Delete();
        });
        check("normal starter supplies preserve skills and stats and cannot duplicate",()=> {
            double skill=player.Skills.Swords.Base;
            Require(HavenStarterHub.Claim(player,0) && player.RawDex==50 && player.RawInt==50 && player.Skills.Swords.Base==skill);
            Require(!HavenStarterHub.Claim(player,0));
            Require(player.Backpack.FindItemByType(typeof(HavenResourceLedger))!=null);
            Require(((Broadsword)player.Backpack.FindItemByType(typeof(Broadsword))).WeaponAttributes.HitLeechMana==30);
        });
        check("arcane kit preserves progression and repair restores durability only",()=> {
            Require(HavenStarterHub.Claim(player,1) && player.Skills.Magery.Base==0);
            var sword=(Broadsword)player.Backpack.FindItemByType(typeof(Broadsword));
            int max=sword.MaxHitPoints; sword.HitPoints=max-5;
            Require(HavenStarterHub.Repair(player)==1 && sword.HitPoints==max && sword.MaxHitPoints==max);
        });
        check("player checklist records valid results and rejects invalid indexes",()=> {
            Require(HavenPlaytest.Record(player,0,1,"fixture pass") && HavenPlaytest.Record(player,1,2,"fixture\tfailed\nnotes"));
            Require(!HavenPlaytest.Record(player,-1,1,"") && !HavenPlaytest.Record(player,0,8,""));
            Require(HavenPlaytest.Status(player,0)==1 && HavenPlaytest.Status(player,1)==2);
            var other=new PlayerMobile(); Require(HavenPlaytest.Status(other,0)==0); other.Delete();
        });
        check("checklist travel reaches services without changing test results",()=> {
            int status=HavenPlaytest.Status(player,0);
            Require(HavenPlaytest.TravelToTest(player,0));
            Require(player.Map==Map.Trammel && player.InRange(HavenPreview.Destinations[0].Point,3));
            Require(HavenPlaytest.Status(player,0)==status);
            Require(!HavenPlaytest.TravelToTest(player,-1) && !HavenPlaytest.TravelToTest(player,999) && !HavenPlaytest.TravelToTest(player,3));
        });
        check("checklist travel obeys combat restriction",()=> {
            var enemy=new PlayerMobile {Player=true,Body=0x190};
            enemy.RawStr=100; enemy.Hits=enemy.HitsMax; enemy.MoveToWorld(player.Location,player.Map);
            player.AggressiveAction(enemy,false); Require(player.Aggressors.Count>0);
            Require(!HavenPlaytest.TravelToTest(player,8));
            enemy.Delete(); player.Aggressors.Clear();
        });
    }
}
