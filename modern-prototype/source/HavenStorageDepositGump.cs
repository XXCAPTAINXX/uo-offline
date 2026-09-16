using System;
using System.Linq;
using System.Collections.Generic;
using Server.Items;
using Server.Network;
using Server.Gumps;
namespace Server.HavenPrototype
{
 public sealed class HavenStorageDepositGump:HavenMenuGump
 {
  readonly Mobile _owner;readonly HavenCompanion _companion;readonly HavenEstateStorage _station;
  readonly Func<HavenEstateStorageGump> _back;readonly Dictionary<Item,int> _snapshot;readonly int _page;bool _used;
  public HavenStorageDepositGump(Mobile owner,HavenCompanion companion,HavenEstateStorage station,Func<HavenEstateStorageGump> back=null):this(owner,companion,station,HavenStorageTools.DepositCandidates(companion).ToDictionary(i=>i,i=>i.Amount),0,back){}
  private HavenStorageDepositGump(Mobile owner,HavenCompanion companion,HavenEstateStorage station,Dictionary<Item,int> snapshot,int page,Func<HavenEstateStorageGump> back):base(60,60)
  {
   _back=back;_owner=owner;_companion=companion;_station=station;_snapshot=snapshot;
   int pages=Math.Max(1,(snapshot.Count+9)/10);_page=Math.Max(0,Math.Min(page,pages-1));
   AddBackground(0,0,680,580,3000);AddLabel(24,20,0,"DEPOSIT COMPANION LOOT");
   AddLabel(24,54,0,snapshot.Count+" selected. Gear, books, tools, bandages and ammo stay.");
   AddLabel(24,83,0,"Changed stacks and inaccessible items stay put. New loot is not added.");
   AddLabel(24,120,0,"Item");AddLabel(455,120,0,"Quantity");
   int row=0;foreach(var pair in snapshot.OrderBy(p=>HavenStorageTools.Name(p.Key)).ThenBy(p=>p.Key.Serial.Value).Skip(_page*10).Take(10))
   {string name=HavenStorageTools.Name(pair.Key);AddLabel(24,151+row*30,0,name.Length>48?name.Substring(0,45)+"...":name);AddItemProperty(pair.Key.Serial);AddLabel(455,151+row*30,0,pair.Value.ToString("N0"));row++;}
   if(snapshot.Count==0)AddLabel(24,151,0,"No unprotected loot is ready to deposit.");
   FlatButton(24,465,110,2,"Previous");AddLabel(160,465,0,"Page "+(_page+1)+" / "+pages);FlatButton(305,465,110,3,"Next");
   FlatButton(24,510,240,1,"Deposit listed loot");FlatButton(430,510,220,0,"Back to storage");
   AddLabel(24,548,0,"Full storage leaves the remaining items in Jenna's backpack.");
  }
  public override void OnResponse(NetState state,RelayInfo info)
  {
   var p=state.Mobile;if(_used||p!=_owner||_station?.Store==null||!_station.Store.CanAccessStores(p,_station))return;_used=true;
   if(info.ButtonID==2||info.ButtonID==3){p.SendGump(new HavenStorageDepositGump(p,_companion,_station,_snapshot,_page+(info.ButtonID==2?-1:1),_back));return;}
   if(info.ButtonID==1){int n=HavenStorageTools.DepositSnapshot(p,_companion,_station,_snapshot);p.SendMessage(n<0?"Your companion must be nearby and available.":"Stored "+n+" of "+_snapshot.Count+" listed stacks/items. Changed, protected or blocked items stayed with Jenna.");}
   p.CloseGump(typeof(HavenEstateStorageGump));p.SendGump(_back==null?new HavenEstateStorageGump(_station,0):_back());
  }
 }
}
