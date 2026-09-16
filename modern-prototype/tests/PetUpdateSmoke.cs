using System;
using System.IO;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class PetUpdateSmoke {
 public static void Initialize(){if(File.Exists("PET-UPDATE-TEST-ONLY"))EventSink.ServerStarted+=Run;}
 static void Require(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Run(){try {
  if(File.Exists("pet-update-save.state")){
   var ids=File.ReadAllLines("pet-update-save.state").Select(int.Parse).ToArray();var savedOwner=World.FindMobile((Serial)ids[0]);var savedPet=World.FindMobile((Serial)ids[1]) as HavenAncientHellhound;
   Require(savedPet!=null&&savedPet.Rider==savedOwner&&savedOwner.Mount==savedPet&&savedPet.Map==Map.Internal&&savedPet.ControlMaster==savedOwner&&savedPet.Skills.Discordance.Base==150,"Mounted savedPet reload lost identity, savedOwner or skill");
   BaseMount.Dismount(savedOwner);Require(!savedOwner.Mounted&&savedPet.Rider==null&&savedPet.Map==(savedOwner.Map==Map.Internal?savedOwner.LogoutMap:savedOwner.Map),"Reload dismount failed");
   File.WriteAllText("pet-update-result.txt","PASS second process: mounted Hellhound, savedOwner and 150 Discordance survived full save/reload; native dismount returned same savedPet.");Core.Kill(false);return;
  }
  var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=200};owner.AddItem(new Backpack());new Account("mount-test-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;
  var pet=new HavenAncientHellhound();pet.SetControlMaster(owner);owner.MoveToWorld(new Point3D(4210,2928,0),Map.Trammel);pet.MoveToWorld(new Point3D(4211,2928,0),Map.Trammel);
  pet.OnDoubleClick(owner);Require(owner.Mount==pet&&pet.Rider==owner&&pet.Map==Map.Internal,"Owner could not mount");
  BaseMount.Dismount(owner);Require(!owner.Mounted&&pet.Rider==null&&pet.Map==owner.Map&&pet.ControlMaster==owner,"Dismount lost pet");
  var stranger=new PlayerMobile{Player=true,Body=0x190};stranger.MoveToWorld(owner.Location,owner.Map);pet.OnDoubleClick(stranger);Require(!stranger.Mounted,"Stranger mounted pet");stranger.Delete();
  BaseMount.SetMountPrevention(owner,BlockMountType.DismountRecovery,TimeSpan.FromMinutes(1));pet.OnDoubleClick(owner);Require(!owner.Mounted,"Mount prevention bypassed");BaseMount.ClearMountPrevention(owner);
  var eligible=HavenLegendaryPetSkills.EligibleSkills(new SkillName[0]);Require(eligible.Count==HavenPetMissions.TrainableSkills.Length&&eligible.Contains(SkillName.Discordance),"Full overcap pool missing");
  var rollPet=new Orc();HavenPetRarity.Attach(rollPet,3);foreach(var skill in HavenPetMissions.TrainableSkills)rollPet.Skills[skill].Base=0;
  var record=HavenLegendaryPetSkills.Roll(rollPet);var rolled=record.Boosted.ToArray();Require(rolled.Length>=3&&rolled.Length<=5&&rolled.All(s=>rollPet.Skills[s].Base>=125&&rollPet.Skills[s].Cap>=rollPet.Skills[s].Base),"Zero-start skills excluded or undercapped");
  HavenLegendaryPetSkills.Roll(rollPet);Require(rolled.SequenceEqual(record.Boosted),"Existing rolls changed");rollPet.Delete();
  pet.Skills.Discordance.Cap=150;pet.Skills.Discordance.Base=150;
  pet.OnDoubleClick(owner);Require(owner.Mounted,"Remount failed");
  var ticket=HavenPetTicket.Store(pet,owner,owner.Backpack);Require(ticket!=null&&!owner.Mounted&&pet.Rider==null&&ticket.Pet==pet,"Storage failed to dismount same pet");
  // Restore the same fixture directly; this is not a player claim shortcut.
  ticket.Pet=null;ticket.Delete();pet.SetControlMaster(owner);pet.MoveToWorld(owner.Location,owner.Map);pet.OnDoubleClick(owner);
  World.Save(false,false);File.WriteAllLines("pet-update-save.state",new[]{owner.Serial.Value.ToString(),pet.Serial.Value.ToString()});
  File.WriteAllText("pet-update-result.txt","PASS owner mount/dismount, stranger rejection, mount prevention, ticket dismount; all 24 skills eligible at zero; 3-5 rolls at 125-150, existing rolls preserved; saved mounted fixture.");
 }catch(Exception e){File.WriteAllText("pet-update-result.txt","FAIL "+e);}Core.Kill(false);}
}
