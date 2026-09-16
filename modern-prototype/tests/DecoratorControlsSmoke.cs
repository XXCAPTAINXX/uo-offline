using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.Multis;
using Server.HavenPrototype;
public static class DecoratorControlsSmoke
{
    public static void Run(Action<string> log)
    {
        var owner = World.FindMobile((Serial)37901);
        var library = World.Items.Values.OfType<HavenHouseMapLibrary>().Single(i=>i.Parent==null && i.Map==Map.Trammel);
        var location = library.Location; int art = library.ItemID;
        owner.MoveToWorld(new Point3D(library.X-1,library.Y,library.Z),library.Map);
        var tool = new InteriorDecorator(); owner.Backpack.DropItem(tool);
        try
        {
            if (!InteriorDecorator.InHouse(owner)) throw new Exception("Actual library room not recognized as owner's house");
            var type = typeof(InteriorDecorator).GetNestedType("InternalTarget",BindingFlags.NonPublic);
            Action<DecorateCommand> use = command => {tool.Command=command;owner.Target=(Target)Activator.CreateInstance(type,new object[]{tool});owner.Target.Invoke(owner,library);};
            use(DecorateCommand.Up); if(library.Z!=location.Z+1)throw new Exception("Native Raise failed");
            use(DecorateCommand.Down); if(library.Z!=location.Z)throw new Exception("Native Lower failed");
            use(DecorateCommand.Turn); if(library.ItemID==art)throw new Exception("Native Turn failed");
            use(DecorateCommand.Down); if(library.Z!=location.Z)throw new Exception("Lower passed through floor");
            log("PASS actual locked-down library responds to native Raise, Lower and Turn; cannot lower beneath its house floor");
            var house=BaseHouse.FindHouseAt(library); Point3D? spot=null;
            for(int x=house.X-12;x<=house.X+12&&!spot.HasValue;x++)
                for(int y=house.Y-12;y<=house.Y+12&&!spot.HasValue;y++)
                {
                    bool clear=true;
                    for(int dx=0;dx<=1;dx++)for(int dy=0;dy<=1;dy++)
                    {var p=new Point3D(x+dx,y+dy,location.Z);if(!house.IsInside(p,16)||!house.Map.CanFit(p.X,p.Y,p.Z,16,true,true,true))clear=false;}
                    if(clear)spot=new Point3D(x,y,location.Z);
                }
            if(!spot.HasValue)throw new Exception("No clear furnishing test square");
            var chest=new WoodenChest();var gold=new Gold(123);chest.DropItem(gold);chest.MoveToWorld(spot.Value,house.Map);
            try
            {
                HavenDecoratorActions.Apply(owner,chest,DecorateCommand.LockDown);if(!house.IsLockedDown(chest))throw new Exception("Lock down failed");
                foreach(var command in new[]{DecorateCommand.East,DecorateCommand.South,DecorateCommand.West,DecorateCommand.North})
                {var before=chest.Location;HavenDecoratorActions.Apply(owner,chest,command);if(before==chest.Location)throw new Exception("Cardinal move failed: "+command);}
                if(chest.Location!=spot.Value||gold.Parent!=chest||!house.IsLockedDown(chest))throw new Exception("Movement lost contents/lockdown");
                var stranger=new PlayerMobile();stranger.MoveToWorld(owner.Location,owner.Map);
                try{HavenDecoratorActions.Apply(stranger,chest,DecorateCommand.Release);if(!house.IsLockedDown(chest))throw new Exception("Stranger released chest");}finally{stranger.Delete();}
                HavenDecoratorActions.Apply(owner,chest,DecorateCommand.Release);if(!chest.Movable||house.IsLockedDown(chest))throw new Exception("Release failed");
                HavenDecoratorActions.Apply(owner,chest,DecorateCommand.Secure);if(!house.IsSecure(chest))throw new Exception("Secure failed");
                HavenDecoratorActions.Apply(owner,chest,DecorateCommand.Release);if(house.IsSecure(chest)||!chest.Movable||gold.Parent!=chest)throw new Exception("Secure release lost contents");
                log("PASS four cardinal moves preserve locked-down chest and contents; native Lock down/Secure/Release work; stranger release denied");
            }
            finally{house.Release(owner,chest);chest.Delete();}
        }
        finally { Target.Cancel(owner); library.ItemID=art;library.Location=location;tool.Delete(); }
    }
}

