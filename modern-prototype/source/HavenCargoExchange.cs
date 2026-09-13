using System;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using Server.Engines.Points;
using Server.Engines.RisingTide;
namespace Server.HavenPrototype {
 public static class HavenCargoExchange {
  public static void Initialize(){CommandSystem.Register("cargo",AccessLevel.Player,e=>{if(HavenPreview.Enabled&&e.Mobile is PlayerMobile&&e.Mobile.Alive)e.Mobile.SendGump(new CargoGump());});}
  public static int Value(Item i){var cargo=i as MaritimeCargo;if(cargo!=null)return cargo.CargoQuality==CargoQuality.Mythical?12500:cargo.CargoQuality==CargoQuality.Legendary?1050:cargo.CargoQuality==CargoQuality.Exalted?550:150;return i is Cannonball||i is FuseCord||i is PowderCharge?100:0;}
  public static bool Exchange(PlayerMobile p,Item item){
   if(!HavenPreview.Enabled||p==null||!p.Alive||item==null||item.Deleted||p.Backpack==null||!item.IsChildOf(p.Backpack)||!HavenResources.Accessible(p,item))return false;
   int value=Value(item);if(value==0)return false;int units=item is MaritimeCargo?1:100;if(item.Amount<units)return false;
   item.Consume(units);PointsSystem.RisingTide.AwardPoints(p,value);return true;
  }
  public class CargoGump:HavenStoneGump {
   public CargoGump():base(40,40){AddBackground(0,0,560,310,0xA28);AddLabel(24,20,0,"MARITIME CARGO EXCHANGE");AddHtml(24,58,510,130,"Turn recovered cargo into doubloons for pirate rewards.<BR>Grandmaster: 150 | Exalted: 550 | Legendary: 1,050<BR>Mythical: 12,500<BR><BR>Surplus: 100 cannonballs, powder charges OR fuse cords = 100 doubloons. Withdraw ledger supplies into your backpack first.",false,false);FlatButton(24,210,240,1,"Turn in cargo or supplies");FlatButton(280,210,250,2,"Browse pirate rewards");FlatButton(400,260,130,0,"Close");}
   public override void OnResponse(NetState s,RelayInfo r){if(!HavenPreview.Enabled||!s.Mobile.Alive)return;if(r.ButtonID==1){s.Mobile.SendMessage("Target cargo or a stack of at least 100 surplus supplies in your backpack.");s.Mobile.Target=new CargoTarget();}else if(r.ButtonID==2&&s.Mobile is PlayerMobile)s.Mobile.SendGump(new CargoRewardsGump((PlayerMobile)s.Mobile));}
  }
  public static readonly Type[] RewardTypes={typeof(MessageInABottle),typeof(SpecialFishingNet),typeof(FabledFishingNet),typeof(RuinedShipPlans),typeof(HavenHooksShield),typeof(XenrrFishingPole)};
  public static readonly string[] RewardNames={"SOS bottle","Special fishing net","Fabled fishing net","Random orc ship-plan fragment (1 of 8)","Hook's boarding shield","Xenrr's fishing pole"};
  public static readonly int[] Costs={300,600,2500,1500,5000,12000};
  public static bool Buy(PlayerMobile p,int choice){
   if(!HavenPreview.Enabled||p==null||!p.Alive||choice<0||choice>=Costs.Length||p.Backpack==null||PointsSystem.RisingTide.GetPoints(p)<Costs[choice])return false;
   var item=(Item)Activator.CreateInstance(RewardTypes[choice]);
   if(!p.Backpack.TryDropItem(p,item,false)){item.Delete();return false;}
   PointsSystem.RisingTide.DeductPoints(p,Costs[choice]);return true;
  }
  class CargoRewardsGump:HavenStoneGump {
   public CargoRewardsGump(PlayerMobile user):base(40,40){AddBackground(0,0,620,420,0xA28);AddLabel(24,20,0,"CARGO REWARDS | Doubloons");for(int n=0;n<Costs.Length;n++){int y=65+n*43;AddLabel(24,y,0,RewardNames[n]);AddLabel(390,y,0,Costs[n].ToString("N0"));FlatButton(480,y,110,10+n,"Choose");}AddLabel(24,320,0,"Your doubloons: "+PointsSystem.RisingTide.GetPoints(user).ToString("N0"));FlatButton(24,350,240,1,"More native pirate rewards");FlatButton(440,350,150,0,"Close");}
   public override void OnResponse(NetState s,RelayInfo r){if(!s.Mobile.Alive||!HavenPreview.Enabled)return;if(r.ButtonID==1&&s.Mobile is PlayerMobile)s.Mobile.SendGump(new RisingTideRewardGump(s.Mobile,(PlayerMobile)s.Mobile));else if(r.ButtonID>=10&&r.ButtonID<10+Costs.Length)s.Mobile.SendGump(new BuyConfirm(r.ButtonID-10));}
  }
  class BuyConfirm:HavenStoneGump {
   readonly int _choice;
   public BuyConfirm(int choice):base(50,50){_choice=choice;AddBackground(0,0,500,205,0xA28);AddLabel(24,25,0,RewardNames[choice]);AddLabel(24,65,0,"Purchase for "+Costs[choice].ToString("N0")+" doubloons?");FlatButton(24,125,205,1,"Confirm purchase");FlatButton(250,125,205,0,"Cancel");}
   public override void OnResponse(NetState s,RelayInfo r){if(r.ButtonID==1)s.Mobile.SendMessage(Buy(s.Mobile as PlayerMobile,_choice)?"Your reward is in your backpack.":"Purchase unavailable. Check your doubloons and backpack space.");}
  }
  class CargoTarget:Target {
   public CargoTarget():base(-1,false,TargetFlags.None){}
   protected override void OnTarget(Mobile from,object selected){var item=selected as Item;if(item==null||item.Deleted||from.Backpack==null||!item.IsChildOf(from.Backpack)||Value(item)==0||!HavenResources.Accessible(from,item)){from.SendMessage("Choose maritime cargo or cannon supplies in your backpack.");return;}int units=item is MaritimeCargo?1:100;if(item.Amount<units){from.SendMessage("You need at least 100 of that supply.");return;}from.SendGump(new Confirm(item,Value(item),units));}
  }
  class Confirm:HavenStoneGump {
   readonly Item _item;readonly int _value;
   public Confirm(Item item,int value,int units):base(70,70){_item=item;_value=value;AddBackground(0,0,440,200,0xA28);AddLabel(24,25,0,"Confirm maritime turn-in");AddLabel(24,65,0,"Exchange "+units+" item(s) for "+value+" doubloons?");FlatButton(24,125,180,1,"Confirm exchange");FlatButton(230,125,180,0,"Cancel");}
   public override void OnResponse(NetState s,RelayInfo r){if(r.ButtonID!=1)return;bool ok=_item!=null&&!_item.Deleted&&Value(_item)==_value&&Exchange(s.Mobile as PlayerMobile,_item);s.Mobile.SendMessage(ok?"Doubloons credited. Use [cargo to browse rewards.":"Turn-in unavailable; keep the unchanged items in your backpack.");}
  }
 }
 public class HavenHooksShield:MetalShield {
  public static void Initialize(){if(HavenPreview.Enabled)Server.Timer.DelayCall(TimeSpan.Zero,()=>{    AddReward(typeof(HavenHooksShield),0x1B7B,5000);
    AddReward(typeof(MessageInABottle),0x99F,300);
    AddReward(typeof(SpecialFishingNet),0xDCA,600);
    AddReward(typeof(FabledFishingNet),0xDCA,2500);
    AddReward(typeof(XenrrFishingPole),0xDBF,12000);
    AddReward(typeof(RuinedShipPlans),5360,1500);});}
  static void AddReward(Type type,int art,int price){if(!RisingTideRewardGump.Rewards.Exists(r=>r.Type==type))RisingTideRewardGump.Rewards.Add(new CollectionItem(type,art,0,0,price));}
  [Constructable]public HavenHooksShield(){Name="Hook's boarding shield";Hue=0x972;Attributes.DefendChance=15;Attributes.RegenStam=3;Attributes.RegenMana=2;Attributes.LowerManaCost=5;HavenAdvancedGear.Attach(this,5);}
  public HavenHooksShield(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
}



