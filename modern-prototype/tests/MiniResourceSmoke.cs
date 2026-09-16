using System;
using System.Linq;
using Server.Items;
using Server.HavenPrototype;
public static class MiniResourceSmoke {
 public static void Run(Action<string> log){
  int tested=0;
  for(int family=0;family<HavenMiniResources.Pools.Length;family++)for(int choice=0;choice<HavenMiniResources.Pools[family].Length;choice++){
   var deed=HavenMiniResources.Create(family,choice,125);var material=HavenResources.Create(deed.ResourceId,deed.Units);
   if(material==null||!material.Stackable||deed.Units<1)throw new Exception("Invalid resource reward");material.Delete();deed.Delete();tested++;
  }
  for(int i=0;i<100;i++){var bag=new Bag();try{HavenMiniResources.Completion(bag);var deeds=bag.Items.OfType<HavenResourceDeed>().ToArray();if(deeds.Length!=2||deeds[0].ResourceId==deeds[1].ResourceId)throw new Exception("Duplicate resource categories");}finally{bag.Delete();}}
  log("PASS "+tested+" resource choices produce valid native materials; 100 parcels each contain two distinct resource categories");
 }
}
