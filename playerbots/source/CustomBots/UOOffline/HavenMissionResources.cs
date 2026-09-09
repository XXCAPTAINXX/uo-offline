using System;
using Server.Items;

namespace Server.UOOffline;

public static class HavenMissionResources
{
    private static readonly double[] MetalSkills = [0, 65, 70, 75, 80, 85, 90, 95, 99];
    private static readonly double[] WoodSkills = [0, 65, 80, 95, 100, 100, 100];
    private static readonly double[] LeatherSkills = [0, 65, 80, 100];
    internal static double Skill(HavenCompanion companion, HavenExpeditionKind kind) => companion == null ? 0 : kind switch
    {
        HavenExpeditionKind.Ore => companion.Skills.Mining.Base,
        HavenExpeditionKind.Wood => companion.Skills.Lumberjacking.Base,
        _ => Math.Min(companion.Skills.Wrestling.Base, companion.Skills.Tactics.Base)
    };
    internal static int SelectIndex(HavenExpeditionKind kind, double skill, double roll)
    {
        var requirements = kind switch { HavenExpeditionKind.Ore => MetalSkills, HavenExpeditionKind.Wood => WoodSkills, _ => LeatherSkills };
        var start = kind switch { HavenExpeditionKind.Ore => 0, HavenExpeditionKind.Wood => 9, _ => 23 };
        var highest = 0;
        while (highest + 1 < requirements.Length && skill >= requirements[highest + 1]) { highest++; }
        if (highest == 0) { return start; }
        roll = Math.Clamp(roll, 0, 0.999999);
        // Sixty percent selects the best unlocked tier; equal-level woods share that chance.
        if (roll < 0.6)
        {
            var first = highest;
            while (first > 1 && requirements[first - 1] == requirements[highest]) { first--; }
            return start + first + (int)(roll / 0.6 * (highest - first + 1));
        }
        return start + 1 + Math.Min(highest - 1, (int)((roll - 0.6) / 0.4 * highest));
    }
    internal static void Add(Bag bag, HavenExpeditionKind kind, int minutes, double skill, double? roll = null)
    {
        var basic = kind switch { HavenExpeditionKind.Ore => 0, HavenExpeditionKind.Wood => 9, _ => 23 };
        var amount = minutes * (kind == HavenExpeditionKind.Wood ? 40 : 20);
        var selected = SelectIndex(kind, skill, roll ?? Utility.RandomDouble());
        if (selected == basic) { bag.DropItem(Deed(basic, amount)); return; }
        bag.DropItem(Deed(basic, amount / 2));
        bag.DropItem(Deed(selected, amount - amount / 2));
    }
    private static CommodityDeed Deed(int index, int amount)
    {
        var item = HavenResourceCatalog.Entries[index].Create(amount);
        var deed = new CommodityDeed();
        if (!deed.SetCommodity(item)) { deed.Delete(); item.Delete(); throw new InvalidOperationException("Gathering resource must be deedable."); }
        return deed;
    }
}
