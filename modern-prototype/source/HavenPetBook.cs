using System;
using System.Linq;
using Server.Accounting;
using Server.Commands;
using Server.Items;
using Server.Gumps;
using Server.Network;

namespace Server.HavenPrototype
{
    public class HavenPetBook : Bag
    {
        public Mobile Owner;
        public static void Initialize(){CommandSystem.Register("petbook",AccessLevel.Player,e=>{var book=Ensure(e.Mobile);if(book!=null)book.OnDoubleClick(e.Mobile);});}
        public HavenPetBook(Mobile owner){Owner=owner;ItemID=0x2259;Name="Bonded pet book";LootType=LootType.Blessed;Weight=1;MaxItems=500;}
        public HavenPetBook(Serial serial):base(serial){}
        public bool CanUse(Mobile p){return p!=null&&p.Alive&&p==Owner&&!Deleted&&p.Backpack!=null&&IsChildOf(p.Backpack)&&HavenResources.Accessible(p,this);}
        public static HavenPetBook Ensure(Mobile p)
        {
            if(p==null||!p.Alive||p.Backpack==null||!(p.Account is Account))return null;
            var book=p.Backpack.FindItemsByType(typeof(HavenPetBook),true).Cast<HavenPetBook>().FirstOrDefault(b=>b.Owner==p);
            if(book!=null)return book;
            book=new HavenPetBook(p);
            if(!p.Backpack.TryDropItem(p,book,false)){book.Delete();p.SendMessage("Make room for your bonded pet book.");return null;}
            return book;
        }
        public override bool OnDragDrop(Mobile p,Item item)
        {
            var ticket=item as HavenPetTicket;
            if(!CanUse(p)||ticket==null||ticket.Owner!=p||ticket.Pet==null||ticket.Pet.Deleted||!ticket.Pet.IsBonded)return false;
            return TryDropItem(p,ticket,false);
        }
        public override bool OnDragDropInto(Mobile p,Item item,Point3D point){return OnDragDrop(p,item);}
        public override void OnDoubleClick(Mobile p)
        {
            if(!CanUse(p))return;
            foreach(var ticket in p.Backpack.FindItemsByType(typeof(HavenPetTicket),true).Cast<HavenPetTicket>().Where(t=>t.Owner==p&&t.Pet!=null&&!t.Pet.Deleted&&t.Pet.IsBonded&&t.Parent!=this).ToArray())
                if(HavenResources.Accessible(p,ticket))TryDropItem(p,ticket,false);
            p.CloseGump(typeof(PetBookGump));p.SendGump(new PetBookGump(this,0));
        }
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Owner);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Owner=r.ReadMobile();}
    }
    public class PetBookGump:HavenMenuGump
    {
        readonly HavenPetBook _book;readonly HavenPetTicket[] _tickets;readonly int _page;
        public PetBookGump(HavenPetBook book,int page):base(50,50)
        {
            _book=book;var all=book.Items.OfType<HavenPetTicket>().Where(t=>!t.Deleted&&t.Pet!=null&&!t.Pet.Deleted).ToArray();
            _page=Math.Max(0,Math.Min(page,Math.Max(0,(all.Length-1)/6)));_tickets=all.Skip(_page*6).Take(6).ToArray();
            AddBackground(0,0,660,420,3000);AddLabel(24,22,0,"BONDED PET BOOK | "+all.Length+" pets");
            AddLabel(24,54,0,"Protected from exchange. Release brings back the same pet.");
            for(int i=0;i<_tickets.Length;i++){int y=94+i*42;AddLabel(24,y,0,_tickets[i].Pet.Name);FlatButton(418,y,90,100+i*2,"Lore");FlatButton(522,y,110,101+i*2,"Release");}
            if(all.Length==0)AddLabel(24,100,0,"Shrink a bonded pet, or open this book to collect its ticket.");
            FlatButton(24,365,120,1,"Previous");AddLabel(180,365,0,"Page "+(_page+1));FlatButton(320,365,120,2,"Next");FlatButton(522,365,110,0,"Close");
        }
        public override void OnResponse(NetState sender,RelayInfo info)
        {
            var p=sender.Mobile;if(info.ButtonID==0||!_book.CanUse(p))return;
            int id=info.ButtonID;
            if(id>=100){int index=(id-100)/2;if(index>=_tickets.Length)return;var t=_tickets[index];if(t.Deleted||t.Parent!=_book||t.Owner!=p)return;
                if(id%2==0){t.ShowLore(p);return;}
                if(!t.Claim(p))p.SendMessage("Cannot release: check follower slots and clear space nearby. Your pet stays in the book.");
            }
            p.SendGump(new PetBookGump(_book,_page+(id==1?-1:id==2?1:0)));
        }
    }
}
