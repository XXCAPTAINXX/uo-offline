using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class WanderingEncounterSmoke {
 public static void Persistence(bool reload,Action<string> log){
  var owner=World.FindMobile((Serial)37901);
  var journal=HavenEncounterJournal.Get(owner);
  if(!reload){journal.Tier=9;journal.Wins=8;journal.Points=123;journal.Log("Persistence audit");
   var active=new HavenWanderingEncounter{Journal=journal,Tier=9,Expires=DateTime.UtcNow.AddMinutes(15)};active.MoveToWorld(owner.Location,owner.Map);journal.Active=active;
   var mob=new HavenEncounterMob(9,false){Encounter=active};active.Creatures.Add(mob);mob.MoveToWorld(owner.Location,owner.Map);
   World.Save(false,false);log("PASS saved journal and interrupted invasion");
  }else{if(journal.Tier!=9||journal.Wins!=8||journal.Points!=123||journal.Active!=null||World.Mobiles.Values.OfType<HavenEncounterMob>().Any())throw new Exception("Restart lost progress or left invasion mobs");log("PASS separate-process reload retains progress and cancels interrupted invasion without penalty");}
 }
 public static void Run(Action<string> log){
  var player=new PlayerMobile();player.Name="Encounter audit";player.AddItem(new Backpack());
  HavenEncounterJournal journal=null;
  try{
   bool found=false;
   for(int x=3400;x<3600&&!found;x+=5)for(int y=2300;y<2500&&!found;y+=5){var p=new Point3D(x,y,Map.Trammel.GetAverageZ(x,y));if(!Map.Trammel.CanSpawnMobile(p))continue;player.MoveToWorld(p,Map.Trammel);if(HavenWanderingEncounter.CanStart(player))found=true;}
   if(!found)throw new Exception("No wilderness test location");
   journal=HavenEncounterJournal.Get(player);journal.Tier=12;
   var encounter=HavenWanderingEncounter.Start(journal);if(encounter==null||encounter.Creatures.Count==0)throw new Exception("No encounter spawned");
   if(HavenWanderingEncounter.Start(journal)!=null)throw new Exception("Duplicate encounter");
   encounter.Participants.Add(player);
   int kills=0;
   while(!encounter.Deleted&&kills<100){encounter.Spawn();var mob=encounter.Creatures.FirstOrDefault();if(mob==null)throw new Exception("Wave stalled");if(!mob.AlwaysMurderer)throw new Exception("Blue invader");encounter.Defeated(mob);mob.Delete();kills++;}
   if(!encounter.Deleted||journal.Tier!=13||journal.Wins!=1||journal.Points!=80)throw new Exception("Progression/clear credit failed");
   var chest=World.Items.Values.OfType<HavenEncounterChest>().Single(c=>c.Claimants.Contains(player));
   var stranger=new PlayerMobile();try{if(!chest.CanClaim(player)||chest.CanClaim(stranger))throw new Exception("Spoils ownership failed");}finally{stranger.Delete();}
   log("PASS three waves plus champion; "+kills+" kills; tier 12 -> 13; participant-only chest and 80 points");chest.Delete();
   var cancelled=HavenWanderingEncounter.Start(journal);cancelled.Finish(false,false);if(journal.Tier!=13||journal.Active!=null)throw new Exception("Cancellation penalty");
   var failed=HavenWanderingEncounter.Start(journal);failed.Finish(false,true);if(journal.Tier!=12||journal.Failures!=1)throw new Exception("Retreat progression");
   log("PASS cancel clears mobs without penalty; retreat lowers difficulty");
   for(int tier=1;tier<=20;tier++){
    if(HavenEncounterBossLoot.Roll(tier,.999,0)!=null)throw new Exception("Guaranteed gear");
    for(int choice=0;choice<5;choice++){var item=HavenEncounterBossLoot.Roll(tier,0,choice);if(tier<5&&item!=null||tier>=5&&item==null)throw new Exception("Tier loot gating");if(item!=null){if(HavenAdvancedGear.Find(item)==null)throw new Exception("Gear cannot level");HavenAdvancedGear.Find(item).Delete();item.Delete();}}
   }
   var catalog=new System.Collections.Generic.List<HavenShopEntry>();HavenCombatCastingGear.AddCatalog(catalog);if(catalog.Count!=5)throw new Exception("Exclusive gear remains in catalog");
   log("PASS chance-drop boundaries at all 20 tiers; evolving loot; five starter catalog options");
  }finally{if(journal!=null)journal.Delete();player.Delete();}
 }
}
