using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.HavenPrototype;
public static class PersonalCampSmoke
{
    static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
    public static void Run(Action<string> log)
    {
        var p=new PlayerMobile{Player=true,Body=0x190,RawStr=100};p.AddItem(new Backpack());new Account("camp-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=p;
        var stranger=new PlayerMobile{Player=true,Body=0x190};stranger.AddItem(new Backpack());
        var camp=new HavenPersonalCamp(p);var cargo=new Gold(123);camp.DropItem(cargo);p.Backpack.DropItem(camp);
        try
        {
            p.MoveToWorld(new Point3D(1430,1690,0),Map.Trammel);
            Check(!camp.Place(p,new Point3D(1429,1690,0)),"Camp accepted guarded city");
            Check(!camp.OnDroppedToWorld(p,p.Location),"Direct ground drop bypass"); bool placed=false;
            for(int dx=-60;dx<60&&!placed;dx++)for(int dy=-60;dy<60&&!placed;dy++)
            {
                int x=3500+dx,y=2580+dy;var point=new Point3D(x,y,Map.Trammel.GetAverageZ(x,y));
                if(!Region.Find(point,Map.Trammel).IsPartOf<Server.Regions.GuardedRegion>() || !Region.Find(point,Map.Trammel).IsPartOf("Haven Island"))continue; p.MoveToWorld(new Point3D(x+2,y,point.Z),Map.Trammel);placed=camp.Place(p,point);
            }
            Check(placed&&camp.Deployed&&!camp.Movable,"Outdoor placement: travel="+HavenPreview.CanTravel(p)+" packed="+camp.IsChildOf(p.Backpack)+" alive="+p.Alive+" position="+p.Location);
            var serial=cargo.Serial;stranger.MoveToWorld(p.Location,p.Map);LRReason reason=LRReason.Inspecific;
            Check(!camp.IsAccessibleTo(stranger)&&!camp.CheckLift(stranger,cargo,ref reason)&&!camp.Pack(stranger),"Foreign owner accessed camp");
            Check(camp.IsAccessibleTo(p),"Owner cannot access deployed storage"); camp.IsLockedDown=true;Check(!camp.Pack(p),"Locked camp packed");camp.IsLockedDown=false;
            p.Backpack.MaxItems=1;Check(!camp.Pack(p)&&camp.Deployed&&cargo.Parent==camp,"Full pack lost camp contents");
            p.Backpack.MaxItems=125;Check(camp.Pack(p)&&camp.IsChildOf(p.Backpack)&&cargo.Parent==camp&&cargo.Serial==serial&&cargo.Amount==123,"Pack changed stored item");
            Check(!World.Items.Values.OfType<HavenCampPiece>().Any(x=>!x.Deleted&&x.Camp==camp),"Packed camp left furniture behind");
            log("PASS personal camp: other city placement rejected, Haven placement accepted, owner-only storage, full pack preserved contents, same item serial retained, furniture cleaned up");
        }
        finally{camp.Delete();p.Delete();stranger.Delete();}
    }
}
