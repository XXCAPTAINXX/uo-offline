using System;
using System.Net;
using System.Net.Sockets;
using Server.Network;
using System.IO;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.SkillHandlers;
using Server.HavenPrototype;
public static class PetTamingSmoke {
 static TcpListener listener;static TcpClient client;static NetState net;static PlayerMobile owner;static HavenCompanion companion;static Chicken animal;static int ticks,stage;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);File.AppendAllText("pet-taming-checks.log","PASS "+name+"\n");}
 public static void Initialize(){if(File.Exists("PET-TAMING-TEST-ONLY"))EventSink.ServerStarted+=()=>Timer.DelayCall(TimeSpan.FromSeconds(8),Run);}
 static Chicken NewAnimal(){var a=new Chicken{CantWalk=true};a.MoveToWorld(owner.Location,owner.Map);return a;}
 static void Run(){try{
 owner=new PlayerMobile{Player=true,Body=0x190,RawStr=250};owner.AddItem(new Backpack());var account=new Account("tame-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"));account[0]=owner;owner.MoveToWorld(new Point3D(1015,527,-65),Map.Malas);companion=HavenCompanion.Claim(owner);companion.SetRole(owner,CompanionRole.Bard);
 foreach(var n in new[]{SkillName.AnimalTaming,SkillName.AnimalLore,SkillName.Musicianship,SkillName.Peacemaking}){companion.Skills[n].Cap=125;companion.Skills[n].Base=125;}
 listener=new TcpListener(IPAddress.Loopback,0);listener.Start();client=new TcpClient();client.Connect((IPEndPoint)listener.LocalEndpoint);net=new NetState(new SocketState(listener.AcceptSocket(),new byte[4]));owner.NetState=net;
 animal=NewAnimal();Check(companion.StartTamingAssist(owner,animal),"Bard starts assisted taming");Timer.DelayCall(TimeSpan.FromSeconds(1),Poll);
 }catch(Exception e){Fail(e);}}
 static void Poll(){try{ticks++;if(ticks>90)throw new Exception("native taming timed out stage="+stage+" pacified="+animal.BardPacified+" taming="+AnimalTaming.IsBeingTamed(animal));
 if(stage==0){companion.ThinkTamingAssist();if(animal.BardPacified){Check(true,"native Peacemaking pacifies target");stage=1;}}
 if(stage==1){var ticket=owner.Backpack.FindItemsByType(typeof(HavenPetTicket),true).Cast<HavenPetTicket>().FirstOrDefault(x=>x.Pet==animal);if(ticket!=null){Check(!companion.TamingAssistActive&&animal.Map==Map.Internal&&ticket.Parent is HavenPetBook,"native taming delivers exact animal to owner book");owner.Backpack.DropItem(ticket);Check(ticket.Claim(owner)&&animal.ControlMaster==owner,"owner claims assisted tame");Check(!owner.Criminal,"assisted peace does not flag owner criminal");animal.Delete();animal=NewAnimal();Check(companion.StartTamingAssist(owner,animal),"start second tame for cancellation");stage=2;}}
 if(stage==2){companion.ThinkTamingAssist();if(AnimalTaming.IsBeingTamed(animal)){companion.StopTamingAssist();stage=3;ticks=0;}}
 if(stage==3&&ticks>=8){Check(!AnimalTaming.IsBeingTamed(animal)&&!animal.Controlled,"cancel stops native timer without taming pet");animal.Delete();var horse=new Horse{IsBonded=true};horse.MoveToWorld(owner.Location,owner.Map);horse.SetControlMaster(companion);horse.IsBonded=true;horse.Kill();Check(horse.IsDeadPet,"assigned pet dies bonded");Check(companion.SupportPatient(horse)&&!horse.IsDeadPet,"companion resurrects assigned pet");File.AppendAllText("pet-taming-checks.log","COMPLETE\n");Core.Kill(false);return;}
 Timer.DelayCall(TimeSpan.FromSeconds(1),Poll);
 }catch(Exception e){Fail(e);}}
 static void Fail(Exception e){File.AppendAllText("pet-taming-checks.log","FAIL "+e+"\n");Core.Kill(false);}
}
