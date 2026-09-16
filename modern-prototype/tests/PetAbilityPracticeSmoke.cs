using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class PetAbilityPracticeSmoke
{
 public static void RunCast(Action<string> log,Action done)
 {
  var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=100};var pet=new PracticeTestHorse{RawInt=500,RawStr=500};var enemy=new Mongbat{HitsMaxSeed=10000,Hits=10000,Frozen=true};var other=new Mongbat{HitsMaxSeed=10000,Hits=10000,Frozen=true};
  new Server.Accounting.Account("spell-fixture-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;owner.Skills.AnimalTaming.Base=120;owner.Skills.AnimalLore.Base=120;
  var listener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);listener.Start();var client=new System.Net.Sockets.TcpClient();client.Connect((System.Net.IPEndPoint)listener.LocalEndpoint);var net=new Server.Network.NetState(new Server.Network.SocketState(listener.AcceptSocket(),new byte[4]));owner.NetState=net;
  Action clean=()=>{pet.Delete();enemy.Delete();other.Delete();owner.NetState=null;net.Dispose();client.Close();listener.Stop();owner.Delete();done();};
  try
  {
   var map=Map.Felucca;var at=new Point3D(1400,1700,map.GetAverageZ(1400,1700));owner.MoveToWorld(at,map);pet.MoveToWorld(at,map);enemy.MoveToWorld(new Point3D(at.X+2,at.Y,at.Z),map);other.MoveToWorld(new Point3D(at.X+1,at.Y,at.Z),map);pet.SetControlMaster(owner);pet.Loyalty=BaseCreature.MaxLoyalty;pet.ControlOrder=OrderType.Attack;pet.Combatant=enemy;owner.Hits=owner.HitsMax;pet.AIObject.m_Timer.Stop();enemy.AIObject.m_Timer.Stop();other.AIObject.m_Timer.Stop();
   pet.Skills.Spellweaving.Base=80;pet.Mana=500;int before=enemy.Hits,otherBefore=other.Hits,ownerBefore=owner.Hits;
   var spell=HavenLegendaryInnates.ChooseSpell(pet,SkillName.Spellweaving);if(!spell.AcquireIndirectTargets(pet.Location,3).Contains(enemy))throw new Exception("Enemy not eligible for actual cast LOS="+pet.InLOS(enemy));if(!spell.Cast())throw new Exception("Beginner area spell did not start");
   Timer.DelayCall(TimeSpan.FromSeconds(5),()=>{try{if(enemy.Hits>=before)throw new Exception("Beginner spell did not damage engaged enemy mana="+pet.Mana+" spell="+pet.Spell+" active="+HavenLegendaryInnates.Active(pet)+" eligible="+HavenLegendaryInnates.Enemy(pet,enemy)+" combat="+pet.Combatant+" distance="+pet.GetDistanceToSqrt(enemy)+" owneralive="+owner.Alive);if(other.Hits!=otherBefore||owner.Hits!=ownerBefore)throw new Exception("Beginner spell changed bystander/owner "+otherBefore+"->"+other.Hits+" owner "+ownerBefore+"->"+owner.Hits);log("PASS beginner Thunderstorm actually damages engaged enemy and spares owner/unrelated creature");}catch(Exception e){log("FAIL "+e);}finally{clean();}});
  }catch(Exception e){log("FAIL "+e);clean();}
 }
 public static void Run(Action<string> log)
 {
  var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=100};owner.AddItem(new Backpack());
  var pet=new PracticeTestHorse{RawInt=500,RawStr=500};pet.AddItem(new Backpack());var enemy=new Mongbat{Frozen=true,HitsMaxSeed=10000,Hits=10000};
  try
  {
   var map=Map.Felucca;var at=new Point3D(1400,1700,map.GetAverageZ(1400,1700));owner.MoveToWorld(at,map);pet.MoveToWorld(at,map);enemy.MoveToWorld(at,map);pet.SetControlMaster(owner);pet.ControlOrder=OrderType.Attack;pet.Combatant=enemy;owner.Combatant=enemy;pet.AIObject.m_Timer.Stop();
   var record=new HavenLegendaryPetSkills();pet.Backpack.DropItem(record);var rolls=(List<SkillName>)typeof(HavenLegendaryPetSkills).GetField("_boosted",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(record);rolls.AddRange(new[]{SkillName.Magery,SkillName.Necromancy,SkillName.Spellweaving,SkillName.Discordance,SkillName.Healing});
   foreach(var n in HavenLegendaryInnates.RequiredSkills(rolls)){pet.Skills[n].Cap=100;pet.Skills[n].Base=0;pet.Skills[n].SetLockNoRelay(SkillLock.Up);}
   if(!HavenLegendaryInnates.Enemy(pet,enemy))throw new Exception("Enemy fixture invalid");
   var now=DateTime.UtcNow.AddHours(-2);
   if(HavenPetAbilityPractice.AttemptAt(pet,SkillName.Magery,owner,now))throw new Exception("Friendly practice");
   if(HavenPetAbilityPractice.AttemptAt(pet,SkillName.Bushido,enemy,now))throw new Exception("Ungrant practice");
   pet.ControlOrder=OrderType.Stay;if(HavenPetAbilityPractice.AttemptAt(pet,SkillName.Magery,enemy,now))throw new Exception("Stay practice");pet.ControlOrder=OrderType.Attack;
   if(!HavenPetAbilityPractice.AttemptAt(pet,SkillName.Magery,enemy,now)||HavenPetAbilityPractice.AttemptAt(pet,SkillName.Discordance,enemy,now))throw new Exception("Shared practice cooldown");
   for(int i=1;i<160;i++)HavenPetAbilityPractice.AttemptAt(pet,i%2==0?SkillName.Magery:SkillName.Discordance,enemy,now.AddSeconds(i*6));
   if(pet.Skills.Magery.Base<=0||pet.Skills.EvalInt.Base<=0||pet.Skills.Discordance.Base<=0||pet.Skills.Musicianship.Base<=0)throw new Exception("Zero-skill/support gains stuck");
   pet.Skills.Magery.SetLockNoRelay(SkillLock.Locked);pet.Skills.EvalInt.SetLockNoRelay(SkillLock.Down);double magery=pet.Skills.Magery.Base,eval=pet.Skills.EvalInt.Base;pet.Skills.Meditation.Base=100;
   for(int i=160;i<180;i++)HavenPetAbilityPractice.AttemptAt(pet,SkillName.Magery,enemy,now.AddSeconds(i*6));
   if(pet.Skills.Magery.Base!=magery||pet.Skills.EvalInt.Base!=eval||pet.Skills.Meditation.Base!=100)throw new Exception("Locks/caps bypassed");
   pet.Skills.Magery.Base=0;if(!(HavenLegendaryInnates.ChooseSpell(pet,SkillName.Magery) is Server.Spells.First.MagicArrowSpell))throw new Exception("Beginner Magery");pet.Skills.Magery.Base=65;if(!(HavenLegendaryInnates.ChooseSpell(pet,SkillName.Magery) is Server.Spells.Sixth.EnergyBoltSpell))throw new Exception("Advanced Magery");
   pet.Skills.Spellweaving.Base=10;if(!(HavenLegendaryInnates.ChooseSpell(pet,SkillName.Spellweaving) is Server.Spells.Spellweaving.ThunderstormSpell))throw new Exception("Beginner weaving");pet.Skills.Spellweaving.Base=85;if(!(HavenLegendaryInnates.ChooseSpell(pet,SkillName.Spellweaving) is Server.Spells.Spellweaving.WordOfDeathSpell))throw new Exception("Advanced weaving");
   pet.Skills.Necromancy.Base=0;pet.Mana=500;pet.NextSpellTime=0;if(!HavenLegendaryInnates.TryUse(pet,SkillName.Necromancy)||pet.Spell!=null)throw new Exception("Low-skill attempt cast advanced spell");
   var splash=HavenLegendaryInnates.ChooseSpell(pet,SkillName.Spellweaving);pet.Skills.Spellweaving.Base=10;splash=HavenLegendaryInnates.ChooseSpell(pet,SkillName.Spellweaving);if(splash.AcquireIndirectTargets(pet.Location,10).Any(t=>t!=enemy))throw new Exception("Unsafe beginner splash target");
   rolls.Clear();rolls.AddRange(new[]{SkillName.Hiding,SkillName.DetectHidden});pet.Skills.Hiding.Cap=150;pet.Skills.DetectHidden.Cap=150;pet.Skills.Hiding.Base=100;pet.Skills.DetectHidden.Base=100;
   for(int i=0;i<120;i++)HavenPetAbilityPractice.AttemptAt(pet,i%2==0?SkillName.Hiding:SkillName.DetectHidden,enemy,DateTime.UtcNow.AddMinutes(10).AddSeconds(i*6));
   if(pet.Skills.Hiding.Base<=100||pet.Skills.DetectHidden.Base<=100||pet.Skills.Hiding.Base>150||pet.Skills.DetectHidden.Base>150)throw new Exception("Utility overcap training stuck");
   if(HavenPetAbilityPractice.AttemptAt(pet,SkillName.Healing,null,DateTime.UtcNow.AddHours(1)))throw new Exception("Idle healing practice");
   log("PASS native gains from zero plus supporting skills; locks/caps; valid hostile/granted actions only; shared6sec cooldown; beginner/advanced spells; low skill cannot cast advanced spell; no idle healing gains");
  }finally{pet.Delete();enemy.Delete();owner.Delete();}
 }
}

public class PracticeTestHorse:Horse {public override bool CanAutoStable {get{return false;}} public PracticeTestHorse(){} public PracticeTestHorse(Serial s):base(s){} }
