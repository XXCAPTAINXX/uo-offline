using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
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
public class HavenWorldTestsPirateHouse
{
    public HavenWorldTestsPirateHouse() { _ = new HavenWorldTestsMarket(); BaseHouse.Configure(); }
    [Fact]
    public void ManaLeechStartsUsefulScalesTo100AndNeverAddsTwice()
    {
        var weapon = new ApprenticeBlade();
        try
        {
            Assert.Equal(20,weapon.WeaponAttributes.HitLeechMana);
            weapon.Level=10; StarterWeaponProgression.ApplyBonuses(weapon,weapon); Assert.Equal(57,weapon.WeaponAttributes.HitLeechMana);
            weapon.Level=20; StarterWeaponProgression.ApplyBonuses(weapon,weapon); Assert.Equal(100,weapon.WeaponAttributes.HitLeechMana);
            StarterWeaponProgression.ApplyBonuses(weapon,weapon); Assert.Equal(100,weapon.WeaponAttributes.HitLeechMana);
        }
        finally { weapon.Delete(); }
        var ordinary = new Longsword(); var legendary = new Longsword(); legendary.AddItem(new HavenLegendaryArtifact());
        try { HavenGearExperience.Gain(ordinary,1900); Assert.Equal(0,ordinary.WeaponAttributes.HitLeechMana); HavenGearExperience.Gain(legendary,1900); Assert.Equal(100,legendary.WeaponAttributes.HitLeechMana); }
        finally { ordinary.Delete(); legendary.Delete(); }
    }

    [SkippableFact]
    public void HomePatrolIsSupportedAccessibleIdempotentAndRedeemsCargo()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Body = 400 }; owner.AddItem(new Backpack());
        var estate = new HavenPirateEstate(); estate.MoveToWorld(HavenPirateEstate.Site, Map.Trammel); estate.Fixtures.Add(new Item(1));
        try
        {
            estate.EnsureHomePatrol(); var board = estate.HomePatrol; Assert.NotNull(board);
            var count = estate.Fixtures.Count; estate.EnsureHomePatrol(); Assert.Equal(count, estate.Fixtures.Count);
            Assert.Contains(estate.Fixtures, i => i is Static && i.ItemID == 9 && i.X == board.X && i.Y == board.Y && i.Z == 0);
            Assert.Equal(4, board.Z);
            owner.MoveToWorld(new Point3D(estate.X + 78, estate.Y + 124, 0), estate.Map);
            Assert.True(estate.Map.CanFit(owner.Location,16,checkMobiles:false)); Assert.True(board.CanUse(owner));
            for (var x = 78; x <= 82; x++)
            for (var y = 126; y <= 130; y++) { Assert.True(estate.Map.CanSpawnMobile(new Point3D(estate.X + x, estate.Y + y, 0))); }
            var cargo = new HavenMaritimeCargo { Value = 17 }; owner.Backpack.DropItem(cargo);
            var before = HavenFrontierRecord.Get(owner).Doubloons; cargo.OnDoubleClick(owner);
            Assert.True(cargo.Deleted); Assert.Equal(before + 17, HavenFrontierRecord.Get(owner).Doubloons);
            cargo.OnDoubleClick(owner); Assert.Equal(before + 17, HavenFrontierRecord.Get(owner).Doubloons);
            owner.MoveToWorld(new Point3D(estate.X + 90, estate.Y + 128, 0), estate.Map);
            Assert.False(board.CanUse(owner)); Assert.False(HavenHomePatrolBoard.Nearby(owner));
            File.WriteAllText("E:/(Offline UO)/uo-offline-haven-rc4/artifacts/home-patrol-design.json", JsonSerializer.Serialize(estate.Fixtures.Where(i => i.Visible && i.Map == estate.Map).Select(i => new { id = i.ItemID, x = i.X - estate.X - 75, y = i.Y - estate.Y - 124, z = i.Z })));
        }
        finally { estate.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void CastleMigrationPreservesRealStorageAndBuildsEditableThreeFloorHouse()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Body = 400 }; owner.AddItem(new Backpack());
        var estate = new HavenPirateEstate(); estate.MoveToWorld(HavenPirateEstate.Site, Map.Trammel); estate.Fixtures.Add(new Item(1));
        var old = new HavenGuildCastle(owner) { Estate = estate }; old.MoveToWorld(new Point3D(4196, 2868, 0), Map.Trammel); old.Furnish();
        HavenPirateHeadquarters house = null;
        var master = old.MasterStorage; var chests = master.FindLinked().ToArray();
        var valuables = new Bag(); valuables.DropItem(new Gold(8765)); valuables.DropItem(new HavenGoldenShovel()); chests[0].DropItem(valuables);
        try
        {
            house = HavenPirateHeadquarters.Replace(old);
            Assert.True(old.Deleted); Assert.Same(master, house.MasterStorage); Assert.Equal(13, master.FindLinked().Count);
            Assert.All(chests, chest => { Assert.False(chest.Deleted); Assert.True(chest.IsSecure); Assert.Same(house, BaseHouse.FindHouseAt(chest)); });
            Assert.Same(chests[0], valuables.Parent); Assert.Equal(8765, valuables.FindItemByType<Gold>().Amount);
            Assert.False(valuables.FindItemByType<HavenGoldenShovel>().Deleted);
            // The custom foundation must retain player storage access before a guild exists.
            var coOwner = new PlayerMobile { Body = 400 };
            var stranger = new PlayerMobile { Body = 400 };
            house.CoOwners.Add(coOwner);
            try
            {
                Assert.Null(owner.Guild);
                foreach (var container in house.CompanyFixtures.OfType<Container>())
                {
                    Assert.True(container.IsAccessibleTo(owner), $"Owner cannot access {container.Name}");
                    Assert.True(container.IsAccessibleTo(coOwner), $"Co-owner cannot access {container.Name}");
                    Assert.False(container.IsAccessibleTo(stranger));
                }
                var gold = valuables.FindItemByType<Gold>();
                Assert.True(valuables.IsAccessibleTo(owner));
                Assert.True(gold.IsAccessibleTo(owner));
                Assert.False(gold.IsAccessibleTo(stranger));
                Assert.True(gold.CheckLift(owner));
                Assert.True(owner.Backpack.TryDropItem(owner, gold, false));
                Assert.Same(owner.Backpack, gold.Parent);
                Assert.True(chests[0].TryDropItem(owner, gold, false));
                Assert.Same(chests[0], gold.Parent);
                Assert.Equal(8765, gold.Amount);
            }
            finally
            {
                house.CoOwners.Remove(coOwner);
                coOwner.Delete();
                stranger.Delete();
            }

            Assert.Equal(31, house.Components.Width); Assert.Equal(32, house.Components.Height);
            Assert.True(house.Components.List.Length > 2000); Assert.Same(owner, house.Owner); Assert.True(house.GetAosMaxSecures() >= 10000);
            Assert.Equal(8, house.CompanyFixtures.OfType<HavenPirateStair>().Count());
            Assert.All(house.CompanyFixtures.OfType<HavenPirateStair>(), ladder => Assert.Equal(0x8A5,ladder.ItemID));
            var fixtureCount=house.CompanyFixtures.Count; house.RefineFloorAccess(); house.RefineDirectLadders(); Assert.Equal(fixtureCount,house.CompanyFixtures.Count);
            Assert.DoesNotContain(house.CompanyFixtures, i => i is HavenCompanyLadder or HavenCompanyCharter);
            Assert.Equal(11,house.CompanyFixtures.OfType<HavenCraftStation>().Count());
            var stationCount=house.CompanyFixtures.Count;house.FurnishCraftStations();Assert.Equal(stationCount,house.CompanyFixtures.Count);
            foreach(var station in house.CompanyFixtures.OfType<HavenCraftStation>())
            {
                var reachable=false;
                for(var dx=-2;dx<=2 && !reachable;dx++)
                for(var dy=-2;dy<=2 && !reachable;dy++)
                {
                    var at=new Point3D(station.X+dx,station.Y+dy,station.Z);
                    if(!house.Map.CanFit(at,16,checkMobiles:false)) { continue; }
                    owner.MoveToWorld(at,house.Map);reachable=station.CanOperate(owner);
                }
                Assert.True(reachable,$"Crafting station inaccessible: {station.Name}");
                Assert.True(BaseTool.CheckAccessible(station,owner));
                station.UsesRemaining=0;Assert.False(BaseTool.CheckAccessible(station,owner));station.UsesRemaining=500;
            }
            var press=house.CompanyFixtures.OfType<HavenCraftStation>().First(s=>s.Kind==HavenCraftStationKind.Smithing);
            owner.MoveToWorld(new Point3D(press.X,press.Y+1,press.Z),house.Map);
            var hammer=new SmithHammer(40);owner.Backpack.DropItem(hammer);
            Assert.True(press.Recharge(owner,hammer));Assert.True(hammer.Deleted);Assert.Equal(540,press.UsesRemaining);
            var wrong=new SewingKit(30);owner.Backpack.DropItem(wrong);Assert.False(press.Recharge(owner,wrong));Assert.False(wrong.Deleted);
            press.UsesRemaining=4999;var excess=new SmithHammer(40);owner.Backpack.DropItem(excess);
            Assert.False(press.Recharge(owner,excess));Assert.Equal(40,excess.UsesRemaining);press.UsesRemaining=500;
            owner.MoveToWorld(new Point3D(master.X,master.Y+1,master.Z),house.Map);
            var deposit=new Bag();var sub=new Bag();deposit.DropItem(sub);owner.Backpack.DropItem(deposit);
            var iron=new IronIngot(111);sub.DropItem(iron);
            Assert.Equal(1,HavenStorageAccess.Collect(owner,master,deposit));
            Assert.Contains(iron,HavenStorageAccess.Contents(master));
            Assert.Equal(11,HavenStorageAccess.Withdraw(owner,master,new[]{iron},11));
            Assert.Equal(100,HavenStorageAccess.Contents(master).OfType<IronIngot>().Sum(i=>i.Amount));

            owner.MoveToWorld(new Point3D(house.X, house.Y + 1, 7), house.Map);
            for (var deck = 0; deck < 5; deck++)
            { Assert.True(house.ChangeDeck(owner,deck)); Assert.Equal(HavenPirateHeadquarters.FloorDestinations[deck].Z,owner.Z); }
            foreach (var ladder in house.CompanyFixtures.OfType<HavenPirateStair>())
            {
                var approach = new Point3D(ladder.X,ladder.Y+1,ladder.Z);
                Assert.True(house.Map.CanFit(approach,16,checkMobiles:false),$"Blocked front of ladder {ladder.Name} at {approach}");
                owner.MoveToWorld(approach,house.Map);
                Assert.True(owner.InLOS(ladder),$"Ladder not visible from its front: {ladder.Name}");
                var index=Array.IndexOf(HavenPirateHeadquarters.DirectLadderSites,new Point3D(ladder.X-house.X,ladder.Y-house.Y,ladder.Z-house.Z));
                Assert.True(index>=0);
                var target=HavenPirateHeadquarters.DirectLadderLandings[index];
                ladder.OnDoubleClick(owner);
                Assert.Equal(new Point3D(house.X+target.X,house.Y+target.Y,house.Z+target.Z),owner.Location);

            }
            foreach (var item in house.CompanyFixtures.Where(i => i is Container or BaseAddon or HavenRepairBench or HavenHouseHitchingPost))
            {
                var reachable = false;
                for (var dx = -2; dx <= 2 && !reachable; dx++)
                { for (var dy = -2; dy <= 2 && !reachable; dy++)
                    {
                        var p = new Point3D(item.X + dx, item.Y + dy, item.Z);
                        if (!house.Map.CanSpawnMobile(p)) { continue; }
                        owner.MoveToWorld(p, house.Map); reachable = owner.InLOS(item);
                    }
                }
                Assert.True(reachable, $"Unreachable: {item.GetType().Name} at {item.Location}");
            }
            for (var y = 15; y >= 8; y--) { Assert.True(house.Map.CanSpawnMobile(new Point3D(house.X, house.Y + y, 7)), $"Blocked front entry y={y}"); }
            Assert.True(estate.HomeTrial.Y + 12 < house.Y - 15);
            var writer = new BufferWriter(true); house.CurrentState.Serialize(writer);
            var restored = new DesignState(house, new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.Equal(house.CurrentState.Components.List.Length, restored.Components.List.Length);
            // Review artifact: exact custom components and retained furnishings.
            File.WriteAllText("E:/(Offline UO)/uo-offline-haven-rc4/artifacts/pirate-house-design.json", JsonSerializer.Serialize(new {
                components = house.Components.List.Select(t => new { id = t.ItemId, x = t.OffsetX, y = t.OffsetY, z = t.OffsetZ }),
                fixtures = house.CompanyFixtures.Where(i => i.Visible && i is not BaseAddon).Concat(house.CompanyFixtures.OfType<BaseAddon>().SelectMany(a => a.Components)).Select(i => new { id = i.ItemID, x = i.X - house.X, y = i.Y - house.Y, z = i.Z - house.Z, name = i.Name })
            }));
        }
        finally
        {
            house ??= HavenPirateHeadquarters.Registry.FirstOrDefault(h=>h.Owner==owner);
            if (house != null) { foreach (var item in house.CompanyFixtures.ToArray()) { item.Delete(); } house.Delete(); }
            if (!old.Deleted) { foreach (var item in old.CompanyFixtures.ToArray()) { item.Delete(); } old.Delete(); }
            estate.Delete(); owner.Delete();
        }
    }

    [SkippableFact]
    public void DiscoveryIncreaseTopsUpOldVisitsExactlyOnce()
    {
        TileDataRequirement.SkipIfMissing(); var owner = new PlayerMobile { Player = true, Body = 400 }; owner.AddItem(new Backpack());
        owner.MoveToWorld(new Point3D(4196, 2868, 0), Map.Trammel);
        try
        {
            var account = HavenSovereignAccount.Get(owner);
            HavenSovereigns.CheckProgress(owner);
            account.Award("visit:0:town:Britain", "Old discovery", 10); account.Award("visit:1:dungeon:Despise", "Old discovery", 20);
            var before = account.Balance; HavenSovereigns.CheckProgress(owner); Assert.Equal(before + 45, account.Balance);
            HavenSovereigns.CheckProgress(owner); Assert.Equal(before + 45, account.Balance);
            Assert.Contains("visit2:0:town:Britain", account.Achievements); Assert.DoesNotContain("visit:0:town:Britain", account.Achievements);
        }
        finally { owner.Delete(); }
    }

    [Fact]
    public void LargeCustomDesignOffsetChunksContainEveryTileOnce()
    {
        var tiles = new List<MultiTileEntry>();
        for (var i = 0; i < 1600; i++) { tiles.Add(new MultiTileEntry((ushort)(0x100 + i), (short)(i % 30), (short)(i / 30 % 30), 8, TileFlag.None)); }
        var data = HousePackets.CreateHouseDesignStateDetailed((Serial)0x40000100, 1, new MultiComponentList(tiles));
        var raw = new List<byte>(); var at = 18;
        for (var block = 0; block < data[17]; block++)
        {
            var size = data[at + 1] | (data[at + 3] & 0xF0) << 4;
            var packed = data[at + 2] | (data[at + 3] & 0xF) << 8;
            using var stream = new ZLibStream(new MemoryStream(data, at + 4, packed), CompressionMode.Decompress);
            using var unpacked = new MemoryStream(); stream.CopyTo(unpacked); Assert.Equal(size, unpacked.Length);
            raw.AddRange(unpacked.ToArray()); at += 4 + packed;
        }
        Assert.Equal(8000, raw.Count);
        for (var i = 0; i < 1600; i++) { Assert.Equal(0x100 + i, raw[i * 5] << 8 | raw[i * 5 + 1]); }
    }
}
