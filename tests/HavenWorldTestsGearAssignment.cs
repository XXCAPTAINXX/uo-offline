using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsGearAssignment
{
    public HavenWorldTestsGearAssignment() { _=new HavenWorldTests(); }
    [Fact]
    public void OfflineAssignmentEarnsTimedLootAndGrowthOnlyOnceAndPreservesEquippedItems()
    {
        var owner=new PlayerMobile { Player=true,Body=400,RawStr=100 };
        var companion=new HavenCompanion { BoundOwner=owner };
        var now=Core.Now; var equipped=companion.Items.ToArray();
        try
        {
            var job=HavenCompanionGearAssignment.Begin(companion,now);
            Assert.Same(job,HavenCompanionGearAssignment.Begin(companion,now+TimeSpan.FromMinutes(2)));
            job.Tick(now+TimeSpan.FromMinutes(4),false); Assert.Equal(0,job.Completed);
            var str=companion.RawStr; var items=companion.Backpack.TotalItems;
            job.Tick(now+TimeSpan.FromMinutes(5),false);
            Assert.Equal(1,job.Completed); Assert.True(companion.RawStr>str); Assert.True(companion.Backpack.TotalItems>items);
            Assert.Equal(5,companion.Backpack.FindItemByType<HavenMark>().Amount);
            var count=companion.Backpack.TotalItems;
            job.Tick(now+TimeSpan.FromMinutes(5),false); Assert.Equal(1,job.Completed); Assert.Equal(count,companion.Backpack.TotalItems);
            Assert.All(equipped,item=>Assert.Equal(companion,item.Parent));
            Assert.True(job.Running);
        }
        finally { companion.Delete();owner.Delete(); }
    }
    [Fact]
    public void LoginStopsGrindingAndAutomaticAfkDeparture()
    {
        var owner=new PlayerMobile { Player=true,Body=400 }; var companion=new HavenCompanion { BoundOwner=owner };
        try
        {
            companion.SetControlMaster(owner);
            var idle=HavenCompanionIdleMissions.Ensure(companion); idle.Afk=true;
            var job=HavenCompanionGearAssignment.Begin(companion,Core.Now);
            idle.Tick(Core.Now+TimeSpan.FromHours(1),true); Assert.Null(companion.Expedition);
            owner.MoveToWorld(new Point3D(3505,2582,14),Map.Trammel);
            job.Tick(Core.Now+TimeSpan.FromMinutes(2),true);
            Assert.False(job.Running); Assert.False(idle.Afk); Assert.False(idle.Enabled);
            Assert.Equal(owner.Map,companion.Map);Assert.Equal(owner.Location,companion.Location);
            job.Tick(Core.Now+TimeSpan.FromHours(2),false); Assert.Equal(0,job.Completed);
        }
        finally { companion.Delete();owner.Delete(); }
    }
    [Fact]
    public void FullPackStopsWithoutCreatingOrDroppingRewards()
    {
        var owner=new PlayerMobile();var companion=new HavenCompanion { BoundOwner=owner };
        try
        {
            var job=HavenCompanionGearAssignment.Begin(companion,Core.Now);
            companion.Backpack.MaxItems=companion.Backpack.TotalItems+10;
            var items=companion.Backpack.TotalItems;
            job.Tick(Core.Now+TimeSpan.FromMinutes(5),false);
            Assert.False(job.Running);Assert.Equal(0,job.Completed);Assert.Equal(items,companion.Backpack.TotalItems);
        }
        finally {companion.Delete();owner.Delete();}
    }
    [Fact]
    public void AssignmentDeadlineAndEarnedRunCountSurviveSerialization()
    {
        var owner=new PlayerMobile();var companion=new HavenCompanion { BoundOwner=owner };
        var copy=new HavenCompanionGearAssignment(World.NewItem);
        try
        {
            var job=HavenCompanionGearAssignment.Begin(companion,Core.Now);
            job.Tick(Core.Now+TimeSpan.FromMinutes(5),false);
            var writer=new BufferWriter(true);job.Serialize(writer);
            copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0,(int)writer.Position).ToArray()));
            Assert.Equal(job.NextReward,copy.NextReward);Assert.Equal(1,copy.Completed);Assert.True(copy.Running);
            Assert.Equal(companion,copy.Companion);
        }
        finally {copy.Delete();companion.Delete();owner.Delete();}
    }
    [Fact]
    public void PreviouslyFinishedTripIsCollectedOfflineOnceWithoutMovingOwner()
    {
        var owner=new PlayerMobile();var companion=new HavenCompanion { BoundOwner=owner };
        try
        {
            var trip=new HavenCompanionExpedition { Companion=companion,Kind=HavenExpeditionKind.Grind,Started=Core.Now-TimeSpan.FromMinutes(10) };
            companion.Backpack.DropItem(trip);
            Assert.False(trip.Return(owner,Core.Now,automatic:true,offline:true));
            var job=HavenCompanionGearAssignment.Begin(companion,Core.Now);
            job.Tick(Core.Now,false);
            Assert.True(trip.Deleted);Assert.Null(companion.Expedition);Assert.Equal(Map.Internal,owner.Map);
            Assert.Equal(5,companion.Backpack.FindItemByType<HavenMark>().Amount);Assert.Equal(0,job.Completed);
            job.Tick(Core.Now,false);Assert.Equal(5,companion.Backpack.FindItemByType<HavenMark>().Amount);
            job.Tick(Core.Now+TimeSpan.FromMinutes(5),false);Assert.Equal(1,job.Completed);
        }
        finally {companion.Delete();owner.Delete();}
    }
}
