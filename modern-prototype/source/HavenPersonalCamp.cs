using System;
using System.Collections.Generic;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Regions;
using Server.Targeting;
using Server.Network;
using Server.Engines.Craft;

namespace Server.HavenPrototype
{
    // The storage itself is packed: no replacement container, item copying or ground spill.
    public class HavenPersonalCamp : Backpack
    {
        public Mobile Owner;
        private readonly List<Item> _pieces = new List<Item>();
        public bool Deployed { get { return Parent == null && Map != null && Map != Map.Internal; } }
        public HavenPersonalCamp(Mobile owner) { Owner=owner; Name="personal expedition camp"; Weight=10; LootType=LootType.Blessed; MaxItems=250; }
        public HavenPersonalCamp(Serial s):base(s) { }
        public override bool OnDroppedToWorld(Mobile p,Point3D point){p.SendMessage("Double-click the camp in your backpack to choose a campsite.");return false;}
        private bool Access(Mobile p) { return p != null && p == Owner && !Deleted && p.Alive && (IsChildOf(p.Backpack) || Deployed && p.Map == Map && p.InRange(this,2) && p.InLOS(this)); }
        public override bool IsAccessibleTo(Mobile p) { return Access(p) && base.IsAccessibleTo(p); }
        public override bool CheckItemUse(Mobile p,Item item) { return Access(p) && base.CheckItemUse(p,item); }
        public override bool CheckLift(Mobile p,Item item,ref LRReason reason) { if(!Access(p)){reason=LRReason.CannotLift;return false;}return base.CheckLift(p,item,ref reason); }
        public override bool OnDragDrop(Mobile p,Item item) { return Access(p) && base.OnDragDrop(p,item); }
        public override bool OnDragDropInto(Mobile p,Item item,Point3D point) { return Access(p) && base.OnDragDropInto(p,item,point); }
        public override void OnDoubleClick(Mobile p)
        {
            if(!Access(p)){p.SendMessage("This is your private camp: keep it in your backpack or stand beside it to use it.");return;}
            if(Deployed) base.OnDoubleClick(p);
            else { p.SendMessage("Choose clear ground in Haven or the wilderness, or clear floor inside your own house. Your stored items stay with the camp.");p.Target=new CampTarget(this); }
        }
        public bool Place(Mobile p,Point3D point)
        {
            if(!Access(p) || !IsChildOf(p.Backpack) || !HavenPreview.CanTravel(p) || !p.InRange(point,3) || !p.InLOS(point))return false;
            var map=p.Map;
            if(map==null || map==Map.Internal)return false;
            var house=BaseHouse.FindHouseAt(point,map,16);
            if(house!=null && !house.IsOwner(p))return false;
            // Entire footprint must be on one floor and inside one owned house, or outdoors.
            for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
            {
                var tile=new Point3D(point.X+dx,point.Y+dy,point.Z);
                var tileHouse=BaseHouse.FindHouseAt(tile,map,16);
                if(tileHouse!=house || !map.CanFit(tile,16,true,true))return false;
                var region=Region.Find(tile,map);
                if(region.IsPartOf<DungeonRegion>() || house==null && region.IsPartOf<GuardedRegion>() && !region.IsPartOf("Haven Island"))return false;
            }
            var nearby=map.GetMobilesInRange(point,12);
            try { foreach(Mobile m in nearby) { var creature=m as BaseCreature; if(creature!=null && creature.Alive && !creature.Controlled && !creature.IsInvulnerable && creature.FightMode!=FightMode.None)return false; } }
            finally { nearby.Free(); }
            MoveToWorld(point,map); Movable=false;
            AddPiece(new HavenCampPiece(this,0xDE3),-1,-1);
            AddPiece(new HavenCampForge(this),-1,1);
            AddPiece(new HavenCampAnvil(this),-1,0);
            AddPiece(new HavenCampPiece(this,0xA59),1,-1);
            p.SendMessage("Camp set. Open the pack for private storage; double-click the bedroll to pack everything together. The forge and anvil support crafting.");
            return true;
        }
        private void AddPiece(Item piece,int dx,int dy){_pieces.Add(piece);piece.MoveToWorld(new Point3D(X+dx,Y+dy,Z),Map);}
        public bool Pack(Mobile p)
        {
            if(IsLockedDown || IsSecure){p.SendMessage("Release the camp from house lockdown or security before packing it.");return false;}
            if(!Access(p) || !Deployed || !HavenPreview.CanTravel(p) || p.Backpack==null || !p.Backpack.CheckHold(p,this,false))return false;
            Movable=true;
            if(!p.Backpack.TryDropItem(p,this,false)){Movable=false;return false;}
            foreach(var piece in _pieces)if(piece!=null&&!piece.Deleted)piece.Delete();
            _pieces.Clear();p.SendMessage("Camp packed. Every stored item remains inside the same pack.");return true;
        }
        public override void OnAfterDelete(){foreach(var piece in _pieces)if(piece!=null&&!piece.Deleted)piece.Delete();_pieces.Clear();base.OnAfterDelete();}
        public static bool Claim(Mobile p)
        {
            if(!HavenMarks.CanUse(p) || HavenRefugeQuest.Stage(p)<1 || HavenBeaconQuest.NearbyJenna(p)==null || p.Backpack==null)return false;
            var account=(Account)p.Account;string key="Haven.PersonalCamp:"+p.Serial.Value;
            if(account.GetTag(key)!=null)return false;
            var camp=new HavenPersonalCamp(p);
            if(!p.Backpack.TryDropItem(p,camp,false)){camp.Delete();return false;}
            account.SetTag(key,"claimed");p.SendMessage("Jenna gives you a personal camp. Set it in Haven before you own a house, then pack it and move it home later.");return true;
        }
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Owner);w.Write(_pieces.Count);foreach(var piece in _pieces)w.Write(piece);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Owner=r.ReadMobile();int count=r.ReadInt();for(int i=0;i<count;i++){var piece=r.ReadItem();if(piece!=null)_pieces.Add(piece);}MaxItems=250;}
        private sealed class CampTarget:Target
        {
            private readonly HavenPersonalCamp _camp;
            public CampTarget(HavenPersonalCamp camp):base(3,true,TargetFlags.None){_camp=camp;}
            protected override void OnTarget(Mobile p,object target){var point=target as IPoint3D;if(point==null || !_camp.Place(p,new Point3D(point)))p.SendMessage("Camp needs a clear 3 by 3 area, away from wild creatures, dungeons and guarded streets outside Haven, or inside your own house. Leave combat first.");}
        }
    }
    public class HavenCampPiece:Item
    {
        public HavenPersonalCamp Camp;
        public HavenCampPiece(HavenPersonalCamp camp,int art):base(art){Camp=camp;Movable=false;Name=art==0xA59?"camp bedroll - pack up":"expedition campfire";}
        public HavenCampPiece(Serial s):base(s){}
        public override void OnDoubleClick(Mobile p){if(ItemID==0xA59 && (Camp==null || !Camp.Pack(p)))p.SendMessage("Stand beside your camp, leave combat, and make room for the pack and its contents.");}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Camp);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Camp=r.ReadItem() as HavenPersonalCamp;}
    }
    [Forge] public class HavenCampForge:HavenCampPiece
    {
        public HavenCampForge(HavenPersonalCamp camp):base(camp,0xFB1){Name="expedition forge";}
        public HavenCampForge(Serial s):base(s){}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    }
    [Anvil] public class HavenCampAnvil:HavenCampPiece
    {
        public HavenCampAnvil(HavenPersonalCamp camp):base(camp,0xFAF){Name="expedition anvil";}
        public HavenCampAnvil(Serial s):base(s){}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    }
}
