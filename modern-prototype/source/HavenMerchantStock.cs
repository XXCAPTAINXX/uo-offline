using System;
using Server.Items;
using Server.Mobiles;
namespace Server.HavenPrototype
{
 public static class HavenMerchantStock
 {
  public static void Refresh(BaseVendor vendor)
  {
   if(!HavenPreview.Enabled||vendor==null||vendor.Deleted||!vendor.IsActiveSeller)return;
   BaseVendor.EconomyStockAmount=Math.Max(1000,BaseVendor.EconomyStockAmount);
   foreach(var entry in vendor.GetBuyInfo())
   {
    var stock=entry as GenericBuyInfo;if(stock==null||stock.Type==null)continue;
    var display=stock.GetDisplayEntity() as Item;
    int minimum=display!=null&&display.Stackable?1000:display!=null?100:10;
    stock.Amount=Math.Max(stock.Amount,Math.Max(stock.MaxAmount,minimum));
   }
  }
 }
}
