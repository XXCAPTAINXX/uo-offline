using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsStorageMenus
{
    public HavenWorldTestsStorageMenus() { _=new HavenWorldTestsMarket(); }
    [Fact]
    public void SatchelCollectsNestedMatchingItemsAndLeavesBagsAndForeignItems()
    {
        var owner=new PlayerMobile { Body=400 };owner.AddItem(new Backpack());
        var stranger=new PlayerMobile { Body=400 };stranger.AddItem(new Backpack());
        try
        {
            var satchel=new HavenResourceSatchel();owner.Backpack.DropItem(satchel);
            var outer=new Bag();var inner=new Bag();owner.Backpack.DropItem(outer);outer.DropItem(inner);
            var iron=new IronIngot(250);var gem=new Ruby(20);var sword=new Longsword();
            inner.DropItem(iron);inner.DropItem(gem);inner.DropItem(sword);
            var foreign=new Bag();var stolen=new IronIngot(900);foreign.DropItem(stolen);stranger.Backpack.DropItem(foreign);
            Assert.Equal(0,HavenStorageAccess.Collect(owner,satchel,foreign));Assert.Same(foreign,stolen.Parent);
            Assert.Equal(2,HavenStorageAccess.Collect(owner,satchel,outer));
            Assert.Same(satchel,iron.Parent);Assert.Same(satchel,gem.Parent);Assert.Same(inner,sword.Parent);
            Assert.Same(outer,inner.Parent);Assert.Same(owner.Backpack,outer.Parent);
            Assert.Equal(0,HavenStorageAccess.Collect(owner,satchel,owner.Backpack));
            Assert.Equal(40,HavenStorageAccess.Withdraw(owner,satchel,new[]{iron},40));
            Assert.Equal(210,satchel.FindItemByType<IronIngot>().Amount);
            Assert.Equal(40,owner.Backpack.Items.OfType<IronIngot>().Single().Amount);
        }
        finally { owner.Delete();stranger.Delete(); }
    }
    [Fact]
    public void FailedPartialWithdrawalRestoresQuantityAndRejectsStaleRows()
    {
        var owner=new PlayerMobile { Body=400 };owner.AddItem(new Backpack());
        try
        {
            var satchel=new HavenResourceSatchel();owner.Backpack.DropItem(satchel);
            var iron=new IronIngot(250);satchel.DropItem(iron);owner.Backpack.MaxItems=1;
            Assert.Equal(0,HavenStorageAccess.Withdraw(owner,satchel,new[]{iron},40));
            Assert.Equal(250,Assert.Single(satchel.Items).Amount);
            owner.Backpack.MaxItems=125;owner.Backpack.DropItem(iron);
            Assert.Equal(0,HavenStorageAccess.Withdraw(owner,satchel,new[]{iron},40));
            Assert.Equal(250,iron.Amount);
        }
        finally { owner.Delete(); }
    }
}
