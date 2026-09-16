using Server;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype
{
 public sealed class HavenBeaconQuestGump:HavenMenuGump
 {
  readonly Mobile _owner;
  public HavenBeaconQuestGump(Mobile p):base(55,55)
  {
   _owner=p;int phase=HavenBeaconQuest.Phase(p);
   AddBackground(0,0,720,620,3000);AddLabel(24,20,0,"THE BEACON BEYOND | A Light That Answers");
   string[] stages={"Begin the investigation","Read Mara's expedition ledger","Test the bond","Examine the waystone","Wake the beacon","Chapter complete"};
   AddLabel(24,54,0,"Objective: "+stages[System.Math.Max(0,System.Math.Min(5,phase))]);
   string[] text={
    "<B>Jenna:</B> The beacon woke when you arrived. Before we follow it anywhere, let's find out what my expedition was really carrying.<BR><BR>Jenna gives you a leveling cape, a starter horse and its bonding apple when you meet. If your pack or follower slots were full, collect the waiting gifts below.<BR><BR>Start beside the New Haven beacon with Jenna. New arrivals should finish meeting her first. Returning adventurers can join this investigation without replaying their arrival.<BR><BR><B>Reward:</B> 2,500 gold paid into your bank, 20 Haven Marks, an instant bandage-enhancing fountain deed, and the expedition's next clue.",
    "Mara kept the expedition records near the Haven plaza. Find the ledger and double-click it with Jenna beside you.<BR><BR><B>Jenna:</B> Mara wrote everything down. Cargo, weather, who ate the last apple. If the beacon has a history, she'll have left us a way into it.",
    "The ledger's final entry reads: <I>We are not bringing treasure home. We are bringing people. If the route fails, let the tide carry the anchor beneath the star.</I><BR><BR>The beacon's twin marks flare when you and Jenna work together. Test that safely at the Haven practice golem.<BR><BR>Deal 50 total damage using weapons, spells, a pet, or Jenna. Keep Jenna nearby. The golem returns attacks for at most 1 damage and stops before a lethal hit. Use [cc to command Jenna to attack, or use your own weapon or spell. Refresh this journal to check progress.<BR><BR><B>Progress:</B> "+HavenBeaconQuest.Damage(p)+" / 50 damage.",
    "A flash from the practice golem lights a worn mark in the stonework nearby. Follow the direction arrow to the tide-marked waystone and double-click it with Jenna nearby.<BR><BR><B>Jenna:</B> That wasn't a battle signal. It was an answer. Someone built this thing to recognize a partnership.",
    "Three shapes emerge from the waystone: a tide, an anchor, and a star. Below them is a sailor's promise: <I>The tide brings us to the anchor; the star leads us home.</I><BR><BR>Return to the beacon with Jenna and touch the symbols in that order. A mistake only resets the sequence.<BR><BR><B>Symbols answered:</B> "+HavenBeaconQuest.Glyph(p)+" / 3.",
    "The beacon unfolds a map of an island refuge. Beside the cove is Mara's expedition mark, still burning gold.<BR><BR><B>Jenna:</B> That's our old refuge. Someone kept the light on.<BR><BR>For a moment another message appears: <I>Rescue route restored. One arrival confirmed.</I><BR><BR>You were not summoned as a weapon. You were an answer to a call for help.<BR><BR><B>Reward received:</B> 2,500 gold banked and 20 Haven Marks. Claim your expedition fountain deed below. Place it in your house to enhance every deposited bandage immediately.<BR><BR>Your next lead is the island refuge; its story chapter will continue from this discovery."
   };
   AddHtml(24,91,670,280,"<BASEFONT COLOR=#3B2A1A>"+text[System.Math.Max(0,System.Math.Min(5,phase))]+"</BASEFONT>",false,true);
   if(phase==0)FlatButton(24,387,220,1,"Begin with Jenna");
   if(phase!=4)FlatButton(474,387,220,7,"Jenna's field guide");
   if(phase==4){FlatButton(24,387,200,10,"Tide");FlatButton(248,387,200,11,"Anchor");FlatButton(472,387,220,12,"Star");}
   if(phase>=1||HavenArrivalStory.Stage(p)>=2){FlatButton(24,528,205,20,HavenStoryGifts.Claimed(p,"cape")?"Replay cape explanation":"Jenna's leveling cape");FlatButton(249,528,205,21,HavenStoryGifts.Claimed(p,"mount")?"Replay horse explanation":"Horse and bonding apple");}
   if(phase==5)FlatButton(474,528,220,22,HavenStoryGifts.Claimed(p,"fountain")?"Replay fountain explanation":"Claim fountain deed");
   FlatButton(24,430,205,2,"Show direction");FlatButton(249,430,205,3,"Refresh journal");FlatButton(474,430,220,4,"Replay Jenna's line");
   FlatButton(24,476,205,5,HavenBeaconQuest.VoiceEnabled(p)?"Voice: on":"Voice: muted");FlatButton(249,476,205,6,"Read arrival story");FlatButton(474,476,220,0,"Close");
   AddLabel(24,578,0,"[storyquest reopens this journal. Progress is saved per character.");
  }
  public override void OnResponse(NetState state,RelayInfo info)
  {
   if(state.Mobile!=_owner||!HavenMarks.CanUse(_owner)||info.ButtonID==0)return;
   if(info.ButtonID>=20&&info.ButtonID<=22){string gift=info.ButtonID==20?"cape":info.ButtonID==21?"mount":"fountain";if(HavenStoryGifts.Claimed(_owner,gift)){HavenBeaconQuest.Speak(_owner,info.ButtonID-15,true);HavenBeaconQuest.Show(_owner);return;}bool ok=info.ButtonID==20?HavenStoryGifts.ClaimCape(_owner):info.ButtonID==21?HavenStoryGifts.ClaimMount(_owner):HavenStoryGifts.ClaimFountain(_owner);if(!ok)_owner.SendMessage("Reward unavailable: keep Jenna nearby, make pack space, and check whether you already claimed it. Your horse also needs one free follower slot.");HavenBeaconQuest.Show(_owner);return;}
   if(info.ButtonID==1&&!HavenBeaconQuest.Accept(_owner))_owner.SendMessage("Stand beside the beacon with Jenna. Finish the arrival meeting first if you are newly summoned.");
   else if(info.ButtonID==2)HavenBeaconQuest.Guide(_owner);
   else if(info.ButtonID==4)HavenBeaconQuest.Speak(_owner,HavenBeaconQuest.CurrentVoice(_owner),true);
   else if(info.ButtonID==5)HavenBeaconQuest.ToggleVoice(_owner);
   else if(info.ButtonID==6){HavenArrivalStory.Show(_owner);return;}
   else if(info.ButtonID==7){_owner.SendGump(new HavenJennaFieldBriefing(_owner));return;}
   else if(info.ButtonID>=10&&info.ButtonID<=12)HavenBeaconQuest.ChooseGlyph(_owner,info.ButtonID-10);
   HavenBeaconQuest.Show(_owner);
  }
 }
}
