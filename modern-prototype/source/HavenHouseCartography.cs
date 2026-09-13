using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.HavenPrototype
{
    public class HavenHouseMapLibrary : Item, ISecurable
    {
        public SecureLevel Level {get;set;}
        [Constructable] public HavenHouseMapLibrary():base(0xA9A){Name="Treasure-map travel library";Weight=10;Level=SecureLevel.CoOwners;}
        public HavenHouseMapLibrary(Serial serial):base(serial){}
        public static bool HouseAccess(Mobile from,Item item){
            if(from==null||from.Deleted||!from.Alive||item==null||item.Deleted||item.Parent!=null||from.Map!=item.Map||!from.InRange(item,3)||Math.Abs(from.Z-item.Z)>8||!from.InLOS(item))return false;
            var house=BaseHouse.FindHouseAt(item);var library=item as HavenHouseMapLibrary;
            return house!=null&&(house.IsOwner(from)||house.IsCoOwner(from)||house.IsGuildMember(from))&&item.IsAccessibleTo(from)&&(library==null||house.HasSecureAccess(from,library.Level));
        }
        public bool Match(Mobile from,TreasureMap map){return HouseAccess(from,this)&&HavenPortableTravel.ValidMap(from,map);}
        public bool Travel(Mobile from,Map map,Point3D point){return HouseAccess(from,this)&&HavenPortableTravel.Go(from,map,point,6);}
        public override void OnDoubleClick(Mobile from){if(HouseAccess(from,this))from.SendGump(new HavenHouseMapLibraryGump(this));else from.SendMessage("Place the library in your house and stand beside it. Secure or lock it down for safekeeping.");}
        public override void GetProperties(ObjectPropertyList list){base.GetProperties(list);list.Add("Browse treasure sites or target a decoded map on any facet.");}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write((int)Level);}public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Level=(SecureLevel)r.ReadInt();}
    }
    public class HavenHouseMapLibraryGump:HavenMenuGump
    {
        readonly HavenHouseMapLibrary _library;readonly HavenPreview.Destination[] _entries;readonly int _page;readonly string _search;
        public static IEnumerable<HavenPreview.Destination> Catalog(){
            yield return new HavenPreview.Destination("New Haven bank",Map.Trammel,3506,2570,14);yield return new HavenPreview.Destination("Britain bank",Map.Trammel,1438,1690,0);yield return new HavenPreview.Destination("Luna",Map.Malas,989,520,-50);yield return new HavenPreview.Destination("Umbra",Map.Malas,1997,1386,-85);
            foreach(var facet in new[]{Map.Malas,Map.TerMur}){int n=0;foreach(var point in HavenTreasureSites.Sites(facet))yield return new HavenPreview.Destination(facet.Name+" treasure "+(++n)+" - "+point.X+", "+point.Y,facet,point.X,point.Y,0);}
            int number=0;foreach(var point in TreasureMap.Locations??new Point2D[0]){number++;foreach(var map in new[]{Map.Trammel,Map.Felucca})yield return new HavenPreview.Destination("Treasure "+number.ToString("D3")+" - "+point.X+", "+point.Y+" ["+map.Name+"]",map,point.X,point.Y,0);}
        }
        public HavenHouseMapLibraryGump(HavenHouseMapLibrary library,string search="",int page=0):base(40,40){
            _library=library;_search=(search??"").Substring(0,Math.Min(60,(search??"").Length));_entries=Catalog().Where(e=>e.Name.IndexOf(_search,StringComparison.OrdinalIgnoreCase)>=0).ToArray();_page=Math.Max(0,Math.Min(page,Math.Max(0,(_entries.Length-1)/10)));
            AddBackground(0,0,650,555,3000);AddLabel(24,24,0,"Treasure-map travel library");FlatButton(24,62,400,1,"Find the site of my decoded map...");AddLabel(24,103,0,"Search");AddBackground(90,94,340,29,0xBB8);AddTextEntry(98,99,320,22,0,0,_search);FlatButton(454,100,170,2,"Search");
            for(int row=0;row<10;row++){int index=_page*10+row;if(index>=_entries.Length)break;FlatButton(24,145+row*30,600,100+index,_entries[index].Name);}
            AddLabel(24,462,0,"Catalog: Trammel, Felucca, Malas, Ter Mur. Target maps on any facet.");FlatButton(24,511,140,3,"Previous");AddLabel(216,511,0,"Page "+(_page+1)+" / "+Math.Max(1,(_entries.Length+9)/10));FlatButton(384,511,120,4,"Next");FlatButton(524,511,100,0,"Close");
        }
        public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(info.ButtonID==0||!HavenHouseMapLibrary.HouseAccess(p,_library))return;if(info.ButtonID==1){p.Target=new ChartTarget(_library);return;}if(info.ButtonID>=2&&info.ButtonID<=4){p.SendGump(new HavenHouseMapLibraryGump(_library,info.GetTextEntry(0)?.Text??_search,info.ButtonID==2?0:_page+(info.ButtonID==3?-1:1)));return;}int index=info.ButtonID-100;if(index>=0&&index<_entries.Length){var e=_entries[index];if(!_library.Travel(p,e.Map,e.Point))p.SendMessage("Travel is blocked or there is no safe landing.");}}
        sealed class ChartTarget:Target{readonly HavenHouseMapLibrary _library;public ChartTarget(HavenHouseMapLibrary library):base(-1,false,TargetFlags.None){_library=library;}protected override void OnTarget(Mobile p,object target){var map=target as TreasureMap;if(_library.Match(p,map))p.SendGump(new HavenMatchedChartGump(_library,map));else p.SendMessage("Target a decoded, unfinished map in your backpack beside your library.");}}
    }
    public class HavenMatchedChartGump:HavenMenuGump{
        readonly HavenHouseMapLibrary _library;readonly TreasureMap _map;readonly Map _facet;readonly Point2D _site;
        public HavenMatchedChartGump(HavenHouseMapLibrary library,TreasureMap map):base(60,60){_library=library;_map=map;_facet=map.Facet;_site=map.ChestLocation;AddBackground(0,0,510,245,3000);AddLabel(24,24,0,"Matching treasure site");AddLabel(24,64,0,_facet.Name+": "+_site.X+", "+_site.Y+" | Level "+map.Level);AddHtml(24,104,460,65,"Travel lands beside the dig site. Keep your decoded map: digging, guardians and treasure still work normally.",false,false);FlatButton(24,204,300,1,"Travel to this site");FlatButton(384,204,100,0,"Close");}
        public override void OnResponse(NetState state,RelayInfo info){if(info.ButtonID==1&&_library.Match(state.Mobile,_map)&&_map.Facet==_facet&&_map.ChestLocation==_site&&!_library.Travel(state.Mobile,_facet,new Point3D(_site,0)))state.Mobile.SendMessage("Travel is blocked or no safe landing was found.");}
    }
    public class HavenMapStorageChest:MetalGoldenChest{
        [Constructable]public HavenMapStorageChest(){Name="Cartographer's treasure-map chest";}
        public HavenMapStorageChest(Serial serial):base(serial){}
        public override int DefaultMaxItems{get{return 1000;}}public override int DefaultMaxWeight{get{return 5000;}}
        public static bool Accepts(Item item){return item is TreasureMap||item is SOS||item is MessageInABottle;}
        public bool CanUse(Mobile from){return !Locked&&HavenHouseMapLibrary.HouseAccess(from,this);}
        public override bool TryDropItem(Mobile from,Item item,bool message){return Accepts(item)&&base.TryDropItem(from,item,message);}
        public override bool OnDragDropInto(Mobile from,Item item,Point3D point){return Accepts(item)&&base.OnDragDropInto(from,item,point);}
        public override bool OnDragDrop(Mobile from,Item item){return Accepts(item)&&base.OnDragDrop(from,item);}
        public override void OnDoubleClick(Mobile from){if(CanUse(from))from.SendGump(new HavenMapStorageGump(this));else from.SendMessage("Place and unlock the chest in your house, then stand beside it.");}
        static bool OwnedSource(Mobile from,Item item){
            if(item==null||item.Deleted||!item.IsAccessibleTo(from))return false;
            if(HavenResources.Accessible(from,item))return true;
            if(item.RootParent is Mobile)return false;
            var point=item.GetWorldLocation();var house=BaseHouse.FindHouseAt(point,item.Map,16);
            return house!=null&&(house.IsOwner(from)||house.IsCoOwner(from))&&from.Map==item.Map&&from.InRange(point,3)&&from.InLOS(point);
        }
        public int Collect(Mobile from,Item source){if(!CanUse(from)||source==this||source==null||source.IsChildOf(this)||!OwnedSource(from,source))return 0;var container=source as Container;var items=container==null?new[]{source}:container.FindItemsByType(typeof(Item),true).ToArray();int moved=0;foreach(var item in items)if(Accepts(item)&&item.Movable&&OwnedSource(from,item)&&TryDropItem(from,item,false))moved++;return moved;}
        public bool Withdraw(Mobile from,Item item){return CanUse(from)&&item!=null&&!item.Deleted&&item.Parent==this&&item.Movable&&from.Backpack!=null&&from.Backpack.TryDropItem(from,item,false);}
        public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
    }
    public class HavenMapStorageGump:HavenMenuGump{
        readonly HavenMapStorageChest _chest;readonly Item[] _items;readonly string _search;readonly int _page;
        public static string Description(Item item){var map=item as TreasureMap;if(map!=null)return "Map | "+map.Facet+" | Level "+map.Level+" | "+map.ChestLocation.X+", "+map.ChestLocation.Y+(map.Completed?" | completed":map.Decoder==null?" | undecoded":" | decoded");var sos=item as SOS;if(sos!=null)return "SOS | "+sos.TargetMap+" | "+sos.TargetLocation.X+", "+sos.TargetLocation.Y;return item.Name??"Message in a bottle";}
        public HavenMapStorageGump(HavenMapStorageChest chest,string search="",int page=0):base(50,50){_chest=chest;_search=(search??"").Substring(0,Math.Min(60,(search??"").Length));_items=chest.Items.Where(x=>!x.Deleted&&Description(x).IndexOf(_search,StringComparison.OrdinalIgnoreCase)>=0).OrderBy(Description).ToArray();_page=Math.Max(0,Math.Min(page,Math.Max(0,(_items.Length-1)/10)));AddBackground(0,0,670,535,3000);AddLabel(24,20,0,"Cartographer's treasure-map chest");AddLabel(24,51,0,"Search by facet, level, coordinates, SOS or bottle. Select to withdraw.");AddBackground(24,80,405,30,0xBB8);AddTextEntry(32,85,388,22,0,0,_search);FlatButton(450,85,194,1,"Search");for(int row=0;row<10;row++){int index=_page*10+row;if(index>=_items.Length)break;FlatButton(24,130+row*29,620,100+index,Description(_items[index]));AddItemProperty(_items[index].Serial);}FlatButton(24,436,190,2,"Collect from my pack");FlatButton(230,436,194,3,"Target item / bag...");FlatButton(440,436,204,4,"Previous");FlatButton(24,484,190,5,"Next");AddLabel(246,484,0,"Page "+(_page+1)+" / "+Math.Max(1,(_items.Length+9)/10));FlatButton(544,484,100,0,"Close");}
        public override void OnResponse(NetState state,RelayInfo info){var p=state.Mobile;if(info.ButtonID==0||!_chest.CanUse(p))return;string search=info.GetTextEntry(0)?.Text??_search;int page=_page;if(info.ButtonID==1)page=0;else if(info.ButtonID==2)p.SendMessage("Stored "+_chest.Collect(p,p.Backpack)+" maps/messages.");else if(info.ButtonID==3){p.Target=new CollectTarget(_chest);return;}else if(info.ButtonID==4)page--;else if(info.ButtonID==5)page++;else if(info.ButtonID>=100&&info.ButtonID-100<_items.Length&&!_chest.Withdraw(p,_items[info.ButtonID-100]))p.SendMessage("Unable to withdraw; check backpack space.");p.SendGump(new HavenMapStorageGump(_chest,search,page));}
        sealed class CollectTarget:Target{readonly HavenMapStorageChest _chest;public CollectTarget(HavenMapStorageChest chest):base(-1,false,TargetFlags.None){_chest=chest;}protected override void OnTarget(Mobile p,object target){p.SendMessage("Stored "+_chest.Collect(p,target as Item)+" maps/messages.");if(_chest.CanUse(p))p.SendGump(new HavenMapStorageGump(_chest));}}
    }
}
