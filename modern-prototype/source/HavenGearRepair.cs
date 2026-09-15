using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;

namespace Server.HavenPrototype
{
    public static class HavenGearRepair
    {
        public const int RestoreCost = 250;
        private sealed class Repair
        {
            public Item Item;
            public int Maximum;
            public Action<int> Apply;
        }
        private static List<Repair> Plan(Mobile owner)
        {
            var result = new List<Repair>();
            if (owner == null || owner.Deleted || !owner.Alive) return result;
            var items = new List<Item>(owner.Items);
            if (owner.Backpack != null) items.AddRange(owner.Backpack.FindItemsByType(typeof(Item), true));
            foreach (var item in items.Distinct().Where(i => !i.Deleted))
            {
                int current, original;
                Action<int> apply;
                var w = item as BaseWeapon; var a = item as BaseArmor;
                var c = item as BaseClothing; var j = item as BaseJewel;
                if (w != null) { current=w.MaxHitPoints; original=(w.InitMaxHits*(100+w.GetDurabilityBonus())+99)/100; apply=n=>{w.MaxHitPoints=n;w.HitPoints=n;}; }
                else if (a != null) { current=a.MaxHitPoints; original=(a.InitMaxHits*(100+a.GetDurabilityBonus())+99)/100; apply=n=>{a.MaxHitPoints=n;a.HitPoints=n;}; }
                else if (c != null) { current=c.MaxHitPoints; original=(c.InitMaxHits*(100+c.ClothingAttributes.DurabilityBonus)+99)/100; apply=n=>{c.MaxHitPoints=n;c.HitPoints=n;}; }
                else if (j != null) { current=j.MaxHitPoints; original=j.InitMaxHits; apply=n=>{j.MaxHitPoints=n;j.HitPoints=n;}; }
                else continue;
                if (current <= 0) continue;
                original = Math.Min(255, original);
                if (HavenGearDurability.Supported(item)) original = Math.Max(255, original);
                if (original > current) result.Add(new Repair { Item=item, Maximum=original, Apply=apply });
            }
            return result;
        }
        private static int Price(List<Repair> plan) { return plan.Count(p => !(p.Item is IHavenStarterGear)) * RestoreCost; }
        public static int Quote(Mobile owner, out int count) { var plan=Plan(owner);count=plan.Count;return Price(plan); }
        public static bool Restore(Mobile owner, int quotedMaximum)
        {
            if (!HavenPreview.Enabled || owner==null || owner.Deleted || !owner.Alive) return false;
            var plan=Plan(owner);var cost=Price(plan);
            if(plan.Count==0){owner.SendMessage("No carried equipment needs maximum-durability restoration.");return false;}
            if(cost>quotedMaximum){owner.SendMessage("Your equipment changed. Reopen the repair menu for an updated price; nothing was charged.");return false;}
            if(cost>0&&!HavenWallet.SpendBank(owner,cost)){owner.SendMessage("You need "+cost+" bank gold. Nothing was changed or charged.");return false;}
            foreach(var repair in plan)repair.Apply(repair.Maximum);
            owner.SendMessage("Restored "+plan.Count+" item(s) for "+cost+" gold. Equipment properties and experience are unchanged.");
            return true;
        }
    }
}
