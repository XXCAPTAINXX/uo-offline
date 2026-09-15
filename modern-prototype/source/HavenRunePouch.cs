using System;
using Server.Gumps;
using System.Linq;
using Server.Commands;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Spells;

namespace Server.HavenPrototype
{
    public class HavenRunePouch : Item
    {
        public int Runes {get;private set;}
        [Constructable] public HavenRunePouch():base(0xE79){Name="Wayfarer's rune pouch";Hue=0x482;Weight=1;LootType=LootType.Blessed;}
        public HavenRunePouch(Serial serial):base(serial){}
        public bool CanUse(Mobile from){return !Deleted&&from!=null&&!from.Deleted&&from.Alive&&from.Backpack!=null&&IsChildOf(from.Backpack)&&HavenResources.Accessible(from,this);}
        public bool Store(Mobile from,Item item){var rune=item as RecallRune;if(!CanUse(from)||Runes>=60000||rune==null||rune.Deleted||rune.GetType()!=typeof(RecallRune)||rune.Marked||rune.House!=null||rune.TargetMap!=null||rune.Name!=null||rune.Hue!=0||rune.Amount!=1||rune.LootType!=LootType.Regular||!string.IsNullOrEmpty(rune.Description)||rune.Target!=Point3D.Zero||rune.Items.Count!=0||(!rune.IsChildOf(from.Backpack)&&from.Holding!=rune)||!HavenResources.Accessible(from,rune))return false;Runes++;rune.Delete();InvalidateProperties();return true;}
        public int StorePack(Mobile from){if(!CanUse(from))return 0;return from.Backpack.FindItemsByType(typeof(RecallRune),true).ToArray().Count(item=>Store(from,item));}
        public int Withdraw(Mobile from,int amount){if(!CanUse(from)||amount<1||amount>100||Runes<amount)return 0;int count=0;for(int i=0;i<amount;i++){var rune=new RecallRune();if(!from.Backpack.TryDropItem(from,rune,false)){rune.Delete();break;}Runes--;count++;}InvalidateProperties();return count;}
        public bool MarkRune(Mobile from,Func<bool> finishCast){
            if(!CanUse(from)||Runes<=0||!SpellHelper.CheckTravel(from,TravelCheckType.Mark)||SpellHelper.CheckMulti(from.Location,from.Map,!Core.AOS))return false;
            var rune=new RecallRune();bool delivered=false;
            try{if(!from.Backpack.CheckHold(from,rune,false)||!finishCast()||!CanUse(from)||Runes<=0)return false;rune.Mark(from);if(!from.Backpack.TryDropItem(from,rune,false))return false;Runes--;delivered=true;InvalidateProperties();from.SendMessage("The pouch places your newly marked rune in your backpack.");return true;}finally{if(!delivered)rune.Delete();}
        }
        public override bool OnDragDrop(Mobile from,Item item){return Store(from,item);}
        public override void OnDoubleClick(Mobile from){if(CanUse(from)){from.CloseGump(typeof(RunePouchGump));from.SendGump(new RunePouchGump(this));}else from.SendMessage("Keep the rune pouch in your backpack.");}
        public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Blank recall runes: "+Runes.ToString("N0")+" / 60,000");list.Add("Cast Mark on this pouch to receive a marked rune.");}
        public static void Initialize(){CommandSystem.Register("rune",AccessLevel.Player,e=>{var pouch=e.Mobile.Backpack==null?null:e.Mobile.Backpack.FindItemByType(typeof(HavenRunePouch),true) as HavenRunePouch;e.Mobile.SendMessage(pouch!=null&&pouch.Withdraw(e.Mobile,1)==1?"A blank rune is in your backpack.":"Keep a rune pouch with blank runes in your pack; leave room for a rune.");});}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Runes);}public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Runes=r.ReadInt();}
    }
    public class RunePouchGump:HavenMenuGump{
        readonly HavenRunePouch _pouch;
        public RunePouchGump(HavenRunePouch pouch):base(50,50){_pouch=pouch;AddBackground(0,0,480,270,3000);AddLabel(24,22,0,"Wayfarer's rune pouch");AddLabel(24,62,0,"Blank runes: "+pouch.Runes.ToString("N0")+" / 60,000");FlatButton(24,104,200,1,"Take 1");FlatButton(244,104,210,2,"Take 10");FlatButton(24,144,430,3,"Store blank runes from my pack");AddHtml(24,180,430,45,"Cast Mark on the pouch to receive a marked rune. Marked and personalized runes stay in your backpack.",false,false);AddLabel(24,232,0,"[rune takes one blank rune");FlatButton(354,232,100,0,"Close");}
        public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(info.ButtonID==0||!_pouch.CanUse(p))return;if(info.ButtonID==3)p.SendMessage("Stored "+_pouch.StorePack(p)+" blank runes.");else if(info.ButtonID==1||info.ButtonID==2)p.SendMessage("Withdrew "+_pouch.Withdraw(p,info.ButtonID==1?1:10)+" runes.");_pouch.OnDoubleClick(p);}
    }
}
