using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;

namespace Server.UOOffline;

public static class HavenCompanionGold
{
    internal static int Consolidate(Container pack)
    {
        if (pack?.Deleted != false) { return 0; }
        var partial = new Dictionary<(int, int, string, LootType, bool), Gold>();
        var removed = 0;
        // Only loose, ordinary gold: leave deliberately separated bags and special gold unchanged.
        foreach (var gold in pack.Items.OfType<Gold>().ToArray())
        {
            if (gold.Deleted || gold.GetType() != typeof(Gold) || !gold.Stackable || gold.Amount >= 60000) { continue; }
            var key = (gold.ItemID, gold.Hue, gold.Name, gold.LootType, gold.PlayerConstructed);
            if (partial.TryGetValue(key, out var target))
            {
                var moved = Math.Min(60000 - target.Amount, gold.Amount);
                target.Amount += moved;
                if (moved == gold.Amount) { gold.Delete(); removed++; }
                else { gold.Amount -= moved; }
                if (target.Amount == 60000) { partial.Remove(key); }
            }
            if (!gold.Deleted) { partial[key] = gold; }
        }
        return removed;
    }
}
