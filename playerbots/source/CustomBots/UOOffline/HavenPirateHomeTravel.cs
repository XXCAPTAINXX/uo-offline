using Server.Spells;

namespace Server.UOOffline;

public partial class HavenPirateEstate
{
    internal string TravelBlockReason(Mobile from)
    {
        if (from?.Deleted != false) { return "This character is unavailable."; }
        if (Deleted || Map == null || Map == Map.Internal) { return "Your island is currently unavailable."; }
        if (from != Owner) { return "This island belongs to another character."; }
        if (!from.Alive) { return "You must be alive to use [home."; }
        if (from.Criminal) { return "You cannot use [home while flagged criminal. Wait for the criminal flag to expire."; }
        if (from.Spell != null) { return "Finish or cancel your current spell before using [home. If a spell target is waiting, press Escape to cancel it."; }
        if (SpellHelper.CheckCombat(from)) { return "You cannot use [home during the PvP travel cooldown. Wait for the combat restriction to expire."; }
        if (!SpellHelper.CheckTravel(from, TravelCheckType.RecallFrom, out _))
        { return "You cannot recall home from this area. Leave the restricted area and try [home again."; }
        return null;
    }

    // Main-loop diagnostics use the same checks as [home without moving anyone.
    internal static string HomeStatus(Mobile from)
    {
        foreach (var estate in Registry)
        {
            if (estate.Deleted || estate.Owner != from) { continue; }
            var reason = estate.TravelBlockReason(from);
            if (reason != null) { return reason; }
            return estate.TryFindHomeLanding(from, out _) ? "Ready" :
                "Your island landing is obstructed or travel to it is restricted.";
        }
        return "No island is registered to this character.";
    }
}
