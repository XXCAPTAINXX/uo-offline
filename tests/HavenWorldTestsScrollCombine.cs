using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsScrollCombine
{
    public HavenWorldTestsScrollCombine() => _ = new HavenWorldTests();
    [SkippableFact]
    public void CombiningPreservesSkillAndRejectsShortagesOtherOwnersAndMaximumTier()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile(); var other = new PlayerMobile();
        owner.AddItem(new Backpack()); other.AddItem(new Backpack());
        var codex = new ProgressionArchive(); owner.Backpack.DropItem(codex);
        try
        {
            foreach (var tier in new[] {105, 110, 115})
            {
                var cost = ChampionScrollCombineGump.Cost(tier);
                for (var i = 0; i < cost - 1; i++) { codex.DropItem(new PowerScroll(SkillName.Magery, tier)); }
                var unrelated = new PowerScroll(SkillName.Tactics, tier); codex.DropItem(unrelated);
                Assert.False(ChampionScrollCombineGump.Combine(owner, codex, SkillName.Magery, tier));
                codex.DropItem(new PowerScroll(SkillName.Magery, tier));
                Assert.False(ChampionScrollCombineGump.Combine(other, codex, SkillName.Magery, tier));
                Assert.True(ChampionScrollCombineGump.Combine(owner, codex, SkillName.Magery, tier));
                Assert.False(ChampionScrollCombineGump.Combine(owner, codex, SkillName.Magery, tier));
                Assert.False(unrelated.Deleted);
                Assert.Single(codex.Items.OfType<PowerScroll>(), s => s.Skill == SkillName.Magery && s.Value == tier + 5);
                foreach (var item in codex.Items.ToArray()) { item.Delete(); }
            }
            codex.DropItem(new PowerScroll(SkillName.Magery, 120));
            Assert.False(ChampionScrollCombineGump.Combine(owner, codex, SkillName.Magery, 120));
            Assert.Single(codex.Items);
        }
        finally { owner.Delete(); other.Delete(); }
    }
}
