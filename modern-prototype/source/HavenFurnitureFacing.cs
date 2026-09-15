using System;
using System.IO;
using System.Linq;
namespace Server.HavenPrototype {
 public static class HavenFurnitureFacing {
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled&&File.Exists("HAVEN-FURNITURE-FACING")){int count=Apply();World.Save(false,false);File.Delete("HAVEN-FURNITURE-FACING");Console.WriteLine("Furniture facing corrected: "+count);}};}
  public static int Apply(){
   int count=0;
   foreach(var house in World.Items.Values.OfType<HavenRecoveredHeadquarters>().Where(h=>!h.Deleted).ToArray())foreach(var item in house.CompanyFixtures.Where(i=>!i.Deleted)){
    int art=0,x=item.X-house.X;
    switch(item.Name){
     case "Galley chair":art=x==-10?0xB56:0xB58;break;
     case "Council chair":art=x==-11?0xB56:0xB58;break;
     case "Quartermaster's chair":art=0xB56;break;
     case "Company officer's chair":art=0xB58;break;
     case "Crew mess chair":art=x==-12?0xB56:0xB58;break;
     case "A chair in the sea breeze":art=x==6?0xB56:0xB58;break;
     case "Cartographer's chair":art=0xB59;break;
     case "Gallery reading chair":art=0xB58;break;
     case "The captain's chair":art=0xB50;break;
     case "Galley recipes":case "Company archives":case "Navigation reference shelves":case "Captain's library":art=0xA97;break;
    }
    if(art!=0&&item.ItemID!=art){item.ItemID=art;count++;}
   }
   return count;
  }
 }
}
