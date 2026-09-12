using System;
using Server.Gumps;
using Server.Network;

namespace Server.HavenPrototype
{
    public partial class HavenCompanion
    {
        public bool ShowAwayTimer(Mobile owner)
        {
            if (!OnMission || !IsOwner(owner)) return false;
            owner.CloseGump(typeof(CompanionMissionTimerGump));
            owner.SendGump(new CompanionMissionTimerGump(this,owner));
            return true;
        }
    }
    public class CompanionMissionTimerGump : HavenMenuGump
    {
        private readonly HavenCompanion _companion;
        public static string Remaining(HavenCompanion companion)
        {
            if(!companion.OnMission) return companion.Map == Map.Internal ? "Waiting to return" : "Mission complete";
            int seconds=Math.Max(0,(int)Math.Ceiling((companion.MissionDue-DateTime.UtcNow).TotalSeconds));
            return CompanionActivityGump.MissionName(companion.MissionKind) + "  " + (seconds/60).ToString("00") + ":" + (seconds%60).ToString("00");
        }
        public CompanionMissionTimerGump(HavenCompanion companion,Mobile owner):base(35,35)
        {
            _companion=companion;
            AddBackground(0,0,310,104,0xA28);
            AddLabelCropped(20,19,190,25,0,CompanionActivityGump.MissionName(companion.MissionKind));
            int seconds=Math.Max(0,(int)Math.Ceiling((companion.MissionDue-DateTime.UtcNow).TotalSeconds));
            AddLabelCropped(220,19,70,25,0,companion.OnMission?(seconds/60).ToString("00")+":"+(seconds%60).ToString("00"):"Done");
            FlatButton(20,61,120,1,"Missions");
            FlatButton(170,61,120,2,"Recall now");
            // One refresh only while this exact panel remains open. Closing/expanding stops updates.
            if(companion.OnMission && owner.NetState!=null) Timer.DelayCall(TimeSpan.FromSeconds(1),()=> {
                if(!companion.IsOwner(owner) || owner.NetState==null || !owner.NetState.Gumps.Contains(this)) return;
                owner.CloseGump(typeof(CompanionMissionTimerGump));
                owner.SendGump(new CompanionMissionTimerGump(companion,owner));
            });
        }
        public override void OnResponse(NetState sender,RelayInfo info)
        {
            var owner=sender.Mobile;
            if(!_companion.IsOwner(owner) || info.ButtonID==0) return;
            if(info.ButtonID==2 && !_companion.Recall(owner)) owner.SendMessage("Cannot recall: leave combat and make sure you and your companion are alive.");
            owner.SendGump(new CompanionActivityGump(_companion));
        }
    }
}
