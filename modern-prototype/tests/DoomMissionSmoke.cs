using System;
using System.Reflection;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Server.Network;
using Server.Gumps;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Engines.Points;
using Server.HavenPrototype;
public static class DoomMissionSmoke {
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Set(HavenCompanion c,string field,object value){typeof(HavenCompanion).GetField(field,Flags).SetValue(c,value);}
 static void Complete(HavenCompanion c){typeof(HavenCompanion).GetMethod("CompleteDueMission",Flags).Invoke(c,null);}
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 public static void Run(Action<string> log){
  var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=100};owner.AddItem(new Backpack());owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);
  var c=new HavenCompanion();Set(c,"_owner",owner);c.MoveToWorld(owner.Location,owner.Map);
  try{
   Check((int)CompanionMission.DoomRecon==26&&(int)CompanionMission.DragonSalvage==25,"Legacy mission enum changed");
   c.Skills.MagicResist.Base=0;c.Skills.Tactics.Base=99.9;c.Skills.Magery.Base=0;c.Skills.Archery.Base=0;
   Check(!HavenRegionalMissions.CanStart(c,CompanionMission.DoomRecon),"Doom requirement bypassed");c.Skills.Tactics.Base=100;Check(HavenRegionalMissions.CanStart(c,CompanionMission.DoomRecon),"Doom still requires Resistance");
   double expected=12345;PointsSystem.DoomGauntlet.SetPoints(owner,expected);
   foreach(int minutes in new[]{5,15,30,60}){
    Check((bool)typeof(HavenCompanion).GetMethod("PrepareResourceMission",Flags).Invoke(c,new object[]{CompanionMission.DoomRecon,minutes}),"Prepare failed");
    int pending=HavenDoomMissionLoot.Pending(owner);Set(c,"_missionMinutes",minutes);Set(c,"_missionDue",DateTime.UtcNow.AddSeconds(-1));Complete(c);
    expected+=minutes*500;if(HavenDoomMissionLoot.Pending(owner)==pending+1)expected=0;Check(PointsSystem.DoomGauntlet.GetPoints(owner)==expected,"Doom points not additive/reset on artifact");
    Complete(c);c.DeliverRewards();Check(PointsSystem.DoomGauntlet.GetPoints(owner)==expected,"Duplicate completion awarded points");
   }
   typeof(HavenCompanion).GetMethod("PrepareResourceMission",Flags).Invoke(c,new object[]{CompanionMission.DoomRecon,60});Set(c,"_missionMinutes",60);Set(c,"_missionDue",DateTime.UtcNow.AddHours(1));
   c.Recall(owner);Check(!c.OnMission&&PointsSystem.DoomGauntlet.GetPoints(owner)==expected,"Early recall awarded points");
   HavenDoomMission.Complete(owner,CompanionMission.DoomBones,60);Check(PointsSystem.DoomGauntlet.GetPoints(owner)==expected,"Other mission awarded Doom points");
   Check(HavenDoomMission.Points(-1)==0&&HavenDoomMission.Points(999)==0,"Invalid duration accepted");
   Check(HavenDoomStatus.Chance(30000)>HavenDoomStatus.Chance(0),"Doom chance did not increase");
   Check(!HavenDoomMission.RollArtifact(owner,1,0),"Out of range roll awarded artifact");
   Check(HavenDoomMission.RollArtifact(owner,0,0),"Winning roll lost artifact");
   Check(PointsSystem.DoomGauntlet.GetPoints(owner)==0,"Artifact did not reset points");
   var parcels=HavenDoomMissionLoot.For(owner);var artifacts=parcels.SelectMany(x=>x.Items).ToArray();
   Check(artifacts.Length>0&&artifacts.All(x=>DoomGauntlet.DoomArtifact.Contains(x.GetType())),"Wrong artifact pool");
   var stranger=new PlayerMobile{Player=true,Body=0x190};stranger.AddItem(new Backpack());Check(HavenDoomMissionLoot.Collect(stranger)==0,"Other owner stole return");stranger.Delete();
   owner.Backpack.DropItem(new Gold(1));int max=owner.Backpack.MaxItems;owner.Backpack.MaxItems=1;
   Check(HavenDoomMissionLoot.Collect(owner)==0&&artifacts.All(x=>x.Parent is HavenDoomMissionLoot),"Full pack lost loot");owner.Backpack.MaxItems=max;
   Check(HavenDoomMissionLoot.Collect(owner)==artifacts.Length&&artifacts.All(x=>x.Parent==owner.Backpack),"Collect lost item identity");Check(HavenDoomMissionLoot.Collect(owner)==0,"Duplicate collection");
   var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();var client=new TcpClient();client.Connect((IPEndPoint)listener.LocalEndpoint);var net=new NetState(new SocketState(listener.AcceptSocket(),new byte[4]));owner.NetState=net;net.Mobile=owner;
   try{
    foreach(int selection in new[]{12,13,14}){
     new CompanionActivityGump(c,0,12,60).OnResponse(net,new RelayInfo(100+selection,new int[0],new TextRelay[0]));
     var menu=net.Gumps.OfType<CompanionActivityGump>().Last();
     Check((int)typeof(CompanionActivityGump).GetField("_selection",Flags).GetValue(menu)==selection,"New mission click ignored: "+selection);
    }
    Check(HavenDoomMission.Preview(owner,60).Contains("% -> "),"Missing percentage comparison");
   }finally{owner.NetState=null;net.Dispose();client.Close();listener.Stop();}
   log("PASS four durations add real Doom points to an offline owner; duplicate completion and reward collection do not pay twice; early recall forfeits; other missions excluded; existing IDs and points preserved; skill gating, displayed chance growth and actual Heartwood/Eodon/Doom button responses; native artifact wins/reset, owner-only protected loot, full-pack retention and duplicate collection");
  }finally{PointsSystem.DoomGauntlet.SetPoints(owner,0);c.Delete();owner.Delete();}
 }
}
