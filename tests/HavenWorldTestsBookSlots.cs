using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsBookSlots
{
    public HavenWorldTestsBookSlots() { _ = new HavenWorldTests(); }

    [SkippableFact]
    public void HundredsOfArchivedScrollsUseOnlyTheBookSlotIncludingAfterTotalsRebuild()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile(); owner.AddItem(new Backpack());
        var bag = new Bag(); owner.Backpack.DropItem(bag);
        var codex = new ProgressionArchive(); bag.DropItem(codex);
        owner.Backpack.MaxItems = 2; bag.MaxItems = 1;
        try
        {
            for (var i = 0; i < 200; i++) { Assert.True(codex.TryDropItem(owner, new PowerScroll(SkillName.Alchemy, 105), false)); }
            Assert.Equal(200, codex.Items.Count); Assert.Equal(0, codex.TotalItems);
            Assert.Equal(1, bag.TotalItems); Assert.Equal(2, owner.Backpack.TotalItems);
            owner.UpdateTotals(); // World.Load rebuilds totals through this same virtual path for existing books.
            Assert.Equal(2, owner.Backpack.TotalItems); Assert.Equal(1, bag.TotalItems);
            var gump = new ChampionCodexGump(codex, ownedOnly: true);
            Assert.False(gump.Withdraw(owner, 100)); Assert.Equal(200, codex.Items.Count);
            owner.Backpack.MaxItems = 3;
            Assert.True(gump.Withdraw(owner, 100)); Assert.Equal(3, owner.Backpack.TotalItems);
            Assert.Equal(199, codex.Items.Count);
            var scroll = owner.Backpack.Items.OfType<PowerScroll>().Single();
            Assert.True(codex.TryDropItem(owner, scroll, false)); Assert.Equal(2, owner.Backpack.TotalItems);
            var extra = new Bag(); Assert.False(codex.TryDropItem(owner, extra, false)); extra.Delete();
            foreach (var item in codex.Items.ToArray()) { item.Delete(); }
            Assert.Equal(2, owner.Backpack.TotalItems);
            codex.Delete(); Assert.Equal(1, owner.Backpack.TotalItems);
        }
        finally { owner.Delete(); }
    }

    [SkippableFact]
    public void CombiningAndSplittingInsideFullBackpackDoNotConsumeSlots()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile(); owner.AddItem(new Backpack());
        var codex = new ProgressionArchive(); owner.Backpack.DropItem(codex); owner.Backpack.MaxItems = 1;
        try
        {
            codex.DropItem(new PowerScroll(SkillName.Magery, 110));
            Assert.True(ChampionScrollCombineGump.Split(owner, codex, SkillName.Magery, 110));
            Assert.Equal(8, codex.Items.Count); Assert.Equal(1, owner.Backpack.TotalItems);
            Assert.True(ChampionScrollCombineGump.Combine(owner, codex, SkillName.Magery, 105));
            Assert.Single(codex.Items); Assert.Equal(1, owner.Backpack.TotalItems);
        }
        finally { owner.Delete(); }
    }

    [SkippableFact]
    public void FilledResourceLedgerUsesOneSlotAndWithdrawalRequiresOneFreeSlot()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile(); owner.AddItem(new Backpack());
        var ledger = new HavenResourceLedger(); owner.Backpack.DropItem(ledger);
        try
        {
            var deed = new CommodityDeed(); Assert.True(deed.SetCommodity(new IronIngot(60000)));
            var index = HavenResourceCatalog.Index(deed.Commodity); owner.Backpack.DropItem(deed);
            Assert.Equal(2, owner.Backpack.TotalItems); Assert.True(ledger.Absorb(owner, deed));
            Assert.Equal(1, owner.Backpack.TotalItems); Assert.Empty(ledger.Items);
            owner.UpdateTotals(); Assert.Equal(1, owner.Backpack.TotalItems);
            owner.Backpack.MaxItems = 1; Assert.False(ledger.Withdraw(owner, index, 10));
            Assert.Equal(60000, ledger.Balance(index));
            owner.Backpack.MaxItems = 2; Assert.True(ledger.Withdraw(owner, index, 10));
            Assert.Equal(2, owner.Backpack.TotalItems); Assert.Equal(59990, ledger.Balance(index));
        }
        finally { owner.Delete(); }
    }
}
