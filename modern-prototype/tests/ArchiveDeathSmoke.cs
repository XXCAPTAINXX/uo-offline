using System;
using System.IO;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class ArchiveDeathSmoke {
 public static void Initialize(){if(File.Exists("ARCHIVE-DEATH-TEST-ONLY"))EventSink.ServerStarted+=Run;}
 static void Require(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Run(){try{
  if(File.Exists("archive-death-save.state")){
   var ids=File.ReadAllLines("archive-death-save.state").Select(int.Parse).ToArray();var savedBook=World.FindItem((Serial)ids[0]) as HavenPetBook;var savedTicket=World.FindItem((Serial)ids[1]) as HavenPetTicket;var savedCodex=World.FindItem((Serial)ids[3]) as HavenChampionCodex;
   Require(savedBook!=null&&savedTicket!=null&&savedTicket.Parent==savedBook&&savedTicket.Pet!=null&&savedTicket.Pet.Serial.Value==ids[2]&&!savedTicket.Pet.Deleted&&savedCodex.Items.Count==150,"Archive contents/identity lost on reload");
   File.WriteAllText("archive-death-result.txt","PASS second process: exact pet/ticket/book identity and 150 archived scrolls survived death, recovery, save and reload.");Core.Kill(false);return;
  }
  var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=200,Young=false};owner.AddItem(new Backpack());new Account("archive-death-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;owner.MoveToWorld(new Point3D(4210,2928,0),Map.Trammel);
  var book=HavenPetBook.Ensure(owner);var pet=new Rabbit();HavenPetRarity.Attach(pet,3);pet.SetControlMaster(owner);var ticket=HavenPetTicket.Store(pet,owner,book);Require(ticket!=null,"Fixture storage failed");
  var codex=new HavenChampionCodex();owner.Backpack.DropItem(codex);for(int i=0;i<150;i++)Require(codex.TryDropItem(owner,new PowerScroll(SkillName.Magery,105),false),"Scroll fixture rejected");
  var outer=new Bag();owner.Backpack.DropItem(outer);outer.DropItem(book);outer.DropItem(codex);var blessed=new Gold{LootType=LootType.Blessed};outer.DropItem(blessed);
  owner.Kill();Require(!owner.Alive,"Death not executed");Require(!book.Deleted&&book.Parent==owner.Backpack&&ticket.Parent==book&&ticket.Pet==pet&&!pet.Deleted,"Death removed pet from book");Require(codex.Parent==owner.Backpack&&codex.Items.Count==150,"Death emptied Codex");Require(blessed.Parent==owner.Backpack,"Ordinary blessed-item protection changed");
  owner.Resurrect();owner.Backpack.AddItem(ticket);Require(book.Items.Count==0,"Loose-ticket fixture failed");book.OnDoubleClick(owner);Require(ticket.Parent==book&&ticket.Pet==pet,"Opening book failed to recover displaced Legendary ticket");
  World.Save(false,false);File.WriteAllLines("archive-death-save.state",new[]{book.Serial.Value.ToString(),ticket.Serial.Value.ToString(),pet.Serial.Value.ToString(),codex.Serial.Value.ToString()});
  File.WriteAllText("archive-death-result.txt","PASS actual player death preserved nested book pet and 150 Codex scrolls; ordinary blessed-item protection retained; opening book recovered the exact displaced ticket/pet; saved for reload.");
 }catch(Exception e){File.WriteAllText("archive-death-result.txt","FAIL "+e);}Core.Kill(false);}
}
