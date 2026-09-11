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
  readonly HavenOfflineMissionPlan _plan;
  public HavenOfflineMissionGump(HavenOfflineMissionPlan p):base(80,80){_plan=p;AddBackground(0,0,560,410,0xA28);Text(24,20,510,30,"<B>Offline companion missions</B>");Text(24,59,510,50,"Default: "+CompanionActivityGump.MissionName(p.Focus)+" — "+p.Minutes+" minutes.<BR>Save a different default from Missions.");Button(24,121,1,p.Enabled?"Offline missions: ON":"Offline missions: OFF");Button(24,166,2,p.Rotate?"Route: cycle eligible missions":"Route: repeat saved default");Button(24,211,3,p.RecallOnLogin?"On login: recall early":"On login: finish current trip");Text(24,252,508,102,"Trips keep their normal skill and combat requirements. Rewards stay in the companion pack / ledger. No backdated extra trips while the server is stopped.<BR>"+p.Status);Button(24,370,4,"Refresh");Button(360,370,0,"Back");}
  void Text(int x,int y,int w,int h,string s){AddHtml(x,y,w,h,"<BASEFONT COLOR=#342B23>"+s+"</BASEFONT>",false,false);}
  void Button(int x,int y,int id,string label){AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);Text(x+34,y,450,30,label);}
  public override void OnResponse(NetState state,RelayInfo info){var c=_plan.Companion;var owner=state.Mobile;if(_plan.Deleted||c==null||!c.IsOwner(owner))return;if(info.ButtonID==0){c.Show(owner,true);return;}if(info.ButtonID==1)_plan.Enabled=!_plan.Enabled;if(info.ButtonID==2)_plan.Rotate=!_plan.Rotate;if(info.ButtonID==3)_plan.RecallOnLogin=!_plan.RecallOnLogin;owner.CloseGump(typeof(HavenOfflineMissionGump));owner.SendGump(new HavenOfflineMissionGump(_plan));}
 }
}
