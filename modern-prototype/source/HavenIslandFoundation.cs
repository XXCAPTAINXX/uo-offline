using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.Items;
using Server.Multis;

namespace Server.HavenPrototype
{
    internal static partial class HavenIslandBlueprint
    {
        internal struct Tile
        {
            public readonly int Id,X,Y,Z;
            public Tile(int id,int x,int y,int z){Id=id;X=x;Y=y;Z=z;}
        }
        internal sealed class Group
        {
            public readonly string Name;public readonly bool House;public readonly Tile[] Tiles;
            public Group(string name,bool house,Tile[] tiles){Name=name;House=house;Tiles=tiles;}
        }
    }
    // Isolated foundation only. No automatic live-world or client-terrain installation.
    public class HavenIslandFoundation:Item
    {
        public readonly List<Item> Fixtures=new List<Item>();
        public static readonly Point3D EstateOrigin=new Point3D(4128,2800,0);
        public HavenIslandFoundation():base(0xBD2){Name="Corsair island foundation";Movable=false;Visible=false;}
        public HavenIslandFoundation(Serial serial):base(serial){}
        public static HavenIslandFoundation BuildTest()
        {
            if(!File.Exists("ISLAND-TEST-ONLY"))throw new InvalidOperationException("Island foundation requires an isolated test world.");
            if(World.Items.Values.OfType<HavenIslandFoundation>().Any(i=>!i.Deleted))throw new InvalidOperationException("Foundation already exists.");
            var map=Map.Trammel;
            if(!map.CanFit(4196,2868,0,16,false,true))throw new InvalidOperationException("Staged island terrain is not loaded.");
            foreach(var group in HavenIslandBlueprint.SettlementPlan)
            foreach(var tile in group.Tiles)
            {
                var point=new Point3D(EstateOrigin.X+tile.X,EstateOrigin.Y+tile.Y,tile.Z);
                if(tile.X<25||tile.X>150||tile.Y<25||tile.Y>164||BaseHouse.FindHouseAt(point,map,16)!=null)
                    throw new InvalidOperationException("Blueprint crosses protected ground: "+point);
                var nearby=map.GetItemsInRange(point,0);
                try{foreach(Item item in nearby)if(item.Visible)throw new InvalidOperationException("Existing property at "+point);}
                finally{nearby.Free();}
            }
            var foundation=new HavenIslandFoundation();foundation.MoveToWorld(EstateOrigin,map);
            try
            {
                foreach(var group in HavenIslandBlueprint.SettlementPlan)
                foreach(var tile in group.Tiles)foundation.Place(new Static(tile.Id){Name=group.Name},tile.X,tile.Y,tile.Z);
                for(int x=85;x<=89;x++)for(int y=135;y<=164;y++)foundation.Place(new Static(0x7CD){Name="Harbor dock"},x,y,0);
                for(int x=90;x<=100;x++)for(int y=159;y<=162;y++)foundation.Place(new Static(0x7CD){Name="Harbor boarding pier"},x,y,0);
                return foundation;
            }
            catch{foundation.Delete();throw;}
        }
        private void Place(Item item,int x,int y,int z)
        {
            Fixtures.Add(item);item.Movable=false;
            if(item.ItemID==0xB20)item.Light=LightType.Circle225;
            item.MoveToWorld(new Point3D(X+x,Y+y,Z+z),Map);
        }
        public string[] CheckRoutes()
        {
            var floors=new HashSet<Point2D>(Fixtures.Where(i=>i.Z==0&&i.ItemData.Height<=1).Select(i=>new Point2D(i.X,i.Y)));
            var visited=new HashSet<Point2D>();var pending=new Queue<Point2D>();pending.Enqueue(new Point2D(X+82,Y+128));
            while(pending.Count>0)
            {
                var p=pending.Dequeue();
                if(visited.Contains(p)||!floors.Contains(p)||!(Map.CanFit(p.X,p.Y,0,16,false,false)||Map.CanFit(p.X,p.Y,1,16,false,false)))continue;
                visited.Add(p);pending.Enqueue(new Point2D(p.X-1,p.Y));pending.Enqueue(new Point2D(p.X+1,p.Y));pending.Enqueue(new Point2D(p.X,p.Y-1));pending.Enqueue(new Point2D(p.X,p.Y+1));
            }
            return HavenIslandBlueprint.SettlementDestinations.Select(p=>(visited.Contains(new Point2D(X+p.X,Y+p.Y))?"PASS ":"FAIL ")+"route "+p.X+","+p.Y).ToArray();
        }
        public override void OnDelete(){foreach(var item in Fixtures.ToArray())if(!item.Deleted)item.Delete();base.OnDelete();}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Fixtures.Count);foreach(var item in Fixtures)w.Write(item);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();int count=r.ReadInt();for(int i=0;i<count;i++){var item=r.ReadItem();if(item!=null)Fixtures.Add(item);}}
    }
}
