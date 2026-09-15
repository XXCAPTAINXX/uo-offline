using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
using Server.SkillHandlers;
public static class LegendaryInnateSmoke {
 public static void Run(Action<string> log,Action done){
 var owner=new PlayerMobile{Body=0x190,RawStr=100};owner.AddItem(new Backpack());new Server.Accounting.Account("innate-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;owner.MoveToWorld(new Point3D(3400,2400,Map.Trammel.GetAverageZ(3400,2400)),Map.Trammel);
 var listener=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);listener.Start();var client=new System.Net.Sockets.TcpClient();client.Connect((System.Net.IPEndPoint)listener.LocalEndpoint);var net=new Server.Network.NetState(new Server.Network.SocketState(listener.AcceptSocket(),new byte[4]));owner.NetState=net;
 var pet=new InnateTestHorse{RawInt=500,RawStr=500};pet.MoveToWorld(owner.Location,owner.Map);pet.SetControlMaster(owner);pet.Loyalty=BaseCreature.MaxLoyalty;pet.AIObject.m_Timer.Stop();pet.ControlOrder=OrderType.Attack;
 var enemy=new Mongbat{HitsMaxSeed=100000,Hits=100000,Frozen=true};enemy.MoveToWorld(owner.Location,owner.Map);
 Action clean=()=>{pet.Delete();enemy.Delete();owner.NetState=null;net.Dispose();client.Close();listener.Stop();owner.Delete();done();};
 try{
 var rolls=new[]{SkillName.Discordance,SkillName.EvalInt,SkillName.SpiritSpeak,SkillName.Healing,SkillName.Mysticism};
 foreach(var skill in rolls){pet.Skills[skill].Cap=150;pet.Skills[skill].Base=135;}
 var profile=PetTrainingHelper.GetAbilityProfile(pet,true);int count=profile.AbilityCount();
 HavenLegendaryInnates.Repair(pet,rolls);HavenLegendaryInnates.Repair(pet,rolls);
 if(rolls.Any(s=>pet.Skills[s].Base!=135)||pet.Skills.Musicianship.Base!=100||pet.Skills.Magery.Base!=100||pet.Skills.Necromancy.Base!=100||pet.Skills.Anatomy.Base!=100||pet.Skills.Focus.Base!=100||profile.AbilityCount()!=count)throw new Exception("Repair changed rolls, missed support or spent slots");
 pet.Combatant=enemy;bool discord=false;
 for(int i=0;i<30;i++){pet.Mana=500;pet.NextSkillTime=0;HavenLegendaryInnates.TryUse(pet,SkillName.Discordance);int effect=0;if(Discordance.GetEffect(enemy,ref effect)){discord=true;break;}}
 if(!discord)throw new Exception("Native Discordance did not apply");
 if(HavenLegendaryInnates.Enemy(pet,owner))throw new Exception("Owner targetable");
 pet.Skills.Magery.Base=135;pet.Mana=500;pet.NextSpellTime=0;
 int before=enemy.Hits;
 if(!HavenLegendaryInnates.TryUse(pet,SkillName.Magery))throw new Exception("Granted Magery did not start");
 Timer.DelayCall(TimeSpan.FromSeconds(5),()=>{
 try{if(enemy.Hits>=before)throw new Exception("Granted Magery did not hit: active="+HavenLegendaryInnates.Active(pet)+" enemy="+HavenLegendaryInnates.Enemy(pet,enemy)+" spell="+pet.Spell+" target="+pet.Target+" mana="+pet.Mana+" magery="+pet.Skills.Magery.Value+" hits="+enemy.Hits+" before="+before+" order="+pet.ControlOrder+" combat="+pet.Combatant+" deleted="+pet.Deleted+" alive="+pet.Alive+" deadpet="+pet.IsDeadPet+" controlled="+pet.Controlled+" master="+pet.ControlMaster+" frozen="+pet.Frozen+" paralyzed="+pet.Paralyzed+" map="+pet.Map+" owneralive="+owner.Alive);log("PASS support skills, no rerolls or ability slots, native Discordance debuff, granted Magery damage, owner excluded");Schools(pet,enemy,log,clean);return;}catch(Exception e){log("FAIL "+e);clean();}
 });
 }catch(Exception e){log("FAIL "+e);clean();}
 }
 public static void Schools(BaseCreature pet,BaseCreature enemy,Action<string> log,Action clean){
  var schools=new[]{SkillName.Necromancy,SkillName.Mysticism,SkillName.Spellweaving,SkillName.Chivalry,SkillName.Bushido,SkillName.Ninjitsu};
  foreach(var skill in schools){pet.Skills[skill].Cap=150;pet.Skills[skill].Base=135;}
  int index=0;Action next=null;next=()=>{
   try{
    if(index==schools.Length){log("PASS remaining six innate spell schools cast and spend mana");clean();return;}
    var school=schools[index++];pet.Mana=500;pet.NextSpellTime=0;pet.Combatant=enemy;
    if(!HavenLegendaryInnates.TryUse(pet,school))throw new Exception("Cannot cast "+school);
    Timer.DelayCall(TimeSpan.FromSeconds(school==SkillName.Spellweaving?5:3),()=>{
     try{if(pet.Mana>=500)throw new Exception("No cast result for "+school);next();}catch(Exception e){log("FAIL "+e);clean();}
    });
   }catch(Exception e){log("FAIL "+e);clean();}
  };next();
 }}





public class InnateTestHorse:Horse {public override bool CanAutoStable {get{return false;}} public InnateTestHorse(){} public InnateTestHorse(Serial s):base(s){} }


