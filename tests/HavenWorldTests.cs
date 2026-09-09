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
                if (item is StarterSupplyStone or HavenUpgradeStone or SpecialRewardStone or FreePetHitchingPost or UOOfflineDungeonPortal or HavenRepairBench || item.Name == "Haven welcome garden") { items.Add(item); }
            }
            Assert.Same(oldPortal, Assert.Single(items.OfType<UOOfflineDungeonPortal>()));
            Assert.False(oldPortal.InRange(HavenRecovery.BankLocation, 6));
            Assert.Single(items.OfType<StarterSupplyStone>());
            Assert.Single(items.OfType<HavenUpgradeStone>());
            Assert.Single(items.OfType<SpecialRewardStone>());
            Assert.Single(items.OfType<FreePetHitchingPost>());
            Assert.Equal(3, items.Count(i => i.Name == "Haven welcome garden"));
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
                    var before = Services(dto.Map, dto.Location);
                    foreach (var item in before) { created.Add(item); }
                    Assert.Contains(before, i => i is StarterSupplyStone);
                    Assert.Contains(before, i => i is HavenUpgradeStone);
                    Assert.Contains(before, i => i is SpecialRewardStone);
                    Assert.Contains(before, i => i is FreePetHitchingPost);
                    Assert.Contains(before, i => i is UOOfflineDungeonPortal);
                    Assert.Contains(before, i => i is HavenRepairBench);
                    HavenContentBootstrap.EnsureBankServices(dto.Map, dto.Location);
                    Assert.Equal(before.Count, Services(dto.Map, dto.Location).Count);
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
            if (item is StarterSupplyStone or HavenUpgradeStone or SpecialRewardStone or FreePetHitchingPost or UOOfflineDungeonPortal or HavenRepairBench)
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
            Assert.Equal(29, created.Count);
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
            Assert.Equal(4, first.Entries.OfType<GumpButton>().Count(b => b.ButtonID is > 0 and < 10000));
        }
        finally { stone.Delete(); }
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

