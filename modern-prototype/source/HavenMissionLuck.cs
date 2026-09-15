using System;
namespace Server.HavenPrototype {
 public static class HavenMissionLuck {
  public static int Seconds(int minutes,int luck){luck=Math.Max(0,luck);return (int)Math.Ceiling(Math.Max(minutes*40.0,minutes*60.0*(1.0-luck/5000.0)));}
  public static string Duration(int minutes,int luck){int seconds=Seconds(minutes,luck);return (seconds/60)+":"+(seconds%60).ToString("D2");}
 }
}
