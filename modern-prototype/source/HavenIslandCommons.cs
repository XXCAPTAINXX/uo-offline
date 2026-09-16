using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.HavenPrototype
{
    public class HavenIslandCommons:Item
    {
        public readonly List<Item> Fixtures=new List<Item>();
        public readonly List<Mobile> Residents=new List<Mobile>();
        public static readonly Point3D Site=new Point3D(3968,2858,0);
        private static readonly Point2D[] ServiceApproaches={new Point2D(6,7),new Point2D(6,14),new Point2D(6,21),new Point2D(26,7),new Point2D(26,14),new Point2D(26,21),new Point2D(25,29),new Point2D(8,35)};
        public HavenIslandCommons():base(0xBD2){Name="Haven island community center";Visible=false;Movable=false;}
        public HavenIslandCommons(Serial serial):base(serial){}
        public static HavenIslandCommons BuildTest()
        {
            if(!HavenIslandInstall.CanBuild)throw new InvalidOperationException("Community-center prototype requires an isolated test world.");
            var map=Map.Trammel;
            for(int x=0;x<33;x++)for(int y=0;y<41;y++)
            {
                var point=new Point3D(Site.X+x,Site.Y+y,0);
                if(!map.CanFit(point,20,true,true)||BaseHouse.FindHouseAt(point,map,20)!=null)throw new InvalidOperationException("Community center blocked at "+point);
                var items=map.GetItemsInRange(point,0);try{foreach(Item item in items)if(item.Visible)throw new InvalidOperationException("Existing property at "+point);}finally{items.Free();}
            }
            var center=new HavenIslandCommons();center.MoveToWorld(Site,map);
            try{center.Build();return center;}catch{center.Delete();throw;}
        }
        private void Place(Item item,int x,int y,int z=0){Fixtures.Add(item);item.Movable=false;item.MoveToWorld(new Point3D(X+x,Y+y,Z+z),Map);}
        private void Build()
        {
            for(int x=0;x<33;x++)for(int y=0;y<41;y++)
            {
                Place(new Static(0x519),x,y);
                if(((x==0||x==32)&&Math.Abs(y-20)>2)||((y==0||y==40)&&Math.Abs(x-16)>2))Place(new Static(0x21),x,y,1);
            }
            foreach(int x in new[]{2,10,22,30})foreach(int y in new[]{2,38}){Place(new Static(0xDB),x,y);Place(new Static(0xB20){Light=LightType.Circle225},x,y,20);}
            int[] services={1,9,10,3,0,6};int[] colors={0x515,0x59D,0x489,0x48D,0x47E,0x972};
            for(int i=0;i<6;i++)
            {
                int x=i<3?5:25,y=6+(i%3)*7;
                Place(new HavenServiceStone(services[i]),x,y);
                Place(new Static(0xB90){Name="Service counter",Hue=colors[i]},x+2,y);
                Place(new Static(0x15AE){Name="Community hall banner",Hue=colors[i]},i<3?1:31,y,5);
            }
            Place(new SmallForgeAddon(),25,27);Place(new AnvilSouthAddon(),23,28);
            Place(new SpinningwheelSouthAddon(),28,28);Place(new LoomSouthAddon(),28,31);
            Place(new LargeCrate{Name="Public receiving crate"},5,28);
            Place(new LargeCrate{Name="Public crafting supplies"},7,28);
            Place(new WoodenChest{Name="Community donations"},5,31);
            Place(new Static(0xB2D){Name="Community bench"},12,31);Place(new Static(0xB2D){Name="Community bench"},18,31);
            var banker=new Banker{Name="Merrin",Title="the community banker"};Residents.Add(banker);banker.CantWalk=true;banker.MoveToWorld(new Point3D(X+8,Y+36,Z),Map);
            for(int x=14;x<=18;x++)for(int y=41;y<=106;y++)Place(new Static(0x7CD){Name="Commons dock walkway"},x,y);
        }
        public string[] CheckRoutes()
        {
            var queue=new Queue<Point2D>();var seen=new HashSet<Point2D>();queue.Enqueue(new Point2D(16,39));
            while(queue.Count>0){var p=queue.Dequeue();if(p.X<1||p.X>31||p.Y<1||p.Y>40||seen.Contains(p)||!Map.CanFit(X+p.X,Y+p.Y,0,16,false,false))continue;seen.Add(p);queue.Enqueue(new Point2D(p.X-1,p.Y));queue.Enqueue(new Point2D(p.X+1,p.Y));queue.Enqueue(new Point2D(p.X,p.Y-1));queue.Enqueue(new Point2D(p.X,p.Y+1));}
            return ServiceApproaches.Select(p=>(seen.Contains(p)?"PASS ":"FAIL ")+"commons service "+p).ToArray();
        }
        public override void OnDelete(){foreach(var item in Fixtures.ToArray())if(!item.Deleted){var container=item as Container;if(container!=null&&container.Items.Count>0){container.Movable=true;container.Name="Recovered community supplies";}else item.Delete();}foreach(var resident in Residents.ToArray())if(!resident.Deleted)resident.Delete();base.OnDelete();}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Fixtures.Count);foreach(var item in Fixtures)w.Write(item);w.Write(Residents.Count);foreach(var resident in Residents)w.Write(resident);}
        public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();int count=r.ReadInt();for(int i=0;i<count;i++){var item=r.ReadItem();if(item!=null)Fixtures.Add(item);}count=r.ReadInt();for(int i=0;i<count;i++){var mobile=r.ReadMobile();if(mobile!=null)Residents.Add(mobile);}}
    }
}
