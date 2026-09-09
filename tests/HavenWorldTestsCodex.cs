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
public class HavenWorldTestsCodex
{
    public HavenWorldTestsCodex() => _ = new HavenWorldTests();
    [SkippableFact]
    public void LedgerWithdrawsOriginalScrollOnceAndRejectsOtherOwners()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile(); var other = new PlayerMobile();
        owner.AddItem(new Backpack()); other.AddItem(new Backpack());
        var codex = new ProgressionArchive(); var scroll = new PowerScroll(SkillName.Alchemy, 105);
        owner.Backpack.DropItem(codex); codex.DropItem(scroll);
        try
        {
            var gump = new ChampionCodexGump(codex, ownedOnly: true);
            Assert.False(gump.Withdraw(other, 100));
            Assert.Same(codex, scroll.Parent);
            Assert.True(gump.Withdraw(owner, 100));
            Assert.Same(owner.Backpack, scroll.Parent);
            Assert.False(gump.Withdraw(owner, 100));
            Assert.Equal(105, scroll.Value);
            Assert.Equal(SkillName.Alchemy, scroll.Skill);
            Assert.Equal("Champion's Codex", codex.DefaultName);
        }
        finally { owner.Delete(); other.Delete(); }
    }
    [SkippableFact]
    public void LedgerClassifiesScrollsAndAllPagesFitTheWindow()
    {
        TileDataRequirement.SkipIfMissing();
        var codex = new ProgressionArchive();
        try
        {
            var scroll = new PowerScroll(SkillName.Magery, 120); codex.DropItem(scroll);
            Assert.Equal(3, ChampionCodexGump.Column(scroll));
            var marks = new HavenMark(5); codex.DropItem(marks);
            Assert.Equal(-1, ChampionCodexGump.Column(marks));
            foreach (var other in new[] { false, true })
            foreach (var page in new[] { -1, 0, 1, 2, 999 })
            {
                var gump = new ChampionCodexGump(codex, page, other);
                foreach (var button in gump.Entries.OfType<GumpButton>())
                { Assert.InRange(button.X, 0, 730); Assert.InRange(button.Y, 0, 560); }
                foreach (var label in gump.Entries.OfType<GumpLabel>())
                { Assert.InRange(label.X, 0, 740); Assert.InRange(label.Y, 0, 560); }
            }
        }
        finally { codex.Delete(); }
    }
}
