using System;
using System.Linq;
using Server;
using Server.CustomBots;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsOceanBosses
{
    public HavenWorldTestsOceanBosses() { _ = new HavenWorldTestsMarket(); }
    private static PlayerMobile Player()
    {
        var p = new PlayerMobile { Player = true, Body = 400, RawStr = 100, RawInt = 100 };
        p.AddItem(new Backpack { MaxItems = 0 }); p.Hits = p.HitsMax; p.Mana = p.ManaMax; return p;
    }
    [SkippableTheory]
    [InlineData(0)] [InlineData(1)]
    public void OriginalArenasHaveAccessibleArrivalsAndOnePersistentBoss(int kind)
    {
        TileDataRequirement.SkipIfMissing(); var impl = Server.Movement.Movement.Impl;
        var visitor = new PlayerBot(); HavenBossLair lair = null;
        try
        {
            Server.Movement.MovementImpl.Configure(); lair = HavenBossLair.Install(kind);
            Assert.Same(lair, HavenBossLair.Install(kind)); var boss = lair.Boss;
            Assert.NotNull(boss); Assert.True(boss.Alive); Assert.True(boss.AlwaysAttackable);
            Assert.True(Map.Trammel.CanSpawnMobile(lair.Arrival));
            visitor.MoveToWorld(lair.Arrival, Map.Trammel);
            Assert.NotEmpty(HavenDungeonNavigation.Route(visitor, boss.Location));
            lair.Pulse(); Assert.Same(boss, lair.Boss);
            boss.Delete(); Assert.Null(lair.Boss); lair.Pulse(); Assert.Null(lair.Boss);
            lair.NextSpawn = Core.Now - TimeSpan.FromSeconds(1); lair.Pulse(); Assert.NotNull(lair.Boss);
        }
        finally { visitor.Delete(); lair?.Delete(); Server.Movement.Movement.Impl = impl; }
    }
    [SkippableFact]
    public void ScalisRoamingWaterNetChanceAndRaceRefundAreUsable()
    {
        TileDataRequirement.SkipIfMissing(); var p = Player(); var net = new FabledFishingNet(); var second = new FabledFishingNet();
        var hunt = HavenScalisHunt.Ensure(Map.Trammel);
        try
        {
            Assert.True(HavenScalisHunt.SeaPoint(Map.Trammel, HavenScalisHunt.RoamingWaters));
            Assert.Same(hunt, HavenScalisHunt.Ensure(Map.Trammel)); p.Skills.Fishing.Base = 100;
            p.MoveToWorld(new Point3D(4001, 3671, -5), Map.Trammel); p.Backpack.DropItem(net); p.Backpack.DropItem(second);
            Assert.False(HavenScalisHunt.NetResult(net, HavenScalisHunt.RoamingWaters, Map.Trammel, p, .9)); Assert.False(net.Deleted);
            Assert.True(HavenScalisHunt.NetResult(net, HavenScalisHunt.RoamingWaters, Map.Trammel, p, .1)); Assert.True(net.Deleted);
            var boss = hunt.Boss; Assert.NotNull(boss); Assert.False(HavenScalisHunt.CheckNet(p));
            Assert.True(HavenScalisHunt.NetResult(second, HavenScalisHunt.RoamingWaters, Map.Trammel, p, .1));
            Assert.Same(boss, hunt.Boss); Assert.True(second.Deleted); Assert.Single(p.Backpack.Items.OfType<FabledFishingNet>());
            hunt.Pulse(); Assert.Same(boss, hunt.Boss); boss.Delete(); hunt.Pulse(); Assert.Null(hunt.Boss);
        }
        finally { hunt.Delete(); net.Delete(); second.Delete(); p.Delete(); }
    }
    [SkippableTheory]
    [InlineData(0, 40000)] [InlineData(1, 30000)] [InlineData(2, 50000)]
    public void QualifiedDamageEarnsOnePersonalRewardAndNoParticipationGetsNone(int kind, int gold)
    {
        TileDataRequirement.SkipIfMissing(); var p = Player(); var idle = Player();
        HavenScalis boss = kind == 0 ? new HavenScalis() : kind == 1 ? new HavenCora() : new HavenCorgul();
        var pet = new Dog();
        try
        {
            boss.MoveToWorld(new Point3D(1000, 1000, 0), Map.Trammel); p.MoveToWorld(new Point3D(1001, 1000, 0), Map.Trammel); idle.MoveToWorld(p.Location, p.Map);
            pet.MoveToWorld(p.Location, p.Map); pet.SetControlMaster(p);
            boss.Credit(p, 300); Assert.Empty(boss.RewardRecipients()); boss.Credit(pet, 300);
            Assert.Single(boss.RewardRecipients()); Assert.Same(p, boss.RewardRecipients()[0]);
            boss.Award(0, 0, 0); Assert.Equal(gold, p.Backpack.GetAmount(typeof(Gold)));
            Assert.Single(p.Backpack.Items, HavenBossArtifact.IsArtifact);
            Assert.Equal(kind == 0 ? 1 : 0, p.Backpack.Items.OfType<HavenSmallSoulForgeDeed>().Count());
            boss.Award(0, 0, 0); Assert.Equal(gold, p.Backpack.GetAmount(typeof(Gold))); Assert.Empty(idle.Backpack.Items);
        }
        finally { pet.Delete(); boss.Delete(); p.Delete(); idle.Delete(); }
    }
    [SkippableTheory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)] [InlineData(12)] [InlineData(13)] [InlineData(14)] [InlineData(15)]
    public void EveryBossArtifactIsDistinctAndEvolvesWithUsableStats(int kind)
    {
        TileDataRequirement.SkipIfMissing(); var item = HavenScalisLoot.Artifact(kind);
        try
        {
            Assert.True(HavenBossArtifact.IsArtifact(item)); Assert.False(string.IsNullOrEmpty(item.Name));
            var aos = Assert.IsAssignableFrom<IAosItem>(item); var damage = aos.Attributes.SpellDamage;
            HavenGearExperience.Gain(item, 1900); Assert.Equal(20, HavenGearExperience.Find(item).Level);
            Assert.True(aos.Attributes.SpellDamage > damage); var capped = aos.Attributes.SpellDamage;
            HavenGearExperience.Gain(item, 1900); Assert.Equal(capped, aos.Attributes.SpellDamage);
        }
        finally { item.Delete(); }
    }
    [SkippableFact]
    public void SoulbinderOfferingConsumesOnlyOnSuccessAndChartExpires()
    {
        TileDataRequirement.SkipIfMissing(); var p = Player(); var outsider = Player(); var lair = HavenBossLair.Install(1);
        var altar = lair.Fixtures.OfType<HavenSoulbinderAltar>().Single();
        try
        {
            p.MoveToWorld(new Point3D(altar.X, altar.Y + 1, altar.Z), altar.Map);
            var map = new TreasureMap(1, Map.Trammel); p.Backpack.DropItem(map);
            Assert.False(altar.Offer(p)); Assert.False(map.Deleted);
            var world = new WorldMap(); p.Backpack.DropItem(world); Assert.True(altar.Offer(p));
            Assert.True(map.Deleted); Assert.True(world.Deleted); Assert.Equal(1, p.Hits);
            var chart = p.Backpack.Items.OfType<HavenSoulboundChart>().Single(); Assert.False(chart.Enter(outsider));
            p.Hits = p.HitsMax; Assert.True(chart.Enter(p)); Assert.True(p.InRange(lair.Arrival, 3));
            chart.Expires = Core.Now - TimeSpan.FromSeconds(1); Assert.False(chart.Enter(p));
        }
        finally { p.Delete(); outsider.Delete(); lair.Delete(); }
    }
    [SkippableFact]
    public void SoulForgeUsesRealRecipeMaterialsAndNativeAddonComponents()
    {
        TileDataRequirement.SkipIfMissing(); var p = Player(); var forge = new HavenSmallSoulForge(); var gear = new Longsword();
        try
        {
            Assert.Contains(forge.Components, c => c is ForgeComponent);
            forge.MoveToWorld(new Point3D(1000, 1000, 0), Map.Trammel); p.MoveToWorld(new Point3D(1001, 1000, 0), Map.Trammel);
            p.Backpack.DropItem(gear); p.Skills.Blacksmith.Base = 100;
            Assert.False(HavenAbyssArtifice.Apply(forge, p, gear, 0));
            p.Backpack.DropItem(new EssencePrecision(8)); p.Backpack.DropItem(new LavaSerpentCrust(2)); p.Backpack.DropItem(new DaemonClaw(2));
            Assert.True(HavenAbyssArtifice.Apply(forge, p, gear, 0)); Assert.Equal(5, gear.Attributes.WeaponDamage);
            Assert.Equal(0, p.Backpack.GetAmount(typeof(EssencePrecision))); Assert.False(HavenAbyssArtifice.Apply(forge, p, gear, 0));
            var deed = forge.Deed; Assert.IsType<HavenSmallSoulForgeDeed>(deed); deed.Delete();
        }
        finally { forge.Delete(); p.Delete(); }
    }
    [SkippableFact]
    public void BossContributionAndRewardGuardSurviveSerialization()
    {
        TileDataRequirement.SkipIfMissing(); var boss = new HavenCorgul(); var copy = new HavenCorgul(World.NewMobile); var p = Player();
        try
        {
            boss.Contributions[p] = 1234; boss.Rewarded = true;
            var writer = new BufferWriter(true); boss.Serialize(writer);
            copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.True(copy.Rewarded); Assert.Equal(1234, copy.Contributions[p]); Assert.Equal(boss.HitsMax, copy.HitsMax);
        }
        finally { boss.Delete(); copy.Delete(); p.Delete(); }
    }
    [SkippableFact]
    public void CoraRiftOnlyDrainsEngagedTargetsAndCleansUp()
    {
        TileDataRequirement.SkipIfMissing(); var boss = new HavenCora(); var p = Player(); var idle = Player();
        var rift = new HavenCoraRift { Cora = boss, Armed = Core.Now - TimeSpan.FromSeconds(1), Expires = Core.Now + TimeSpan.FromSeconds(9) };
        try
        {
            boss.MoveToWorld(new Point3D(1000, 1000, 0), Map.Trammel); p.MoveToWorld(new Point3D(1001, 1000, 0), Map.Trammel); idle.MoveToWorld(p.Location, p.Map);
            boss.Combatant = p; boss.Credit(p, 600); rift.MoveToWorld(p.Location, p.Map);
            var mana = p.Mana; var idleMana = idle.Mana; rift.Pulse(); Assert.True(p.Mana < mana); Assert.Equal(idleMana, idle.Mana);
            boss.Delete(); rift.Pulse(); Assert.True(rift.Deleted);
        }
        finally { rift.Delete(); boss.Delete(); p.Delete(); idle.Delete(); }
    }
    [SkippableTheory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void NativeCombatAllowsPlayersAndBotsToDamageEveryBoss(int kind)
    {
        TileDataRequirement.SkipIfMissing(); var p = Player(); var bot = new PlayerBot(BotClass.Mage, BotSkillTier.Expert);
        HavenScalis boss = kind == 0 ? new HavenScalis() : kind == 1 ? new HavenCora() : new HavenCorgul();
        var harmful = Mobile.AllowHarmfulHandler; var notoriety = Notoriety.Handler;
        try
        {
            Mobile.AllowHarmfulHandler = Server.Misc.NotorietyHandlers.Mobile_AllowHarmful;
            Notoriety.Handler = Server.Misc.NotorietyHandlers.MobileNotoriety;
            boss.MoveToWorld(new Point3D(1000, 1000, 0), Map.Trammel); p.MoveToWorld(new Point3D(1001, 1000, 0), Map.Trammel); bot.MoveToWorld(p.Location, p.Map);
            Assert.True(p.CanBeHarmful(boss, false)); Assert.True(bot.CanBeHarmful(boss, false));
            var hp = boss.Hits; boss.Damage(600, p); Assert.True(boss.Hits < hp); Assert.Contains(p, boss.RewardRecipients());
        }
        finally { Mobile.AllowHarmfulHandler = harmful; Notoriety.Handler = notoriety; boss.Delete(); p.Delete(); bot.Delete(); }
    }
    [SkippableFact]
    public void CoraApproachSupportsNormalPlayerTravelAndNetTargetRequiresOwnership()
    {
        TileDataRequirement.SkipIfMissing(); var p = Player(); var lair = HavenBossLair.Install(0); var net = new FabledFishingNet();
        try
        {
            p.MoveToWorld(new Point3D(3500, 2570, 20), Map.Trammel);
            Assert.True(HavenFrontierSupport.Travel(p, lair.Arrival, Map.Trammel));
            net.MoveToWorld(p.Location, p.Map); net.OnTarget(p, HavenScalisHunt.RoamingWaters);
            Assert.False(net.InUse); Assert.False(net.Deleted);
        }
        finally { p.Delete(); lair.Delete(); net.Delete(); }
    }
}
