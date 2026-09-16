using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Accounting;
using Server.Mobiles;
using Server.Network;
using Server.HavenPrototype;
public static class StoryVoiceSmoke
{
 class Capture:NetState{public readonly List<int> Sounds=new List<int>();public Capture(Socket socket):base(new SocketState(socket,new byte[4])){}public override void Send(Packet packet){if(packet is PlaySound){int length;var data=packet.Compile(false,out length);Sounds.Add((data[2]<<8)|data[3]);}else base.Send(packet);}}
 public static void Run(Action<string> log,Action done)
 {
  var p=new PlayerMobile{Player=true,Body=0x190};new Account("voice-test-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=p;
  var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();var client=new TcpClient();client.Connect((IPEndPoint)listener.LocalEndpoint);var net=new Capture(listener.AcceptSocket());p.NetState=net;net.Mobile=p;net.Account=p.Account;p.MoveToWorld(new Point3D(3506,2570,14),Map.Trammel);
  var durations=HavenStoryVoice.VoiceSeconds.ToArray();for(int i=0;i<durations.Length;i++)HavenStoryVoice.VoiceSeconds[i]=0.01;
  Action finish=()=>{HavenStoryVoice.Clear(p);for(int i=0;i<durations.Length;i++)HavenStoryVoice.VoiceSeconds[i]=durations[i];p.NetState=null;net.Dispose();client.Close();listener.Stop();p.Delete();done();};
  try{HavenBeaconQuest.Speak(p,0);HavenBeaconQuest.Speak(p,5);HavenBeaconQuest.Speak(p,6);HavenBeaconQuest.Speak(p,5);}
  catch(Exception e){log("FAIL "+e);finish();return;}
  Timer.DelayCall(TimeSpan.FromSeconds(1.6),()=>{try{
   if(!net.Sounds.SequenceEqual(new[]{32750,32755,32756}))throw new Exception("Voice sequence: "+String.Join(",",net.Sounds));
   HavenBeaconQuest.Speak(p,7);HavenBeaconQuest.Speak(p,5);HavenBeaconQuest.ToggleVoice(p);int atMute=net.Sounds.Count;if(HavenBeaconQuest.VoiceEnabled(p))throw new Exception("Mute not applied: CanUse="+HavenMarks.CanUse(p)+" alive="+p.Alive+" deleted="+p.Deleted);
   Timer.DelayCall(TimeSpan.FromSeconds(0.8),()=>{try{if(net.Sounds.Count!=atMute)throw new Exception("Muted queue packets: "+String.Join(",",net.Sounds));log("PASS captured actual voice packets: greeting, cape and horse in order; duplicate queued line suppressed; mute clears later pending audio");}catch(Exception e){log("FAIL "+e);}finally{finish();}});
  }catch(Exception e){log("FAIL "+e);finish();}});
 }
}
