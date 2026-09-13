using System;
using System.Runtime.CompilerServices;
using Server.Commands;
using Server.Network;
using Server.Mobiles;
namespace Server.HavenPrototype {
 public static class HavenAfkMissions {
  sealed class State {public bool Manual;public DateTime Activity=DateTime.UtcNow;}
  static readonly ConditionalWeakTable<Mobile,State> States=new ConditionalWeakTable<Mobile,State>();
  public static readonly TimeSpan IdleDelay=TimeSpan.FromMinutes(5);
  public static bool IsManual(Mobile p){return p!=null&&States.GetOrCreateValue(p).Manual;}
  public static bool IsIdle(DateTime activity,DateTime now){return now-activity>=IdleDelay;}
  public static bool IsAway(Mobile p,bool auto){return p!=null&&(p.NetState==null||IsManual(p));}
  static bool Fighting(Mobile mobile){var foe=mobile?.Combatant as Mobile;return mobile!=null && mobile.Alive && (HavenPreview.TravelCombatSeconds(mobile)>0 || (foe!=null&&!foe.Deleted&&foe.Alive&&foe.Map==mobile.Map&&mobile.InRange(foe,HavenCompanion.SupportRange)));}
  public static bool CombatActive(HavenCompanion companion,Mobile owner)
  {
   if(Fighting(owner)||Fighting(companion))return true;
   if(companion==null||companion.Map==null||companion.Map==Map.Internal)return false;
   var nearby=companion.GetMobilesInRange(HavenCompanion.SupportRange);
   try{foreach(Mobile mobile in nearby){var pet=mobile as BaseCreature;if(pet!=null&&pet.Controlled&&(pet.ControlMaster==owner||pet.ControlMaster==companion)&&Fighting(pet))return true;var target=mobile.Combatant as Mobile;var targetPet=target as BaseCreature;if(mobile.Alive&&target!=null&&(target==owner||target==companion||(targetPet!=null&&targetPet.Controlled&&(targetPet.ControlMaster==owner||targetPet.ControlMaster==companion))))return true;}}finally{nearby.Free();}
   return false;
  }
  public static void Activity(Mobile p,bool movement=false){if(p==null)return;var s=States.GetOrCreateValue(p);s.Activity=DateTime.UtcNow;if(movement&&s.Manual){s.Manual=false;p.SendMessage("AFK mode ended. Your mission return setting now applies.");}}
  public static void Set(Mobile p,bool enabled){if(p==null)return;var s=States.GetOrCreateValue(p);s.Manual=enabled;s.Activity=DateTime.UtcNow;p.SendMessage(enabled?"AFK mode on. Enabled offline mission plans can repeat. Move or use [afk off to return.":"AFK mode off. Your mission return setting now applies.");}
  public static void Initialize(){
   CommandSystem.Register("afk",AccessLevel.Player,e=>{if(!HavenPreview.Enabled)return;string arg=e.ArgString.Trim().ToLowerInvariant();if(arg!=""&&arg!="on"&&arg!="off"){e.Mobile.SendMessage("Use [afk, [afk on, or [afk off.");return;}Set(e.Mobile,arg=="on"||(arg==""&&!IsManual(e.Mobile)));});
   EventSink.Login+=e=>{States.Remove(e.Mobile);Activity(e.Mobile);};EventSink.Logout+=e=>States.Remove(e.Mobile);
   // Only player input, never ping/keepalive or server-generated mission updates.
   foreach(int id in new[]{0x02,0x03,0x05,0x06,0x07,0x08,0x12,0x13,0x6C,0x9A,0xAD,0xB1}){
    var old=PacketHandlers.GetHandler(id);if(old==null)continue;int packet=id;
    PacketHandlers.Register(id,old.Length,old.Ingame,(state,reader)=>{Activity(state.Mobile,packet==0x02);old.OnReceive(state,reader);});
    PacketHandlers.GetHandler(id).ThrottleCallback=old.ThrottleCallback;
   }
  }
 }
}
