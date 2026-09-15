using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
using Server.Engines.RisingTide;
using Server.Engines.Points;
public static class CargoBulkSmoke {
 public static void Run(Action<string> log){
 var p=new PlayerMobile();p.AddItem(new Backpack());
 try{
 var bag=new Bag();p.Backpack.DropItem(bag);
 var cargo=new MaritimeCargo(CargoQuality.Legendary);bag.DropItem(cargo);
 var balls=new Cannonball();balls.Amount=250;p.Backpack.DropItem(balls);
 var gold=new Gold(55);bag.DropItem(gold);
 var offers=HavenCargoExchange.PreviewAll(p);double before=PointsSystem.RisingTide.GetPoints(p);
 int paid=HavenCargoExchange.ExchangeAll(p,offers);
 if(paid!=1250||!cargo.Deleted||balls.Amount!=50||gold.Amount!=55||PointsSystem.RisingTide.GetPoints(p)!=before+1250)throw new Exception("Bulk payout or leftovers wrong");
 if(HavenCargoExchange.ExchangeAll(p,offers)!=0)throw new Exception("Repeated payout");
 balls.Amount=150;offers=HavenCargoExchange.PreviewAll(p);balls.Amount=50;
 if(HavenCargoExchange.ExchangeAll(p,offers)!=0||balls.Amount!=50)throw new Exception("Changed snapshot consumed");
 log("PASS nested cargo, supply batches, exact points, leftovers, unrelated items, replay and changed snapshot");
 }finally{p.Delete();}
 }
}
