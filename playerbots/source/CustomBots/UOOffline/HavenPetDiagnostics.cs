using System;
using System.Collections.Generic;
using Server.Mobiles;

namespace Server.UOOffline;

// Read only, on the game loop, and limited to the account character's follower list.
internal static class HavenPetDiagnostics
{
    internal static List<object> Snapshot(PlayerMobile owner)
    {
        var result = new List<object>();
        foreach (var follower in owner.AllFollowers)
        {
            if (follower is not BaseCreature { Deleted: false } pet) { continue; }
            result.Add(new
            {
                Serial = pet.Serial.Value, pet.Name, Type = pet.GetType().Name,
                OwnerSerial = pet.ControlMaster?.Serial.Value, pet.Controlled, pet.Summoned,
                Map = pet.Map?.Name, pet.X, pet.Y, pet.Z,
                Distance = pet.Map == owner.Map ? pet.GetDistanceToSqrt(owner) : (double?)null,
                Order = pet.ControlOrder.ToString(), Target = Describe(pet.ControlTarget), Combatant = Describe(pet.Combatant),
                Brain = pet.AI.ToString(), Action = pet.AIObject?.Action.ToString(),
                Thinking = pet.AIObject?.AITimer.Running, pet.Warmode,
                pet.Alive, pet.IsDeadPet, pet.IsStabled, pet.Frozen, pet.Paralyzed, pet.Blessed,
                RiderSerial = (pet as BaseMount)?.Rider?.Serial.Value,
                pet.BardPacified, pet.BardProvoked, pet.BardEndTime,
                pet.Loyalty, pet.MinTameSkill, ControlChance = pet.GetControlChance(owner),
                pet.Hits, pet.HitsMax, pet.Stam, pet.StamMax, pet.Mana, pet.ManaMax,
                SwingDelayMs = Math.Max(0, pet.NextCombatTime - Core.TickCount),
                CanSeeTarget = pet.ControlTarget != null && pet.CanSee(pet.ControlTarget),
                TargetInLOS = pet.ControlTarget != null && pet.InLOS(pet.ControlTarget),
                CanHarmTarget = pet.ControlTarget != null && pet.CanBeHarmful(pet.ControlTarget, false)
            });
        }
        return result;
    }

    private static object Describe(Mobile target) => target == null ? null : new
    {
        Serial = target.Serial.Value, target.Name, Type = target.GetType().Name,
        Map = target.Map?.Name, target.X, target.Y, target.Z,
        target.Alive, target.Deleted, target.Hidden,
        OwnerSerial = (target as BaseCreature)?.ControlMaster?.Serial.Value
    };
}
