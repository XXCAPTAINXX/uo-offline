using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.UOOffline;

public partial class HavenRepairBench
{
    internal bool RepairAll(Mobile from, bool restore = false)
    {
        if (Deleted || from?.Deleted != false || !from.Alive || from.Map != Map || !from.InRange(this, 3)) { return false; }
        var repairs = new List<(Item Item, int Maximum)>();
        var cost = 0;
        void Add(Item item)
        {
            var (hits, max, original) = item switch
            {
                BaseWeapon w => (w.HitPoints, w.MaxHitPoints, (w.InitMaxHits * (100 + w.GetDurabilityBonus()) + 99) / 100),
                BaseArmor a => (a.HitPoints, a.MaxHitPoints, (a.InitMaxHits * (100 + a.GetDurabilityBonus()) + 99) / 100),
                BaseClothing c => (c.HitPoints, c.MaxHitPoints, (c.InitMaxHits * (100 + c.ClothingAttributes.DurabilityBonus) + 99) / 100),
                BaseJewel j => (j.HitPoints, j.MaxHitPoints, j.InitMaxHits),
                _ => (0, 0, 0)
            };
            if (item.Deleted || (restore ? original <= max : max <= 0 || hits >= max)) { return; }
            repairs.Add((item, restore ? Math.Max(max, original) : max));
            if (!IsStarterGear(item)) { cost += restore ? RestoreCost : RepairCost; }
        }
        foreach (var item in from.Items) { Add(item); }
        if (from.Backpack != null) { foreach (var item in from.Backpack.FindItemsByType<Item>()) { Add(item); } }
        if (repairs.Count == 0) { from.SendMessage("None of your equipped or backpack gear needs this service."); return false; }
        if (cost > 0 && !HavenEconomy.TryPay(from, cost))
        { from.SendMessage($"You need {cost:N0} gold for all {repairs.Count} items. Nothing was changed or charged."); return false; }
        foreach (var (item, maximum) in repairs)
        {
            switch (item)
            {
                case BaseWeapon w: w.MaxHitPoints = maximum; w.HitPoints = maximum; break;
                case BaseArmor a: a.MaxHitPoints = maximum; a.HitPoints = maximum; break;
                case BaseClothing c: c.MaxHitPoints = maximum; c.HitPoints = maximum; break;
                case BaseJewel j: j.MaxHitPoints = maximum; j.HitPoints = maximum; break;
            }
        }
        from.PlaySound(0x2A);
        from.SendMessage($"Finished {repairs.Count} item(s) for {cost:N0} gold. Starter gear is free; equipment bonuses are unchanged.");
        return true;
    }
}
