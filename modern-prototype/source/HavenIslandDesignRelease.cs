using System;
using System.IO;
using System.Linq;
namespace Server.HavenPrototype {
 public static class HavenIslandDesignRelease {
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled&&File.Exists("HAVEN-DESIGN-REVIEW"))Timer.DelayCall(TimeSpan.FromSeconds(3),Apply);};}
  static void Apply(){
   try{
    var house=World.Items.Values.OfType<HavenRecoveredHeadquarters>().Single(h=>!h.Deleted);
    house.RefineRoomsForReview();HavenCoveDesignReview.Apply();HavenTrainingServices.Ensure();house.CheckWalkingRoutes(true);
    World.Save(false,false);File.Delete("HAVEN-DESIGN-REVIEW");
    Console.WriteLine("ISLAND DESIGN RELEASE PASS: rooms, gallery stairs, cove entrance and trainers saved");
   }catch(Exception ex){Console.WriteLine("ISLAND DESIGN RELEASE FAILED: "+ex);}
  }
 }
}
