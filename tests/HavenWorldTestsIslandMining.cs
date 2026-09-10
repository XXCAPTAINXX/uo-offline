using System.Linq;
using Server;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsIslandMining
{
    [SkippableFact]
    public void IslandOutcropMinesRealOreAcrossFourIndependentEightTileBanks()
    {
        TileDataRequirement.SkipIfMissing();
        _ = new HavenWorldTestsMarket();
        var player = new PlayerMobile { Body = 400, RawStr = 100 };
        player.AddItem(new Backpack());
        player.Skills.Mining.Base = 120;
        var tool = new Pickaxe();
        player.Backpack.DropItem(tool);
        var estate = new HavenPirateEstate();
        estate.MoveToWorld(HavenPirateEstate.Site, Map.Trammel);
        var mining = Mining.System;
        var definition = mining.OreAndStone;
        var hadBanks = definition.Banks.TryGetValue(Map.Trammel, out var previousBanks);
        var previousSkillCheck = Mobile.SkillCheckLocationHandler;
        Mobile.SkillCheckLocationHandler = Server.Misc.SkillCheck.Mobile_SkillCheckLocation;
        definition.Banks.Remove(Map.Trammel);
        try
        {
            estate.Build(player);
            var rocks = estate.Fixtures.Where(i => i.Name == "Island ore outcrop").ToArray();
            Assert.Equal(20, rocks.Length);
            Assert.Equal(8, definition.BankWidth);
            Assert.Equal(8, definition.BankHeight);
            var groups = rocks.GroupBy(i => definition.GetBank(i.Map, i.X, i.Y)).ToArray();
            Assert.Equal(4, groups.Length);
            foreach (var group in groups)
            {
                var rock = group.First();
                Assert.True(mining.GetHarvestDetails(player, tool, rock, out var id, out var map, out var location, out var land));
                Assert.False(land);
                Assert.Same(definition, mining.GetDefinition(id, land));
                Assert.Equal(rock.Location, location);
                Assert.Same(estate.Map, map);
                var reachable = false;
                for (var dx = -2; dx <= 2 && !reachable; dx++)
                {
                    for (var dy = -2; dy <= 2 && !reachable; dy++)
                    {
                        var point = new Point3D(rock.X + dx, rock.Y + dy, rock.Z);
                        if (!map.CanSpawnMobile(point)) { continue; }
                        player.MoveToWorld(point, map);
                        reachable = player.InLOS(rock) && mining.CheckRange(player, tool, definition, map, location, false);
                    }
                }
                Assert.True(reachable, $"Unreachable ore bank at {rock.Location}");
                var before = group.Key.Current;
                for (var attempt = 0; attempt < 50 && group.Key.Current == before; attempt++)
                {
                    mining.FinishHarvesting(player, tool, definition, rock, tool);
                }
                Assert.True(group.Key.Current < before, $"Native mining did not extract ore at {rock.Location}");
                Assert.NotNull(player.Backpack.FindItemByType<BaseOre>());
            }
            var exhausted = groups[0];
            var untouched = groups[1].Key.Current;
            exhausted.Key.Consume(exhausted.Key.Current, player);
            Assert.All(exhausted, rock => Assert.False(mining.CheckResources(player, tool, definition, rock.Map, rock.Location, false)));
            Assert.Equal(untouched, groups[1].Key.Current);
            Assert.True(mining.CheckResources(player, tool, definition, groups[1].First().Map, groups[1].First().Location, false));
        }
        finally
        {
            estate.Delete();
            player.Delete();
            Mobile.SkillCheckLocationHandler = previousSkillCheck;
            definition.Banks.Remove(Map.Trammel);
            if (hadBanks) { definition.Banks[Map.Trammel] = previousBanks; }
        }
    }
}
