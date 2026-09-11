using System;
using System.Collections;
using System.Linq;
using Server.Accounting;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.HavenPrototype
{
    public static class HavenStarterHome
    {
        public static void Initialize() {
            CommandSystem.Register("home",AccessLevel.Player,e=> { if(HavenPreview.Enabled) { if(Find(e.Mobile)!=null) Travel(e.Mobile); else e.Mobile.SendGump(new HavenHomeClaimGump()); } });
            EventSink.ServerStarted += () => { if(HavenPreview.Enabled) foreach(var house in World.Items.Values.OfType<HavenPirateLodge>().ToArray()) house.RepairLegacyDoors(); };
        }
        public static HavenPirateLodge Find(Mobile from) { return World.Items.Values.OfType<HavenPirateLodge>().FirstOrDefault(h=>!h.Deleted && h.Owner!=null && h.IsOwner(from)); }
        public static bool FindSite(out Point3D site,out Map siteMap)
        {
            var probe=new PlayerMobile { Player=true }; probe.Map=Map.Trammel;
            try
            {
                var anchors=new[]{new Point2D(1100,2500),new Point2D(1700,2100),new Point2D(2500,700),new Point2D(600,1600),new Point2D(1400,1800)};
                foreach(var map in new[]{Map.Trammel,Map.Malas})
                {
                probe.Map=map;
                if(map==Map.Malas) anchors=new[]{new Point2D(1000,800),new Point2D(1400,800),new Point2D(1800,900)};
                foreach(var anchor in anchors)
                for(int dx=-128;dx<=128;dx+=16) for(int dy=-128;dy<=128;dy+=16)
                {
                    int x=anchor.X+dx,y=anchor.Y+dy; var point=new Point3D(x,y,map.GetAverageZ(x,y)); ArrayList move;
                    if(HousePlacement.Check(probe,0x147B,point,out move)==HousePlacementResult.Valid && move.Count==0) {site=point;siteMap=map;return true;}
                }
                }
            }
            finally {probe.Delete();}
            site=Point3D.Zero;siteMap=null;return false;
        }
        public static HavenPirateLodge Claim(Mobile from)
        {
            if(!HavenPreview.CanTravel(from) || !(from.Account is Account)) return null;
            var existing=Find(from); if(existing!=null) return existing;
            Point3D site;Map siteMap; if(!FindSite(out site,out siteMap)) {from.SendMessage("No safe empty house plot found; nothing was changed.");return null;}
            var house=new HavenPirateLodge(from); house.MoveToWorld(site,siteMap); house.Furnish();
            from.SendMessage("Your account's pirate lodge is ready. Use [home to return. Storage chests are secured to the owner account.");
            return house;
        }
        public static bool Travel(Mobile from)
        {
            var house=Find(from); if(house==null || !HavenPreview.CanTravel(from)) {from.SendMessage("Home travel requires an owned lodge, life and no active combat or criminal flag.");return false;}
            var landing=new Point3D(house.X-1,house.Y+4,house.Z+7);
            if(!house.Map.CanFit(landing,16,false,true)) {from.SendMessage("The home arrival is blocked; clear its porch.");return false;}
            BaseCreature.TeleportPets(from,landing,house.Map);from.MoveToWorld(landing,house.Map);return true;
        }
    }
    public class HavenHomeClaimGump:Gump
    {
        public HavenHomeClaimGump():base(60,60)
        {
            AddBackground(0,0,540,300,0xA28);AddLabel(24,20,0,"Claim a pirate starter home");
            AddHtml(24,60,490,155,"<BASEFONT COLOR=#202020>A free 18 x 18 custom lodge on a safe empty Trammel or Malas plot, with secure storage, workshop, upstairs quarters and a spiral staircase. Characters on your same account share ownership.<BR><BR>This is a new home, not your old island or an import of its possessions. Claiming does not change your skills.</BASEFONT>",false,false);
            AddButton(24,245,0xFA5,0xFA7,1,GumpButtonType.Reply,0);AddLabel(60,245,0,"Claim home");AddButton(400,245,0xFA5,0xFA7,0,GumpButtonType.Reply,0);AddLabel(436,245,0,"Close");
        }
        public override void OnResponse(NetState sender,RelayInfo info) {if(info.ButtonID==1 && HavenStarterHome.Claim(sender.Mobile)!=null) HavenStarterHome.Travel(sender.Mobile);}
    }
    public class HavenPirateLodge:HouseFoundation
    {
        public HavenPirateLodge(Mobile owner):base(owner,0x147B,5000,5000) {Name="R.E.C. pirate lodge";Type=FoundationType.DarkWood;Public=false;RestrictDecay=true;Build();}
        public HavenPirateLodge(Serial serial):base(serial) {}
        public override int GetAosMaxSecures() {return 5000;}
        public override int GetAosMaxLockdowns() {return 5000;}
        private void Build()
        {
            var list=GetEmptyFoundation();
            foreach(var tile in list.List.ToArray()) if(tile.m_OffsetZ==7) list.Remove(tile.m_ItemID,tile.m_OffsetX,tile.m_OffsetY,tile.m_OffsetZ);
            Action<int,int,int,int> tileAt=(id,x,y,z)=>list.Add(id,x,y,z);
            Action<int,int,int,int,int> floor=(x1,y1,x2,y2,z)=>{for(int x=x1;x<=x2;x++)for(int y=y1;y<=y2;y++)tileAt(0x4A9+(x+y+40)%4,x,y,z);};
            floor(-7,-7,9,9,7);floor(-6,-6,2,2,27);
            // Staircase clears its two-by-two vertical opening.
            foreach(var tile in list.List.ToArray()) if(tile.m_OffsetZ==27 && tile.m_OffsetX>=0 && tile.m_OffsetX<=1 && tile.m_OffsetY>=-5 && tile.m_OffsetY<=-4) list.Remove(tile.m_ItemID,tile.m_OffsetX,tile.m_OffsetY,tile.m_OffsetZ);
            Action<int,int,int,int,int> room=(x1,y1,x2,y2,z)=>{
                for(int x=x1+1;x<=x2;x++){tileAt(x%3==0?0xE:0x7,x,y1,z); if(x!=-1)tileAt(x%3==0?0xE:0x7,x,y2,z);}
                for(int y=y1+1;y<=y2;y++){tileAt(y%3==0?0xF:0x8,x1,y,z);tileAt(y%3==0?0xF:0x8,x2,y,z);}
                tileAt(0x9,x1,y1,z);
            };
            room(-7,-7,2,2,7);room(-7,-7,2,2,27);
            // Workshop entrance opens onto the south working deck.
            for(int x=4;x<=8;x++){tileAt(x==6?0xE:0x7,x,-7,7);if(x!=5)tileAt(0x7,x,1,7);}
            for(int y=-6;y<=1;y++){tileAt(0x8,3,y,7);tileAt(y==-3?0xF:0x8,8,y,7);}tileAt(0x9,3,-7,7);
            Action<int,int,int,int,int> roof=(x1,y1,x2,y2,z)=>{
                int mid=(x1+x2)/2;
                for(int x=x1;x<=x2;x++)for(int y=y1;y<=y2;y++)tileAt(x<mid?0x5C4:x>mid?0x5C3:0x5C2,x,y,z+3*Math.Min(x-x1,x2-x));
            };
            roof(-7,-7,3,3,47);roof(3,-7,9,2,27);
            // Covered porch and gallery supported by timber columns.
            floor(-6,3,2,5,27);for(int x=-6;x<=2;x++)tileAt(0x12,x,5,27);
            foreach(int x in new[]{-6,2}) {tileAt(0x9,x,5,7);for(int y=3;y<=5;y++)tileAt(0x11,x,y,27);}
            for(int y=-6;y<=9;y++)tileAt(0x11,9,y,7);
            for(int x=-7;x<=9;x++)if(x<-2 || x>1)tileAt(0x12,x,9,7);
            CurrentState=new DesignState(this,list);CurrentState.Revision=++LastRevision;DesignState=new DesignState(CurrentState);BackupState=new DesignState(CurrentState);
        }
        private void Place(Item item,int x,int y,int z) {item.Movable=false;item.MoveToWorld(new Point3D(X+x,Y+y,Z+z),Map);Fixtures.Add(item);var addon=item as BaseAddon;if(addon!=null)Addons.Add(addon,Owner);}
        private void Decor(int id,string name,int x,int y,int z) {Place(new Static(id){Name=name},x,y,z);}
        private void Chest(string name,int x,int y)
        {
            var chest=new HavenHomeChest {Name=name,MaxItems=1000};Place(chest,x,y,7);chest.Movable=false;chest.IsSecure=true;Secures.Add(new SecureInfo(chest,SecureLevel.Owner,Owner));
        }
        public void Furnish()
        {
            if(Fixtures.Count>0)return;
            Sign.Name="R.E.C. - pirate lodge";
            AddDoor(new DarkWoodHouseDoor(DoorFacing.WestCW){Level=SecureLevel.Owner},-1,2,7);AddDoor(new DarkWoodHouseDoor(DoorFacing.WestCW){Level=SecureLevel.Owner},-1,2,27);AddDoor(new DarkWoodHouseDoor(DoorFacing.WestCW){Level=SecureLevel.Owner},5,1,7);
            Chest("Receiving chest - owner account",-5,0);Chest("Resource stores",-5,-2);Chest("Armory and treasures",-5,-4);Chest("Crafting supplies",5,-1);
            var supply=(Container)Fixtures.First(i=>i.Name=="Crafting supplies");
            foreach(var tool in new Item[]{new SmithHammer(),new SewingKit(),new Scissors(),new DovetailSaw(),new TinkerTools()})supply.DropItem(tool);
            Decor(0xB90,"Receiving desk",-2,-2,7);Decor(0xFF1,"Cargo manifest",-2,-2,13);Decor(0xB57,"Clerk's chair",-2,-1,7);
            Decor(0xE3F,"Export crates",-3,-6,7);Decor(0xE3F,"Export crates",-3,-6,10);Decor(0xE77,"Sealed provisions",-2,-6,7);
            Place(new SpiralStaircaseAddon(true){Level=SecureLevel.Owner},0,-5,7);
            Place(new SmallForgeAddon(),6,-6,7);Place(new AnvilSouthAddon(),5,-5,7);Place(new LoomSouthAddon(),6,-2,7);
            Place(new SpinningwheelSouthAddon(),4,-4,7);
            Decor(0xB90,"Shipwright's bench",7,-4,7);Decor(0xFAF,"Workshop tools",7,-4,13);
            Place(new SmallBedSouthAddon(),-5,-5,27);Decor(0xA4D,"Captain's wardrobe",-3,-5,27);
            Place(new BluePlainRugAddon(),-3,-2,27);
            Decor(0xB90,"Chart desk",-5,-1,27);Decor(0x14EB,"Voyage chart",-5,-1,33);Decor(0xB57,"Captain's chair",-5,0,27);
            Decor(0xB90,"Navigation table",-4,-1,27);Decor(0x14F3,"Model of the company ship",-4,-1,33);
            Decor(0xB90,"Bedside table",-6,-5,27);Place(new Lantern(),-6,-5,33);
            Decor(0xA9A,"Shipping records",-6,-3,27);Decor(0xB2D,"Porch bench",-4,4,7);
            Decor(0xB90,"Gallery table",-4,4,27);Decor(0xB56,"Gallery chair",-5,4,27);Decor(0xB58,"Gallery chair",-3,4,27);Place(new Lantern(),-4,4,33);
            Decor(0xE77,"Fresh water barrel",7,4,7);Decor(0x14F8,"Coiled rigging rope",7,3,7);Decor(0x1EA0,"Dockside net",8,4,7);
            Decor(0x14F7,"Recovered ship's anchor",7,6,7);Decor(0xE3F,"Rigging stores",8,6,7);
            Decor(0xE3F,"Rigging stores",8,6,10);Decor(0xE3F,"Ready export cargo",8,7,7);Decor(0xE3F,"Ready export cargo",7,7,7);
            Decor(0x11CA,"Porch flowers",-6,6,7);Decor(0x11CA,"Porch flowers",2,6,7);
        }
        public void RepairLegacyDoors()
        {
            foreach(var old in Doors.OfType<DarkWoodDoor>().ToArray())
            {
                old.Open=false;
                var replacement=new DarkWoodHouseDoor(DoorFacing.WestCW){Level=SecureLevel.Owner,Hue=old.Hue};
                AddDoor(replacement,old.X-X,old.Y-Y,old.Z-Z);
                Doors.Remove(old); old.Delete();
            }
        }
        public override void Serialize(GenericWriter writer){base.Serialize(writer);writer.Write(0);}
        public override void Deserialize(GenericReader reader){base.Deserialize(reader);reader.ReadInt();}
    }
    public class HavenHomeChest:WoodenChest
    {
        public override int DefaultMaxWeight {get{return 0;}}
        public HavenHomeChest() {}
        public HavenHomeChest(Serial serial):base(serial) {}
        public override void Serialize(GenericWriter writer){base.Serialize(writer);writer.Write(0);}
        public override void Deserialize(GenericReader reader){base.Deserialize(reader);reader.ReadInt();}
    }
}
