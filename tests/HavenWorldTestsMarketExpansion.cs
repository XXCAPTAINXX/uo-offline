using System;
using Server;
using Server.CustomBots;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsMarketExpansion
{
    public HavenWorldTestsMarketExpansion() { _ = new HavenWorldTestsMarket(); }
    private static PlayerMobile Player()
    {
        var player = new PlayerMobile { Player = true, RawStr = 100 };
        player.AddItem(new Backpack()); player.MoveToWorld(new Point3D(3500, 2570, 20), Map.Trammel);
        return player;
    }
    private static HavenMarketStall Stall(HavenMarketTrade trade)
    {
        var stall = new HavenMarketStall(); stall.MoveToWorld(new Point3D(3500, 2570, 20), Map.Trammel); stall.Setup(trade); return stall;
    }
    [SkippableFact]
    public void MinaxNotesConserveCreditsAndRejectForeignOrFullPack()
    {
        TileDataRequirement.SkipIfMissing();
        var player = Player(); var other = Player();
        try
        {
            var record = HavenFrontierRecord.Get(player); record.MinaxCredits = 100;
            Assert.False(HavenMinaxCreditNote.Withdraw(player, 101));
            Assert.False(HavenMinaxCreditNote.Withdraw(player, -10));
            Assert.True(HavenMinaxCreditNote.Withdraw(player, 40));
            var note = player.Backpack.FindItemByType<HavenMinaxCreditNote>();
            Assert.Equal(60, record.MinaxCredits); Assert.Equal(40, note.Amount);
            Assert.False(note.Redeem(other)); Assert.True(note.Redeem(player)); Assert.False(note.Redeem(player));
            Assert.Equal(100, record.MinaxCredits);
            player.Backpack.MaxItems = 1; player.Backpack.DropItem(new Dagger());
            Assert.False(HavenMinaxCreditNote.Withdraw(player, 20)); Assert.Equal(100, record.MinaxCredits);
        }
        finally { player.Delete(); other.Delete(); }
    }
    [SkippableFact]
    public void CapturedPetRetainsStatsAndTransfersOnlyAfterPurchase()
    {
        TileDataRequirement.SkipIfMissing();
        var stall = Stall(HavenMarketTrade.Pets); var buyer = Player();
        var tamer = new PlayerBot(BotClass.Tamer, BotSkillTier.Expert); var pet = new Horse();
        try
        {
            pet.SetControlMaster(tamer); pet.Owners.Add(tamer); var dex = pet.RawDex;
            Assert.True(HavenMarketPets.Consign(tamer, pet));
            var ticket = Assert.IsType<HavenMarketPetTicket>(Assert.Single(stall.Stock));
            Assert.True(new HavenAnimalLoreGump(buyer, pet).CanRefresh(buyer));
            Assert.Equal(Map.Internal, pet.Map); Assert.False(pet.Controlled); Assert.False(ticket.Claim(buyer));
            // Use the real wallet-backed market transaction.
            buyer.Backpack.DropItem(new Gold(stall.Prices[0]));
            Assert.True(stall.Buy(buyer, ticket, stall.Prices[0], true));
            buyer.FollowersMax = 0; Assert.False(ticket.Claim(buyer)); Assert.False(ticket.Deleted);
            buyer.FollowersMax = 5; Assert.True(ticket.Claim(buyer));
            Assert.True(ticket.Deleted); Assert.Same(buyer, pet.ControlMaster); Assert.Equal(dex, pet.RawDex);
            Assert.False(pet.IsStabled); Assert.DoesNotContain(tamer, pet.Owners);
            Assert.True(pet.BondingBegin < Core.Now - pet.BondingDelay);
        }
        finally { stall.Delete(); pet.Delete(); tamer.Delete(); buyer.Delete(); }
    }
    [SkippableFact]
    public void PlayerOwnedPetsCannotBeConsignedOrDeletedByStaleTickets()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Player(); var bot = new PlayerBot(BotClass.Tamer, BotSkillTier.Expert); var horse = new Horse();
        var ticket = new HavenMarketPetTicket { Pet = horse };
        try
        {
            horse.SetControlMaster(bot); horse.Owners.Add(owner);
            Assert.False(HavenMarketPets.Consign(bot, horse));
            horse.SetControlMaster(owner); horse.MoveToWorld(owner.Location, owner.Map);
            owner.Backpack.DropItem(ticket); Assert.False(ticket.Claim(owner));
            ticket.Delete(); Assert.False(horse.Deleted); Assert.Same(owner, horse.ControlMaster);
            Assert.False(BotTaming.IsGoodQuarry(horse, bot));
        }
        finally { ticket.Delete(); horse.Delete(); bot.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void ExpeditionProgressAndCreditsSurviveSerialization()
    {
        TileDataRequirement.SkipIfMissing();
        var job = new HavenMarketExpedition { Route = 2, Progress = 39, Credits = 45, Completed = 17, Failed = 3 };
        var copy = new HavenMarketExpedition(World.NewItem);
        try
        {
            var writer = new BufferWriter(true); job.Serialize(writer);
            copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.Equal(2, copy.Route); Assert.Equal(39, copy.Progress); Assert.Equal(45, copy.Credits);
            Assert.Equal(17, copy.Completed); Assert.Equal(3, copy.Failed);
        }
        finally { job.Delete(); copy.Delete(); }
    }
    [SkippableFact]
    public void ExpansionAddsMissingTradesWithoutResettingExistingStock()
    {
        TileDataRequirement.SkipIfMissing();
        var center = new HavenCommunityCenter();
        try
        {
            center.MoveToWorld(new Point3D(1000, 1000, 100), Map.Malas); center.Build();
            var first = (HavenMarketStall)center.Fixtures.Find(i => i is HavenMarketStall { Trade: HavenMarketTrade.Smith });
            var item = new Dagger(); first.ListItem(item, 1234);
            foreach (var fixture in center.Fixtures.ToArray())
            { if (fixture is HavenMarketStall { Trade: >= HavenMarketTrade.Pets }) { fixture.Delete(); } }
            center.EnsureMarketTrades(); var count = center.Fixtures.Count;
            center.EnsureMarketTrades(); Assert.Equal(count, center.Fixtures.Count);
            Assert.Equal(16, center.Fixtures.FindAll(i => i is HavenMarketStall { Deleted: false }).Count);
            Assert.Same(first, item.Parent); Assert.Equal(1234, first.Prices[0]);
        }
        finally { center.Delete(); }
    }
    [SkippableFact]
    public void PetSuppliesNeedGatheringThenConsumeMaterialsAndProduceAllThreeItems()
    {
        TileDataRequirement.SkipIfMissing();
        var stall = Stall(HavenMarketTrade.PetSupplies);
        try
        {
            var job = HavenMarketExpansion.Get(stall);
            for (var i = 0; i < 3; i++)
            {
                job.Progress = 4; Assert.False(job.Step(stall, 0)); Assert.Equal(i, stall.Stock.Count);
                job.Progress = 4; Assert.True(job.Step(stall, 0)); Assert.Equal(i + 1, stall.Stock.Count);
            }
            Assert.Contains(stall.Stock, i => i is HavenBondingPotion);
            Assert.Contains(stall.Stock, i => i is HavenPetLeash);
            Assert.Contains(stall.Stock, i => i is HavenHouseHitchingPost);
            Assert.Equal(0, stall.Artisan.Backpack.GetAmount(typeof(Ginseng)));
            Assert.Equal(0, stall.Artisan.Backpack.GetAmount(typeof(Leather)));
            Assert.Equal(0, stall.Artisan.Backpack.GetAmount(typeof(IronIngot)));
        }
        finally { stall.Delete(); }
    }
    [SkippableFact]
    public void MarketTicketPersistenceKeepsTheSameAnimalAndStats()
    {
        TileDataRequirement.SkipIfMissing();
        var pet = new Horse { RawDex = 187 }; pet.Internalize();
        var ticket = new HavenMarketPetTicket { Pet = pet }; var copy = new HavenMarketPetTicket(World.NewItem);
        try
        {
            var writer = new BufferWriter(true); ticket.Serialize(writer);
            copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.Same(pet, copy.Pet); Assert.Equal(187, copy.Pet.RawDex); Assert.Null(copy.Owner);
        }
        finally { copy.Pet = null; copy.Delete(); ticket.Delete(); pet.Delete(); }
    }
}
