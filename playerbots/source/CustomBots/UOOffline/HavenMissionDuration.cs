using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.UOOffline;

public static class HavenMissionDuration
{
    internal static readonly int[] Choices = [5, 15, 30, 60];
    internal static bool Valid(int minutes) => minutes is 5 or 15 or 30 or 60;
    internal static int BonusPercent(int minutes) => minutes switch { 15 => 110, 30 => 115, 60 => 125, _ => 100 };
    internal static int Selected(HavenCompanion companion)
    {
        var value=companion?.Backpack?.FindItemByType<HavenMissionPlan>()?.Minutes ?? 5;
        return Valid(value) ? value : 5;
    }
    internal static void Select(HavenCompanion companion,int minutes)
    {
        if(!Valid(minutes) || companion?.Backpack==null) { return; }
        var plan=companion.Backpack.FindItemByType<HavenMissionPlan>();
        if(plan==null) { plan=new HavenMissionPlan(); companion.Backpack.DropItem(plan); }
        plan.Minutes=minutes;
    }
    internal static int Scale(int value,int percent) => (int)((long)value*percent/100);
    internal static int SearchRolls(int minutes,int percent) => Math.Clamp(Scale(minutes,percent)/5,1,15);
}

// Separate records preserve the binary layouts of missions already in saved worlds.
[SerializationGenerator(0)]
public partial class HavenMissionPlan : Item
{
    [SerializableField(0)] private int _minutes=5;
    [Constructible] public HavenMissionPlan():base(1) { Name="mission duration preference"; Visible=false; Movable=false; Weight=0; }
    public override bool IsVirtualItem=>true;
}
[SerializationGenerator(0)]
public partial class HavenMissionContract : Item
{
    [SerializableField(0)] private int _minutes=5;
    [Constructible] public HavenMissionContract():base(1) { Name="mission duration"; Visible=false; Movable=false; Weight=0; }
    public override bool IsVirtualItem=>true;
}

public sealed class HavenMissionDurationGump : Gump
{
    private readonly HavenCompanion _companion; private readonly HavenExpeditionKind _kind;
    public HavenMissionDurationGump(HavenCompanion companion,HavenExpeditionKind kind):base(80,80)
    {
        _companion=companion; _kind=kind;
        AddBackground(0,0,490,365,9270); AddLabel(24,20,1152,HavenRegionalMissions.Name(kind));
        AddHtml(24,52,440,60,"<BASEFONT COLOR=#FFFFFF>Longer trips earn more loot and training. Finish the full trip for its bonus. Returning early earns normal rewards for time spent; pets require completion.</BASEFONT>");
        var taming=HavenTamingMissions.IsTaming(kind);
        for(var i=0;i<HavenMissionDuration.Choices.Length;i++)
        {
            var minutes=HavenMissionDuration.Choices[i]; var bonus=HavenMissionDuration.BonusPercent(minutes); var y=126+i*43;
            AddButton(24,y,4005,4007,minutes); AddLabel(64,y,1152,$"Send for {minutes} minutes");
            AddLabel(278,y,2101,$"+{bonus-100}% completion bonus");
            if(taming) { AddLabel(64,y+20,2101,$"One pet; {HavenMissionDuration.SearchRolls(minutes,bonus)} search / supply rolls"); }
            else { AddLabel(64,y+20,2101,$"{minutes*bonus/500.0:F1}x the five-minute loot; extra skill, stat and gear training"); }
        }
        AddLabel(24,306,2101,$"Current AFK trip length: {HavenMissionDuration.Selected(companion)} minutes");
        AddButton(350,332,4017,4019,0); AddLabel(390,332,1152,"Cancel");
    }
    public override void OnResponse(NetState state,in RelayInfo info)
    {
        if(info.ButtonID==0 || _companion.Deleted || _companion.BoundOwner!=state.Mobile) { return; }
        if(HavenCompanionExpedition.Start(_companion,state.Mobile,_kind,info.ButtonID)) { return; }
        state.Mobile.SendMessage("Your companion must be nearby, available and meet the mission's skills.");
        HavenCompanionGump.DisplayTo(state.Mobile,_companion,4);
    }
}
