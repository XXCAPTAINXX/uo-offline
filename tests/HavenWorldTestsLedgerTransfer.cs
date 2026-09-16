using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsLedgerTransfer
{
    public HavenWorldTestsLedgerTransfer() { _ = new HavenWorldTests(); }

    [SkippableFact]
    public void TransferMergesEveryMaterialAndBothBooksPersistWithoutDuplicateResources()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile(); owner.AddItem(new Backpack());
        var companion = new HavenCompanion { BoundOwner = owner };
        var source = new HavenResourceLedger(); companion.Backpack.DropItem(source);
        var nested = new Bag(); owner.Backpack.DropItem(nested);
        var destination = new HavenResourceLedger(); nested.DropItem(destination);
        var sourceCopy = new HavenResourceLedger(World.NewItem);
        var destinationCopy = new HavenResourceLedger(World.NewItem);
        owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); companion.MoveToWorld(owner.Location, owner.Map);
        try
        {
            for (var index = 0; index < HavenResourceCatalog.Entries.Length; index++)
            {
                source.Balances.Add(100_000L + index); destination.Balances.Add(20 + index);
            }
            var expected = source.Balances.Zip(destination.Balances, (a, b) => a + b).ToArray();
            owner.Backpack.MaxItems = owner.Backpack.TotalItems; // No room for intermediate deeds is required.
            Assert.True(source.TransferAll(owner, destination, out var types));
            Assert.Equal(expected.Length, types); Assert.Equal(expected, destination.Balances);
            Assert.All(source.Balances, value => Assert.Equal(0, value));
            Assert.False(source.Deleted); Assert.False(destination.Deleted);
            Assert.True(source.TransferAll(owner, destination, out types)); Assert.Equal(0, types);
            Assert.Equal(expected, destination.Balances);
            var writer = new BufferWriter(true); source.Serialize(writer);
            sourceCopy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            writer = new BufferWriter(true); destination.Serialize(writer);
            destinationCopy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.Equal(source.Balances, sourceCopy.Balances); Assert.Equal(expected, destinationCopy.Balances);
        }
        finally { sourceCopy.Delete(); destinationCopy.Delete(); companion.Delete(); owner.Delete(); }
    }

    [Fact]
    public void OverflowInLastMaterialRejectsWholeTransferBeforeChangingEitherBook()
    {
        var owner = new PlayerMobile(); owner.AddItem(new Backpack());
        var source = new HavenResourceLedger(); var destination = new HavenResourceLedger();
        owner.Backpack.DropItem(source); owner.Backpack.DropItem(destination);
        try
        {
            source.Balances.AddRange(new long[] { 10, 20, 30 });
            destination.Balances.AddRange(new long[] { 5, 6, long.MaxValue - 29 });
            var beforeSource = source.Balances.ToArray(); var beforeDestination = destination.Balances.ToArray();
            Assert.False(source.TransferAll(owner, destination, out var types)); Assert.Equal(0, types);
            Assert.Equal(beforeSource, source.Balances); Assert.Equal(beforeDestination, destination.Balances);
            destination.Balances[2] = long.MaxValue - 30;
            Assert.True(source.TransferAll(owner, destination, out types)); Assert.Equal(3, types);
            Assert.Equal(long.MaxValue, destination.Balance(2)); Assert.Equal(0, source.Balance(2));
        }
        finally { owner.Delete(); }
    }

    [SkippableFact]
    public void TransferRechecksOwnershipLocationAndDestinationPossession()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile(); owner.AddItem(new Backpack());
        var stranger = new PlayerMobile(); stranger.AddItem(new Backpack());
        var companion = new HavenCompanion { BoundOwner = owner };
        var source = new HavenResourceLedger(); companion.Backpack.DropItem(source); source.Balances.Add(50);
        var destination = new HavenResourceLedger(); owner.Backpack.DropItem(destination);
        owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); companion.MoveToWorld(owner.Location, owner.Map);
        stranger.MoveToWorld(owner.Location, owner.Map);
        try
        {
            Assert.False(source.TransferAll(owner, source, out _)); Assert.False(source.TransferAll(owner, null, out _));
            Assert.False(source.TransferAll(stranger, destination, out _));
            stranger.Backpack.DropItem(destination); Assert.False(source.TransferAll(owner, destination, out _));
            companion.Backpack.DropItem(destination); Assert.False(source.TransferAll(owner, destination, out _));
            owner.Backpack.DropItem(destination);
            companion.Internalize(); Assert.False(source.TransferAll(owner, destination, out _));
            companion.MoveToWorld(new Point3D(owner.X + 30, owner.Y, owner.Z), owner.Map);
            Assert.False(source.TransferAll(owner, destination, out _));
            companion.MoveToWorld(owner.Location, owner.Map); companion.BoundOwner = stranger;
            Assert.False(source.TransferAll(owner, destination, out _));
            companion.BoundOwner = owner;
            Assert.Equal(50, source.Balance(0)); Assert.Equal(0, destination.Balance(0));
            destination.Delete(); Assert.False(source.TransferAll(owner, destination, out _)); Assert.Equal(50, source.Balance(0));
        }
        finally { companion.Delete(); owner.Delete(); stranger.Delete(); }
    }

    [Fact]
    public void TransferExtendsAnEmptyDestinationAndRejectsDeletedSource()
    {
        var owner = new PlayerMobile(); owner.AddItem(new Backpack());
        var source = new HavenResourceLedger(); var destination = new HavenResourceLedger();
        owner.Backpack.DropItem(source); owner.Backpack.DropItem(destination);
        try
        {
            source.Balances.AddRange(new long[] { 0, 0, 70 });
            Assert.True(source.TransferAll(owner, destination, out var types)); Assert.Equal(1, types);
            Assert.Equal(new long[] { 0, 0, 70 }, destination.Balances);
            source.Delete(); Assert.False(source.TransferAll(owner, destination, out _)); Assert.Equal(70, destination.Balance(2));
        }
        finally { owner.Delete(); }
    }
}
