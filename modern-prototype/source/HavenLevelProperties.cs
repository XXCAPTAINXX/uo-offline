using Server;
namespace Server.HavenPrototype {
 public static class HavenLevelProperties {
  public static string Line(int level,int xp,int cap){return "Equipment level: "+level+"/20 | XP: "+xp+"/"+cap+(level>=20?" (maximum level)":"");}
  public static void Add(Item item,ObjectPropertyList list){
   if(item is IHavenStarterGear||item is HavenSetRing||item is HavenConcordTalisman)return;
   var evolution=HavenEquipmentEvolution.Find(item);var advanced=HavenAdvancedGear.Find(item);
   if(evolution!=null)list.Add(Line(evolution.Level,evolution.Experience,1900));
   else if(advanced!=null)list.Add(Line(advanced.Level,advanced.Experience,1900));
   else if(HavenAdvancedGear.AutoKind(item)>0)list.Add(Line(1,0,1900));
  }
 }
}
