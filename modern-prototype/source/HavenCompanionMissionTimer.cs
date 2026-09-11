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
            if(!companion.OnMission) return "Ready to return";
            int seconds=Math.Max(0,(int)Math.Ceiling((companion.MissionDue-DateTime.UtcNow).TotalSeconds));
            return companion.MissionKind + "  " + (seconds/60).ToString("00") + ":" + (seconds%60).ToString("00");
        }
        public CompanionMissionTimerGump(HavenCompanion companion,Mobile owner):base(35,35)
        {
            _companion=companion;
            AddBackground(0,0,300,120,0xA28);
            AddLabel(16,12,0,companion.Name);
            AddLabel(16,39,0,Remaining(companion));
            AddButton(16,77,0xFA5,0xFA7,1,GumpButtonType.Reply,0);AddLabel(50,77,0,"Open");
            if(!companion.OnMission) {AddButton(140,77,0xFA5,0xFA7,2,GumpButtonType.Reply,0);AddLabel(174,77,0,"Recall");}
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
            if(info.ButtonID==2 && !_companion.Recall(owner)) owner.SendMessage("Cannot recall yet: finish the mission and leave combat first.");
            _companion.Show(owner,true);
        }
    }
}
