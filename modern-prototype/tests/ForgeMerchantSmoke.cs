using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class ForgeMerchantSmoke
{
 public static void Run(Action<string> log)
 {
  var p=new PlayerMobile{Player=true,AccessLevel=AccessLevel.GameMaster};p.AddItem(new Backpack());p.MoveToWorld(new Point3D(3400,2400,0),Map.Felucca);p.Skills.Blacksmith.Base=80;
  var forge=new HavenSmallSoulForge();forge.MoveToWorld(p.Location,p.Map);
  for(int i=0;i<HavenAbyssArtifice.Recipes.Length;i++)
  {
   var r=HavenAbyssArtifice.Recipes[i];var gear=new Broadsword();p.Backpack.DropItem(gear);
   var essence=(Item)Activator.CreateInstance(r.Essence);essence.Amount=8;p.Backpack.DropItem(essence);
   var first=(Item)Activator.CreateInstance(r.First);first.Amount=2;p.Backpack.DropItem(first);
   if(HavenAbyssArtifice.Apply(forge,p,gear,i)||essence.Deleted||essence.Amount!=8||first.Deleted)throw new Exception("Missing material consumed resources");
   var second=(Item)Activator.CreateInstance(r.Second);second.Amount=2;p.Backpack.DropItem(second);
   int before=gear.Attributes[r.Attribute];
   if(!HavenAbyssArtifice.Apply(forge,p,gear,i)||gear.Attributes[r.Attribute]!=before+r.Bonus||!essence.Deleted||!first.Deleted||!second.Deleted)throw new Exception("Recipe failed "+i);
   if(HavenAbyssArtifice.Apply(forge,p,gear,i)||gear.Items.OfType<HavenAbyssAttunement>().Count()!=1||gear.TotalItems!=0)throw new Exception("Repeated attunement or inventory slot");
   gear.Delete();
  }
  var special=new CorgulsEnchantedSash();p.Backpack.DropItem(special);if(HavenAbyssArtifice.Eligible(p,special))throw new Exception("Evolving artifact allowed");special.Delete();
  var ordinary=new Broadsword();p.Backpack.DropItem(ordinary);p.Skills.Blacksmith.Base=79.9;if(HavenAbyssArtifice.Apply(forge,p,ordinary,0))throw new Exception("Low skill accepted");
  p.MoveToWorld(new Point3D(3408,2400,0),p.Map);if(HavenAbyssArtifice.CanUse(forge,p))throw new Exception("Distant forge accepted");ordinary.Delete();forge.Delete();
  log("PASS all 11 recipes, exact material costs, missing ingredient atomicity, repeat protection, virtual marker and evolving artifact exclusion");
  var reward=HavenBossExtras.ForgeReward(0.049);if(!(reward is HavenSmallSoulForgeDeed)||HavenBossExtras.ForgeReward(0.05)!=null)throw new Exception("Forge five percent boundary");reward.Delete();
  var vendor=new Alchemist();vendor.MoveToWorld(p.Location,p.Map);HavenMerchantStock.Refresh(vendor);
  var stock=vendor.GetBuyInfo().OfType<GenericBuyInfo>().First(x=>x.GetDisplayEntity() is Item && ((Item)x.GetDisplayEntity()).Stackable);
  if(stock.Amount<1000)throw new Exception("Stacked supply quantity");stock.Amount=1;
  if(!vendor.OnBuyItems(p,new List<BuyItemResponse>{new BuyItemResponse(stock.GetDisplayEntity().Serial,1)})||stock.Amount<1000)throw new Exception("Purchase did not refill immediately");
  int large=5000;if(stock.EconomyItem)BaseVendor.EconomyStockAmount=large;stock.Amount=large;HavenMerchantStock.Refresh(vendor);if(stock.Amount<large)throw new Exception("Larger stock reduced");
  vendor.Delete();p.Delete();log("PASS native merchant purchase refills immediately; bulk supply floor and larger stock preserved");
 }
}
