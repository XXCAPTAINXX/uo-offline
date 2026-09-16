using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Server.Network;
namespace Server.HavenPrototype
{
 public static class HavenStoryVoice
 {
  public const int FirstSound=32750;
  public static void Initialize(){EventSink.Logout+=e=>Clear(e.Mobile);}
  public static readonly double[] VoiceSeconds={8.016,9.912,8.424,9.864,10.704,12.84,11.544,13.416};
  sealed class Playback{public readonly Queue<int> Pending=new Queue<int>();public DateTime Next;public int Playing=-1;public Timer Timer;}
  static readonly ConditionalWeakTable<Mobile,Playback> Players=new ConditionalWeakTable<Mobile,Playback>();
  public static void Clear(Mobile p){Playback state;if(p!=null&&Players.TryGetValue(p,out state)){state.Pending.Clear();if(state.Timer!=null)state.Timer.Stop();state.Timer=null;}}
  public static void Enqueue(Mobile p,int line,bool replay)
  {
   if(p==null||p.Deleted||p.NetState==null||line<0||line>=VoiceSeconds.Length)return;
   var state=Players.GetValue(p,x=>new Playback());
   if(state.Pending.Contains(line)||DateTime.UtcNow<state.Next&&state.Playing==line){if(replay)p.SendMessage("That voice line is already playing or queued.");return;}
   if(state.Pending.Count>=VoiceSeconds.Length)return;
   state.Pending.Enqueue(line);if(replay&&DateTime.UtcNow<state.Next)p.SendMessage("Jenna will replay that line after she finishes speaking.");
   Pump(p,state);
  }
  static void Pump(Mobile p,Playback state)
  {
   if(p.Deleted||p.NetState==null||!HavenBeaconQuest.VoiceEnabled(p)){Clear(p);return;}
   if(state.Timer!=null)return;
   if(DateTime.UtcNow<state.Next){state.Timer=Timer.DelayCall(state.Next-DateTime.UtcNow,()=>{state.Timer=null;Pump(p,state);});return;}
   if(state.Pending.Count==0)return;
   int line=state.Pending.Dequeue();state.Playing=line;state.Next=DateTime.UtcNow.AddSeconds(VoiceSeconds[line]+0.4);
   p.Send(new PlaySound(FirstSound+line,p.Location));
   if(state.Pending.Count>0){state.Timer=Timer.DelayCall(TimeSpan.FromSeconds(VoiceSeconds[line]+0.4),()=>{state.Timer=null;Pump(p,state);});}
  }
 }
}
