using Server.Items;
using Server.Mobiles;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsFieldGuide
{
    [Fact]
    public void GuideReplacementDoesNotDuplicateAnExistingPackOrBankBook()
    {
        var player = new PlayerMobile { Player = true };
        player.AddItem(new Backpack());
        try
        {
            Assert.True(HavenFieldGuide.ClaimBook(player));
            var book = player.Backpack.FindItemByType<HavenFieldGuideBook>();
            Assert.NotNull(book);
            Assert.False(HavenFieldGuide.ClaimBook(player));
            player.BankBox.DropItem(book);
            Assert.False(HavenFieldGuide.ClaimBook(player));
            book.Delete();
            Assert.True(HavenFieldGuide.ClaimBook(player));
        }
        finally
        {
            player.Delete();
        }
    }

    [Fact]
    public void FullBackpackCannotReceiveAFreeGuideAndCanRetryAfterMakingRoom()
    {
        var player = new PlayerMobile { Player = true };
        var pack = new Backpack { MaxItems = 1 };
        player.AddItem(pack);
        var filler = new Gold(1);
        pack.DropItem(filler);
        try
        {
            Assert.False(HavenFieldGuide.ClaimBook(player));
            Assert.Null(pack.FindItemByType<HavenFieldGuideBook>());
            filler.Delete();
            Assert.True(HavenFieldGuide.ClaimBook(player));
        }
        finally
        {
            player.Delete();
        }
    }
}
