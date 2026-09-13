using System.Linq;
using Server.Items;

namespace Server.HavenPrototype
{
    public partial class HavenCompanion
    {
        public override DeathMoveResult GetParentMoveResultFor(Item item)
        {
            return item != null && item.Parent == this ? DeathMoveResult.RemainEquiped : base.GetParentMoveResultFor(item);
        }
        // Starter garments must be removable through the owner's native paperdoll.
        // Weapons and role equipment retain their existing ownership rules.
        internal void EnsureWardrobe()
        {
            if (Deleted) return;
            foreach (var item in Items.ToArray())
                if (item is BaseClothing || (item is BaseArmor && !(item is BaseShield))) item.Movable = true;
            bool covered = new[] { Layer.Shirt, Layer.InnerTorso, Layer.MiddleTorso, Layer.OuterTorso }
                .Any(layer => FindItemOnLayer(layer) != null);
            if (!covered)
            {
                BaseClothing top = Female ? (BaseClothing)new FancyDress(0x59B) : new Shirt(0x59B);
                top.MaxHitPoints = 0; top.HitPoints = 0; top.LootType = LootType.Blessed;
                AddItem(top);
            }
            if (FindItemOnLayer(Layer.OuterTorso) == null && FindItemOnLayer(Layer.Pants) == null && FindItemOnLayer(Layer.OuterLegs) == null)
            {
                var pants = new LongPants(0x455) { MaxHitPoints = 0, HitPoints = 0, LootType = LootType.Blessed };
                AddItem(pants);
            }
        }
        public void OpenPaperdoll(Mobile from)
        {
            if (!CanOpenPack(from))
            {
                if (IsOwner(from)) from.SendMessage("Your companion must be alive, nearby and back from her mission to change equipment.");
                return;
            }
            from.CloseGump(typeof(CompanionGump));
            DisplayPaperdollTo(from);
            from.SendMessage("Drag clothing onto the paperdoll to equip it. Remove the old item first if that slot is occupied. Use [c for commands.");
        }
    }
}
