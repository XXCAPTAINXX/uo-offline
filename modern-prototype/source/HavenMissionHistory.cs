using System;
using System.Collections.Generic;
using System.Linq;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype
{
 public sealed class HavenMissionHistory : Item
 {
  static readonly Dictionary<int,HavenMissionHistory> Records=new Dictionary<int,HavenMissionHistory>();
  public HavenCompanion Companion;public readonly List<string> Entries=new List<string>();
  public HavenMissionHistory(HavenCompanion c):base(1){Companion=c;Visible=false;Movable=false;Internalize();Records[c.Serial.Value]=this;}
  public HavenMissionHistory(Serial serial):base(serial){}
  public static HavenMissionHistory Find(HavenCompanion c){HavenMissionHistory h;return c!=null&&Records.TryGetValue(c.Serial.Value,out h)&&!h.Deleted?h:null;}
  public static void Record(HavenCompanion c,string report){if(c==null||c.Deleted)return;var h=Find(c)??new HavenMissionHistory(c);h.Entries.Insert(0,DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'")+" | "+CompanionActivityGump.MissionName(c.MissionKind)+": "+report);if(h.Entries.Count>20)h.Entries.RemoveRange(20,h.Entries.Count-20);}
  public static void Show(HavenCompanion c,Mobile owner){if(c!=null&&c.IsOwner(owner)){owner.CloseGump(typeof(HistoryGump));owner.SendGump(new HistoryGump(c));}}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Companion);w.Write(Entries.Count);foreach(var e in Entries)w.Write(e);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Companion=r.ReadMobile() as HavenCompanion;int count=r.ReadInt();for(int i=0;i<count;i++){var e=r.ReadString();if(i<20)Entries.Add(e);}if(Companion!=null)Records[Companion.Serial.Value]=this;}
  public override void OnDelete(){if(Companion!=null&&Find(Companion)==this)Records.Remove(Companion.Serial.Value);base.OnDelete();}
  public static void Initialize(){Timer.DelayCall(TimeSpan.FromMinutes(1),TimeSpan.FromMinutes(1),()=>{foreach(var h in Records.Values.ToArray())if(h.Companion==null||h.Companion.Deleted)h.Delete();});}
  sealed class HistoryGump:HavenStoneGump
  {
   readonly HavenCompanion C;readonly int Page;
   public HistoryGump(HavenCompanion c,int page=0):base(45,45){C=c;var h=Find(c);var list=h==null?new List<string>():h.Entries;int pages=Math.Max(1,(list.Count+4)/5);Page=Math.Max(0,Math.Min(pages-1,page));AddBackground(0,0,740,540,0xA28);AddLabel(24,20,0,"MISSION HISTORY | Most recent 20 trips");if(list.Count==0)AddLabel(24,80,0,"Completed and recalled missions will appear here.");for(int row=0;row<5&&Page*5+row<list.Count;row++){string s=list[Page*5+row].Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;");AddHtml(24,65+row*78,685,72,"<BASEFONT COLOR=#3B2A1A>"+s+"</BASEFONT>",false,true);}if(Page>0)FlatButton(24,475,140,1,"Previous");AddLabel(220,475,0,"Page "+(Page+1)+" / "+pages);if(Page+1<pages)FlatButton(360,475,140,2,"Next");FlatButton(550,475,155,0,"Back to missions");}
   public override void OnResponse(NetState state,RelayInfo info){if(!C.IsOwner(state.Mobile))return;if(info.ButtonID==0)state.Mobile.SendGump(new CompanionActivityGump(C));else if(info.ButtonID==1||info.ButtonID==2)state.Mobile.SendGump(new HistoryGump(C,Page+(info.ButtonID==1?-1:1)));}
  }
 }
 public sealed class HavenMissionRecallGump:HavenStoneGump
 {
  readonly HavenCompanion C;readonly DateTime Due;
  public static void Show(HavenCompanion c,Mobile owner){if(!c.IsOwner(owner))return;if(!c.OnMission){c.Recall(owner);return;}owner.CloseGump(typeof(HavenMissionRecallGump));owner.SendGump(new HavenMissionRecallGump(c));}
  public HavenMissionRecallGump(HavenCompanion c):base(80,80){C=c;Due=c.MissionDue;AddBackground(0,0,500,225,0xA28);AddLabel(24,20,0,"RECALL THIS MISSION?");AddLabelCropped(24,57,450,24,0,CompanionActivityGump.MissionName(c.MissionKind));AddHtml(24,90,450,65,"<BASEFONT COLOR=#3B2A1A>Returning before completion forfeits this trip's rewards and training. Keep the mission running or recall now.</BASEFONT>",false,false);FlatButton(24,173,215,0,"Keep mission running");FlatButton(260,173,215,1,"Recall and forfeit rewards");}
  public static bool Confirm(HavenCompanion c,Mobile owner,DateTime due){return c!=null&&c.IsOwner(owner)&&c.OnMission&&c.MissionDue==due&&c.Recall(owner);}
  public override void OnResponse(NetState state,RelayInfo info){if(!C.IsOwner(state.Mobile))return;if(info.ButtonID==1&&!Confirm(C,state.Mobile,Due))state.Mobile.SendMessage("The mission changed or finished. Open its current status before recalling.");state.Mobile.SendGump(new CompanionActivityGump(C));}
 }
}
