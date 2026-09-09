using System;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsMyStats
{
    public HavenWorldTestsMyStats() => _ = new HavenWorldTests();
    [SkippableFact]
    public void StatsShowEffectiveBonusesAndHavenLuckWithoutTrainingSkills()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { RawStr = 100, RawDex = 80, RawInt = 90 };
        var ring = new GoldRing();
        var manaHandler = Mobile.ManaRegenRateHandler; var stamHandler = Mobile.StamRegenRateHandler;
        try
        {
            player.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
            ring.Attributes.SpellDamage = 25; player.AddItem(ring);
            player.AddStatMod(new StatMod(StatType.Str, "stats-test", 10, TimeSpan.FromMinutes(1)));
            var total = player.Skills.Total;
            Mobile.ManaRegenRateHandler = _ => throw new InvalidOperationException("Stats display must not invoke skill-gaining regeneration handlers.");
            Mobile.StamRegenRateHandler = _ => throw new InvalidOperationException("Stats display must not invoke skill-gaining regeneration handlers.");
            var overview = new HavenMyStatsGump(player);
            var labels = overview.Entries.OfType<GumpLabel>().Select(e => e.Text).ToArray();
            Assert.Contains("+10", labels); Assert.Contains("110", labels);
            Assert.Contains(labels, s => s.Contains("Haven island: +1,000"));
            var combat = new HavenMyStatsGump(player, 1);
            Assert.Contains(combat.Entries.OfType<GumpLabel>(), e => e.Text == "25%");
            Assert.Equal(total, player.Skills.Total); Assert.Equal(100, player.RawStr);
            foreach (var gump in new[] { overview, combat })
            {
                foreach (var label in gump.Entries.OfType<GumpLabel>()) { Assert.InRange(label.X, 0, 535); Assert.InRange(label.Y, 0, 465); }
            }
        }
        finally { Mobile.ManaRegenRateHandler = manaHandler; Mobile.StamRegenRateHandler = stamHandler; ring.Delete(); player.Delete(); }
    }
}
