using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Spells;
using Server.Targeting;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenHouseMapLibrary : Item, ISecurable
{
    [SerializableField(0)] private SecureLevel _level=SecureLevel.CoOwners;
    [Constructible] public HavenHouseMapLibrary() : base(0xA9A) { Name="Treasure-map travel library";Weight=10; }
    internal static bool HouseAccess(Mobile from,Item item)
    {
        if(from?.Deleted!=false || !from.Alive || item?.Deleted!=false || item.Parent!=null || from.Map!=item.Map ||
            !from.InRange(item,3) || Math.Abs(from.Z-item.Z)>8 || !from.InLOS(item)) { return false; }
        var house=BaseHouse.FindHouseAt(item);
        return house!=null && (house.IsOwner(from)||house.IsCoOwner(from)||house.IsGuildMember(from)) && item.IsAccessibleTo(from);
    }
    public override void OnDoubleClick(Mobile from)
    {
        if(!HouseAccess(from,this)) { from.SendMessage("Place this library in your house and stand beside it. Secure or lock it down for safekeeping.");return; }
        from.SendGump(new HavenHouseMapLibraryGump(this));
    }
    internal bool Match(Mobile from,TreasureMap map) => HouseAccess(from,this) && map?.Deleted==false && map.Decoder!=null && !map.Completed &&
        from.Backpack!=null && map.IsChildOf(from.Backpack) && map.ChestMap!=null && map.ChestMap!=Map.Internal &&
        map.ChestLocation.X>0 && map.ChestLocation.Y>0 && map.ChestLocation.X<map.ChestMap.Width && map.ChestLocation.Y<map.ChestMap.Height;
    internal bool Travel(Mobile from,Map map,Point3D point)
    {
        if(!HouseAccess(from,this) || map==null || map==Map.Internal || from.Criminal || from.Spell!=null || SpellHelper.CheckCombat(from) ||
            !SpellHelper.CheckTravel(from,TravelCheckType.RecallFrom,out _)) { return false; }
        for(var r=1;r<=6;r++)
        for(var dx=-r;dx<=r;dx++)
        for(var dy=-r;dy<=r;dy++)
        {
            if(Math.Abs(dx)!=r && Math.Abs(dy)!=r) { continue; }
            var x=point.X+dx;var y=point.Y+dy;if(x<0||y<0||x>=map.Width||y>=map.Height) { continue; }
            var p=new Point3D(x,y,map.GetAverageZ(x,y));
            if(!map.CanSpawnMobile(p) || BaseHouse.FindHouseAt(p,map,16)!=null || !SpellHelper.CheckTravel(from,map,p,TravelCheckType.RecallTo,out _)) { continue; }
            BaseCreature.TeleportPets(from,p,map);from.MoveToWorld(p,map);from.PlaySound(0x1FE);return true;
        }
        return false;
    }
    public override void GetProperties(IPropertyList list)
    { base.GetProperties(list);list.Add($"{"House library: browse treasure runes or target a decoded map."}");list.Add($"{"Matching maps support every facet; normal travel restrictions apply."}"); }
}
public sealed class HavenHouseMapLibraryGump : Gump
{
    private readonly HavenHouseMapLibrary _library;private readonly int _page;
    private readonly List<(string Name,Point3D Location,Map Map)> _entries;
    public HavenHouseMapLibraryGump(HavenHouseMapLibrary library,string search="",int page=0) : base(40,40)
    {
        _library=library;search=search.Length>60 ? search[..60] : search;
        _entries=HavenTravelLibrary.Destinations().Where(e=>e.Name.Contains(search,StringComparison.OrdinalIgnoreCase)).ToList();
        _page=Math.Clamp(page,0,Math.Max(0,(_entries.Count-1)/10));
        AddBackground(0,0,650,555,9270);AddBackground(10,10,630,535,3000);
        AddLabel(25,25,0,"Treasure-map travel library");
        AddButton(25,60,4005,4007,1);AddLabel(65,62,0,"Find the rune for my decoded map...");
        AddLabel(25,100,0,"Search");AddBackground(90,95,340,28,9350);AddTextEntry(98,100,320,22,0,0,search);
        AddButton(455,100,4005,4007,2);AddLabel(495,102,0,"Find");
        for(var i=0;i<10 && _page*10+i<_entries.Count;i++)
        { var index=_page*10+i;var y=145+i*31;AddButton(25,y,4005,4007,100+index);AddLabel(65,y+2,0,_entries[index].Name); }
        AddLabel(25,468,0,"Catalog: Trammel / Felucca. Targeting supports decoded maps on all facets.");
        AddButton(25,510,4014,4016,3);AddLabel(65,512,0,"Previous");AddLabel(260,512,0,$"Page {_page+1}");
        AddButton(390,510,4005,4007,4);AddLabel(430,512,0,"Next");AddButton(570,510,4017,4019,0);
    }
    public override void OnResponse(NetState state,in RelayInfo info)
    {
        var from=state.Mobile;if(info.ButtonID==0 || !HavenHouseMapLibrary.HouseAccess(from,_library)) { return; }
        if(info.ButtonID==1) { from.Target=new ChartTarget(_library);from.SendMessage("Target a decoded, unfinished treasure map in your backpack.");return; }
        if(info.ButtonID is >=2 and <=4)
        { from.SendGump(new HavenHouseMapLibraryGump(_library,info.GetTextEntry(0) ?? "",info.ButtonID==2 ? 0 : _page+(info.ButtonID==3 ? -1 : 1)));return; }
        var index=info.ButtonID-100;if(index<0||index>=_entries.Count) { return; }
        var entry=_entries[index];if(!_library.Travel(from,entry.Map,entry.Location)) { from.SendMessage("Travel is blocked, or this destination has no safe landing."); }
    }
    private sealed class ChartTarget(HavenHouseMapLibrary library) : Target(3,false,TargetFlags.None)
    {
        protected override void OnTarget(Mobile from,object target)
        {
            if(target is TreasureMap map && library.Match(from,map)) { from.SendGump(new HavenMatchedChartGump(library,map)); }
            else { from.SendMessage("Use a decoded, unfinished map in your backpack while standing beside your library."); }
        }
    }
}
public sealed class HavenMatchedChartGump : Gump
{
    private readonly HavenHouseMapLibrary _library;private readonly TreasureMap _map;private readonly Map _facet;private readonly Point2D _site;
    public HavenMatchedChartGump(HavenHouseMapLibrary library,TreasureMap map) : base(60,60)
    {
        _library=library;_map=map;_facet=map.ChestMap;_site=map.ChestLocation;
        AddBackground(0,0,510,255,9270);AddBackground(10,10,490,235,3000);
        AddLabel(25,25,0,"Matching treasure rune");AddLabel(25,65,0,$"{_facet.Name}: {_site.X}, {_site.Y} | Level {map.Level}");
        AddHtml(25,105,455,65,"<BASEFONT COLOR=#181818>Travel lands beside the dig site. Keep your decoded map: digging, guardians and treasure still work normally.</BASEFONT>",false,false);
        AddButton(25,205,4005,4007,1);AddLabel(65,207,0,"Travel to this rune");AddButton(425,205,4017,4019,0);
    }
    public override void OnResponse(NetState state,in RelayInfo info)
    {
        if(info.ButtonID==1 && _library.Match(state.Mobile,_map) && _map.ChestMap==_facet && _map.ChestLocation==_site &&
            !_library.Travel(state.Mobile,_facet,new Point3D(_site.X,_site.Y,0))) { state.Mobile.SendMessage("Travel is blocked, or no safe landing was found."); }
    }
}
[SerializationGenerator(0)]
public partial class HavenMapStorageChest : MetalGoldenChest
{
    [Constructible] public HavenMapStorageChest() { Name="Cartographer's treasure-map chest"; }
    public override int DefaultMaxItems=>1000;
    public override int DefaultMaxWeight=>5000;
    internal static bool Accepts(Item item) => item is TreasureMap or SOS or MessageInABottle;
    public override void OnDoubleClick(Mobile from)=>HavenStorageMenu.DisplayTo(from,this);
    public override bool TryDropItem(Mobile from,Item item,bool message)=>Accepts(item)&&base.TryDropItem(from,item,message);
    public override bool TryDropItem(Mobile from,Item item,bool message,bool sound)=>Accepts(item)&&base.TryDropItem(from,item,message,sound);
    public override bool OnDragDropInto(Mobile from,Item item,Point3D p)=>Accepts(item)&&base.OnDragDropInto(from,item,p);
    public override void GetProperties(IPropertyList list)
    { base.GetProperties(list);list.Add($"{"Organizes treasure maps, SOS messages and bottles; targets nested bags."}"); }
}
public partial class HavenPirateHeadquarters
{
    internal void FurnishMapLibrary()
    {
        if(Deleted || !HasCompound || Customizer!=null) { return; }
        if(CompanyFixtures.Any(i=>i is HavenHouseMapLibrary && !i.Deleted)) { return; }
        var shelf=CompanyFixtures.FirstOrDefault(i=>i is Static && i.Name=="R.E.C. navigators' library");
        if(shelf==null) { return; }
        var at=new Point3D(X+12,Y-2,Z+27);
        if(!Map.CanFit(at,16,checkMobiles:false)) { throw new InvalidOperationException("Map chest location is occupied; existing furniture preserved."); }
        Place(new HavenHouseMapLibrary { Level=SecureLevel.Guild },shelf.X-X,shelf.Y-Y,shelf.Z-Z-1);
        Place(new HavenMapStorageChest(),12,-2,26);
        CompanyFixtures.Remove(shelf);LockDowns.Remove(shelf);shelf.Delete();this.MarkDirty();
    }
}
