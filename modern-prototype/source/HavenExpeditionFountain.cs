using System;
using Server.Items;
namespace Server.HavenPrototype
{
 [Flipable(0x2AC0,0x2AC3)]
 public class HavenExpeditionFountain:BaseAddonContainer
 {
  [Constructable] public HavenExpeditionFountain():base(0x2AC0){Name="Expedition fountain of life";}
  public HavenExpeditionFountain(Serial s):base(s){}
  public override BaseAddonContainerDeed Deed{get{return new HavenExpeditionFountainDeed();}}
  public override int DefaultGumpID{get{return 0x484;}}
  public override int DefaultDropSound{get{return 66;}}
  public override int DefaultMaxItems{get{return 125;}}
  public override bool OnDragLift(Mobile from){return false;}
  bool Accepted(Item item){return item!=null&&(item.GetType()==typeof(Bandage)||item is EnhancedBandage);}
  public override bool OnDragDrop(Mobile from,Item item){if(!Accepted(item)){from.SendMessage("Only ordinary or enhanced bandages belong in this fountain.");return false;}bool result=base.OnDragDrop(from,item);if(result)ConvertBandages();return result;}
  public override bool OnDragDropInto(Mobile from,Item item,Point3D p){if(!Accepted(item)){from.SendMessage("Only ordinary or enhanced bandages belong in this fountain.");return false;}bool result=base.OnDragDropInto(from,item,p);if(result)ConvertBandages();return result;}
  public void ConvertBandages()
  {
   foreach(Item item in Items.ToArray())if(item.GetType()==typeof(Bandage))
   {var enhanced=new EnhancedBandage(item.Amount);item.Delete();DropItem(enhanced);}
  }
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Instantly enhances every ordinary bandage deposited.");list.Add("One-for-one conversion. No charges or recharge timer.");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenExpeditionFountainDeed:BaseAddonContainerDeed
 {
  [Constructable] public HavenExpeditionFountainDeed(){Name="Expedition fountain of life deed";LootType=LootType.Blessed;}
  public HavenExpeditionFountainDeed(Serial s):base(s){}
  public override BaseAddonContainer Addon{get{return new HavenExpeditionFountain();}}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Enhances all deposited ordinary bandages immediately.");list.Add("No charges or recharge timer. Place in your house.");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
}
