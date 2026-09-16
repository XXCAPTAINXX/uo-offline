using System;
using Server;
using Server.Items;
namespace Server.HavenPrototype {
 public static class HavenLevelProperties {
  public static string Line(int level,int xp,int cap){return "Equipment level: "+level+"/20 | Total XP: "+xp+"/"+cap+(level>=20?" (maximum level)":"");}
  public static void Progress(ObjectPropertyList list,int level,int xp,int next){if(level<20)list.Add("Next level: "+Math.Max(0,next-xp)+" XP remaining");}
  public static void Add(Item item,ObjectPropertyList list){
   if(item is IHavenStarterGear||item is HavenSetRing||item is HavenConcordTalisman)return;
   var evolution=HavenEquipmentEvolution.Find(item);var advanced=HavenAdvancedGear.Find(item);
   int level,xp;
   if(evolution!=null){level=evolution.Level;xp=evolution.Experience;}
   else if(advanced!=null){level=advanced.Level;xp=advanced.Experience;}
   else if(HavenAdvancedGear.AutoKind(item)>0){level=1;xp=0;}
   else return;
   list.Add(Line(level,xp,1900));Progress(list,level,xp,level*100);
   list.Add(evolution!=null&&(evolution.Kind>=1&&evolution.Kind<=3||evolution.Kind==HavenCompanionClothing.EvolutionKind)?"Grows through Jenna's combat and completed missions.":"Equip to earn XP from credited hostile monster kills.");
   var w=item as BaseWeapon;if(w!=null&&level<20){int next=level+1;int mana=evolution!=null?75+(next-1)*25/19:20+(next-1)*80/19;list.Add("Next level mana-leech floor: "+mana+"% (higher existing bonuses kept)");}
  }
 }
}
