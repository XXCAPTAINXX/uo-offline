using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.Items;
using Server.Accounting;
using Server.Mobiles;
using Server.SkillHandlers;
using Server.HavenPrototype;
public class AngryAssistTestHorse:Horse {public override bool CanAngerOnTame{get{return true;}}}
public static class AssistedTameSmoke {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Begin(Action<string> log,Action finish){
  var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=100};owner.AddItem(new Backpack());new Account("tame-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);
  var c=new HavenCompanion();typeof(HavenCompanion).GetField("_owner",flags).SetValue(c,owner);c.SetControlMaster(owner);c.MoveToWorld(owner.Location,owner.Map);c.SetRole(owner,CompanionRole.Bard);
  foreach(var name in new[]{SkillName.AnimalTaming,SkillName.AnimalLore,SkillName.Peacemaking,SkillName.Musicianship}){c.Skills[name].Cap=125;c.Skills[name].Base=125;}
  var horse=new AngryAssistTestHorse{MinTameSkill=-24.9};horse.MoveToWorld(new Point3D(owner.X+1,owner.Y,owner.Z),owner.Map);
  try{
   Check(c.StartTamingAssist(owner,horse),"Assist failed to start");
   Check(!c.CanBeHarmful(horse,false),"Companion can attack active tame target");
   Check(!(bool)typeof(HavenCompanion).GetMethod("ThinkBardMasteries",flags).Invoke(c,null),"Bard cast interfered with taming");
   c.ThinkTamingAssist();
   Check(HavenCompanion.ActivePeace(horse),"Native peace did not succeed");
   Check(AnimalTaming.IsBeingTamed(horse)&&c.Target==null,"Native tame did not start immediately or left a stale cursor");
   var peaceEnd=horse.BardEndTime;c.ThinkTamingAssist();Check(horse.BardEndTime==peaceEnd,"Repeated peace on an already calmed animal");
   var firstAttempt=(DateTime)typeof(HavenCompanion).GetField("_nextTamingAttempt",flags).GetValue(c);
   var pulse=Timer.DelayCall(TimeSpan.FromSeconds(0.5),TimeSpan.FromSeconds(0.5),()=>c.ThinkTamingAssist());
   // Damage deliberately interrupts the first native tame; the next cycle must recover.
   Timer.DelayCall(TimeSpan.FromSeconds(0.5),()=>horse.Damage(1,owner));
   Timer.DelayCall(TimeSpan.FromSeconds(25),()=>{
    try{
     var book=HavenPetBook.Ensure(owner);Check(book!=null,"Missing owner pet book");var ticket=book.Items.OfType<HavenPetTicket>().SingleOrDefault(t=>t.Pet==horse);
     Check(ticket!=null&&!c.TamingAssistActive&&horse.Map==Map.Internal,"Native taming did not complete into owner's book");
     Check(!AnimalTaming.IsBeingTamed(horse)&&c.Target==null,"Tame left timer/cursor state behind");
     Check((DateTime)typeof(HavenCompanion).GetField("_nextTamingAttempt",flags).GetValue(c)>firstAttempt,"Interrupted attempt was not retried");
     log("PASS real peace -> native tame timer -> original pet ticket in book after an interrupted attempt and automatic retry, angry species calmed entry, no repeated peace, no conflicting bard cast, no harmful attack or dangling target");
    }catch(Exception ex){log("FAIL "+ex);}finally{pulse.Stop();c.StopTamingAssist();horse.Delete();c.Delete();owner.Delete();finish();}
   });
  }catch(Exception ex){log("FAIL "+ex);c.StopTamingAssist();horse.Delete();c.Delete();owner.Delete();finish();}
 }
}
