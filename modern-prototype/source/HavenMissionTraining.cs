using System;
using System.Collections.Generic;
using System.Linq;
namespace Server.HavenPrototype
{
 public static class HavenMissionTraining
 {
  public static int[] Snapshot(HavenCompanion c){return c.Skills.Select(s=>s.BaseFixedPoint).ToArray();}
  public static string Complete(HavenCompanion c,int minutes,int[] before)
  {
   int offered=HavenMissionSpoils.Experience(minutes),items=0,total=0,levels=0;
   foreach(var item in c.Items.ToArray())
   {
    var record=HavenEquipmentEvolution.Find(item);
    if(record!=null){int xp=record.Experience,level=record.Level;record.Gain(offered);int gain=record.Experience-xp;if(gain>0){items++;total+=gain;levels+=record.Level-level;}continue;}
    var advanced=HavenAdvancedGear.Find(item)??HavenAdvancedGear.Attach(item,HavenAdvancedGear.AutoKind(item));
    if(advanced!=null){int xp=advanced.Experience,level=advanced.Level;advanced.Gain(offered);int gain=advanced.Experience-xp;if(gain>0){items++;total+=gain;levels+=advanced.Level-level;}}
   }
   var gains=new List<string>();
   for(int i=0;i<c.Skills.Length&&i<before.Length;i++){int delta=c.Skills[i].BaseFixedPoint-before[i];if(delta>0)gains.Add(c.Skills[i].Name+" +"+(delta/10.0).ToString("0.0"));}
   string skills=gains.Count>0?" Skills: "+String.Join(", ",gains)+".":" No skill increases this trip.";
   return skills+" Gear: "+total+" XP across "+items+" equipped items"+(levels>0?"; "+levels+" levels gained":"")+".";
  }
 }
}
