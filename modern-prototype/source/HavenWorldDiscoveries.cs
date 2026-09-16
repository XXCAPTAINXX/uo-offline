using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Items;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    // Clothing rules ported from the original HavenWorldDiscoveries.
    public static class HavenWorldDiscoveries
    {
        static readonly ConditionalWeakTable<BaseCreature, HashSet<Serial>> Attempts = new ConditionalWeakTable<BaseCreature, HashSet<Serial>>();
        public static void Initialize()
        {
            EventSink.CreatureDeath += e => {
                if (!HavenPreview.Enabled) return;
                var creature = e.Creature as BaseCreature;
                var player = e.Killer == null ? null : e.Killer.GetDamageMaster(creature) ?? e.Killer;
                Award(creature, player, Utility.RandomDouble());
            };
        }
        public static bool Award(BaseCreature creature, Mobile player, double roll)
        {
            if (creature == null || creature.Deleted || !(player is PlayerMobile) || player.Backpack == null ||
                player.Map != creature.Map || !player.InRange(creature, 24) || creature.Controlled || creature.Summoned ||
                creature.IsBonded || creature.NoKillAwards || creature.IsInvulnerable || creature.Owners.Count != 0 ||
                creature is BaseVendor || creature is HavenCompanion || !Attempts.GetOrCreateValue(creature).Add(player.Serial)) return false;
            // The original 0.2% utility-item branch is separate; preserve the 2.5% clothing interval.
            if (roll < 0 || roll >= .027) return false;
            if (roll < .002) { HavenAdvancedRewards.Deliver(player, creature, UtilityItem(Utility.Random(4))); return true; }
            HavenAdvancedRewards.Deliver(player, creature, Clothing(Utility.RandomDouble(), Utility.Random(7), Utility.Random(10)));
            return true;
        }
        public static Item UtilityItem(int index) { switch(index) { case 0:return new HavenGoldenShovel();case 1:return new HavenEndlessBandage();case 2:return new HavenResourceSatchel();default:return new HavenTideSteedDeed();} }
        public static BaseClothing Clothing(double roll, int profile, int style)
        {
            int tier = roll < .60 ? 0 : roll < .85 ? 1 : roll < .95 ? 2 : roll < .99 ? 3 : 4;
            BaseClothing item;
            switch (style) {
                case 0: item = new Shoes(); break; case 1: item = new Sandals(); break;
                case 2: item = new Doublet(); break; case 3: item = new ShortPants(); break;
                case 4: item = new BodySash(); break; case 5: item = new HalfApron(); break;
                case 6: item = new FancyShirt(); break; case 7: item = new Surcoat(); break;
                case 8: item = new LongPants(); break; default: item = new ThighBoots(); break;
            }
            string[] names = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
            string[] roles = { "Vanguard", "Arcanist", "Minstrel", "Beastkeeper", "Healer", "Artisan", "Ranger" };
            profile = Math.Max(0, Math.Min(6, profile));
            item.Name = names[tier] + " " + roles[profile] + " " + item.GetType().Name;
            item.Hue = new[] { 0, 0x59B, 0x482, 0x489, 0x8A5 }[tier];
            int amount = new[] { 1, 2, 4, 6, 8 }[tier];
            item.Attributes.BonusStr = item.Attributes.BonusDex = item.Attributes.BonusInt = amount;
            if (profile == 1 || profile == 2 || profile == 4) {
                item.Attributes.SpellDamage = amount * 2; item.Attributes.LowerManaCost = Math.Min(8, amount); item.Attributes.RegenMana = 1 + tier;
            } else { item.Attributes.WeaponDamage = amount * 2; item.Attributes.RegenStam = 1 + tier; }
            item.Attributes.Luck = amount * 10;
            SkillName[][] skills = {
                new[] { SkillName.Swords, SkillName.Tactics }, new[] { SkillName.Magery, SkillName.Spellweaving },
                new[] { SkillName.Musicianship, SkillName.Discordance }, new[] { SkillName.AnimalTaming, SkillName.AnimalLore },
                new[] { SkillName.Healing, SkillName.Veterinary }, new[] { SkillName.Blacksmith, SkillName.Tailoring },
                new[] { SkillName.Archery, SkillName.Tactics }
            };
            item.SkillBonuses.SetValues(0, skills[profile][0], amount + tier);
            if (tier >= 2) item.SkillBonuses.SetValues(1, skills[profile][1], amount);
            if (tier == 4) {
                // The shared legendary record supplies the rarity prefix.
                item.Name = item.Name.Substring(10);
                HavenAdvancedGear.Attach(item, 2);
            }
            return item;
        }
    }
}
