using System;
using System.Linq;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype {
 public class HavenPetBulkExchangeConfirm:HavenPetMenuGump {
  readonly HavenPetBook _book;readonly HavenPetTicket[] _tickets;
  internal static bool Eligible(Mobile owner,HavenPetTicket ticket,HavenPetBook book){return book!=null&&book.CanUse(owner)&&ticket!=null&&ticket.Parent==book&&HavenPetExchange.Eligible(owner,ticket,book)&&Math.Max(ticket.Rarity,HavenPetDefenses.Tier(ticket.Pet))<=1;}
  public HavenPetBulkExchangeConfirm(HavenPetBook book):base(70,70){
   _book=book;_tickets=book.Items.OfType<HavenPetTicket>().Where(t=>Eligible(book.Owner,t,book)).ToArray();
   AddBackground(0,0,570,300,3000);AddLabel(24,22,53,"Turn in Rare and below");
   AddHtml(24,65,522,150,"Exchange "+_tickets.Length+" pets for "+_tickets.Sum(HavenPetExchange.Value)+" credits.<BR><BR>Includes eligible Normal and Rare pets across the entire book, regardless of search or page.<BR>Favorites, bonded pets, Epic and Legendary pets are kept.<BR><BR>Confirming permanently removes these pets and their training.",false,false);
   if(_tickets.Length>0)FlatButton(24,249,310,1,"Confirm turn-in: "+_tickets.Length+" pets");FlatButton(350,249,190,0,"Keep my pets");
  }
  public override void OnResponse(NetState sender,RelayInfo info){
   var owner=sender.Mobile;if(!_book.CanUse(owner))return;
   if(info.ButtonID==1){int count=0,credits=0;foreach(var ticket in _tickets){if(!Eligible(owner,ticket,_book))continue;int value=HavenPetExchange.Value(ticket);if(HavenPetExchange.Exchange(owner,ticket,_book)){count++;credits+=value;}}owner.SendMessage("Exchanged "+count+" pets for "+credits+" credits. Protected or unavailable pets were kept.");}
   _book.OnDoubleClick(owner);
  }
 }
}
