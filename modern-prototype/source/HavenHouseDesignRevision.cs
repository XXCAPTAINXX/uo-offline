using System;
using System.IO;
using System.Linq;
using Server.Items;
using Server.Multis;

namespace Server.HavenPrototype
{
    public partial class HavenRecoveredHeadquarters
    {
        // Explicit operator migration with rollback of new fixtures and moved originals.
        internal void RefineRoomsForReview()
        {
            if (!File.Exists("HAVEN-DESIGN-REVIEW")) throw new InvalidOperationException("Design review marker required");
            if (CompanyFixtures.Any(i => i.Name == "Island design revision 2")) return;
            var originals=CompanyFixtures.ToArray();
            var positions=originals.ToDictionary(i=>i,i=>i.Location);
            var arts=originals.ToDictionary(i=>i,i=>i.ItemID);
            var visibility=originals.ToDictionary(i=>i,i=>i.Visible);
            var originalTiles=new MultiComponentList(Components);
            try {
            void Chest(string name, int x, int y, int z = 7) { Place(new WoodenChest { Name = name }, x, y, z); }
            void Chair(string name, int x, int y, int z = 7, int id = 0xB57) { Decorate(id, name, x, y, z); }
            void Move(string name, int x, int y, int z, int art = 0)
            {
                var item = CompanyFixtures.Single(i => i.Name == name);
                item.MoveToWorld(new Point3D(X+x,Y+y,Z+z),Map); if (art != 0) item.ItemID = art;
            }
            // Native joined table addons make coherent work and dining surfaces.
            Place(new LongWoodenTableSouthAddon { Name = "Galley preparation counter" },-8,-12);
            Chest("Galley provisions",-5,-10); Chest("Galley crockery",-5,-9);
            Decorate(0x9D7,"Bread for the crew",-9,-12,13);
            Decorate(0x9ED,"Fresh kitchen supplies",-7,-12,13);
            Place(new LongWoodenTableEastAddon { Name = "Galley dining table" },-9,-7);
            foreach(int y in new[]{-8,-6}) { Chair("Galley chair",-10,y); Chair("Galley chair",-7,y,7,0xB59); }
            Decorate(0xA9A,"Galley recipes",-10,-13);
            // Keep the existing desk as an archive desk; use one joined council table.
            Move("Company council table",-5,-11,27);
            Place(new LongWoodenTableEastAddon { Name = "Company council dining table" },-10,-8,27);
            Move("Quartermaster's chair",-11,-9,27,0xB57);
            Move("Company officer's chair",-8,-9,27,0xB59);
            Move("R.E.C. trade routes",-10,-8,33);
            foreach(int y in new[]{-8,-7}) { Chair("Council chair",-11,y,27); Chair("Council chair",-8,y,27,0xB59); }
            foreach(int x in new[]{-12,-10,-8}) Decorate(0xA9A,"Company archives",x,-13,27);
            Chest("Council chart chest",-5,-12,27);
            Decorate(0x14EB,"Charts under discussion",-9,-7,33);
            Decorate(0x1047,"Council minutes",-5,-11,33);
            // Workshop bays: material storage belongs beside each craft.
            Chest("Forge stock",13,-10); Chest("Sail cloth and thread",13,-6);
            Decorate(0xB7D,"Sailmaker's cutting table",7,-5);
            Decorate(0x1766,"Cloth on the cutting table",7,-5,13);
            Decorate(0xB7D,"Shipwright's workbench",7,-1);
            Chest("Shipwright tools",9,-1);
            Decorate(0x14F8,"Rigging by the cargo door",12,2);
            Chair("Cartographer's chair",6,-3,27,0xB56);
            Place(new AlchemistTableSouthAddon(),12,-10,27);
            Chest("Survey instruments",8,-10,27);
            Decorate(0xA9A,"Navigation reference shelves",10,-12,27);
            // Crew bunks, kit and shared mess; keep the ladder side open.
            Chest("Port crew sea chest",-12,10,27); Chest("Starboard crew sea chest",-7,10,27);
            Decorate(0xB90,"Crew loft mess table",-10,12,27);
            Chair("Crew mess chair",-12,12,27); Chair("Crew mess chair",-8,12,27,0xB59);
            Chest("Stable tack",-12,11); Chest("Stable feed",-9,11);
            Move("The captain's chair",-6,-6,47,0xB4F);
            Decorate(0xA9A,"Captain's library",-12,-12,47);
            Decorate(0xA9A,"Captain's library",-10,-12,47);
            // A single planted edge leaves the arrival and receiving routes legible.
            var shrubs = CompanyFixtures.Where(i=>i.Name=="Salt-tolerant island planting").ToArray();
            var garden = new[]{new Point3D(12,6,7),new Point3D(14,6,7),new Point3D(14,8,7),new Point3D(14,10,7),new Point3D(12,10,7)};
            for(int i=0;i<Math.Min(shrubs.Length,garden.Length);i++) shrubs[i].MoveToWorld(new Point3D(X+garden[i].X,Y+garden[i].Y,Z+7),Map);
            Move("A wind-bent island palm",13,8,7);
            var flowers=CompanyFixtures.Where(i=>i.Name=="Flowers beside the crew path").ToArray();
            for(int i=0;i<flowers.Length;i++) flowers[i].MoveToWorld(new Point3D(X+12,Y+7+i*2,Z+7),Map);
            var seats=CompanyFixtures.Where(i=>i.Name=="A chair in the sea breeze").ToArray();
            for(int i=0;i<seats.Length;i++) {seats[i].ItemID=i==0?0xB57:0xB59;seats[i].MoveToWorld(new Point3D(X+6+i*2,Y+8,Z+7),Map);}
            Move("A barrel of grog",6,11,7); Move("Spare rigging for the next voyage",6,12,7);
            Move("Anchor salvaged from the Blackwake",5,13,7);
            Place(new BluePlainRugAddon { Name="Crew common room rug" },-10,12,27);
            Place(new Lantern { Name="Council reading lamp",Duration=TimeSpan.Zero,Burning=true },-5,-11,33);
            Place(new Lantern { Name="Crew mess lantern",Duration=TimeSpan.Zero,Burning=true },-10,12,33);
            // The stable and work bays use coherent groups, with open central aisles.
            foreach(int y in new[]{6,7,10})Decorate(0x864,"Stable stall divider",-10,y);
            Decorate(0xF3A,"Grooming tools",-12,11,13);
            Decorate(0xE7F,"Stable water bucket",-11,11);
            var wheel=CompanyFixtures.OfType<SpinningwheelSouthAddon>().Single();
            wheel.MoveToWorld(new Point3D(X+10,Y-5,Z+7),Map);
            Decorate(0x1034,"Shipwright saw",7,-1,13);
            Decorate(0x102A,"Shipwright hammer",8,-1,13);
            Decorate(0x175D,"Sail cloth ready for cutting",8,-5,13);
            Place(new LongWoodenTableSouthAddon{Name="Gallery chart table"},-12,0,27);
            Decorate(0x14EB,"Harbor approaches chart",-12,0,33);
            Chair("Gallery reading chair",-10,0,27);
            foreach(int x in new[]{-3,1})Place(new Lantern{Name="Gallery stair lamp",Duration=TimeSpan.Zero,Burning=true},x,1,27);
            foreach(int x in new[]{12,13,14})foreach(int y in new[]{6,7,8,9,10})Decorate(0x31F4,"Courtyard planted bed",x,y,7);
            foreach(int y in new[]{6,10})Place(new DecoHay{Name="Fresh stall bedding"},-12,y,7);
            Place(new BluePlainRugAddon{Name="Council chamber rug"},-10,-8,27);
            // Broad entry and real gallery stairs are part of the structure, not named teleport props.
            var tiles = new MultiComponentList(Components);
            for(int x=-2;x<=-1;x++)
            {
                tiles.Remove(0x12,x,2,27);
                for(int step=0;step<4;step++) tiles.Add(0x722,x,6-step,7+step*5);
            }
            foreach(int x in new[]{-3,3}) { tiles.Add(0x9,x,14,7); tiles.Add(0x9,x,14,27); }
            for(int x=-3;x<=3;x++) tiles.Add(0x4A9,x,14,32);
            CurrentState = new DesignState(this,tiles); CurrentState.Revision=++LastRevision;
            DesignState=new DesignState(CurrentState); BackupState=new DesignState(CurrentState);
            var managed=new System.Collections.Generic.HashSet<Item>(CompanyFixtures);
            foreach(var addon in CompanyFixtures.OfType<BaseAddon>())foreach(var part in addon.Components)managed.Add(part);
            foreach(var fixture in CompanyFixtures.Where(i=>!positions.ContainsKey(i)||positions[i]!=i.Location)){
                var nearby=Map.GetItemsInRange(fixture.Location,0);
                try{foreach(Item item in nearby)if(item!=this&&item.Visible&&!managed.Contains(item)&&Math.Abs(item.Z-fixture.Z)<16)throw new InvalidOperationException("Player property occupies new fixture site: "+fixture.Location);}finally{nearby.Free();}
            }
            CheckFloorAccess(); CheckWalkingRoutes(true);
            foreach(var ladder in CompanyFixtures.OfType<HavenRecoveredLadder>().Where(l=>l.Name==LadderNames[0]||l.Name==LadderNames[1])) ladder.Visible=false;
            Place(new Static(1){Name="Island design revision 2",Visible=false},0,0,7);
            }catch{
                foreach(var added in CompanyFixtures.Except(originals).ToArray()){
                    var addon=added as BaseAddon;if(addon!=null)Addons.Remove(addon);
                    LockDowns.Remove(added);Secures.RemoveAll(sec=>sec.Item==added);CompanyFixtures.Remove(added);added.Delete();
                }
                foreach(var item in originals){item.MoveToWorld(positions[item],Map);item.ItemID=arts[item];item.Visible=visibility[item];}
                CurrentState=new DesignState(this,originalTiles);CurrentState.Revision=++LastRevision;
                DesignState=new DesignState(CurrentState);BackupState=new DesignState(CurrentState);
                throw;
            }
        }
    }
}






