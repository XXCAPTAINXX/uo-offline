using System;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
namespace Server.HavenPrototype
{
 public static class HavenStoryGifts
 {
  static string Key(Mobile p,string gift){return "Haven.StoryGift:"+p.Serial.Value+":"+gift;}
  public static bool Claimed(Mobile p,string gift){var a=p.Account as Account;return a!=null&&a.GetTag(Key(p,gift))=="1";}
  public static bool ClaimFountain(Mobile p)
  {
   if(!HavenMarks.CanUse(p)||HavenBeaconQuest.Phase(p)!=5||HavenBeaconQuest.NearbyJenna(p)==null||Claimed(p,"fountain")||p.Backpack==null)return false;
   var deed=new HavenExpeditionFountainDeed();if(!p.Backpack.TryDropItem(p,deed,false)){deed.Delete();return false;}
   ((Account)p.Account).SetTag(Key(p,"fountain"),"1");p.SendMessage("Jenna gives you an expedition fountain deed. Place it in your house; all ordinary bandages deposited are enhanced immediately.");return true;
  }
  public static bool ClaimMount(Mobile p)
  {
   if(!HavenMarks.CanUse(p)||HavenBeaconQuest.Phase(p)<1||HavenBeaconQuest.NearbyJenna(p)==null||Claimed(p,"mount"))return false;
   if(p.Backpack==null||p.Followers>=p.FollowersMax){p.SendMessage("Make room in your backpack and free one follower slot for your horse.");return false;}
   Point3D point;if(!HavenPreview.FindLanding(new HavenPreview.Destination("Starter horse",p.Map,p.X,p.Y,p.Z),out point))return false;
   var horse=new HavenStoryHorse(p);var treat=new HavenStoryBondingApple(p,horse);
   if(!p.Backpack.CheckHold(p,treat,false)||!horse.SetControlMaster(p)){treat.Delete();horse.Delete();return false;}
   horse.Owners.Add(p);horse.ControlTarget=p;horse.ControlOrder=OrderType.Follow;horse.MoveToWorld(point,p.Map);p.Backpack.DropItem(treat);
   ((Account)p.Account).SetTag(Key(p,"mount"),"1");
   p.SendMessage(0x59B,"Jenna: A gentle horse for the road. Drag the bonding apple from your pack onto your horse to bond immediately. Double-click your horse to ride; say 'all follow me' to call it. A bonded pet can be resurrected if it dies.");return true;
  }
  public static bool ClaimCape(Mobile p)
  {
   if(!HavenMarks.CanUse(p)||HavenBeaconQuest.Phase(p)<1||HavenBeaconQuest.NearbyJenna(p)==null||Claimed(p,"cape")||p.Backpack==null)return false;
   var cape=new HavenEvolvingCape();cape.Progress.Owner=p;HavenStarterGear.Apply(cape);
   if(!p.Backpack.TryDropItem(p,cape,false)){cape.Delete();return false;}
   ((Account)p.Account).SetTag(Key(p,"cape"),"1");
   p.SendMessage(0x59B,"Jenna: Wear this cape while defeating hostile monsters. It earns experience and grows to level 20, improving luck, regeneration, defenses and attributes. Hover over it to check progress. Practice golems do not give kill XP.");return true;
  }
 }
 public class HavenStoryHorse:Horse
 {
  public Mobile GiftOwner;
  public override TrainingDefinition TrainingDefinition{get{return HavenPetTrainingBridge.Definition(this);}}
  public HavenStoryHorse(Mobile owner):base("a Haven trail horse"){GiftOwner=owner;MinTameSkill=0;SetStr(35);SetDex(60);SetInt(10);SetHits(40);SetDamage(2,3);ControlSlotsMin=1;ControlSlotsMax=5;}
  public HavenStoryHorse(Serial s):base(s){}
  public override bool OnDragDrop(Mobile from,Item dropped)
  {
   var apple=dropped as HavenStoryBondingApple;if(apple==null)return base.OnDragDrop(from,dropped);
   if(from!=GiftOwner||ControlMaster!=from||apple.Owner!=from||apple.Horse!=this||IsBonded||IsDeadPet||!Alive||from.Map!=Map||!from.InRange(this,2)||!from.InLOS(this))
   {from.SendMessage("This treat is only for your living, unbonded quest horse. Stand beside it while it is under your control.");return false;}
   IsBonded=true;Loyalty=MaxLoyalty;apple.Delete();from.SendMessage("Your Haven trail horse has bonded with you.");InvalidateProperties();return true;
  }
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(GiftOwner);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();GiftOwner=r.ReadMobile();ControlSlotsMin=1;ControlSlotsMax=5;}
 }
 public class HavenStoryBondingApple:Item
 {
  public Mobile Owner;public HavenStoryHorse Horse;
  public HavenStoryBondingApple(Mobile owner,HavenStoryHorse horse):base(0x9D0){Owner=owner;Horse=horse;Name="Jenna's bonding apple";LootType=LootType.Blessed;Weight=0.1;}
  public HavenStoryBondingApple(Serial s):base(s){}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Feed to your Haven trail horse to bond immediately.");list.Add("Only works for the horse and owner Jenna gave it to.");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Owner);w.Write(Horse);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Owner=r.ReadMobile();Horse=r.ReadMobile() as HavenStoryHorse;}
 }
}
