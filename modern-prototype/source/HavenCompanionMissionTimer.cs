using System;
using System.Linq;
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
        public static void Initialize(){Timer.DelayCall(TimeSpan.FromSeconds(2),TimeSpan.FromSeconds(1),Maintain);}
        private static void Maintain(){if(!HavenPreview.Enabled)return;foreach(var c in World.Mobiles.Values.OfType<HavenCompanion>().ToArray()){var p=c.BoundOwner;if(p==null||p.NetState==null)continue;if(c.OnMission){if(!p.HasGump(typeof(CompanionMissionTimerGump)))p.SendGump(new CompanionMissionTimerGump(c,p));}else if(p.HasGump(typeof(CompanionMissionTimerGump)))p.CloseGump(typeof(CompanionMissionTimerGump));}}

        public static string Remaining(HavenCompanion companion)
        {
            if(!companion.OnMission) return companion.Map == Map.Internal ? "Waiting to return" : "Mission complete";
            int seconds=Math.Max(0,(int)Math.Ceiling((companion.MissionDue-DateTime.UtcNow).TotalSeconds));
            return CompanionActivityGump.MissionName(companion.MissionKind) + "  " + (seconds/60).ToString("00") + ":" + (seconds%60).ToString("00");
        }
        public CompanionMissionTimerGump(HavenCompanion companion,Mobile owner):base(35,35)
        {
            _companion=companion;
            Closable=false;
            AddBackground(0,0,260,76,0xA28);
            AddLabelCropped(14,11,164,22,0,CompanionActivityGump.MissionName(companion.MissionKind));
            int seconds=Math.Max(0,(int)Math.Ceiling((companion.MissionDue-DateTime.UtcNow).TotalSeconds));
            AddLabelCropped(192,11,56,22,0,companion.OnMission?(seconds/60).ToString("00")+":"+(seconds%60).ToString("00"):"Done");
            FlatButton(14,43,108,1,"Missions");
            FlatButton(138,43,108,2,"Recall now");
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
            if(info.ButtonID==2 && !_companion.Recall(owner)) owner.SendMessage("Cannot recall: your current location is unavailable.");
            owner.SendGump(new CompanionActivityGump(_companion));
        }
    }
}
