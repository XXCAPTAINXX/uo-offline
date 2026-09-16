using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.SkillHandlers;
using Server.HavenPrototype;
public static class PetTamingProgressionSmoke
{
 public static void Run(Action<string> log)
 {
  for(int tier=0;tier<=3;tier++)
  {
   var pet=new Dragon();
   try
   {
    HavenPetMissions.ApplyRarity(pet,tier);var record=HavenLegendaryPetSkills.Find(pet);
    var rolls=record==null?new SkillName[0]:record.Boosted.ToArray();var caps=rolls.ToDictionary(n=>n,n=>pet.Skills[n].Cap);
    AnimalTaming.ScaleSkills(pet,.9,true);
    for(int i=0;i<pet.Skills.Length;i++){var skill=pet.Skills[i];if(skill.Base>90.001)throw new Exception("Starting skill above90 tier"+tier);if(skill.Cap!=(caps.ContainsKey((SkillName)i)?caps[(SkillName)i]:100))throw new Exception("Wrong first-tame cap "+skill.Name);}
    if(record!=null){HavenLegendaryPetSkills.Roll(pet);if(rolls.Any(n=>pet.Skills[n].Base>90.001))throw new Exception("Repair restored trained levels");
     var support=HavenLegendaryInnates.RequiredSkills(rolls).Except(rolls);if(support.Any(n=>pet.Skills[n].Cap!=100||pet.Skills[n].Base>90.001))throw new Exception("Support caps/skills");
     using(var stream=new System.IO.MemoryStream()){var w=new BinaryFileWriter(stream,true);record.Serialize(w);w.Flush();stream.Position=0;record.Deserialize(new BinaryFileReader(new System.IO.BinaryReader(stream)));}HavenLegendaryPetSkills.Roll(pet);if(rolls.Any(n=>pet.Skills[n].Base>90.001))throw new Exception("Reload repair erased training");
    }
    var normal=HavenPetMissions.TrainableSkills.First(n=>!rolls.Contains(n));pet.Skills[normal].Cap=120;pet.Skills[normal].Base=110;AnimalTaming.ScaleSkills(pet,.9,false);if(pet.Skills[normal].Cap!=120||pet.Skills[normal].Base!=99)throw new Exception("Retame erased purchased cap");
   }finally{pet.Delete();}
  }
  var ticket=new HavenPetTicket(null,8,5,3);try{var pet=ticket.Pet;var record=HavenLegendaryPetSkills.Find(pet);if(record==null||record.Boosted.Count()<3||pet.Skills.Any(s=>s.Base>90.001))throw new Exception("Mission pet bypassed loss");HavenLegendaryPetSkills.Roll(pet);if(pet.Skills.Any(s=>s.Base>90.001))throw new Exception("Mission repair restored levels");}finally{ticket.Delete();}
  var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=100};owner.AddItem(new Backpack());new Server.Accounting.Account("tame-test-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;owner.MoveToWorld(new Point3D(3400,2400,Map.Felucca.GetAverageZ(3400,2400)),Map.Felucca);owner.Skills.AnimalTaming.Base=120;owner.Skills.AnimalLore.Base=120;
  bool found=false;for(int x=1400;x<1440&&!found;x++)for(int y=1700;y<1740&&!found;y++){var at=new Point3D(x,y,Map.Felucca.GetAverageZ(x,y));if(Map.Felucca.CanFit(at,16,false,false)){owner.MoveToWorld(at,Map.Felucca);found=true;}}if(!found)throw new Exception("No walkable test tile");
  var claim=new HavenPetTicket(owner,0);var claimed=claim.Pet;
  try
  {
   owner.Backpack.DropItem(claim);double before=claimed.Skills.Wrestling.Base;
   if(!claim.Claim(owner))throw new Exception("Claim failed alive="+owner.Alive+" fit="+owner.Map.CanFit(owner.Location,16,false,false)+" followers="+owner.Followers);if(claimed.Skills.Wrestling.Base!=before)throw new Exception("Claim applied skill loss twice "+before+" -> "+claimed.Skills.Wrestling.Base);
   claimed.Skills.Wrestling.Cap=120;claimed.Skills.Wrestling.Base=110;
   var stored=HavenPetTicket.Store(claimed,owner,owner.Backpack);if(stored==null||!stored.Claim(owner)||claimed.Skills.Wrestling.Cap!=120||claimed.Skills.Wrestling.Base!=110)throw new Exception("Stored pet lost earned progress");
  }finally{claim.Delete();claimed.Delete();owner.Delete();}
  log("PASS all rarities start at <=90 skill, ordinary caps100, random caps retained, support skills reduced, repair/reload preserves training, retame preserves purchased caps, mission tickets follow same rules");
 }
}
