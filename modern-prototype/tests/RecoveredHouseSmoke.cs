using System;
using System.IO;
using System.Linq;
using Server;
using Server.Items;
using Server.Multis;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class RecoveredHouseSmoke {
 public static void Initialize(){if(File.Exists("RECOVERED-HOUSE-TEST-ONLY"))EventSink.ServerStarted+=Run;}
 static void Run(){try {
  foreach(var site in World.Items.Values.OfType<HavenIslandEncounters>().ToArray()) {
   if(File.Exists("recovered-route-save.state")&&site.Patrol.Count<4)throw new Exception("Patrol route missing after saved reload");
   site.EnsurePatrol();
   for(int i=0;i<site.Patrol.Count;i++) {
    var node=site.Patrol[i];var next=site.Patrol[(i+1)%site.Patrol.Count];int z;
    if(node.NextPoint!=next||!site.Safe(node.Location)||!Server.Movement.Movement.CheckMovement(node.Location,site.Map,node.Location,Utility.GetDirection(node,next),out z)||z!=next.Z||Math.Abs(node.X-next.X)>1||Math.Abs(node.Y-next.Y)>1)throw new Exception("Invalid native patrol segment");
   }
  }

  var cove=World.Items.Values.OfType<HavenCoveEncounter>().Single();
  var approach=World.Items.Values.OfType<HavenCoveApproach>().SingleOrDefault();
  if(File.Exists("recovered-route-save.state")&&approach==null)throw new Exception("Cove approach missing after reload");
  if(approach==null)approach=HavenCoveApproach.BuildTest(cove);
  if(approach.Fixtures.OfType<HavenCoveBoard>().Single().Camp!=cove)throw new Exception("Cove board lost encounter link");
  if(!new MovementPath(new Point3D(4210,2928,0),new Point3D(cove.X,cove.Y-1,cove.Z),cove.Map).Success)throw new Exception("Cove trail blocked");
  if(File.Exists("recovered-house-save.state")) {
   var ids=File.ReadAllLines("recovered-house-save.state").Select(int.Parse).ToArray();
   var saved=World.FindItem((Serial)ids[0]) as HavenRecoveredHeadquarters;
   if(saved==null||saved.Owner==null||saved.Owner.Serial.Value!=ids[1]||saved.Vault.Serial.Value!=ids[2]||World.FindItem((Serial)ids[3]).Parent!=saved.Vault||World.FindItem((Serial)ids[4]).Parent!=World.FindItem((Serial)ids[3]))throw new Exception("Reload lost ownership or nested vault");
   if(saved.Stations.Count!=3||saved.Stations.Any(s=>s.Estate!=saved)||saved.CompanyFixtures.Count!=70)throw new Exception("Reload lost fixtures or station linkage");
   saved.CheckFloorAccess();saved.CheckWalkingRoutes();
   File.WriteAllText("recovered-house-result.txt","PASS second process: custom house, owner, all 70 fixtures, ladder/walking routes, 3 connected stations and exact nested vault survive full save/reload; three native patrol loops and linked cove approach pass.");
   if(File.Exists("RECOVERED-HOUSE-PERSIST")){World.Save(false,false);File.WriteAllText("recovered-route-save.state","Patrol and cove route saved");}
   Core.Kill(false);return;
  }

  var old=World.Items.Values.OfType<HavenIslandEstate>().Single();
  var vault=old.Vault;var nested=new Bag();var gold=new Gold(73);nested.DropItem(gold);vault.DropItem(nested);
  var before=new System.Collections.Generic.HashSet<Serial>(World.Items.Values.Where(i=>!i.Deleted).Select(i=>i.Serial));
  bool rolledBack=false;try{HavenRecoveredHouseMigration.Rehearse(old,()=>{throw new InvalidOperationException("injected rollback");});}catch(InvalidOperationException e){if(e.Message!="injected rollback")throw;rolledBack=true;}
  if(!rolledBack||old.Deleted||old.Map!=Map.Trammel||old.Vault!=vault||old.Stations.Any(s=>s.Map!=old.Map)||gold.Parent!=nested||World.Items.Values.Any(i=>!i.Deleted&&!before.Contains(i.Serial)))throw new Exception("Rollback changed world items or vault");
  var house=HavenRecoveredHouseMigration.Rehearse(old);
  if(!old.Deleted||house.Vault!=vault||gold.Parent!=nested||nested.Parent!=vault)throw new Exception("Vault identity lost");
  var c=house.Components;
  var packet=new DesignStateDetailed(house.Serial.Value,house.LastRevision,true,c.Min.X,c.Min.Y,c.Max.X,c.Max.Y,c.List);
  int packetLength;var bytes=packet.Compile(false,out packetLength);File.WriteAllBytes("recovered-house-packet.bin",bytes.Take(packetLength).ToArray());
  using(var csv=new StreamWriter("recovered-house-tiles.csv")){csv.WriteLine("kind,id,x,y,z");foreach(var tile in c.List)csv.WriteLine("component,"+tile.m_ItemID+","+tile.m_OffsetX+","+tile.m_OffsetY+","+tile.m_OffsetZ);foreach(var item in house.CompanyFixtures){csv.WriteLine("fixture,"+item.ItemID+","+(item.X-house.X)+","+(item.Y-house.Y)+","+(item.Z-house.Z));var addon=item as BaseAddon;if(addon!=null)foreach(var part in addon.Components)csv.WriteLine("fixture,"+part.ItemID+","+(part.X-house.X)+","+(part.Y-house.Y)+","+(part.Z-house.Z));}}
  File.WriteAllText("recovered-house-result.txt","PASS: custom 31x31 compound, ladder landings, same nested vault; native house packet constructed. Tiles="+c.List.Length+" Fixtures="+house.CompanyFixtures.Count);
  if(File.Exists("RECOVERED-HOUSE-PERSIST")){World.Save(false,false);File.WriteAllLines("recovered-house-save.state",new[]{house.Serial.Value,house.Owner.Serial.Value,vault.Serial.Value,nested.Serial.Value,gold.Serial.Value}.Select(i=>i.ToString()).ToArray());}
 }catch(Exception e){File.WriteAllText("recovered-house-result.txt","FAIL: "+e);}
 Core.Kill(false);
 }
}

