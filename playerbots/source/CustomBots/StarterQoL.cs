// =========================================================================
// StarterQoL.cs — compact offline-shard quality-of-life items.
//
// Original implementations inspired by public ServUO community concepts,
// rewritten for current ModernUO APIs:
//
//   StarterReagentPouch
//     * reagents only
//     * 90% carried-weight reduction
//
//   BritanniaCleanupBag
//     * accepts ordinary movable non-container items
//     * 30-second retrieval window
//     * converts discarded items into persistent Cleanup Points
//
//   GoldRepairBench
//     * repairs equipped durability-bearing gear in one action
//     * charges backpack/account-bank gold per missing durability point
//     * does not reduce maximum durability
//
// Commands:
//   [QoLKit       (GameMaster test convenience)
// =========================================================================

using System;
using ModernUO.Serialization;
using Server;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.CustomBots
{
    [SerializationGenerator(0)]
    public partial class StarterReagentPouch : Bag
    {
        public const int ReductionPercent = 90;

        [Constructible]
        public StarterReagentPouch()
        {
            Name = "Starter Reagent Pouch";
            Hue = 0x48D;
            LootType = LootType.Blessed;
        }

        public override double DefaultWeight => 1.0;
        public override int DefaultMaxItems => 125;
        public override int DefaultMaxWeight => 0;

        public override int GetTotal(TotalType type)
        {
            int total = base.GetTotal(type);

            if (type == TotalType.Weight)
            {
                total -= total * ReductionPercent / 100;
            }

            return total;
        }

        public override bool CheckHold(
            Mobile m,
            Item item,
            bool message,
            bool checkItems,
            int plusItems,
            int plusWeight
        )
        {
            if (item is not BaseReagent)
            {
                if (message)
                {
                    m?.SendMessage("That pouch can only hold spell reagents.");
                }

                return false;
            }

            return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
        }

        public override void AddItem(Item dropped)
        {
            base.AddItem(dropped);
            RefreshCarryWeight();
        }

        public override void RemoveItem(Item dropped)
        {
            base.RemoveItem(dropped);
            RefreshCarryWeight();
        }

        private void RefreshCarryWeight()
        {
            // BaseQuiver has a private/generated InvalidateWeight helper that
            // custom containers cannot call. Rebuild totals at the actual
            // inventory root instead; Mobile.UpdateTotals recursively asks
            // this pouch for its overridden reduced TotalWeight.
            if (RootParent is Mobile mobile)
            {
                mobile.UpdateTotals();
            }
            else if (Parent is Container parent)
            {
                parent.UpdateTotals();
            }
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add($"Reagents only; {ReductionPercent}% weight reduction");
        }
    }

    [SerializationGenerator(0)]
    public partial class BritanniaCleanupBag : Bag
    {
        private Timer _timer;

        [Constructible]
        public BritanniaCleanupBag()
        {
            Name = "Britannia Cleanup Bag";
            Hue = 0x59B;
            LootType = LootType.Blessed;
        }

        public override double DefaultWeight => 1.0;
        public override int DefaultMaxItems => 50;
        public override int DefaultMaxWeight => 0;

        [AfterDeserialization(false)]
        private void AfterDeserialization()
        {
            if (Items.Count > 0)
            {
                StartTimer();
            }
        }

        public override bool CheckHold(
            Mobile m,
            Item item,
            bool message,
            bool checkItems,
            int plusItems,
            int plusWeight
        )
        {
            if (item == null ||
                item is Container ||
                item is OfflineTravelBook ||
                item is AdventurersWallet ||
                item is StarterReagentPouch ||
                item is BritanniaCleanupBag ||
                item is GoldRepairBench ||
                !item.Movable ||
                item.LootType is LootType.Blessed or LootType.Newbied)
            {
                if (message)
                {
                    m?.SendMessage(
                        "That item is protected or unsuitable for the cleanup program."
                    );
                }

                return false;
            }

            return base.CheckHold(m, item, message, checkItems, plusItems, plusWeight);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!base.OnDragDrop(from, dropped))
            {
                return false;
            }

            NotifyAndStart(from);
            return true;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (!base.OnDragDropInto(from, item, p))
            {
                return false;
            }

            NotifyAndStart(from);
            return true;
        }

        public override void RemoveItem(Item dropped)
        {
            base.RemoveItem(dropped);

            if (Items.Count == 0)
            {
                _timer?.Stop();
                _timer = null;
            }
        }

        private void NotifyAndStart(Mobile from)
        {
            from?.SendMessage(
                0x59,
                "Cleanup item accepted. You have 30 seconds to take it back before recycling."
            );

            StartTimer();
        }

        private void StartTimer()
        {
            _timer?.Stop();
            _timer = Timer.DelayCall(TimeSpan.FromSeconds(30), ProcessCleanup);
        }

        private void ProcessCleanup()
        {
            _timer = null;

            if (Deleted || Items.Count == 0)
            {
                return;
            }

            if (RootParent is not Mobile owner || owner.Deleted)
            {
                // Do not destroy items if the bag is sitting loose in the
                // world. Wait until it belongs to a player again.
                StartTimer();
                return;
            }

            int points = 0;
            var snapshot = Items.ToArray();

            foreach (var item in snapshot)
            {
                if (item == null || item.Deleted)
                {
                    continue;
                }

                points += PointValue(item);
                item.Delete();
            }

            if (points > 0)
            {
                OfflineWalletSystem.CreditCleanup(owner, points, "Britannia cleanup");
                owner.SendMessage("The cleanup bag has recycled its contents.");
            }
        }

        private static int PointValue(Item item)
        {
            int amount = Math.Max(1, item.Amount);

            // Reward stacks modestly without making easily farmed stackables
            // an unlimited currency printer.
            int stackBonus = Math.Min(20, (amount - 1) / 10);
            int weightBonus = Math.Min(4, Math.Max(0, item.PileWeight / 20));

            return 1 + stackBonus + weightBonus;
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Recycles ordinary items after 30 seconds");
            list.Add("Awards persistent Cleanup Points");
        }
    }

    [SerializationGenerator(0)]
    public partial class GoldRepairBench : Item
    {
        public const int GoldPerDurabilityPoint = 25;

        [Constructible]
        public GoldRepairBench() : base(0x19F1)
        {
            Name = "Gold Repair Bench";
            Weight = 20.0;
            Movable = true;
            Hue = 0x835;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || from.Deleted)
            {
                return;
            }

            if (IsChildOf(from.Backpack))
            {
                from.SendMessage("Place the repair bench in your house or workshop before using it.");
                return;
            }

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendMessage("You are too far away from the repair bench.");
                return;
            }

            var house = BaseHouse.FindHouseAt(this);
            if (house != null && !house.IsFriend(from))
            {
                from.SendMessage("You do not have permission to use this repair bench.");
                return;
            }

            int missing = 0;
            int count = 0;

            foreach (var item in from.Items)
            {
                if (item is not IDurability durability ||
                    durability.MaxHitPoints <= 0 ||
                    durability.HitPoints >= durability.MaxHitPoints)
                {
                    continue;
                }

                missing += durability.MaxHitPoints - Math.Max(0, durability.HitPoints);
                count++;
            }

            if (count == 0 || missing <= 0)
            {
                from.SendMessage("Your equipped durability-bearing items do not need repair.");
                return;
            }

            int cost = Math.Max(100, missing * GoldPerDurabilityPoint);

            bool paid = from.Backpack?.ConsumeTotal(typeof(Gold), cost) == true ||
                        Banker.Withdraw(from, cost);

            if (!paid)
            {
                from.SendMessage(
                    $"Repairing your equipped gear would cost {cost:N0} gold. You do not have enough."
                );
                return;
            }

            int repaired = 0;

            foreach (var item in from.Items)
            {
                if (item is not IDurability durability ||
                    durability.MaxHitPoints <= 0 ||
                    durability.HitPoints >= durability.MaxHitPoints)
                {
                    continue;
                }

                durability.HitPoints = durability.MaxHitPoints;
                repaired++;
            }

            Effects.PlaySound(Location, Map, 0x2A);
            from.SendMessage(
                0x35,
                $"Repair complete: {repaired} equipped item(s) restored for {cost:N0} gold."
            );
        }

        public override void GetProperties(IPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Repairs all equipped durability-bearing gear");
            list.Add($"{GoldPerDurabilityPoint} gold per missing durability point; 100 gold minimum");
            list.Add("Does not reduce maximum durability");
        }
    }

    public static class StarterQoLCommands
    {
        public static void Configure()
        {
            CommandSystem.Register("QoLKit", AccessLevel.GameMaster, QoLKit_OnCommand);
        }

        private static void QoLKit_OnCommand(CommandEventArgs e)
        {
            var from = e.Mobile;
            if (from?.Backpack == null)
            {
                return;
            }

            var bag = new Bag { Name = "QoL Test Kit" };
            bag.DropItem(new StarterReagentPouch());
            bag.DropItem(new BritanniaCleanupBag());
            bag.DropItem(new GoldRepairBench());

            if (!from.AddToBackpack(bag))
            {
                bag.Delete();
                from.SendMessage("Make room in your backpack first.");
                return;
            }

            from.SendMessage("QoL test kit added.");
        }
    }
}
