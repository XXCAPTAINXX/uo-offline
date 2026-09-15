using System;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using CompanionParty = Server.Engines.PartySystem.Party;

namespace Server.UOOffline;

public partial class HavenCompanion
{
    private DateTime _nextSkinning = Core.Now;

    // Account tags keep this preference through saves without changing companion serialization.
    internal bool AutoSkinning
    {
        get => (BoundOwner?.Account as Account)?.GetTag($"HavenSkinning:{BoundOwner.Serial}") != "off";
        set
        {
            if (BoundOwner?.Account is Account account)
            {
                account.SetTag($"HavenSkinning:{BoundOwner.Serial}", value ? "on" : "off");
            }
        }
    }

    private static bool HasLiveCombatant(Mobile mobile) =>
        mobile?.Combatant is Mobile { Deleted: false, Alive: true } target &&
        target is not BaseCreature { IsDeadPet: true };

    internal bool CanSkinWhileHunting() =>
        !Deleted && Alive && !IsDeadPet && Controlled && ControlMaster == BoundOwner &&
        AutoSkinning && !IsStabled && Expedition == null && !TamingAssistActive &&
        BoundOwner is { Deleted: false, Alive: true } && Map != null && Map != Map.Internal &&
        BoundOwner.Map == Map && InRange(BoundOwner, 12) &&
        ControlOrder is OrderType.Follow or OrderType.Guard &&
        !HasLiveCombatant(this) && !HasLiveCombatant(BoundOwner) &&
        Hits >= HitsMax && BoundOwner.Hits >= BoundOwner.HitsMax &&
        !Poisoned && !BoundOwner.Poisoned && Spell == null;

    private bool HuntingParticipant(Mobile mobile)
    {
        if (mobile is BaseCreature creature)
        {
            mobile = creature.GetMaster() ?? mobile;
        }
        return mobile != null && (mobile == BoundOwner || mobile == this ||
            CompanionParty.Get(BoundOwner)?.Contains(mobile) == true);
    }

    internal bool CanSkinCorpse(Corpse corpse)
    {
        if (corpse is not { Deleted: false, Animated: false } || corpse.Map != Map ||
            corpse.Owner is not BaseCreature { Hides: > 0, Controlled: false, Summoned: false, IsBonded: false } creature ||
            creature.Body.IsHuman || corpse.IsCriminalAction(BoundOwner))
        {
            return false;
        }
        // Felucca permits looting strangers; still require this hunting group's contribution.
        var earned = HuntingParticipant(corpse.Killer);
        foreach (var attacker in corpse.Aggressors)
        {
            if (HuntingParticipant(attacker)) { earned = true; break; }
        }
        if (!earned) { return false; }
        if (!corpse.Carved) { return true; }
        foreach (var item in corpse.Items)
        {
            if (item is BaseHides or BaseLeather) { return true; }
        }
        return false;
    }

    internal bool SkinCorpse(Corpse corpse)
    {
        if (!CanSkinWhileHunting() || !CanSkinCorpse(corpse) || !InRange(corpse, 2) || !InLOS(corpse) || Backpack == null)
        {
            return false;
        }
        if (!corpse.Carved)
        {
            // Built-in field tools are temporary, never added to loot or saved.
            var knife = new SkinningKnife();
            // The owner earned the loot rights above; harvest on their behalf.
            // Calling the creature's standard carver keeps map bonuses and hide types
            // while avoiding a second player-only rights check against the companion.
            try { ((BaseCreature)corpse.Owner).OnCarve(this, corpse, knife); }
            finally { knife.Delete(); }
        }
        var gathered = false;
        var scissors = new Scissors();
        try
        {
            for (var i = corpse.Items.Count - 1; i >= 0; i--)
            {
                var item = corpse.Items[i];
                if (item is not (BaseHides or BaseLeather) || !Backpack.CheckHold(this, item, false)) { continue; }
                Backpack.DropItem(item);
                if (item is IScissorable hides) { hides.Scissor(this, scissors); }
                gathered = true;
            }
        }
        finally { scissors.Delete(); }
        if (gathered) { PlaySound(0x248); }
        return gathered;
    }

    private bool ThinkSkinning()
    {
        if (BoundOwner?.NetState == null || Core.Now < _nextSkinning || !CanSkinWhileHunting()) { return false; }
        _nextSkinning = Core.Now + TimeSpan.FromSeconds(1);
        // Do not abandon defense when an attacker has not yet become a combatant.
        foreach (var aggression in BoundOwner.Aggressors)
        {
            if (!aggression.Expired && aggression.Attacker is { Deleted: false, Alive: true } attacker &&
                attacker.Map == Map && InRange(attacker, 12)) { return false; }
        }
        foreach (var corpse in Map.GetItemsInRange<Corpse>(Location, 6))
        {
            if (!CanSkinCorpse(corpse) || !BoundOwner.InRange(corpse, 12) || !InLOS(corpse)) { continue; }
            if (InRange(corpse, 2)) { return SkinCorpse(corpse); }
            if (AIObject?.MoveToPoint(corpse.Location, false) == true)
            {
                _nextSkinning = Core.Now;
                return true;
            }
        }
        return false;
    }
}
