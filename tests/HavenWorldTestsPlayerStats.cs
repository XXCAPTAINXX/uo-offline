using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsPlayerStats
{
    public HavenWorldTestsPlayerStats() => _ = new HavenWorldTests();
    [SkippableFact]
    public void NewAndExistingPlayersKeepScrollAndVeteranBonusesWithoutRepeatedIncreases()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile();
        try
        {
            Assert.Equal(300, player.StatCap);
            foreach (var oldCap in new[] { 225, 230, 250, 255 })
            {
                player.StatCap = oldCap;
                var total = player.RawStr + player.RawDex + player.RawInt;
                HavenPlayerStats.Apply(player);
                Assert.Equal(oldCap + 75, player.StatCap);
                Assert.Equal(total, player.RawStr + player.RawDex + player.RawInt);
                HavenPlayerStats.Apply(player);
                Assert.Equal(oldCap + 75, player.StatCap);
            }
            player.StatCap = 400;
            HavenPlayerStats.Apply(player);
            Assert.Equal(400, player.StatCap);
        }
        finally { player.Delete(); }
    }
    [SkippableFact]
    public void NativeScrollAddsItsTierAboveThreeHundredAndRejectsRepeatedOrLowerTiers()
    {
        TileDataRequirement.SkipIfMissing();
        var player = new PlayerMobile { Player = true, Body = 0x190 };
        player.AddItem(new Backpack());
        player.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
        var scroll = new StatCapScroll(250);
        var lower = new StatCapScroll(230);
        var repeated = new StatCapScroll(250);
        try
        {
            player.HasStatReward = true;
            player.StatCap += 5;
            player.Backpack.DropItem(scroll);
            player.Backpack.DropItem(lower);
            player.Backpack.DropItem(repeated);
            Assert.True(scroll.CanUse(player));
            scroll.Use(player);
            Assert.True(scroll.Deleted);
            Assert.Equal(330, player.StatCap);
            Assert.False(lower.CanUse(player));
            Assert.False(repeated.CanUse(player));
            repeated.Use(player);
            Assert.False(repeated.Deleted);
            Assert.Equal(330, player.StatCap);
        }
        finally { scroll.Delete(); lower.Delete(); repeated.Delete(); player.Delete(); }
    }
}
