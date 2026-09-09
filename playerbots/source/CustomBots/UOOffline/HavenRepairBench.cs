using ModernUO.Serialization;
using Server.Items;

using Server.Gumps;
using Server.Network;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenRepairBench : WoodenBench
{
    public const int RepairCost = 50;
    public const int RestoreCost = 250;
    private static bool IsStarterGear(Item item) => item is IStarterUpgradeable or IEvolvingStarterWeapon or HavenLevelingCape or HavenStarterSash;
    [Constructible]
    public HavenRepairBench()
    {
        Name = "Adventurer's repair bench - 50 gold per item";
        Hue = 0x59B;
        Movable = false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Map != Map || !from.InRange(this, 3))
        {
            from.SendMessage("Stand beside the repair bench.");
            return;
        }
        from.CloseGump<RepairBenchGump>();
        from.SendGump(new RepairBenchGump(this));
    }

    internal bool Restore(Mobile from, Item item)
    {
        if (Deleted || from.Deleted || !from.Alive || from.Map != Map || !from.InRange(this, 3) ||
            item?.Deleted != false || (item.Parent != from && (from.Backpack == null || !item.IsChildOf(from.Backpack))))
        {
            from.SendMessage("Bring your own equipment to the repair bench while alive.");
            return false;
        }
        var (current, maximum) = item switch
        {
            BaseWeapon weapon => (weapon.MaxHitPoints, (weapon.InitMaxHits * (100 + weapon.GetDurabilityBonus()) + 99) / 100),
            BaseArmor armor => (armor.MaxHitPoints, (armor.InitMaxHits * (100 + armor.GetDurabilityBonus()) + 99) / 100),
            BaseClothing clothing => (clothing.MaxHitPoints, (clothing.InitMaxHits * (100 + clothing.ClothingAttributes.DurabilityBonus) + 99) / 100),
            _ => (0, 0)
        };
        if (maximum <= current)
        {
            from.SendMessage("This item is already at or above its type's normal maximum durability, or has no restorable durability. You were not charged.");
            return false;
        }
        var cost = IsStarterGear(item) ? 0 : RestoreCost;
        if (cost > 0 && !HavenEconomy.TryPay(from, cost))
        {
            from.SendMessage("Restoration costs 250 gold. Your wallet, backpack and bank funds are insufficient.");
            return false;
        }
        switch (item)
        {
            case BaseWeapon weapon:
                weapon.MaxHitPoints = maximum;
                weapon.HitPoints = maximum;
                break;
            case BaseArmor armor:
                armor.MaxHitPoints = maximum;
                armor.HitPoints = maximum;
                break;
            case BaseClothing clothing:
                clothing.MaxHitPoints = maximum;
                clothing.HitPoints = maximum;
                break;
        }
        from.PlaySound(0x2A);
        from.SendMessage($"Restored to {maximum}/{maximum} durability for {cost} gold. All bonuses are unchanged.");
        return true;
    }

    internal bool Repair(Mobile from, Item item)
    {
        if (Deleted || from.Deleted || !from.Alive || from.Map != Map || !from.InRange(this, 3) ||
            item?.Deleted != false || (item.Parent != from && (from.Backpack == null || !item.IsChildOf(from.Backpack))))
        {
            from.SendMessage("Bring your own equipment to the repair bench while alive.");
            return false;
        }
        var damaged = item switch
        {
            BaseWeapon weapon => weapon.MaxHitPoints > 0 && weapon.HitPoints < weapon.MaxHitPoints,
            BaseArmor armor => armor.MaxHitPoints > 0 && armor.HitPoints < armor.MaxHitPoints,
            BaseClothing clothing => clothing.MaxHitPoints > 0 && clothing.HitPoints < clothing.MaxHitPoints,
            _ => false
        };
        if (!damaged)
        {
            from.SendMessage("That item has no repairable damage. You were not charged.");
            return false;
        }
        var cost = IsStarterGear(item) ? 0 : RepairCost;
        if (cost > 0 && !HavenEconomy.TryPay(from, cost))
        {
            from.SendMessage("Repairs cost 50 gold. Your wallet, backpack and bank funds are insufficient.");
            return false;
        }
        switch (item)
        {
            case BaseWeapon weapon when weapon.MaxHitPoints > 0 && weapon.HitPoints < weapon.MaxHitPoints:
                weapon.HitPoints = weapon.MaxHitPoints;
                break;
            case BaseArmor armor when armor.MaxHitPoints > 0 && armor.HitPoints < armor.MaxHitPoints:
                armor.HitPoints = armor.MaxHitPoints;
                break;
            case BaseClothing clothing when clothing.MaxHitPoints > 0 && clothing.HitPoints < clothing.MaxHitPoints:
                clothing.HitPoints = clothing.MaxHitPoints;
                break;
            default:
                from.SendMessage("That item has no repairable damage. Repairs do not restore lost maximum durability.");
                return false;
        }
        from.PlaySound(0x2A);
        if (cost == 0) { from.SendMessage("Starter equipment repaired for free. Bonuses and maximum durability are unchanged."); }
        else { from.SendMessage("Repaired for 50 gold. Bonuses and maximum durability are unchanged."); }
        return true;
    }

    private sealed class RepairBenchGump : Gump
    {
        private readonly HavenRepairBench _bench;
        public RepairBenchGump(HavenRepairBench bench) : base(40, 40)
        {
            _bench = bench;
            AddBackground(0, 0, 520, 340, 5054);
            AddBackground(12, 12, 496, 316, 3000);
            AddHtml(30, 25, 460, 30, "<B>Adventurer's repair bench</B>");
            AddButton(30, 75, 4005, 4007, 1);
            AddLabel(68, 77, 0, "Repair ALL gear - 50 gold per item");
            AddHtml(68, 110, 400, 50, "Repairs equipped gear and everything inside your backpack. Starter gear is free. No maximum durability loss.");
            AddButton(30, 170, 4005, 4007, 2);
            AddLabel(68, 172, 0, "Restore ALL maximums - 250 gold per item");
            AddHtml(68, 205, 400, 70, "Starter gear is free. Restore the item's normal type maximum, including durability bonuses, and fully repair it. Wallet funds are used first.");
            AddButton(370, 290, 4005, 4007, 0);
            AddLabel(408, 292, 0, "Close");
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            var from = sender.Mobile;
            if (info.ButtonID is not (1 or 2) || _bench.Deleted || from.Map != _bench.Map || !from.InRange(_bench, 3)) { return; }

            _bench.RepairAll(from, info.ButtonID == 2);
            _bench.OnDoubleClick(from);
        }
    }
}
