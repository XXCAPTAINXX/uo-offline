using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.HavenPrototype;
public static class MissionVarietySmoke {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(Action<string> log){
  var shield=new HavenHooksShield();var c=new HavenCompanion();
  try{
   Check(shield.ArmorAttributes.SoulCharge==25&&shield.Attributes.RegenMana==3,"Hook starting boost");
   var record=HavenAdvancedGear.Find(shield);record.Gain(1900);record.Apply();record.Apply();
   Check(shield.ArmorAttributes.SoulCharge==44&&shield.Attributes.RegenMana==6&&shield.Attributes.LowerManaCost==9&&shield.Attributes.DefendChance==19&&shield.Attributes.BonusStam==14,"Hook level20 idempotency");
   Check((int)CompanionMission.AbyssIngredients==22&&(int)CompanionMission.JewelRecovery==23,"Saved mission IDs changed");
   c.Skills.MagicResist.Base=100;c.Skills.Tactics.Base=100;
   var flags=BindingFlags.Instance|BindingFlags.NonPublic;
   foreach(var kind in new[]{CompanionMission.JewelRecovery,CompanionMission.EnchantedTimber,CompanionMission.DragonSalvage}){
    c.Skills.MagicResist.Base=HavenRegionalMissions.Requirement(kind)-0.1;
    Check(!HavenRegionalMissions.CanStart(c,kind),"Underqualified route accepted");c.Skills.MagicResist.Base=100;
    foreach(int minutes in new[]{5,15,30,60}){
     Check((bool)typeof(HavenCompanion).GetMethod("PrepareResourceMission",flags).Invoke(c,new object[]{kind,minutes}),"Dispatch rejected");
     var scheduled=(Dictionary<int,int>)typeof(HavenCompanion).GetField("_scheduledResources",flags).GetValue(c);
     var snapshot=scheduled.OrderBy(x=>x.Key).ToArray();Check(snapshot.Length>=2,"Missing specialist materials");
     foreach(var pair in snapshot){var item=HavenResources.Create(pair.Key,pair.Value);Check(item!=null&&item.Stackable,"Invalid reward material");item.Delete();}
     using(var stream=new MemoryStream()){
      var writer=new BinaryFileWriter(stream,true);typeof(HavenCompanion).GetMethod("SerializeResourceMissions",flags).Invoke(c,new object[]{writer});writer.Flush();scheduled.Clear();stream.Position=0;
      typeof(HavenCompanion).GetMethod("DeserializeResourceMissions",flags).Invoke(c,new object[]{new BinaryFileReader(new BinaryReader(stream))});
     }
     Check(c.MissionKind==kind&&snapshot.SequenceEqual(scheduled.OrderBy(x=>x.Key)),"Saved dispatch rerolled or lost rewards");
    }
   }
   for(int theme=0;theme<3;theme++){
    Check(HavenCoveSpoils.Adventure(theme,0.25,0)==null&&HavenCoveSpoils.Gear(theme,0.05,0)==null,"Guaranteed drop regression");
    for(int choice=0;choice<6;choice++){var adventure=HavenCoveSpoils.Adventure(theme,0,choice);var gear=HavenCoveSpoils.Gear(theme,0,choice);Check(adventure!=null&&HavenAdvancedGear.Find(gear)!=null,"Missing themed reward/evolution");adventure.Delete();gear.Delete();}
   }
   for(int selection=11;selection<=13;selection++)new CompanionActivityGump(c,0,selection,60);
   log("PASS Hook levels 1/20 and repeated apply; legacy mission IDs; all 12 specialist dispatch/duration combinations, gating, native materials and saved reward snapshots; themed drop boundaries and evolving gear; new mission menu pages");
  }finally{shield.Delete();c.Delete();}
 }
}
