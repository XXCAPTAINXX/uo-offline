using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsMissionDeadline
{
    public HavenWorldTestsMissionDeadline() => _ = new HavenWorldTests();
    [SkippableFact]
    public void EarlyAutomaticReturnWaitsThenDeliversExactlyOnePet()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        owner.AddItem(new Backpack());
        var companion = new HavenCompanion { BoundOwner = owner };
        try
        {
            owner.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
            companion.MoveToWorld(owner.Location, owner.Map);
            companion.Skills.AnimalTaming.Base = 100;
            companion.Skills.AnimalLore.Base = 100;
            Assert.True(HavenCompanionExpedition.Start(companion, owner, HavenExpeditionKind.TameDragon));
            var trip = companion.Expedition;
            var training = companion.TrainingMinutes;
            foreach (var early in new[] { 1000, 8, 1 })
            {
                Assert.False(trip.Return(owner, trip.Due.AddMilliseconds(-early), automatic: true));
                Assert.False(trip.Claimed);
                Assert.Same(trip, companion.Expedition);
                Assert.Equal(Map.Internal, companion.Map);
                Assert.Equal(training, companion.TrainingMinutes);
                Assert.Null(companion.Backpack.FindItemByType<Bag>());
            }
            Assert.True(trip.Return(owner, trip.Due, automatic: true));
            var ticket = companion.Backpack.FindItemByType<HavenExpeditionPetClaim>();
            Assert.NotNull(ticket);
            Assert.Equal(HavenExpeditionKind.TameDragon, ticket.Kind);
            Assert.Same(owner, ticket.Owner);
            Assert.False(trip.Return(owner, trip.Due.AddSeconds(30), automatic: true));
        }
        finally { companion.Delete(); owner.Delete(); }
    }
}
