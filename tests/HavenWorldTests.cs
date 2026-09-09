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
            if (item is StarterSupplyStone or HavenUpgradeStone or SpecialRewardStone or FreePetHitchingPost or UOOfflineDungeonPortal)
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

