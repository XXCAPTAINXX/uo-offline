using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Mobiles;
namespace Server.HavenPrototype {
 public static class HavenNewcomerBonus {
  private static readonly Dictionary<Mobile,int> Previous=new Dictionary<Mobile,int>();
  public static int Luck(Mobile p){return HavenPreview.Enabled && p is PlayerMobile && p.Map==Map.Trammel && p.X>=3314 && p.X<3814 && p.Y>=2345 && p.Y<3095?1000:0;}
  public static double Chance(Mobile p,Skill skill){return Luck(p)>0 && skill.BaseFixedPoint<1000?5.0:1.0;}
  public static int Amount(Mobile p,Skill skill,int normal){return Luck(p)>0 && skill.BaseFixedPoint<1000?Math.Min(normal*5,1000-skill.BaseFixedPoint):normal;}
  public static void Initialize(){CommandSystem.Register("havenluck",AccessLevel.Player,e=>e.Mobile.SendMessage("Total Luck: "+e.Mobile.Luck+". Haven area bonus: "+Luck(e.Mobile)+". In Haven, natural skill-gain chance and amount are 5x below 100.0."));EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Timer.DelayCall(TimeSpan.FromSeconds(2),TimeSpan.FromSeconds(5),Refresh);};}
  private static void Refresh(){var live=new HashSet<Mobile>();foreach(var state in Server.Network.NetState.Instances){var p=state.Mobile as PlayerMobile;if(p==null)continue;live.Add(p);int now=Luck(p),old;if(!Previous.TryGetValue(p,out old)||old!=now){p.Delta(MobileDelta.Stat);p.SendMessage(now>0?"Haven bonus: +1,000 Luck and faster natural skill training below 100.0.":"You left the Haven bonus area.");Previous[p]=now;}}var remove=new List<Mobile>();foreach(var p in Previous.Keys)if(!live.Contains(p))remove.Add(p);foreach(var p in remove)Previous.Remove(p);}
 }
}
