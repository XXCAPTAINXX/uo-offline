using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using Server.CustomBots;
using Server.Engines.Spawners;
using Server.Gumps;
using Server.Json;
using Server.Items;
using Server.Accounting;
using Server.Accounting.Security;
using Server.Multis.Deeds;
using Server.Menus.ItemLists;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTests
{
    [SkippableFact]
    public void HavenSteedsMoveToWalkableWildernessWithoutMovingPetsOrWeakeningStats()
    {
        TileDataRequirement.SkipIfMissing();
        var map = Map.Trammel;
        var point = HavenContentBootstrap.HavenWildernessSteed;
        Assert.True(map.CanSpawnMobile(point.X, point.Y, map.GetAverageZ(point.X, point.Y)), "Wilderness destination must be walkable");
        Assert.True(map.CanSpawnMobile(3670, 2587, map.GetAverageZ(3670, 2587)), "Warden spawn must be walkable");
        var wild = new VampiricSteed();
        var pet = new VampiricSteed { Controlled = true };
        var spawner = new VampiricSteedSpawner();
        try
        {
            var old = new Point3D(3690, 2525, map.GetAverageZ(3690, 2525));
            wild.MoveToWorld(old, map);
            pet.MoveToWorld(old, map);
            spawner.MoveToWorld(old, map);
            HavenContentBootstrap.RelocateOldHavenSteeds();
            Assert.Equal(point.X, wild.X);
            Assert.Equal(point.Y, wild.Y);
            Assert.Equal(wild.Location, spawner.Location);
            Assert.Equal(wild.Location, wild.Home);
            Assert.Equal(old, pet.Location);
            Assert.Equal(20, wild.DamageMax);
            HavenContentBootstrap.RelocateOldHavenSteeds();
            Assert.Equal(point.Y, wild.Y);
        }
        finally { wild.Delete(); pet.Delete(); spawner.Delete(); }
    }
    [SkippableFact]
    public void IslandSteedsRegainNormalCapabilitiesWhenTamedOrLeavingIsland()
    {
        TileDataRequirement.SkipIfMissing();
        var steed = new VampiricSteed();
        try
        {
            var fullHits = steed.HitsMax;
            steed.MoveToWorld(new Point3D(3490, 2582, 20), Map.Trammel);
            steed.UpdateIslandDifficulty();
            Assert.Equal(120, steed.HitsMax);
            Assert.Equal(AIType.AI_Melee, steed.AI);
            var damage = 20;
            steed.AlterMeleeDamageTo(null, ref damage);
            Assert.Equal(8, damage);
            steed.Controlled = true;
            steed.UpdateIslandDifficulty();
            Assert.Equal(fullHits, steed.HitsMax);
            Assert.Equal(AIType.AI_Mage, steed.AI);
            damage = 20;
            steed.AlterMeleeDamageTo(null, ref damage);
            Assert.Equal(20, damage);
            Assert.InRange(steed.RawDex, 180, 210);
            Assert.InRange(steed.StamMax, 180, 210);
            Assert.Equal(steed.StamMax, steed.Stam);
            var tamedDex = steed.RawDex;
            steed.UpdateIslandDifficulty();
            Assert.Equal(tamedDex, steed.RawDex);
            steed.Controlled = false;
            steed.MoveToWorld(new Point3D(1388, 1498, 0), Map.Felucca);
            steed.UpdateIslandDifficulty();
            Assert.Equal(fullHits, steed.HitsMax);
            Assert.Equal(AIType.AI_Mage, steed.AI);
        }
        finally { steed.Delete(); }
    }
    [SkippableFact]
    public void WalletDoubleClickCollectsLooseGoldButLeavesSecuredAndDistantGold()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        player.AddItem(new Backpack());
        var wallet = new AdventurersWallet();
        var nearby = new Gold(400);
        var far = new Gold(600);
        var secured = new Gold(900) { Movable = false };
        try
        {
            player.Backpack.DropItem(wallet);
            player.Backpack.DropItem(new Gold(100));
            player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            nearby.MoveToWorld(player.Location, player.Map);
            secured.MoveToWorld(player.Location, player.Map);
            far.MoveToWorld(new Point3D(player.X + 20, player.Y, player.Z), player.Map);
            wallet.OnDoubleClick(player);
            Assert.Equal(500, wallet.Balance);
            Assert.True(nearby.Deleted);
            Assert.False(far.Deleted);
            Assert.False(secured.Deleted);
            wallet.OnDoubleClick(player);
            Assert.Equal(500, wallet.Balance);
            var speech = new Server.SpeechEventArgs(player, "withdraw 100", Server.MessageType.Regular, 0, Array.Empty<int>());
            HavenWalletCommands.OnSpeech(speech);
            Assert.True(speech.Handled);
            Assert.Equal(400, wallet.Balance);
            Assert.Equal(100, player.Backpack.GetAmount(typeof(Gold)));
        }
        finally { nearby.Delete(); far.Delete(); secured.Delete(); player.Delete(); }
    }

    [Fact]
    public void WalletSaveLoadPreservesGoldAndShardsAndMigratesOldGoldOnlyWallets()
    {
        var wallet = new AdventurersWallet { Balance = 123456, AstralShards = 42, HavenMarks = 73, ItemID = 0xE79 };
        var header = new Item(World.NewItem);
        AdventurersWallet copy = null;
        AdventurersWallet legacy = null;
        try
        {
            var writer = new BufferWriter(true);
            wallet.Serialize(writer);
            var data = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();
            copy = new AdventurersWallet(World.NewItem);
            copy.Deserialize(new BufferReader(data));
            Assert.Equal(123456, copy.Balance);
            Assert.Equal(42, copy.AstralShards);
            Assert.Equal(73, copy.HavenMarks);
            Assert.Equal(0xEEF, copy.ItemID);
            Assert.Equal(0x8A5, copy.Hue);
            var reader = new BufferReader(data);
            header.Deserialize(reader);
            var versionPosition = (int)reader.Position;
            var v1Data = data.AsSpan(0, data.Length - 8).ToArray();
            v1Data[versionPosition] = 1;
            var v1 = new AdventurersWallet(World.NewItem);
            try { v1.Deserialize(new BufferReader(v1Data)); Assert.Equal(123456, v1.Balance); Assert.Equal(42, v1.AstralShards); Assert.Equal(0, v1.HavenMarks); }
            finally { v1.Delete(); }
            var oldData = data.AsSpan(0, data.Length - 16).ToArray();
            oldData[versionPosition] = 0;
            legacy = new AdventurersWallet(World.NewItem);
            legacy.Deserialize(new BufferReader(oldData));
            Assert.Equal(123456, legacy.Balance);
            Assert.Equal(0, legacy.AstralShards);
        }
        finally { wallet.Delete(); header.Delete(); copy?.Delete(); legacy?.Delete(); }
    }

    [SkippableFact]
    public void GearUpgradeSpendsWalletGoldAndRejectsStalePrice()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        player.AddItem(new Backpack());
        var wallet = new AdventurersWallet { Balance = 15000 };
        var robe = new NewHavenAdventurersRobe();
        var stone = new HavenUpgradeStone();
        try
        {
            player.Backpack.DropItem(wallet); player.Backpack.DropItem(robe);
            player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); stone.MoveToWorld(player.Location, player.Map);
            stone.ApplyUpgrade(player, 0);
            Assert.Equal(1, robe.UpgradeTier);
            Assert.Equal(10000, wallet.Balance);
            stone.ApplyUpgrade(player, 0);
            Assert.Equal(10000, wallet.Balance);
        }
        finally { player.Delete(); stone.Delete(); }
    }

    [Fact]
    public void AstralPurchasesUseShardsAndDoNotChargeFullPacks()
    {
        var player = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        player.AddItem(new Backpack());
        var wallet = new AdventurersWallet { Balance = 50000, AstralShards = 60 };
        player.Backpack.DropItem(wallet);
        try
        {
            player.Backpack.MaxItems = 1;
            Assert.False(HavenAstralRewards.Buy(player, wallet, 2));
            Assert.Equal(60, wallet.AstralShards);
            player.Backpack.MaxItems = 125;
            Assert.True(HavenAstralRewards.Buy(player, wallet, 2));
            Assert.Equal(0, wallet.AstralShards);
            Assert.Equal(50000, wallet.Balance);
            Assert.Single(player.Backpack.Items.OfType<AstralFortuneEarrings>());
            Assert.False(HavenAstralRewards.Buy(player, wallet, 0));
            Assert.True(HavenAstralRewards.Award(player, 3));
            Assert.Equal(0, wallet.AstralShards);
            Assert.Equal(3, player.Backpack.GetAmount(typeof(AstralShard)));
            player.Backpack.FindItemByType<AstralShard>().OnDoubleClick(player);
            Assert.Equal(3, wallet.AstralShards);
        }
        finally { player.Delete(); }
    }

    [SkippableTheory]
    [InlineData(HavenCompanionRole.Fighter)]
    [InlineData(HavenCompanionRole.Healer)]
    [InlineData(HavenCompanionRole.Bard)]
    [InlineData(HavenCompanionRole.Caster)]
    public void CompanionRecoversAllResourcesWithoutRefillingEveryThink(HavenCompanionRole role)
    {
        TileDataRequirement.SkipIfMissing();
        var companion = new HavenCompanion { Role = role };
        try
        {
            companion.Hits = companion.Stam = companion.Mana = 1;
            var now = Core.Now.AddSeconds(1);
            companion.RecoverResources(now);
            Assert.True(companion.Hits > 1 && companion.Stam >= 13 && companion.Mana >= 9);
            var stamina = companion.Stam;
            companion.RecoverResources(now);
            Assert.Equal(stamina, companion.Stam);
            companion.RecoverResources(now.AddSeconds(3));
            Assert.True(companion.Stam > stamina);
        }
        finally { companion.Delete(); }
    }

    [SkippableFact]
    public void CasterChoosesNativeSpellsAndRejectsPlayerTargets()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var companion = new HavenCompanion { BoundOwner = owner, Role = HavenCompanionRole.Caster };
        var enemy = new OldHavenWarden();
        try
        {
            foreach (var mobile in new Mobile[] { owner, companion, enemy }) { mobile.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); }
            companion.Mana = companion.ManaMax;
            Assert.Null(companion.ChooseAttackSpell(owner));
            Assert.IsType<Server.Spells.Sixth.EnergyBoltSpell>(companion.ChooseAttackSpell(enemy));
            companion.Skills.Spellweaving.Base = 100;
            enemy.Hits = 1;
            Assert.IsType<Server.Spells.Spellweaving.WordOfDeathSpell>(companion.ChooseAttackSpell(enemy));
            Assert.True(companion.CanCastAt(owner, true));
            companion.Role = HavenCompanionRole.Fighter;
            Assert.Null(companion.ChooseAttackSpell(enemy));
        }
        finally { companion.Delete(); enemy.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void CompanionEquipmentSwapStoresOldGearAndRejectsAnotherOwner()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var other = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack()); other.AddItem(new Backpack());
        var companion = new HavenCompanion { BoundOwner = owner };
        var sword = new Longsword();
        try
        {
            foreach (var mobile in new Mobile[] { owner, other, companion }) { mobile.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); }
            owner.Backpack.DropItem(sword);
            var old = companion.FindItemOnLayer(Layer.OneHanded);
            Assert.False(companion.EquipFromOwner(other, sword));
            Assert.True(companion.EquipFromOwner(owner, sword));
            Assert.Same(sword, companion.FindItemOnLayer(Layer.OneHanded));
            Assert.True(old.IsChildOf(companion.Backpack));
        }
        finally { companion.Delete(); owner.Delete(); other.Delete(); }
    }

    [Fact]
    public void ShopRowsIncludeStatTooltips()
    {
        var stone = new SpecialRewardStone();
        try
        {
            var gump = new HavenListGump(stone, new SpecialRewardStone.RewardMenu());
            Assert.Equal(12, gump.Entries.OfType<GumpTooltip>().Count());
        }
        finally { stone.Delete(); }
    }
    [SkippableFact]
    public void MerchantPurchaseUsesWalletAndCannotDoubleChargeInsufficientFunds()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190, Str = 100, AccessLevel = AccessLevel.Player };
        player.AddItem(new Backpack());
        var wallet = new AdventurersWallet { Balance = 10000 };
        var vendor = new Provisioner();
        try
        {
            player.Backpack.DropItem(wallet);
            player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            vendor.MoveToWorld(player.Location, player.Map);
            var offer = vendor.GetBuyInfo().OfType<GenericBuyInfo>().First(i => i.Amount > 0 && i.Price > 0 && i.GetDisplayEntity() is Item);
            var display = offer.GetDisplayEntity();
            Assert.True(vendor.OnBuyItems(player, new List<BuyItemResponse> { new(display.Serial, 1) }));
            Assert.True(wallet.Balance < 10000);
            Assert.Equal(0, player.Backpack.GetAmount(typeof(Gold)));
            wallet.Balance = 1;
            Assert.False(vendor.OnBuyItems(player, new List<BuyItemResponse> { new(display.Serial, 100) }));
            Assert.Equal(1, wallet.Balance);
        }
        finally { vendor.Delete(); player.Delete(); }
    }

    [SkippableFact]
    public void ArcherUsesBowWithoutConsumingAmmoAndCanEquipAnImprovedCrossbow()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        var companion = new HavenCompanion { BoundOwner = owner, Role = HavenCompanionRole.Archer };
        var enemy = new OldHavenWarden();
        try
        {
            foreach (var mobile in new Mobile[] { owner, companion, enemy }) { mobile.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); }
            companion.ConfigureCombatRole();
            Assert.IsAssignableFrom<Bow>(companion.Weapon);
            Assert.Equal(AIType.AI_Archer, companion.AI);
            var arrows = companion.Backpack.GetAmount(typeof(Arrow));
            Assert.True(((BaseRanged)companion.Weapon).OnFired(companion, enemy));
            Assert.Equal(arrows, companion.Backpack.GetAmount(typeof(Arrow)));
            var crossbow = new Crossbow();
            owner.Backpack.DropItem(crossbow);
            Assert.True(companion.EquipFromOwner(owner, crossbow));
            Assert.Same(crossbow, companion.Weapon);
            companion.Role = HavenCompanionRole.Fighter;
            companion.ConfigureCombatRole();
            Assert.False(companion.Weapon is BaseRanged);
            Assert.True(crossbow.IsChildOf(companion.Backpack));
        }
        finally { companion.Delete(); enemy.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void AstralEligibilityExcludesPetsAndRequiresNearbyMonsterCredit()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        var monster = new OldHavenWarden();
        try
        {
            player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            monster.MoveToWorld(player.Location, player.Map);
            Assert.True(HavenAstralRewards.Eligible(monster, player));
            monster.Controlled = true;
            Assert.False(HavenAstralRewards.Eligible(monster, player));
            monster.Controlled = false;
            player.MoveToWorld(new Point3D(player.X + 30, player.Y, player.Z), player.Map);
            Assert.False(HavenAstralRewards.Eligible(monster, player));
        }
        finally { monster.Delete(); player.Delete(); }
    }
    [Fact]
    public void ArcaneSuppliesContainCompleteBooksChargedSendingAndSoloBundles()
    {
        var menu = new ArcaneSupplyStone.Menu();
        for (var i = 0; i < menu.Entries.Length; i++)
        {
            var item = menu.CreateItem(i);
            try
            {
                Assert.NotNull(item);
                if (item is Spellbook book) { Assert.Equal(book.BookCount, book.SpellCount); }
                if (item is BagOfSending bag) { Assert.Equal(30, bag.Charges); }
                if (i == 14) { Assert.Equal(8, item.Items.Count); }
            }
            finally { item.Delete(); }
        }
    }

    [SkippableFact]
    public void AtlasAccepts48MarkedRunesAcrossChaptersAndRejectsAnotherOwner()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var other = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack()); other.AddItem(new Backpack());
        var atlas = new HavenRunicAtlas();
        owner.Backpack.DropItem(atlas);
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            for (var i = 0; i < 49; i++)
            {
                var rune = new RecallRune { Marked = true, Target = owner.Location, TargetMap = owner.Map, Description = "test destination" };
                try
                {
                    Assert.False(atlas.OnDragDrop(other, rune));
                    Assert.Equal(i < 48, atlas.OnDragDrop(owner, rune));
                }
                finally { rune.Delete(); }
            }
            Assert.Equal(48, atlas.Items.OfType<Runebook>().Sum(b => b.Entries.Count));
        }
        finally { owner.Delete(); other.Delete(); }
    }

    [Fact]
    public void CapeIsFreeBoundAndLevelsOnlyWhileWorn()
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        var other = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        try
        {
            HavenLevelingCape.Claim(owner); HavenLevelingCape.Claim(owner);
            var cape = Assert.Single(owner.Backpack.Items.OfType<HavenLevelingCape>());
            cape.GainExperience(owner, 100);
            Assert.Equal(1, cape.Level);
            Assert.False(cape.CanEquip(other));
            Assert.True(owner.EquipItem(cape));
            HavenGearExperience.GainEquipped(owner, 100);
            Assert.Equal(3, cape.Level);
            Assert.Equal(75, cape.Attributes.Luck);
        }
        finally { owner.Delete(); other.Delete(); }
    }

    [Fact]
    public void GearProgressPersistsWithoutApplyingBonusesAgain()
    {
        var armor = new HavenStarterSash();
        HavenGearExperience copy = null;
        try
        {
            var initialLuck = armor.Attributes.Luck;
            HavenGearExperience.Gain(armor, 400);
            Assert.Equal(20 + initialLuck, armor.Attributes.Luck);
            Assert.Equal(2, armor.Attributes.BonusDex);
            var progress = HavenGearExperience.Find(armor);
            var writer = new BufferWriter(true);
            progress.Serialize(writer);
            copy = new HavenGearExperience(World.NewItem);
            copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.Equal(5, copy.Level);
            Assert.Equal(5, copy.AppliedLevel);
            HavenGearExperience.Gain(armor, 1);
            Assert.Equal(20 + initialLuck, armor.Attributes.Luck);
            Assert.True(progress.IsVirtualItem);
        }
        finally { armor.Delete(); copy?.Delete(); }
    }

    [Fact]
    public void BlessingDeedPreservesItselfForInvalidTargets()
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        var deed = new HavenEquipmentBlessDeed(); var weapon = new Longsword(); var gold = new Gold(100);
        owner.Backpack.DropItem(deed); owner.Backpack.DropItem(weapon); owner.Backpack.DropItem(gold);
        try
        {
            Assert.False(deed.Bless(owner, gold)); Assert.False(deed.Deleted);
            Assert.True(deed.Bless(owner, weapon)); Assert.True(deed.Deleted);
            Assert.Equal(LootType.Blessed, weapon.LootType);
        }
        finally { owner.Delete(); }
    }
    private static bool _npcConfigured;
    public HavenWorldTests()
    {
        if (!_npcConfigured)
        {
            NPCSpeeds.Configure();
            NameList.Configure();
            _npcConfigured = true;
        }
    }
    private static string SpawnRoot => Environment.GetEnvironmentVariable("HAVEN_WORLD_DATA") ?? Core.BaseDirectory;

    [Fact]
    public void WalletWithdrawalsPreserveGoldAndRejectFullOrForeignPacks()
    {
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        var other = new PlayerMobile { Player = true, Body = 0x190 };
        player.AddItem(new Backpack());
        other.AddItem(new Backpack());
        var wallet = new AdventurersWallet { Balance = 5000 };
        player.Backpack.DropItem(wallet);
        try
        {
            Assert.False(wallet.Withdraw(other, 100));
            Assert.False(wallet.Withdraw(player, -1));
            Assert.False(wallet.Withdraw(player, 5001));
            player.Backpack.MaxItems = 1;
            Assert.False(wallet.Withdraw(player, 100));
            Assert.Equal(5000, wallet.Balance);
            player.Backpack.MaxItems = 125;
            Assert.True(wallet.Withdraw(player, 1000));
            Assert.Equal(4000, wallet.Balance);
            Assert.Equal(1000, player.Backpack.GetAmount(typeof(Gold)));
            wallet.DepositBackpackGold(player);
            Assert.Equal(5000, wallet.Balance);
            Assert.Equal(0, player.Backpack.GetAmount(typeof(Gold)));
        }
        finally { player.Delete(); other.Delete(); }
    }

    [SkippableFact]
    public void RepairBenchRestoresOwnedDurabilityWithoutChangingProperties()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        var other = new PlayerMobile { Player = true, Body = 0x190 };
        player.AddItem(new Backpack());
        var bench = new HavenRepairBench();
        var wallet = new AdventurersWallet { Balance = 49 };
        player.Backpack.DropItem(wallet);
        var sword = new Longsword { MaxHitPoints = 50, HitPoints = 5 };
        sword.Attributes.WeaponDamage = 30;
        player.Backpack.DropItem(sword);
        try
        {
            foreach (var mobile in new Mobile[] { player, other }) { mobile.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); }
            bench.MoveToWorld(player.Location, player.Map);
            Assert.False(bench.Repair(other, sword));
            Assert.False(bench.Repair(player, sword));
            Assert.Equal(49, wallet.Balance);
            Assert.Equal(5, sword.HitPoints);
            wallet.Balance = 100;
            Assert.True(bench.Repair(player, sword));
            Assert.Equal(50, wallet.Balance);
            Assert.Equal(50, sword.HitPoints);
            Assert.Equal(50, sword.MaxHitPoints);
            Assert.Equal(30, sword.Attributes.WeaponDamage);
            Assert.False(bench.Repair(player, sword));
            Assert.Equal(50, wallet.Balance);
            sword.HitPoints = 10;
            Assert.False(bench.Restore(player, sword));
            Assert.Equal(50, wallet.Balance);
            wallet.Balance = 500;
            Assert.True(bench.Restore(player, sword));
            Assert.Equal(250, wallet.Balance);
            Assert.Equal(sword.InitMaxHits, sword.MaxHitPoints);
            Assert.Equal(sword.MaxHitPoints, sword.HitPoints);
            Assert.Equal(30, sword.Attributes.WeaponDamage);
            Assert.False(bench.Restore(player, sword));
            Assert.Equal(250, wallet.Balance);
            sword.HitPoints = 10;
            player.MoveToWorld(new Point3D(1427, 1695, 0), Map.Felucca);
            Assert.False(bench.Repair(player, sword));
            Assert.Equal(10, sword.HitPoints);
        }
        finally { bench.Delete(); player.Delete(); other.Delete(); }
    }

    [SkippableFact]
    public void StarterEquipmentRepairsAndRestoresForFree()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        player.AddItem(new Backpack());
        var bench = new HavenRepairBench();
        var blade = new ApprenticeBlade { Level = 4, MaxHitPoints = 20, HitPoints = 5, BoundTo = player };
        player.Backpack.DropItem(blade);
        try
        {
            player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            bench.MoveToWorld(player.Location, player.Map);
            Assert.True(bench.Repair(player, blade));
            Assert.Equal(20, blade.HitPoints);
            Assert.True(bench.Restore(player, blade));
            Assert.Equal(blade.InitMaxHits, blade.MaxHitPoints);
            Assert.Equal(blade.MaxHitPoints, blade.HitPoints);
            Assert.Equal(4, blade.Level);
            Assert.Same(player, blade.BoundTo);
            Assert.False(bench.Restore(player, blade));
        }
        finally { bench.Delete(); player.Delete(); }
    }

    [SkippableFact]
    public void CompanionRecallPreservesIdentityInventoryAndOwnership()
    {
        TileDataRequirement.SkipIfMissing();
        var account = CreateTestAccount();
        var player = new PlayerMobile { Player = true, Body = 0x190, Account = account };
        var other = new PlayerMobile { Player = true, Body = 0x190 };
        HavenCompanion companion = null;
        try
        {
            player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            other.MoveToWorld(player.Location, player.Map);
            companion = HavenCompanions.ClaimOrRecall(player);
            Assert.NotNull(companion);
            Assert.Same(player, companion.ControlMaster);
            Assert.True(companion.IsBonded);
            Assert.IsType<HavenCompanionPack>(companion.Backpack);
            Assert.Equal(1000, companion.Backpack.MaxItems);
            Assert.Equal(50000, companion.Backpack.MaxWeight);
            var gold = new Gold(777);
            companion.Backpack.DropItem(gold);
            Assert.Same(companion, HavenCompanions.ClaimOrRecall(player));
            Assert.Equal(777, companion.Backpack.GetAmount(typeof(Gold)));
            Assert.True(companion.CheckNonlocalLift(player, gold));
            Assert.False(companion.CheckNonlocalLift(other, gold));
            Assert.False(companion.Backpack.CheckContentDisplay(other));
            Assert.False(companion.CanBeControlledBy(other));
            companion.JoinParty(player);
            Assert.True(Server.Engines.PartySystem.Party.Get(player).Contains(companion));
            companion.IsStabled = true;
            companion.Internalize();
            Assert.Null(HavenCompanions.ClaimOrRecall(player));
            Assert.Equal(777, companion.Backpack.GetAmount(typeof(Gold)));
        }
        finally { companion?.Delete(); player.Delete(); other.Delete(); account.Delete(); }
    }

    [Fact]
    public void CompanionOfflineTrainingIsCountedOnceAndGrowsBeyondNativeSkills()
    {
        var companion = new HavenCompanion();
        try
        {
            var start = companion.LastTraining;
            companion.UpdateTraining(start.AddDays(1));
            Assert.Equal(1440, companion.TrainingMinutes, 5);
            var mastery = companion.Mastery;
            companion.UpdateTraining(start.AddDays(1));
            companion.UpdateTraining(start);
            Assert.Equal(1440, companion.TrainingMinutes, 5);
            Assert.Equal(mastery, companion.Mastery);
            var damage = companion.DamageMax;
            companion.TrainingMinutes = 1000000000;
            companion.UpdateTraining(start.AddDays(2));
            Assert.True(companion.Mastery > 6553.5);
            Assert.Equal(6553.5, companion.Skills.Swords.Base);
            Assert.True(companion.DamageMax > damage);
        }
        finally { companion.Delete(); }
    }

    [Fact]
    public void CompanionTrainingRoleAndOwnerSurviveSerialization()
    {
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        var companion = new HavenCompanion { BoundOwner = player, Role = HavenCompanionRole.Bard, TrainingMinutes = 1440 };
        HavenCompanion copy = null;
        try
        {
            var writer = new BufferWriter(true);
            companion.Serialize(writer);
            var data = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();
            copy = new HavenCompanion(World.NewMobile);
            var reader = new BufferReader(data);
            copy.Deserialize(reader);
            Assert.Equal(data.Length, reader.Position);
            Assert.Same(player, copy.BoundOwner);
            Assert.Equal(HavenCompanionRole.Bard, copy.Role);
            Assert.Equal(companion.TrainingMinutes, copy.TrainingMinutes);
            Assert.Equal(companion.LastTraining, copy.LastTraining);
        }
        finally { copy?.Delete(); companion.Delete(); player.Delete(); }
    }

    [SkippableFact]
    public void PetResurrectionWorksAtStableAndBankButChecksOwnerAndDistance()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        var other = new PlayerMobile { Player = true, Body = 0x190 };
        var trainer = new AnimalTrainer();
        var vet = new HavenPetHealer();
        var pet = new Horse { IsBonded = true, IsDeadPet = true };
        try
        {
            Assert.True(HavenRecovery.FindLocation(HavenRecovery.BankLocation, out var location));
            foreach (var mobile in new Mobile[] { player, other, trainer, vet, pet }) { mobile.MoveToWorld(location, Map.Trammel); }
            pet.SetControlMaster(player);
            Assert.True(pet.Controlled);
            Assert.True(pet.Map.CanFit(pet.Location, 16, false, false));
            pet.Skills.Wrestling.Base = 80;
            Assert.False(HavenPetRecovery.Resurrect(other, trainer, pet));
            Assert.True(HavenPetRecovery.Resurrect(player, trainer, pet));
            Assert.False(pet.IsDeadPet);
            Assert.Equal(80, pet.Skills.Wrestling.Base);
            Assert.False(HavenPetRecovery.Resurrect(player, trainer, pet));
            pet.IsDeadPet = true;
            pet.MoveToWorld(new Point3D(1427, 1695, 0), Map.Felucca);
            Assert.False(HavenPetRecovery.Resurrect(player, vet, pet));
            pet.MoveToWorld(player.Location, player.Map);
            Assert.True(HavenPetRecovery.Resurrect(player, vet, pet));
        }
        finally { pet.Delete(); vet.Delete(); trainer.Delete(); player.Delete(); other.Delete(); }
    }

    [SkippableTheory]
    [InlineData(HavenCompanionRole.Fighter)]
    [InlineData(HavenCompanionRole.Healer)]
    [InlineData(HavenCompanionRole.Bard)]
    public void CompanionHealingChecksRangeAndOwner(HavenCompanionRole role)
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        var other = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        var companion = new HavenCompanion { BoundOwner = player, Role = role };
        try
        {
            foreach (var mobile in new Mobile[] { player, other, companion }) { mobile.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); }
            player.Hits = other.Hits = 10;
            Assert.False(companion.Support(other));
            Assert.True(companion.Support(player));
            Assert.True(player.Hits > 10);
            Assert.False(companion.Support(player));
            Assert.Equal(10, other.Hits);
        }
        finally { companion.Delete(); player.Delete(); other.Delete(); }
    }

    [SkippableFact]
    public void CompanionOffersResurrectionWithoutForcingItAndBardSongsDoNotStack()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x192, Str = 100, Dex = 80, Int = 60 };
        var companion = new HavenCompanion { BoundOwner = player, Role = HavenCompanionRole.Bard };
        try
        {
            Assert.True(HavenRecovery.FindLocation(HavenRecovery.BankLocation, out var location));
            player.MoveToWorld(location, Map.Trammel);
            companion.MoveToWorld(location, Map.Trammel);
            Assert.True(companion.Support(player));
            Assert.False(player.Alive);
            Assert.False(companion.Support(player));
            player.Body = 0x190;
            companion.BardSupport(player);
            Assert.Equal(100, player.Str);
            companion.Skills.Musicianship.Base = 80;
            companion.Skills.Peacemaking.Base = 80;
            companion.BardSupport(player);
            Assert.Equal(103, player.Str);
            Assert.Equal(83, player.Dex);
            Assert.Equal(63, player.Int);
            companion.BardSupport(player);
            Assert.Equal(103, player.Str);
        }
        finally { companion.Delete(); player.Delete(); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CompanionTabsAreCompactAndContainNavigation(int tab)
    {
        var companion = new HavenCompanion();
        try
        {
            var menu = new HavenCompanionGump(companion, tab);
            CheckBounds(menu, 370, 370);
            foreach (var id in new[] { 100, 101, 102, 110, 0 })
            {
                Assert.Contains(menu.Entries.OfType<GumpButton>(), b => b.ButtonID == id);
            }
            Assert.Equal(0.1, companion.ActiveMoveSpeed);
            Assert.Equal(0.1, companion.PassiveMoveSpeed);
        }
        finally { companion.Delete(); }
    }

    [SkippableFact]
    public void CompanionBardUsesNativeDiscordanceAndDoesNotTargetPlayers()
    {
        TileDataRequirement.SkipIfMissing();
        var oldHandler = Mobile.SkillCheckTargetHandler;
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        var companion = new HavenCompanion { BoundOwner = player, Role = HavenCompanionRole.Bard };
        var rat = new Rat();
        try
        {
            Mobile.SkillCheckTargetHandler = Server.Misc.SkillCheck.Mobile_SkillCheckTarget;
            Assert.True(HavenRecovery.FindLocation(HavenRecovery.BankLocation, out var location));
            foreach (var mobile in new Mobile[] { player, companion, rat }) { mobile.MoveToWorld(location, Map.Trammel); }
            companion.Skills.Musicianship.Cap = companion.Skills.Discordance.Cap = 120;
            companion.Skills.Musicianship.Base = companion.Skills.Discordance.Base = 120;
            rat.SetResistance(ResistanceType.Physical, 50);
            Assert.False(companion.TryDiscord(player));
            Assert.True(companion.TryDiscord(rat));
            var effect = 0;
            Assert.True(Server.SkillHandlers.Discordance.GetEffect(rat, ref effect));
            Assert.True(effect > 0);
            Assert.False(companion.TryDiscord(rat));
        }
        finally { Mobile.SkillCheckTargetHandler = oldHandler; companion.Delete(); rat.Delete(); player.Delete(); }
    }

    [SkippableFact]
    public void CompanionEquipmentDoesNotWearDuringCombat()
    {
        TileDataRequirement.SkipIfMissing();
        var companion = new HavenCompanion();
        var slime = new Slime();
        var weapon = (BaseWeapon)companion.FindItemOnLayer(Layer.OneHanded);
        var armor = (BaseArmor)companion.FindItemOnLayer(Layer.InnerTorso);
        var cloak = (BaseClothing)companion.FindItemOnLayer(Layer.Cloak);
        try
        {
            Assert.True(HavenRecovery.FindLocation(HavenRecovery.BankLocation, out var location));
            companion.MoveToWorld(location, Map.Trammel);
            slime.MoveToWorld(location, Map.Trammel);
            weapon.MaxHitPoints = 1; weapon.HitPoints = 1;
            armor.MaxHitPoints = 1; armor.HitPoints = 1;
            cloak.MaxHitPoints = 1; cloak.HitPoints = 1;
            for (var i = 0; i < 100; i++)
            {
                armor.OnHit(weapon, 10);
                cloak.OnHit(weapon, 10);
            }
            weapon.OnHit(companion, slime);
            Assert.False(weapon.Deleted);
            Assert.False(armor.Deleted);
            Assert.False(cloak.Deleted);
            Assert.Equal(1, weapon.HitPoints);
            Assert.Equal(1, armor.HitPoints);
            Assert.Equal(1, cloak.HitPoints);
        }
        finally { companion.Delete(); slime.Delete(); }
    }

    [SkippableTheory]
    [InlineData(0x190)]
    [InlineData(0x192)]
    public void BankTravelWorksForLivingPlayersAndGhosts(int body)
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = body };
        try
        {
            var alive = player.Alive;
            player.MoveToWorld(new Point3D(1427, 1695, 0), Map.Felucca);
            HavenRecovery.GoToBank(player);
            Assert.Same(Map.Trammel, player.Map);
            Assert.True(player.InRange(HavenRecovery.BankLocation, 5));
            var healers = new List<HavenBankHealer>();
            foreach (var npc in Map.Trammel.GetMobilesInRange<HavenBankHealer>(player.Location, 2)) { healers.Add(npc); }
            var healer = Assert.Single(healers);
            Assert.Equal("Elias Thorne", healer.Name);
            Assert.Equal(alive, player.Alive);
        }
        finally { player.Delete(); }
    }

    [SkippableFact]
    public void RecoveryNpcsArePresentAndNotDuplicated()
    {
        TileDataRequirement.SkipIfMissing();
        var npcs = new List<BaseCreature>();
        try
        {
            HavenRecovery.EnsureServices();
            HavenRecovery.EnsureServices();
            foreach (var npc in Map.Trammel.GetMobilesInRange<BaseCreature>(HavenRecovery.BankLocation, 15))
            {
                if (npc is HavenBankHealer or HavenCorpseSummoner or HavenPetHealer) { npcs.Add(npc); }
            }
            Assert.Single(npcs.OfType<HavenBankHealer>());
            Assert.Single(npcs.OfType<HavenCorpseSummoner>());
            Assert.Single(npcs.OfType<HavenPetHealer>());
        }
        finally { foreach (var npc in npcs) { npc.Delete(); } }
    }

    [SkippableFact]
    public void HavenPlazaMovesPortalAwayFromArrivalAndDoesNotDuplicateServices()
    {
        TileDataRequirement.SkipIfMissing();
        var items = new List<Item>();
        var oldPortal = new UOOfflineDungeonPortal();
        oldPortal.MoveToWorld(new Point3D(3489, 2582, 20), Map.Trammel);
        try
        {
            HavenContentBootstrap.EnsureHavenPlaza();
            HavenContentBootstrap.EnsureBankServices(Map.Trammel, new Point3D(3483, 2575, 20));
            foreach (var item in Map.Trammel.GetItemsInRange<Item>(HavenRecovery.BankLocation, 20))
            {
                if (item is StarterSupplyStone or HavenUpgradeStone or SpecialRewardStone or FreePetHitchingPost or UOOfflineDungeonPortal or HavenRepairBench or ArcaneSupplyStone or HavenTrainingStone || item.Name == "Haven square flowers") { items.Add(item); }
            }
            Assert.Same(oldPortal, Assert.Single(items.OfType<UOOfflineDungeonPortal>()));
            Assert.False(oldPortal.InRange(HavenRecovery.BankLocation, 6));
            Assert.Single(items.OfType<StarterSupplyStone>());
            Assert.Single(items.OfType<HavenUpgradeStone>());
            Assert.Single(items.OfType<SpecialRewardStone>());
            Assert.Single(items.OfType<FreePetHitchingPost>());
            Assert.Equal(2, items.Count(i => i.Name == "Haven square flowers"));
            Assert.True(Assert.Single(items.OfType<ArcaneSupplyStone>()).InRange(new Point3D(3504, 2583, 14), 1));
            Assert.True(Assert.Single(items.OfType<HavenTrainingStone>()).InRange(new Point3D(3508, 2583, 14), 1));
        }
        finally { foreach (var item in items) { item.Delete(); } oldPortal.Delete(); }
    }

    [SkippableFact]
    public void CorpseRecoveryMovesOnlyOwnedCorpseWithoutCopyingLoot()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190, Name = "recovery test" };
        var other = new PlayerMobile { Player = true, Body = 0x190, Name = "other owner" };
        var npc = new HavenCorpseSummoner();
        var corpse = new Corpse(player, new List<Item>());
        var foreign = new Corpse(other, new List<Item>());
        var gold = new Gold(123);
        corpse.DropItem(gold);
        try
        {
            player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            npc.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            corpse.MoveToWorld(new Point3D(1427, 1695, 0), Map.Felucca);
            player.Corpse = corpse;
            Assert.True(HavenRecovery.RecoverCorpse(player, npc));
            Assert.Same(player.Map, corpse.Map);
            Assert.Equal(player.Location, corpse.Location);
            Assert.Contains(gold, corpse.Items);
            Assert.True(HavenRecovery.RecoverCorpse(player, npc));
            Assert.Equal(123, corpse.GetAmount(typeof(Gold)));
            player.Corpse = foreign;
            Assert.False(HavenRecovery.RecoverCorpse(player, npc));
            player.Corpse = corpse;
            player.MoveToWorld(new Point3D(1427, 1695, 0), Map.Felucca);
            Assert.False(HavenRecovery.RecoverCorpse(player, npc));
            corpse.Delete();
            player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            Assert.False(HavenRecovery.RecoverCorpse(player, npc));
        }
        finally { corpse.Delete(); foreign.Delete(); npc.Delete(); player.Delete(); other.Delete(); }
    }

    [Theory]
    [InlineData(500, 0, 250, true, 250, 0)]
    [InlineData(100, 200, 250, true, 0, 50)]
    [InlineData(100, 100, 250, false, 100, 100)]
    public void StonePaymentUsesWalletAndDoesNotChargeInsufficientFunds(int balance, int gold, int price, bool paid, int expectedWallet, int expectedGold)
    {
        var player = new PlayerMobile { Player = true };
        player.AddItem(new Backpack());
        var wallet = new AdventurersWallet();
        wallet.Deposit(balance);
        player.Backpack.DropItem(wallet);
        if (gold > 0) { player.Backpack.DropItem(new Gold(gold)); }
        try
        {
            Assert.Equal(paid, HavenEconomy.TryPay(player, price));
            Assert.Equal(expectedWallet, wallet.Balance);
            Assert.Equal(expectedGold, player.Backpack.GetAmount(typeof(Gold)));
        }
        finally { player.Delete(); }
    }

    [Fact]
    public void PkSpawnersStayEmptyAndGenerationIsDisabled()
    {
        var spawner = new PlayerBotSpawner("PK", 12, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));
        try
        {
            Assert.False(BotPopulation.PKEnabled);
            spawner.Respawn();
            Assert.Empty(spawner.Spawned);
            Assert.Equal((0, 0), GeneratePKsCommand.PlaceDefault());
        }
        finally { spawner.Delete(); }
    }

    [Fact]
    public void BotDensityKeepsSavedCountsAndNativeSpawnsUnchanged()
    {
        var bot = new PlayerBotSpawner("PK", 12, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));
        var native = new Spawner(12, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, "Banker");
        try
        {
            Assert.Equal(1067, BotPopulation.ScaleCount(1600));
            Assert.Equal(8, BotPopulation.ScaleCount(bot.Count));
            Assert.Equal(8, BotPopulation.ScaleCount(bot.Count));
            Assert.Equal(12, bot.Count);
            Assert.Equal(12, native.Count);
            Assert.Equal(1, BotPopulation.ScaleCount(1));
            Assert.Equal(0, BotPopulation.ScaleCount(0));
            Assert.Equal(1267, BotPopulation.StartupCap);
        }
        finally { bot.Delete(); native.Delete(); }
    }

    [Theory]
    [InlineData(AccessLevel.Player, SkillName.Swords, typeof(ApprenticeBlade))]
    [InlineData(AccessLevel.Owner, SkillName.Archery, typeof(ApprenticeBow))]
    [InlineData(AccessLevel.Player, SkillName.Fencing, typeof(ApprenticeFencer))]
    [InlineData(AccessLevel.Player, SkillName.Macing, typeof(ApprenticeMace))]
    public void StarterKitIncludesBoundEquipmentAndSupplies(AccessLevel access, SkillName skill, Type weaponType)
    {
        var player = new PlayerMobile { Player = true, AccessLevel = access };
        player.AddItem(new Backpack());
        try
        {
            player.Skills[skill].Base = 50;
            StarterProvisioner.Provision(player);
            var pack = player.Backpack;
            var robe = Assert.Single(pack.Items.OfType<NewHavenAdventurersRobe>());
            var book = Assert.Single(pack.Items.OfType<ApprenticeGrimoire>());
            Assert.Same(player, robe.BoundTo);
            Assert.Same(player, book.BoundTo);
            Assert.Equal(ulong.MaxValue, book.Content);
            Assert.Equal(LootType.Blessed, robe.LootType);
            Assert.Equal(weaponType, Assert.Single(pack.Items.OfType<BaseWeapon>()).GetType());
            Assert.Single(pack.Items.OfType<AdventurersWallet>());
            Assert.Single(pack.Items.OfType<CleanupTrashBag>());
            Assert.Single(pack.Items.OfType<SmallBrickHouseDeed>());
            var earrings = Assert.Single(pack.Items.OfType<StarterFortuneEarrings>());
            Assert.Equal(100, earrings.Attributes.LowerRegCost);
            Assert.Equal(200, earrings.Attributes.Luck);
            Assert.Equal(50, Assert.Single(pack.Items.OfType<Bandage>()).Amount);
            if (skill == SkillName.Archery)
            {
                Assert.Equal(100, Assert.Single(pack.Items.OfType<Arrow>()).Amount);
            }
            StarterProvisioner.Provision(player);
            Assert.Single(pack.Items.OfType<SmallBrickHouseDeed>());
            for (var i = 0; i < 4; i++)
            {
                Assert.True(robe.TryUpgrade(player));
            }
            Assert.False(robe.TryUpgrade(player));
            Assert.Equal(150, robe.Attributes.Luck);
        }
        finally { player.Delete(); }
    }

    [Fact]
    public void MissedBundlePreservesBankedProgressionAndCannotBeClaimedTwice()
    {
        var account = CreateTestAccount();
        var player = new PlayerMobile { Player = true, Account = account };
        player.AddItem(new Backpack());
        var robe = new NewHavenAdventurersRobe();
        robe.BindTo(player);
        robe.TryUpgrade(player);
        player.BankBox.DropItem(robe);
        try
        {
            StarterBundleClaims.Claim(player);
            var bundle = Assert.Single(player.Backpack.Items.OfType<Bag>());
            Assert.DoesNotContain(bundle.Items, i => i is NewHavenAdventurersRobe);
            Assert.Single(bundle.Items.OfType<StarterFortuneEarrings>());
            Assert.Equal(1, robe.UpgradeTier);
            player.BankBox.DropItem(bundle);
            StarterBundleClaims.Claim(player);
            Assert.Empty(player.Backpack.Items);
            Assert.Equal("claimed", account.GetTag($"HavenStarterBundle:{player.Serial}"));
        }
        finally { player.Delete(); account.Delete(); }
    }

    [Fact]
    public void FullBackpackDoesNotConsumeBundleClaim()
    {
        var account = CreateTestAccount();
        var player = new PlayerMobile { Player = true, Account = account };
        player.AddItem(new Backpack { MaxItems = 1 });
        try
        {
            StarterBundleClaims.Claim(player);
            Assert.Empty(player.Backpack.Items);
            Assert.Null(account.GetTag($"HavenStarterBundle:{player.Serial}"));
            player.Backpack.MaxItems = 125;
            StarterBundleClaims.Claim(player);
            Assert.Single(player.Backpack.Items.OfType<Bag>());
        }
        finally { player.Delete(); account.Delete(); }
    }

    [Fact]
    public void EveryShopItemHasAnIconAndStatsBeforePurchase()
    {
        var stone = new StarterSupplyStone();
        try
        {
            foreach (ItemListMenu menu in new ItemListMenu[] { new StarterSupplyStone.StarterSupplyMenu(), new SpecialRewardStone.RewardMenu() })
            {
                for (var index = 0; index < menu.Entries.Length; index++)
                {
                    var preview = new HavenItemPreviewGump(stone, menu, index, index / 4);
                    CheckBounds(preview, 540, 460);
                    Assert.Single(preview.Entries.OfType<GumpItem>());
                    var stats = Assert.Single(preview.Entries.OfType<GumpHtml>(), h => h.Scrollbar).Text;
                    Assert.Contains("Loot type", stats);
                    Assert.Contains(preview.Entries.OfType<GumpButton>(), b => b.ButtonID == 1);
                    if (menu.Entries[index].Name.Contains("Fortune Earrings"))
                    {
                        Assert.Contains("Lower Reagent Cost (%): 100", stats);
                        Assert.Contains("Luck: 200", stats);
                    }
                }
            }
            var list = new HavenListGump(stone, new StarterSupplyStone.StarterSupplyMenu());
            Assert.Equal(4, list.Entries.OfType<GumpItem>().Count());
            Assert.Contains(list.Entries.OfType<GumpButton>(), b => b.ButtonID == 10003);
        }
        finally { stone.Delete(); }
    }

    private static Account CreateTestAccount()
    {
        var previous = AccountSecurity.CurrentAlgorithm;
        try
        {
            AccountSecurity.CurrentAlgorithm = PasswordProtectionAlgorithm.SHA2;
            return new Account("haven-test-" + Guid.NewGuid(), "test-only-password");
        }
        finally { AccountSecurity.CurrentAlgorithm = previous; }
    }

    [Fact]
    public void BackpackGrimoireLevelsOnlyOneOwnedBook()
    {
        var player = new PlayerMobile { Player = true };
        var other = new PlayerMobile { Player = true };
        player.AddItem(new Backpack());
        try
        {
            var foreign = new ApprenticeGrimoire();
            foreign.BindTo(other);
            player.Backpack.DropItem(foreign);
            var book = new ApprenticeGrimoire();
            book.BindTo(player);
            player.Backpack.DropItem(book);
            var spare = new ApprenticeGrimoire();
            spare.BindTo(player);
            player.Backpack.DropItem(spare);
            for (var i = 0; i < 30; i++)
            {
                StarterProgression.OnSuccessfulSpellCast(player);
            }
            Assert.Equal(1, foreign.Level);
            Assert.Equal(0, foreign.Experience);
            Assert.Equal(3, book.Level + spare.Level);
            Assert.Equal(0, book.Experience + spare.Experience);
        }
        finally { player.Delete(); other.Delete(); }
    }

    [Fact]
    public void MlPopulationUsesNewHavenAndEveryEnabledFacet()
    {
        var expansion = Core.Expansion;
        try
        {
            Core.Expansion = Expansion.ML;
            var files = HavenWorldPopulation.FindSpawnFiles(SpawnRoot);
            Assert.NotEmpty(files);
            Assert.Contains(files, p => p.Replace('\\', '/').Contains("post-uoml/trammel/Vendors.json"));
            Assert.DoesNotContain(files, p => p.Replace('\\', '/').Contains("uoml/trammel/") && !p.Contains("post-uoml"));
            foreach (var facet in new[] { "felucca", "trammel", "ilshenar", "malas", "tokuno" })
            {
                Assert.Contains(files, p => p.Replace('\\', '/').Contains("/" + facet + "/"));
            }
            Assert.DoesNotContain(files, p => p.Replace('\\', '/').Contains("/termur/"));
            foreach (var dto in HavenWorldPopulation.CreateHavenDefinitions())
            {
                foreach (var entry in dto.Entries)
                {
                    Assert.NotNull(AssemblyHandler.FindTypeByName(entry.SpawnedName));
                }
            }
        }
        finally
        {
            Core.Expansion = expansion;
        }
    }

    [SkippableFact]
    public void NativeNewHavenBankerSpawnsAndSurvivesRepeatRepair()
    {
        TileDataRequirement.SkipIfMissing();
        var file = Path.Combine(SpawnRoot, "Data", "Spawns", "post-uoml", "trammel", "Vendors.json");
        var dto = JsonConfig.Deserialize<List<SpawnerDto>>(file, SpawnerJsonSerializer.Options)
            .First(d => d.Entries.Any(e => e.SpawnedName == "Banker"));
        BaseSpawner spawner = null;
        try
        {
            Assert.True(HavenWorldPopulation.EnsureSpawner(dto));
            var atLocation = new List<BaseSpawner>();
            foreach (var item in dto.Map.GetItemsAt<BaseSpawner>(dto.Location)) { atLocation.Add(item); }
            spawner = Assert.Single(atLocation);
            Assert.Contains(spawner.Spawned.Keys, s => s is Banker);
            var banker = spawner.Spawned.Keys.OfType<Banker>().First();
            Assert.False(HavenWorldPopulation.EnsureSpawner(dto));
            Assert.False(banker.Deleted);
            Assert.Contains(banker, spawner.Spawned.Keys);
        }
        finally
        {
            spawner?.Delete();
        }
    }

    [SkippableFact]
    public void EveryNativeBankGetsAllFiveServicesWithoutDuplicates()
    {
        TileDataRequirement.SkipIfMissing();
        var expansion = Core.Expansion;
        var created = new HashSet<Item>();
        try
        {
            Core.Expansion = Expansion.ML;
            var banks = 0;
            foreach (var file in HavenWorldPopulation.FindSpawnFiles(SpawnRoot).Where(p => Path.GetFileName(p) == "Vendors.json"))
            {
                foreach (var dto in JsonConfig.Deserialize<List<SpawnerDto>>(file, SpawnerJsonSerializer.Options))
                {
                    if (!dto.Entries.Any(e => e.SpawnedName == "Banker"))
                    {
                        continue;
                    }
                    banks++;
                    HavenContentBootstrap.EnsureBankServices(dto.Map, dto.Location);
                    var serviceLocation = dto.Map == Map.Trammel && Utility.InRange(dto.Location, new Point3D(3490, 2582, 20), 20)
                        ? new Point3D(3506, 2576, 14) : dto.Location;
                    var before = Services(dto.Map, serviceLocation);
                    foreach (var item in before) { created.Add(item); }
                    Assert.Contains(before, i => i is StarterSupplyStone);
                    Assert.Contains(before, i => i is HavenUpgradeStone);
                    Assert.Contains(before, i => i is SpecialRewardStone);
                    Assert.Contains(before, i => i is FreePetHitchingPost);
                    Assert.Contains(before, i => i is UOOfflineDungeonPortal);
                    Assert.Contains(before, i => i is HavenRepairBench);
                    Assert.Contains(before, i => i is ArcaneSupplyStone);
                    HavenContentBootstrap.EnsureBankServices(dto.Map, dto.Location);
                    Assert.Equal(before.Count, Services(dto.Map, serviceLocation).Count);
                }
            }
            Assert.True(banks >= 30, $"Only {banks} bank spawn points checked.");
        }
        finally
        {
            foreach (var item in created) { item.Delete(); }
            Core.Expansion = expansion;
        }
    }

    private static List<Item> Services(Map map, Point3D location)
    {
        var result = new List<Item>();
        foreach (var item in map.GetItemsInRange<Item>(location, 18))
        {
            if (item is StarterSupplyStone or HavenUpgradeStone or SpecialRewardStone or FreePetHitchingPost or UOOfflineDungeonPortal or HavenRepairBench or ArcaneSupplyStone or HavenTrainingStone)
            {
                result.Add(item);
            }
        }
        return result;
    }

    [SkippableFact]
    public void HavenInstructorsAnimalsAndMountsProduceLivingNpcs()
    {
        TileDataRequirement.SkipIfMissing();
        var created = new List<BaseSpawner>();
        try
        {
            foreach (var dto in HavenWorldPopulation.CreateHavenDefinitions())
            {
                Assert.True(HavenWorldPopulation.EnsureSpawner(dto));
                BaseSpawner spawner = null;
                foreach (var item in dto.Map.GetItemsAt<BaseSpawner>(dto.Location)) { spawner = item; }
                Assert.NotNull(spawner);
                created.Add(spawner);
                Assert.False(spawner.IsEmpty, $"No NPCs spawned for {dto.Name} at {dto.Location}.");
                Assert.Contains(spawner.Spawned.Keys, s => s is BaseCreature { Deleted: false, Alive: true });
            }
            Assert.Equal(33, created.Count);
        }
        finally
        {
            foreach (var spawner in created) { spawner.Delete(); }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void GmTabsFitWithinWindowEvenWithFullDraft(int tab)
    {
        var player = new PlayerMobile();
        try
        {
            foreach (var name in new[] { "BankSitter", "Wander", "Idle", "Adventurer", "Traveler" })
            {
                BotPanelState.AddDraftEntry(player, name, 5);
            }
            BotPanelState.AddDraftEntry(player, "Wander", 3);
            Assert.Equal(5, BotPanelState.GetDraft(player).Count);
            var gump = new BotPanelGump(player, true, tab);
            CheckBounds(gump, 620, 560);
        }
        finally
        {
            BotPanelState.ClearDraft(player);
            player.Delete();
        }
    }

    [Fact]
    public void LongStoneMenusHaveReadableRowsAndNavigation()
    {
        var stone = new StarterSupplyStone();
        try
        {
            var entries = Enumerable.Range(0, 32).Select(i => new ItemListEntry("Destination " + i, 0)).ToArray();
            var menu = new ItemListMenu("Dungeon travel", entries);
            var first = new HavenListGump(stone, menu);
            var last = new HavenListGump(stone, menu, 7);
            CheckBounds(first, 540, 460);
            CheckBounds(last, 540, 460);
            Assert.Contains(first.Entries.OfType<GumpButton>(), b => b.ButtonID == 10002);
            Assert.DoesNotContain(first.Entries.OfType<GumpButton>(), b => b.ButtonID == 10001);
            Assert.Contains(last.Entries.OfType<GumpButton>(), b => b.ButtonID == 10001);
            Assert.DoesNotContain(last.Entries.OfType<GumpButton>(), b => b.ButtonID == 10002);
            Assert.Equal(12, first.Entries.OfType<GumpButton>().Count(b => b.ButtonID is > 0 and < 10000));
        }
        finally { stone.Delete(); }
    }

    [SkippableFact]
    public void CompanionDefendsOwnerWithoutCombatTarget()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var companion = new HavenCompanion { BoundOwner = owner };
        var enemy = new OldHavenWarden();
        try
        {
            foreach (var mobile in new Mobile[] { owner, companion, enemy }) { mobile.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); }
            companion.SetControlMaster(owner);
            owner.AggressiveAction(enemy);
            owner.Combatant = null;
            companion.ControlOrder = OrderType.Stay;
            Assert.False(companion.DefendOwner());
            companion.ControlOrder = OrderType.Follow;
            Assert.True(companion.DefendOwner());
            Assert.Same(enemy, companion.ControlTarget);
        }
        finally { companion.Delete(); enemy.Delete(); owner.Delete(); }
    }

    [Fact]
    public void EarringsEvolveForFightersAndCasters()
    {
        var earrings = new StarterFortuneEarrings();
        try
        {
            var strength = earrings.Attributes.BonusStr;
            var dexterity = earrings.Attributes.BonusDex;
            var intelligence = earrings.Attributes.BonusInt;
            HavenGearExperience.Gain(earrings, 400);
            Assert.Equal(5, HavenGearExperience.Find(earrings).Level);
            Assert.Equal(strength + 1, earrings.Attributes.BonusStr);
            Assert.Equal(dexterity + 1, earrings.Attributes.BonusDex);
            Assert.Equal(intelligence + 1, earrings.Attributes.BonusInt);
            HavenGearExperience.Gain(earrings, 1);
            Assert.Equal(intelligence + 1, earrings.Attributes.BonusInt);
        }
        finally { earrings.Delete(); }
    }

    [Fact]
    public void MasteryFocusRequiresSkillAndDoesNotStack()
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        var manual = new HavenMasteryManual();
        owner.Backpack.DropItem(manual);
        try
        {
            Assert.False(manual.Activate(owner, 0));
            owner.Skills.Swords.Base = 100;
            Assert.True(manual.Activate(owner, 0));
            Assert.True(manual.Activate(owner, 0));
            Assert.Equal(6, owner.GetStatMod("HavenMasteryStr").Offset);
            owner.Skills.Magery.Base = 100;
            Assert.True(manual.Activate(owner, 2));
            Assert.Equal(0, owner.GetStatMod("HavenMasteryStr").Offset);
            Assert.Equal(9, owner.GetStatMod("HavenMasteryInt").Offset);
        }
        finally { owner.Delete(); }
    }
    [SkippableFact]
    public void BandagesCapSelfHealingAndVeterinaryAtTwoSeconds()
    {
        TileDataRequirement.SkipIfMissing();
        var healer = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100, RawDex = 10 };
        var pet = new Horse();
        try
        {
            healer.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            pet.MoveToWorld(healer.Location, healer.Map);
            pet.SetControlMaster(healer);
            healer.Hits = 1;
            pet.Hits = 1;
            var self = BandageContext.BeginHeal(healer, healer);
            Assert.NotNull(self);
            Assert.InRange(self.Delay.TotalSeconds, 0.1, 2);
            self.StopHeal();
            var veterinary = BandageContext.BeginHeal(healer, pet);
            Assert.NotNull(veterinary);
            Assert.InRange(veterinary.Delay.TotalSeconds, 0.1, 2);
            veterinary.StopHeal();
        }
        finally { BandageContext.GetContext(healer)?.StopHeal(); pet.Delete(); healer.Delete(); }
    }
    [SkippableFact]
    public void DeadCompanionRecoversAfterFiveSecondsWithoutLosingGear()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var companion = new HavenCompanion { BoundOwner = owner, IsBonded = true };
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            companion.MoveToWorld(owner.Location, owner.Map);
            companion.SetControlMaster(owner);
            var pack = companion.Backpack;
            companion.IsDeadPet = true;
            companion.Hits = 0;
            var now = Core.Now;
            Assert.False(companion.RecoverFromDeath(now));
            Assert.False(companion.RecoverFromDeath(now.AddSeconds(4)));
            Assert.True(companion.RecoverFromDeath(now.AddSeconds(5)));
            Assert.False(companion.IsDeadPet);
            Assert.Equal(companion.HitsMax, companion.Hits);
            Assert.Same(pack, companion.Backpack);
            companion.IsDeadPet = true;
            Assert.True(companion.RecoverFromDeath(now, true));
        }
        finally { companion.Delete(); owner.Delete(); }
    }
    [Fact]
    public void TamingLoreAndFocusDoNotConsumeSkillBudgetOrLowerOtherSkills()
    {
        var player = new PlayerMobile { Player = true, Body = 0x190, SkillsCap = 1000 };
        try
        {
            player.Skills.Swords.Base = 100;
            player.Skills.AnimalTaming.Base = 20;
            player.Skills.AnimalLore.Base = 20;
            player.Skills.Focus.Base = 20;
            Assert.Equal(1000, HavenFreeSkills.CountedTotal(player));
            Server.Misc.SkillCheck.Gain(player, player.Skills.AnimalTaming);
            Server.Misc.SkillCheck.Gain(player, player.Skills.AnimalLore);
            Server.Misc.SkillCheck.Gain(player, player.Skills.Focus);
            Assert.Equal(20.1, player.Skills.Focus.Base);
            Assert.Equal(20.1, player.Skills.AnimalTaming.Base);
            Assert.Equal(20.1, player.Skills.AnimalLore.Base);
            Assert.Equal(100.0, player.Skills.Swords.Base);
            player.Skills.AnimalLore.SetLockNoRelay(SkillLock.Down);
            Server.Misc.SkillCheck.Gain(player, player.Skills.Healing);
            Assert.Equal(0.0, player.Skills.Healing.Base);
            Assert.Equal(20.1, player.Skills.AnimalLore.Base);
            player.Skills.Swords.Base = 99;
            Server.Misc.SkillCheck.Gain(player, player.Skills.Healing);
            Assert.True(player.Skills.Healing.Base > 0);
            player.Skills.AnimalTaming.Base = player.Skills.AnimalTaming.Cap;
            Server.Misc.SkillCheck.Gain(player, player.Skills.AnimalTaming);
            Assert.Equal(player.Skills.AnimalTaming.Cap, player.Skills.AnimalTaming.Base);
            Assert.Equal(1000, player.SkillsCap);
        }
        finally { player.Delete(); }
    }
    [SkippableFact]
    public void TrainersTeachFreeSkillsAtTotalCap()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190, SkillsCap = 1000 };
        var trainer = new AnimalTrainer();
        try
        {
            player.Skills.Swords.Base = 100;
            trainer.Skills.AnimalLore.Base = 90;
            trainer.Skills.AnimalTaming.Base = 90;
            trainer.Skills.Focus.Base = 90;
            foreach (var skill in new[] { SkillName.AnimalTaming, SkillName.AnimalLore, SkillName.Focus })
            {
                var learned = 0;
                Assert.Equal(BaseCreature.TeachResult.Success, trainer.CheckTeachSkills(skill, player, 100, ref learned, true));
                Assert.True(player.Skills[skill].Base > 0);
            }
            Assert.Equal(1000, HavenFreeSkills.CountedTotal(player));
        }
        finally { trainer.Delete(); player.Delete(); }
    }
    [Fact]
    public void ShopTooltipsKeepAllBonusesInOneArgumentAndShowInlineStats()
    {
        var bracelet = SpecialBraceletFactory.CreateRandom();
        try
        {
            var text = HavenItemPreviewGump.Tooltip(bracelet);
            Assert.DoesNotContain("\n", text);
            Assert.DoesNotContain("\t", text);
            Assert.False(text.StartsWith("Weight:"));
            Assert.NotEmpty(HavenItemPreviewGump.ShortStats(bracelet));
        }
        finally { bracelet.Delete(); }
        for (var category = 0; category < 5; category++)
        {
            var menu = new HavenTrainingStone.Menu(category);
            for (var i = 0; i < menu.Entries.Length; i++) { var item = menu.CreateItem(i); Assert.NotNull(item); item.Delete(); }
        }
    }

    [SkippableFact]
    public void PetScrollBundleHasSixSkillsAndRaisesOnlyOwnedPetCaps()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var other = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        var pet = new Horse();
        var bundle = new HavenPetScrollBundle(110);
        owner.Backpack.DropItem(bundle);
        try
        {
            foreach (var mobile in new Mobile[] { owner, other, pet }) { mobile.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); }
            var scrolls = bundle.Items.OfType<HavenPetPowerScroll>().ToArray();
            Assert.Equal(6, scrolls.Length);
            Assert.DoesNotContain(scrolls, s => s.Skill is SkillName.Magery or SkillName.EvalInt);
            var scroll = scrolls[0];
            pet.SetControlMaster(other);
            Assert.False(scroll.ApplyTo(owner, pet));
            pet.SetControlMaster(owner);
            var before = pet.Skills[scroll.Skill].Base;
            Assert.True(scroll.ApplyTo(owner, pet));
            Assert.Equal(110, pet.Skills[scroll.Skill].Cap);
            Assert.Equal(before, pet.Skills[scroll.Skill].Base);
            var duplicate = new HavenPetPowerScroll(scroll.Skill, 110);
            owner.Backpack.DropItem(duplicate);
            Assert.False(duplicate.ApplyTo(owner, pet));
            Assert.False(duplicate.Deleted);
        }
        finally { pet.Delete(); owner.Delete(); other.Delete(); }
    }

    private sealed class BondableTestHorse : Horse { public override bool IsBondable => true; }

    [SkippableFact]
    public void BondingPotionAndReusableLeashPreserveTheOwnedPet()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        var pet = new BondableTestHorse();
        var potion = new HavenBondingPotion();
        var leash = new HavenPetLeash();
        owner.Backpack.DropItem(potion); owner.Backpack.DropItem(leash);
        var post = new HavenHouseHitchingPost();
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            pet.MoveToWorld(owner.Location, owner.Map);
            pet.SetControlMaster(owner);
            Assert.True(potion.ApplyTo(owner, pet));
            Assert.True(pet.IsBonded);
            Assert.True(potion.Deleted);
            leash.OnDoubleClick(owner);
            owner.Target.Invoke(owner, pet);
            Assert.Equal(Map.Internal, pet.Map);
            var token = owner.Backpack.FindItemByType<ShrunkenPet>();
            Assert.NotNull(token);
            Assert.Same(pet, token.Pet);
            token.OnDoubleClick(owner);
            Assert.Equal(owner.Map, pet.Map);
            Assert.Same(owner, pet.ControlMaster);
            Assert.False(leash.Deleted);
            post.MoveToWorld(owner.Location, owner.Map);
            Assert.False(post.CanUse(owner));
        }
        finally { post.Delete(); pet.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void CompanionBaseStatsGrowWithTrainingWithoutStackingOrRefilling()
    {
        TileDataRequirement.SkipIfMissing();
        var companion = new HavenCompanion();
        try
        {
            companion.Hits = companion.Stam = companion.Mana = 1;
            var time = companion.LastTraining.AddHours(4);
            companion.UpdateTraining(time);
            Assert.True(companion.RawStr > 100 && companion.RawDex > 80 && companion.RawInt > 60);
            Assert.Equal(1, companion.Hits);
            Assert.Equal(1, companion.Stam);
            Assert.Equal(1, companion.Mana);
            var strength = companion.RawStr;
            companion.UpdateTraining(time);
            Assert.Equal(strength, companion.RawStr);
            companion.Role = HavenCompanionRole.Caster;
            companion.UpdateTraining(time.AddSeconds(1));
            Assert.Equal(strength, companion.RawStr);
        }
        finally { companion.Delete(); }
    }
    [SkippableFact]
    public void CompanionSupportReachesTwelveTilesButNotThirteen()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        var companion = new HavenCompanion { BoundOwner = owner };
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            owner.Hits = 1;
            companion.MoveToWorld(new Point3D(owner.X + 13, owner.Y, owner.Z), owner.Map);
            Assert.False(companion.Support(owner));
            var found = false;
            for (var x = -12; x <= 12 && !found; x++)
            {
                for (var y = -12; y <= 12 && !found; y++)
                {
                    if (Math.Max(Math.Abs(x), Math.Abs(y)) != 12) { continue; }
                    var point = new Point3D(owner.X + x, owner.Y + y, owner.Map.GetAverageZ(owner.X + x, owner.Y + y));
                    if (!owner.Map.CanSpawnMobile(point)) { continue; }
                    companion.MoveToWorld(point, owner.Map);
                    if (!companion.InLOS(owner)) { continue; }
                    Assert.True(companion.Support(owner));
                    found = true;
                }
            }
            Assert.True(found);
            Assert.True(owner.Hits > 1);
        }
        finally { companion.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void HavenLuckStacksWithGearAndEndsOutsideIsland()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        var earrings = new StarterFortuneEarrings();
        try
        {
            player.AddItem(earrings);
            player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            Assert.Equal(1200, player.Luck);
            player.MoveToWorld(new Point3D(3670, 2587, 0), Map.Trammel);
            Assert.Equal(1200, player.Luck);
            player.MoveToWorld(new Point3D(1428, 1697, 0), Map.Trammel);
            Assert.Equal(200, player.Luck);
            player.MoveToWorld(HavenRecovery.BankLocation, Map.Felucca);
            Assert.Equal(200, player.Luck);
        }
        finally { player.Delete(); }
    }
    [SkippableFact]
    public void HavenTrainingAcceleratesToHundredOnlyAndRespectsLocks()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        try
        {
            player.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            var skill = player.Skills.AnimalLore;
            skill.Cap = 120;
            skill.Base = 50;
            Assert.Equal(5.0, HavenNewcomerTraining.ChanceMultiplier(player, skill));
            Server.Misc.SkillCheck.Gain(player, skill);
            Assert.Equal(50.5, skill.Base);
            skill.Base = 99.8;
            Server.Misc.SkillCheck.Gain(player, skill);
            Assert.Equal(100.0, skill.Base);
            Assert.Equal(1.0, HavenNewcomerTraining.ChanceMultiplier(player, skill));
            Server.Misc.SkillCheck.Gain(player, skill);
            Assert.Equal(100.1, skill.Base);
            skill.Base = 50;
            skill.SetLockNoRelay(SkillLock.Locked);
            Server.Misc.SkillCheck.Gain(player, skill);
            Assert.Equal(50.0, skill.Base);
            skill.SetLockNoRelay(SkillLock.Up);
            player.MoveToWorld(new Point3D(1428, 1697, 0), Map.Trammel);
            Server.Misc.SkillCheck.Gain(player, skill);
            Assert.Equal(50.1, skill.Base);
        }
        finally { player.Delete(); }
    }
    [Fact]
    public void PlayerLoginAppliesThousandPointBudgetWithoutCountingTamingAndLore()
    {
        var player = new PlayerMobile { Player = true, SkillsCap = 7200 };
        try
        {
            player.Skills.AnimalTaming.Base = 100;
            player.Skills.AnimalLore.Base = 100;
            player.Skills.Swords.Base = 80;
            HavenFreeSkills.ApplyPlayerCap(player);
            Assert.Equal(10000, player.SkillsCap);
            Assert.Equal(800, HavenFreeSkills.CountedTotal(player));
            HavenFreeSkills.ApplyPlayerCap(player);
            Assert.Equal(10000, player.SkillsCap);
            Assert.Equal(100, player.Skills.AnimalTaming.Base);
        }
        finally { player.Delete(); }
    }
    [Fact]
    public void WalletMarksDepositSpendAndWithdrawWithoutLosingCurrency()
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        var other = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack()); other.AddItem(new Backpack());
        var wallet = new AdventurersWallet { Balance = 50000, AstralShards = 12 };
        owner.Backpack.DropItem(wallet);
        owner.Backpack.DropItem(new HavenMark(10));
        try
        {
            Assert.Equal(0, wallet.DepositBackpackMarks(other));
            Assert.Equal(10, wallet.DepositBackpackMarks(owner));
            Assert.Equal(0, wallet.DepositBackpackMarks(owner));
            owner.Backpack.DropItem(new HavenMark(5));
            Assert.False(HavenEconomy.TryPayMarks(owner, 16));
            Assert.Equal(10, wallet.HavenMarks);
            Assert.Equal(5, owner.Backpack.GetAmount(typeof(HavenMark)));
            SpecialRewardStone.RewardMenu.Buy(owner, 0);
            Assert.Equal(0, wallet.HavenMarks);
            Assert.Equal(0, owner.Backpack.GetAmount(typeof(HavenMark)));
            Assert.Equal(50000, wallet.Balance);
            Assert.Equal(12, wallet.AstralShards);
            wallet.HavenMarks = 30;
            owner.Backpack.MaxItems = owner.Backpack.Items.Count;
            SpecialRewardStone.RewardMenu.Buy(owner, 1);
            Assert.Equal(30, wallet.HavenMarks);
            Assert.False(wallet.WithdrawMarks(owner, 5));
            Assert.Equal(30, wallet.HavenMarks);
            owner.Backpack.MaxItems = 125;
            Assert.True(wallet.WithdrawMarks(owner, 5));
            Assert.Equal(25, wallet.HavenMarks);
            Assert.Equal(5, owner.Backpack.GetAmount(typeof(HavenMark)));
            CheckBounds(new HavenWalletGump(wallet), 460, 395);
        }
        finally { owner.Delete(); other.Delete(); }
    }

    [SkippableFact]
    public void RobeUpgradeUsesStoredMarksBeforeGold()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        var wallet = new AdventurersWallet { HavenMarks = 2, Balance = 5000 };
        var robe = new NewHavenAdventurersRobe();
        var stone = new HavenUpgradeStone();
        owner.Backpack.DropItem(wallet); owner.Backpack.DropItem(robe);
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            stone.MoveToWorld(owner.Location, owner.Map);
            stone.ApplyUpgrade(owner, 0);
            Assert.Equal(1, robe.UpgradeTier);
            Assert.Equal(1, wallet.HavenMarks);
            Assert.Equal(5000, wallet.Balance);
        }
        finally { stone.Delete(); owner.Delete(); }
    }
    [Fact]
    public void ChampionPendantRequiresMarksAndCannotUseGoldFallback()
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        owner.AddItem(new Backpack());
        var wallet = new AdventurersWallet { Balance = 1000000, HavenMarks = 249 };
        owner.Backpack.DropItem(wallet);
        try
        {
            SpecialRewardStone.RewardMenu.Buy(owner, 9);
            Assert.Null(owner.Backpack.FindItemByType<HavenChampionPendant>());
            Assert.Equal(1000000, wallet.Balance);
            Assert.Equal(249, wallet.HavenMarks);
            wallet.HavenMarks = 250;
            SpecialRewardStone.RewardMenu.Buy(owner, 9);
            var pendant = owner.Backpack.FindItemByType<HavenChampionPendant>();
            Assert.NotNull(pendant);
            Assert.Equal(1000, pendant.Attributes.Luck);
            Assert.Equal(40, pendant.Attributes.SpellDamage);
            Assert.Equal(40, pendant.Attributes.WeaponDamage);
            Assert.Equal(0, wallet.HavenMarks);
        }
        finally { owner.Delete(); }
    }

    [SkippableFact]
    public void BardSongsAdaptAndClearTheirStatBonuses()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190, Str = 100, Dex = 80, Int = 60 };
        var companion = new HavenCompanion { BoundOwner = owner, Role = HavenCompanionRole.Bard };
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            companion.MoveToWorld(owner.Location, owner.Map);
            companion.Skills.Musicianship.Base = companion.Skills.Peacemaking.Base = 100;
            owner.Hits = 1;
            Assert.Equal("Recovery", companion.SelectSong(owner));
            companion.BardSupport(owner);
            Assert.True(owner.Str > 100);
            companion.ClearSongs();
            Assert.Equal(100, owner.Str);
            owner.Hits = owner.HitsMax; owner.Mana = 0;
            Assert.Equal("Arcane", companion.SelectSong(owner));
            companion.BardSupport(owner);
            Assert.True(owner.Int > owner.RawInt);
            companion.Delete();
            Assert.Equal(owner.RawInt, owner.Int);
            Assert.Equal(TimeSpan.FromSeconds(60), HavenCompanion.SongBuff("Arcane", 3, 3, 6).Duration);
        }
        finally { companion.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void BardProvokesTwoHostileAttackersUsingNativeSkill()
    {
        TileDataRequirement.SkipIfMissing();
        var oldHandler = Mobile.SkillCheckTargetHandler;
        var owner = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        var companion = new HavenCompanion { BoundOwner = owner, Role = HavenCompanionRole.Bard };
        var first = new Rat(); var second = new Rat();
        try
        {
            Mobile.SkillCheckTargetHandler = Server.Misc.SkillCheck.Mobile_SkillCheckTarget;
            foreach (var mobile in new Mobile[] { owner, companion, first, second }) { mobile.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); }
            owner.Hits = owner.HitsMax;
            companion.Skills.Musicianship.Base = companion.Skills.Provocation.Base = 120;
            first.Karma = second.Karma = -1000;
            second.Combatant = owner;
            companion.ThinkBardCombat(first);
            Assert.True(first.BardProvoked);
            Assert.Same(second, first.BardTarget);
        }
        finally { Mobile.SkillCheckTargetHandler = oldHandler; companion.Delete(); first.Delete(); second.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void BardUsesPeacemakingWhenOwnerIsInDanger()
    {
        TileDataRequirement.SkipIfMissing();
        var oldHandler = Mobile.SkillCheckTargetHandler;
        var owner = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        var companion = new HavenCompanion { BoundOwner = owner, Role = HavenCompanionRole.Bard };
        var enemy = new Rat { Karma = -1000 };
        try
        {
            Mobile.SkillCheckTargetHandler = Server.Misc.SkillCheck.Mobile_SkillCheckTarget;
            foreach (var mobile in new Mobile[] { owner, companion, enemy }) { mobile.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); }
            owner.Hits = 1;
            companion.Skills.Musicianship.Base = companion.Skills.Peacemaking.Base = 120;
            companion.ThinkBardCombat(enemy);
            Assert.True(enemy.BardPacified);
            Assert.Equal(OrderType.Follow, companion.ControlOrder);
        }
        finally { Mobile.SkillCheckTargetHandler = oldHandler; companion.Delete(); enemy.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void TamingAssistCalmsWithoutAttackingAndEndsWhenTamed()
    {
        TileDataRequirement.SkipIfMissing();
        var oldHandler = Mobile.SkillCheckTargetHandler;
        var owner = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        var stranger = new PlayerMobile();
        var companion = new HavenCompanion { BoundOwner = owner, Role = HavenCompanionRole.Bard };
        var animal = new Horse();
        try
        {
            Mobile.SkillCheckTargetHandler = Server.Misc.SkillCheck.Mobile_SkillCheckTarget;
            foreach (var mobile in new Mobile[] { owner, companion, animal }) { mobile.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); }
            companion.Skills.Musicianship.Base = companion.Skills.Peacemaking.Base = 120;
            Assert.False(companion.StartTamingAssist(stranger, animal));
            Assert.True(companion.StartTamingAssist(owner, animal));
            Assert.False(companion.CanBeHarmful(animal, false));
            owner.Combatant = animal;
            Assert.False(companion.DefendOwner());
            companion.ThinkTamingAssist();
            Assert.True(animal.BardPacified);
            Assert.Null(companion.Combatant);
            Assert.False(companion.CanBeHarmful(animal, false));
            owner.Hits = 1;
            companion.Mana = companion.ManaMax;
            Assert.True(companion.Support(owner));
            Assert.True(owner.Hits > 1);
            animal.Controlled = true;
            companion.ThinkTamingAssist();
            Assert.False(companion.TamingAssistActive);
        }
        finally { Mobile.SkillCheckTargetHandler = oldHandler; companion.Delete(); animal.Delete(); stranger.Delete(); owner.Delete(); }
    }
    [SkippableFact]
    public void HavenHousingAllowsClearLandButProtectsServicesAndOtherRegions()
    {
        TileDataRequirement.SkipIfMissing();
        var map = Map.Trammel;
        var island = new Server.Regions.NoHousingRegion("Haven Island", map, 90, new Rectangle3D(3314, 2345, -128, 500, 750, 256));
        var town = new Server.Regions.TownRegion("New Haven", map, island);
        var shop = new Server.Regions.NoHousingRegion("the New Haven Bank", map, town);
        var other = new Server.Regions.TownRegion("Britain", map, 50);
        var owner = new PlayerMobile { Player = true };
        try
        {
            island.Register();
            var point = new Point3D(3400, 2500, 0);
            Assert.True(HavenHousing.IsResidentialRegion(island, map, point));
            Assert.True(HavenHousing.IsResidentialRegion(town, map, point));
            Assert.False(HavenHousing.IsResidentialRegion(other, map, point));
            Assert.False(HavenHousing.IsResidentialRegion(island, Map.Felucca, point));
            Assert.True(HavenHousing.BlocksFootprint(shop, map, point));
            Assert.True(HavenHousing.BlocksFootprint(island, map, HavenRecovery.BankLocation));
            owner.MoveToWorld(point, map);
            Assert.Equal(Server.Multis.HousePlacementResult.BadRegion,
                Server.Multis.HousePlacement.Check(owner, 0x64, HavenRecovery.BankLocation, out _));
            Point3D? clear = null;
            for (var x = 3340; x < 3790 && clear == null; x += 5)
            {
                for (var y = 2490; y < 3070 && clear == null; y += 5)
                {
                    var candidate = new Point3D(x, y, map.GetAverageZ(x, y));
                    if (Server.Multis.HousePlacement.Check(owner, 0x64, candidate, out _) == Server.Multis.HousePlacementResult.Valid) { clear = candidate; }
                }
            }
            Assert.NotNull(clear);
            var obstruction = new Static(0x6) { Movable = false };
            try
            {
                obstruction.MoveToWorld(clear.Value, map);
                Assert.NotEqual(Server.Multis.HousePlacementResult.Valid,
                    Server.Multis.HousePlacement.Check(owner, 0x64, clear.Value, out _));
            }
            finally { obstruction.Delete(); }
        }
        finally { island.Unregister(); owner.Delete(); }
    }
    [SkippableFact]
    public void HavenRewardsAndExistingSpawnerTimersUseFasterProgression()
    {
        TileDataRequirement.SkipIfMissing();
        var boss = new OldHavenBossSpawner();
        var steeds = new VampiricSteedSpawner();
        var warden = new OldHavenWarden();
        try
        {
            boss.MinDelay = TimeSpan.FromMinutes(10); boss.MaxDelay = TimeSpan.FromMinutes(15);
            steeds.MinDelay = TimeSpan.FromMinutes(25); steeds.MaxDelay = TimeSpan.FromMinutes(40);
            boss.Running = steeds.Running = false;
            boss.NextSpawn = steeds.NextSpawn = TimeSpan.FromMinutes(20);
            HavenContentBootstrap.ApplyRespawnTiming(boss);
            HavenContentBootstrap.ApplyRespawnTiming(steeds);
            Assert.Equal(TimeSpan.FromMinutes(2), boss.MinDelay);
            Assert.Equal(TimeSpan.FromMinutes(3), boss.MaxDelay);
            Assert.True(boss.NextSpawn <= boss.MaxDelay);
            Assert.Equal(TimeSpan.FromSeconds(30), steeds.MinDelay);
            Assert.Equal(TimeSpan.FromSeconds(60), steeds.MaxDelay);
            Assert.True(steeds.NextSpawn <= steeds.MaxDelay);
            Assert.Equal(0, warden.Backpack.GetAmount(typeof(HavenMark)));
            warden.GenerateLoot(false);
            Assert.InRange(warden.Backpack.GetAmount(typeof(HavenMark)), 3, 6);
            Assert.True(warden.Backpack.GetAmount(typeof(Gold)) >= 1500);
            Assert.True(warden.Backpack.Items.Count(i => i is BaseWeapon or BaseArmor or BaseJewel or BaseHat) >= 2);
        }
        finally { boss.Delete(); steeds.Delete(); warden.Delete(); }
    }
    [SkippableFact]
    public void MatchingJewelryBonusesRequireEquippedPairAndDoNotAccumulate()
    {
        TileDataRequirement.SkipIfMissing();
        var wearer = new PlayerMobile { Player = true };
        wearer.AddItem(new Backpack());
        var ring = new HavenSetRing(6);
        var bracelet = new BraceletOfFortune();
        var talisman = new HavenConcordTalisman();
        try
        {
            wearer.AddItem(ring); wearer.AddItem(bracelet);
            Assert.Equal(650, AosAttributes.GetValue(wearer, AosAttribute.Luck));
            wearer.AddItem(talisman);
            Assert.Equal(1200, AosAttributes.GetValue(wearer, AosAttribute.Luck));
            wearer.Backpack.DropItem(bracelet);
            Assert.Equal(450, AosAttributes.GetValue(wearer, AosAttribute.Luck));
            wearer.AddItem(bracelet);
            Assert.Equal(1200, AosAttributes.GetValue(wearer, AosAttribute.Luck));
            wearer.Backpack.DropItem(talisman);
            Assert.Equal(650, AosAttributes.GetValue(wearer, AosAttribute.Luck));
            for (var i = 0; i < 9; i++)
            {
                var preview = new HavenSetRing(i);
                try { Assert.Contains(HavenJewelrySets.Descriptions[i], HavenItemPreviewGump.Describe(preview)); }
                finally { preview.Delete(); }
            }
        }
        finally { wearer.Delete(); ring.Delete(); bracelet.Delete(); talisman.Delete(); }
    }

    [SkippableFact]
    public void RingAndTalismanPurchasesUseMarksAndBothEvolve()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true };
        owner.AddItem(new Backpack());
        var wallet = new AdventurersWallet { HavenMarks = 180, Balance = 1000000 };
        try
        {
            owner.Backpack.DropItem(wallet);
            SpecialRewardStone.RewardMenu.Buy(owner, 10);
            SpecialRewardStone.RewardMenu.Buy(owner, 19);
            Assert.Equal(0, wallet.HavenMarks);
            Assert.Equal(1000000, wallet.Balance);
            var ring = owner.Backpack.FindItemByType<HavenSetRing>();
            var talisman = owner.Backpack.FindItemByType<HavenConcordTalisman>();
            Assert.NotNull(ring); Assert.NotNull(talisman);
            owner.AddItem(ring); owner.AddItem(talisman);
            HavenGearExperience.GainEquipped(owner, 400);
            Assert.Equal(5, HavenGearExperience.Find(ring).Level);
            Assert.Equal(5, HavenGearExperience.Find(talisman).Level);
            SpecialRewardStone.RewardMenu.Buy(owner, 11);
            Assert.Null(owner.Backpack.FindItemByType<HavenSetRing>());
            Assert.Equal(1000000, wallet.Balance);
        }
        finally { owner.Delete(); }
    }

    [SkippableFact]
    public void CompanionWeaponsUnlockAreaAndUniversalSlayerWithoutHarmingOwnerOrPets()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190, Str = 100 };
        var companion = new HavenCompanion { BoundOwner = owner };
        var enemy = new Rat { Karma = -1000, Str = 100 };
        var secondary = new Rat { Karma = -1000, Str = 100 };
        var pet = new Horse { Controlled = true, ControlMaster = owner };
        var weapon = new HavenCompanionBlade();
        try
        {
            foreach (var mobile in new Mobile[] { owner, companion, enemy, secondary, pet })
            { mobile.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); mobile.Hits = mobile.HitsMax; }
            (companion.Weapon as BaseWeapon)?.Delete(); companion.AddItem(weapon);
            HavenGearExperience.GainEquipped(companion, 300);
            Assert.Equal(0, weapon.WeaponAttributes.HitEnergyArea);
            HavenGearExperience.GainEquipped(companion, 100);
            Assert.Equal(10, weapon.WeaponAttributes.HitEnergyArea);
            HavenGearExperience.GainEquipped(companion, 1500);
            Assert.Equal(40, weapon.WeaponAttributes.HitEnergyArea);
            Assert.Equal(40, weapon.Attributes.WeaponDamage);
            Assert.Equal(0, weapon.Attributes.SpellDamage);
            Assert.Equal(20, weapon.Attributes.WeaponSpeed);
            Assert.Equal(40, weapon.WeaponAttributes.HitLowerAttack);
            Assert.Equal(40, weapon.WeaponAttributes.HitLowerDefend);
            Assert.Equal(40, weapon.WeaponAttributes.HitLeechMana);
            Assert.Equal(40, weapon.WeaponAttributes.HitLeechHits);
            Assert.Equal(CheckSlayerResult.Slayer, weapon.CheckSlayers(companion, enemy));
            Assert.False(HavenCompanionWeaponEvolution.Slays(weapon, owner, enemy));
            Assert.False(HavenCompanionWeaponEvolution.Slays(weapon, companion, pet));
            var ownerHits = owner.Hits; var petHits = pet.Hits; var enemyHits = secondary.Hits;
            weapon.DoAreaAttack(companion, enemy, 0x1F1, 120, 0, 0, 0, 0, 100);
            Assert.True(secondary.Hits < enemyHits);
            Assert.Equal(ownerHits, owner.Hits); Assert.Equal(petHits, pet.Hits);
            companion.Role = HavenCompanionRole.Bard;
            Assert.True(companion.StartTamingAssist(owner, enemy));
            Assert.False(HavenCompanionWeaponEvolution.Slays(weapon, companion, enemy));
        }
        finally { companion.Delete(); owner.Delete(); enemy.Delete(); secondary.Delete(); pet.Delete(); weapon.Delete(); }
    }
    [SkippableFact]
    public void WardenRelocatesOutOfRuinsToReachableOpenGround()
    {
        TileDataRequirement.SkipIfMissing();
        var map = Map.Trammel;
        var p = HavenContentBootstrap.OldHavenBoss;
        var destination = new Point3D(p.X, p.Y, map.GetAverageZ(p.X, p.Y));
        var warden = new OldHavenWarden();
        var spawner = new OldHavenBossSpawner();
        var walker = new Mobile { Body = 0x190 };
        try
        {
            Assert.True(map.CanSpawnMobile(destination.X, destination.Y, destination.Z));
            foreach (var approach in new[] { new Point2D(p.X + 15, p.Y), new Point2D(p.X, p.Y + 15) })
            {
                walker.MoveToWorld(new Point3D(approach.X, approach.Y, map.GetAverageZ(approach.X, approach.Y)), map);
                Assert.True(new MovementPath(walker, destination).Success, $"No walking route from {approach} to {destination}");
            }
            warden.MoveToWorld(new Point3D(3670, 2587, 0), map);
            spawner.MoveToWorld(warden.Location, map);
            HavenContentBootstrap.RelocateHavenWarden();
            Assert.Equal(destination, warden.Location);
            Assert.Equal(destination, spawner.Location);
            Assert.Equal(destination, warden.Home);
            Assert.Equal(6, warden.RangeHome);
        }
        finally { warden.Delete(); spawner.Delete(); walker.Delete(); }
    }
    [SkippableFact]
    public void RepairAllIncludesEquippedAndNestedGearWithAtomicWalletPayment()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        var bench = new HavenRepairBench();
        var wallet = new AdventurersWallet { Balance = 99 };
        var weapon = new Longsword { MaxHitPoints = 40, HitPoints = 1 };
        var armor = new LeatherChest { MaxHitPoints = 40, HitPoints = 1 };
        var starter = new ApprenticeBlade { MaxHitPoints = 40, HitPoints = 1 };
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); bench.MoveToWorld(owner.Location, owner.Map);
            owner.Backpack.DropItem(wallet); owner.AddItem(weapon);
            var bag = new Bag(); owner.Backpack.DropItem(bag); bag.DropItem(armor); bag.DropItem(starter);
            Assert.False(bench.RepairAll(owner));
            Assert.Equal(99, wallet.Balance); Assert.Equal(1, weapon.HitPoints); Assert.Equal(1, armor.HitPoints);
            wallet.Balance = 100;
            Assert.True(bench.RepairAll(owner));
            Assert.Equal(0, wallet.Balance);
            Assert.Equal(40, weapon.HitPoints); Assert.Equal(40, armor.HitPoints); Assert.Equal(40, starter.HitPoints);
            Assert.Equal(40, weapon.MaxHitPoints);
            Assert.Null(owner.Target);
            Assert.False(bench.RepairAll(owner));
            weapon.MaxHitPoints = 1; weapon.HitPoints = 1; wallet.Balance = 1000;
            Assert.True(bench.RepairAll(owner, true));
            Assert.Equal(weapon.MaxHitPoints, weapon.HitPoints);
            Assert.True(weapon.MaxHitPoints > 1);
            Assert.Null(owner.Target);
        }
        finally { owner.Delete(); bench.Delete(); weapon.Delete(); armor.Delete(); starter.Delete(); }
    }
    [SkippableFact]
    public void CompanionGearGrowthSupportsEveryRoleAndStopsWhenUnequipped()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true };
        var companion = new HavenCompanion { BoundOwner = owner };
        var armor = new HavenStarterSash();
        try
        {
            companion.FindItemOnLayer(armor.Layer)?.Delete();
            companion.AddItem(armor);
            var weaponDamage = AosAttributes.GetValue(companion, AosAttribute.WeaponDamage);
            var spellDamage = AosAttributes.GetValue(companion, AosAttribute.SpellDamage);
            HavenGearExperience.Gain(armor, 1900);
            Assert.Equal(weaponDamage + 8, AosAttributes.GetValue(companion, AosAttribute.WeaponDamage));
            Assert.Equal(spellDamage + 8, AosAttributes.GetValue(companion, AosAttribute.SpellDamage));
            foreach (var role in Enum.GetValues<HavenCompanionRole>())
            {
                companion.Role = role; companion.ConfigureCombatRole();
                Assert.Equal(8, HavenCompanionGearGrowth.GetBonus(companion, AosAttribute.WeaponDamage));
                Assert.Equal(8, HavenCompanionGearGrowth.GetBonus(companion, AosAttribute.SpellDamage));
                foreach (var attribute in new[] { AosAttribute.RegenHits, AosAttribute.RegenStam, AosAttribute.RegenMana, AosAttribute.AttackChance, AosAttribute.DefendChance, AosAttribute.LowerManaCost })
                { Assert.Equal(2, HavenCompanionGearGrowth.GetBonus(companion, attribute)); }
            }
            companion.Backpack.DropItem(armor);
            Assert.Equal(0, HavenCompanionGearGrowth.GetBonus(companion, AosAttribute.WeaponDamage));
            companion.AddItem(armor);
            Assert.Equal(8, HavenCompanionGearGrowth.GetBonus(companion, AosAttribute.WeaponDamage));
            Assert.Equal(20, HavenGearExperience.Find(armor).Level);
            owner.AddItem(armor);
            Assert.Equal(0, HavenCompanionGearGrowth.GetBonus(owner, AosAttribute.WeaponDamage));
        }
        finally { companion.Delete(); owner.Delete(); armor.Delete(); }
    }
    [SkippableFact]
    public void GatheringExpeditionSurvivesReloadAndAwardsProgressOnlyOnce()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        var companion = new HavenCompanion { BoundOwner = owner };
        HavenCompanionExpedition restored = null;
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            companion.MoveToWorld(owner.Location, owner.Map); companion.SetControlMaster(owner);
            var mining = companion.Skills.Mining.Base;
            var strength = companion.RawStr; var dexterity = companion.RawDex; var intelligence = companion.RawInt;
            Assert.True(HavenCompanionExpedition.Start(companion, owner, HavenExpeditionKind.Ore));
            Assert.Equal(Map.Internal, companion.Map);
            Assert.False(HavenCompanionExpedition.Start(companion, owner, HavenExpeditionKind.Grind));
            owner.AutoStablePets();
            Assert.False(companion.IsStabled); Assert.Same(owner, companion.ControlMaster);
            var original = companion.Expedition;
            var writer = new BufferWriter(true); original.Serialize(writer);
            var data = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();
            original.Delete();
            restored = new HavenCompanionExpedition(World.NewItem);
            restored.Deserialize(new BufferReader(data));
            companion.Backpack.DropItem(restored);
            Assert.Same(companion, restored.Companion);
            Assert.Equal(HavenExpeditionKind.Ore, restored.Kind);
            Assert.True(restored.Return(owner, restored.Due));
            Assert.Equal(owner.Map, companion.Map); Assert.Equal(owner.Location, companion.Location);
            Assert.Null(companion.Expedition);
            Assert.True(companion.Skills.Mining.Base > mining);
            Assert.True(companion.RawStr >= strength + 5);
            Assert.True(companion.RawDex >= dexterity + 5);
            Assert.True(companion.RawInt >= intelligence + 5);
            Assert.Equal(100, companion.Backpack.FindItemByType<IronOre>().Amount);
            var trained = companion.TrainingMinutes;
            Assert.False(restored.Return(owner, restored.Due));
            Assert.Equal(trained, companion.TrainingMinutes);
            Assert.Equal(100, companion.Backpack.FindItemByType<IronOre>().Amount);
        }
        finally { restored?.Delete(); companion.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void ExpeditionEarlyReturnHasNoInstantLootAndAllRewardChoicesWork()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var other = new PlayerMobile { Player = true };
        var companion = new HavenCompanion { BoundOwner = owner };
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); companion.MoveToWorld(owner.Location, owner.Map);
            Assert.False(HavenCompanionExpedition.Start(companion, other, HavenExpeditionKind.Grind));
            Assert.True(HavenCompanionExpedition.Start(companion, owner, HavenExpeditionKind.Grind));
            var trip = companion.Expedition;
            Assert.False(trip.Return(other, trip.Due));
            Assert.True(trip.Return(owner, trip.Started));
            Assert.Null(companion.Backpack.FindItemByType<HavenMark>());
            foreach (var kind in Enum.GetValues<HavenExpeditionKind>())
            {
                var loot = HavenCompanionExpedition.CreateLoot(kind, 5, null, 0.99);
                try
                {
                    Assert.NotEmpty(loot.Items);
                    if (kind == HavenExpeditionKind.Grind)
                    {
                        Assert.Equal(5, loot.GetAmount(typeof(HavenMark)));
                        Assert.InRange(loot.GetAmount(typeof(Gold)), 1000, 1500);
                    }
                }
                finally { loot.Delete(); }
            }
            CheckBounds(new HavenCompanionGump(companion, 4), 370, 370);
        }
        finally { companion.Delete(); owner.Delete(); other.Delete(); }
    }
    [SkippableFact]
    public void OrdinaryGearEvolvesOnlyForCompanionsAndCasterUsesBook()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile();
        var companion = new HavenCompanion { BoundOwner = player };
        var armor = new LeatherChest();
        try
        {
            player.AddItem(armor);
            HavenGearExperience.Gain(armor, 1900);
            Assert.Null(HavenGearExperience.Find(armor));
            companion.AddItem(armor);
            HavenGearExperience.Gain(armor, 1900);
            Assert.Equal(20, HavenGearExperience.Find(armor).Level);
            companion.Backpack.DropItem(armor);
            Assert.True(HavenGearExperience.IsSpecial(armor));
            companion.Role = HavenCompanionRole.Caster;
            companion.ConfigureCombatRole();
            var book = Assert.IsType<ApprenticeGrimoire>(companion.FindItemOnLayer(Layer.OneHanded));
            book.Level = 20;
            HavenCompanionGearGrowth.ApplySpellbook(book);
            Assert.Equal(40, book.Attributes.SpellDamage);
            Assert.Equal(0, book.Attributes.WeaponDamage);
            companion.Role = HavenCompanionRole.Fighter;
            companion.ConfigureCombatRole();
            Assert.IsAssignableFrom<BaseWeapon>(companion.Weapon);
        }
        finally { armor.Delete(); companion.Delete(); player.Delete(); }
    }

    [Fact]
    public void TrainingSentinelIsStationaryHarmlessAndSurvivesLethalDamage()
    {
        var sentinel = new HavenTrainingSentinel();
        var player = new PlayerMobile();
        try
        {
            Assert.True(sentinel.CantWalk);
            Assert.True(sentinel.AlwaysAttackable);
            Assert.False(sentinel.CanBeHarmful(player, false, false));
            Assert.True(sentinel.HitsMax >= 30000);
            sentinel.Hits = 1;
            Assert.False(sentinel.OnBeforeDeath());
            Assert.Equal(sentinel.HitsMax, sentinel.Hits);
            Assert.False(sentinel.Tamable);
        }
        finally { sentinel.Delete(); player.Delete(); }
    }

    [SkippableFact]
    public void TamingMissionRequiresBothSkillsAndPetClaimPreservesFollowerLimits()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var other = new PlayerMobile();
        owner.AddItem(new Backpack());
        var companion = new HavenCompanion { BoundOwner = owner };
        BaseCreature claimedPet = null;
        Bag loot = null;
        try
        {
            owner.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
            companion.MoveToWorld(owner.Location, owner.Map);
            companion.Skills.AnimalTaming.Base = 100;
            companion.Skills.AnimalLore.Base = 20;
            Assert.False(HavenCompanionExpedition.Start(companion, owner, HavenExpeditionKind.TameDragon));
            Assert.Null(companion.Expedition);
            companion.Skills.AnimalLore.Base = 100;
            Assert.True(HavenCompanionExpedition.Start(companion, owner, HavenExpeditionKind.TameDragon));
            Assert.True(companion.Expedition.Return(owner, companion.Expedition.Due, 0.99));
            var voucher = companion.Backpack.FindItemByType<HavenExpeditionPetClaim>();
            Assert.NotNull(voucher);
            Assert.Equal(owner, voucher.Owner);
            Assert.Equal(HavenExpeditionKind.TameDragon, voucher.Kind);
            owner.Backpack.DropItem(voucher);
            Assert.False(voucher.Claim(other));
            owner.FollowersMax = 0;
            Assert.False(voucher.Claim(owner));
            Assert.False(voucher.Deleted);
            owner.FollowersMax = 5;
            Assert.True(voucher.Claim(owner));
            Assert.True(voucher.Deleted);
            Assert.False(voucher.Claim(owner));
            foreach (var pet in owner.Map.GetMobilesInRange<Dragon>(owner.Location, 0))
            { if (pet.ControlMaster == owner) { claimedPet = pet; break; } }
            Assert.NotNull(claimedPet);
            Assert.Equal(3, claimedPet.ControlSlots);
            loot = HavenCompanionExpedition.CreateLoot(HavenExpeditionKind.TameHorse, 4, owner);
            Assert.Null(loot.FindItemByType<HavenExpeditionPetClaim>());
            CheckBounds(new HavenCompanionGump(companion, 5), 370, 370);
        }
        finally { claimedPet?.Delete(); loot?.Delete(); companion.Delete(); owner.Delete(); other.Delete(); }
    }

    [SkippableFact]
    public void RarePetAbilitiesSupportOwnerAndRespectCooldownAndPetProtection()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var wolf = new HavenMoonfang();
        var enemy = new Dragon();
        var friendly = new Horse();
        try
        {
            owner.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
            wolf.MoveToWorld(owner.Location, owner.Map);
            enemy.MoveToWorld(owner.Location, owner.Map);
            friendly.MoveToWorld(owner.Location, owner.Map);
            wolf.SetControlMaster(owner);
            friendly.SetControlMaster(owner);
            owner.Hits = 1;
            var next = DateTime.MinValue;
            Assert.False(HavenRarePetAbility.Activate(wolf, friendly, 2, ref next));
            Assert.True(HavenRarePetAbility.Activate(wolf, enemy, 2, ref next));
            Assert.True(owner.Hits > 1);
            Assert.False(HavenRarePetAbility.Activate(wolf, enemy, 2, ref next));
            Assert.Equal(110, wolf.Skills.Wrestling.Cap);
        }
        finally { friendly.Delete(); enemy.Delete(); wolf.Delete(); owner.Delete(); }
    }

    [Fact]
    public void FullTamingMissionsAlwaysReturnSelectedPetWithPersistedRarity()
    {
        foreach (var (roll, tier) in new[] { (0.0, 0), (0.699, 0), (0.70, 1), (0.919, 1), (0.92, 2), (0.989, 2), (0.99, 3) })
        {
            var bag = HavenCompanionExpedition.CreateLoot(HavenExpeditionKind.TameHorse, 5, null, roll);
            HavenExpeditionPetClaim copy = null;
            try
            {
                var claim = bag.FindItemByType<HavenExpeditionPetClaim>();
                Assert.NotNull(claim);
                Assert.Equal(HavenExpeditionKind.TameHorse, claim.Kind);
                Assert.Equal(tier, claim.Rarity);
                var writer = new BufferWriter(true);
                claim.Serialize(writer);
                copy = new HavenExpeditionPetClaim(World.NewItem);
                copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
                Assert.Equal(tier, copy.Rarity);
                Assert.Equal(claim.Kind, copy.Kind);
            }
            finally { bag.Delete(); copy?.Delete(); }
        }
    }

    private static void CheckBounds(Gump gump, int width, int height)
    {
        foreach (var html in gump.Entries.OfType<GumpHtml>())
        {
            Assert.InRange(html.X + html.Width, 0, width);
            Assert.InRange(html.Y + html.Height, 0, height);
        }
        foreach (var button in gump.Entries.OfType<GumpButton>())
        {
            Assert.InRange(button.X, 0, width - 20);
            Assert.InRange(button.Y, 0, height - 20);
        }
    }
}
