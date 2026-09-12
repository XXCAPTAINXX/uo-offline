using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenOfflineMissionPlan:Item {
  public HavenCompanion Companion;public bool Enabled,Rotate,RecallOnLogin;public int Minutes=5,Cursor;public CompanionMission Focus=CompanionMission.Supply;public bool ActiveTrip;public string Status="Disabled";Timer _timer;
  public HavenOfflineMissionPlan():base(1){Name="Companion offline mission plan";Visible=false;Movable=false;Weight=0;Schedule();}
  public HavenOfflineMissionPlan(Serial s):base(s){}
  public static HavenOfflineMissionPlan Find(HavenCompanion c){return c?.Backpack?.FindItemByType(typeof(HavenOfflineMissionPlan),true) as HavenOfflineMissionPlan;}
  public static HavenOfflineMissionPlan Ensure(HavenCompanion c){var p=Find(c);if(p!=null)return p;p=new HavenOfflineMissionPlan{Companion=c};c.Backpack.DropItem(p);return p;}
  public bool Configure(Mobile owner,CompanionMission kind,int minutes){if(Companion==null||!Companion.IsOwner(owner)||!Companion.CanOpenPack(owner)||kind<CompanionMission.Supply||kind>CompanionMission.AbyssIngredients||(minutes!=5&&minutes!=15&&minutes!=30&&minutes!=60))return false;Focus=kind;Minutes=minutes;Status="Default saved. Enable offline missions to repeat after logout.";return true;}
  void Schedule(){_timer?.Stop();_timer=Timer.DelayCall(TimeSpan.FromSeconds(5),TimeSpan.FromSeconds(5),Tick);}
  public void Tick(){var c=Companion;var owner=c?.BoundOwner;if(c==null||c.Deleted||owner==null||owner.Deleted){Delete();return;}if(Parent!=c.Backpack){Enabled=false;Status="Plan must remain in companion pack.";return;}
   if(owner.NetState!=null){if(ActiveTrip&&c.OnMission&&RecallOnLogin){if(c.Recall(owner)){ActiveTrip=false;Status="Recalled on login; early trip gives no completion rewards.";}else Status="Waiting to recall safely after login.";}else{if(!c.OnMission)ActiveTrip=false;Status=c.OnMission?"Current trip will finish; no new trips while online.":Enabled?"Armed for logout.":"Disabled.";}return;}
   if(c.OnMission){Status=ActiveTrip?"Offline trip running.":"Waiting for your manually started mission to finish.";return;}
   ActiveTrip=false;if(!Enabled){Status="Disabled.";return;}
   int count=(int)CompanionMission.AbyssIngredients+1;string reason=null;
   for(int i=0;i<(Rotate?count:1);i++){var kind=Rotate?(CompanionMission)((Cursor+i)%count):Focus;reason=c.MissionStartError(owner,Minutes,kind,true);if(reason!=null)continue;if(c.StartOfflineMission(Minutes,kind)){ActiveTrip=true;if(Rotate)Cursor=((int)kind+1)%count;Status="Running "+CompanionActivityGump.MissionName(kind)+" for "+Minutes+" minutes.";return;}reason="Reward storage is full; collect pending rewards.";}
   Status=reason??"No eligible mission.";
  }
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Companion);w.Write(Enabled);w.Write(Rotate);w.Write(RecallOnLogin);w.Write(Minutes);w.Write(Cursor);w.Write((int)Focus);w.Write(ActiveTrip);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Companion=r.ReadMobile() as HavenCompanion;Enabled=r.ReadBool();Rotate=r.ReadBool();RecallOnLogin=r.ReadBool();Minutes=r.ReadInt();Cursor=r.ReadInt();Focus=(CompanionMission)r.ReadInt();ActiveTrip=r.ReadBool();if(Minutes!=5&&Minutes!=15&&Minutes!=30&&Minutes!=60){Minutes=5;Enabled=false;}if(Focus<0||Focus>CompanionMission.AbyssIngredients){Focus=0;Enabled=false;}Cursor=Math.Max(0,Cursor)%((int)CompanionMission.AbyssIngredients+1);Schedule();}
  public override void OnDelete(){_timer?.Stop();_timer=null;base.OnDelete();}
 }
 public class HavenOfflineMissionGump:HavenMenuGump {
  readonly HavenOfflineMissionPlan _plan; readonly CompanionMission? _candidate; readonly int _duration;
  public HavenOfflineMissionGump(HavenOfflineMissionPlan p,CompanionMission? candidate=null,int duration=5):base(80,80){_plan=p;_candidate=candidate??p.Focus;_duration=candidate.HasValue?duration:p.Minutes;AddBackground(0,0,560,550,0xA28);Text(24,20,510,30,"Offline companion missions");Text(24,59,510,50,"Default: "+CompanionActivityGump.MissionName(p.Focus)+" — "+p.Minutes+" minutes.<BR>Choose and save your route and duration below.");Button(24,121,1,p.Enabled?"Offline missions: ON":"Offline missions: OFF");Button(24,166,2,p.Rotate?"Route: cycle eligible missions":"Route: repeat saved default");Button(24,211,3,p.RecallOnLogin?"On login: recall early":"On login: finish current trip");Text(24,252,508,102,"Trips keep their normal skill and combat requirements. Rewards stay in the companion pack / ledger. No backdated extra trips while the server is stopped.<BR>"+p.Status);Text(24,352,510,40,"Selected: "+CompanionActivityGump.MissionName(_candidate.Value));Button(24,394,6,"Choose mission");int[] choices={5,15,30,60};for(int i=0;i<4;i++){int value=choices[i];Button(24+i*126,432,20+i,(_duration==value?"[":"")+value+" min"+(_duration==value?"]":""));}Button(24,475,5,"Save mission and duration");Button(24,513,4,"Refresh");Button(360,513,0,"Back");}
  void Text(int x,int y,int w,int h,string s){AddHtml(x,y,w,h,"<BASEFONT COLOR=#171511>"+s+"</BASEFONT>",false,false);}
  void Button(int x,int y,int id,string label){FlatButton(x,y,id>=20&&id<=23?106:id==0||id==4?145:480,id,label);}
  public override void OnResponse(NetState state,RelayInfo info){var c=_plan.Companion;var owner=state.Mobile;if(_plan.Deleted||c==null||!c.IsOwner(owner))return;if(info.ButtonID==0){if(_candidate.HasValue){var kind=_candidate.Value;int tab=HavenPetMissions.Valid(kind)?1:0;int selection=tab==1?(int)kind-6:HavenRegionalMissions.Valid(kind)?6:Array.IndexOf(new[]{CompanionMission.Supply,CompanionMission.Mining,CompanionMission.Lumber,CompanionMission.Leather,CompanionMission.Malas,CompanionMission.Abyss},kind);if(HavenRegionalMissions.Valid(kind))selection+=Array.IndexOf(HavenRegionalMissions.Kinds,kind);owner.SendGump(new CompanionActivityGump(c,tab,selection,_duration));}else c.Show(owner,true);return;}if(info.ButtonID==6){owner.SendGump(new HavenOfflineRoutePicker(_plan,_candidate.Value,_duration));return;}int chosenMinutes=info.ButtonID==20?5:info.ButtonID==21?15:info.ButtonID==22?30:info.ButtonID==23?60:_duration;if(info.ButtonID==5&&_candidate.HasValue&&!_plan.Configure(owner,_candidate.Value,_duration))owner.SendMessage("Stand beside your recalled companion to save this default.");if(info.ButtonID==1)_plan.Enabled=!_plan.Enabled;if(info.ButtonID==2)_plan.Rotate=!_plan.Rotate;if(info.ButtonID==3)_plan.RecallOnLogin=!_plan.RecallOnLogin;owner.CloseGump(typeof(HavenOfflineMissionGump));owner.SendGump(new HavenOfflineMissionGump(_plan,_candidate,chosenMinutes));}
 }
 public class HavenOfflineRoutePicker:HavenMenuGump {
  readonly HavenOfflineMissionPlan _plan;readonly CompanionMission _selected;readonly int _minutes,_page;
  public HavenOfflineRoutePicker(HavenOfflineMissionPlan plan,CompanionMission selected,int minutes,int page=0):base(80,80){
   _plan=plan;_selected=selected;_minutes=minutes;_page=Math.Max(0,Math.Min(2,page));
   AddBackground(0,0,560,450,3000);AddLabel(24,20,0,"Choose an offline mission");
   for(int row=0;row<8;row++){int index=_page*8+row;if(index>(int)CompanionMission.AbyssIngredients)break;var kind=(CompanionMission)index;Button(24,62+row*39,100+index,CompanionActivityGump.MissionName(kind)+(kind==selected?" [selected]":""));}
   if(_page>0)Button(24,404,1,"Previous");AddLabel(194,404,0,"Page "+(_page+1)+" / 3");if(_page<2)Button(304,404,2,"Next");Button(420,404,0,"Back");
  }
  void Button(int x,int y,int id,string label){FlatButton(x,y,id>=100?490:95,id,label);}
  public override void OnResponse(NetState state,RelayInfo info){if(_plan.Deleted||_plan.Companion==null||!_plan.Companion.IsOwner(state.Mobile))return;int id=info.ButtonID;if(id==1||id==2){state.Mobile.SendGump(new HavenOfflineRoutePicker(_plan,_selected,_minutes,_page+(id==1?-1:1)));return;}if(id==0||(id>=100&&id<=100+(int)CompanionMission.AbyssIngredients))state.Mobile.SendGump(new HavenOfflineMissionGump(_plan,id==0?_selected:(CompanionMission)(id-100),_minutes));}
 }

}
