using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

public static class StarterHomeSmoke
{
    private static void Require(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run(Action<string,Action> check,bool reload)
    {
        if(reload)
        {
            check("pirate lodge owner secure storage and cargo survive reload",()=>{
                var house=World.Items.Values.OfType<HavenPirateLodge>().Single();
                Require(house.Owner!=null && house.Secures.Count==4,"Ownership or secures lost");
                Require(house.Secures[0].Item.Items.OfType<Gold>().Sum(g=>g.Amount)==321,"Stored items lost");
                Require(HavenStarterHome.Find(house.Owner)==house,"Home lookup lost");
            });return;
        }
        var owner=new PlayerMobile {Player=true,Name="Home fixture",Body=0x190};owner.RawStr=100;owner.AddItem(new Backpack());
        var account=new Account("home-fixture",Guid.NewGuid().ToString("N"));account[0]=owner;owner.MoveToWorld(new Point3D(3506,2570,14),Map.Trammel);
        HavenPirateLodge lodge=null;
        check("pirate lodge uses a safe native plot and claim cannot duplicate",()=>{
            lodge=HavenStarterHome.Claim(owner);Require(lodge!=null,"No valid plot");Require(HavenStarterHome.Claim(owner)==lodge,"Duplicate claim");
            Require(lodge.Components.Width==18,"Wrong foundation size");
        });
        if(lodge==null)return;
        check("pirate lodge shares ownership with account but rejects strangers",()=>{
            var alt=new PlayerMobile {Player=true};account[1]=alt;var stranger=new PlayerMobile {Player=true};
            Require(lodge.IsOwner(alt) && HavenStarterHome.Find(alt)==lodge,"Alt ownership missing");
            Require(!lodge.HasSecureAccess(stranger,lodge.Secures[0]) && lodge.HasSecureAccess(alt,lodge.Secures[0]),"Storage permissions incorrect");
            stranger.Delete();
        });
        check("home arrival and secured cargo work without changing progression",()=>{
            Require(HavenStarterHome.Travel(owner),"Porch blocked");
            var chest=(Container)lodge.Secures[0].Item;chest.DropItem(new Gold(321));
            Require(chest.IsSecure && !chest.Movable && chest.Items.OfType<Gold>().Sum(g=>g.Amount)==321,"Chest failed");
        });
        check("native spiral staircase provides both floor destinations",()=>{
            var stairs=lodge.Fixtures.OfType<SpiralStaircaseAddon>().Single();
            var low=stairs.Components.OfType<TeleporterComponent>().OrderBy(c=>c.Z).First();
            var high=stairs.Components.OfType<TeleporterComponent>().OrderBy(c=>c.Z).Last();
            Require(stairs.CheckAccessible(owner,low),"Stair access denied");
            low.DoTeleport(owner);Require(owner.Z==lodge.Z+27,"Upstairs destination wrong");
            high.DoTeleport(owner);Require(owner.Z==lodge.Z+7,"Ground destination wrong");
            Require(HavenStarterHome.Travel(owner),"Return home failed");
        });
        var rows=new List<string>{"kind,id,x,y,z"};
        foreach(var t in lodge.Components.List)rows.Add("component,"+t.m_ItemID+","+t.m_OffsetX+","+t.m_OffsetY+","+t.m_OffsetZ);
        foreach(var item in lodge.Fixtures.Concat(lodge.Doors))
        {
            var addon=item as BaseAddon;
            if(addon!=null) foreach(var c in addon.Components) rows.Add("fixture,"+c.ItemID+","+(c.X-lodge.X)+","+(c.Y-lodge.Y)+","+(c.Z-lodge.Z));
            else rows.Add("fixture,"+item.ItemID+","+(item.X-lodge.X)+","+(item.Y-lodge.Y)+","+(item.Z-lodge.Z));
        }
        File.WriteAllLines("house-design.csv",rows);
    }
}
