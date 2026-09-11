using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenCompanionRolesGump:HavenMenuGump {
  readonly HavenCompanion _companion;
  static readonly string[] Names={"Warrior","Caster","Archer","Bard","Healer"};
  static readonly string[] Details={"Sword and shield; close combat.","Magery / Spellweaving; ranged spells.","Bow combat from range.","Crowd control, songs and taming assistance.","Stronger healing, triage and group recovery."};
  public HavenCompanionRolesGump(HavenCompanion c):base(50,50){_companion=c;AddBackground(0,0,490,370,3000);AddLabel(24,20,0,c.Name+" - combat role");AddLabel(24,50,0,"All roles can heal. Change roles nearby, outside combat.");for(int i=0;i<5;i++){int y=90+i*48;AddButton(24,y,0xFA5,0xFA7,10+i,GumpButtonType.Reply,0);AddLabel(60,y,0,Names[i]+((int)c.Role==i?" [active]":""));AddLabel(60,y+20,0,Details[i]);}AddButton(24,337,0xFA5,0xFA7,0,GumpButtonType.Reply,0);AddLabel(60,337,0,"Back");}
  public override void OnResponse(NetState state,RelayInfo info){var owner=state.Mobile;if(!_companion.IsOwner(owner))return;if(info.ButtonID==0){_companion.Show(owner,true);return;}if(_companion.ShowAwayTimer(owner))return;if(info.ButtonID>=10&&info.ButtonID<15&&!_companion.SetRole(owner,(CompanionRole)(info.ButtonID-10)))owner.SendMessage("Stand nearby and finish combat before changing roles.");owner.SendGump(new HavenCompanionRolesGump(_companion));}
 }
}
