using ModernUO.Serialization;
using Server.Items;
using Server.Targeting;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenRepairBench : WoodenBench
{
    [Constructible]
    public HavenRepairBench()
    {
        Name = "Adventurer's repair bench - free repairs";
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
        from.SendMessage("Choose a weapon, armor, shield or piece of clothing you carry or wear. Repairs are free.");
        from.Target = new RepairTarget(this);
    }

    internal bool Repair(Mobile from, Item item)
    {
        if (Deleted || from.Deleted || !from.Alive || from.Map != Map || !from.InRange(this, 3) ||
            item?.Deleted != false || (item.Parent != from && (from.Backpack == null || !item.IsChildOf(from.Backpack))))
        {
            from.SendMessage("Bring your own equipment to the repair bench while alive.");
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
        from.SendMessage("Your item is fully repaired. Its properties and maximum durability are unchanged.");
        return true;
    }

    private sealed class RepairTarget : Target
    {
        private readonly HavenRepairBench _bench;
        public RepairTarget(HavenRepairBench bench) : base(3, false, TargetFlags.None) => _bench = bench;
        protected override void OnTarget(Mobile from, object targeted) => _bench.Repair(from, targeted as Item);
    }
}
