using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;
namespace Server.HavenPrototype {
 public class HavenBondingPotion:Item {
  public HavenBondingPotion():base(0xF0E){Name="pet bonding potion";Hue=0x489;Weight=1;}
  public HavenBondingPotion(Serial s):base(s){}
  public override void OnDoubleClick(Mobile p){if(p.Backpack!=null&&IsChildOf(p.Backpack))p.Target=new BondTarget(this);}
  internal bool ApplyTo(Mobile p,BaseCreature pet){if(Deleted||!p.Alive||p.Backpack==null||!IsChildOf(p.Backpack)||pet==null||pet.Deleted||!pet.Alive||pet.IsDeadPet||!pet.Controlled||pet.ControlMaster!=p||pet.Summoned||pet.IsBonded||!pet.IsBondable||pet.Map!=p.Map||!p.InRange(pet,3)||!p.InLOS(pet))return false;pet.IsBonded=true;pet.Loyalty=BaseCreature.MaxLoyalty;p.SendMessage(pet.Name+" is now bonded to you.");Delete();return true;}
  class BondTarget:Target {readonly HavenBondingPotion _p;public BondTarget(HavenBondingPotion p):base(3,false,TargetFlags.None){_p=p;}protected override void OnTarget(Mobile p,object target){if(!_p.ApplyTo(p,target as BaseCreature))p.SendMessage("Choose your living, unbonded pet nearby. The potion was kept.");}}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenPetLeash:Item {
  public HavenPetLeash():base(0x14F8){Name="reusable pet shrinking leash";Weight=1;LootType=LootType.Blessed;}
  public HavenPetLeash(Serial s):base(s){}
  public override void OnDoubleClick(Mobile p){HavenFreePetHitchingPost.BeginShrink(p,this);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Reusable: shrink your living pet from your backpack");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenHouseHitchingPost:HavenFreePetHitchingPost {
  public HavenHouseHitchingPost(){Name="house pet shrinking post";Movable=true;Weight=10;}
  public HavenHouseHitchingPost(Serial s):base(s){}
  internal bool CanUse(Mobile p){var house=BaseHouse.FindHouseAt(this);return Parent==null&&Map==p.Map&&house!=null&&house.IsCoOwner(p);}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Place in a house you own or co-own");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public static class HavenTamingSupplies {
  public static int SearchRolls(int minutes){int bonus=minutes==60?125:minutes==30?115:minutes==15?110:100;return Math.Max(1,Math.Min(15,minutes*bonus/500));}
  public static Item Bonus(double roll){if(roll<0.08)return new HavenBondingPotion();if(roll<0.16)return new HavenPetLeash();if(roll<0.25)return new PowerScroll((SkillName)new[]{(int)SkillName.Wrestling,(int)SkillName.Tactics,(int)SkillName.Anatomy,(int)SkillName.Healing,(int)SkillName.MagicResist}[Utility.Random(5)],105);if(roll<0.27)return new HavenHouseHitchingPost();return null;}
 }
}
