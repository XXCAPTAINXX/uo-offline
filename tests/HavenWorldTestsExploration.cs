using System;
using Server;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsExploration
{
    public HavenWorldTestsExploration() { _ = new HavenWorldTests(); }
    [SkippableFact]
    public void AtlasUsesSharedDungeonEntrancesAndRetainsTowns()
    {
        TileDataRequirement.SkipIfMissing();
        Assert.Contains(HavenTravelGump.Entries(0, 0), e => e.Name == "New Haven bank and recovery");
        Assert.Contains(HavenTravelGump.Entries(1, 0), e => e.Name.Contains("Destard") && e.Map == Map.Trammel);
        Assert.Contains(HavenTravelGump.Entries(1, 1), e => e.Name.Contains("Deceit") && e.Map == Map.Felucca);
        Assert.NotEmpty(HavenTravelGump.Entries(0, 4));
    }
    [SkippableFact]
    public void GoldenShovelRequiresOwnedDecodedUnfinishedMapAndLeavesTheDigUnfinished()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Body = 0x190 }; player.AddItem(new Backpack());
        var shovel = new HavenGoldenShovel(); var map = new TreasureMap(1, Map.Trammel) { ChestLocation = new Point2D(3579, 2513) };
        player.Backpack.DropItem(shovel); player.Backpack.DropItem(map); player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
        try
        {
            Assert.False(shovel.Travel(player, map)); map.Decoder = player;
            Assert.True(shovel.Travel(player, map)); Assert.False(map.Completed); Assert.True(player.InRange(map.ChestLocation, 2));
            map.Completed = true; Assert.False(shovel.Travel(player, map));
            map.Completed = false; map.Internalize(); Assert.False(shovel.Travel(player, map));
        }
        finally { map.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void ResourceSatchelPropagatesReducedWeightThroughNestedContainersAndRecalculation()
    {
        TileDataRequirement.SkipIfMissing();
        var pack = new Backpack(); var outer = new Bag(); var satchel = new HavenResourceSatchel(); var logs = new Log(501); var gear = new Katana();
        try
        {
            pack.DropItem(outer); outer.DropItem(satchel); var before = pack.TotalWeight;
            satchel.DropItem(logs); var reduced = (logs.PileWeight + 9) / 10;
            Assert.Equal(reduced, satchel.TotalWeight); Assert.Equal(before + reduced, pack.TotalWeight);
            pack.UpdateTotals(); Assert.Equal(before + reduced, pack.TotalWeight);
            logs.Amount = 13; Assert.Equal(before + (logs.PileWeight + 9) / 10, pack.TotalWeight);
            Assert.False(HavenResourceSatchel.Accepts(gear)); Assert.False(HavenResourceSatchel.Accepts(outer));
            pack.DropItem(logs); Assert.Equal(before + logs.PileWeight, pack.TotalWeight); Assert.Equal(0, satchel.TotalWeight);
            pack.UpdateTotals(); Assert.Equal(before + logs.PileWeight, pack.TotalWeight);
        }
        finally { pack.Delete(); gear.Delete(); }
    }
    [SkippableFact]
    public void SatchelAcceptsGemDropsAndKeepsReducedWeightWhenStacksChange()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Body = 400, RawStr = 100 };
        player.AddItem(new Backpack());
        var satchel = new HavenResourceSatchel(); player.Backpack.DropItem(satchel);
        var types = new[] { typeof(Amber), typeof(Amethyst), typeof(Citrine), typeof(Diamond), typeof(Emerald),
            typeof(Ruby), typeof(Sapphire), typeof(StarSapphire), typeof(Tourmaline), typeof(BlueDiamond),
            typeof(BrilliantAmber), typeof(DarkSapphire), typeof(EcruCitrine), typeof(FireRuby), typeof(PerfectEmerald), typeof(ArcaneGem) };
        try
        {
            for (var index = 0; index < types.Length; index++)
            {
                var gem = types[index].CreateInstance<Item>();
                Assert.NotNull(gem);
                gem.Amount = 100;
                player.Backpack.DropItem(gem);
                try
                {
                    var baseline = player.Backpack.TotalWeight - gem.PileWeight;
                    var accepted = (index % 3) switch
                    {
                        0 => satchel.TryDropItem(player, gem, false),
                        1 => satchel.TryDropItem(player, gem, false, false),
                        _ => satchel.OnDragDropInto(player, gem, new Point3D(50, 50, 0))
                    };
                    Assert.True(accepted, types[index].Name);
                    Assert.Same(satchel, gem.Parent);
                    Assert.Equal(100, gem.Amount);
                    Assert.Equal(baseline + (gem.PileWeight + 9) / 10, player.Backpack.TotalWeight);
                    gem.Amount = 37;
                    player.Backpack.UpdateTotals();
                    Assert.Equal(baseline + (gem.PileWeight + 9) / 10, player.Backpack.TotalWeight);
                    player.Backpack.DropItem(gem);
                    Assert.Equal(baseline + gem.PileWeight, player.Backpack.TotalWeight);
                    Assert.Equal(0, satchel.TotalWeight);
                }
                finally { gem.Delete(); }
            }
        }
        finally { player.Delete(); }
    }
    [SkippableFact]
    public void SatchelIsAvailableForWalletGold()
    {
        TileDataRequirement.SkipIfMissing();
        var menu = new ArcaneSupplyStone.Menu();
        Assert.Contains(menu.Entries, e => e.Name.Contains("Resource satchel"));
        var item = menu.CreateItem(21); try { Assert.IsType<HavenResourceSatchel>(item); } finally { item.Delete(); }
    }
    [SkippableFact]
    public void EndlessBandageUsesNativeHealingWithoutConsumption()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Body = 0x190, RawStr = 100, RawDex = 100 }; player.AddItem(new Backpack());
        var bandage = new HavenEndlessBandage(); player.Backpack.DropItem(bandage); player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
        player.Skills.Healing.Base = 100; player.Skills.Anatomy.Base = 100; player.Hits = 50;
        try
        {
            Bandage.BandageTargetRequest(player, bandage, player);
            Assert.False(bandage.Deleted); Assert.Equal(1, bandage.Amount); Assert.False(bandage.Stackable);
            bandage.Consume(500); Assert.False(bandage.Deleted); Assert.Equal(1, bandage.Amount);
        }
        finally { player.Delete(); }
    }
    [SkippableFact]
    public void WorldDiscoveriesIncludeWeakWildMobsButExcludePetsAndReplay()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Body = 0x190 }; player.AddItem(new Backpack()); var rabbit = new Rabbit();
        player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); rabbit.MoveToWorld(player.Location, player.Map);
        try
        {
            Assert.True(HavenWorldDiscoveries.Eligible(rabbit, player));
            Assert.True(HavenWorldDiscoveries.Award(rabbit, player, 0));
            var count = player.Backpack.Items.Count; Assert.False(HavenWorldDiscoveries.Award(rabbit, player, 0)); Assert.Equal(count, player.Backpack.Items.Count);
            rabbit.SetControlMaster(player); Assert.False(HavenWorldDiscoveries.Eligible(rabbit, player));
        }
        finally { rabbit.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void ClothingTiersHaveMeaningfulBuildBonusesAndOnlyLegendaryEvolves()
    {
        TileDataRequirement.SkipIfMissing();
        for (var profile = 0; profile < 7; profile++)
        {
            var common = HavenWorldDiscoveries.Clothing(0, profile); var legendary = HavenWorldDiscoveries.Clothing(.999, profile);
            try
            {
                Assert.True(legendary.Attributes.BonusStr > common.Attributes.BonusStr);
                Assert.True(legendary.SkillBonuses.GetBonus(0) > common.SkillBonuses.GetBonus(0));
                Assert.False(HavenGearExperience.IsSpecial(common)); Assert.True(HavenGearExperience.IsSpecial(legendary));
            }
            finally { common.Delete(); legendary.Delete(); }
        }
    }
    [SkippableFact]
    public void SeaHorseEnablesRealWaterMovementFishingAndSafeDismount()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Body = 0x190, RawStr = 100, RawDex = 100 }; player.AddItem(new Backpack());
        var steed = new HavenTideSteed(); var pole = new FishingPole(); player.Backpack.DropItem(pole);
        var cargo = new IronIngot(50); var stranger = new PlayerMobile();
        player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); steed.MoveToWorld(player.Location, player.Map); steed.SetControlMaster(player);
        var origin = player.Location;
        player.Skills.Fishing.Base = 50;
        var fishingBefore = player.Skills.Fishing.Value;
        try
        {
            steed.Rider = player; Assert.True(player.CanSwim); Assert.Equal(fishingBefore + 10, player.Skills.Fishing.Value);
            Assert.True(Fishing.System.CheckHarvest(player, pole));
            steed.Backpack.DropItem(cargo); Assert.False(HavenTideCargoGump.Withdraw(steed, stranger, cargo)); Assert.True(HavenTideCargoGump.Withdraw(steed, player, cargo));
            player.MoveToWorld(new Point3D(4237, 2990, -5), Map.Trammel); player.Direction = Direction.South;
            Assert.True(player.Move(Direction.South));
            steed.Rider = null; Assert.False(player.CanSwim); Assert.Equal(fishingBefore, player.Skills.Fishing.Value); Assert.Equal(origin, player.Location);
        }
        finally { steed.Delete(); player.Delete(); stranger.Delete(); }
    }
}
