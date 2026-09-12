using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Server.Accounting;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Gumps;
using Server.Network;
using Server.Targeting;
namespace Server.HavenPrototype {
 public interface IHavenEstateStores {
  Container StorageVault { get; }
  bool CanAccessStores(Mobile p,HavenEstateStorage station);
  bool Deposit(Mobile p,HavenEstateStorage station,Item item);
  bool Withdraw(Mobile p,HavenEstateStorage station,Item item);
 }
 public class HavenIslandEstate:Castle,IHavenEstateStores {
  public Container StorageVault { get { return Vault; } }
  private string _ownerAccount;public Container Vault;public readonly List<HavenEstateStorage> Stations=new List<HavenEstateStorage>();
  public HavenIslandEstate(Mobile owner):base(owner){Public=false;RestrictDecay=true;_ownerAccount=(owner.Account as Account)?.Username;Vault=new HavenHomeChest{Name="Island private stores",MaxItems=3000};Vault.Internalize();}
  public HavenIslandEstate(Serial serial):base(serial){}
  public static HavenIslandEstate BuildTest(Mobile owner){if(!HavenIslandInstall.CanBuild||owner==null||!(owner.Account is Account))throw new InvalidOperationException("Estate requires isolated test and an owner account.");ArrayList moved;var spot=new Point3D(4196,2868,0);var probe=new PlayerMobile{Player=true,Map=Map.Trammel};try{var placement=HousePlacement.Check(probe,0x7E,spot,out moved);if(placement!=HousePlacementResult.Valid||moved.Count!=0)throw new InvalidOperationException("Estate plot obstructed: "+placement+", displaced="+moved.Count);}finally{probe.Delete();}var estate=new HavenIslandEstate(owner);estate.MoveToWorld(spot,Map.Trammel);try{estate.InstallStores();return estate;}catch{estate.Delete();throw;}}
  public bool CanAccessStores(Mobile p,HavenEstateStorage station){return !Deleted&&p!=null&&!p.Deleted&&p.Alive&&Owner!=null&&!Owner.Deleted&&p.Account!=null&&p.Account==Owner.Account&&station!=null&&!station.Deleted&&Stations.Contains(station)&&station.Estate==this&&p.Map==Map&&station.Map==Map&&p.InRange(station,3)&&Math.Abs(p.Z-station.Z)<=8&&p.InLOS(station)&&Vault!=null&&!Vault.Deleted;}
  void InstallStores(){var offsets=new[]{new Point2D(-5,-4),new Point2D(5,-4),new Point2D(5,8)};for(int i=0;i<offsets.Length;i++){var offset=offsets[i];Point3D? found=null;for(int radius=0;radius<=4&&found==null;radius++)for(int dx=-radius;dx<=radius&&found==null;dx++)for(int dy=-radius;dy<=radius&&found==null;dy++)foreach(int z in new[]{6,0,26}){var p=new Point3D(X+offset.X+dx,Y+offset.Y+dy,Z+z);if(Map.CanFit(p,16,false,false)&&BaseHouse.FindHouseAt(p,Map,16)==this&&!Stations.Any(s=>s.Location==p)){found=p;break;}}if(found==null)throw new InvalidOperationException("No accessible estate store floor near "+offset+"");var station=new HavenEstateStorage(this){Name=i==0?"Receiving chest":i==1?"Workshop stores":"Armory stores"};Stations.Add(station);station.MoveToWorld(found.Value,Map);}}
  internal static bool BackpackOrigin(Mobile p,Item item){if(item.IsChildOf(p.Backpack))return HavenResources.Accessible(p,item);var bounce=item.GetBounce();var parent=bounce?.m_Parent as Item;return item.Parent==null&&item.Map==Map.Internal&&bounce!=null&&bounce.m_Mobile==p&&parent!=null&&(parent==p.Backpack||parent.IsChildOf(p.Backpack))&&HavenResources.Accessible(p,parent);}
  public bool Deposit(Mobile p,HavenEstateStorage station,Item item){if(!CanAccessStores(p,station)||item==null||item.Deleted||!item.Movable||p.Backpack==null||!BackpackOrigin(p,item))return false;return Vault.TryDropItem(p,item,false);}
  public bool Withdraw(Mobile p,HavenEstateStorage station,Item item){if(!CanAccessStores(p,station)||item==null||item.Deleted||item.Parent!=Vault||p.Backpack==null)return false;return p.Backpack.TryDropItem(p,item,false);}
  public override void OnDelete(){foreach(var station in Stations.ToArray())if(!station.Deleted)station.Delete();if(Vault!=null&&!Vault.Deleted){if(Vault.Items.Count==0)Vault.Delete();else{Vault.Name="Recovered island stores";Vault.Movable=true;if(Owner!=null&&!Owner.Deleted)Owner.BankBox.DropItem(Vault);else{var recovery=new HavenEstateRecovery(_ownerAccount);recovery.DropItem(Vault);recovery.Internalize();}}}base.OnDelete();}
  public override void OnTransfer(){base.OnTransfer();_ownerAccount=(Owner?.Account as Account)?.Username??_ownerAccount;}
  public override void Serialize(GenericWriter w){base.Serialize(w);_ownerAccount=(Owner?.Account as Account)?.Username??_ownerAccount;w.Write(1);w.Write(_ownerAccount);w.Write(Vault);w.Write(Stations.Count);foreach(var station in Stations)w.Write(station);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);int version=r.ReadInt();_ownerAccount=version>=1?r.ReadString():(Owner?.Account as Account)?.Username;Vault=r.ReadItem() as Container;Stations.Clear();int count=r.ReadInt();if(count<0||count>3)throw new InvalidOperationException("Invalid estate storage count");for(int i=0;i<count;i++){var station=r.ReadItem() as HavenEstateStorage;if(station!=null)Stations.Add(station);}}
 }
 public class HavenEstateRecovery:HavenHomeChest {
  private string _account;
  public HavenEstateRecovery(string account){_account=account;Name="Recovered island estate stores";LootType=LootType.Blessed;}
  public HavenEstateRecovery(Serial serial):base(serial){}
  public static int Recover(Mobile p){var a=p.Account as Account;if(a==null)return 0;var stores=World.Items.Values.OfType<HavenEstateRecovery>().Where(x=>!x.Deleted&&x._account==a.Username&&x.Map==Map.Internal&&x.Parent==null).ToArray();foreach(var store in stores)p.BankBox.DropItem(store);return stores.Length;}
  public static void Initialize(){CommandSystem.Register("islandstores",AccessLevel.Player,e=>e.Mobile.SendMessage(Recover(e.Mobile)+" recovered island storage chest(s) delivered to your bank."));}
  public override bool IsAccessibleTo(Mobile p){return p?.Account is Account&&((Account)p.Account).Username==_account&&base.IsAccessibleTo(p);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_account);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_account=r.ReadString();}
 }
 public class HavenEstateStorage:Item {
  public Item Estate;
  public IHavenEstateStores Store { get { return Estate as IHavenEstateStores; } }
  public HavenEstateStorage(Item estate):base(0xE43){Estate=estate;Movable=false;Hue=0x972;Name="Connected estate stores";}
  public HavenEstateStorage(Serial serial):base(serial){}
  public override void OnDoubleClick(Mobile p){if(Store!=null&&Store.CanAccessStores(p,this))p.SendGump(new HavenEstateStorageGump(this,0));else p.SendMessage("These stores belong to the estate owner's account. Stand beside a storage chest to use them.");}
  public override bool OnDragDrop(Mobile p,Item item){return Store!=null&&Store.Deposit(p,this,item);}
  public void TargetDeposit(Mobile p){if(Store==null||!Store.CanAccessStores(p,this))return;p.SendMessage("Target an item in your backpack for the connected estate stores.");p.Target=new StoreTarget(this);}
  class StoreTarget:Target {readonly HavenEstateStorage _station;public StoreTarget(HavenEstateStorage s):base(3,false,TargetFlags.None){_station=s;}protected override void OnTarget(Mobile p,object target){p.SendMessage(_station.Store!=null&&_station.Store.Deposit(p,_station,target as Item)?"Stored. Available from every estate storage chest.":"Cannot store that item: check ownership, distance and capacity.");_station.OnDoubleClick(p);}}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Estate);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Estate=r.ReadItem();}
 }
 public class HavenEstateStorageGump:HavenMenuGump {
  readonly HavenEstateStorage _station;readonly Item[] _items;readonly int _page;
  public HavenEstateStorageGump(HavenEstateStorage station,int page):base(60,60){_station=station;var all=station.Store.StorageVault.Items.Where(i=>!i.Deleted).OrderBy(i=>i.Name??i.GetType().Name).ThenBy(i=>i.Serial.Value).ToArray();int pages=Math.Max(1,(all.Length+7)/8);_page=Math.Max(0,Math.Min(page,pages-1));_items=all.Skip(_page*8).Take(8).ToArray();AddBackground(0,0,610,465,3000);AddLabel(24,22,0,"CONNECTED ESTATE STORES | "+all.Length+" entries");AddLabel(24,55,0,"Private to your account. All estate chests share these items.");FlatButton(24,90,220,3,"Deposit from backpack");for(int i=0;i<_items.Length;i++){var item=_items[i];string name=item.Name??item.GetType().Name;if(name.Length>42)name=name.Substring(0,39)+"...";AddLabel(24,135+i*31,0,name+" x"+item.Amount);FlatButton(450,135+i*31,130,100+i,"Withdraw");}if(_page>0)FlatButton(24,408,120,1,"Previous");AddLabel(174,408,0,"Page "+(_page+1)+" / "+pages);if(_page+1<pages)FlatButton(316,408,120,2,"Next");FlatButton(450,408,130,0,"Close");}
  public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(info.ButtonID==0||_station.Store==null||!_station.Store.CanAccessStores(p,_station))return;int id=info.ButtonID;if(id==3){_station.TargetDeposit(p);return;}if(id>=100&&id<100+_items.Length)p.SendMessage(_station.Store.Withdraw(p,_station,_items[id-100])?"Item withdrawn.":"Item unavailable, or your backpack is full.");p.SendGump(new HavenEstateStorageGump(_station,_page+(id==1?-1:id==2?1:0)));}
 }
}
