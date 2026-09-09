using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsIdleMissions
{
    public HavenWorldTestsIdleMissions() => _ = new HavenWorldTests();
    [SkippableFact]
    public void IdleCyclesReturnOnMovementAndNeverTakeOverManualTrips()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var companion = new HavenCompanion { BoundOwner = owner };
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            companion.MoveToWorld(owner.Location, owner.Map);
            owner.Hits = owner.HitsMax; companion.Hits = companion.HitsMax;
            var record = HavenCompanionIdleMissions.Ensure(companion);
            var now = Core.Now;
            record.Tick(now, true);
            record.Tick(now + TimeSpan.FromMinutes(4), true); Assert.Null(companion.Expedition);
            record.Tick(now + TimeSpan.FromMinutes(5), true);
            Assert.NotNull(record.ActiveTrip); Assert.Equal(HavenExpeditionKind.Grind, record.ActiveTrip.Kind);
            var trip = record.ActiveTrip;
            Assert.True(trip.Return(owner, trip.Due, automatic: true));
            companion.Hits = companion.HitsMax; // Recover after mission stat growth before leaving again.
            record.Tick(now + TimeSpan.FromMinutes(10), true);
            Assert.Equal(HavenExpeditionKind.Ore, record.ActiveTrip.Kind);
            owner.LastMoveTime++;
            record.Tick(now + TimeSpan.FromMinutes(10.1), true);
            Assert.Null(companion.Expedition); Assert.Equal(owner.Map, companion.Map);
            Assert.Null(record.ActiveTrip);
            Assert.True(HavenCompanionExpedition.Start(companion, owner, HavenExpeditionKind.Wood));
            var manual = companion.Expedition;
            owner.LastMoveTime++;
            record.Tick(now + TimeSpan.FromMinutes(11), true);
            Assert.Same(manual, companion.Expedition);
            manual.Return(owner, Core.Now);
            record.Enabled = false; record.Tick(now + TimeSpan.FromMinutes(20), true);
            Assert.Null(companion.Expedition);
        }
        finally { companion.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void HighestPetNeedsBothSkillsAndGatheringIsDeeded()
    {
        TileDataRequirement.SkipIfMissing();
        var companion = new HavenCompanion();
        try
        {
            companion.Skills.AnimalTaming.Base = 120; companion.Skills.AnimalLore.Base = 70;
            Assert.Equal(HavenExpeditionKind.TameFrostmane, HavenCompanionIdleMissions.BestTaming(companion));
            companion.Skills.AnimalLore.Base = 110;
            Assert.Equal(HavenExpeditionKind.TameStormscale, HavenCompanionIdleMissions.BestTaming(companion));
            foreach (var kind in new[] { HavenExpeditionKind.Ore, HavenExpeditionKind.Wood, HavenExpeditionKind.Leather, HavenExpeditionKind.Reagents })
            {
                var bag = HavenCompanionExpedition.CreateLoot(kind, 5);
                try
                {
                    Assert.Equal(kind == HavenExpeditionKind.Reagents ? 8 : 1, bag.Items.Count);
                    foreach (var item in bag.Items)
                    {
                        var deed = Assert.IsType<CommodityDeed>(item);
                        Assert.NotNull(deed.Commodity); Assert.Equal(Map.Internal, deed.Commodity.Map);
                        Assert.Equal(1.0, deed.Weight);
                    }
                }
                finally { bag.Delete(); }
            }
        }
        finally { companion.Delete(); }
    }

    [SkippableFact]
    public void AfkStartsImmediatelyButCombatDisconnectAndMovementCancelIt()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var companion = new HavenCompanion { BoundOwner = owner };
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); companion.MoveToWorld(owner.Location, owner.Map);
            owner.Hits = owner.HitsMax; companion.Hits = companion.HitsMax;
            var record = HavenCompanionIdleMissions.Ensure(companion);
            record.Tick(Core.Now, true); record.Afk = true; record.Tick(Core.Now, true);
            Assert.NotNull(record.ActiveTrip);
            owner.Warmode = true; record.Tick(Core.Now, true);
            Assert.False(record.Afk); Assert.Null(companion.Expedition);
            owner.Warmode = false; record.Afk = true; record.Tick(Core.Now, false);
            Assert.False(record.Afk); Assert.Null(companion.Expedition);
        }
        finally { companion.Delete(); owner.Delete(); }
    }
}
