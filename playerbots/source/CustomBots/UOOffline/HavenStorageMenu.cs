using System;
using System.Collections.Generic;
using System.Linq;
using Server.CustomBots;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

public static class HavenStorageAccess
{
    internal static bool CanUse(Mobile from, Container storage)
    {
        if (storage?.Deleted != false || from?.Deleted != false || !from.Alive) { return false; }
        if (storage is HavenMapStorageChest) { return HavenHouseMapLibrary.HouseAccess(from,storage) && !((HavenMapStorageChest)storage).Locked; }
        if (storage is GuildMasterChest master) { return master.CanAccess(from); }
        if (storage is GuildProfessionChest chest)
        {
            return chest.MasterChest?.CanAccess(from,false)==true && from.Map==chest.Map && from.InRange(chest,3) &&
                Math.Abs(from.Z-chest.Z)<=8 && from.InLOS(chest) && chest.IsAccessibleTo(from);
        }
        if (storage is not HavenResourceSatchel) { return false; }
        return storage.IsChildOf(from.Backpack) || storage.RootParent is HavenCompanion companion && companion.BoundOwner==from &&
            companion.Map==from.Map && from.InRange(companion,12) && from.InLOS(companion);
    }
    internal static List<Container> Roots(Container storage)
    {
        var roots=new List<Container> { storage };
        if (storage is GuildMasterChest master)
        {
            var house=BaseHouse.FindHouseAt(master);
            if(house!=null)
            { foreach(var secure in house.Secures) { if(secure.Item is HavenMapStorageChest maps && !maps.Deleted && !maps.Locked && BaseHouse.FindHouseAt(maps)==house) { roots.Add(maps); } } }
            foreach (var chest in master.FindLinked())
            { if (house!=null && BaseHouse.FindHouseAt(chest)==house) { roots.Add(chest); } }
        }
        return roots;
    }
    internal static List<Item> Contents(Container storage)
    {
        var result=new List<Item>(); var pending=new Stack<Container>(Roots(storage));
        while (pending.Count>0)
        {
            var container=pending.Pop();
            foreach (var item in container.Items.ToArray())
            {
                if (item.Deleted || item.IsVirtualItem) { continue; }
                if (item is Container child) { pending.Push(child); }
                else { result.Add(item); }
            }
        }
        return result;
    }
    internal static bool Accepts(Container storage, Item item) => item is not Container && !item.IsVirtualItem && item.Movable &&
        !item.IsSecure && !item.IsLockedDown && storage switch
        {
            HavenMapStorageChest => HavenMapStorageChest.Accepts(item),
            HavenResourceSatchel => HavenResourceSatchel.Accepts(item),
            GuildProfessionChest chest => chest.Role==GuildStorageRole.Overflow || GuildStorageClassifier.Classify(item)==chest.Role,
            GuildMasterChest => item is not GuildStorageKit,
            _ => false
        };
    internal static bool OwnedSource(Mobile from, Item source)
    {
        if (source?.Deleted != false) { return false; }
        if (source==from.Backpack || source.IsChildOf(from.Backpack)) { return true; }
        if (source.RootParent is HavenCompanion companion)
        { return companion.BoundOwner==from && from.Map==companion.Map && from.InRange(companion,12) && from.InLOS(companion); }
        var root=source.RootParent as Item ?? source;
        var house=BaseHouse.FindHouseAt(root);
        return house!=null && (house.IsOwner(from)||house.IsCoOwner(from)||house.IsGuildMember(from)) &&
            source.IsAccessibleTo(from) && from.Map==root.Map && from.InRange(root,3) && Math.Abs(from.Z-root.Z)<=8 && from.InLOS(root);
    }
    internal static int Collect(Mobile from, Container storage, Item source)
    {
        if (!CanUse(from,storage) || !OwnedSource(from,source) || source==storage || source.IsChildOf(storage)) { return 0; }
        var pending=new Stack<Item>(); pending.Push(source); var moved=0;
        while (pending.Count>0)
        {
            var item=pending.Pop();
            if (item.Deleted || item==storage || item.IsChildOf(storage) || item.IsVirtualItem) { continue; }
            if (item is Container bag)
            { foreach (var child in bag.Items.ToArray()) { pending.Push(child); } continue; }
            if (!OwnedSource(from,item) || !Accepts(storage,item)) { continue; }
            if (storage.TryDropItem(from,item,false)) { moved++; }
        }
        return moved;
    }
    internal static int Withdraw(Mobile from, Container storage, IEnumerable<Item> candidates, int amount)
    {
        if (!CanUse(from,storage) || from.Backpack==null || amount is <1 or >60000) { return 0; }
        var roots=Roots(storage); var moved=0;
        foreach (var item in candidates.ToArray())
        {
            if (moved>=amount) { break; }
            if (item.Deleted || item.IsVirtualItem || !item.Movable || !roots.Any(root=>item.IsChildOf(root))) { continue; }
            var take=item.Stackable ? Math.Min(amount-moved,item.Amount) : 1;
            Item remainder=null;
            if (take<item.Amount)
            { remainder=Mobile.LiftItemDupe(item,take); if (remainder==null) { continue; } }
            if (from.Backpack.TryDropItem(from,item,false)) { moved+=take; }
            else
            {
                if (remainder!=null) { item.Amount+=remainder.Amount; remainder.Delete(); }
                break;
            }
        }
        return moved;
    }
    internal static string DisplayName(Item item) => item is TreasureMap map ? $"{map.ChestMap?.Name} | Level {map.Level} | {(map.Completed ? "Completed" : map.Decoder==null ? "Undecoded" : $"{map.ChestLocation.X}, {map.ChestLocation.Y}")}" : item is CommodityDeed { Commodity: not null } deed
        ? $"{HavenMissionJournal.ItemName(deed.Commodity)} deed ({deed.Commodity.Amount:N0})"
        : HavenMissionJournal.ItemName(item);
    internal static string Description(Container storage) => storage switch
    {
        HavenMapStorageChest => "Treasure maps, SOS messages and message bottles. Search by facet, level or coordinates. Target a bag to collect matching items, including sub-bags.",
        HavenResourceSatchel => "Crafting resources, gems, fish and bandages. Equipment, deeds and bags stay outside. Resource weight is reduced by 90%.",
        GuildProfessionChest chest => RoleDescription(chest.Role),
        _ => "Receiving chest: targets loose items and sorts them into the linked profession stores. Unknown items go to Unsorted; unavailable destinations use Overflow."
    };
    internal static string RoleDescription(GuildStorageRole role) => role switch
    {
        GuildStorageRole.Smithing => "Ore, ingots, and matching resource deeds.",
        GuildStorageRole.Tailoring => "Clothing, hides, leather, cloth, cotton, flax, thread and yarn; matching deeds.",
        GuildStorageRole.Carpentry => "Logs, boards and lumber; matching deeds.",
        GuildStorageRole.Tinkering => "Tinker tools, lockpicks, gears, springs, axles and clock parts.",
        GuildStorageRole.Alchemy => "Reagents, potions, potion kegs and bottles; matching deeds.",
        GuildStorageRole.Scribing => "Scrolls, spellbooks and runebooks.",
        GuildStorageRole.Pantry => "Food, fish, cooking ingredients and matching deeds.",
        GuildStorageRole.Taming => "Bandages, pet claims, leashes and bonding supplies.",
        GuildStorageRole.Armory => "Weapons, armor, shields, jewelry, arrows and bolts.",
        GuildStorageRole.Treasury => "Gold, checks and gems.",
        GuildStorageRole.Resources => "Granite, sand, scales, stones, crystals and essences; matching deeds.",
        GuildStorageRole.Overflow => "Loose items whose usual storage is unavailable.",
        _ => "Loose items without a matching profession category."
    };
}

public sealed class HavenStorageMenu : Gump
{
    private readonly Container _storage;
    private readonly List<Item[]> _rows;
    private readonly string _search;
    private readonly int _page;
    private const int PageSize=10;
    public static void DisplayTo(Mobile from, Container storage, string search="", int page=0, int amount=1)
    {
        if (!HavenStorageAccess.CanUse(from,storage)) { from.SendMessage("Stand beside your storage, or keep your satchel in your pack."); return; }
        from.CloseGump<HavenStorageMenu>(); from.SendGump(new HavenStorageMenu(storage,search,page,amount));
    }
    private HavenStorageMenu(Container storage,string search,int page,int amount) : base(35,35)
    {
        _storage=storage; _search=search.Length>60 ? search[..60] : search;
        var all=HavenStorageAccess.Contents(storage);
        _rows=all.Where(i=>HavenStorageAccess.DisplayName(i).Contains(_search,StringComparison.OrdinalIgnoreCase))
            .GroupBy(i=> i.Stackable ? $"{i.GetType().FullName}|{i.ItemID}|{i.Hue}|{i.Name}|{i.LootType}|{i.PlayerConstructed}" : $"serial:{i.Serial}")
            .Select(g=>g.ToArray()).OrderBy(g=>HavenStorageAccess.DisplayName(g[0])).ToList();
        _page=Math.Clamp(page,0,Math.Max(0,(_rows.Count-1)/PageSize));
        AddBackground(0,0,700,620,9270); AddBackground(10,10,680,600,3000);
        AddLabel(25,24,0,storage.Name ?? "House storage");
        AddHtml(25,52,650,50,$"<BASEFONT COLOR=#181818>{HavenStorageAccess.Description(storage)}</BASEFONT>");
        AddLabel(25,109,0,"Search"); AddBackground(85,103,270,28,9350); AddTextEntry(92,108,255,22,0,0,_search);
        AddButton(368,108,4005,4007,1); AddLabel(405,109,0,"Find");
        AddLabel(490,109,0,"Take"); AddBackground(530,103,125,28,9350); AddTextEntry(539,108,110,22,0,1,amount.ToString());
        AddLabel(25,144,0,$"{_rows.Count:N0} entries   |   {all.Count:N0} stored stacks/items   |   Page {_page+1}");
        for(var row=0;row<PageSize && _page*PageSize+row<_rows.Count;row++)
        {
            var index=_page*PageSize+row;var group=_rows[index];var item=group[0];var y=180+row*33;
            AddButton(25,y,4005,4007,100+index);AddItem(67,y,item.ItemID,item.Hue);AddItemProperty(item.Serial);
            var name=HavenStorageAccess.DisplayName(item); if(name.Length>49) { name=name[..46]+"..."; }
            AddLabel(120,y+2,0,name);
            AddLabel(560,y+2,0,$"{group.Sum(i=>(long)i.Amount):N0}");
        }
        AddButton(25,521,4005,4007,2);AddLabel(62,523,0,"Previous");
        AddButton(175,521,4005,4007,3);AddLabel(212,523,0,"Next");
        AddButton(330,521,4005,4007,4);AddLabel(367,523,0,"Collect item / bag...");
        AddButton(25,560,4005,4007,5);AddLabel(62,562,0,"Normal container view");
        AddButton(305,560,4005,4007,6);AddLabel(342,562,0,"Accepted items");
        AddButton(575,560,4017,4019,0);AddLabel(612,562,0,"Close");
        AddLabel(25,592,0,"Collect checks sub-bags; unmatched items and the bags stay where they are.");
    }
    public override void OnResponse(NetState sender,in RelayInfo info)
    {
        var from=sender.Mobile;if(info.ButtonID==0 || !HavenStorageAccess.CanUse(from,_storage)) { return; }
        var search=info.GetTextEntry(0) ?? _search;
        if (!int.TryParse(info.GetTextEntry(1),out var amount) || amount is <1 or >60000)
        { from.SendMessage("Enter a withdrawal amount from 1 to 60,000."); DisplayTo(from,_storage,search,_page); return; }
        var page=_page;
        switch(info.ButtonID)
        {
            case 1: page=0;break;
            case 2: page--;break;
            case 3: page++;break;
            case 4: from.Target=new CollectTarget(_storage);from.SendMessage("Target your item, bag, or backpack; sub-bags are checked too.");return;
            case 5: _storage.DisplayTo(from);return;
            case 6: from.SendGump(new HavenStorageHelp(_storage));return;
            default:
                var index=info.ButtonID-100;
                if(index>=0 && index<_rows.Count)
                { var moved=HavenStorageAccess.Withdraw(from,_storage,_rows[index],amount);from.SendMessage($"Withdrew {moved:N0}. Items that do not fit remain in storage."); }
                break;
        }
        DisplayTo(from,_storage,search,page,amount);
    }
    private sealed class CollectTarget(Container storage) : Target(12,false,TargetFlags.None)
    {
        protected override void OnTarget(Mobile from,object targeted)
        {
            if(targeted is Item item) { from.SendMessage($"Collected {HavenStorageAccess.Collect(from,storage,item):N0} matching stacks/items."); }
            DisplayTo(from,storage);
        }
    }
}
public sealed class HavenStorageHelp : Gump
{
    private readonly Container _storage;
    public HavenStorageHelp(Container storage) : base(45,45)
    {
        _storage=storage; AddBackground(0,0,690,575,9270);AddBackground(10,10,670,555,3000);
        AddLabel(25,25,0,"What belongs in storage");
        var text=storage is HavenResourceSatchel or HavenMapStorageChest ? HavenStorageAccess.Description(storage) : string.Join("<BR><BR>",Enum.GetValues<GuildStorageRole>().Select(role=>$"<B>{GuildStorageNames.For(role)}</B><BR>{HavenStorageAccess.RoleDescription(role)}"));
        AddHtml(25,65,635,430,$"<BASEFONT COLOR=#181818>{text}</BASEFONT>",false,true);
        AddButton(25,530,4005,4007,1);AddLabel(65,532,0,"Back to storage");
    }
    public override void OnResponse(NetState sender,in RelayInfo info)
    { if(info.ButtonID==1) { HavenStorageMenu.DisplayTo(sender.Mobile,_storage); } }
}
