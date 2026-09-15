using System;
using Server.Items;
using Server.Gumps;
using Server.Network;
namespace Server.HavenPrototype
{
 public sealed class HavenStorageQuantityGump:HavenMenuGump
 {
  readonly HavenEstateStorage _station;readonly Item _item;readonly Func<HavenEstateStorageGump> _back;
  public HavenStorageQuantityGump(HavenEstateStorage station,Item item,Func<HavenEstateStorageGump> back):base(100,100)
  {
   _station=station;_item=item;_back=back;AddBackground(0,0,520,235,3000);AddLabel(24,20,0,"WITHDRAW QUANTITY");string name=HavenStorageTools.Name(item);AddLabel(24,52,0,name.Length>50?name.Substring(0,47)+"...":name);AddLabel(24,82,0,"Available: "+item.Amount.ToString("N0"));AddTextEntry(24,113,130,24,0,1,"1");FlatButton(170,113,160,1,"Take quantity");FlatButton(24,157,95,2,"Take 1");FlatButton(130,157,95,3,"Take 10");FlatButton(236,157,95,4,"Take 100");FlatButton(342,157,145,5,"Take all");FlatButton(342,197,145,0,"Back");
  }
  public override void OnResponse(NetState state,RelayInfo info)
  {
   var p=state.Mobile;if(_station.Store==null||!_station.Store.CanAccessStores(p,_station))return;int amount=0;
   if(info.ButtonID==1)int.TryParse(info.GetTextEntry(1)?.Text,out amount);else if(info.ButtonID==2)amount=1;else if(info.ButtonID==3)amount=10;else if(info.ButtonID==4)amount=100;else if(info.ButtonID==5)amount=_item.Amount;
   if(info.ButtonID!=0)p.SendMessage(HavenStorageTools.WithdrawAmount(p,_station,_item,amount)?"Quantity withdrawn.":"Check quantity and backpack space. No items were withdrawn.");
   p.CloseGump(typeof(HavenEstateStorageGump));p.SendGump(_back());
  }
 }
}
