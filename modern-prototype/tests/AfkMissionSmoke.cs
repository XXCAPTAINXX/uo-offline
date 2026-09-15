using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.Network;
using Server.HavenPrototype;
public static class AfkMissionSmoke {
 public static void Run(Action<string> log){
  var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=250};owner.AddItem(new Backpack());new Account("afk-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;owner.MoveToWorld(new Point3D(3400,2400,Map.Trammel.GetAverageZ(3400,2400)),Map.Trammel);
  var c=HavenCompanion.Claim(owner);var plan=HavenOfflineMissionPlan.Ensure(c);plan.Enabled=true;plan.Focus=CompanionMission.Supply;
  var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();var client=new TcpClient();client.Connect((IPEndPoint)listener.LocalEndpoint);var net=new NetState(new SocketState(listener.AcceptSocket(),new byte[4]));owner.NetState=net;
  try{
   HavenAfkMissions.Activity(owner);plan.Tick();if(c.OnMission)throw new Exception("Active owner sent away");
   HavenAfkMissions.Set(owner,true);plan.Tick();if(!c.OnMission||!plan.ActiveTrip)throw new Exception("Manual AFK did not dispatch");
   var due=c.MissionDue;plan.Tick();if(c.MissionDue!=due)throw new Exception("Repeated pulse replaced trip");
   HavenAfkMissions.Activity(owner,true);plan.RecallOnLogin=false;plan.Tick();if(!c.OnMission||c.MissionDue!=due)throw new Exception("Return did not preserve trip");
   plan.RecallOnLogin=true;plan.Tick();if(c.OnMission)throw new Exception("Recall on return failed");
   var states=typeof(HavenAfkMissions).GetField("States",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);var state=states.GetType().GetMethod("GetOrCreateValue").Invoke(states,new object[]{owner});state.GetType().GetField("Activity").SetValue(state,DateTime.UtcNow.AddMinutes(-6));
   plan.AutoAfk=false;plan.Tick();if(c.OnMission)throw new Exception("Disabled automatic AFK dispatched");
   plan.AutoAfk=true;plan.Tick();if(c.OnMission)throw new Exception("Legacy automatic AFK still dispatched");
   HavenAfkMissions.Activity(owner);plan.Tick();if(c.OnMission)throw new Exception("Activity did not end auto AFK");
   HavenAfkMissions.Set(owner,true);plan.Enabled=false;plan.Tick();if(c.OnMission)throw new Exception("Disabled mission plan dispatched");
   var now=DateTime.UtcNow;if(HavenAfkMissions.IsIdle(now.AddSeconds(-299),now)||!HavenAfkMissions.IsIdle(now.AddMinutes(-5),now))throw new Exception("Idle boundary failed");
   plan.Enabled=true;HavenAfkMissions.Set(owner,true);
   var enemy=new Mongbat{HitsMaxSeed=100000,Hits=100000,Frozen=true};enemy.MoveToWorld(c.Location,c.Map);
   var pet=new Horse{Frozen=true};pet.SetControlMaster(owner);pet.MoveToWorld(c.Location,c.Map);
   try{pet.Combatant=enemy;plan.Tick();if(c.OnMission||!HavenAfkMissions.CombatActive(c,owner))throw new Exception("Companion left during pet combat");pet.Combatant=null;enemy.Combatant=c;plan.Tick();if(c.OnMission)throw new Exception("Companion left while being targeted");}finally{pet.Delete();enemy.Delete();}
   new HavenOfflineMissionGump(plan);
   log("PASS connected manual AFK dispatch; automatic AFK disabled even for legacy flags; pet combat and hostile targeting block departure; no duplicate trip; finish/recall on return; activity reset; disabled plan and auto switch; five-minute threshold; settings construction");
  }finally{owner.NetState=null;net.Dispose();client.Close();listener.Stop();c.Delete();owner.Delete();}
 }
}
