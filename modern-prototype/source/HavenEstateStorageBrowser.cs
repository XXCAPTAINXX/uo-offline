using System;
using System.Linq;
using Server.Items;
using Server.Network;
using Server.Gumps;

namespace Server.HavenPrototype
{
 public class HavenEstateStorageGump:HavenMenuGump
 {
  readonly HavenEstateStorage _station;readonly Container _folder;readonly Item[] _items;readonly int _page,_category,_sort;readonly string _search;
  const int Size=12;
  public HavenEstateStorageGump(HavenEstateStorage station,int page,Container folder=null,string search="",int category=0,int sort=0):base(35,35)
  {
   _station=station;var root=station.Store.StorageVault;_folder=folder!=null&&HavenStorageTools.Within(folder,root)&&HavenStorageTools.Browse(folder)?folder:root;_search=(search??"").Trim();_category=Math.Max(0,Math.Min(6,category));_sort=Math.Max(0,Math.Min(2,sort));
   var query=HavenStorageTools.Contents(_search.Length>0?root:_folder,_search.Length>0||_folder==root).Where(i=>(_category==0||HavenStorageTools.Category(i)==_category)&&(_search.Length==0||HavenStorageTools.Name(i).IndexOf(_search,StringComparison.OrdinalIgnoreCase)>=0||i.GetType().Name.IndexOf(_search,StringComparison.OrdinalIgnoreCase)>=0));
   var ordered=_sort==1?query.OrderByDescending(i=>i.Amount).ThenBy(HavenStorageTools.Name):_sort==2?query.OrderBy(HavenStorageTools.Category).ThenBy(HavenStorageTools.Name):query.OrderBy(HavenStorageTools.Name);
   var all=ordered.ThenBy(i=>i.Serial.Value).ToArray();int pages=Math.Max(1,(all.Length+Size-1)/Size);_page=Math.Max(0,Math.Min(page,pages-1));_items=all.Skip(_page*Size).Take(Size).ToArray();
   AddBackground(0,0,850,650,3000);AddLabel(24,18,0,"HOUSE STORAGE | "+all.Length+" matches | "+root.TotalItems+" stored items");
   AddLabel(24,46,0,_search.Length>0?"Search covers storage and accessible bags.":"Location: "+(_folder==root?"All stored contents":HavenStorageTools.Name(_folder)));
   AddBackground(24,74,400,28,3000);AddTextEntry(30,77,385,22,0,1,_search);FlatButton(435,78,90,3,"Search");FlatButton(535,78,80,4,"Clear");FlatButton(630,78,195,5,"Sort: "+new[]{"Name","Quantity","Category"}[_sort]);
   for(int i=0;i<7;i++)FlatButton(24+i*115,116,108,20+i,(_category==i?"[":"")+HavenStorageTools.Categories[i]+(_category==i?"]":""));
   AddLabel(24,149,0,"Item — hover for properties");AddLabel(330,149,0,"In bag");AddLabel(466,149,0,"Quantity");AddLabel(555,149,0,"Category");
   for(int i=0;i<_items.Length;i++){var item=_items[i];int y=177+i*30;string name=HavenStorageTools.Name(item);if(name.Length>35)name=name.Substring(0,32)+"...";AddLabel(24,y,0,name);AddItemProperty(item.Serial);string source=item.Parent==root?"Stores":HavenStorageTools.Name((Item)item.Parent);if(source.Length>15)source=source.Substring(0,12)+"...";AddLabel(330,y,0,source);AddLabel(466,y,0,item.Amount.ToString("N0"));AddLabel(555,y,0,HavenStorageTools.Categories[HavenStorageTools.Category(item)]);FlatButton(677,y,67,100+i,"Take");AddItemProperty(item.Serial);if(HavenStorageTools.Browse(item as Container))FlatButton(754,y,70,200+i,"Open");}
   if(_items.Length==0)AddLabel(24,197,0,"No matching items. Try another search or category.");
   FlatButton(24,550,105,1,"Previous");AddLabel(146,550,0,"Page "+(_page+1)+" / "+pages);FlatButton(290,550,105,2,"Next");FlatButton(415,550,120,6,"All stores");if(_folder!=root)FlatButton(550,550,115,7,"Up one bag");FlatButton(710,550,115,8,"Refresh");
   FlatButton(24,588,210,9,"Deposit from backpack");FlatButton(247,588,230,10,"Deposit companion loot");FlatButton(710,588,115,0,"Close");AddLabel(24,617,0,"Deposit keeps ALL gear, books, tools, ledgers, pet items, bandages and ammo.");
  }
  public override void OnResponse(NetState state,RelayInfo info)
  {
   var p=state.Mobile;int id=info.ButtonID;var store=_station.Store;if(id==0||store==null||!store.CanAccessStores(p,_station))return;var root=store.StorageVault;
   if(_folder!=root&&(!HavenStorageTools.Within(_folder,root)||!HavenStorageTools.Browse(_folder))){_station.OnDoubleClick(p);return;}
   string search=_search;int page=_page,category=_category,sort=_sort;Container folder=_folder;
   if(id==1)page--;else if(id==2)page++;else if(id==3){search=(info.GetTextEntry(1)?.Text??"");if(search.Length>100)search=search.Substring(0,100);page=0;}else if(id==4){search="";page=0;}else if(id==5){sort=(sort+1)%3;page=0;}else if(id==6){folder=root;search="";page=0;}else if(id==7){folder=(_folder.Parent as Container)??root;search="";page=0;}else if(id==9){_station.TargetDeposit(p);return;}else if(id==10){HavenStorageTools.ReportDeposit(p,HavenStorageTools.NearbyCompanion(p),_station);}else if(id>=20&&id<27){category=id-20;page=0;}else if(id>=100&&id<100+_items.Length){var item=_items[id-100];if(item.Stackable&&item.Amount>1){p.SendGump(new HavenStorageQuantityGump(_station,item,()=>new HavenEstateStorageGump(_station,_page,_folder,_search,_category,_sort)));return;}p.SendMessage(HavenStorageTools.Withdraw(p,_station,item)?"Withdrawn to your backpack.":"Item moved, inaccessible, or your backpack is full.");}else if(id>=200&&id<200+_items.Length){var next=_items[id-200] as Container;if(HavenStorageTools.Within(next,root)&&HavenStorageTools.Browse(next)){folder=next;search="";category=0;page=0;}}
   p.CloseGump(typeof(HavenEstateStorageGump));p.SendGump(new HavenEstateStorageGump(_station,page,folder,search,category,sort));
  }
 }
}
