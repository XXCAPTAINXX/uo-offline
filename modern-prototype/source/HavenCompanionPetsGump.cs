using System;
using System.Linq;
using Server;
using Server.Commands;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
namespace Server.HavenPrototype
{
 public class HavenCompanionPetsGump:HavenMenuGump
 {
  readonly HavenCompanion _companion;readonly HavenCompanionAssignedPet[] _pets;readonly int _page;
  public static void Initialize(){CommandSystem.Register("companionpets",AccessLevel.Player,e=>{var c=World.Mobiles.Values.OfType<HavenCompanion>().FirstOrDefault(x=>x.IsOwner(e.Mobile));Show(e.Mobile,c);});}
  static bool CanUse(Mobile p,HavenCompanion c){return p!=null&&p.Alive&&c!=null&&!c.Deleted&&c.IsOwner(p)&&p.Map==c.Map&&p.InRange(c,12);}
  public static void Show(Mobile p,HavenCompanion c,int page=0){if(!CanUse(p,c)){p.SendMessage("Bring your companion within 12 tiles to manage assigned pets.");return;}p.CloseGump(typeof(HavenCompanionPetsGump));p.SendGump(new HavenCompanionPetsGump(c,page));}
  public HavenCompanionPetsGump(HavenCompanion c,int page=0):base(50,50){_companion=c;var all=c.Backpack.Items.OfType<HavenCompanionAssignedPet>().Where(x=>!x.Deleted&&x.Pet!=null&&!x.Pet.Deleted).ToArray();_page=Math.Max(0,Math.Min(Math.Max(0,(all.Length-1)/5),page));_pets=all.Skip(_page*5).Take(5).ToArray();AddBackground(0,0,620,490,0xA28);Text(24,20,570,30,"<B>Companion pets</B>");Text(24,57,570,56,"Follower slots: "+c.Followers+" / "+c.FollowersMax+"<BR>Assign nearby pets out of combat. Reclaim puts the original pet into a ticket in the companion pack.");Button(24,124,1,"Assign my pet...");Button(320,124,4,c.TamingAssistActive?"Stop tame assist":"Tame assist...");for(int i=0;i<_pets.Length;i++){var item=_pets[i];int y=175+i*48;Text(24,y,260,44,item.Pet.Name+"<BR>"+(item.Pet.IsDeadPet?"Needs resurrection":item.Parked?"Waiting for mission return":"Following / fighting / riding"));Button(292,y,100+i*2,item.AutoMount?"Ride: on":"Ride: off");Button(450,y,101+i*2,"Reclaim");}if(all.Length==0)Text(24,180,550,30,"No pets assigned.");Button(24,444,2,"Previous");Text(220,444,100,25,"Page "+(_page+1));Button(335,444,3,"Next");Button(490,444,0,"Close");}
  void Text(int x,int y,int w,int h,string s){AddHtml(x,y,w,h,"<BASEFONT COLOR=#342B23>"+s+"</BASEFONT>",false,false);}
  void Button(int x,int y,int id,string label){AddButton(x,y,0xFA5,0xFA7,id,GumpButtonType.Reply,0);Text(x+33,y,180,28,label);}
  public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;int id=info.ButtonID;if(id==0||!CanUse(p,_companion))return;if(id==1){p.Target=new AssignTarget(_companion);p.SendMessage("Target your living, dismounted pet within three tiles of the companion.");return;}if(id==4){_companion.RequestTamingAssist(p);return;}if(id==2||id==3){Show(p,_companion,_page+(id==2?-1:1));return;}int index=(id-100)/2;if(id>=100&&index<_pets.Length){var item=_pets[index];if(!item.Deleted&&item.Owner==p&&item.Companion==_companion&&item.Pet.ControlMaster==_companion){if((id-100)%2==0){item.AutoMount=!item.AutoMount;item.Tick();}else if(!item.ClaimBack(p))p.SendMessage("Reclaim failed: check pet health and space in the companion pack.");}}Show(p,_companion,_page);}
  class AssignTarget:Target{readonly HavenCompanion _c;public AssignTarget(HavenCompanion c):base(3,false,TargetFlags.None){_c=c;}protected override void OnTarget(Mobile p,object target){if(!CanUse(p,_c)||!(target is BaseCreature)||!_c.AcceptAssignedPet(p,(BaseCreature)target))p.SendMessage("Assignment failed: check ownership, distance, combat, follower slots and companion Taming/Lore.");Show(p,_c);}}
 }
}
