using System;
using System.Linq;
using Server.Items;
using Server.Mobiles;
namespace Server.HavenPrototype {
 public class HavenDockCargoTrader:BaseCreature {
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)Ensure();};}
  public static HavenDockCargoTrader Ensure(){
   var old=World.Mobiles.Values.OfType<HavenDockCargoTrader>().FirstOrDefault(m=>!m.Deleted);if(old!=null)return old;
   if(!World.Items.Values.OfType<HavenIslandFoundation>().Any(i=>!i.Deleted))return null;
   var map=Map.Trammel;var point=new Point3D(4213,2938,0);
   for(int z=0;z<=10;z++){var candidate=new Point3D(4213,2938,z);if(map.CanFit(candidate,16,true,true)){point=candidate;break;}}
   if(!map.CanFit(point,16,true,true)){Console.WriteLine("Dock cargo trader: arrival tile occupied");return null;}
   var trader=new HavenDockCargoTrader();trader.Home=point;trader.MoveToWorld(point,map);Console.WriteLine("Dock cargo trader ready: "+trader.Serial+" at "+point);return trader;
  }
  [Constructable]public HavenDockCargoTrader():base(AIType.AI_Vendor,FightMode.None,10,1,0.2,0.4){Name="Bram";Title="the cargo quartermaster";Body=0x190;Hue=Utility.RandomSkinHue();Blessed=true;CantWalk=true;RangeHome=0;AddItem(new FancyShirt{Hue=0x53D});AddItem(new ShortPants{Hue=0x972});AddItem(new Boots());AddItem(new Bandana{Hue=0x53D});}
  public HavenDockCargoTrader(Serial s):base(s){}
  public override void OnDoubleClick(Mobile from){if(HavenPreview.Enabled&&from.Alive&&from.Map==Map&&from.InRange(this,3)&&from.InLOS(this))from.SendGump(new HavenCargoExchange.CargoGump());else from.SendMessage("Stand beside the dock quartermaster to trade cargo.");}
  public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Double-click: cargo and surplus supplies for doubloons");list.Add("Fishing gear, ship plans and pirate rewards");}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Blessed=true;CantWalk=true;RangeHome=0;}
 }
}

