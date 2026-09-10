using System;
using Server.Mobiles;

namespace Server.UOOffline;

public partial class HavenCompanion
{
    private Mobile _explicitWildTarget;
    private bool _checkingExplicitAttack;
    internal static bool WildCustomTameable(Mobile target) => target is BaseCreature { Deleted:false, Alive:true, Tamable:true, Controlled:false, Summoned:false } pet && HavenTamingMissions.IsCustomPet(pet);
    internal bool MayHarmWildPet(Mobile target) => !WildCustomTameable(target) || _checkingExplicitAttack || _calmingAnimal ||
        _explicitWildTarget == target && ControlOrder == OrderType.Attack && ControlTarget == target;
    internal bool OrderAttack(Mobile owner, Mobile target)
    {
        if (Deleted || IsDeadPet || !Alive || !Controlled || owner != BoundOwner || !owner.Alive || owner.Map != Map || !owner.InRange(this,14) ||
            target?.Deleted != false || !target.Alive || target.Map != Map || !InRange(target,14) || !CanSee(target) || !InLOS(target)) { return false; }
        _checkingExplicitAttack=true;
        try { if (!CanBeHarmful(target,false)) { return false; } }
        finally { _checkingExplicitAttack=false; }
        StopTamingAssist(); _explicitWildTarget=WildCustomTameable(target) ? target : null;
        ControlTarget=target; ControlOrder=OrderType.Attack; Combatant=target; FocusMob=target;
        if (AIObject != null) { AIObject.Action=ActionType.Combat; }
        return true;
    }
    internal void ClearExplicitAttack() => _explicitWildTarget=null;
    private void RespectWildPets()
    {
        if (ControlOrder != OrderType.Attack || ControlTarget != _explicitWildTarget || _explicitWildTarget?.Deleted == true) { _explicitWildTarget=null; }
        if (Combatant is Mobile target && !MayHarmWildPet(target) || ControlTarget is Mobile control && !MayHarmWildPet(control))
        { Combatant=null; FocusMob=null; ControlTarget=BoundOwner; ControlOrder=OrderType.Guard; }
    }
    public override bool HandlesOnSpeech(Mobile from) => from == BoundOwner && from.Alive && from.Map == Map && from.InRange(this,14) || base.HandlesOnSpeech(from);
    public override void OnSpeech(SpeechEventArgs e)
    {
        if (e.Mobile == BoundOwner && Controlled && e.Mobile.Alive && e.Mobile.Map == Map && e.Mobile.InRange(this,14))
        {
            var speech=e.Speech.Trim(); string command=null;
            if (speech.StartsWith("all ",StringComparison.OrdinalIgnoreCase)) { command=speech[4..]; }
            else if (!string.IsNullOrEmpty(Name) && speech.StartsWith(Name+" ",StringComparison.OrdinalIgnoreCase)) { command=speech[(Name.Length+1)..]; }
            else if (!string.IsNullOrEmpty(Name))
            { var first=Name.Split(' ')[0]; if (speech.StartsWith(first+" ",StringComparison.OrdinalIgnoreCase)) { command=speech[(first.Length+1)..]; } }
            var keyword=command?.Trim().ToLowerInvariant() switch {
                "come" => 0x164, "follow" => 0x165, "guard" or "guard me" => 0x16B,
                "stop" => 0x167, "kill" => 0x168, "attack" => 0x169, "follow me" => 0x16C, "stay" => 0x170, _ => -1 };
            if (keyword>=0)
            {
                if (keyword is not (0x168 or 0x169)) { _explicitWildTarget=null; StopTamingAssist(); }
                AIObject?.AllOnSpeechPet(new SpeechEventArgs(e.Mobile,e.Speech,e.Type,e.Hue,new[]{keyword}));
                return;
            }
        }
        base.OnSpeech(e);
    }
}
