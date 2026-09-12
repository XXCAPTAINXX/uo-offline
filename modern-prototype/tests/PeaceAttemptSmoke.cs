using System;
using System.IO;
using Server;
using Server.HavenPrototype;
public static class PeaceAttemptSmoke {
 public static void Initialize(){if(File.Exists("PEACE-ATTEMPT-TEST-ONLY"))EventSink.ServerStarted+=Run;}
 static void Assert(bool v,string m){if(!v)throw new Exception(m);}
 static void Run(){var c=new HavenCompanion(); var target=new Mobile();
 try {
  var s=c.Skills.Peacemaking;s.Cap=125;s.Base=87.9;s.SetLockNoRelay(SkillLock.Up);
  Assert(HavenCompanionProgression.CheckPeaceAttempt(c,target,0,50),"easy result");
  Assert(s.BaseFixedPoint==882,"easy attempt gain");
  Assert(!HavenCompanionProgression.CheckPeaceAttempt(c,target,140,150),"hard result");
  Assert(s.BaseFixedPoint==885,"failed attempt gain");
  s.SetLockNoRelay(SkillLock.Locked);HavenCompanionProgression.CheckPeaceAttempt(c,target,0,50);
  Assert(s.BaseFixedPoint==885,"locked gain");
  s.SetLockNoRelay(SkillLock.Up);s.Base=125;HavenCompanionProgression.CheckPeaceAttempt(c,target,0,50);
  Assert(s.BaseFixedPoint==1250,"cap overflow");
  File.WriteAllText("peace-attempt-result.txt","PASS: easy and failed attempts gain 0.3 from 87.9; lock and cap respected.");
 }catch(Exception e){File.WriteAllText("peace-attempt-result.txt","FAIL: "+e);}
 finally{c.Delete();target.Delete();Core.Kill(false);}
 }
}

