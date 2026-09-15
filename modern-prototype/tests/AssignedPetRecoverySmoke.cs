using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class AssignedPetRecoverySmoke
{
 public static void Run(Action<string> log)
 {
  var p=new PlayerMobile{Player=true,Body=0x190};p.AddItem(new Backpack());new Account("pet-recovery-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=p;p.MoveToWorld(new Point3D(3400,2400,0),Map.Felucca);var c=HavenCompanion.Claim(p);c.Skills.AnimalTaming.Base=120;c.Skills.AnimalLore.Base=120;
  var pet=new Horse{IsBonded=true};pet.SetControlMaster(p);pet.MoveToWorld(p.Location,p.Map);if(!c.AcceptAssignedPet(p,pet))throw new Exception("Assignment setup");var record=c.Backpack.Items.OfType<HavenCompanionAssignedPet>().Single();if(!record.IsVirtualItem)throw new Exception("Assignment consumes slot");
  p.FollowersMax=p.Followers;record.Delete();if(pet.Deleted||pet.ControlMaster!=null||!pet.IsStabled||pet.StabledBy!=p||pet.Map!=Map.Internal||!p.Stabled.Contains(pet)||!pet.IsBonded)throw new Exception("Full follower recovery lost pet");record.Delete();if(p.Stabled.Count(x=>x==pet)!=1)throw new Exception("Duplicate stable entry");
  p.FollowersMax=20;p.Stabled.Remove(pet);pet.IsStabled=false;pet.StabledBy=null;pet.SetControlMaster(p);pet.MoveToWorld(p.Location,p.Map);if(!c.AcceptAssignedPet(p,pet))throw new Exception("Reassignment");record=c.Backpack.Items.OfType<HavenCompanionAssignedPet>().Single();record.Delete();if(pet.ControlMaster!=p||pet.Map!=p.Map||pet.IsStabled)throw new Exception("Normal recovery failed");
  pet.Delete();c.Delete();p.Delete();log("PASS full followers stable the exact bonded pet, repeat deletion does not duplicate, normal return and virtual assignment");
 }
}
