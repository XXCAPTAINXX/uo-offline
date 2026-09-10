using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsMarkEquipment
{
    public HavenWorldTestsMarkEquipment() => _ = new HavenWorldTests();

    [Fact]
    public void MarkRewardsUseNativeSlotsAndApplyTheirRoleSkillsWhenWorn()
    {
        var boots = new HavenBulwarkBoots();
        var sash = new HavenTideweaverSash();
        var doublet = new HavenBeastkeepersDoublet();
        var apron = new HavenProspectorsApron();
        try
        {
            AssertWearableProfile(boots, Layer.Shoes, 0x170B, (SkillName.Parry, 10), (SkillName.Healing, 5));
            Assert.Equal(5, boots.Attributes.DefendChance);
            Assert.Equal(5, boots.Attributes.BonusStr);
            Assert.Equal(2, boots.Attributes.RegenHits);

            AssertWearableProfile(sash, Layer.MiddleTorso, 0x1541, (SkillName.Spellweaving, 10), (SkillName.Focus, 10));
            Assert.Equal(5, sash.Attributes.LowerManaCost);
            Assert.Equal(5, sash.Attributes.SpellDamage);
            Assert.Equal(2, sash.Attributes.RegenMana);

            AssertWearableProfile(doublet, Layer.MiddleTorso, 0x1F7B,
                (SkillName.AnimalTaming, 10), (SkillName.AnimalLore, 10), (SkillName.Veterinary, 10));
            Assert.Equal(2, doublet.Attributes.RegenHits);
            Assert.Equal(5, doublet.Attributes.DefendChance);

            AssertWearableProfile(apron, Layer.Waist, 0x153B,
                (SkillName.Mining, 10), (SkillName.Lumberjacking, 10), (SkillName.Fishing, 10));
            Assert.Equal(150, apron.Attributes.Luck);
            Assert.Equal(2, apron.Attributes.RegenStam);
            Assert.Equal(5, apron.Attributes.BonusStr);
        }
        finally
        {
            boots.Delete();
            sash.Delete();
            doublet.Delete();
            apron.Delete();
        }
    }

    [Theory]
    [InlineData(typeof(HavenBulwarkBoots))]
    [InlineData(typeof(HavenTideweaverSash))]
    [InlineData(typeof(HavenBeastkeepersDoublet))]
    [InlineData(typeof(HavenProspectorsApron))]
    public void SharedExperienceLevelsWornMarkGearToTwentyAndExcludesOrdinaryAndPackedGear(Type rewardType)
    {
        var player = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100, RawDex = 100, RawInt = 100 };
        player.AddItem(new Backpack());
        var reward = (BaseClothing)Activator.CreateInstance(rewardType);
        var packedReward = (BaseClothing)Activator.CreateInstance(rewardType);
        var ordinary = new Shirt();
        ordinary.Attributes.Luck = 37;
        ordinary.Attributes.BonusStr = 3;
        var startingLuck = reward.Attributes.Luck;
        var startingStrength = reward.Attributes.BonusStr;
        try
        {
            Assert.True(player.EquipItem(reward));
            Assert.True(player.EquipItem(ordinary));
            player.Backpack.DropItem(packedReward);
            Assert.True(HavenGearExperience.IsSpecial(reward));
            Assert.False(HavenGearExperience.IsSpecial(ordinary));

            HavenGearExperience.GainEquipped(player, 400);
            var progress = HavenGearExperience.Find(reward);
            Assert.NotNull(progress);
            Assert.Equal(5, progress.Level);
            Assert.Equal(startingLuck + 20, reward.Attributes.Luck);
            Assert.Equal(startingStrength + 1, reward.Attributes.BonusStr);
            Assert.Null(HavenGearExperience.Find(packedReward));
            Assert.Null(HavenGearExperience.Find(ordinary));
            Assert.Equal(37, ordinary.Attributes.Luck);
            Assert.Equal(3, ordinary.Attributes.BonusStr);

            HavenGearExperience.GainEquipped(player, 10000);
            Assert.Equal(20, progress.Level);
            Assert.Equal(1900, progress.Experience);
            Assert.Equal(startingLuck + 95, reward.Attributes.Luck);
            Assert.Equal(startingStrength + 4, reward.Attributes.BonusStr);
            Assert.Equal(4, reward.Attributes.BonusDex);
            Assert.Equal(4, reward.Attributes.BonusInt);

            HavenGearExperience.GainEquipped(player, 10000);
            Assert.Equal(1900, progress.Experience);
            Assert.Equal(startingLuck + 95, reward.Attributes.Luck);
            Assert.Equal(startingStrength + 4, reward.Attributes.BonusStr);
            Assert.Null(HavenGearExperience.Find(ordinary));
            Assert.Null(HavenGearExperience.Find(packedReward));
        }
        finally
        {
            reward.Delete();
            packedReward.Delete();
            ordinary.Delete();
            player.Delete();
        }
    }

    [Theory]
    [InlineData(typeof(HavenBulwarkBoots))]
    [InlineData(typeof(HavenTideweaverSash))]
    [InlineData(typeof(HavenBeastkeepersDoublet))]
    [InlineData(typeof(HavenProspectorsApron))]
    public void NativeSerializationPreservesRewardAndProgressWithoutReapplyingGrowth(Type rewardType)
    {
        var original = (BaseClothing)Activator.CreateInstance(rewardType);
        BaseClothing restored = null;
        HavenGearExperience originalProgress = null;
        HavenGearExperience restoredProgress = null;
        try
        {
            HavenGearExperience.Gain(original, 400);
            originalProgress = HavenGearExperience.Find(original);
            // Save each entity independently, then reconnect the restored progression child.
            original.RemoveItem(originalProgress);
            var itemData = Serialize(original);
            var progressData = Serialize(originalProgress);

            restored = (BaseClothing)Activator.CreateInstance(rewardType, World.NewItem);
            restored.Deserialize(new BufferReader(itemData));
            restoredProgress = new HavenGearExperience(World.NewItem);
            restoredProgress.Deserialize(new BufferReader(progressData));
            restored.AddItem(restoredProgress);

            Assert.Equal(original.Name, restored.Name);
            Assert.Equal(original.ItemID, restored.ItemID);
            Assert.Equal(original.Hue, restored.Hue);
            Assert.Equal(original.Layer, restored.Layer);
            Assert.Equal(LootType.Blessed, restored.LootType);
            Assert.True(HavenGearExperience.IsSpecial(restored));
            Assert.Same(restoredProgress, HavenGearExperience.Find(restored));
            Assert.Equal(5, restoredProgress.Level);
            Assert.Equal(5, restoredProgress.AppliedLevel);
            Assert.Equal(400, restoredProgress.Experience);

            HavenGearExperience.Gain(restored, 1);
            Assert.Equal(401, restoredProgress.Experience);
            foreach (var attribute in Enum.GetValues<AosAttribute>())
            {
                Assert.Equal(original.Attributes[attribute], restored.Attributes[attribute]);
            }
            for (var slot = 0; slot < 5; slot++)
            {
                Assert.Equal(original.SkillBonuses.GetSkill(slot), restored.SkillBonuses.GetSkill(slot));
                Assert.Equal(original.SkillBonuses.GetBonus(slot), restored.SkillBonuses.GetBonus(slot));
            }
        }
        finally
        {
            original.Delete();
            restored?.Delete();
            originalProgress?.Delete();
            restoredProgress?.Delete();
        }
    }

    private static void AssertWearableProfile(BaseClothing item, Layer layer, int itemId,
        params (SkillName Skill, double Bonus)[] skills)
    {
        var player = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100, RawDex = 100, RawInt = 100 };
        try
        {
            Assert.Equal(layer, item.Layer);
            Assert.Equal(itemId, item.ItemID);
            Assert.Equal(LootType.Blessed, item.LootType);
            Assert.True(item.Movable);
            Assert.False(string.IsNullOrWhiteSpace(item.Name));
            var before = new double[skills.Length];
            for (var slot = 0; slot < skills.Length; slot++)
            {
                var skill = skills[slot];
                Assert.Equal(skill.Skill, item.SkillBonuses.GetSkill(slot));
                Assert.Equal(skill.Bonus, item.SkillBonuses.GetBonus(slot));
                player.Skills[skill.Skill].Cap = 120;
                player.Skills[skill.Skill].Base = 60;
                before[slot] = player.Skills[skill.Skill].Value;
            }

            Assert.True(player.EquipItem(item));
            Assert.Same(item, player.FindItemOnLayer(layer));
            for (var slot = 0; slot < skills.Length; slot++)
            {
                Assert.Equal(before[slot] + skills[slot].Bonus, player.Skills[skills[slot].Skill].Value, 6);
            }
            player.RemoveItem(item);
            for (var slot = 0; slot < skills.Length; slot++)
            {
                Assert.Equal(before[slot], player.Skills[skills[slot].Skill].Value, 6);
            }
        }
        finally
        {
            player.RemoveItem(item);
            player.Delete();
        }
    }

    private static byte[] Serialize(Item item)
    {
        var writer = new BufferWriter(true);
        item.Serialize(writer);
        return writer.Buffer.AsSpan(0, (int)writer.Position).ToArray();
    }
}
