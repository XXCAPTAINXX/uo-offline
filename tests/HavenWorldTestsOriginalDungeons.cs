using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsOriginalDungeons
{
    public HavenWorldTestsOriginalDungeons() { _ = new HavenWorldTests(); }
    [SkippableFact]
    public void OriginalLocationsPreserveProgressAndHaveUsableRoomFloors()
    {
        TileDataRequirement.SkipIfMissing();
        Assert.True(HavenFrontierHub.Install(false));
        var hub = Assert.Single(HavenFrontierHub.Registry);
        var owner = new PlayerMobile { Player = true, Body = 400, RawStr = 200, RawDex = 100, RawInt = 100 };
        owner.AddItem(new Backpack()); owner.MoveToWorld(new Point3D(4760,3242,0),Map.Trammel);
        var record = HavenFrontierRecord.Get(owner); record.Rooms = 31; record.MinaxCredits = 72;
        var stockExit = new Teleporter(new Point3D(1477,1471,-8),Map.Trammel);
        stockExit.MoveToWorld(new Point3D(6440,2677,20),Map.Trammel);
        try
        {
            var probe = new List<string>();
            foreach(var p in new[]{HavenOriginalDungeons.ShadowEntrance,new Point3D(519,2192,25),new Point3D(56,2328,0),HavenOriginalDungeons.BlackthornLanding,new Point3D(1477,1473,-8)})
            {
                var map=p.X<600?Map.TerMur:Map.Trammel;
                probe.Add($"{map} {p} land {map.Tiles.GetLandTile(p.X,p.Y).ID} average {map.GetAverageZ(p.X,p.Y)}");
                foreach(var tile in map.Tiles.GetStaticTiles(p.X,p.Y))
                {var id=TileData.ItemTable[tile.ID];probe.Add($"static {tile.ID} z {tile.Z} height {id.Height} calc {id.CalcHeight} flags {id.Flags}");}
                for(var z=p.Z-5;z<=p.Z+15;z++){if(map.CanFit(new Point3D(p.X,p.Y,z),16,checkMobiles:false)){probe.Add($"fits z {z}");}}
            }
            File.WriteAllLines(Path.Combine(Path.GetTempPath(),"haven-original-probe.txt"),probe);
            Assert.Null(HavenOriginalDungeons.Preflight());
            Assert.True(HavenOriginalDungeons.Migrate()); Assert.True(HavenOriginalDungeons.Installed);
            Assert.Equal(new Point3D(1477,1473,-8),stockExit.PointDest);
            var exitCount=0;
            foreach(var exit in Map.Trammel.GetItemsInRange<Teleporter>(stockExit.Location,0)){exitCount++;}
            Assert.Equal(1,exitCount);
            Assert.Contains(HavenTravelGump.Entries(1,5),e=>e.Name.Contains("Shadowguard")&&e.Map==Map.TerMur);
            Assert.Equal(Map.TerMur,owner.Map); Assert.Equal(HavenOriginalDungeons.ShadowEntrance,owner.Location);
            Assert.Equal(31,record.Rooms); Assert.Equal(72,record.MinaxCredits);
            var count=hub.Owned.Count;Assert.True(HavenOriginalDungeons.Migrate());Assert.Equal(count,hub.Owned.Count);
            foreach(var passage in hub.Owned.OfType<Teleporter>())
            {
                Assert.True(passage.Map.CanFit(passage.Location,16,checkMobiles:false),$"Stair source {passage.Location}");
                Assert.True(passage.MapDest.CanFit(passage.PointDest,16,checkMobiles:false),$"Stair destination {passage.PointDest}");
            }
            var survey=new List<string>();
            foreach (var room in HavenShadowChamber.Registry)
            {
                owner.MoveToWorld(HavenOriginalDungeons.ShadowEntrance,Map.TerMur);
                Assert.True(room.Start(owner),room.Room.ToString());
                survey.Add($"{room.Room}: arrival {room.Arrival} fits {room.Map.CanFit(room.Arrival,16,checkMobiles:false)} fixtures {room.Fixtures.Count}");
                foreach(var item in room.Puzzle)
                {
                    var adjacent=false;
                    for(var x=-1;x<=1;x++)for(var y=-1;y<=1;y++)
                    {if((x!=0||y!=0)&&room.Map.CanFit(new Point3D(item.X+x,item.Y+y,item.Z),16,checkMobiles:false)){adjacent=true;}}
                    survey.Add($" node {item.Name} {item.Location} adjacent {adjacent}");
                    Assert.True(adjacent,item.Name);
                    Assert.True(Approachable(owner,room.Arrival,item),$"No walking approach to {room.Room}: {item.Name}");
                }
                foreach(var actor in room.Actors)
                {
                    survey.Add($" actor {actor.Role} {actor.Location} fits {room.Map.CanFit(actor.Location,16,checkMobiles:false)}");
                    Assert.True(room.Map.CanFit(actor.Location,16,checkMobiles:false),$"{room.Room} actor {actor.Role}");
                }
                if(room.Room==HavenShadowRoom.Belfry)
                {
                    var bell=room.Puzzle.OfType<HavenShadowNode>().Single(n=>n.Kind==5);
                    owner.MoveToWorld(new Point3D(bell.X+1,bell.Y,bell.Z),room.Map);
                    Assert.True(HavenShadowPuzzles.Use(room,owner,bell,null));
                    foreach(var drake in room.Actors.ToArray()){room.Killed(drake);drake.Delete();}
                    Assert.True(HavenShadowPuzzles.Use(room,owner,bell,null));
                    Assert.Equal(2,owner.Z);Assert.True(room.Map.CanFit(owner.Location,16,checkMobiles:false));
                }
                room.Finish(false);
            }
            File.WriteAllLines(Path.Combine(Path.GetTempPath(),"haven-original-survey.txt"),survey);
            Assert.All(HavenShadowChamber.Registry,r=>Assert.True(r.Map.CanFit(r.Arrival,16,checkMobiles:false),r.Room.ToString()));
            var blackthorn=HavenFrontierBattle.Registry.Single(b=>!b.Pirate);
            blackthorn.NextRun=Core.Now-TimeSpan.FromMinutes(1);owner.MoveToWorld(blackthorn.Location,blackthorn.Map);
            Assert.True(blackthorn.Start(owner));
            for(var wave=0;wave<3;wave++)
            {
                foreach(var enemy in blackthorn.Enemies){Assert.True(enemy.Map.CanFit(enemy.Location,16,checkMobiles:false));}
                owner.MoveToWorld(blackthorn.Center,blackthorn.Map);blackthorn.Credit(owner);
                foreach(var role in new[]{0,1,2})
                foreach(var enemy in blackthorn.Enemies.Where(e=>e.Role==role).ToArray()){blackthorn.Killed(enemy);enemy.Delete();}
            }
            Assert.False(blackthorn.Active);Assert.Equal(1,record.Rifts);Assert.True(record.MinaxCredits>72);
        }
        finally { stockExit.Delete();hub.Delete();owner.Delete(); }
    }
    private static bool Approachable(Mobile mobile,Point3D start,Item node)
    {
        mobile.MoveToWorld(start,node.Map);
        for(var dx=-2;dx<=2;dx++)
        {
            for(var dy=-2;dy<=2;dy++)
            {
                var p=new Point3D(node.X+dx,node.Y+dy,node.Z);
                if(!node.Map.CanFit(p,16,checkMobiles:false)){continue;}
                if(start==p||new MovementPath(mobile,p).Success){return true;}
            }
        }
        return false;
    }
    [SkippableFact]
    public void MigrationRefusesConflictingEntranceBeforeChangingExistingRooms()
    {
        TileDataRequirement.SkipIfMissing();Assert.True(HavenFrontierHub.Install(false));
        var hub=Assert.Single(HavenFrontierHub.Registry);
        var conflict=new Teleporter(new Point3D(1,1,0),Map.Trammel);
        conflict.MoveToWorld(new Point3D(1477,1473,-8),Map.Trammel);
        try
        {
            Assert.Contains("Different teleporter",HavenOriginalDungeons.Preflight());
            Assert.Throws<InvalidOperationException>(()=>HavenOriginalDungeons.Migrate());
            Assert.All(HavenShadowChamber.Registry,r=>{Assert.Equal(Map.Trammel,r.Map);Assert.NotEmpty(r.Fixtures);});
        }
        finally{conflict.Delete();hub.Delete();}
    }
    [SkippableFact]
    public void CompanionWalksAndSolvesOriginalBarAndOrchard()
    {
        TileDataRequirement.SkipIfMissing();Assert.True(HavenFrontierHub.Install(false));
        var hub=Assert.Single(HavenFrontierHub.Registry);var owner=new PlayerMobile{Player=true,Body=400,RawStr=200};
        var companion=new HavenCompanion{BoundOwner=owner};var before=Core.Now;
        try
        {
            Assert.True(HavenOriginalDungeons.Migrate());companion.SetControlMaster(owner);companion.Hits=companion.HitsMax;
            foreach(var kind in new[]{HavenShadowRoom.Bar,HavenShadowRoom.Orchard})
            {
                owner.MoveToWorld(HavenOriginalDungeons.ShadowEntrance,Map.TerMur);owner.Hits=owner.HitsMax;
                companion.MoveToWorld(owner.Location,owner.Map);companion.ControlOrder=OrderType.Follow;
                var room=HavenShadowChamber.Registry.Single(r=>r.Room==kind);Assert.True(room.Start(owner));
                for(var step=0;step<500&&room.Active;step++)
                {Core._now+=TimeSpan.FromSeconds(3);HavenShadowPuzzles.Assist(room,companion);}
                Assert.False(room.Active,$"Companion stalled in {kind} at {companion.Location}");
                Assert.NotEqual(0,HavenFrontierRecord.Get(owner).Rooms & 1<<(int)kind);
            }
        }
        finally{Core._now=before;hub.Delete();companion.Delete();owner.Delete();}
    }
}
