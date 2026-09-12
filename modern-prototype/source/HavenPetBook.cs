using System;
using System.Linq;
using Server.Accounting;
using Server.Commands;
using Server.Items;
using Server.Gumps;
using Server.Network;
using Server.Targeting;

namespace Server.HavenPrototype
{
    public class HavenPetBook : Bag
    {
        public Mobile Owner;
        public static void Initialize(){CommandSystem.Register("petbook",AccessLevel.Player,e=>{var book=Ensure(e.Mobile);if(book!=null)book.OnDoubleClick(e.Mobile);});}
        public HavenPetBook(Mobile owner){Owner=owner;ItemID=0xFF4;Hue=0x59D;Name="Pet sanctuary book";LootType=LootType.Blessed;Weight=1;MaxItems=500;}
        public HavenPetBook(Serial serial):base(serial){}
        public bool CanUse(Mobile p){return p!=null&&p.Alive&&p==Owner&&!Deleted&&p.Backpack!=null&&IsChildOf(p.Backpack)&&HavenResources.Accessible(p,this);}
        public static HavenPetBook Ensure(Mobile p)
        {
            if(p==null||!p.Alive||p.Backpack==null||!(p.Account is Account))return null;
            var book=p.Backpack.FindItemsByType(typeof(HavenPetBook),true).Cast<HavenPetBook>().FirstOrDefault(b=>b.Owner==p);
            if(book!=null)return book;
            book=new HavenPetBook(p);
            if(!p.Backpack.TryDropItem(p,book,false)){book.Delete();p.SendMessage("Make room for your pet sanctuary book.");return null;}
            return book;
        }
        public override bool OnDragDrop(Mobile p,Item item)
        {
            var ticket=item as HavenPetTicket;
            if(!CanUse(p)||ticket==null||ticket.Deleted||ticket.Owner!=p||ticket.Pet==null||ticket.Pet.Deleted||ticket.Pet.Controlled||ticket.Pet.Map!=Map.Internal||!ticket.IsChildOf(p.Backpack)||!HavenResources.Accessible(p,ticket))return false;
            return TryDropItem(p,ticket,false);
        }
        public void BeginAdd(Mobile p)
        {
            if(!CanUse(p))return;
            p.SendMessage("Target one of your pet tickets in your backpack. Stored tickets are protected from exchange.");
            p.Target=new AddTicketTarget(this);
        }
        private sealed class AddTicketTarget:Target
        {
            private readonly HavenPetBook _book;
            public AddTicketTarget(HavenPetBook book):base(12,false,TargetFlags.None){_book=book;}
            protected override void OnTarget(Mobile p,object target)
            {
                if(!_book.CanUse(p))return;
                p.SendMessage(target is HavenPetTicket && _book.OnDragDrop(p,(Item)target)?"Pet added to your sanctuary book.":"Choose your own available pet ticket in your backpack, and make sure the book has room.");
                p.SendGump(new PetBookGump(_book,0));
            }
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
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Owner=r.ReadMobile();if(ItemID==0x2259){ItemID=0xFF4;Hue=0x59D;Name="Pet sanctuary book";}}
    }
    public class PetBookGump:HavenMenuGump
    {
        readonly HavenPetBook _book; readonly HavenPetTicket[] _tickets; readonly int _page,_sort,_filter;readonly string _search;
        static readonly string[] Sorts={"Recently added","Rarity","Name","Follower slots","Taming skill"};
        static readonly string[] Filters={"All pets","Favorites","Exchangeable","Bonded","Legendary"};
        public PetBookGump(HavenPetBook book,int page,int sort=0,int filter=0,string search=""):base(50,50)
        {
            _book=book;_sort=Math.Max(0,Math.Min(4,sort));_filter=Math.Max(0,Math.Min(4,filter));_search=(search??"").Trim();if(_search.Length>60)_search=_search.Substring(0,60);
            var query=book.Items.OfType<HavenPetTicket>().Where(t=>!t.Deleted&&t.Owner==book.Owner&&t.Pet!=null&&!t.Pet.Deleted);
            if(_filter==1)query=query.Where(t=>t.Favorite);
            if(_search.Length>0)query=query.Where(t=>(t.Pet.Name??"").IndexOf(_search,StringComparison.OrdinalIgnoreCase)>=0);
            if(_filter==2)query=query.Where(t=>HavenPetExchange.Eligible(book.Owner,t,book));
            if(_filter==3)query=query.Where(t=>t.Pet.IsBonded);
            if(_filter==4)query=query.Where(t=>Math.Max(t.Rarity,HavenPetDefenses.Tier(t.Pet))>=3);
            var ordered=_sort==0?query.OrderByDescending(t=>t.AcquiredAt):_sort==2?query.OrderBy(t=>t.Pet.Name):_sort==3?query.OrderBy(t=>t.Pet.ControlSlots):_sort==4?query.OrderBy(t=>t.Pet.MinTameSkill):query.OrderByDescending(t=>Math.Max(t.Rarity,HavenPetDefenses.Tier(t.Pet)));
            var all=ordered.ThenBy(t=>t.Pet.Name).ThenBy(t=>t.Serial.Value).ToArray();
            int pages=Math.Max(1,(all.Length+5)/6);_page=Math.Max(0,Math.Min(page,pages-1));_tickets=all.Skip(_page*6).Take(6).ToArray();
            AddBackground(0,0,850,585,3000);
            AddLabel(24,20,0,"PET SANCTUARY | "+book.Items.OfType<HavenPetTicket>().Count()+" pets");
            AddLabel(510,20,0,"Pet credits: "+HavenPetExchange.Balance(book.Owner));
            FlatButton(24,57,210,3,"Add pet ticket...");FlatButton(248,57,230,4,"Sort: "+Sorts[_sort]);FlatButton(492,57,240,5,"Show: "+Filters[_filter]);
            AddBackground(24,92,365,28,9350);AddTextEntry(30,96,350,22,0,1,_search);FlatButton(404,94,100,7,"Search");FlatButton(514,94,90,8,"Clear");
            AddLabel(24,129,0,"Favorites, bonded and Legendary pets are protected. Exchanges require confirmation.");
            for(int i=0;i<_tickets.Length;i++){
                var t=_tickets[i];var pet=t.Pet;int y=169+i*52;int tier=Math.Max(t.Rarity,HavenPetDefenses.Tier(pet));
                string name=pet.Name??"Unnamed pet";if(name.Length>42)name=name.Substring(0,39)+"...";
                AddLabel(24,y,0,(t.Favorite?"* ":"")+name);
                AddLabel(24,y+19,0,HavenPetRarity.Label(tier)+" | "+pet.ControlSlots+" slots | Taming "+pet.MinTameSkill.ToString("0.0")+(pet.IsBonded?" | Bonded":""));
                FlatButton(420,y+6,84,100+i*3,"Lore");FlatButton(514,y+6,90,101+i*3,"Claim");
                FlatButton(742,y+6,84,300+i,t.Favorite?"Unfavorite":"Favorite");
                if(HavenPetExchange.Eligible(book.Owner,t,book))FlatButton(614,y+6,118,102+i*3,"Exchange +"+HavenPetExchange.Value(t));
                else AddLabel(626,y+8,0,"Protected");
                AddLabel(24,y+34,0,t.AcquiredAt==DateTime.MinValue?"Added: unknown (older ticket)":"Added: "+t.AcquiredAt.ToString("yyyy-MM-dd HH:mm")+" UTC");
            }
            if(_tickets.Length==0)AddLabel(24,182,0,"No pets in this view. Add a ticket or change the filter.");
            if(_page>0)FlatButton(24,509,110,1,"Previous");AddLabel(151,509,0,"Page "+(_page+1)+" / "+pages);
            if(_page+1<pages)FlatButton(275,509,100,2,"Next");
            FlatButton(420,509,184,6,"Spend pet credits");FlatButton(614,509,118,0,"Close");
        }
        public override void OnResponse(NetState sender,RelayInfo info)
        {
            var p=sender.Mobile;int id=info.ButtonID;if(id==0||!_book.CanUse(p))return;
            if(id==3){_book.BeginAdd(p);return;}if(id>=300&&id<300+_tickets.Length){var t=_tickets[id-300];if(!t.Deleted&&t.Parent==_book&&t.Owner==p){t.Favorite=!t.Favorite;t.InvalidateProperties();}p.SendGump(new PetBookGump(_book,_page,_sort,_filter,_search));return;}if(id==6){HavenPetExchange.Show(p);return;}
            if(id>=100){int index=(id-100)/3;if(index<0||index>=_tickets.Length)return;var t=_tickets[index];if(t.Deleted||t.Parent!=_book||t.Owner!=p)return;
                int action=(id-100)%3;if(action==0){t.ShowLore(p);return;}
                if(action==2){if(HavenPetExchange.Eligible(p,t,_book))p.SendGump(new HavenPetExchangeConfirm(t,_book));return;}
                if(!t.Claim(p))p.SendMessage("Cannot claim: check follower slots and clear space nearby. Your pet stays in the book.");
            }
            p.SendGump(new PetBookGump(_book,_page+(id==1?-1:id==2?1:0),id==4?(_sort+1)%5:_sort,id==5?(_filter+1)%5:_filter,id==7?(info.GetTextEntry(1)?.Text??""):id==8?"":_search));
        }
    }
}
