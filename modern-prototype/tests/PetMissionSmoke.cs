using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class PetMissionSmoke {
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);File.AppendAllText("pet-mission-checks.log","PASS "+label+"\n");}
 static HavenPetTicket Scheduled(HavenCompanion c){return (HavenPetTicket)typeof(HavenCompanion).GetField("_scheduledPet",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(c);}
 static void Finish(HavenCompanion c){typeof(HavenCompanion).GetField("_missionDue",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,DateTime.UtcNow.AddSeconds(-1));typeof(HavenCompanion).GetMethod("CompleteDueMission",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(c,null);}
 public static void Initialize(){if(File.Exists("PET-MISSION-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(8),Run);}
 static void Run(){try{
  var saved=World.Mobiles.Values.OfType<HavenCompanion>().FirstOrDefault(x=>x.BoundOwner!=null&&x.BoundOwner.Name=="Pet mission fixture");
  if(saved!=null){var account=(Account)saved.BoundOwner.Account;var ticket=Scheduled(saved)??saved.BoundOwner.Backpack.FindItemsByType(typeof(HavenPetTicket),true).Cast<HavenPetTicket>().FirstOrDefault();Check(ticket!=null&&ticket.Pet!=null&&ticket.Pet.Serial.Value.ToString()==account.GetTag("PetFixtureSerial"),"reload preserves exact reserved pet, including overdue completion");if(saved.OnMission)Finish(saved);Check(saved.BoundOwner.Backpack.FindItemsByType(typeof(HavenPetTicket),true).Length==1,"completed reloaded mission delivers one ticket");saved.DeliverPetTickets();Check(saved.BoundOwner.Backpack.FindItemsByType(typeof(HavenPetTicket),true).Length==1,"repeat collection never duplicates ticket");File.AppendAllText("pet-mission-checks.log","RELOAD COMPLETE\n");Core.Kill(false);return;}
  var p=new PlayerMobile{Player=true,Name="Pet mission fixture",Body=0x190,RawStr=250};p.AddItem(new Backpack());var a=new Account("pet-mission-fixture",Guid.NewGuid().ToString("N"));a[0]=p;p.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);var c=HavenCompanion.Claim(p);
  Check(c.Skills.AnimalTaming.Base>=50,"companion can start basic pet routes without GM training");Check(!c.StartMission(p,5,CompanionMission.TameStormscale),"advanced route enforces both skills");
  for(int i=0;i<12;i++){var pet=HavenPetMissions.Create(i);Check(pet!=null&&pet.HitsMax>0,"route has usable species "+i);pet.Delete();}
  Check(c.StartMission(p,5,CompanionMission.TameHorse),"basic taming mission starts");var canceled=Scheduled(c);var canceledPet=canceled.Pet;Check(c.Recall(p)&&canceled.Deleted&&canceledPet.Deleted,"early recall deletes reserved pet and awards no ticket");
  Check(c.StartMission(p,5,CompanionMission.TameHorse),"second mission starts");var reserved=Scheduled(c);int serial=reserved.Pet.Serial.Value;Finish(c);Check(reserved.Parent==p.Backpack&&reserved.Pet.Serial.Value==serial,"completion delivers the exact snapshotted pet ticket");
  var stranger=new PlayerMobile();stranger.AddItem(new Backpack());Check(!reserved.Claim(stranger)&&!reserved.Deleted,"other player cannot claim ticket");stranger.Delete();int max=p.FollowersMax;p.FollowersMax=0;Check(!reserved.Claim(p)&&!reserved.Deleted,"full follower capacity preserves ticket");p.FollowersMax=max;
  var claimed=reserved.Pet;Check(reserved.Claim(p)&&reserved.Deleted&&claimed.ControlMaster==p&&claimed.Map==p.Map,"ticket claims actual pet with correct ownership");claimed.Delete();Check(c.Recall(p),"companion recalls after completed pet mission");
  c.Skills.AnimalTaming.Base=c.Skills.AnimalLore.Base=125;Check(c.StartMission(p,5,CompanionMission.TameStormhorn),"custom species route starts with trained skills");a.SetTag("PetFixtureSerial",Scheduled(c).Pet.Serial.Value.ToString());World.Save();File.AppendAllText("pet-mission-checks.log","FRESH COMPLETE\n");Core.Kill(false);
 }catch(Exception e){File.AppendAllText("pet-mission-checks.log","FAIL "+e+"\n");Core.Kill(false);}}
}
