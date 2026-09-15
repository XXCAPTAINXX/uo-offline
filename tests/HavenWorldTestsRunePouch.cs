using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsRunePouch
{
    public HavenWorldTestsRunePouch() => _ = new HavenWorldTests();

    private static PlayerMobile Owner()
    {
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
        return owner;
    }

    [SkippableFact]
    public void StoresOnlyBlankRunesAndPreservesBalancesAcrossSave()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Owner();
        var pouch = new HavenRunePouch();
        owner.Backpack.DropItem(pouch);
        try
        {
            var nested = new Bag(); owner.Backpack.DropItem(nested);
            for (var i = 0; i < 12; i++) { nested.DropItem(new RecallRune()); }
            var marked = new RecallRune(); marked.Mark(owner); nested.DropItem(marked);
            var named = new RecallRune { Description = "Keep me" }; nested.DropItem(named);
            var recall = new RecallScroll(10); nested.DropItem(recall);
            var gate = new GateTravelScroll(10); nested.DropItem(gate);
            Assert.Equal(12, pouch.StorePack(owner));
            Assert.Equal(12, pouch.Runes);
            Assert.Equal(6, owner.Backpack.TotalItems); // Pouch, bag, two protected runes, two scroll stacks.
            Assert.False(pouch.Store(owner, marked));
            Assert.False(pouch.Store(owner, recall));
            Assert.False(pouch.Store(owner, gate));
            Assert.False(marked.Deleted); Assert.False(named.Deleted);
            Assert.Equal(10, recall.Amount); Assert.Equal(10, gate.Amount);
            var writer = new BufferWriter(true); pouch.Serialize(writer);
            var copy = new HavenRunePouch(World.NewItem);
            try
            {
                copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
                Assert.Equal(12, copy.Runes);
            }
            finally { copy.Delete(); }
            Assert.Equal(10, pouch.Withdraw(owner, 10));
            Assert.Equal(2, pouch.Runes);
            Assert.Equal(0, pouch.Withdraw(owner, 10));
        }
        finally { owner.Delete(); }
    }

    [SkippableFact]
    public void MarkCreatesOneNormalRuneAtCurrentLocationAndConsumesOneBlank()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Owner();
        var pouch = new HavenRunePouch { Runes = 3 }; owner.Backpack.DropItem(pouch);
        try
        {
            var callbacks = 0;
            Assert.True(pouch.MarkRune(owner, () => { callbacks++; return true; }));
            Assert.Equal(1, callbacks); Assert.Equal(2, pouch.Runes);
            var rune = owner.Backpack.FindItemByType<RecallRune>();
            Assert.NotNull(rune); Assert.True(rune.Marked);
            Assert.Equal(owner.Location, rune.Target); Assert.Equal(owner.Map, rune.TargetMap);
            Assert.Equal(typeof(RecallRune), rune.GetType());
        }
        finally { owner.Delete(); }
    }

    [SkippableFact]
    public void FailedMarkFullBackpackAndOtherOwnersCannotConsumeRunes()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = Owner(); var stranger = Owner();
        var pouch = new HavenRunePouch { Runes = 3 }; owner.Backpack.DropItem(pouch);
        try
        {
            Assert.False(pouch.MarkRune(owner, () => false));
            Assert.Equal(3, pouch.Runes); Assert.Null(owner.Backpack.FindItemByType<RecallRune>());
            Assert.False(pouch.MarkRune(stranger, () => throw new Exception("Must not cast")));
            Assert.Equal(0, pouch.Withdraw(stranger, 1));
            owner.Backpack.MaxItems = owner.Backpack.TotalItems;
            Assert.False(pouch.MarkRune(owner, () => throw new Exception("Must check capacity first")));
            Assert.Equal(0, pouch.Withdraw(owner, 1));
            Assert.Equal(3, pouch.Runes); Assert.Null(owner.Backpack.FindItemByType<RecallRune>());
            owner.Backpack.MaxItems = 125;
            Assert.True(pouch.MarkRune(owner, () => true));
            Assert.Equal(2, pouch.Runes);
        }
        finally { owner.Delete(); stranger.Delete(); }
    }
}
