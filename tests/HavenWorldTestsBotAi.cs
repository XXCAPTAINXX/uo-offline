using Server;
using Server.CustomBots;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsBotAi
{
    public HavenWorldTestsBotAi() => _ = new HavenWorldTests();
    private sealed class GroupPolicy : PlayerGroupBehavior
    {
        public bool PullsNewEnemies => WantsFreshFights;
    }
    [Fact]
    public void GroupedBotsDoNotPullFreshEncounters() => Assert.False(new GroupPolicy().PullsNewEnemies);

    [SkippableFact]
    public void BotRegistryTracksConstructionAndDeletionWithoutDuplicateEntries()
    {
        TileDataRequirement.SkipIfMissing();
        var before = BehaviorTickManager.RegisteredCount;
        var bot = new PlayerBot();
        try
        {
            Assert.Equal(before + 1, BehaviorTickManager.RegisteredCount);
            BehaviorTickManager.Register(bot);
            Assert.Equal(before + 1, BehaviorTickManager.RegisteredCount);
        }
        finally { bot.Delete(); }
        Assert.Equal(before, BehaviorTickManager.RegisteredCount);
    }
}
