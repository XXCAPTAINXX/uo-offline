using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.HavenPrototype;
using PartyGroup=Server.Engines.PartySystem.Party;
public static class CompanionPresenceSmoke
{
 public static void Run(Action<string> log)
 {
  var p=new PlayerMobile{Player=true,Body=0x190};p.AddItem(new Backpack());p.MoveToWorld(new Point3D(3400,2400,0),Map.Felucca);new Server.Accounting.Account("presence-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=p;var c=HavenCompanion.Claim(p);
  if(!HavenCompanionPresence.EnsureParty(c,p)||PartyGroup.Get(c)!=PartyGroup.Get(p))throw new Exception("Auto party");var party=PartyGroup.Get(p);
  if(!HavenCompanionPresence.EnsureParty(c,p)||party.Count!=2)throw new Exception("Duplicate party entry");party.Disband();if(!HavenCompanionPresence.EnsureParty(c,p))throw new Exception("Rejoin disbanded party");
  var listener=new TcpListener(IPAddress.Loopback,0);listener.Start();var client=new TcpClient();client.Connect((IPEndPoint)listener.LocalEndpoint);var net=new NetState(new SocketState(listener.AcceptSocket(),new byte[4]));p.NetState=net;
  HavenCompanionPresence.Maintain();var bar=net.Gumps.OfType<CompanionCombatBarGump>().Single();HavenCompanionPresence.Maintain();if(net.Gumps.OfType<CompanionCombatBarGump>().Single()!=bar)throw new Exception("Bar redrawn while already open");
  p.CloseGump(typeof(CompanionCombatBarGump));HavenCompanionPresence.Maintain();if(!p.HasGump(typeof(CompanionCombatBarGump)))throw new Exception("Missing bar not restored");
  typeof(HavenCompanion).GetField("_missionDue",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(c,DateTime.UtcNow.AddMinutes(5));p.CloseGump(typeof(CompanionCombatBarGump));c.ShowCombatBar(p);if(!p.HasGump(typeof(CompanionCombatBarGump)))throw new Exception("Mission hid combat bar");
  net.Dispose();client.Close();listener.Stop();PartyGroup.Get(p).Disband();c.Delete();p.Delete();log("PASS automatic party, disband recovery, no duplicate membership or redraw, bar restoration and mission visibility");
 }
}
