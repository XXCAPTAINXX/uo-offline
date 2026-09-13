using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Multis;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    // Native port of the existing R.E.C. courtyard compound, not a replacement design.
    public partial class HavenRecoveredHeadquarters : HouseFoundation, IHavenEstateStores
    {
        public readonly List<Item> CompanyFixtures = new List<Item>();
        public Container Vault;
        private string _ownerAccount;
        public readonly List<HavenEstateStorage> Stations = new List<HavenEstateStorage>();
        public Container StorageVault { get { return Vault; } }
        public HavenRecoveredHeadquarters(Mobile owner) : base(owner, 0x18A8, 5000, 10000)
        {
            Name = "R.E.C. - Rare Export Company";
            Type = FoundationType.DarkWood;
            Public = false;
            RestrictDecay = true;
            _ownerAccount = (owner.Account as Server.Accounting.Account)?.Username;
            BuildPirateLayout();
        }
        public HavenRecoveredHeadquarters(Serial serial) : base(serial) { }
        public override int GetAosMaxSecures() { return 10000; }
        public override int GetAosMaxLockdowns() { return 5000; }
        public bool CanAccessStores(Mobile p,HavenEstateStorage station)
        {
            return !Deleted && p!=null && !p.Deleted && p.Alive && Owner!=null && !Owner.Deleted &&
                p.Account!=null && p.Account==Owner.Account && station!=null && !station.Deleted &&
                Stations.Contains(station) && station.Estate==this && p.Map==Map && station.Map==Map &&
                p.InRange(station,3) && Math.Abs(p.Z-station.Z)<=8 && p.InLOS(station) && Vault!=null && !Vault.Deleted;
        }
        public bool Deposit(Mobile p,HavenEstateStorage station,Item item)
        {
            return CanAccessStores(p,station) && item!=null && !item.Deleted && item.Movable && p.Backpack!=null &&
                HavenIslandEstate.BackpackOrigin(p,item) && Vault.TryDropItem(p,item,false);
        }
        public bool Withdraw(Mobile p,HavenEstateStorage station,Item item)
        {
            return CanAccessStores(p,station) && item!=null && !item.Deleted && item.Parent==Vault &&
                p.Backpack!=null && p.Backpack.TryDropItem(p,item,false);
        }
        internal void InstallStores(Container existing)
        {
            if(Vault!=null || Stations.Count!=0)throw new InvalidOperationException("Stores already installed");
            Vault=existing ?? new HavenHomeChest {Name="Island private stores",MaxItems=3000};
            Vault.Internalize();
            var sites=new[]{new Point3D(1,6,7),new Point3D(12,-10,7),new Point3D(-12,-11,27)};
            var names=new[]{"Receiving chest","Workshop stores","Armory stores"};
            for(int i=0;i<sites.Length;i++){
                var station=new HavenEstateStorage(this){Name=names[i]};Stations.Add(station);
                Place(station,sites[i].X,sites[i].Y,sites[i].Z);
            }
        }

        internal void Place(Item item, int x, int y, int z = 7)
        {
            var point = new Point3D(X + x, Y + y, Z + z);
            if (!IsInside(point, 16)) { item.Delete(); throw new InvalidOperationException("Recovered furnishing outside house: " + point); }
            item.Movable = false;
            item.MoveToWorld(point, Map);
            CompanyFixtures.Add(item);
            var addon = item as BaseAddon;
            if (addon != null) Addons.Add(addon, Owner);
            else if (item is Container)
            {
                item.IsSecure = true;
                Secures.Add(new SecureInfo((Container)item, SecureLevel.Owner, Owner));
            }
            else { item.IsLockedDown = true; LockDowns.Add(item, Owner); }
        }
        internal void Decorate(int id, string name, int x, int y, int z = 7)
        {
            Item item = id == 0xE3F || id == 0xA4D ? (Item)new WoodenChest() : new Static(id);
            item.ItemID = id; item.Name = name;
            Place(item, x, y, z);
        }
        internal void FurnishRecoveredRooms()
        {
            if (CompanyFixtures.Count != 0) throw new InvalidOperationException("Compound already furnished");
            Sign.Name = "R.E.C. - Rare Export Company | Custom courtyard compound";
            Place(new SmallForgeAddon(), 11, -12);
            Place(new AnvilSouthAddon(), 10, -9);
            Place(new LoomSouthAddon(), 11, -7);
            Place(new SpinningwheelSouthAddon(), 13, -3);
            Place(new StoneOvenSouthAddon(), -12, -13);
            Place(new FlourMillSouthAddon(), -12, -7);
            Place(new WaterTroughEastAddon(), -12, -4);
            Place(new WaterTroughEastAddon(), -12, 12);
            Place(new LargeBedSouthAddon(), -12, -7, 47);
            Place(new SmallBedSouthAddon(), -12, 7, 27);
            Place(new SmallBedSouthAddon(), -6, 7, 27);
            Decorate(0xB90, "Cartographer's desk", 6, -5, 27);
            Decorate(0x1047, "The company export ledger", 6, -5, 33);
            Decorate(0xB90, "The captain's desk", -6, -7, 47);
            Decorate(0xB2D, "The captain's chair", -6, -6, 47);
            Decorate(0xA4D, "Captain's sea chest", -5, -11, 47);
            Decorate(0xB90, "Company council table", -8, -7, 27);
            Decorate(0xB2D, "Quartermaster's chair", -9, -8, 27);
            Decorate(0xB2D, "Company officer's chair", -6, -8, 27);
            Decorate(0x14EB, "R.E.C. trade routes", -8, -7, 33);
            FurnishRecoveredCourtyard();
            for (int i = 0; i < LadderSites.Length; i++)
                Place(new HavenRecoveredLadder(this, i), LadderSites[i].X, LadderSites[i].Y, LadderSites[i].Z);
        }
        internal static readonly Point3D[] LadderSites = {
            new Point3D(-2,1,7), new Point3D(-9,-4,27), new Point3D(-9,-4,47), new Point3D(5,1,7),
            new Point3D(5,1,27), new Point3D(-5,11,7), new Point3D(-5,11,27), new Point3D(-7,-4,27)
        };
        internal static readonly Point3D[] LadderLandings = {
            new Point3D(-9,-3,27), new Point3D(-2,2,7), new Point3D(-7,-3,27), new Point3D(5,2,27),
            new Point3D(5,2,7), new Point3D(-5,12,27), new Point3D(-5,12,7), new Point3D(-9,-3,47)
        };
        internal static readonly string[] LadderNames = {
            "Up to guild hall", "Down to courtyard", "Down to guild hall", "Up to workshop loft",
            "Down to workshop", "Up to crew loft", "Down to barn", "Up to captain's quarters"
        };
        internal void CheckFloorAccess()
        {
            foreach (var p in LadderLandings)
                if (!IsInside(new Point3D(X+p.X,Y+p.Y,Z+p.Z),16) || !Map.CanFit(X+p.X,Y+p.Y,Z+p.Z,16,false,false))
                    throw new InvalidOperationException("Recovered ladder landing blocked: " + p);
        }
        internal void CheckWalkingRoutes()
        {
            var probe=new Mobile {Body=0x190};
            var visited=new HashSet<Point3D>();var pending=new Queue<Point3D>();
            var dx=new[]{0,1,1,1,0,-1,-1,-1};var dy=new[]{-1,-1,0,1,1,1,0,-1};
            var start=new Point3D(X,Y+18,Z);pending.Enqueue(start);visited.Add(start);
            try {
                probe.MoveToWorld(start,Map);
                while(pending.Count>0) {
                    var at=pending.Dequeue();
                    for(int d=0;d<8;d++) {
                        int x=at.X+dx[d],y=at.Y+dy[d],nextZ;
                        if(x<X-17||x>X+17||y<Y-17||y>Y+18)continue;
                        if(!Server.Movement.Movement.CheckMovement(probe,Map,at,(Direction)d,out nextZ))continue;
                        var next=new Point3D(x,y,nextZ);if(visited.Add(next))pending.Enqueue(next);
                    }
                    for(int i=0;i<LadderSites.Length;i++) {
                        var site=LadderSites[i];
                        if(Math.Abs(at.X-X-site.X)>2||Math.Abs(at.Y-Y-site.Y)>2||Math.Abs(at.Z-Z-site.Z)>8)continue;
                        var ladder=CompanyFixtures.OfType<HavenRecoveredLadder>().First(l=>l.X==X+site.X&&l.Y==Y+site.Y&&l.Z==Z+site.Z);
                        if(!Map.LineOfSight(new Point3D(at.X,at.Y,at.Z+14),Map.GetPoint(ladder,false)))continue;
                        var landing=LadderLandings[i];var next=new Point3D(X+landing.X,Y+landing.Y,Z+landing.Z);
                        if(visited.Add(next))pending.Enqueue(next);
                    }
                }
                foreach(var item in CompanyFixtures.Where(i=>i is HavenRecoveredLadder || i is HavenEstateStorage || i is Container || i is BaseAddon))
                    if(!visited.Any(p=>Math.Abs(p.X-item.X)<=2&&Math.Abs(p.Y-item.Y)<=2&&Math.Abs(p.Z-item.Z)<=8&&Map.LineOfSight(new Point3D(p.X,p.Y,p.Z+14),Map.GetPoint(item,false))))
                        throw new InvalidOperationException("No walking route to "+item.Name+" ("+item.GetType().Name+") at "+item.Location+"; reachable="+visited.Count);
            }finally{probe.Delete();}
        }
        internal void DiscardUnpublishedFixtures()
        {
            // Only newly constructed, unpublished fixtures may use this rollback path.
            if(CompanyFixtures.OfType<Container>().Any(c=>c.Items.Count!=0))throw new InvalidOperationException("Rollback fixture unexpectedly contains items");
            foreach(var item in CompanyFixtures.ToArray()) {
                var addon=item as BaseAddon;if(addon!=null)Addons.Remove(addon);
                LockDowns.Remove(item);Secures.RemoveAll(s=>s.Item==item);
                item.Delete();
            }
            CompanyFixtures.Clear();Stations.Clear();
        }
        public override void OnDelete()
        {
            if (Vault != null && !Vault.Deleted) {
                if(Vault.Items.Count==0)Vault.Delete();
                else if(Owner!=null && !Owner.Deleted)Owner.BankBox.DropItem(Vault);
                else {var recovery=new HavenEstateRecovery(_ownerAccount);recovery.DropItem(Vault);recovery.Internalize();}
            }
            Vault = null;
            // Containers and addons remain subject to native house recovery; never delete filled fixtures here.
            foreach (var item in CompanyFixtures.Where(i => !(i is Container) && !(i is BaseAddon)).ToArray())
                if (!item.Deleted) item.Delete();
            CompanyFixtures.Clear();
            base.OnDelete();
        }
        public override void OnTransfer(){base.OnTransfer();_ownerAccount=(Owner?.Account as Server.Accounting.Account)?.Username??_ownerAccount;}
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(_ownerAccount); w.Write(Vault); w.Write(CompanyFixtures.Count); foreach(var item in CompanyFixtures) w.Write(item); w.Write(Stations.Count);foreach(var station in Stations)w.Write(station); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); _ownerAccount=r.ReadString(); Vault=r.ReadItem() as Container; int count=r.ReadInt(); for(int i=0;i<count;i++){var item=r.ReadItem();if(item!=null)CompanyFixtures.Add(item);}count=r.ReadInt();for(int i=0;i<count;i++){var station=r.ReadItem() as HavenEstateStorage;if(station!=null)Stations.Add(station);} }
    }
    public class HavenRecoveredLadder : Item
    {
        HavenRecoveredHeadquarters _house; int _index;
        public HavenRecoveredLadder(HavenRecoveredHeadquarters house,int index):base(0x89D){_house=house;_index=index;Name=HavenRecoveredHeadquarters.LadderNames[index];Movable=false;}
        public HavenRecoveredLadder(Serial serial):base(serial){}
        public override void OnDoubleClick(Mobile from)
        {
            if(_house==null||_house.Deleted||!from.Alive||from.Map!=Map||!from.InRange(this,2)||Math.Abs(from.Z-Z)>8||!from.InLOS(this)||!_house.IsFriend(from))return;
            var p=HavenRecoveredHeadquarters.LadderLandings[_index];var to=new Point3D(_house.X+p.X,_house.Y+p.Y,_house.Z+p.Z);
            if(!_house.Map.CanFit(to,16,false,true))return;
            BaseCreature.TeleportPets(from,to,Map);from.MoveToWorld(to,Map);
        }
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(_house);w.Write(_index);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();_house=r.ReadItem() as HavenRecoveredHeadquarters;_index=r.ReadInt();}
    }
}
