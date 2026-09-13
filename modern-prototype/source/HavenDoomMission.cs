using Server.Engines.Points;
using Server.Mobiles;
namespace Server.HavenPrototype {
 public static class HavenDoomMission {
  public static int Points(int minutes){return minutes==5||minutes==15||minutes==30||minutes==60?minutes*500:0;}
  public static string Preview(Mobile owner,int minutes){
   double points=owner==null?0:PointsSystem.DoomGauntlet.GetPoints(owner);
   return "Drop: "+(HavenDoomStatus.Chance(points)*100).ToString("0.00")+"% -> "+(HavenDoomStatus.Chance(points+Points(minutes))*100).ToString("0.00")+"% projected";
  }
  // Called only by the once-only due-mission completion path, never recall or collection.
  public static string Complete(Mobile owner,CompanionMission kind,int minutes){
   var player=owner as PlayerMobile;int amount=Points(minutes);
   if(kind!=CompanionMission.DoomRecon||player==null||player.Deleted||amount==0)return "";
   PointsSystem.DoomGauntlet.AwardPoints(player,amount);
   double total=PointsSystem.DoomGauntlet.GetPoints(player);
   return " Doom reconnaissance: +"+amount.ToString("N0")+" points; total "+total.ToString("N0")+". Current artifact chance: "+(HavenDoomStatus.Chance(total)*100).ToString("0.00")+"% per eligible boss kill. Use [doom to check.";
  }
 }
}
