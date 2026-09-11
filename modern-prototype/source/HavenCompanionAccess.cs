using Server.Items;
namespace Server.HavenPrototype
{
    public static class HavenCompanionAccess
    {
        public static int InventoryRange(Mobile owner, Item item)
        {
            var c = item == null ? null : item.RootParent as HavenCompanion;
            return HavenPreview.Enabled && c != null && c.CanOpenPack(owner) &&
                (item == c.Backpack || item.IsChildOf(c.Backpack)) ? 12 : 2;
        }
        public static int BandageRange(Mobile healer, Mobile patient)
        {
            var c = healer as HavenCompanion;
            var other = patient;
            if (c == null) { c = patient as HavenCompanion; other = healer; }
            return HavenPreview.Enabled && c != null && !c.Deleted && !c.OnMission && !c.IsStabled &&
                (other == c || (c.BoundOwner == other && c.ControlMaster == other)) &&
                other != null && c.Map != Map.Internal && c.Map == other.Map && c.InLOS(other) ? 12 : Bandage.Range;
        }
    }
}
