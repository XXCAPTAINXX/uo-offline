using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Server.Items;
using Server.Targeting;

namespace Server.HavenPrototype
{
 public static class HavenStorageTools
 {
  public static string Name(Item item){return item.Name ?? Regex.Replace(item.GetType().Name,"([a-z])([A-Z])","$1 $2");}
  public static readonly string[] Categories={"All","Resources","Gear","Supplies","Books","Containers","Other"};
  public static int Category(Item i){if(i is Container)return 5;if(i is BaseWeapon||i is BaseArmor||i is BaseClothing||i is BaseJewel||i is Spellbook)return 2;if(i is Spellbook||i is TreasureMap||i is SpecialScroll||i.GetType().Name.IndexOf("Book",StringComparison.OrdinalIgnoreCase)>=0||i.GetType().Name.IndexOf("Ledger",StringComparison.OrdinalIgnoreCase)>=0)return 4;if(HavenResources.Types.Contains(i.GetType()))return 1;if(i is Gold||i is BasePotion||i is Food||i is Bandage)return 3;return 6;}
  public static readonly string[] GearFilters={"All gear","Weapons","Armor","Shields","Clothing","Jewelry","Spellbooks"};
  public static int GearKind(Item i){if(i is BaseShield)return 3;if(i is BaseWeapon)return 1;if(i is BaseArmor)return 2;if(i is BaseClothing)return 4;if(i is BaseJewel)return 5;if(i is Spellbook)return 6;return 0;}
  public static bool Matches(Item i,string search){var text=Name(i)+" "+i.GetType().Name+" "+Categories[Category(i)];return (search??"").Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries).All(word=>text.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0);}
  public static Item[] Query(Container root,Container folder,string search,int category,int sort,int gear)
  {
   bool searching=!String.IsNullOrWhiteSpace(search);
   var query=Contents(searching?root:folder,searching||folder==root).Where(i=>(category==0||Category(i)==category)&&(category!=2||gear==0||GearKind(i)==gear)&&Matches(i,search));
   IOrderedEnumerable<Item> ordered;
   if(sort==1)ordered=query.OrderByDescending(i=>i.Amount).ThenBy(Name);
   else if(sort==2)ordered=query.OrderBy(Category).ThenBy(GearKind).ThenBy(Name);
   else if(sort==3)ordered=query.OrderByDescending(i=>i.Weight*i.Amount).ThenBy(Name);
   else if(sort==4)ordered=query.OrderByDescending(Name);
   else ordered=query.OrderBy(Name);
   return ordered.ThenBy(i=>i.Serial.Value).ToArray();
  }
  public static bool Browse(Container c){var locked=c as LockableContainer;var trapped=c as TrapableContainer;return c!=null&&!c.Deleted&&c.GetType().Namespace=="Server.Items"&&(locked==null||!locked.Locked)&&(trapped==null||trapped.TrapType==TrapType.None);}
  public static bool Within(Item item,Container root){if(item==null||item.Deleted||root==null||root.Deleted||!item.IsChildOf(root))return false;for(var parent=item.Parent as Item;parent!=null&&parent!=root;parent=parent.Parent as Item)if(!Browse(parent as Container))return false;return true;}
  public static IEnumerable<Item> Contents(Container root,bool recursive){foreach(var i in root.Items.ToArray()){if(i.Deleted)continue;yield return i;var c=i as Container;if(recursive&&Browse(c))foreach(var child in Contents(c,true))yield return child;}}
  public static bool Protected(Item i){return i==null||i.Deleted||!i.Movable||i.LootType==LootType.Blessed||i.LootType==LootType.Newbied||i is BaseWeapon||i is BaseArmor||i is BaseClothing||i is BaseJewel||i is Spellbook||i is BaseTool||i is BaseInstrument||HavenEquipmentEvolution.Find(i)!=null||HavenAdvancedGear.Find(i)!=null||i.GetType().Name.IndexOf("Ledger",StringComparison.OrdinalIgnoreCase)>=0||i.GetType().Name.IndexOf("Book",StringComparison.OrdinalIgnoreCase)>=0||i.GetType().Name.IndexOf("Codex",StringComparison.OrdinalIgnoreCase)>=0||i.GetType().Name.IndexOf("Pet",StringComparison.OrdinalIgnoreCase)>=0;}
  static bool KeepSupplies(Item i){return i is Bandage||i is Arrow||i is Bolt||i is Key||i is Lockpick||new[]{"Travel","Rune","Compass","Dye","Quiver","Whistle"}.Any(word=>i.GetType().Name.IndexOf(word,StringComparison.OrdinalIgnoreCase)>=0);}
  public static Item[] DepositCandidates(HavenCompanion c){return c?.Backpack==null?new Item[0]:Contents(c.Backpack,true).Where(i=>!(i is Container)&&!Protected(i)&&!KeepSupplies(i)&&Within(i,c.Backpack)&&!ProtectedParent(i,c.Backpack)).ToArray();}
  static bool ProtectedParent(Item item,Container root){for(var parent=item.Parent as Item;parent!=null&&parent!=root;parent=parent.Parent as Item)if(Protected(parent))return true;return false;}
  public static int Deposit(Mobile p,HavenCompanion c,HavenEstateStorage station){if(c==null||!c.CanOpenPack(p)||station?.Store==null||!station.Store.CanAccessStores(p,station))return -1;int count=0;foreach(var item in DepositCandidates(c)){if(!c.CanOpenPack(p)||!station.Store.CanAccessStores(p,station))break;if(Within(item,c.Backpack)&&!Protected(item)&&station.Store.StorageVault.TryDropItem(p,item,false))count++;}return count;}
  public static int DepositSnapshot(Mobile p,HavenCompanion c,HavenEstateStorage station,IDictionary<Item,int> snapshot)
  {
   if(c==null||!c.CanOpenPack(p)||station?.Store==null||!station.Store.CanAccessStores(p,station))return -1;
   var allowed=new HashSet<Item>(DepositCandidates(c));int count=0;
   foreach(var item in (snapshot??new Dictionary<Item,int>()).Keys)
   {if(!c.CanOpenPack(p)||!station.Store.CanAccessStores(p,station))break;if(allowed.Contains(item)&&item.Amount==snapshot[item]&&Within(item,c.Backpack)&&!Protected(item)&&!ProtectedParent(item,c.Backpack)&&station.Store.StorageVault.TryDropItem(p,item,false))count++;}
   return count;
  }
  public static bool Withdraw(Mobile p,HavenEstateStorage station,Item item){return station?.Store!=null&&station.Store.CanAccessStores(p,station)&&Within(item,station.Store.StorageVault)&&p.Backpack!=null&&p.Backpack.TryDropItem(p,item,false);}
  public static bool WithdrawAmount(Mobile p,HavenEstateStorage station,Item item,int amount)
  {
   if(station?.Store==null||!station.Store.CanAccessStores(p,station)||!Within(item,station.Store.StorageVault)||p.Backpack==null||amount<1||amount>item.Amount)return false;
   if(amount==item.Amount)return Withdraw(p,station,item);
   if(!item.Stackable)return false;
   int original=item.Amount;var remainder=Mobile.LiftItemDupe(item,amount);
   if(remainder==null)return false;
   if(p.Backpack.TryDropItem(p,item,false))return true;
   item.Amount=original;remainder.Delete();return false;
  }
  public static HavenCompanion NearbyCompanion(Mobile p){return World.Mobiles.Values.OfType<HavenCompanion>().Where(c=>!c.Deleted&&c.IsOwner(p)&&c.CanOpenPack(p)).OrderBy(c=>c.GetDistanceToSqrt(p)).FirstOrDefault();}
  public static void ReportDeposit(Mobile p,HavenCompanion c,HavenEstateStorage station){int n=Deposit(p,c,station);p.SendMessage(n<0?"Stand beside your house storage with your companion nearby.":"Stored "+n+" loot stacks/items. Gear, books, ledgers, tools and pet items stay with your companion; anything that did not fit stays too.");}
  public static void TargetHouse(Mobile p,HavenCompanion c){if(!c.CanOpenPack(p)){p.SendMessage("Your companion must be nearby and available.");return;}p.SendMessage("Target a connected house-storage chest. Gear and utility items will stay with your companion.");p.Target=new HouseTarget(c);}
  sealed class HouseTarget:Target {readonly HavenCompanion _c;public HouseTarget(HavenCompanion c):base(3,false,TargetFlags.None){_c=c;}protected override void OnTarget(Mobile p,object o){var station=o as HavenEstateStorage;if(station?.Store!=null&&station.Store.CanAccessStores(p,station)&&_c.CanOpenPack(p))p.SendGump(new HavenStorageDepositGump(p,_c,station));else p.SendMessage("Stand beside your house storage with your companion nearby.");}}
 }
}
