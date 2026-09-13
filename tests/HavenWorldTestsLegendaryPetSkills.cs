using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsLegendaryPetSkills
{
    public HavenWorldTestsLegendaryPetSkills() => _ = new HavenWorldTests();
    private static void Mark(BaseCreature pet, int tier)
    { if (pet.Backpack == null) { pet.AddItem(new Backpack()); } pet.Backpack.DropItem(new HavenPetRarity { Tier = tier }); }
    [SkippableFact]
    public void LegendaryRollRaisesActiveSkillsAndCapsOnceAndPersistsItsSelection()
    {
        TileDataRequirement.SkipIfMissing();
        var pet = new HavenMoonfang(); Mark(pet, 3);
        try
        {
            var record = HavenLegendaryPetSkills.Roll(pet, 0, 3, 145);
            Assert.Equal(3, record.Boosted.Count);
            foreach (var name in record.Boosted)
            { Assert.Equal(145, pet.Skills[name].Base); Assert.True(pet.Skills[name].Cap >= 145); }
            Assert.Equal(0, pet.Skills.Magery.Base);
            Assert.Same(record, HavenLegendaryPetSkills.Roll(pet, 0, 3, 150));
            foreach (var name in record.Boosted) { Assert.Equal(145, pet.Skills[name].Base); }
            var writer = new BufferWriter(true); record.Serialize(writer);
            var copy = new HavenLegendaryPetSkills(World.NewItem);
            try
            {
                copy.Deserialize(new BufferReader(writer.Buffer.AsSpan(0, (int)writer.Position).ToArray()));
                Assert.Equal(record.Boosted, copy.Boosted);
            }
            finally { copy.Delete(); }
        }
        finally { pet.Delete(); }
    }
    [SkippableFact]
    public void FailedRollCannotRetryAndOrdinaryOrEpicPetsNeverRoll()
    {
        TileDataRequirement.SkipIfMissing();
        var legendary = new HavenMoonfang(); var epic = new HavenMoonfang(); var ordinary = new Horse();
        Mark(legendary, 3); Mark(epic, 2); Mark(ordinary, 3);
        try
        {
            var record = HavenLegendaryPetSkills.Roll(legendary, 0.50);
            Assert.Empty(record.Boosted);
            Assert.Same(record, HavenLegendaryPetSkills.Roll(legendary, 0)); Assert.Empty(record.Boosted);
            Assert.Null(HavenLegendaryPetSkills.Roll(epic, 0)); Assert.Null(HavenLegendaryPetSkills.Roll(ordinary, 0));
        }
        finally { legendary.Delete(); epic.Delete(); ordinary.Delete(); }
    }
    [SkippableFact]
    public void TicketPreviewAndClaimKeepTheSameOverCapSkills()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true }; owner.AddItem(new Backpack());
        var pet = new HavenMoonfang(); Mark(pet, 3);
        var record = HavenLegendaryPetSkills.Roll(pet, 0, 3, 150);
        var ticket = new HavenExpeditionPetClaim { Owner = owner, Kind = HavenExpeditionKind.TameMoonfang, Rarity = 3, ReservedPet = pet };
        owner.Backpack.DropItem(ticket);
        try
        {
            owner.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel);
            Assert.Same(pet, ticket.Inspect(owner)); Assert.Same(pet, ticket.Inspect(owner));
            Assert.True(ticket.Claim(owner)); Assert.True(ticket.Deleted); Assert.Equal(1, pet.ControlSlots);
            foreach (var name in record.Boosted) { Assert.Equal(150, pet.Skills[name].Base); Assert.True(pet.Skills[name].Cap >= 150); }
        }
        finally { ticket.Delete(); pet.Delete(); owner.Delete(); }
    }
}
