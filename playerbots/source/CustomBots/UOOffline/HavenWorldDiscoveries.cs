using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

public static class HavenWorldDiscoveries
{
    private static readonly ConditionalWeakTable<BaseCreature, HashSet<Serial>> Attempts = new();
    internal static bool Eligible(BaseCreature creature, Mobile player) => creature?.Deleted == false && player is PlayerMobile &&
        player.Backpack != null && player.Map == creature.Map && player.InRange(creature, 24) &&
        !creature.Controlled && !creature.Summoned && !creature.IsBonded && !creature.NoKillAwards && !creature.IsInvulnerable &&
        creature.Owners.Count == 0 && creature is not BaseVendor and not HavenCompanion and not HavenTrainingSentinel;
    internal static bool Award(BaseCreature creature, Mobile player, double roll)
    {
        if (!Eligible(creature, player) || !Attempts.GetOrCreateValue(creature).Add(player.Serial)) { return false; }
        Item item;
        if (roll < 0.002)
        { item = Utility.Random(4) switch { 0 => new HavenGoldenShovel(), 1 => new HavenEndlessBandage(), 2 => new HavenResourceSatchel(), _ => new HavenTideSteedDeed() }; }
        else if (roll < 0.027) { item = Clothing(Utility.RandomDouble(), Utility.Random(7)); }
        else { return false; }
        if (!player.Backpack.TryDropItem(player, item, false))
        { if (creature.Corpse is { Deleted: false } corpse) { corpse.DropItem(item); } else { item.MoveToWorld(player.Location, player.Map); } }
        player.SendMessage(0x8A5, $"A discovery: {item.Name}!");
        if (player is Server.CustomBots.PlayerBot) { HavenMarketProduction.Consign(player, item); }
        return true;
    }
    internal static BaseClothing Clothing(double roll, int profile)
    {
        var tier = roll < .60 ? 0 : roll < .85 ? 1 : roll < .95 ? 2 : roll < .99 ? 3 : 4;
        var item = Utility.Random(10) switch
        {
            0 => (BaseClothing)new Shoes(), 1 => new Sandals(), 2 => new Doublet(), 3 => new ShortPants(),
            4 => new BodySash(), 5 => new HalfApron(), 6 => new FancyShirt(), 7 => new Surcoat(), 8 => new LongPants(), _ => new ThighBoots()
        };
        var names = new[] { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
        var roles = new[] { "Vanguard", "Arcanist", "Minstrel", "Beastkeeper", "Healer", "Artisan", "Ranger" };
        profile = Math.Clamp(profile, 0, 6);
        item.Name = $"{names[tier]} {roles[profile]} {item.GetType().Name}"; item.Hue = new[] { 0, 0x59B, 0x482, 0x489, 0x8A5 }[tier];
        var amount = new[] { 1, 2, 4, 6, 8 }[tier];
        item.Attributes.BonusStr = amount; item.Attributes.BonusDex = amount; item.Attributes.BonusInt = amount;
        if (profile is 1 or 2 or 4) { item.Attributes.SpellDamage = amount * 2; item.Attributes.LowerManaCost = Math.Min(8, amount); item.Attributes.RegenMana = 1 + tier; }
        else { item.Attributes.WeaponDamage = amount * 2; item.Attributes.RegenStam = 1 + tier; }
        item.Attributes.Luck = amount * 10;
        var skills = profile switch
        {
            0 => new[] { SkillName.Swords, SkillName.Tactics }, 1 => new[] { SkillName.Magery, SkillName.Spellweaving },
            2 => new[] { SkillName.Musicianship, SkillName.Discordance }, 3 => new[] { SkillName.AnimalTaming, SkillName.AnimalLore },
            4 => new[] { SkillName.Healing, SkillName.Veterinary }, 5 => new[] { SkillName.Blacksmith, SkillName.Tailoring },
            _ => new[] { SkillName.Archery, SkillName.Tactics }
        };
        item.SkillBonuses.SetValues(0, skills[0], amount + tier); if (tier >= 2) { item.SkillBonuses.SetValues(1, skills[1], amount); }
        if (tier == 4) { item.AddItem(new HavenLegendaryArtifact()); }
        return item;
    }
}
