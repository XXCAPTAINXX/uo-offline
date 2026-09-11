using System;
using System.IO;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class PetAssignmentSmoke
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);File.AppendAllText("pet-assignment-checks.log","PASS "+name+"\n");}
 public static void Initialize(){if(File.Exists("PET-ASSIGNMENT-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(8),Run);}
 static void Run(){try{
 var saved=World.Mobiles.Values.OfType<HavenCompanion>().FirstOrDefault(x=>x.BoundOwner!=null&&x.BoundOwner.Name=="Assigned pet persistence owner");
 if(saved!=null){saved.BoundOwner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);var assigned=saved.Backpack.Items.OfType<HavenCompanionAssignedPet>().Single();var original=assigned.Pet;Check(original!=null&&original.ControlMaster==saved&&assigned.Owner==saved.BoundOwner,"assignment ownership survives reload");Check(saved.Recall(saved.BoundOwner),"recall persisted mission with assigned pet");assigned.Tick();Check(!assigned.Parked&&original.Map==saved.Map,"parked assigned pet returns after reload");Check(assigned.ClaimBack(saved.BoundOwner),"reclaim after reload");Check(saved.Backpack.FindItemsByType(typeof(HavenPetTicket),true).Cast<HavenPetTicket>().Any(x=>x.Pet==original),"reloaded reclaim retains exact pet");File.AppendAllText("pet-assignment-checks.log","RELOAD COMPLETE\n");Core.Kill(false);return;}

 var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=250};owner.AddItem(new Backpack());var account=new Account("assign-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"));account[0]=owner;owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);
 var companion=HavenCompanion.Claim(owner);companion.Skills.AnimalTaming.Base=125;companion.Skills.AnimalLore.Base=125;var horse=new Horse();horse.SetControlMaster(owner);horse.MoveToWorld(owner.Location,owner.Map);
 Check(companion.AcceptAssignedPet(owner,horse),"owner can assign trained pet");var record=companion.Backpack.Items.OfType<HavenCompanionAssignedPet>().Single();Check(horse.ControlMaster==companion&&horse.Rider==companion,"companion automatically rides assigned mount");record.AutoMount=false;record.Tick();Check(horse.Rider==null&&horse.ControlTarget==companion,"ride toggle dismounts and follows");
 horse.IsBonded=true;horse.Kill();Check(horse.IsDeadPet,"assigned bonded pet enters dead-pet state");Check(companion.SupportPatient(horse)&&!horse.IsDeadPet,"companion resurrects its assigned pet");
 Check(companion.StartMission(owner,30,CompanionMission.Mining),"companion can dispatch with assigned pet");Check(record.Parked&&horse.Map==Map.Internal,"unmounted assigned pet safely parks for mission");Check(companion.Recall(owner),"early recall with assigned pet");record.Tick();Check(!record.Parked&&horse.Map==owner.Map,"assigned pet returns after mission recall");
 var stranger=new PlayerMobile();Check(!record.ClaimBack(stranger),"stranger cannot reclaim assigned pet");stranger.Delete();Check(record.ClaimBack(owner),"owner reclaims original pet ticket");var ticket=companion.Backpack.FindItemByType(typeof(HavenPetTicket),true) as HavenPetTicket;Check(ticket!=null&&ticket.Pet==horse&&horse.ControlMaster==null,"claim ticket preserves original pet identity");owner.Backpack.DropItem(ticket);owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);File.AppendAllText("pet-assignment-checks.log","claim fit="+owner.Map.CanFit(owner.Location,16,false,false)+" followers="+owner.Followers+"/"+owner.FollowersMax+" slots="+horse.ControlSlots+" alive="+owner.Alive+" accessible="+HavenResources.Accessible(owner,ticket)+"\n");Check(ticket.Claim(owner)&&horse.ControlMaster==owner,"original pet returns to owner from ticket");
 owner.Name="Assigned pet persistence owner";companion.MoveToWorld(owner.Location,owner.Map);horse.MoveToWorld(owner.Location,owner.Map);Check(companion.AcceptAssignedPet(owner,horse),"reassign pet for persistence fixture");var savedRecord=companion.Backpack.Items.OfType<HavenCompanionAssignedPet>().Single();savedRecord.AutoMount=false;savedRecord.Tick();Check(companion.StartMission(owner,30,CompanionMission.Mining),"persist mission with assigned pet");World.Save();File.AppendAllText("pet-assignment-checks.log","FRESH COMPLETE\n");Core.Kill(false);
 }catch(Exception e){File.AppendAllText("pet-assignment-checks.log","FAIL "+e+"\n");Core.Kill(false);}}
}
