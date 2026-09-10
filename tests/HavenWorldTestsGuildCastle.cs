using System;
using System.Linq;
using Server;
using Server.CustomBots;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsGuildCastle
{
    public HavenWorldTestsGuildCastle() { _=new HavenWorldTestsMarket(); BaseHouse.Configure(); }
    private static HavenGuildCastle Build(PlayerMobile owner)
    {
        var house=new HavenGuildCastle(owner);house.MoveToWorld(new Point3D(4196,2868,0),Map.Trammel);
        try { house.Furnish();return house; } catch { house.Delete();throw; }
    }
    private static void Clean(HavenGuildCastle house)
    { foreach(var item in house.CompanyFixtures.ToArray()) { item.Delete(); } house.Delete(); }
    [SkippableFact]
    public void CastleUsesThreeDecksAndFurnishingDoesNotRepeat()
    {
        TileDataRequirement.SkipIfMissing();var owner=new PlayerMobile{Body=400};owner.AddItem(new Backpack());
        var house=Build(owner);
        try
        {
            var count=house.CompanyFixtures.Count;house.Furnish();Assert.Equal(count,house.CompanyFixtures.Count);
            Assert.Equal(31,house.Components.Width);Assert.Equal(13,house.MasterStorage.FindLinked().Count);
            Assert.Contains(house.CompanyFixtures,i=>i is LoomSouthAddon);Assert.Contains(house.CompanyFixtures,i=>i is HavenSmallSoulForge);
            Assert.Contains(house.CompanyFixtures,i=>i is FlourMillSouthAddon);Assert.Contains(house.CompanyFixtures,i=>i is HavenHouseHitchingPost);
            Assert.Contains(house.CompanyFixtures,i=>i is HavenRepairBench);
            owner.MoveToWorld(new Point3D(house.X,house.Y+1,6),Map.Trammel);
            for(var deck=0;deck<3;deck++) { Assert.True(house.ChangeDeck(owner,deck));Assert.Equal(6+deck*20,owner.Z); }
            Assert.All(house.CompanyFixtures.OfType<GuildProfessionChest>(),c=>Assert.True(c.IsSecure));
            Assert.Equal(14,house.Secures.Count);
            Assert.True(house.GetAosMaxSecures() >= 4000);
            foreach (var fixture in house.CompanyFixtures.Where(i=> i is Container or BaseAddon or HavenRepairBench or HavenHouseHitchingPost))
            {
                var reachable=false;
                for(var dx=-2;dx<=2 && !reachable;dx++)
                {
                    for(var dy=-2;dy<=2 && !reachable;dy++)
                    {
                        var point=new Point3D(fixture.X+dx,fixture.Y+dy,fixture.Z);
                        if(!house.Map.CanSpawnMobile(point)) { continue; }
                        owner.MoveToWorld(point,house.Map);reachable=owner.InLOS(fixture);
                    }
                }
                Assert.True(reachable,$"Unreachable fixture {fixture.Name ?? fixture.GetType().Name} at {fixture.Location}");
            }
        }
        finally { Clean(house);owner.Delete(); }
    }
    [SkippableFact]
    public void HomeTrialInstallsOnceAndRunsShortPirateWavesAwayFromHouse()
    {
        TileDataRequirement.SkipIfMissing();var player=new PlayerMobile{Body=400};player.AddItem(new Backpack());
        var estate=new HavenPirateEstate();estate.MoveToWorld(HavenPirateEstate.Site,Map.Trammel);
        estate.Fixtures.Add(new Item(1));
        try
        {
            estate.EnsureHomeTrial(); var trial=estate.HomeTrial; Assert.NotNull(trial);
            estate.EnsureHomeTrial(); Assert.Same(trial,estate.HomeTrial);
            Assert.True(trial.X + 12 < estate.X + 53);
            player.MoveToWorld(new Point3D(trial.X+1,trial.Y,trial.Z),trial.Map);
            trial.OnDoubleClick(player); Assert.Equal(1,trial.Stage);Assert.Equal(3,HavenTrialTheme.Get(trial));
            for(var stage=1;stage<=3;stage++)
            {
                for(var kill=0;kill<6;kill++)
                {
                    trial.Tick(); var mob=trial.Creatures.First();Assert.Contains("Blackwake",mob.Name);
                    trial.Defeated(mob);mob.Delete();
                }
                Assert.Equal(stage+1,trial.Stage);
            }
            trial.Tick();var boss=Assert.Single(trial.Creatures);Assert.Contains("Captain Blackwake",boss.Name);
            trial.Defeated(boss);boss.Delete();Assert.Equal(0,trial.Stage);Assert.True(trial.ReadyAt > Core.Now);
        }
        finally { estate.Delete();player.Delete(); }
    }
    [SkippableFact]
    public void SorterRoutesActualDeedsAndRejectsStrangersWithoutMovingTheirItems()
    {
        TileDataRequirement.SkipIfMissing();var owner=new PlayerMobile{Body=400};owner.AddItem(new Backpack());
        var stranger=new PlayerMobile{Body=400};stranger.AddItem(new Backpack());var house=Build(owner);
        try
        {
            foreach(var m in new[]{owner,stranger}) { m.MoveToWorld(new Point3D(house.X+2,house.Y+1,6),Map.Trammel); }
            var deed=new CommodityDeed(new IronIngot(123));owner.Backpack.DropItem(deed);
            Assert.Same(house, BaseHouse.FindHouseAt(house.MasterStorage));
            Assert.True(house.MasterStorage.CanAccess(owner), "Owner must reach the receiving chest");
            Assert.True(BaseHouse.CheckAccessible(owner,house.MasterStorage));
            Assert.False(BaseHouse.CheckAccessible(stranger,house.MasterStorage));
            Assert.True(house.MasterStorage.TryDeposit(owner,deed));
            var chest=Assert.IsType<GuildProfessionChest>(deed.Parent);Assert.Equal(GuildStorageRole.Smithing,chest.Role);
            Assert.Equal(123,deed.Commodity.Amount);Assert.False(deed.Deleted);
            var other=new CommodityDeed(new Board(42));stranger.Backpack.DropItem(other);
            Assert.False(house.MasterStorage.TryDeposit(stranger,other));Assert.Same(stranger.Backpack,other.Parent);
            Assert.False(house.ChangeDeck(stranger,2));
            Assert.Equal(0,house.MasterStorage.SortAll(stranger));
            var stash=new Item(1);owner.Backpack.DropItem(stash);Assert.True(house.MasterStorage.TryDeposit(owner,stash));
            Assert.Equal(GuildStorageRole.Unsorted,Assert.IsType<GuildProfessionChest>(stash.Parent).Role);
        }
        finally { Clean(house);owner.Delete();stranger.Delete(); }
    }
}
