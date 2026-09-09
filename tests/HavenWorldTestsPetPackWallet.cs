using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsPetPackWallet
{
    public HavenWorldTestsPetPackWallet() => _ = new HavenWorldTests();
    [SkippableFact]
    public void WalletCollectsNestedShardsOnceAndPreservesThemOnOverflow()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile(); owner.AddItem(new Backpack());
        var wallet = new AdventurersWallet(); owner.Backpack.DropItem(wallet);
        var bag = new Bag(); owner.Backpack.DropItem(bag); var shards = new AstralShard(5); bag.DropItem(shards);
        try
        {
            Assert.Equal(5, wallet.DepositBackpackShards(owner)); Assert.True(shards.Deleted); Assert.Equal(5, wallet.AstralShards);
            Assert.Equal(0, wallet.DepositBackpackShards(owner));
            var retained = new AstralShard(5); bag.DropItem(retained); wallet.AstralShards = long.MaxValue;
            Assert.Equal(0, wallet.DepositBackpackShards(owner)); Assert.False(retained.Deleted);
        }
        finally { owner.Delete(); }
    }
    [SkippableFact]
    public void CustomPetPackAllowsOwnerItemsButProtectsRecordsAndOtherOwners()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile(); var other = new PlayerMobile(); var pet = new HavenFrostmane();
        owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); other.MoveToWorld(owner.Location, owner.Map);
        pet.MoveToWorld(owner.Location, owner.Map); pet.SetControlMaster(owner);
        var record = HavenPetTraining.Get(pet); var bag = new Bag(); pet.Backpack.DropItem(bag); var gold = new Gold(10); bag.DropItem(gold);
        try
        {
            Assert.True(HavenCustomPetPack.CanOpen(owner, pet)); Assert.False(HavenCustomPetPack.CanOpen(other, pet));
            Assert.True(pet.CheckNonlocalLift(owner, gold)); Assert.True(pet.CheckNonlocalDrop(owner, gold, bag));
            Assert.False(pet.CheckNonlocalLift(other, gold)); Assert.False(pet.CheckNonlocalDrop(other, gold, bag));
            Assert.False(pet.CheckNonlocalLift(owner, record));
        }
        finally { pet.Delete(); owner.Delete(); other.Delete(); }
    }
}
