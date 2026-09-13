using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Server.Network;
using Server.Gumps;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
using Server.Spells.SkillMasteries;
public static class PetServicesSmoke {
 static TcpListener listener;static TcpClient client;static NetState net;static PlayerMobile owner;static HavenCompanion bard;static HavenSnowBear bear;static PlayerMobile bearOwner;static HavenChampionCodex archive;static Rabbit wild;static int ticks,initialHits;static bool followingPassed;
 public static void Initialize(){if(File.Exists("PET-SERVICES-TEST-ONLY"))EventSink.ServerStarted+=Run;}
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Run(){try{
  if(File.Exists("pet-services-save.state")){
   var ids=File.ReadAllLines("pet-services-save.state").Select(int.Parse).ToArray();var saved=World.FindItem((Serial)ids[0]) as HavenChampionCodex;
   Require(saved!=null&&saved.Items.Count==599&&saved.GetTotal(TotalType.Weight)==0&&saved.GetTotal(TotalType.Items)==0&&saved.DefaultMaxItems==0&&saved.DefaultMaxWeight==0,"Codex reload lost contents or unlimited storage");
   var savedBear=World.FindMobile((Serial)ids[1]) as HavenSnowBear;Require(savedBear!=null&&savedBear.Skills.Healing.Base>=110,"Bear healing did not persist");var viewer=saved.RootParent as PlayerMobile;var savedBook=viewer.Backpack.FindItemByType(typeof(HavenPetBook),true) as HavenPetBook;
   var planning=new HavenPetTrainingPlanningGump(viewer,savedBear);planning.AddGumpLayout();var info=new HavenPetTrainingInfoGump(viewer);info.AddGumpLayout();
   foreach(var menu in new Gump[]{new PetBookGump(savedBook,0),new HavenPetBulkExchangeConfirm(savedBook),new HavenPetTrainingGump(savedBear),new HavenPetUpgradeGump(savedBear,null,0,0,0),new HavenPetLoreGump(savedBear),planning,info}){
    Require(menu.Entries.OfType<GumpBackground>().Any(b=>b.GumpID==5054),"Pet menu theme missing: "+menu.GetType().Name);
    Require(!menu.Entries.OfType<GumpLabel>().Any(l=>l.Hue==0),"Dark label on dark panel: "+menu.GetType().Name);
   }
   File.WriteAllText("pet-menu-result.txt","PASS sanctuary, bulk confirmation, training, upgrade, lore, planning and info share the dark frame and readable labels. No in-client visual review yet.");
   Finish("PASS second process: 599 Codex scrolls persisted with zero content weight/slots and no limits; bear native healing persisted.");return;
  }
  File.WriteAllLines("pet-services-saved-bards.txt",World.Mobiles.Values.OfType<HavenCompanion>().Select(c=>c.Name+" role="+c.Role+" taming="+c.Skills.AnimalTaming.Value+" lore="+c.Skills.AnimalLore.Value+" peace="+c.Skills.Peacemaking.Value+" music="+c.Skills.Musicianship.Value+" prov="+c.Skills.Provocation.Value+" mana="+c.Mana+" party="+(Server.Engines.PartySystem.Party.Get(c)!=null)));
  owner=new PlayerMobile{Player=true,Body=0x190,RawStr=200};owner.AddItem(new Backpack());new Account("services-test-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;owner.MoveToWorld(new Point3D(4210,2928,0),Map.Trammel);
  var codex=new HavenChampionCodex();archive=codex;owner.Backpack.DropItem(codex);int weight=owner.Backpack.TotalWeight,items=owner.Backpack.TotalItems;
  for(int i=0;i<600;i++)Require(codex.TryDropItem(owner,new PowerScroll(SkillName.Magery,105),false),"Codex rejected scroll "+i);
  Require(codex.Items.Count==600&&owner.Backpack.TotalWeight==weight&&owner.Backpack.TotalItems==items,"Codex contents counted against backpack");
  var junk=new Gold();Require(!codex.CheckHold(owner,junk,false,true,0,0),"Codex accepted unrelated item");junk.Delete();
  Require(codex.Convert(owner,SkillName.Magery,105,false)&&codex.Items.Count==593,"Codex combine failed");Require(codex.Convert(owner,SkillName.Magery,110,true)&&codex.Items.Count==600,"Codex split failed");
  var scroll=codex.Items[0];Require(codex.Withdraw(owner,scroll)&&scroll.Parent==owner.Backpack,"Codex withdrawal lost item");
  var book=HavenPetBook.Ensure(owner);
  for(int tier=0;tier<4;tier++){var pet=new Rabbit();HavenPetRarity.Attach(pet,tier);pet.SetControlMaster(owner);var ticket=HavenPetTicket.Store(pet,owner,book);Require(ticket!=null,"Book storage failed");Require(HavenPetBulkExchangeConfirm.Eligible(owner,ticket,book)==(tier<=1),"Bulk rarity protection failed");ticket.Favorite=true;Require(!HavenPetBulkExchangeConfirm.Eligible(owner,ticket,book),"Favorite exchange allowed");ticket.Favorite=false;pet.IsBonded=true;Require(!HavenPetBulkExchangeConfirm.Eligible(owner,ticket,book),"Bonded exchange allowed");}
  listener=new TcpListener(IPAddress.Loopback,0);listener.Start();client=new TcpClient();client.Connect((IPEndPoint)listener.LocalEndpoint);net=new NetState(new SocketState(listener.AcceptSocket(),new byte[4]));owner.NetState=net;bard=HavenCompanion.Claim(owner);Require(bard!=null&&bard.SetRole(owner,CompanionRole.Bard)&&bard.JoinOwnerParty(owner),"Bard fixture failed");bard.AIObject.m_Timer.Stop();bard.Skills.Peacemaking.Base=105;bard.Skills.Musicianship.Base=100;bard.Skills.Provocation.Base=75;bard.Skills.Discordance.Base=75;
  bearOwner=new PlayerMobile{Player=true,Body=0x190,RawStr=200};bearOwner.MoveToWorld(new Point3D(4210,2935,0),owner.Map);bear=new HavenSnowBear();bear.SetControlMaster(bearOwner);bear.MoveToWorld(bearOwner.Location,bearOwner.Map);bear.AIObject.m_Timer.Stop();bearOwner.Hits=30;initialHits=bearOwner.Hits;bear.SupportHealing();
  wild=new Rabbit();wild.MoveToWorld(new Point3D(4215,2928,0),owner.Map);wild.AIObject.m_Timer.Stop();wild.CantWalk=true;bard.CantWalk=true;
  Timer.DelayCall(TimeSpan.FromSeconds(1),TimeSpan.FromSeconds(1),Tick);
 }catch(Exception e){Finish("FAIL "+e);}}
 static void Tick(){try{
  ticks++;bard.Mana=bard.ManaMax;bool started=bard.ThinkBardMasteries();File.AppendAllText("pet-services-casts.txt",ticks+" start="+started+" support="+bard.CanSupportRole(owner)+" cast="+bard.Spell+" songs="+String.Join(",",(SkillMasterySpell.GetSpells(bard)??new System.Collections.Generic.List<SkillMasterySpell>()).Select(s=>s.GetType().Name+":"+(s.Timer!=null)))+" status="+bard.BardStatus()+Environment.NewLine);
  bool active=SkillMasterySpell.UnderPartyEffects(owner,typeof(ResilienceSpell))&&SkillMasterySpell.UnderPartyEffects(owner,typeof(PerseveranceSpell));
  if(active&&!followingPassed){Require(bearOwner.Hits>initialHits,"Native bear healing failed");followingPassed=true;bard.ClearRoleSupport();Require(bard.StartTamingAssist(owner,wild),"Taming start failed");}
  else if(active&&followingPassed){Require(bard.TamingAssistActive&&bard.ChooseBardMastery(wild)==SkillName.Peacemaking,"Wrong taming mastery");World.Save(false,false);File.WriteAllLines("pet-services-save.state",new[]{archive.Serial.Value.ToString(),bear.Serial.Value.ToString()});Finish("PASS 600 weightless Codex scrolls, combine/split/withdraw; bulk rarity/favorite/bonded protection; native bear healing; both native Peace buffs at 105 Peace/100 Music while following and during Tame assist.");}
  if(ticks>=60)Finish("FAIL bard timeout: "+bard.BardStatus()+" spell="+bard.Spell+" target="+bard.Target);
 }catch(Exception e){Finish("FAIL "+e);}}
 static void Finish(string result){File.WriteAllText("pet-services-result.txt",result);Core.Kill(false);}
}
