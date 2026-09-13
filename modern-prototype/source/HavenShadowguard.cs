using Server.Engines.PartySystem;
using Server.Engines.Shadowguard;

namespace Server.HavenPrototype
{
    public static class HavenShadowguard
    {
        public static bool HasRequiredRooms(ShadowguardController controller, Mobile member)
        {
            var companion = member as HavenCompanion;
            if (companion != null)
            {
                var owner = companion.BoundOwner;
                var party = owner == null ? null : Party.Get(owner);
                if (owner == null || !companion.Controlled || companion.ControlMaster != owner || party == null || !party.Contains(companion)) return false;
                member = owner;
            }
            return controller != null && member != null && controller.Table != null && controller.Table.ContainsKey(member) &&
                (controller.Table[member] & EncounterType.Required) == EncounterType.Required;
        }
    }
}
