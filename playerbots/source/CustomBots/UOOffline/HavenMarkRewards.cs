using System;
using System.Linq;
using Server.Gumps;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.UOOffline;

public static class HavenMarkRewards
{
    internal sealed record Offer(string Name, int Price, string Description, Func<Item> Create, int LegacyIndex = -1);
    internal sealed record Group(string Name, string Description, Offer[] Offers);
    internal static readonly Group[] Groups =
    [
        new("Bracelets", "Nine build themes; marks or gold.", Legacy(0,9)),
        new("Rings and relics", "Matching sets, pendant and talisman.", Legacy(9,11)),
        new("Field equipment", "Four new pieces that grow with you.", [
            new("Bulwark boots",180,"Shoes slot. +10 Parrying, +5 Healing, +5% defense chance, +5 Strength and +2 health regeneration. Blessed; grows to level 20 with shared combat experience.",()=>new HavenBulwarkBoots()),
            new("Tideweaver sash",200,"Middle torso slot. +10 Spellweaving, +10 Focus, +5% lower mana cost, +5% spell damage and +2 mana regeneration. Blessed; grows to level 20 with shared combat experience.",()=>new HavenTideweaverSash()),
            new("Beastkeeper's doublet",220,"Middle torso slot. +10 Animal Taming, Animal Lore and Veterinary, +5% defense chance and +2 health regeneration. Blessed; grows to level 20. Uses the same slot as a sash.",()=>new HavenBeastkeepersDoublet()),
            new("Prospector's apron",140,"Waist slot. +10 Mining, Lumberjacking and Fishing, +150 Luck, +5 Strength and +2 stamina regeneration. Blessed; grows to level 20 with shared combat experience.",()=>new HavenProspectorsApron())]),
        new("Pet supplies", "Bonding, colors and portable shrinking.", [
            new("Pet bonding potion",5,"Instantly bonds one eligible living pet you own. One use; consumed only on success.",()=>new HavenBondingPotion()),
            new("Pet dye",10,"Choose one of eight colors or restore the original color. Works on your pet or its shrunken token. One successful use; does not alter rarity or stats.",()=>new HavenPetDye()),
            new("Reusable pet leash",25,"Portable shrinking tool with unlimited uses. Keep in your backpack and target your living pet within three tiles.",()=>new HavenPetLeash())]),
        new("Crafting supplies", "Runic tools and equipment upkeep.", [
            new("Agapite runic hammer - 25 uses",100,"25 runic smithing uses. Applies native Agapite runic properties to eligible crafts; your skill and normal materials are still required.",()=>new RunicHammer(CraftResource.Agapite,25)),
            new("Verite runic hammer - 25 uses",175,"25 runic smithing uses with native Verite intensity and properties. Normal crafting skills and materials apply.",()=>new RunicHammer(CraftResource.Verite,25)),
            new("Valorite runic hammer - 25 uses",300,"25 runic smithing uses with native Valorite intensity and properties. Normal crafting skills and materials apply.",()=>new RunicHammer(CraftResource.Valorite,25)),
            new("Horned runic sewing kit - 25 uses",90,"25 runic tailoring uses with native Horned Leather intensity and properties. Normal skills and materials apply.",()=>new RunicSewingKit(CraftResource.HornedLeather,25)),
            new("Barbed runic sewing kit - 25 uses",175,"25 runic tailoring uses with native Barbed Leather intensity and properties. Normal skills and materials apply.",()=>new RunicSewingKit(CraftResource.BarbedLeather,25)),
            new("Fortification powder - 10 uses",10,"Ten uses to restore eligible equipment's maximum durability. Native durability limits apply.",()=>new PowderOfTemperament(10)),
            new("Equipment bless deed",30,"Bless one eligible weapon, armor piece, clothing item or jewelry item that belongs to you. Consumed on success.",()=>new HavenEquipmentBlessDeed())]),
        new("Resource deeds", "Bulk materials for your resource ledger.", [
            new("5,000 iron ingots",50,"One commodity deed containing 5,000 iron ingots. Compatible with the Resource Ledger.",()=>Deed(new IronIngot(5000))),
            new("1,000 valorite ingots",150,"One commodity deed containing 1,000 valorite ingots. Compatible with the Resource Ledger.",()=>Deed(new ValoriteIngot(1000))),
            new("2,500 oak boards",75,"One commodity deed containing 2,500 oak boards. Compatible with the Resource Ledger.",()=>Deed(new OakBoard(2500))),
            new("1,000 heartwood boards",125,"One commodity deed containing 1,000 heartwood boards. Compatible with the Resource Ledger.",()=>Deed(new HeartwoodBoard(1000))),
            new("2,500 spined leather",75,"One commodity deed containing 2,500 spined leather. Compatible with the Resource Ledger.",()=>Deed(new SpinedLeather(2500))),
            new("1,000 barbed leather",125,"One commodity deed containing 1,000 barbed leather. Compatible with the Resource Ledger.",()=>Deed(new BarbedLeather(1000)))]),
        new("Adventure and storage", "Reusable tools and fishing supplies.", [
            new("Resource Ledger",10,"Absorbs compatible resource deeds into balances. Withdraw a deed for the amount you choose, or transfer balances between ledgers. Works in your pack or your companion's pack.",()=>new HavenResourceLedger()),
            new("Gatherer's Resource Satchel",75,"Holds crafting resources, gems, fish and bandages with 90% resource weight reduction. Normal item-count limits; equipment and bags stay outside.",()=>new HavenResourceSatchel()),
            new("Wayfarer rune pouch",10,"Starts empty. Stores up to 60,000 ordinary blank recall runes. Cast Mark on the pouch to consume a stored blank and make a marked rune.",()=>new HavenRunePouch()),
            new("Runic Atlas",125,"Stores 48 marked locations in three chapters of 16. Drop marked runes on the atlas; normal recall and gate rules apply.",()=>new HavenRunicAtlas()),
            new("Mercy's Endless Bandage",500,"Unlimited bandage uses. Normal Healing and Veterinary skills, delays and checks apply. Keep this blessed item in your backpack.",()=>new HavenEndlessBandage()),
            new("The Gilded Pathfinder",750,"Target a decoded, unfinished treasure map in your backpack to travel beside its dig site. Normal travel restrictions, digging, guardians and loot remain. Blessed shovel with 1,000 digging uses.",()=>new HavenGoldenShovel()),
            new("White Fabled Fishing Net",20,"At 100 Fishing, has a 25% chance to summon Scalis when none is alive on that facet. If Scalis is already alive, reports his coordinates without consuming the net. Deep water required.",()=>new FabledFishingNet())]),
        new("House rewards", "Map furniture and resource generators.", [
            new("Treasure-map storage chest",40,"Organizes treasure maps, SOS messages and message bottles. Search by facet, level or coordinates. Holds up to 1,000 items and 5,000 weight; place and secure in your house.",()=>new HavenMapStorageChest()),
            new("House treasure-map library",100,"Place in your house and lock down or secure. Target a decoded map to find the matching travel destination. All supported facets; normal travel restrictions apply.",()=>new HavenHouseMapLibrary()),
            new("Tree stump deed",250,"Place a resource stump in your house and choose its orientation. Produces ten random logs each day and stores up to 100. No veteran account age required.",()=>new TreeStumpDeed()),
            new("Mining cart deed",350,"Choose an ore or gem cart and its orientation when placing in your house. Produces ten ingots or five gems per day, storing up to 100 ingots or 50 gems. No veteran account age required.",()=>new MiningCartDeed())]),
        new("Crafting recipes", "Permanently learned crafting options.", [
            new("Elven Quiver recipe",25,"Learn permanently with 65 Tailoring. Crafting uses 28 leather. This is the recipe, not a finished quiver.",()=>new RecipeScroll(501)),
            new("Slayer Longbow recipe",50,"Learn permanently with 75 Fletching. Crafting uses 20 logs and one Brilliant Amber. This is the recipe, not a finished bow.",()=>new RecipeScroll(206)),
            new("Barbed Longbow recipe",50,"Learn permanently with 75 Fletching. Crafting uses 20 logs and one Fire Ruby. This is the recipe, not a finished bow.",()=>new RecipeScroll(205))])
    ];

    private static Offer[] Legacy(int start,int count)
    {
        var entries=new SpecialRewardStone.RewardMenu().Entries;
        return Enumerable.Range(start,count).Select(index=>new Offer(
            entries[index].Name.Split(" - ")[0],index switch { 9=>250,19=>150,>=10=>30,_=>15 },
            index<9 ? "A Haven build bracelet that levels with shared experience. Also accepts 25,000 gold if you do not have enough marks." : "Special Haven equipment that levels with shared experience. Inspect its stats below.",
            ()=>SpecialRewardStone.RewardMenu.CreateReward(index),index)).ToArray();
    }
    private static Item Deed(Item resource)
    {
        var deed=new CommodityDeed();
        if(deed.SetCommodity(resource)) { return deed; }
        deed.Delete(); resource.Delete(); throw new InvalidOperationException("Haven reward resource cannot be deeded.");
    }
    internal static Offer Find(int group,int index) => group>=0 && group<Groups.Length && index>=0 && index<Groups[group].Offers.Length ? Groups[group].Offers[index] : null;
    internal static long AvailableMarks(Mobile from)
    {
        var pack=from?.Backpack;if(pack==null) { return 0; }
        var wallet=Math.Max(0,pack.FindItemByType<AdventurersWallet>()?.HavenMarks ?? 0);
        var loose=Math.Max(0,pack.GetAmount(typeof(HavenMark)));
        return wallet>long.MaxValue-loose ? long.MaxValue : wallet+loose;
    }
    internal static bool Buy(Mobile from,int group,int index)
    {
        var offer=Find(group,index);
        if(from?.Deleted!=false || !from.Alive || from.Backpack==null || offer==null) { return false; }
        var item=offer.Create();
        if(!from.Backpack.CheckHold(from,item,false))
        { item.Delete();from.SendMessage("Make room in your backpack; no marks were spent.");return false; }
        var marks=HavenEconomy.TryPayMarks(from,offer.Price);
        if(!marks && (offer.LegacyIndex is not (>=0 and <9) || !HavenEconomy.TryPay(from,25000)))
        { item.Delete();from.SendMessage($"You need {offer.Price:N0} Haven marks for {offer.Name}.");return false; }
        from.Backpack.DropItem(item);
        if(marks) { from.SendMessage($"Purchased {offer.Name} for {offer.Price:N0} Haven marks."); }
        else { from.SendMessage($"Purchased {offer.Name} for 25,000 gold."); }
        return true;
    }
    internal static bool CanUse(Mobile from,Item anchor) => from?.Alive==true && HavenShopAccess.CanUse(from,anchor);
    public static void DisplayTo(Mobile from,Item anchor)
    {
        if(!CanUse(from,anchor)) { from?.SendMessage("Keep your wallet in your pack, or stand beside the Haven reward stone.");return; }
        from.CloseGump<HavenMarkCategoryGump>();from.CloseGump<HavenListGump>();from.CloseGump<HavenMarkRewardPreview>();
        from.SendGump(new HavenMarkCategoryGump(anchor,from));
    }
    internal sealed class Menu : ItemListMenu,IHavenShop
    {
        internal int Category { get; }
        // House furniture includes tall bookcase artwork; reserve its full height.
        internal int RowHeight => Category == 7 ? 124 : 66;
        internal int PageSize => Category == 7 ? 2 : 4;
        internal Menu(int category) : base("Haven marks: "+Groups[category].Name,
            Groups[category].Offers.Select(offer=>new ItemListEntry($"{offer.Name} - {offer.Price:N0} marks",0x14F0)).ToArray()) { Category=category; }
        public Item CreateItem(int index) => Groups[Category].Offers[index].Create();
        public override void OnResponse(NetState state,int index) => Buy(state.Mobile,Category,index);
    }
}

public sealed class HavenMarkCategoryGump : Gump
{
    private readonly Item _anchor;
    public HavenMarkCategoryGump(Item anchor,Mobile from) : base(35,35)
    {
        _anchor=anchor;AddBackground(0,0,720,515,9270);AddBackground(10,10,700,495,3000);
        AddHtml(25,24,665,30,"<BASEFONT COLOR=#181818><B>Haven Mark Exchange</B></BASEFONT>");
        AddLabel(25,65,0,$"Available Haven marks: {HavenMarkRewards.AvailableMarks(from):N0}");
        AddHtml(25,94,665,36,"<BASEFONT COLOR=#333333>Choose a category. Every item has a price and preview. Purchases use wallet marks, then loose marks in your backpack.</BASEFONT>");
        for(var i=0;i<HavenMarkRewards.Groups.Length;i++)
        {
            var group=HavenMarkRewards.Groups[i];var x=25+i%2*345;var y=150+i/2*62;
            AddButton(x,y,4005,4007,i+1);AddLabel(x+38,y+2,0,group.Name);
            AddHtml(x+38,y+25,294,33,$"<BASEFONT COLOR=#333333>{group.Description}</BASEFONT>");
        }
        AddLabel(25,477,0,"Supplies and resource deeds can be purchased repeatedly.");
        AddButton(620,466,4017,4019,0);AddLabel(657,468,0,"Close");
    }
    public override void OnResponse(NetState sender,in RelayInfo info)
    {
        var index=info.ButtonID-1;if(!HavenMarkRewards.CanUse(sender.Mobile,_anchor) || index<0 || index>=HavenMarkRewards.Groups.Length) { return; }
        sender.Mobile.SendGump(new HavenListGump(_anchor,new HavenMarkRewards.Menu(index)));
    }
}

public sealed class HavenMarkRewardPreview : Gump
{
    private readonly Item _anchor;
    private readonly HavenMarkRewards.Menu _menu;
    private readonly int _index;
    private readonly int _page;
    internal HavenMarkRewardPreview(Item anchor,HavenMarkRewards.Menu menu,int index,int page,Mobile from) : base(35,35)
    {
        _anchor=anchor;_menu=menu;_index=index;_page=page;var offer=HavenMarkRewards.Find(menu.Category,index);
        AddBackground(0,0,620,490,9270);AddBackground(10,10,600,470,3000);
        AddHtml(25,25,565,48,$"<BASEFONT COLOR=#181818><B>{offer.Name}</B></BASEFONT>");
        AddLabel(25,77,0,$"Price: {offer.Price:N0} Haven marks   |   Available: {HavenMarkRewards.AvailableMarks(from):N0}");
        var item=offer.Create();
        try
        {
            AddItem(30,130,item.ItemID,item.Hue);
            AddHtml(105,115,480,285,$"<BASEFONT COLOR=#181818>{offer.Description}<BR><BR>{HavenItemPreviewGump.Describe(item)}</BASEFONT>",false,true);
        }
        finally { item.Delete(); }
        AddButton(25,437,4014,4016,2);AddLabel(65,439,0,"Back");
        AddButton(230,437,4005,4007,1);AddLabel(270,439,0,"Buy item");
        AddButton(490,437,4017,4019,0);AddLabel(530,439,0,"Close");
    }
    public override void OnResponse(NetState sender,in RelayInfo info)
    {
        if(info.ButtonID==0 || !HavenMarkRewards.CanUse(sender.Mobile,_anchor)) { return; }
        if(info.ButtonID==1) { HavenMarkRewards.Buy(sender.Mobile,_menu.Category,_index); }
        if(info.ButtonID is 1 or 2) { sender.Mobile.SendGump(new HavenListGump(_anchor,_menu,_page)); }
    }
}
