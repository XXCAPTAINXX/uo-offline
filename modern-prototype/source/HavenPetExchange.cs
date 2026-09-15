using System;
using System.Linq;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
namespace Server.HavenPrototype {
 public static class HavenPetExchange {
  static string Key(Mobile p){return "Haven.PetCredits:"+p.Serial.Value;}
  public static int Balance(Mobile p){int n;var a=p.Account as Account;return a!=null&&int.TryParse(a.GetTag(Key(p)),out n)?Math.Max(0,Math.Min(1000000,n)):0;}
  static void Set(Mobile p,int n){((Account)p.Account).SetTag(Key(p),n.ToString());}
  public static bool Eligible(Mobile p,HavenPetTicket t,HavenPetBook book=null){return HavenMarks.CanUse(p)&&t!=null&&!t.Deleted&&t.Owner==p&&!t.Favorite&&t.Rarity<3&&t.Kind>=-1&&t.Kind<12&&p.Backpack!=null&&t.IsChildOf(p.Backpack)&&HavenResources.Accessible(p,t)&&t.Pet!=null&&!t.Pet.Deleted&&HavenPetDefenses.Tier(t.Pet)<3&&t.Pet.Map==Map.Internal&&!t.Pet.IsBonded&&(!(t.Parent is HavenPetBook)||(book!=null&&t.Parent==book&&book.CanUse(p)))&&!t.Pet.Controlled;}
  public static int Value(HavenPetTicket t){return new[]{1,3,8,20}[Math.Max(0,Math.Min(3,t.Rarity))];}
  public static bool Exchange(Mobile p,HavenPetTicket t,HavenPetBook book=null){if(!Eligible(p,t,book))return false;int value=Value(t),balance=Balance(p);if(balance>1000000-value)return false;Set(p,balance+value);t.Delete();return true;}
  public static int Cost(int tier){return tier==1?10:tier==2?30:tier==3?80:int.MaxValue;}
  public static bool Qualified(Mobile p,int kind){return kind>=6&&kind<12&&World.Mobiles.Values.OfType<HavenCompanion>().Any(c=>!c.Deleted&&c.BoundOwner==p&&HavenPetMissions.CanStart(c,(CompanionMission)(kind+6)));}
  public static bool Redeem(Mobile p,int kind,int tier){int cost=Cost(tier);if(!HavenMarks.CanUse(p)||p.Backpack==null||!Qualified(p,kind)||Balance(p)<cost)return false;
   var ticket=new HavenPetTicket(p,kind,5,tier);var supplies=ticket.TakeSupplies();if(supplies!=null)supplies.Delete();
   var book=HavenPetBook.Ensure(p);if(book==null||!book.TryDropItem(p,ticket,false)){ticket.Delete();return false;}Set(p,Balance(p)-cost);return true;
  }
  public static void Initialize(){CommandSystem.Register("petexchange",AccessLevel.Player,e=>Show(e.Mobile));}
  public static void Show(Mobile p){if(HavenMarks.CanUse(p)){p.CloseGump(typeof(HavenPetExchangeGump));p.SendGump(new HavenPetExchangeGump(p));}}
 }
 public class HavenPetExchangeGump:HavenPetMenuGump {
  readonly HavenPetTicket[] _tickets;readonly int _page;
  public HavenPetExchangeGump(Mobile p,int page=0):base(50,50){_tickets=p.Backpack==null?new HavenPetTicket[0]:p.Backpack.FindItemsByType(typeof(HavenPetTicket),true).Cast<HavenPetTicket>().Where(t=>HavenPetExchange.Eligible(p,t)).ToArray();_page=Math.Max(0,Math.Min(page,Math.Max(0,(_tickets.Length-1)/6)));
   AddBackground(0,0,710,545,3000);AddLabel(24,22,0,"MISSION PET EXCHANGE    Credits: "+HavenPetExchange.Balance(p));AddHtml(24,58,660,42,"Exchange pet tickets in your backpack for credits toward a higher-rarity pet. Stored pets are accepted. Bonded and Legendary pets cannot be exchanged. Confirming surrenders the pet.",false,false);
   AddLabel(24,111,0,"Available pet tickets");for(int row=0;row<6;row++){int index=_page*6+row;if(index>=_tickets.Length)break;var t=_tickets[index];FlatButton(24,143+row*34,660,100+index,(t.Pet.Name??"Pet")+" | "+HavenPetExchange.Value(t)+" credits");t.SendPropertiesTo(p);AddItemProperty(t.Serial);}
   if(_tickets.Length==0)AddLabel(24,151,0,"No eligible tickets. Legendary pets are protected; other claims must be in your backpack.");
   if(_page>0)FlatButton(24,354,120,1,"Previous");AddLabel(200,354,0,"Page "+(_page+1)+" / "+Math.Max(1,(_tickets.Length+5)/6));if((_page+1)*6<_tickets.Length)FlatButton(554,354,130,2,"Next");
   AddLabel(24,403,0,"Redeem: choose an unlocked mission species, then confirm.");FlatButton(24,440,210,10,"Rare or better: 10 credits");FlatButton(249,440,210,11,"Epic or better: 30 credits");FlatButton(474,440,210,12,"Legendary: 80 credits");FlatButton(554,497,130,0,"Close");
  }
  public override void OnResponse(NetState s,RelayInfo info){var p=s.Mobile;int id=info.ButtonID;if(!HavenMarks.CanUse(p)||id==0)return;if(id>=100&&id-100<_tickets.Length){var t=_tickets[id-100];if(HavenPetExchange.Eligible(p,t))p.SendGump(new HavenPetExchangeConfirm(t));return;}if(id>=10&&id<=12){p.SendGump(new HavenPetRedeemGump(p,id-9));return;}p.SendGump(new HavenPetExchangeGump(p,_page+(id==1?-1:id==2?1:0)));}
 }
 public class HavenPetExchangeConfirm:HavenPetMenuGump {
  readonly HavenPetTicket _ticket;readonly int _rarity;readonly HavenPetBook _book;
  public HavenPetExchangeConfirm(HavenPetTicket ticket,HavenPetBook book=null):base(80,80){_book=book;_ticket=ticket;_rarity=ticket.Rarity;AddBackground(0,0,540,240,3000);AddLabel(24,24,0,"Confirm pet ticket exchange");AddHtml(24,65,490,95,HavenMenuText.Encode(ticket.Pet.Name)+"<BR>Receive "+HavenPetExchange.Value(ticket)+" pet credits.<BR>This permanently consumes this ticket and the exact pet stored inside, including its training.",false,false);FlatButton(24,193,300,1,"Exchange this ticket");FlatButton(354,193,160,0,"Keep my ticket");}
  public override void OnResponse(NetState s,RelayInfo i){if(i.ButtonID==1)s.Mobile.SendMessage(_ticket.Rarity==_rarity&&HavenPetExchange.Exchange(s.Mobile,_ticket,_book)?"Ticket exchanged for pet credits.":"No exchange: ticket is unavailable or your credit balance is full.");if(_book!=null&&_book.CanUse(s.Mobile))_book.OnDoubleClick(s.Mobile);else HavenPetExchange.Show(s.Mobile);}
 }
 public class HavenPetRedeemGump:HavenPetMenuGump {
  readonly int _tier,_kind;
  public HavenPetRedeemGump(Mobile p,int tier,int kind=-1):base(80,80){_tier=tier;_kind=kind;AddBackground(0,0,550,410,3000);AddLabel(24,24,0,"Redeem "+HavenPetRarity.Label(tier)+" or better | "+HavenPetExchange.Cost(tier)+" credits");
   if(kind<0){AddLabel(24,64,0,"Species unlocked by your companion's mission skills:");int row=0;for(int k=6;k<12;k++)if(HavenPetExchange.Qualified(p,k))FlatButton(24,105+row++*36,500,100+k,HavenPetMissions.Names[k]);if(row==0)AddLabel(24,110,0,"Your companion has not unlocked a rare-pet mission yet.");}
   else{AddHtml(24,80,500,140,"Species: "+HavenPetMissions.Names[kind]+"<BR>Guaranteed minimum rarity: "+HavenPetRarity.Label(tier)+"<BR>Cost: "+HavenPetExchange.Cost(tier)+" pet credits.<BR>You receive a new unclaimed pet ticket. No bonus supplies.",false,false);FlatButton(24,286,500,1,"Confirm redemption");}
   FlatButton(374,362,150,0,"Back");
  }
  public override void OnResponse(NetState s,RelayInfo i){if(!HavenMarks.CanUse(s.Mobile))return;if(i.ButtonID>=106&&i.ButtonID<=111&&HavenPetExchange.Qualified(s.Mobile,i.ButtonID-100)){s.Mobile.SendGump(new HavenPetRedeemGump(s.Mobile,_tier,i.ButtonID-100));return;}if(i.ButtonID==1&&_kind>=6)s.Mobile.SendMessage(HavenPetExchange.Redeem(s.Mobile,_kind,_tier)?"Your upgraded pet is in [petbook.":"No credits spent: check credit balance, backpack room and mission eligibility.");HavenPetExchange.Show(s.Mobile);}
 }
}
