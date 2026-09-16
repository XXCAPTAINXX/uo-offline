using System;
using System.Linq;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
namespace Server.HavenPrototype
{
 public static class HavenArrivalStory
 {
  public static readonly HavenPreview.Destination Arrival=new HavenPreview.Destination("Haven summoning beacon",Map.Trammel,3506,2570,14);
  static string Key(Mobile p){return "Haven.ArrivalStory:"+p.Serial.Value;}
  public static int Stage(Mobile p){int n;var a=p==null?null:p.Account as Account;return a!=null&&Int32.TryParse(a.GetTag(Key(p)),out n)?n:0;}
  static void SetStage(Mobile p,int n){var a=p.Account as Account;if(a!=null)a.SetTag(Key(p),n.ToString());}
  public static void Initialize()
  {
   CommandSystem.Register("story",AccessLevel.Player,e=>Show(e.Mobile));
   EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)EnsureBeacon();};
   EventSink.Login+=e=>Timer.DelayCall(TimeSpan.FromSeconds(2),()=>{if(HavenPreview.Enabled&&e.Mobile!=null&&!e.Mobile.Deleted&&e.Mobile.NetState!=null&&Stage(e.Mobile)==1)Show(e.Mobile);});
  }
  public static bool PlaceNewCharacter(Mobile p)
  {
   if(!HavenPreview.Enabled||p==null||p.Deleted||!p.Player||!(p.Account is Account)||p.Map!=Map.Internal||Stage(p)!=0)return false;
   Point3D point;if(!HavenPreview.FindLanding(Arrival,out point))return false;
   p.MoveToWorld(point,Arrival.Map);SetStage(p,1);return true;
  }
  public static HavenArrivalBeacon EnsureBeacon()
  {
   if(!HavenPreview.Enabled)return null;
   var existing=World.Items.Values.OfType<HavenArrivalBeacon>().FirstOrDefault(i=>!i.Deleted);if(existing!=null)return existing;
   Point3D point;if(!HavenPreview.FindLanding(Arrival,out point))return null;
   var beacon=new HavenArrivalBeacon();beacon.MoveToWorld(point,Arrival.Map);return beacon;
  }
  public static void Show(Mobile p,int page=0)
  {
   if(!HavenPreview.Enabled||p==null||p.Deleted||!p.Player)return;
   p.CloseGump(typeof(HavenArrivalStoryGump));p.SendGump(new HavenArrivalStoryGump(p,page));
  }
  public static HavenCompanion Meet(Mobile p)
  {
   if(!HavenPreview.Enabled||p==null||!p.Alive||Stage(p)!=1||p.Map!=Arrival.Map||!p.InRange(Arrival.Point,8)||!p.InLOS(Arrival.Point))return null;
   var c=HavenCompanion.Claim(p);if(c==null||!c.Alive||c.IsDeadPet||c.IsStabled||c.OnMission||c.Map!=p.Map||!c.InRange(p,8)||!c.InLOS(p))return null;
   c.Female=true;c.Body=0x191;c.FacialHairItemID=0;c.Name="Jenna Ashford";c.EnsureWardrobe();
   SetStage(p,2);HavenCompanionPresence.EnsureParty(c,p);c.SayTo(p,"Well... you're definitely not the supplies I ordered. Welcome to Haven.");return c;
  }
 }
 public class HavenArrivalBeacon:Item
 {
  [Constructable] public HavenArrivalBeacon():base(0x1F14){Name="the recovered summoning beacon";Hue=0x489;Movable=false;}
  public HavenArrivalBeacon(Serial s):base(s){}
  public override void OnDoubleClick(Mobile p){if(p.Map==Map&&p.InRange(this,3)&&p.InLOS(this))HavenArrivalStory.Show(p);else p.SendMessage("Come closer to the summoning beacon.");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public sealed class HavenArrivalStoryGump:HavenMenuGump
 {
  readonly Mobile _owner;readonly int _page;
  public HavenArrivalStoryGump(Mobile p,int page):base(60,60)
  {
   _owner=p;_page=Math.Max(0,Math.Min(1,page));bool first=HavenArrivalStory.Stage(p)==1;
   AddBackground(0,0,650,440,3000);AddLabel(24,20,0,"THE BEACON BEYOND | "+(_page==0?"An unexpected arrival":"A reason to stay"));
   string text=_page==0?
    "The last thing you remember is a light where no light should have been.<BR><BR>You wake beside a cracked beacon in New Haven. Someone has draped a travel cloak over you. Beyond the doorway, a town carries on as though people fall out of other worlds every morning.<BR><BR>A woman kneels beside the stone, trying very hard to look as though this was the plan.<BR><BR><B>Jenna:</B> Well... you're definitely not the supplies I ordered. Can you stand? Good. Welcome to Haven. We can work out the impossible part over breakfast.":
    "<B>Jenna:</B> My expedition found this beacon in a ruin. Then the paths changed, our camp vanished, and I was the only one who made it back. I brought the beacon here hoping it could reach them.<BR><BR>It reached you instead.<BR><BR>The same mark shines on your hand and hers. For the first time since the expedition vanished, the stone points somewhere.<BR><BR><B>Jenna:</B> I'll help you learn this world. You help me find my people. Along the way, we might even discover why an ancient ruin thinks we're a team.<BR><BR><B>Your first step:</B> Get your bearings in Haven and speak with Jenna using [c. The road to your own island refuge begins here.";
   AddHtml(24,62,602,292,"<BASEFONT COLOR=#3B2A1A>"+text+"</BASEFONT>",false,true);
   if(_page==0)FlatButton(24,375,240,1,first?"Meet Jenna":"Read the introduction");else FlatButton(24,375,240,2,"Back to the arrival");
   FlatButton(420,375,205,0,"Continue adventuring");AddLabel(24,410,0,first?"Meet Jenna beside the New Haven beacon. [story reopens this page.":"Story replay only. Your location and progress are preserved.");
  }
  public override void OnResponse(NetState state,RelayInfo info)
  {
   if(state.Mobile!=_owner||_owner.Deleted||info.ButtonID==0)return;
   if(info.ButtonID==1&&_page==0){if(HavenArrivalStory.Stage(_owner)==1&&HavenArrivalStory.Meet(_owner)==null){_owner.SendMessage("Return alive to the New Haven beacon. If you already recruited Jenna, recall her here before continuing.");HavenArrivalStory.Show(_owner);return;}HavenArrivalStory.Show(_owner,1);}
   else if(info.ButtonID==2)HavenArrivalStory.Show(_owner);
  }
 }
}
