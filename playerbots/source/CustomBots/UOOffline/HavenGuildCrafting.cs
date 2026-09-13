using System;
using System.Collections.Generic;
using Server.CustomBots;
using Server.Engines.Craft;
using Server.Items;

namespace Server.UOOffline;

public enum HavenGuildJob { Auto, Mine, Wood, Hunt, Craft, Train }

internal static class HavenGuildCrafting
{
    private static readonly SkillName[] Crafts = { SkillName.Blacksmith, SkillName.Tailoring, SkillName.Carpentry, SkillName.Tinkering, SkillName.Fletching, SkillName.Inscribe, SkillName.Alchemy, SkillName.Cooking };
    internal static SkillName BestSkill(PlayerBot worker)
    {
        var best = SkillName.Tactics;
        foreach (var skill in Crafts) { if (worker.Skills[skill].Base > worker.Skills[best].Base) { best = skill; } }
        foreach (var skill in new[] { SkillName.Mining, SkillName.Lumberjacking, SkillName.Magery })
        { if (worker.Skills[skill].Base > worker.Skills[best].Base) { best = skill; } }
        return best;
    }
    internal static HavenMarketTrade BestTrade(PlayerBot worker)
    {
        var best = 0;
        for (var i = 1; i < Crafts.Length; i++) { if (worker.Skills[Crafts[i]].Base > worker.Skills[Crafts[best]].Base) { best = i; } }
        return (HavenMarketTrade)best;
    }
    internal static HavenExpeditionKind GatherFor(PlayerBot worker) => BestTrade(worker) switch
    {
        HavenMarketTrade.Tailor => HavenExpeditionKind.Leather,
        HavenMarketTrade.Carpenter or HavenMarketTrade.Fletcher => HavenExpeditionKind.Wood,
        _ => HavenExpeditionKind.Ore
    };
    internal static bool TryWork(HavenGuildCrew crew)
    {
        if (crew.Worker?.Deleted != false || crew.Products.Count >= 24) { return false; }
        var trade = BestTrade(crew.Worker); var system = HavenMarketProduction.System(trade);
        if (system == null || crew.Worker.Skills[system.MainSkill].Base < 40) { return false; }
        var choices = new List<CraftItem>();
        foreach (var recipe in system.CraftItems)
        {
            if (recipe.Recipe != null || recipe.RequiredExpansion > Core.Expansion || recipe.Resources.Count == 0) { continue; }
            if (!Requirements(crew, recipe, out _)) { continue; }
            recipe.GetSuccessChance(crew.Worker, null, system, false, out var eligible);
            if (eligible) { choices.Add(recipe); }
        }
        if (choices.Count == 0) { return false; }
        return Craft(crew, system, choices[Utility.Random(choices.Count)]);
    }
    private static bool Requirements(HavenGuildCrew crew, CraftItem recipe, out Dictionary<int, int> required)
    {
        required = new Dictionary<int, int>();
        foreach (var material in recipe.Resources)
        {
            var index = -1;
            for (var i = 0; i < HavenResourceCatalog.Entries.Length; i++)
            { if (HavenResourceCatalog.Entries[i].Type == material.ItemType) { index = i; break; } }
            if (index < 0) { return false; }
            required.TryGetValue(index, out var amount); required[index] = amount + material.Amount;
        }
        foreach (var entry in required)
        { if (entry.Key >= crew.Resources.Count || crew.Resources[entry.Key] < entry.Value) { return false; } }
        return true;
    }
    internal static bool Craft(HavenGuildCrew crew, CraftSystem system, CraftItem recipe, double roll = -1)
    {
        if (!crew.ValidGuild || crew.Products.Count >= 24 || recipe.Recipe != null || recipe.RequiredExpansion > Core.Expansion || !Requirements(crew, recipe, out var required)) { return false; }
        var chance = recipe.GetSuccessChance(crew.Worker, null, system, false, out var eligible);
        if (!eligible) { return false; }
        // Create the supported output before charging; construction failure preserves materials.
        var item = BotItemFactory.Create(recipe.ItemType.FullName); if (item == null) { return false; }
        var success = (roll < 0 ? Utility.RandomDouble() : roll) < chance;
        if (success)
        {
            var exceptional = Utility.RandomDouble() < recipe.GetExceptionalChance(system, chance, crew.Worker);
            try
            {
                if (item is ICraftable craftable) { craftable.OnCraft(exceptional ? 2 : 1, false, crew.Worker, system, recipe.Resources[0].ItemType, null, recipe, 0); }
            }
            catch { item.Delete(); throw; }
        }
        foreach (var entry in required) { crew.Resources[entry.Key] -= entry.Value; }
        var skill = crew.Worker.Skills[system.MainSkill]; skill.Base = Math.Min(skill.Cap, skill.Base + .1);
        if (!success) { item.Delete(); crew.Log($"Crafting attempt failed: {recipe.ItemType.Name}; materials consumed"); }
        else
        {
            crew.AddItem(item); crew.Products.Add(item); crew.CraftsCompleted++;
            crew.Log($"Crafted {BotAppraisal.NameFor(item)}");
        }
        crew.MarkDirty(); return true;
    }
    internal static bool TakeProduct(HavenGuildCrew crew, Mobile from, Item item)
    {
        if (!crew.CanWithdraw(from) || from.Backpack == null || item?.Deleted != false || item.Parent != crew || !crew.Products.Contains(item) ||
            !from.Backpack.TryDropItem(from, item, true)) { return false; }
        crew.Products.Remove(item); crew.MarkDirty(); crew.Log($"Withdrew crafted item: {BotAppraisal.NameFor(item)}"); return true;
    }
}
