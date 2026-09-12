using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
namespace Server.HavenPrototype {
 public partial class HavenAncientHellhound {
  Mobile _rider;HavenHellhoundMountItem _mountItem;
  public Mobile Rider {
   get{return _rider;}
   set{
    if(_rider==value)return;
    if(value==null){
     var rider=_rider;_rider=null;
     var location=rider.Location;var map=rider.Map;
     if(map==null||map==Map.Internal){location=rider.LogoutLocation;map=rider.LogoutMap;}
     Direction=rider.Direction;if(map!=null)MoveToWorld(location,map);
     if(_mountItem!=null&&!_mountItem.Deleted)_mountItem.Internalize();
    }else{
     if(_rider!=null)Rider=null;
     BaseMount.Dismount(value);
     if(_mountItem==null||_mountItem.Deleted)_mountItem=new HavenHellhoundMountItem(this);
     _mountItem.Hue=Hue;value.AddItem(_mountItem);_rider=value;value.Direction=Direction;Combatant=null;Internalize();
    }
   }
  }
  public override void OnDoubleClick(Mobile from) {
   if(Deleted||!Alive||IsDeadPet||!from.Alive||IsStabled)return;
   if(from.Map!=Map||!from.InRange(this,1)||!from.InLOS(this)){from.SendMessage("Stand beside your hellhound to ride it.");return;}
   if(!(Controlled&&ControlMaster==from)&&from.AccessLevel<AccessLevel.GameMaster){from.SendMessage("Only this hellhound's owner may ride it.");return;}
   if(from.Mounted){from.SendLocalizedMessage(1005583);return;}
   if(from.IsBodyMod&&!from.Body.IsHuman||from.Race==Race.Gargoyle||from.Flying){from.SendMessage("You cannot ride in your current form.");return;}
   if(from.HasTrade||!DesignContext.Check(from)||!BaseMount.CheckMountAllowed(from,true))return;
   if(Poisoned){from.SendMessage("Your hellhound is too ill to ride.");return;}
   Rider=from;
  }
  void ValidateRider(){
   if(_rider==null)return;
   if(_rider.Deleted||_mountItem==null||_mountItem.Deleted||_mountItem.Parent!=_rider){Rider=null;return;}
   if(Map!=Map.Internal)Internalize();
  }
  public void OnRiderDamaged(Mobile from,ref int amount,bool willKill){}
  public override bool OnBeforeDeath(){Rider=null;return base.OnBeforeDeath();}
  public override void OnDelete(){Rider=null;base.OnDelete();if(_mountItem!=null&&!_mountItem.Deleted)_mountItem.Delete();_mountItem=null;}
 }
 public class HavenHellhoundMountItem:Item,IMountItem {
  HavenAncientHellhound _pet;
  // Native Ancient Hell Hound riding art, also used by the standard ethereal.
  public HavenHellhoundMountItem(HavenAncientHellhound pet):base(0x3EC9){_pet=pet;Layer=Layer.Mount;Movable=false;Weight=0;Name="ancient hellhound";}
  public HavenHellhoundMountItem(Serial serial):base(serial){}
  public IMount Mount{get{return _pet;}}
  public override DeathMoveResult OnParentDeath(Mobile parent){if(_pet!=null)_pet.Rider=null;return DeathMoveResult.RemainEquiped;}
  public override void OnAfterDelete(){if(_pet!=null&&!_pet.Deleted&&_pet.Rider!=null)_pet.Rider=null;_pet=null;base.OnAfterDelete();}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_pet);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_pet=r.ReadMobile() as HavenAncientHellhound;}
 }
}
