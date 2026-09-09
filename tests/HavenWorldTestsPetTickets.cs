using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsPetTickets
{
    public HavenWorldTestsPetTickets() => _ = new HavenWorldTests();
    [SkippableFact]
    public void InspectionReservesExactPetAndFailedClaimDoesNotReroll()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var stranger = new PlayerMobile();
        var ticket = new HavenExpeditionPetClaim { Owner = owner, Kind = HavenExpeditionKind.TameFrostmane, Rarity = 3 };
        BaseCreature pet = null;
        try
        {
            owner.AddItem(new Backpack()); owner.Backpack.DropItem(ticket);
            owner.MoveToWorld(new Point3D(3511, 2575, 14), Map.Trammel);
            Assert.Null(ticket.Inspect(stranger));
            pet = ticket.Inspect(owner);
            Assert.NotNull(pet); Assert.Equal(Map.Internal, pet.Map);
            var strength = pet.RawStr; var skill = pet.Skills.Wrestling.Base;
            owner.FollowersMax = 0;
            Assert.False(ticket.Claim(owner)); Assert.False(pet.Deleted);
            Assert.Same(pet, ticket.Inspect(owner));
            owner.FollowersMax = 5;
            Assert.True(ticket.Claim(owner)); Assert.True(ticket.Deleted);
            Assert.False(pet.Deleted); Assert.Same(owner, pet.ControlMaster);
            Assert.Equal(strength, pet.RawStr); Assert.Equal(skill, pet.Skills.Wrestling.Base);
            Assert.Equal(owner.Map, pet.Map);
            BaseCreature.Configure();
            owner.Skills.AnimalTaming.Cap = 120;
            owner.Skills.AnimalTaming.Base = 120;
            Assert.False(pet.IsBonded);
            Assert.True(pet.BondingBegin + pet.BondingDelay <= Core.Now);
            var food = new Apple();
            Assert.True(pet.OnDragDrop(owner, food));
            Assert.True(pet.IsBonded);
        }
        finally { ticket.Delete(); pet?.Delete(); owner.Delete(); stranger.Delete(); }
    }
    [SkippableFact]
    public void ReservedPetReferenceSurvivesSerializationAndTicketDeletionCleansUp()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile();
        var ticket = new HavenExpeditionPetClaim { Owner = owner, Kind = HavenExpeditionKind.TameDragon };
        HavenExpeditionPetClaim copy = null;
        try
        {
            owner.AddItem(new Backpack()); owner.Backpack.DropItem(ticket);
            var pet = ticket.Inspect(owner); Assert.NotNull(pet);
            var writer = new BufferWriter(true); ticket.Serialize(writer);
            copy = new HavenExpeditionPetClaim(World.NewItem);
            copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
            Assert.Same(pet, copy.ReservedPet);
            copy.ReservedPet = null;
            ticket.Delete(); Assert.True(pet.Deleted);
        }
        finally { copy?.Delete(); ticket.Delete(); owner.Delete(); }
    }
}
