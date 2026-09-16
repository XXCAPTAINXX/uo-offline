using System;
using System.IO;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class PetSignatureSmoke
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);File.AppendAllText("pet-signature-checks.log","PASS "+name+"\n");}
 static BaseCreature Pet(Mobile owner,BaseCreature pet){owner.FollowersMax=100;pet.SetControlMaster(owner);pet.MoveToWorld(owner.Location,owner.Map);pet.ControlOrder=OrderType.Attack;HavenPetMissions.ApplyRarity(pet,3);return pet;}
 static Ogre Enemy(Mobile owner){var e=new Ogre();e.HitsMaxSeed=5000;e.Hits=5000;e.MoveToWorld(owner.Location,owner.Map);return e;}
 public static void Initialize(){if(File.Exists("PET-SIGNATURE-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(8),Run);}
 static void Run(){try{
 var persisted=World.Mobiles.Values.OfType<HavenEmberwing>().FirstOrDefault(x=>x.Name=="Pet persistence fixture v2");
 if(persisted!=null){var savedOwner=(persisted.ControlMaster??persisted.StabledBy) as PlayerMobile;Check(savedOwner!=null,"pet owner or native stable owner survives reload");savedOwner.MoveToWorld(new Point3D(1100,1100,Map.Malas.GetAverageZ(1100,1100)),Map.Malas);savedOwner.ClaimAutoStabledPets();Check(persisted.ControlMaster==savedOwner,"native login reclaims auto-stabled pet");var enemy=World.Mobiles.Values.OfType<Ogre>().First(x=>x.Name=="Pet training target fixture v2");persisted.Frozen=false;var profile=PetTrainingHelper.GetTrainingProfile(persisted);Check(profile!=null&&Math.Abs(profile.TrainingProgress-50)<0.001,"native training progress survives reload");Check(HavenPetDefenses.Tier(persisted)==3,"rarity survives reload");var roll=persisted.Backpack.FindItemByType(typeof(HavenLegendaryPetSkills),true);Check(roll!=null&&HavenLegendaryPetSkills.Roll(persisted)==roll,"legendary roll persists without reroll");Check(HavenPetSignatures.Active(persisted),"pet active after simulated owner login");HavenPetTrainingBridge.Award(enemy,persisted,10000);Check(Math.Abs(profile.TrainingProgress-50)<0.001,"per-enemy training cap survives reload");Check(HavenPetSignatures.Find(persisted).Field==null,"temporary field does not survive restart");File.AppendAllText("pet-signature-checks.log","RELOAD COMPLETE\n");Core.Kill(false);return;}

 var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=250,RawInt=250};owner.AddItem(new Backpack());var account=new Account("signature-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"));account[0]=owner;owner.MoveToWorld(new Point3D(1100,1100,Map.Malas.GetAverageZ(1100,1100)),Map.Malas);
 var foe=Enemy(owner);var bystander=Enemy(owner);
 foreach(var raw in new BaseCreature[]{new HavenEmberwing(),new HavenMoonfang(),new HavenFrostmane(),new HavenVerdantLlama(),new HavenStormscale(),new HavenStormhorn(),new HavenSnowBear(),new HavenAncientHellhound(),new VampiricSteed(),new HavenChelonian()}){
 var pet=Pet(owner,raw);foe.Combatant=pet;pet.Combatant=foe;foe.Mana=foe.ManaMax;owner.Mana=0;owner.Hits=owner.HitsMax-100;int hp=foe.Hits;
 Check(HavenPetSignatures.Active(pet),pet.GetType().Name+" active");
 var definition=PetTrainingHelper.GetTrainingDefinition(pet);Check(definition!=null&&definition.Class!=Class.Untrainable&&definition.ControlSlotsMax==5,"custom pet native training definition");
 var training=PetTrainingHelper.GetTrainingProfile(pet,true);training.BeginTraining();foe.Skills.Wrestling.Base=pet.Skills.Wrestling.Base;
 HavenPetTrainingBridge.Award(foe,pet,100);Check(Math.Abs(training.TrainingProgress-3.0)<0.001,"training uses original triple damage pace");
 HavenPetTrainingBridge.Award(foe,pet,10000);Check(Math.Abs(training.TrainingProgress-50.0)<0.001,"one enemy capped at half a one-slot training stage");
 HavenPetTrainingBridge.Award(foe,pet,10000);Check(Math.Abs(training.TrainingProgress-50.0)<0.001,"same enemy cannot bypass training cap");

 Check(!HavenPetSignatures.Enemy(pet,bystander,true),"secondary effect excludes unengaged bystander");
 File.AppendAllText("pet-signature-checks.log","enemy="+HavenPetSignatures.Enemy(pet,foe)+" ready="+HavenPetSignatures.Ready(pet)+" harmful="+pet.CanBeHarmful(foe,false)+" ownerharm="+owner.CanBeHarmful(foe,false)+" los="+pet.InLOS(foe)+" cansee="+pet.CanSee(foe)+" map="+pet.Map+"/"+foe.Map+" alive="+foe.Alive+" hidden="+foe.Hidden+" deleted="+foe.Deleted+" blessed="+foe.Blessed+" owners="+foe.Owners.Count+"\n");
 Check(HavenPetSignatures.Activate(pet,foe),pet.GetType().Name+" signature activates");
 Check(!HavenPetSignatures.Activate(pet,foe),"cooldown blocks repeated signature");
 var state=HavenPetSignatures.Find(pet);
 if(pet is HavenEmberwing){state.Field.Tick();Check(foe.Hits<hp,"Cinderwake damages engaged enemy");Check(bystander.Hits==bystander.HitsMax,"Cinderwake leaves bystander unharmed");}
 if(pet is HavenVerdantLlama){int before=owner.Hits;state.Field.Tick();Check(owner.Hits>before,"Sanctuary Grove heals owner");}
 if(pet is HavenMoonfang){Check(!HavenPetHex.Apply(pet,foe,0,14,8),"Moon Hunt debuff cannot stack");}
 if(pet is HavenStormhorn)Check(owner.Mana>0,"Arcane Reservoir transfers mana");
 if(pet is HavenStormscale){var ai=pet.AIObject as HavenStormscaleAI;Check(ai!=null&&ai.Fire(foe),"Stormscale native ranged bolt fires");Check(!ai.Fire(foe),"three-second bolt cooldown");}
 if(pet is HavenSnowBear){pet.Hits=pet.HitsMax/3;int damage=20;pet.AlterMeleeDamageTo(foe,ref damage);Check(damage==30,"bear Colossal Rage adds fifty percent");}
 if(pet is HavenChelonian){pet.Hits=pet.HitsMax/3;int damage=100;pet.AlterMeleeDamageFrom(foe,ref damage);Check(damage==65,"legendary Living Shell reduces damage 35 percent");}
 if(state!=null)state.Clear();pet.Delete();foe.Delete();foe=Enemy(owner);
 }
 foe.Delete();bystander.Delete();
 var fixture=Pet(owner,new HavenEmberwing());fixture.Name="Pet persistence fixture v2";var target=Enemy(owner);target.Name="Pet training target fixture v2";target.Skills.Wrestling.Base=fixture.Skills.Wrestling.Base;var progress=PetTrainingHelper.GetTrainingProfile(fixture,true);progress.BeginTraining();HavenPetTrainingBridge.Award(target,fixture,10000);var stateFixture=HavenPetSignatures.Ensure(fixture);stateFixture.Field=new HavenSignatureField(fixture,owner,false,3);stateFixture.Field.MoveToWorld(fixture.Location,fixture.Map);fixture.ControlOrder=OrderType.Stay;fixture.Combatant=null;fixture.Frozen=true;target.Combatant=null;target.Frozen=true;World.Save();File.AppendAllText("pet-signature-checks.log","FRESH COMPLETE\n");Core.Kill(false);
 }catch(Exception e){File.AppendAllText("pet-signature-checks.log","FAIL "+e+"\n");Core.Kill(false);}}
}
