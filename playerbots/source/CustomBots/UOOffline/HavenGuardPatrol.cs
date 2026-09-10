using System;
using System.Runtime.CompilerServices;
using Server.Mobiles;
using Server.Regions;

namespace Server.UOOffline;

public static class HavenGuardPatrol
{
    internal const int SearchRange = 8;
    internal const int LeashRange = 12;
    private sealed class PatrolState { internal Serial Target; internal DateTime NextSearch; }
    private static readonly ConditionalWeakTable<BaseCreature, PatrolState> States = new();

    public static void Clear(BaseCreature pet)
    { if (States.TryGetValue(pet, out var state)) { state.Target = Serial.Zero; } }

    internal static bool Available(BaseCreature pet) => pet is { Deleted: false, Alive: true, Controlled: true, IsDeadPet: false } &&
        pet.ControlMaster is { Deleted: false, Alive: true, Hidden: false, Player: true } owner &&
        owner.Map != null && owner.Map != Map.Internal && pet.Map == owner.Map && pet.InRange(owner, LeashRange) &&
        pet is not BaseMount { Rider: not null } && pet is not HavenCompanion { TamingAssistActive: true } &&
        !owner.Region.IsPartOf<GuardedRegion>();

    internal static bool Hostile(BaseCreature pet, BaseCreature enemy, int range)
    {
        var owner = pet.ControlMaster;
        if (owner?.Deleted != false) { return false; }
        if (enemy is not { Deleted: false, Alive: true, IsDeadPet: false, Controlled: false, Summoned: false,
                Blessed: false, Hidden: false, BardPacified: false, IsInvulnerable: false, InitialInnocent: false } ||
            enemy is BaseVendor || enemy is HavenTrainingSentinel || enemy.Owners.Count != 0 ||
            enemy.Map != owner.Map || !owner.InRange(enemy, range) || !pet.InRange(enemy, LeashRange) ||
            Server.SkillHandlers.AnimalTaming.IsBeingTamed(enemy) ||
            !owner.CanSee(enemy) || !pet.CanSee(enemy) || !owner.InLOS(enemy) || !pet.InLOS(enemy) ||
            !owner.CanBeHarmful(enemy, false) || !pet.CanBeHarmful(enemy, false) || !enemy.IsEnemy(owner)) { return false; }
        return enemy.Combatant == owner || enemy.Combatant == pet ||
            enemy.FightMode is FightMode.Closest or FightMode.Strongest or FightMode.Weakest ||
            enemy.FightMode == FightMode.Evil && owner.Karma < 0;
    }

    internal static BaseCreature Closest(BaseCreature pet)
    {
        if (!Available(pet)) { return null; }
        BaseCreature result = null;
        var distance = double.MaxValue;
        var owner = pet.ControlMaster;
        foreach (var enemy in owner.Map.GetMobilesInRange<BaseCreature>(owner.Location, SearchRange))
        {
            if (!Hostile(pet, enemy, SearchRange)) { continue; }
            var current = owner.GetDistanceToSqrt(enemy);
            if (current < distance || current == distance && (result == null || enemy.Serial.Value < result.Serial.Value))
            { result = enemy; distance = current; }
        }
        return result;
    }

    public static bool TryAcquire(BaseCreature pet)
    {
        if (pet.ControlOrder != OrderType.Guard || !Available(pet)) { return false; }
        var state = States.GetOrCreateValue(pet);
        if (Core.Now < state.NextSearch) { return false; }
        state.NextSearch = Core.Now + TimeSpan.FromSeconds(1);
        var enemy = Closest(pet);
        if (enemy == null) { return false; }
        pet.ControlTarget = enemy;
        pet.ControlOrder = OrderType.Attack;
        pet.Combatant = enemy;
        state.Target = enemy.Serial;
        return true;
    }

    public static bool ReturnIfOutOfRange(BaseCreature pet)
    {
        if (!States.TryGetValue(pet, out var state) || state.Target == Serial.Zero) { return false; }
        if (pet.ControlOrder != OrderType.Attack || pet.ControlTarget?.Serial != state.Target)
        { state.Target = Serial.Zero; return false; }
        if (Available(pet) && pet.ControlTarget is BaseCreature enemy && Hostile(pet, enemy, LeashRange)) { return false; }
        state.Target = Serial.Zero;
        pet.Combatant = null; pet.FocusMob = null;
        pet.ControlTarget = pet.ControlMaster;
        pet.ControlOrder = OrderType.Guard;
        return true;
    }
}
