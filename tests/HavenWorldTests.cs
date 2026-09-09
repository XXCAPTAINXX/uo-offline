using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server;
using Server.CustomBots;
using Server.Engines.Spawners;
using Server.Gumps;
using Server.Json;
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
            var last = new HavenListGump(stone, menu, 5);
            CheckBounds(first, 540, 460);
            CheckBounds(last, 540, 460);
            Assert.Contains(first.Entries.OfType<GumpButton>(), b => b.ButtonID == 10002);
            Assert.DoesNotContain(first.Entries.OfType<GumpButton>(), b => b.ButtonID == 10001);
            Assert.Contains(last.Entries.OfType<GumpButton>(), b => b.ButtonID == 10001);
            Assert.DoesNotContain(last.Entries.OfType<GumpButton>(), b => b.ButtonID == 10002);
            Assert.Equal(6, first.Entries.OfType<GumpButton>().Count(b => b.ButtonID is > 0 and < 10000));
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
