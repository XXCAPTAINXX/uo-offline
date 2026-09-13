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
public class HavenWorldTestsMissionDuration
{
    public HavenWorldTestsMissionDuration() { _=new HavenWorldTests(); }
    private static (PlayerMobile,HavenCompanion) Pair()
    {
        var owner=new PlayerMobile { Player=true,Body=0x190,RawStr=100,RawDex=100,RawInt=100 };owner.AddItem(new Backpack());
        owner.MoveToWorld(HavenRecovery.BankLocation,Map.Trammel); var companion=new HavenCompanion { BoundOwner=owner };
        companion.MoveToWorld(owner.Location,owner.Map); return (owner,companion);
    }
    [SkippableFact]
    public void DurationIsFrozenAtDepartureAndLongRewardsAreCappedAndClaimedOnce()
    {
        TileDataRequirement.SkipIfMissing();var (owner,companion)=Pair();
        try
        {
            Assert.False(HavenCompanionExpedition.Start(companion,owner,HavenExpeditionKind.Ore,999));
            Assert.True(HavenCompanionExpedition.Start(companion,owner,HavenExpeditionKind.Ore,60));
            var trip=companion.Expedition;Assert.Equal(TimeSpan.FromHours(1),trip.Due-trip.Started);
            HavenMissionDuration.Select(companion,15);Assert.Equal(60,trip.DurationMinutes);
            Assert.False(trip.Return(owner,trip.Started+TimeSpan.FromMinutes(5),automatic:true));Assert.False(trip.Claimed);
            Assert.True(trip.Return(owner,trip.Due+TimeSpan.FromDays(2),automatic:true));
            var amount=HavenWorldTestsResources.TotalDeededAmount(companion.Backpack);
            Assert.Equal(1500,amount);Assert.False(trip.Return(owner,Core.Now));Assert.Equal(15,HavenMissionDuration.Selected(companion));
        }
        finally { companion.Delete();owner.Delete(); }
    }
    [SkippableFact]
    public void EarlyReturnHasNoCompletionBonusOrPetAndOrdinaryPetsNeverGetRarity()
    {
        TileDataRequirement.SkipIfMissing();var (owner,companion)=Pair();
        try
        {
            Assert.True(HavenCompanionExpedition.Start(companion,owner,HavenExpeditionKind.Ore,60));
            var trip=companion.Expedition;Assert.True(trip.Return(owner,trip.Started+TimeSpan.FromMinutes(10)));
            Assert.Equal(200,HavenWorldTestsResources.TotalDeededAmount(companion.Backpack));
            var early=HavenCompanionExpedition.CreateLoot(HavenExpeditionKind.TameHorse,59,owner,1,companion,100,false);
            Assert.Empty(early.Items);early.Delete();
            var ordinary=HavenCompanionExpedition.CreateLoot(HavenExpeditionKind.TameHorse,60,owner,1,companion,125,true);
            Assert.Equal(0,Assert.Single(ordinary.Items.ToArray().OfType<HavenExpeditionPetClaim>()).Rarity);ordinary.Delete();
            var rare=HavenCompanionExpedition.CreateLoot(HavenExpeditionKind.TameEmberwing,60,owner,.99,companion,125,true);
            Assert.Equal(3,Assert.Single(rare.Items.ToArray().OfType<HavenExpeditionPetClaim>()).Rarity);rare.Delete();
            Assert.Equal(15,HavenMissionDuration.SearchRolls(60,125));
        }
        finally { companion.Delete();owner.Delete(); }
    }
    [SkippableFact]
    public void SavedContractsAndPreferencesRoundTripAndLegacyTripsStayFiveMinutes()
    {
        TileDataRequirement.SkipIfMissing();var plan=new HavenMissionPlan { Minutes=30 };var contract=new HavenMissionContract { Minutes=60 };
        var planCopy=new HavenMissionPlan(World.NewItem);var contractCopy=new HavenMissionContract(World.NewItem);var legacy=new HavenCompanionExpedition { Started=Core.Now };
        try
        {
            var writer=new BufferWriter(true);plan.Serialize(writer);planCopy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0,(int)writer.Position).ToArray()));
            Assert.Equal(30,planCopy.Minutes);
            writer=new BufferWriter(true);contract.Serialize(writer);contractCopy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0,(int)writer.Position).ToArray()));
            Assert.Equal(60,contractCopy.Minutes);Assert.Equal(5,legacy.DurationMinutes);
            legacy.AddItem(contractCopy);Assert.Equal(TimeSpan.FromHours(1),legacy.Due-legacy.Started);
        }
        finally { plan.Delete();contract.Delete();planCopy.Delete();contractCopy.Delete();legacy.Delete(); }
    }
    [SkippableFact]
    public void ExplicitAfkUsesSelectedDurationWithoutActivityCancellingTheTrip()
    {
        TileDataRequirement.SkipIfMissing();var (owner,companion)=Pair();
        try
        {
            HavenMissionDuration.Select(companion,30);var afk=HavenCompanionIdleMissions.Ensure(companion);afk.Afk=true;
            owner.Hits=owner.HitsMax;companion.Hits=companion.HitsMax;afk.Tick(Core.Now,true);
            Assert.NotNull(afk.ActiveTrip);Assert.Equal(30,afk.ActiveTrip.DurationMinutes);
            owner.LastMoveTime++;afk.Tick(Core.Now+TimeSpan.FromMinutes(6),true);
            Assert.True(afk.Afk);Assert.NotNull(afk.ActiveTrip);Assert.False(afk.ActiveTrip.Claimed);
        }
        finally { companion.Delete();owner.Delete(); }
    }
}
