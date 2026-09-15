using Server;
using Server.Gumps;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsGuildAndFrontierMenus
{
    public HavenWorldTestsGuildAndFrontierMenus() { _=new HavenWorldTests(); }
    [Fact]
    public void GuildFilterAllowsSpacesAndDottedAbbreviationsButStillChecksProfanity()
    {
        Assert.True(BaseGuildGump.CheckProfanity("Rare Export Company"));
        Assert.True(BaseGuildGump.CheckProfanity("REC"));
        Assert.True(BaseGuildGump.CheckProfanity("R.E."));
        Assert.False(BaseGuildGump.CheckProfanity("fuck"));
        Assert.False(BaseGuildGump.CheckProfanity(""));
    }
    [Fact]
    public void FrontierPreviewUsesActualStartingStatsWithoutChargingOrGivingItems()
    {
        var owner=new PlayerMobile { Body=400 };owner.AddItem(new Backpack());
        try
        {
            var record=HavenFrontierRecord.Get(owner);record.MinaxCredits=100;
            for(var choice=0;choice<3;choice++)
            {
                _=new HavenFrontierRewardPreview(owner,choice,false);
                Assert.Empty(owner.Backpack.Items);Assert.Equal(100,record.MinaxCredits);
            }
            Assert.True(record.Buy(owner,0,false));Assert.Equal(50,record.MinaxCredits);
            var book=Assert.IsType<Spellbook>(Assert.Single(owner.Backpack.Items));
            Assert.Equal(25,book.Attributes.SpellDamage);Assert.Equal(ulong.MaxValue,book.Content);
            Assert.True(HavenLegendaryArtifact.IsLegendary(book));
        }
        finally { owner.Delete(); }
    }
}
