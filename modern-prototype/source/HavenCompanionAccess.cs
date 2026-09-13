using Server.Items;
namespace Server.HavenPrototype
{
    public static class HavenCompanionAccess
    {
        public static int InventoryRange(Mobile owner, Item item)
        {
            var c = item == null ? null : item.RootParent as HavenCompanion;
            return HavenPreview.Enabled && c != null && c.CanOpenPack(owner) &&
                (item == c.Backpack || item.IsChildOf(c.Backpack)) ? HavenCompanion.SupportRange : 2;
        }
        public static int BandageRange(Mobile healer, Mobile patient)
        {
            var c = healer as HavenCompanion;
            var other = patient;
            if (c == null) { c = patient as HavenCompanion; other = healer; }
            return HavenPreview.Enabled && c != null && !c.Deleted && !c.OnMission && !c.IsStabled &&
                (other == c || (c.BoundOwner == other && c.ControlMaster == other) || (healer == c && other is Server.Mobiles.BaseCreature && (((Server.Mobiles.BaseCreature)other).ControlMaster == c.BoundOwner || ((Server.Mobiles.BaseCreature)other).ControlMaster == c))) &&
                other != null && c.Map != Map.Internal && c.Map == other.Map && c.InLOS(other) ? HavenCompanion.SupportRange : Bandage.Range;
        }
    }
}
