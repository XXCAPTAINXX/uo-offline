using System;
using Server;
using Server.CustomBots;
using Server.Engines.BulkOrders;
using Server.Engines.Craft;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Spells;
using Server.Spells.SkillMasteries;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsMarket
{
    public HavenWorldTestsMarket()
    {
        _ = new HavenWorldTests();
        if (Server.Misc.AntiMacroSystem.Settings == null) { Server.Misc.AntiMacroSystem.Configure(); }
        if (DefBlacksmithy.CraftSystem == null) { DefBlacksmithy.Initialize(); }
        if (DefTailoring.CraftSystem == null) { DefTailoring.Initialize(); }
        if (DefCarpentry.CraftSystem == null) { DefCarpentry.Initialize(); }
        if (DefTinkering.CraftSystem == null) { DefTinkering.Initialize(); }
        if (DefBowFletching.CraftSystem == null) { DefBowFletching.Initialize(); }
        if (DefInscription.CraftSystem == null) { DefInscription.Initialize(); }
        if (DefAlchemy.CraftSystem == null) { DefAlchemy.Initialize(); }
        if (DefCooking.CraftSystem == null) { DefCooking.Initialize(); }
    }

    [SkippableFact]
    public void SurveyedIslandTerrainSupportsCommonsCastleAndBoatDock()
    {
        TileDataRequirement.SkipIfMissing();
        var data = Environment.GetEnvironmentVariable("MODERNUO_TEST_DATA_DIR") ?? "";
        Skip.IfNot(System.IO.File.Exists(System.IO.Path.Combine(data, "haven-islands-manifest.json")) ||
            System.IO.File.Exists(System.IO.Path.Combine(data, "haven-frontiers-manifest.json")) || System.IO.File.Exists(System.IO.Path.Combine(data, "haven-original-dungeons-manifest.json")), "Requires the staged Haven island terrain");
        Assert.True(HavenPirateEstate.TerrainReady());
        var owner = new PlayerMobile(); owner.AddItem(new Backpack());
        var stranger = new PlayerMobile(); var estate = new HavenPirateEstate(); var boat = new LargeBoat(); var commons = new HavenCommunityCenter();
        try
        {
            owner.MoveToWorld(new Point3D(4196, 2868, 0), Map.Trammel);
            commons.MoveToWorld(HavenPirateEstate.CommonsSite, Map.Trammel); commons.Build();
            Assert.True(HavenCommunityCenter.Travel(stranger));
            estate.MoveToWorld(HavenPirateEstate.Site, Map.Trammel); estate.Build(owner);
            Assert.Equal(HousePlacementResult.Valid, HousePlacement.Check(owner, 0x7E, owner.Location, out _));
            Assert.False(Region.Find(owner.Location, Map.Trammel).AllowHousing(stranger, owner.Location));
            Assert.True(boat.CanFit(new Point3D(4237, 2960, -5), Map.Trammel, boat.NorthID));
            for (var y = 2960; y <= 2990; y++)
            { Assert.True(boat.CanFit(new Point3D(4237, y, -5), Map.Trammel, boat.NorthID)); }
            Assert.True(estate.Travel(owner)); Assert.False(estate.Travel(stranger));
            Assert.Contains(estate.Fixtures, i => i.ItemID == 0xCCA);
            Assert.Contains(estate.Fixtures, i => i.ItemID == 0x53B);
            Assert.Contains(estate.Fixtures, i => i is Server.Engines.Spawners.Spawner);
        }
        finally { commons.Delete(); estate.Delete(); boat.Delete(); owner.Delete(); stranger.Delete(); }
    }

    [SkippableFact]
    public void NewPersistentRecordsKeepRecipeIdentityAndGuildBalances()
    {
        TileDataRequirement.SkipIfMissing();
        var recipe = new HavenDoomRecipe { ArtifactType = typeof(LegacyOfTheDreadLord) };
        var copy = new HavenDoomRecipe(World.NewItem);
        var crew = new HavenGuildCrew { CompletedJobs = 37, Due = Core.Now + TimeSpan.FromMinutes(3) };
        crew.Resources.Add(12345);
        var crewCopy = new HavenGuildCrew(World.NewItem);
        try
        {
            var writer = new BufferWriter(true); recipe.Serialize(writer);
            copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.Equal(recipe.ArtifactType, copy.ArtifactType);
            writer = new BufferWriter(true); crew.Serialize(writer);
            crewCopy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.Equal(37, crewCopy.CompletedJobs); Assert.Equal(12345, crewCopy.Resources[0]); Assert.Equal(crew.Due, crewCopy.Due);
        }
        finally { recipe.Delete(); copy.Delete(); crew.Delete(); crewCopy.Delete(); }
    }

    [SkippableFact]
    public void CommonsBuildHasWalkableArrivalAndRemovesOnlyOwnedFixtures()
    {
        TileDataRequirement.SkipIfMissing();
        var center = new HavenCommunityCenter(); var visitor = new PlayerMobile();
        var unrelated = new Gold(1);
        try
        {
            center.MoveToWorld(new Point3D(1000, 1000, 100), Map.Malas);
            unrelated.MoveToWorld(center.Location, center.Map);
            center.Build();
            Assert.Equal(13, center.Fixtures.FindAll(i => i is HavenMarketStall).Count);
            Assert.Contains(center.Fixtures, i => i is HavenTravelLibrary);
            Assert.Equal(5, center.Fixtures.FindAll(i => i is HavenPracticeChest).Count);
            Assert.True(HavenCommunityCenter.Travel(visitor));
            Assert.Same(Map.Malas, visitor.Map); Assert.True(visitor.InRange(center.Arrival, 2));
            var count = center.Fixtures.Count; center.Build(); Assert.Equal(count, center.Fixtures.Count);
            var fixtures = center.Fixtures.ToArray(); var residents = center.Residents.ToArray();
            center.Delete();
            foreach (var item in fixtures) { Assert.True(item.Deleted); }
            foreach (var npc in residents) { Assert.True(npc.Deleted); }
            Assert.False(unrelated.Deleted);
        }
        finally { center.Delete(); visitor.Delete(); unrelated.Delete(); }
    }

    [SkippableFact]
    public void PracticeChestUsesNativeSkillDifficultiesAndCannotHurtOrStoreItems()
    {
        TileDataRequirement.SkipIfMissing();
        var chest = new HavenPracticeChest { Difficulty = 80 }; var player = new PlayerMobile(); var gold = new Gold(1);
        try
        {
            chest.Reset(); Assert.Equal(80, chest.RequiredSkill); Assert.Equal(110, chest.MaxLockLevel); Assert.Equal(80, chest.TrapPower);
            Assert.False(chest.ExecuteTrap(player)); Assert.False(chest.TryDropItem(player, gold, false));
            chest.LockPick(player); Assert.False(chest.Locked);
            chest.TrapType = TrapType.None; chest.Reset(); Assert.True(chest.Locked); Assert.Equal(TrapType.DartTrap, chest.TrapType);
        }
        finally { chest.Delete(); player.Delete(); gold.Delete(); }
    }

    [SkippableFact]
    public void VendorStockCannotBeLiftedOrUsedWithoutBuying()
    {
        TileDataRequirement.SkipIfMissing();
        var stall = new HavenMarketStall(); var player = new PlayerMobile(); var book = new Runebook();
        try
        {
            stall.ListItem(book, 100);
            Assert.False(book.CheckLift(player)); Assert.False(book.CheckItemUse(player));
            Assert.False(stall.CheckTarget(player, null, book));
        }
        finally { stall.Delete(); player.Delete(); }
    }

    [SkippableFact]
    public void MarketSiteIsOnWalkableHavenGround()
    {
        TileDataRequirement.SkipIfMissing();
        var locations = HavenMarketCommands.ProposeLocations(Map.Trammel);
        Assert.NotNull(locations); Assert.Equal(Enum.GetValues<HavenMarketTrade>().Length, locations.Length);
        foreach (var point in locations) { Assert.True(Map.Trammel.CanSpawnMobile(point)); }
        System.IO.File.WriteAllText(System.IO.Path.Combine(Core.BaseDirectory, "haven-market-proposed-site.txt"), string.Join(Environment.NewLine, locations));
    }

    [SkippableFact]
    public void DoomSetupBuildsSixLinkedRoomsWithoutResettingAnExistingEncounter()
    {
        TileDataRequirement.SkipIfMissing();
        try
        {
            Assert.True(HavenDoom.EnsureGauntlet(null));
            var rooms = HavenDoom.Controllers(); Assert.Equal(6, rooms.Count);
            foreach (var room in rooms) { Assert.NotNull(room.Sequence); }
            Assert.False(HavenDoom.EnsureGauntlet(null));
            foreach (var room in rooms) { Assert.False(room.Deleted); }
        }
        finally { Server.Engines.Doom.GenGauntlet.RemoveGauntlet(null); }
    }

    [SkippableFact]
    public void WorkshopsProduceRealGoodsAndStopAtStockLimit()
    {
        TileDataRequirement.SkipIfMissing();
        foreach (var trade in Enum.GetValues<HavenMarketTrade>())
        {
            if (HavenMarketProduction.System(trade) == null && trade != HavenMarketTrade.Gatherer) { continue; }
            var stall = new HavenMarketStall();
            try
            {
                stall.MoveToWorld(new Point3D(3500, 2570, 20), Map.Trammel); stall.Setup(trade);
                if (trade == HavenMarketTrade.Smith)
                {
                    stall.Order = new SmallSmithBOD(0, 10, typeof(Dagger), 1023922, 0xF52, false, BulkMaterialType.None);
                    stall.AddItem(stall.Order); stall.NextOrder = Core.Now + TimeSpan.FromDays(1);
                }
                for (var minute = 1; minute <= 300 && stall.Stock.Count < 24; minute++) { stall.Work(Core.Now + TimeSpan.FromMinutes(minute)); }
                Assert.NotEmpty(stall.Stock); Assert.Equal(stall.Stock.Count, stall.Prices.Count);
                foreach (var item in stall.Stock) { Assert.False(item.Deleted); Assert.Same(stall, item.Parent); }
                Assert.InRange(stall.Stock.Count, 1, 24);
                if (trade == HavenMarketTrade.Smith) { Assert.Contains(stall.Stock, item => item is SmallBOD { Complete: true }); }
            }
            finally { stall.Delete(); }
        }
    }

    [SkippableFact]
    public void DoomRecipeRequiresMatchingArtifactAndConsumesMaterialsOnlyOnce()
    {
        TileDataRequirement.SkipIfMissing();
        var crafter = new PlayerMobile(); crafter.AddItem(new Backpack());
        var recipe = new HavenDoomRecipe();
        var artifact = (Item)Activator.CreateInstance(recipe.ArtifactType);
        var wrong = new Longsword();
        crafter.Backpack.DropItem(recipe); crafter.Backpack.DropItem(artifact); crafter.Backpack.DropItem(wrong);
        crafter.Backpack.DropItem(new IronIngot(100)); crafter.Backpack.DropItem(new Diamond(20));
        try
        {
            Assert.False(recipe.Upgrade(crafter, artifact)); Assert.False(recipe.Deleted);
            crafter.Skills.Blacksmith.Base = 100;
            Assert.False(recipe.Upgrade(crafter, wrong)); Assert.Equal(100, crafter.Backpack.GetAmount(typeof(IronIngot)));
            Assert.True(recipe.Upgrade(crafter, artifact)); Assert.True(recipe.Deleted);
            Assert.True(HavenDoom.Reforged(artifact)); Assert.True(HavenGearExperience.IsSpecial(artifact));
            Assert.Equal(0, crafter.Backpack.GetAmount(typeof(IronIngot)));
            var attributes = ((IAosItem)artifact).Attributes;
            var damage = attributes.WeaponDamage;
            HavenGearExperience.Gain(artifact, 1900); Assert.True(attributes.WeaponDamage > damage);
            damage = attributes.WeaponDamage; HavenGearExperience.Gain(artifact, 1900); Assert.Equal(damage, attributes.WeaponDamage);
        }
        finally { crafter.Delete(); }
    }

    private sealed class FullAbsorption : SkillMasterySpell
    {
        public FullAbsorption(Mobile caster) : base(caster, null, new SpellInfo("Absorption test", "", -1, 0)) { }
        public void Activate() { BeginTimer(); }
        public override void OnCast() { FinishSequence(); }
        public override void OnDamaged(Mobile attacker, Mobile defender, DamageType type, ref int damage) { damage = 0; }
    }
    [SkippableFact]
    public void AbsorbedDamageStaysZeroThroughNativeResistanceCalculation()
    {
        TileDataRequirement.SkipIfMissing();
        var defender = new PlayerMobile { RawStr = 100 }; var attacker = new PlayerMobile();
        var effect = new FullAbsorption(defender);
        try
        {
            defender.Hits = defender.HitsMax; effect.Activate(); var before = defender.Hits;
            Assert.Equal(0, AOS.Damage(defender, attacker, 100, false, 100, 0, 0, 0, 0));
            Assert.Equal(before, defender.Hits);
        }
        finally { effect.Expire(); defender.Delete(); attacker.Delete(); }
    }

    [SkippableFact]
    public void ActualBardSongsCastTogetherAndOverlappingInvigorateSurvivesExpiry()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, RawStr = 100 };
        var first = new HavenCompanion { BoundOwner = owner, RawInt = 500 };
        var second = new HavenCompanion { BoundOwner = owner, RawInt = 500 };
        var strong = new InvigorateSpell(first, null); var weak = new InvigorateSpell(second, null);
        var inspire = new InspireSpell(first, null);
        var oldSkillCheck = Mobile.SkillCheckLocationHandler;
        Mobile.SkillCheckLocationHandler = Server.Misc.SkillCheck.Mobile_SkillCheckLocation;
        try
        {
            owner.MoveToWorld(new Point3D(3500, 2570, 20), Map.Trammel);
            foreach (var bard in new[] { first, second })
            {
                bard.SetControlMaster(owner); bard.MoveToWorld(owner.Location, owner.Map);
                foreach (var name in new[] { SkillName.Provocation, SkillName.Musicianship, SkillName.Peacemaking, SkillName.Discordance })
                { bard.Skills[name].Cap = 150; bard.Skills[name].Base = bard == first ? 130 : 120; }
                var lute = new Lute(); bard.Backpack.DropItem(lute); BaseInstrument.SetInstrument(bard, lute); bard.Mana = bard.ManaMax;
            }
            foreach (var song in new SkillMasterySpell[] { strong, weak, inspire })
            {
                Assert.True(song.CheckCast()); song.Caster.Spell = song; song.State = SpellState.Sequencing; song.OnCast();
                Assert.NotNull(song.Timer); Assert.Null(song.Caster.Spell);
            }
            Assert.Same(strong, SkillMasterySpell.GetSpellForParty(owner, typeof(InvigorateSpell)));
            var bonus = owner.GetStatMod(InvigorateSpell.StatModName + "str").Offset;
            weak.Expire(); Assert.Equal(bonus, owner.GetStatMod(InvigorateSpell.StatModName + "str").Offset);
            Assert.NotNull(SkillMasterySpell.GetSpellForParty(owner, typeof(InspireSpell)));
            strong.Expire(); Assert.Null(owner.GetStatMod(InvigorateSpell.StatModName + "str"));
        }
        finally { Mobile.SkillCheckLocationHandler = oldSkillCheck; inspire.Expire(); strong.Expire(); weak.Expire(); first.Delete(); second.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void MarketPurchaseIsWalletFundedAndCannotBeReplayed()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        player.AddItem(new Backpack());
        var wallet = new AdventurersWallet { Balance = 5000 }; player.Backpack.DropItem(wallet);
        var stall = new HavenMarketStall(); var goods = new Longsword();
        try
        {
            player.MoveToWorld(new Point3D(3500, 2570, 20), Map.Trammel); stall.MoveToWorld(player.Location, player.Map);
            Assert.True(stall.ListItem(goods, 1000));
            Assert.False(stall.Buy(player, goods, 1)); Assert.Equal(5000, wallet.Balance);
            Assert.True(stall.Buy(player, goods, 1000)); Assert.Equal(4000, wallet.Balance);
            Assert.True(goods.IsChildOf(player.Backpack));
            Assert.False(stall.Buy(player, goods, 1000)); Assert.Equal(4000, wallet.Balance);
            Assert.Equal(1000, stall.Sales);
        }
        finally { stall.Delete(); player.Delete(); if (!goods.Deleted) { goods.Delete(); } }
    }

    [SkippableFact]
    public void ModernAppraisalRecognizesManaLeechAndCastingAttributes()
    {
        TileDataRequirement.SkipIfMissing();
        var sword = new Longsword(); var book = new Spellbook(ulong.MaxValue);
        try
        {
            var plain = BotAppraisal.Value(sword);
            sword.WeaponAttributes.HitLeechMana = 50; sword.Attributes.WeaponSpeed = 30;
            Assert.True(BotAppraisal.Value(sword) > plain);
            Assert.Contains("enchanted", BotAppraisal.NameFor(sword));
            book.Attributes.LowerManaCost = 8; book.Attributes.CastRecovery = 3;
            Assert.True(BotAppraisal.Value(book) > 1000);
        }
        finally { sword.Delete(); book.Delete(); }
    }

    [SkippableFact]
    public void BardUpkeepCannotGenerateManaAtNormalOrUnlimitedSkills()
    {
        TileDataRequirement.SkipIfMissing();
        var bard = new PlayerMobile();
        try
        {
            foreach (var value in new[] { 100.0, 120.0, 1000.0 })
            {
                foreach (var name in new[] { SkillName.Provocation, SkillName.Peacemaking, SkillName.Discordance, SkillName.Musicianship })
                { bard.Skills[name].Cap = value; bard.Skills[name].Base = value; }
                Assert.True(new InspireSpell(bard, null).ScaleUpkeep() >= 1);
                Assert.True(new InvigorateSpell(bard, null).ScaleUpkeep() >= 1);
            }
        }
        finally { bard.Delete(); }
    }

    [SkippableFact]
    public void GuildWorkRequiresLeaderAndCreditsOnlyOnceAfterDowntime()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 }; owner.AddItem(new Backpack());
        var outsider = new PlayerMobile(); outsider.AddItem(new Backpack());
        var guild = new Guild(owner, "Market tests", "MKT");
        var bot = new PlayerBot(BotClass.Miner, BotSkillTier.Expert);
        HavenGuildCrew crew = null;
        try
        {
            owner.MoveToWorld(new Point3D(3500, 2570, 20), Map.Trammel); bot.MoveToWorld(owner.Location, owner.Map);
            Assert.Null(HavenGuildCrew.Recruit(outsider, bot));
            crew = HavenGuildCrew.Recruit(owner, bot); Assert.NotNull(crew);
            Assert.True(HavenGuildCrew.Retained(bot));
            var now = Core.Now + TimeSpan.FromDays(30);
            crew.Work(now); Assert.Equal(1, crew.CompletedJobs);
            crew.Work(now); Assert.Equal(1, crew.CompletedJobs);
            Assert.False(crew.Withdraw(outsider, 0, 1));
            var addedGear = new Katana(); bot.Backpack.DropItem(addedGear);
            crew.Dismiss(owner); Assert.False(HavenGuildCrew.Retained(bot));
            Assert.True(addedGear.IsChildOf(owner.Backpack));
        }
        finally { crew?.Delete(); bot.Delete(); guild.Disband(); owner.Delete(); outsider.Delete(); }
    }
}
