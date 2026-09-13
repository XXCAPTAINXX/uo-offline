using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsReleasePetAI
{
    public HavenWorldTestsReleasePetAI() { _ = new HavenWorldTests(); }

    [SkippableFact]
    public void StormscaleMageryPurchaseUsesCastingAIWithoutLosingItsSignature()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100 };
        owner.AddItem(new Backpack());
        owner.MoveToWorld(new Point3D(4196, 2868, 0), Map.Trammel);
        var pet = new HavenStormscale();
        try
        {
            pet.SetControlMaster(owner);
            pet.MoveToWorld(owner.Location, owner.Map);
            Assert.IsType<HavenStormscaleAI>(pet.AIObject);
            var training = HavenPetTraining.Get(pet);
            Assert.True(training.Begin(owner, pet));
            training.Progress = 10000;
            var points = training.PointsTenths;

            Assert.True(HavenPetAbilities.Learn(owner, pet, 0));
            Assert.Equal(points - HavenPetAbilities.Cost(0), training.PointsTenths);
            Assert.True(HavenPetAbilities.Knows(pet, 0));
            Assert.Equal(AIType.AI_Mage, pet.AI);
            Assert.IsType<MageAI>(pet.AIObject);
            Assert.Equal(5, HavenPetSignatures.Kind(pet));
            Assert.False(HavenPetAbilities.Learn(owner, pet, 0));
        }
        finally { pet.Delete(); owner.Delete(); }
    }

    [SkippableFact]
    public void SavedStormscaleMageryAndDefaultRangedAIRebuildCorrectly()
    {
        TileDataRequirement.SkipIfMissing();
        var original = new HavenStormscale { AI = AIType.AI_Mage };
        var restored = new HavenStormscale(World.NewMobile);
        try
        {
            var writer = new BufferWriter(true);
            original.Serialize(writer);
            var data = writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();
            var reader = new BufferReader(data);
            restored.Deserialize(reader);

            Assert.Equal(data.Length, reader.Position);
            Assert.Equal(AIType.AI_Mage, restored.AI);
            Assert.IsType<MageAI>(restored.AIObject);
            restored.AI = AIType.AI_Melee;
            Assert.IsType<HavenStormscaleAI>(restored.AIObject);
        }
        finally { restored.Delete(); original.Delete(); }
    }
}
