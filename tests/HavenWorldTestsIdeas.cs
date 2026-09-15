using System;
using Server;
using Server.CustomBots;
using Server.Engines.Craft;
using Server.Engines.PartySystem;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Spells.Second;
using Server.Spells.Fourth;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsIdeas
{
    public HavenWorldTestsIdeas() { _ = new HavenWorldTestsMarket(); }
    private static PlayerMobile Player()
    {
        var player = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100, RawDex = 100, RawInt = 100 };
        player.AddItem(new Backpack()); player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); return player;
    }
    [SkippableFact]
    public void DirectorySearchAndRemotePurchasesUseRealStockAndRejectChangedPricesAndReplay()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(); var wallet = new AdventurersWallet { Balance = 10000 }; player.Backpack.DropItem(wallet);
        var stall = new HavenMarketStall(); stall.MoveToWorld(new Point3D(4196, 2868, 0), Map.Trammel); stall.Setup(HavenMarketTrade.Smith);
        var sword = new Katana { Name = "Test runic katana" }; stall.ListItem(sword, 1000);
        try
        {
            var found = HavenMarketDirectory.Search("runic", (int)HavenMarketTrade.Smith);
            Assert.Contains(found, row => row.Item == sword && row.Price == 1000);
            Assert.DoesNotContain(HavenMarketDirectory.Search("runic", (int)HavenMarketTrade.Cook), row => row.Item == sword);
            Assert.False(stall.Buy(player, sword, 1000)); Assert.Equal(10000, wallet.Balance);
            stall.Prices[0] = 1200; Assert.False(stall.Buy(player, sword, 1000, true)); Assert.Equal(10000, wallet.Balance);
            Assert.True(stall.Buy(player, sword, 1200, true)); Assert.Equal(8800, wallet.Balance); Assert.True(sword.IsChildOf(player.Backpack));
            Assert.False(stall.Buy(player, sword, 1200, true)); Assert.Equal(8800, wallet.Balance);
            Assert.DoesNotContain(HavenMarketDirectory.Search("runic"), row => row.Item == sword);
        }
        finally { stall.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void GuildOfficersCanOrderButNeedSeparateTreasuryPermissionAndLoseItOnLeaving()
    {
        TileDataRequirement.SkipIfMissing(); var leader = Player(); var officer = Player(); var outsider = Player();
        var guild = new Guild(leader, "Fellowship tests", "FT"); guild.AddMember(officer); officer.GuildRank = RankDefinition.Ranks[2];
        var bot = new PlayerBot(BotClass.Miner, BotSkillTier.Expert); bot.MoveToWorld(leader.Location, leader.Map);
        var crew = HavenGuildCrew.Recruit(officer, bot);
        try
        {
            Assert.NotNull(crew); Assert.True(crew.SetJob(officer, HavenGuildJob.Mine)); Assert.False(crew.SetJob(outsider, HavenGuildJob.Hunt));
            crew.Resources.Add(1000); Assert.False(crew.Withdraw(officer, 0, 100));
            Assert.False(crew.SetTreasurer(officer, officer)); Assert.True(crew.SetTreasurer(leader, officer));
            Assert.True(crew.Withdraw(officer, 0, 100)); Assert.Equal(900, crew.Resources[0]);
            guild.RemoveMember(officer); Assert.False(crew.CanManage(officer)); Assert.False(crew.CanWithdraw(officer));
            Assert.False(crew.Withdraw(outsider, 0, 100));
        }
        finally { crew?.Delete(); bot.Delete(); guild.Disband(); leader.Delete(); officer.Delete(); outsider.Delete(); }
    }
    [SkippableFact]
    public void DismissingAnOfficerRecruitCannotBypassTreasuryPermission()
    {
        TileDataRequirement.SkipIfMissing(); var leader = Player(); var officer = Player();
        var guild = new Guild(leader, "Dismissal tests", "DT"); guild.AddMember(officer); officer.GuildRank = RankDefinition.Ranks[2];
        var bot = new PlayerBot(BotClass.Miner, BotSkillTier.Expert); bot.MoveToWorld(leader.Location, leader.Map);
        var crew = HavenGuildCrew.Recruit(officer, bot); var sword = new Katana();
        try
        {
            Assert.NotNull(crew); Assert.True(crew.CanManage(officer)); Assert.False(crew.CanWithdraw(officer));
            bot.Backpack.DropItem(sword); crew.Dismiss(officer);
            Assert.Null(crew.Worker); Assert.True(sword.IsChildOf(leader.BankBox)); Assert.False(sword.IsChildOf(officer.Backpack));
        }
        finally { crew?.Delete(); sword.Delete(); bot.Delete(); guild.Disband(); leader.Delete(); officer.Delete(); }
    }
    [SkippableFact]
    public void GuildCraftingConsumesExactResourcesStoresActualProductAndCannotWithdrawTwice()
    {
        TileDataRequirement.SkipIfMissing(); var leader = Player(); var guild = new Guild(leader, "Guild crafts", "GC");
        var bot = new PlayerBot(BotClass.Miner, BotSkillTier.Expert); bot.MoveToWorld(leader.Location, leader.Map);
        bot.Skills.Blacksmith.Base = 100; bot.Skills.ArmsLore.Base = 100;
        var crew = HavenGuildCrew.Recruit(leader, bot);
        try
        {
            Assert.NotNull(crew); crew.Resources.Add(1000);
            CraftItem recipe = null; foreach (var item in DefBlacksmithy.CraftSystem.CraftItems) { if (item.ItemType == typeof(Katana)) { recipe = item; break; } }
            Assert.NotNull(recipe); var needed = recipe.Resources[0].Amount;
            Assert.True(HavenGuildCrafting.Craft(crew, DefBlacksmithy.CraftSystem, recipe, 0));
            Assert.Equal(1000 - needed, crew.Resources[0]); Assert.Single(crew.Products); Assert.Equal(1, crew.CraftsCompleted);
            var product = crew.Products[0]; Assert.IsType<Katana>(product);
            Assert.True(HavenGuildCrafting.TakeProduct(crew, leader, product)); Assert.False(HavenGuildCrafting.TakeProduct(crew, leader, product));
            Assert.True(product.IsChildOf(leader.Backpack));
        }
        finally { crew?.Delete(); bot.Delete(); guild.Disband(); leader.Delete(); }
    }
    [SkippableFact]
    public void PartySupportIncludesPetsAndPrioritizesNativeCureAndHealingOnlyForAllies()
    {
        TileDataRequirement.SkipIfMissing(); using var poisons = new HavenPoisonTestScope(); var leader = Player(); var outsider = Player();
        leader.MoveToWorld(new Point3D(4196, 2868, 0), Map.Trammel);
        var bot = new PlayerBot(BotClass.Mage, BotSkillTier.Grandmaster); bot.MoveToWorld(leader.Location, leader.Map);
        bot.RawInt = 200; bot.Mana = bot.ManaMax; bot.Skills.Magery.Base = 100;
        var party = new Party(leader); leader.Party = party; party.Add(bot);
        var pet = new Dog(); pet.SetControlMaster(leader); pet.MoveToWorld(new Point3D(leader.X + 1, leader.Y, leader.Z), leader.Map);
        try
        {
            pet.Hits = 1;
            Assert.True(HavenPartySupport.Ally(bot, pet)); Assert.False(HavenPartySupport.Ally(bot, outsider));
            Assert.True(bot.InLOS(pet), "Party patient must be visible");
            Assert.True(bot.CanBeBeneficial(pet, false, true), "Native beneficial checks must allow party pet healing");
            Assert.True(HavenPartySupport.Need(bot, pet) > 0); Assert.IsType<GreaterHealSpell>(HavenPartySupport.ChooseSpell(bot, pet));
            pet.Poison = Poison.Regular; Assert.IsType<CureSpell>(HavenPartySupport.ChooseSpell(bot, pet));
            bot.Mana = 0; Assert.Null(HavenPartySupport.ChooseSpell(bot, pet));
            party.Remove(bot); Assert.False(HavenPartySupport.Ally(bot, pet)); Assert.Null(HavenPartySupport.ChooseSpell(bot, pet));
        }
        finally { pet.Poison = null; pet.Delete(); party.Disband(); bot.Delete(); leader.Delete(); outsider.Delete(); }
    }
}
