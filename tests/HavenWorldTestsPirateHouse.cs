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
            Assert.Equal(31, house.Components.Width); Assert.Equal(32, house.Components.Height);
            Assert.True(house.Components.List.Length > 2000); Assert.Same(owner, house.Owner); Assert.True(house.GetAosMaxSecures() >= 10000);
            Assert.Equal(3, house.CompanyFixtures.OfType<HavenPirateStair>().Count());
            Assert.DoesNotContain(house.CompanyFixtures, i => i is HavenCompanyLadder or HavenCompanyCharter);
            owner.MoveToWorld(new Point3D(house.X, house.Y + 1, 7), house.Map);
            for (var deck = 0; deck < 3; deck++) { Assert.True(house.ChangeDeck(owner, deck)); Assert.Equal(7 + deck * 20, owner.Z); }
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
