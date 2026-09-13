using System;
using Server;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsCompanionSkillRate
{
    public HavenWorldTestsCompanionSkillRate() => _ = new HavenWorldTests();
    [SkippableFact]
    public void SkillsGainTwentyFivePercentMoreWithoutExtraTrainingTimeOrLevels()
    {
        TileDataRequirement.SkipIfMissing();
        var companion = new HavenCompanion();
        try
        {
            var now = Core.Now;
            companion.TrainingMinutes = 0; companion.LastTraining = now - TimeSpan.FromMinutes(100);
            companion.UpdateTraining(now);
            Assert.Equal(100, companion.TrainingMinutes, 5);
            Assert.Equal(81.25, companion.Mastery, 5);
            Assert.Equal(2, companion.TrainingLevel);
            Assert.InRange(companion.Skills.Healing.Base, 81.2, 81.3);
            companion.UpdateTraining(now);
            Assert.Equal(81.25, companion.Mastery, 5);
        }
        finally { companion.Delete(); }
    }
}
