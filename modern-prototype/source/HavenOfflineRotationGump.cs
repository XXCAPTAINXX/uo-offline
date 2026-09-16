using System;
using Server.Network;
namespace Server.HavenPrototype
{
 public class HavenOfflineRotationGump:HavenStoneGump
 {
  readonly HavenOfflineMissionPlan Plan;readonly int Page;
  public static string Availability(HavenCompanion c,CompanionMission k){if(HavenPetMissions.Valid(k))return HavenPetMissions.CanStart(c,k)?"Skills ready":"Needs taming/lore "+HavenPetMissions.Requirements[(int)k-6];if(HavenRegionalMissions.Valid(k))return HavenRegionalMissions.CanStart(c,k)?"Skills ready":(k==CompanionMission.DoomRecon?"Combat ":"Combat/resist ")+HavenRegionalMissions.Requirement(k);if(k==CompanionMission.Malas||k==CompanionMission.Abyss)return Math.Max(c.Skills.Magery.Base,c.Skills.Tactics.Base)>=(k==CompanionMission.Malas?60:80)?"Skills ready":"Needs Magery/Tactics "+(k==CompanionMission.Malas?60:80);return "Skills ready";}
  public HavenOfflineRotationGump(HavenOfflineMissionPlan plan,int page=0):base(70,70){Plan=plan;Page=Math.Max(0,Math.Min(3,page));AddBackground(0,0,720,525,0xA28);AddLabel(24,20,0,"OFFLINE ROTATION | Choose the missions Jenna repeats");AddHtml(24,53,670,48,"<BASEFONT COLOR=#3B2A1A>Selected routes run in order when their requirements are met. Unselected routes are skipped. Changes apply to the next trip.</BASEFONT>",false,false);for(int row=0;row<7;row++){int i=Page*7+row;if(i>(int)CompanionMission.DoomRecon)break;var k=(CompanionMission)i;FlatButton(24,118+row*44,460,100+i,(plan.Includes(k)?"[On] ":"[Off] ")+CompanionActivityGump.MissionName(k));AddLabelCropped(500,118+row*44,190,28,0,Availability(plan.Companion,k));}if(Page>0)FlatButton(24,450,130,1,"Previous");if(Page<3)FlatButton(170,450,130,2,"Next");FlatButton(320,450,110,3,"Select all");FlatButton(440,450,110,4,"Clear all");FlatButton(565,450,130,0,"Back");}
  public override void OnResponse(NetState state,Server.Gumps.RelayInfo info){if(Plan.Deleted||Plan.Companion==null||!Plan.Companion.IsOwner(state.Mobile))return;int id=info.ButtonID;if(id==0){state.Mobile.SendGump(new HavenOfflineMissionGump(Plan));return;}if(id==3)Plan.RouteMask=(1<<((int)CompanionMission.DoomRecon+1))-1;if(id==4)Plan.RouteMask=0;if(id>=100&&id<=100+(int)CompanionMission.DoomRecon)Plan.ToggleRoute(state.Mobile,(CompanionMission)(id-100));state.Mobile.SendGump(new HavenOfflineRotationGump(Plan,Page+(id==1?-1:id==2?1:0)));}
 }
}
