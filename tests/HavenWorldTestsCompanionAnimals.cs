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
public class HavenWorldTestsCompanionAnimals
{
    public HavenWorldTestsCompanionAnimals() { _=new HavenWorldTests(); }
    private static PlayerMobile Owner()
    {
        var owner=new PlayerMobile { Body=400,Player=true,RawStr=100 };owner.AddItem(new Backpack());
        owner.MoveToWorld(HavenRecovery.BankLocation,Map.Trammel);return owner;
    }
    private static HavenCompanion Companion(Mobile owner)
    {
        var companion=new HavenCompanion { BoundOwner=owner,Role=HavenCompanionRole.Bard };
        companion.MoveToWorld(owner.Location,owner.Map);companion.Skills.AnimalTaming.Base=120;companion.Skills.AnimalLore.Base=120;return companion;
    }
    [SkippableFact]
    public void TamedClaimPreservesActualAnimalAndCannotBeDuplicatedOrStolen()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var stranger=Owner();var companion=Companion(owner);
        var pet=new Horse { RawDex=173,RawStr=287 };pet.Skills.Healing.Base=97.4;pet.MoveToWorld(owner.Location,owner.Map);
        try
        {
            Assert.False(companion.StartTamingAssist(stranger,pet));Assert.True(companion.StartTamingAssist(owner,pet));
            Assert.True(companion.ContinuingAssistedTame(pet));Assert.False(companion.FinishAssistedTame(pet));
            pet.SetControlMaster(companion);Assert.True(companion.FinishAssistedTame(pet));
            Assert.False(companion.FinishAssistedTame(pet));Assert.False(companion.TamingAssistActive);
            var claim=Assert.Single(companion.Backpack.Items.OfType<HavenTamedPetClaim>());
            Assert.Same(pet,claim.Pet);Assert.Equal(Map.Internal,pet.Map);Assert.Null(pet.ControlMaster);
            stranger.Backpack.DropItem(claim);claim.OnDoubleClick(stranger);Assert.Same(pet,claim.Pet);
            owner.Backpack.DropItem(claim);claim.OnDoubleClick(owner);
            Assert.True(claim.Deleted);Assert.Same(owner,pet.ControlMaster);Assert.Equal(owner.Map,pet.Map);
            Assert.Equal(173,pet.RawDex);Assert.Equal(287,pet.RawStr);Assert.Equal(97.4,pet.Skills.Healing.Base);
            Assert.True(pet.BondingBegin<Core.Now-pet.BondingDelay);
        }
        finally { pet.Delete();companion.Delete();stranger.Delete();owner.Delete(); }
    }
    [SkippableFact]
    public void AssignedMountCanDismountParkResumeAndBeReclaimedOnlyByOwner()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var stranger=Owner();var companion=Companion(owner);
        var pet=new Horse();pet.SetControlMaster(owner);pet.MoveToWorld(owner.Location,owner.Map);
        try
        {
            Assert.False(companion.AcceptAssignedPet(stranger,pet));
            pet.ControlTarget=companion;pet.ControlOrder=OrderType.Transfer;Assert.True(pet.AIObject.DoOrderTransfer());
            var record=Assert.Single(companion.Backpack.Items.OfType<HavenCompanionAssignedPet>());
            Assert.Same(companion,pet.ControlMaster);Assert.Same(companion,pet.Rider);
            record.AutoMount=false;record.Tick();Assert.Null(pet.Rider);Assert.Equal(OrderType.Follow,pet.ControlOrder);
            companion.ParkAssignedPets();Assert.True(record.Parked);Assert.Equal(Map.Internal,pet.Map);
            companion.ThinkAssignedPets();Assert.False(record.Parked);Assert.Equal(companion.Map,pet.Map);
            Assert.False(record.ClaimBack(stranger));Assert.True(record.ClaimBack(owner));Assert.True(record.Deleted);
            Assert.Same(pet,Assert.Single(companion.Backpack.Items.OfType<HavenTamedPetClaim>()).Pet);
            Assert.Equal(0,companion.Followers);
        }
        finally { pet.Delete();companion.Delete();stranger.Delete();owner.Delete(); }
    }
    [SkippableFact]
    public void SkillAndCapacityChecksPreventInvalidAssignmentsAndCancelledTames()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var companion=Companion(owner);var pet=new Horse { MinTameSkill=110 };
        pet.MoveToWorld(owner.Location,owner.Map);
        try
        {
            companion.Skills.AnimalLore.Base=50;Assert.False(companion.StartTamingAssist(owner,pet));
            companion.Skills.AnimalLore.Base=120;Assert.True(companion.StartTamingAssist(owner,pet));
            companion.StopTamingAssist();Assert.False(companion.ContinuingAssistedTame(pet));
            pet.SetControlMaster(owner);companion.FollowersMax=0;Assert.False(companion.AcceptAssignedPet(owner,pet));
            Assert.Same(owner,pet.ControlMaster);companion.FollowersMax=5;Assert.True(companion.AcceptAssignedPet(owner,pet));
            var record=Assert.Single(companion.Backpack.Items.OfType<HavenCompanionAssignedPet>());record.Delete();
            Assert.Same(owner,pet.ControlMaster);Assert.Null(pet.Rider);Assert.Equal(owner.Map,pet.Map);
        }
        finally { pet.Delete();companion.Delete();owner.Delete(); }
    }
    [SkippableFact]
    public void FullPackRetainsTamedPetAndAssignmentSurvivesSerialization()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var companion=Companion(owner);var pet=new Horse();
        var copy=new HavenCompanionAssignedPet(World.NewItem);
        pet.MoveToWorld(owner.Location,owner.Map);
        try
        {
            Assert.True(companion.StartTamingAssist(owner,pet));pet.SetControlMaster(companion);
            companion.Backpack.MaxItems=1;Assert.True(companion.FinishAssistedTame(pet));
            var record=Assert.Single(companion.Backpack.Items.OfType<HavenCompanionAssignedPet>());
            Assert.Same(companion,pet.ControlMaster);Assert.NotEqual(Map.Internal,pet.Map);
            Assert.Empty(companion.Backpack.Items.OfType<HavenTamedPetClaim>());
            record.Park();var writer=new BufferWriter(true);record.Serialize(writer);
            copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0,(int)writer.Position).ToArray()));
            Assert.Same(owner,copy.Owner);Assert.Same(companion,copy.Companion);Assert.Same(pet,copy.Pet);
            Assert.True(copy.Parked);Assert.False(copy.AutoMount);
            copy.Pet=null;copy.Delete();companion.Backpack.MaxItems=125;
            Assert.True(record.ClaimBack(owner));Assert.Same(pet,Assert.Single(companion.Backpack.Items.OfType<HavenTamedPetClaim>()).Pet);
        }
        finally { copy.Pet=null;copy.Delete();pet.Delete();companion.Delete();owner.Delete(); }
    }

}
