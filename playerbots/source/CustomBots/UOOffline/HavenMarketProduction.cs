using System;
using System.Collections.Generic;
using Server.CustomBots;
using Server.Engines.Craft;
using Server.Items;

namespace Server.UOOffline;

public static class HavenMarketProduction
{
    internal static string Name(HavenMarketTrade trade) => trade switch
    {
        HavenMarketTrade.Smith => "Gareth Ironwood", HavenMarketTrade.Tailor => "Elara Threadwell",
        HavenMarketTrade.Carpenter => "Rowan Ashwood", HavenMarketTrade.Tinker => "Tessa Copperhand",
        HavenMarketTrade.Fletcher => "Fenn Reed", HavenMarketTrade.Scribe => "Mira Quill",
        HavenMarketTrade.Alchemist => "Silas Vale", HavenMarketTrade.Cook => "Nora Hearth",
        HavenMarketTrade.Gatherer => "Bram Fieldstone",
        HavenMarketTrade.Pets => "Lydia Wildmere", HavenMarketTrade.PetSupplies => "Finn Bridlewood",
        HavenMarketTrade.DungeonSupplies => "Darian Ashford",
        HavenMarketTrade.Artifacts => "Seraphine Relicward", HavenMarketTrade.Jewelry => "Jasper Silverleaf", HavenMarketTrade.GearSets => "Freya Oathkeeper",
        _ => "Cassian Farwalker"
    };
    internal static CraftSystem System(HavenMarketTrade trade) => trade switch
    {
        HavenMarketTrade.Smith => DefBlacksmithy.CraftSystem, HavenMarketTrade.Tailor => DefTailoring.CraftSystem,
        HavenMarketTrade.Carpenter => DefCarpentry.CraftSystem, HavenMarketTrade.Tinker => DefTinkering.CraftSystem,
        HavenMarketTrade.Fletcher => DefBowFletching.CraftSystem, HavenMarketTrade.Scribe => DefInscription.CraftSystem,
        HavenMarketTrade.Alchemist => DefAlchemy.CraftSystem, HavenMarketTrade.Cook => DefCooking.CraftSystem,
        _ => null
    };
    internal static void Work(HavenMarketStall stall)
    {
        if (stall.Trade is HavenMarketTrade.PetSupplies or HavenMarketTrade.DungeonSupplies)
        { HavenMarketExpansion.Work(stall); return; }
        if (stall.Trade == HavenMarketTrade.Gatherer)
        {
            if (stall.Stock.Count >= 24) { return; }
            var kind = (stall.WorkSequence % 3) switch { 0 => HavenExpeditionKind.Ore, 1 => HavenExpeditionKind.Wood, _ => HavenExpeditionKind.Leather };
            var index = HavenMissionResources.SelectIndex(kind, 100, Utility.RandomDouble());
            var material = HavenResourceCatalog.Entries[index].Create(50); var deed = new CommodityDeed();
            if (deed.SetCommodity(material)) { stall.ListItem(deed, 500 + index * 20); }
            else { deed.Delete(); material.Delete(); }
            return;
        }
        var system = System(stall.Trade);
        if (system == null || stall.Stock.Count >= 24) { return; }
        CraftItem recipe = null;
        if (stall.Product != null)
        { foreach (var candidate in system.CraftItems) { if (candidate.ItemType == stall.Product) { recipe = candidate; break; } } }
        if (recipe == null)
        {
            var choices = new List<CraftItem>();
            foreach (var candidate in system.CraftItems)
            {
                if (candidate.Recipe != null || candidate.RequiredExpansion > Core.Expansion) { continue; }
                var supported = true;
                foreach (var skill in candidate.Skills) { if (stall.Artisan.Skills[skill.SkillToMake].Base < skill.MinSkill) { supported = false; break; } }
                if (supported) { choices.Add(candidate); }
            }
            if (choices.Count == 0) { return; }
            recipe = choices[Utility.Random(choices.Count)]; stall.Product = recipe.ItemType;
        }
        foreach (var requirement in recipe.Resources)
        {
            if (stall.Artisan.Backpack.GetAmount(requirement.ItemType) >= requirement.Amount) { continue; }
            var gathered = BotItemFactory.Create(requirement.ItemType.FullName);
            if (gathered == null) { stall.Product = null; return; }
            if (!gathered.Stackable) { gathered.Delete(); stall.Product = null; return; }
            gathered.Amount = Math.Max(10, requirement.Amount);
            stall.Artisan.Backpack.DropItem(gathered); return;
        }
        var chance = recipe.GetSuccessChance(stall.Artisan, null, system, false, out var eligible);
        if (!eligible) { stall.Product = null; return; }
        foreach (var requirement in recipe.Resources) { stall.Artisan.Backpack.ConsumeTotal(requirement.ItemType, requirement.Amount); }
        var skillValue = stall.Artisan.Skills[system.MainSkill]; skillValue.Base = Math.Min(skillValue.Cap, skillValue.Base + 0.02);
        if (Utility.RandomDouble() >= chance) { return; }
        var item = BotItemFactory.Create(recipe.ItemType.FullName);
        if (item == null) { stall.Product = null; return; }
        var exceptional = Utility.RandomDouble() < recipe.GetExceptionalChance(system, chance, stall.Artisan);
        // This is a paid-time workshop job; resources were gathered and consumed above.
        try
        {
            if (item is ICraftable craftable) { craftable.OnCraft(exceptional ? 2 : 1, false, stall.Artisan, system, recipe.Resources.Count > 0 ? recipe.Resources[0].ItemType : typeof(IronIngot), null, recipe, 0); }
        }
        catch { item.Delete(); throw; }
        if (!stall.ListItem(item, Math.Max(100, BotAppraisal.Value(item)))) { item.Delete(); }
        stall.Product = null;
    }
    public static bool Consign(Mobile producer, Item item)
    {
        if (producer is not PlayerBot bot || item?.Deleted != false || item.RootParent != producer || !item.Movable || item.IsVirtualItem) { return false; }
        if (HavenBotLoot.PlayerOwner(bot) != null) { HavenBotLoot.Receive(bot, item); return true; }
        if (HavenGuildCrew.Retained(bot) || item is Gold) { return false; }
        if (HavenBotEquipment.EquipIfBetter((PlayerBot)producer, item)) { return false; }
        var trade = item is HavenMinaxCreditNote or HavenMaritimeCargo or HavenDoomRecipe or PowerScroll or CommodityDeed or HavenMark or AstralShard ? HavenMarketTrade.DungeonSupplies :
            item is HavenBondingPotion or HavenPetLeash or HavenHouseHitchingPost ? HavenMarketTrade.PetSupplies :
            HavenBossArtifact.IsArtifact(item) || item is HavenSmallSoulForgeDeed || HavenDoom.IsArtifact(item) || HavenLegendaryArtifact.IsLegendary(item) ? HavenMarketTrade.Artifacts :
            item is HavenSetRing or ValorGauntlets or SpiritualityHelm || HavenJewelrySets.BraceletTheme(item) >= 0 ? HavenMarketTrade.GearSets :
            item is BaseJewel ? HavenMarketTrade.Jewelry : HavenMarketTrade.Adventurer;
        foreach (var stall in HavenMarketStall.Registry)
        {
            if (!stall.Deleted && stall.Trade == trade && stall.Stock.Count < 24)
            {
                var floor = item is HavenSmallSoulForgeDeed ? 250000 : HavenBossArtifact.IsArtifact(item) ? 75000 : item is HavenMinaxCreditNote note ? 1000 * note.Amount : item is HavenMaritimeCargo cargo ? 1000 * cargo.Value :
                    item is HavenDoomRecipe ? 12000 : item is HavenTideSteedDeed ? 100000 : item is HavenGoldenShovel or HavenEndlessBandage || HavenDoom.IsArtifact(item) ? 50000 : item is HavenResourceSatchel ? 15000 : 100;
                if (!stall.ListItem(item, Math.Max(floor, BotAppraisal.Value(item)))) { return false; }
                HavenMarketProvenance.Attach(item, "Adventuring loot", producer.Name);
                return true;
            }
        }
        return false;
    }
}
