using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
using Server.Multis;
public static class IslandDecoratingSmoke
{
    public static void Run(Action<string> log)
    {
        var owner=World.FindMobile((Serial)37901);var foundation=World.Items.Values.OfType<HavenIslandFoundation>().Single();
        Point3D? spot=null;
        for(int x=4155;x<4180&&!spot.HasValue;x++)for(int y=2830;y<2860&&!spot.HasValue;y++)
        {var p=new Point3D(x,y,foundation.Map.GetAverageZ(x,y));if(BaseHouse.FindHouseAt(p,foundation.Map,20)==null&&foundation.Map.CanFit(x,y,p.Z,20,true,true,true)&&foundation.Map.CanFit(x+1,y,p.Z,20,true,true,true))spot=p;}
        if(!spot.HasValue)throw new Exception("No outdoor fixture test site");
        owner.MoveToWorld(new Point3D(spot.Value.X,spot.Value.Y+2,spot.Value.Z),foundation.Map);
        var fixture=new Static(0xA9A);fixture.MoveToWorld(spot.Value,foundation.Map);foundation.Fixtures.Add(fixture);
        var chest=new WoodenChest();var contents=new Gold(123);chest.DropItem(contents);foundation.Fixtures.Add(chest);
        try
        {
            if(!HavenIslandDecorating.Owner(owner))throw new Exception("Island owner denied");
            HavenDecoratorActions.Apply(owner,fixture,DecorateCommand.Turn);if(fixture.ItemID==0xA9A)throw new Exception("Static facing unchanged");
            HavenDecoratorActions.Apply(owner,fixture,DecorateCommand.East);if(fixture.X!=spot.Value.X+1)throw new Exception("Outdoor east failed");
            HavenDecoratorActions.Apply(owner,fixture,DecorateCommand.Up);if(fixture.Z!=spot.Value.Z+1)throw new Exception("Outdoor up failed");
            HavenDecoratorActions.Apply(owner,fixture,DecorateCommand.Down);if(fixture.Z!=spot.Value.Z)throw new Exception("Outdoor down failed");
            var stranger=new PlayerMobile();stranger.MoveToWorld(owner.Location,owner.Map);
            try{var before=fixture.Location;HavenDecoratorActions.Apply(stranger,fixture,DecorateCommand.West);if(fixture.Location!=before)throw new Exception("Stranger moved island fixture");}finally{stranger.Delete();}
            fixture.Internalize();chest.MoveToWorld(spot.Value,foundation.Map);
            HavenDecoratorActions.Apply(owner,chest,DecorateCommand.East);if(chest.X!=spot.Value.X+1||contents.Parent!=chest)throw new Exception("Container move lost contents");
            log("PASS island owner moves outdoor managed fixtures without house lockdown; static furniture rotates; Up/Down work; stranger denied; container identity and contents preserved");
        }
        finally{foundation.Fixtures.Remove(fixture);foundation.Fixtures.Remove(chest);fixture.Delete();chest.Delete();}
    }
}
