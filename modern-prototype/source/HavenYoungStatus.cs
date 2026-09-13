using System;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
namespace Server.HavenPrototype {
 public static class HavenYoungStatus {
  public static bool CheckProgress(Mobile from) {
   var p=from as PlayerMobile;
   if(!HavenPreview.Enabled||p==null||p.Deleted||!p.Young||p.Skills.Total<4500)return false;
   return Remove(p);
  }
  public static void Initialize() {
   EventSink.Login+=e=>CheckProgress(e.Mobile);
   EventSink.SkillGain+=e=>Timer.DelayCall(TimeSpan.Zero,()=>CheckProgress(e.From));
   EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(1),()=>{foreach(var p in World.Mobiles.Values.OfType<PlayerMobile>().ToArray())CheckProgress(p);});
   CommandSystem.Register("renounceyoung",AccessLevel.Player,e=>Open(e.Mobile));
   EventSink.Speech+=e=>{if(IsRenunciation(e.Speech))Open(e.Mobile);};
  }
  public static bool IsRenunciation(string text){return text!=null&&String.Equals(text.Trim().TrimEnd('.','!'),"i renounce my young player status",StringComparison.OrdinalIgnoreCase);}
  public static void Open(Mobile from){var p=from as PlayerMobile;var account=from.Account as Account;if(p==null||account==null)return;if(!p.Young&&!account.Young){from.SendMessage("You no longer have Young player status.");return;}from.CloseGump(typeof(RenounceYoungGump));from.CloseGump(typeof(HavenRenounceYoungGump));from.SendGump(new HavenRenounceYoungGump());}
  public static bool Remove(Mobile from){var p=from as PlayerMobile;var account=from==null?null:from.Account as Account;if(p==null||account==null)return false;account.RemoveYoungStatus(0);p.Young=false;from.SendMessage("Young status has been removed from your account and characters.");return true;}
 }
 public class HavenRenounceYoungGump:Gump {
  public HavenRenounceYoungGump():base(100,100){AddBackground(0,0,430,240,0xA28);AddHtml(24,22,380,35,"<BASEFONT COLOR=#342B23><B>Leave Young player protection</B></BASEFONT>",false,false);AddHtml(24,65,380,95,"<BASEFONT COLOR=#342B23>This permanently removes Young status from this account and its characters. Young-player protection and travel restrictions will end.</BASEFONT>",false,false);AddButton(24,184,0xFA5,0xFA7,1,GumpButtonType.Reply,0);AddLabel(58,184,0,"Remove Young status");AddButton(285,184,0xFA5,0xFA7,0,GumpButtonType.Reply,0);AddLabel(318,184,0,"Cancel");}
  public override void OnResponse(NetState state,RelayInfo info){if(info.ButtonID==1)HavenYoungStatus.Remove(state.Mobile);}
 }
}
