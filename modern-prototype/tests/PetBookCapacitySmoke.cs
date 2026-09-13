using System;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class PetBookCapacitySmoke {
 public static void Run(Action<string> log){
 var owner=new PlayerMobile();owner.AddItem(new Backpack());new Server.Accounting.Account("book-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"))[0]=owner;var book=new HavenPetBook(owner);owner.Backpack.DropItem(book);
 var first=new HavenPetTicket(owner,0);var second=new HavenPetTicket(owner,0);var wrong=new Gold(1);
 try{
 int count=owner.Backpack.TotalItems,weight=owner.Backpack.TotalWeight;owner.Backpack.MaxItems=count;
 if(!book.TryDropItem(owner,first,false)||!book.TryDropItem(owner,second,false))throw new Exception("Full backpack blocks book entries");
 if(owner.Backpack.TotalItems!=count||owner.Backpack.TotalWeight!=weight)throw new Exception("Pet records count toward backpack capacity");
 owner.UpdateTotals();if(owner.Backpack.TotalItems!=count||owner.Backpack.TotalWeight!=weight)throw new Exception("Recount exposes records");
 if(book.TryDropItem(owner,wrong,false))throw new Exception("Accepts ordinary items");
 book.MaxItems=2;var extra=new HavenPetTicket(owner,0);try{if(book.TryDropItem(owner,extra,false))throw new Exception("Book limit lost");}finally{extra.Delete();}
 owner.Backpack.MaxItems=125;owner.Backpack.DropItem(first);if(owner.Backpack.TotalItems!=count+1)throw new Exception("Withdrawn ticket not counted");
 if(!book.TryDropItem(owner,first,false)||owner.Backpack.TotalItems!=count)throw new Exception("Returned ticket count wrong");
 book.MaxItems=500;var bonded=new Horse();bonded.SetControlMaster(owner);bonded.IsBonded=true;var stored=HavenPetTicket.Store(bonded,owner,owner.Backpack);if(stored==null||stored.Parent!=book||bonded.Controlled||owner.Backpack.TotalItems!=count)throw new Exception("Bonded shrink failed");
 log("PASS bonded shrink; book stays one item at full backpack capacity, stored ticket weight excluded, recount stable, withdrawn tickets count normally, ordinary items denied, book limit preserved");
 }finally{wrong.Delete();owner.Delete();if(!first.Deleted)first.Delete();if(!second.Deleted)second.Delete();}
 }
}

