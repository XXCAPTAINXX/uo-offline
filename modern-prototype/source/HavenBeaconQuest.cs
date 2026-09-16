using System;
using System.Linq;
using Server.Accounting;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Network;
namespace Server.HavenPrototype
{
 public static class HavenBeaconQuest
 {
  public static readonly Point3D PracticePoint=new Point3D(3469,2601,10);
  public static readonly string[] VoiceLines={
   "Well, you're definitely not the supplies I ordered. Welcome to Haven. Let's find out why that beacon chose you.",
   "That is Captain Mara's handwriting. She was planning a rescue, not a treasure hunt. Come on. Let's see what this mark of yours can do.",
   "There! Did you see the light? The beacon answers when we work together. And the golem still has most of its dignity.",
   "Tide, anchor, star. A sailor's way home. Remember that order. I think our stubborn little beacon is about to tell us something.",
   "An island! That's our old refuge. If the beacon remembers the route, someone may still be out there. Looks like you and I have an adventure ahead of us.",
   "Here, this cape is yours. Wear it when you fight, and it will grow stronger with you, all the way to level twenty. Hover over it to see your progress. Consider it a welcome present.",
   "And this gentle horse is yours, too. Feed it the apple in your pack, and it will bond with you immediately. Double-click to ride. Look after each other, all right?",
   "You earned this. Place the expedition fountain in your house, then put your bandages inside. It will enhance the whole stack at once. No waiting around. We've got adventures to get to.",
   "Before we chase that light across the sea, let's meet the recovery steward. Ava brings you back to life. Mara helps our pets and brings surviving corpses home. Remember to open them and collect your things.",
   "Now we know how to get back on our feet. Let's visit the supply stones. Select something to inspect it, check the price, and only buy when you're ready. You don't have to spend anything for my benefit.",
   "Blackwake Cove was our landing place. If those crews have taken it over, they may have our records too. Let's find their camp. We're only scouting for now.",
   "Three crews, three captains. We only need one captain's papers. Choose a normal expedition first: three waves, then the captain. Challenge mode brings all three captains together. That can wait.",
   "Listen to this. Keep the refuge light burning. The next arrival must find the shore. Someone wanted you here. I don't know whether that makes me feel better. But at least we're asking the right questions."
  };
  static string Key(Mobile p,string field){return "Haven.BeaconQuest:"+p.Serial.Value+":"+field;}
  static int Read(Mobile p,string field){int n;var a=p==null?null:p.Account as Account;return a!=null&&Int32.TryParse(a.GetTag(Key(p,field)),out n)?n:0;}
  static void Write(Mobile p,string field,int n){((Account)p.Account).SetTag(Key(p,field),n.ToString());}
  public static int Phase(Mobile p){return Read(p,"phase");}
  public static int Damage(Mobile p){return Read(p,"damage");}
  public static int Glyph(Mobile p){return Read(p,"glyph");}
  public static bool VoiceEnabled(Mobile p){return Read(p,"muted")==0;}
  public static void ToggleVoice(Mobile p){if(HavenMarks.CanUse(p)){Write(p,"muted",VoiceEnabled(p)?1:0);if(!VoiceEnabled(p))HavenStoryVoice.Clear(p);}}
  public static void Initialize(){HavenTrainingGolem.PracticeDamage+=RecordPractice;CommandSystem.Register("storyquest",AccessLevel.Player,e=>Show(e.Mobile));EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Timer.DelayCall(TimeSpan.FromSeconds(4),()=>EnsureNodes());};}
  public static HavenCompanion NearbyJenna(Mobile p){return p==null?null:World.Mobiles.Values.OfType<HavenCompanion>().FirstOrDefault(c=>!c.Deleted&&c.IsOwner(p)&&c.Alive&&!c.IsDeadPet&&!c.IsStabled&&!c.OnMission&&c.Map==p.Map&&c.InRange(p,8)&&c.InLOS(p));}
  static bool Ready(Mobile p){return HavenMarks.CanUse(p)&&NearbyJenna(p)!=null;}
  public static bool AtBeacon(Mobile p){var b=World.Items.Values.OfType<HavenArrivalBeacon>().FirstOrDefault(i=>!i.Deleted);return b!=null&&p.Map==b.Map&&p.InRange(b,3)&&Math.Abs(p.Z-b.Z)<=8&&p.InLOS(b);}
  public static HavenBeaconClue Node(int kind){return World.Items.Values.OfType<HavenBeaconClue>().FirstOrDefault(n=>!n.Deleted&&n.Kind==kind);}
  public static bool EnsureNodes()
  {
   if(!HavenPreview.Enabled)return false;
   for(int kind=0;kind<2;kind++)
   {
    var existing=Node(kind);if(existing!=null&&(kind!=1||existing.PlacementRevision>=1))continue;
    int x=kind==0?3495:3473,y=kind==0?2572:2603;
    var site=new HavenPreview.Destination("Expedition clue",Map.Trammel,x,y,Map.Trammel.GetAverageZ(x,y));Point3D landing;
    if(!HavenPreview.FindLanding(site,out landing))continue;
    var clue=existing??new HavenBeaconClue(kind);clue.MoveToWorld(landing,Map.Trammel);clue.PlacementRevision=1;
   }
   return Node(0)!=null&&Node(1)!=null&&HavenTrainingGolem.EnsureStation();
  }
  public static void StopGuide(Mobile p){if(p.QuestArrow is HavenBeaconQuestArrow)p.QuestArrow.Stop();}
  static void Advance(Mobile p,int phase,int voice){Write(p,"phase",phase);StopGuide(p);Speak(p,voice);Show(p);}
  public static bool Accept(Mobile p)
  {
   if(!Ready(p)||!AtBeacon(p)||Phase(p)!=0||HavenArrivalStory.Stage(p)==1||!EnsureNodes())return false;
   Write(p,"damage",0);Write(p,"glyph",0);Advance(p,1,0);HavenStoryGifts.GiveStarterItems(p);return true;
  }
  public static bool Inspect(Mobile p,HavenBeaconClue node)
  {
   if(!Ready(p)||node==null||node.Deleted||p.Map!=node.Map||!p.InRange(node,3)||Math.Abs(p.Z-node.Z)>8||!p.InLOS(node))return false;
   if(node.Kind==0&&Phase(p)==1){Advance(p,2,1);return true;}
   if(node.Kind==1&&Phase(p)==3){Advance(p,4,3);return true;}
   return false;
  }
  public static void RecordPractice(HavenTrainingGolem golem,Mobile attacker,int amount)
  {
   if(golem==null||golem.Deleted||amount<=0||golem.Map!=Map.Trammel||!golem.InRange(PracticePoint,4))return;
   Mobile p=attacker;for(int i=0;i<4&&p is BaseCreature;i++){var next=((BaseCreature)p).GetMaster();if(next==null||next==p)break;p=next;}
   if(!Ready(p)||Phase(p)!=2||p.Map!=golem.Map||!p.InRange(golem,12)||!p.InLOS(golem))return;
   Write(p,"damage",Math.Min(50,Damage(p)+Math.Min(50,amount)));
   if(Damage(p)>=50)Advance(p,3,2);
  }
  public static bool ChooseGlyph(Mobile p,int glyph)
  {
   if(!Ready(p)||!AtBeacon(p)){if(p!=null)p.SendMessage("Stand beside the beacon with Jenna to answer its symbols.");return false;}
   if(Phase(p)!=4||glyph<0||glyph>2)return false;
   int step=Glyph(p);if(glyph!=step){Write(p,"glyph",0);p.SendMessage("The light fades. Remember the waystone: tide, anchor, star. Try again; nothing was lost.");return false;}
   if(step<2){Write(p,"glyph",step+1);return true;}
   if(HavenMarks.Balance(p)>HavenMarks.Maximum-20){p.SendMessage("Spend at least 20 Marks before collecting this reward.");return false;}
   if(!Banker.Deposit(p,2500)){p.SendMessage("Your bank could not accept the reward. Make room and try the final symbol again.");return false;}
   HavenMarks.Award(p,20);Write(p,"glyph",3);Advance(p,5,4);p.SendMessage("A Light That Answers complete: 2,500 gold banked and 20 Haven Marks awarded.");return true;
  }
  public static int CurrentVoice(Mobile p){int phase=Phase(p);return phase<=1?0:phase==2?1:phase==3?2:phase==4?3:4;}
  public static void Speak(Mobile p,int line,bool replay=false)
  {
   if(p==null||p.Deleted||line<0||line>=VoiceLines.Length)return;
   p.SendMessage(0x59B,"Jenna: "+VoiceLines[line]);
   if(VoiceEnabled(p))HavenStoryVoice.Enqueue(p,line,replay);
   else if(replay)p.SendMessage("Voice is muted. Turn it on to hear Jenna.");
  }
  public static void Show(Mobile p){if(!HavenMarks.CanUse(p))return;p.CloseGump(typeof(HavenBeaconQuestGump));p.SendGump(new HavenBeaconQuestGump(p));}
  public static IEntity Target(Mobile p)
  {
   int phase=Phase(p);if(phase==1)return Node(0);if(phase==3)return Node(1);
   if(phase==2)return World.Mobiles.Values.OfType<HavenTrainingGolem>().FirstOrDefault(g=>!g.Deleted&&g.Map==Map.Trammel&&g.InRange(PracticePoint,4));
   return World.Items.Values.OfType<HavenArrivalBeacon>().FirstOrDefault(b=>!b.Deleted);
  }
  public static void Guide(Mobile p)
  {
   if(!HavenMarks.CanUse(p))return;var target=Target(p);if(target==null){p.SendMessage("That objective is unavailable. Please try again shortly.");return;}
   p.SendMessage("Next objective: Trammel, "+target.X+", "+target.Y+".");if(p.Map!=target.Map){p.SendMessage("Travel to New Haven first.");return;}
   if(p.QuestArrow!=null)p.QuestArrow.Stop();p.QuestArrow=new HavenBeaconQuestArrow(p,target);
  }
 }
 public class HavenBeaconClue:Item
 {
  public int Kind{get;private set;}
  public int PlacementRevision{get;internal set;}
  public HavenBeaconClue(int kind):base(kind==0?0xFF1:0x1F14){Kind=kind;Name=kind==0?"Mara's expedition ledger":"the tide-marked waystone";Hue=kind==0?0:0x489;Movable=false;}
  public HavenBeaconClue(Serial s):base(s){}
  public override void OnDoubleClick(Mobile p){if(!HavenBeaconQuest.Inspect(p,this)){p.SendMessage("Follow your current objective with Jenna nearby. Stand beside the clue to inspect it.");HavenBeaconQuest.Show(p);}}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(1);w.Write(Kind);w.Write(PlacementRevision);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);int version=r.ReadInt();Kind=r.ReadInt();PlacementRevision=version>=1?r.ReadInt():0;}
 }
 public sealed class HavenBeaconQuestArrow:QuestArrow
 {
  readonly IEntity _target;readonly Timer _timer;
  public HavenBeaconQuestArrow(Mobile p,IEntity target):base(p,target){_target=target;Update();_timer=Timer.DelayCall(TimeSpan.FromSeconds(2),TimeSpan.FromSeconds(2),Tick);}
  void Tick(){if(Mobile.Deleted||Mobile.NetState==null||_target.Deleted||Mobile.Map!=_target.Map||Mobile.InRange(_target.Location,2)){Stop();return;}Update();}
  public override void OnClick(bool rightClick){if(rightClick)Stop();}
  public override void OnStop(){if(_timer!=null)_timer.Stop();base.OnStop();}
 }
}
