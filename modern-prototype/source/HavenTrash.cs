using System;
using System.Linq;
using Server;
using Server.Commands;
using Server.Items;
using Server.Engines.Points;
using System.Collections.Generic;
namespace Server.HavenPrototype {
 public class HavenTrashBag : TrashBarrel {
  public HavenTrashBag(){ItemID=0xE76;Name="trash bag";Hue=0x3B2;Movable=true;Weight=1;LootType=LootType.Blessed;MaxItems=49;}
  public HavenTrashBag(Serial serial):base(serial){}
  public override int DefaultGumpID{get{return 0x3D;}}
  public virtual bool CanUse(Mobile p){return p!=null&&p.Alive&&p.Backpack!=null&&IsChildOf(p.Backpack)&&HavenResources.Accessible(p,this);}
  public static bool Protected(Item item){if(item.LootType==LootType.Blessed||item.LootType==LootType.Newbied||item.Insured)return true;var c=item as Container;return c!=null&&c.FindItemsByType(typeof(Item),true).Any(Protected);}
  private bool Accept(Mobile p,Item item){if(!CanUse(p))return false;if(Protected(item)){p.SendMessage("Remove blessed, insured or newbie items before discarding this.");return false;}if(TotalItems+item.TotalItems+1>=50){p.SendMessage("Trash holds up to 49 items. Wait for it to empty or remove something.");return false;}return true;}
  public override bool OnDragDrop(Mobile p,Item item){return Accept(p,item)&&base.OnDragDrop(p,item);}
  public override bool OnDragDropInto(Mobile p,Item item,Point3D point){return Accept(p,item)&&base.OnDragDropInto(p,item,point);}
  public override void OnDoubleClick(Mobile p){if(CanUse(p))base.OnDoubleClick(p);}
  public override bool AddCleanupItem(Mobile p,Item item){
   bool added=base.AddCleanupItem(p,item);
   var pending=m_Cleanup.Where(x=>x.mobiles==p&&!x.confirm).Sum(x=>x.points);
   p.SendMessage(added?"Pending CUB points: "+pending.ToString("N0")+". Credited when the trash empties.":"This item has no native CUB point value.");
   return added;
  }
  public override void OnItemRemoved(Item item){
   base.OnItemRemoved(item);
   if(m_Cleanup!=null)m_Cleanup.RemoveAll(x=>!x.confirm&&(x.items==item||(x.items!=null&&x.items.IsChildOf(item))));
  }
  public override void OnSubItemRemoved(Item item){
   base.OnSubItemRemoved(item);
   if(m_Cleanup!=null)m_Cleanup.RemoveAll(x=>!x.confirm&&(x.items==item||(x.items!=null&&x.items.IsChildOf(item))));
  }
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Trash empties 3 minutes after the last deposit");list.Add("Retrieve mistakes before it empties; maximum 49 items");list.Add("Eligible trash earns Clean Up Britannia points when emptied");}
  public override void Serialize(GenericWriter w){
   base.Serialize(w);w.Write(1);WritePendingCleanup(w);
  }
  internal void WritePendingCleanup(GenericWriter w){
   var pending=m_Cleanup.Where(x=>!x.confirm&&x.items!=null&&!x.items.Deleted&&x.items.IsChildOf(this)&&x.mobiles!=null&&!x.mobiles.Deleted).ToList();
   w.Write(pending.Count);foreach(var entry in pending){w.Write(entry.mobiles);w.Write(entry.items);w.Write(entry.points);}
  }
  public override void Deserialize(GenericReader r){
   base.Deserialize(r);int version=r.ReadInt();
   if(version>=1)ReadPendingCleanup(r);
  }
  internal void ReadPendingCleanup(GenericReader r){
   {int count=r.ReadInt();for(int i=0;i<count;i++){var owner=r.ReadMobile();var item=r.ReadItem();double points=r.ReadDouble();if(owner!=null&&item!=null&&points>0)m_Cleanup.Add(new CleanupArray{mobiles=owner,items=item,serials=item.Serial,points=points});}}
  }
 }
 public class HavenPublicTrashChest : HavenTrashBag {
  public HavenPublicTrashChest(){ItemID=0xE41;Name="Haven public trash chest";Movable=false;}
  public HavenPublicTrashChest(Serial serial):base(serial){}
  public override int DefaultGumpID{get{return 0x42;}}
  public override bool CanUse(Mobile p){return p!=null&&p.Alive&&p.Map==Map&&p.InRange(GetWorldLocation(),2)&&p.InLOS(this);}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public static class HavenTrash {
  public static void Initialize(){CommandSystem.Register("trashbag",AccessLevel.Player,e=>Claim(e.Mobile));EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Timer.DelayCall(TimeSpan.FromSeconds(3),Ensure);};}
  public static bool Claim(Mobile p){if(!HavenPreview.Enabled||p==null||!p.Alive||p.Backpack==null)return false;if(p.Backpack.FindItemsByType(typeof(HavenTrashBag),true).Any()){p.SendMessage("You already have a trash bag in your pack.");return false;}var bag=new HavenTrashBag();if(!p.Backpack.TryDropItem(p,bag,false)){bag.Delete();return false;}p.SendMessage("Trash bag added. Contents empty three minutes after the last deposit.");return true;}
  public static void Ensure(){if(!HavenPreview.Enabled)return;var chest=World.Items.Values.OfType<HavenPublicTrashChest>().FirstOrDefault(x=>!x.Deleted&&x.Map==Map.Trammel);if(chest!=null&&chest.Hue==0x48F&&chest.X==3504&&chest.Y==2576&&chest.Z==18)return;var location=new Point3D(3504,2576,18);if(!Map.Trammel.CanFit(location,16,false,false)){Console.WriteLine("Haven trash: requested tile is blocked; existing chest retained.");return;}if(chest==null)chest=new HavenPublicTrashChest();chest.Hue=0x48F;chest.Name="PUBLIC TRASH - empties after 3 minutes";chest.MoveToWorld(location,Map.Trammel);}

 }
}


