using System;
using Server.Gumps;
using Server.Network;

namespace Server.HavenPrototype
{
    public class CompanionMissionTimerGump : Gump
    {
        private readonly HavenCompanion _companion;
        public static string Remaining(HavenCompanion companion)
        {
            if(!companion.OnMission) return companion.Map == Map.Internal ? "Waiting to return" : "Mission complete";
            int seconds=Math.Max(0,(int)Math.Ceiling((companion.MissionDue-DateTime.UtcNow).TotalSeconds));
            return companion.MissionKind + "  " + (seconds/60).ToString("00") + ":" + (seconds%60).ToString("00");
        }
        public CompanionMissionTimerGump(HavenCompanion companion,Mobile owner):base(35,35)
        {
            _companion=companion;
            AddBackground(0,0,220,64,0x13BE);
            AddLabel(12,8,1152,Remaining(companion));
            AddButton(12,35,0xFA5,0xFA7,1,GumpButtonType.Reply,0);AddLabel(46,35,1152,"Open");
            AddButton(112,35,0xFA5,0xFA7,2,GumpButtonType.Reply,0);AddLabel(146,35,1152,"Recall now");
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
            _companion.Show(owner,true);
        }
    }
}
