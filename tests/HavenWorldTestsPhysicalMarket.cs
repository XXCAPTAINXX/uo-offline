using System;
using System.Linq;
using Server;
using Server.CustomBots;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsPhysicalMarket
{
    public HavenWorldTestsPhysicalMarket() { _ = new HavenWorldTestsMarket(); }
    private static PlayerMobile Player()
    {
        var player = new PlayerMobile { Player = true, RawStr = 100 }; player.AddItem(new Backpack());
        player.MoveToWorld(new Point3D(3500, 2570, 20), Map.Trammel); return player;
    }
    private static PlayerBot Bot()
    {
        var bot = new PlayerBot(BotClass.Mage, BotSkillTier.Expert);
        bot.MoveToWorld(new Point3D(3500, 2570, 20), Map.Trammel); return bot;
    }
    private static Party Group(PlayerMobile player, PlayerBot bot)
    { var party = new Party(player); player.Party = party; party.Add(bot); return party; }
    [SkippableFact]
    public void GroupLootGoesToTheOwnersCompanionAndFallsBackWithoutLoss()
    {
        TileDataRequirement.SkipIfMissing();
        var player = Player(); var stranger = Player(); var bot = Bot(); var party = Group(player, bot);
        var companion = new HavenCompanion { BoundOwner = player }; var wrong = new HavenCompanion { BoundOwner = stranger };
        try
        {
            companion.SetControlMaster(player); companion.MoveToWorld(player.Location, player.Map);
            wrong.SetControlMaster(stranger); wrong.MoveToWorld(player.Location, player.Map);
            var loot = new Gold(1234); HavenBotLoot.Receive(bot, loot);
            Assert.True(loot.IsChildOf(companion.Backpack)); Assert.False(loot.IsChildOf(wrong.Backpack));
            companion.Backpack.MaxItems = companion.Backpack.TotalItems;
            var artifact = new LegacyOfTheDreadLord(); HavenBotLoot.Receive(bot, artifact);
            Assert.True(artifact.IsChildOf(player.Backpack)); Assert.False(artifact.IsChildOf(bot.Backpack));
            var stolen = new Diamond(); stranger.Backpack.DropItem(stolen); HavenBotLoot.Receive(bot, stolen);
            Assert.True(stolen.IsChildOf(stranger.Backpack));
        }
        finally { party.Disband(); companion.Delete(); wrong.Delete(); bot.Delete(); player.Delete(); stranger.Delete(); }
    }
    [SkippableFact]
    public void NativeDoomAwardToGroupedBotIsDeliveredToPlayerNotMarket()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(); var bot = Bot(); var party = Group(player, bot);
        var artifact = new LegacyOfTheDreadLord();
        try { DemonKnight.DistributeArtifact(bot, artifact); Assert.True(artifact.IsChildOf(player.Backpack)); }
        finally { party.Disband(); artifact.Delete(); bot.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void OfferChecksRecipientAndTransfersNativePartyOwnership()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(); var stranger = Player(); var bot = Bot();
        try
        {
            bot.Behavior = new DungeonCrawlerBehavior();
            Assert.True(BotPlayerParty.CanJoin(bot, out _));
            HavenDungeonCourtesy.OfferTo(bot, player);
            Assert.False(HavenDungeonCourtesy.Reply(bot, stranger, true));
            Assert.True(HavenDungeonCourtesy.Reply(bot, player, true));
            Assert.Same(player, HavenBotLoot.PlayerOwner(bot)); Assert.IsType<PlayerGroupBehavior>(bot.Behavior);
            Assert.False(HavenDungeonCourtesy.Reply(bot, player, true));
        }
        finally { Party.Get(player)?.Disband(); bot.Delete(); player.Delete(); stranger.Delete(); }
    }
    [SkippableFact]
    public void DecliningMakesBotLeaveDungeonWithoutTakingPlayerItems()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(); var bot = Bot();
        try
        {
            bot.MoveToWorld(new Point3D(410, 465, -1), Map.Malas); player.MoveToWorld(bot.Location, bot.Map);
            HavenDungeonCourtesy.OfferTo(bot, player); Assert.True(HavenDungeonCourtesy.Reply(bot, player, false));
            Assert.Same(Map.Trammel, bot.Map); Assert.IsType<TravelerBehavior>(bot.Behavior); Assert.Null(HavenBotLoot.PlayerOwner(bot));
        }
        finally { bot.Delete(); player.Delete(); }
    }
    [SkippableFact]
    public void TimedWorkCannotManufactureAnyDungeonLoot()
    {
        TileDataRequirement.SkipIfMissing(); var stall = new HavenMarketStall(); stall.Setup(HavenMarketTrade.DungeonSupplies);
        try
        {
            var job = HavenMarketExpansion.Get(stall);
            for (var i = 0; i < 200; i++) { job.Step(stall, 0); }
            Assert.Empty(stall.Stock); Assert.Equal(0, job.Completed); Assert.Equal(0, job.Credits);
        }
        finally { stall.Delete(); }
    }
    [SkippableFact]
    public void ManagedBotsMustSolveTheOrchardAndCannotSkipRoofSeals()
    {
        TileDataRequirement.SkipIfMissing(); var bot = Bot(); var crew = new HavenDungeonCrew();
        var room = new HavenShadowChamber(); var roof = new HavenShadowChamber();
        try
        {
            crew.Workers.Add(bot); HavenDungeonCrew.Registry[bot] = crew;
            room.MoveToWorld(new Point3D(4760, 3169, 0), Map.Trammel); room.Build(HavenShadowRoom.Orchard);
            roof.MoveToWorld(new Point3D(4798, 3209, 0), Map.Trammel); roof.Build(HavenShadowRoom.Roof);
            bot.MoveToWorld(room.ExitLocation, room.Map); Assert.False(roof.Start(bot)); Assert.True(room.Start(bot));
            room.Tick(); Assert.True(room.Active);
            for (var i = 0; i < 16; i += 2)
            {
                var node = room.Puzzle.OfType<HavenShadowNode>().Single(n => n.Kind == 1 && n.Key == i);
                bot.MoveToWorld(new Point3D(node.X, node.Y + 1, node.Z), node.Map);
                Assert.True(HavenShadowPuzzles.AssistBot(room, bot));
            }
            Assert.False(room.Active); Assert.Equal(2, HavenFrontierRecord.Get(bot).Rooms); Assert.False(roof.Start(bot));
        }
        finally { room.Delete(); roof.Delete(); crew.Delete(); bot.Delete(); }
    }
    [SkippableFact]
    public void NavigationUsesWalkableCurrentMapAndRestoresMovementSettings()
    {
        TileDataRequirement.SkipIfMissing(); var bot = Bot();
        var ignored = Server.Movement.MovementImpl.AlwaysIgnoreDoors;
        var implementation = Server.Movement.Movement.Impl;
        try
        {
            Server.Movement.MovementImpl.Configure();
            bot.MoveToWorld(new Point3D(410, 465, -1), Map.Malas);
            var goal = new Point3D(471, 428, -1);
            var route = HavenDungeonNavigation.Route(bot, goal);
            Assert.NotEmpty(route); Assert.Equal(goal.X, route[^1].X); Assert.Equal(goal.Y, route[^1].Y);
            Assert.Equal(ignored, Server.Movement.MovementImpl.AlwaysIgnoreDoors);
            bot.Internalize(); Assert.Empty(HavenDungeonNavigation.Route(bot, goal));
        }
        finally { Server.Movement.Movement.Impl = implementation; bot.Delete(); }
    }
    [SkippableFact]
    public void ExpiredCrewReleasesEveryWorkerAndPreservesTheirBelongings()
    {
        TileDataRequirement.SkipIfMissing(); var bot = Bot();
        var crew = new HavenDungeonCrew { Ends = Core.Now - TimeSpan.FromSeconds(1) };
        var gold = new Gold(4321); bot.Backpack.DropItem(gold);
        try
        {
            crew.Workers.Add(bot); HavenDungeonCrew.Registry[bot] = crew; bot.LifecycleExempt = true;
            crew.Tick(bot);
            Assert.True(crew.Deleted); Assert.Null(HavenDungeonCrew.For(bot)); Assert.False(bot.LifecycleExempt);
            Assert.True(gold.IsChildOf(bot.Backpack)); Assert.IsType<TravelerBehavior>(bot.Behavior);
        }
        finally { crew.Delete(); bot.Delete(); }
    }
    [SkippableFact]
    public void EndingBotCrewDoesNotCancelHumanShadowguardRoom()
    {
        TileDataRequirement.SkipIfMissing(); var player = Player(); var bot = Bot(); var room = new HavenShadowChamber();
        var crew = new HavenDungeonCrew { Chamber = room };
        try
        {
            room.MoveToWorld(new Point3D(4760, 3169, 0), Map.Trammel); room.Build(HavenShadowRoom.Orchard);
            player.MoveToWorld(room.ExitLocation, room.Map); Assert.True(room.Start(player));
            crew.Workers.Add(bot); crew.End(false); Assert.True(room.Active); Assert.Contains(player, room.Members);
        }
        finally { crew.Delete(); room.Delete(); bot.Delete(); player.Delete(); }
    }
}
