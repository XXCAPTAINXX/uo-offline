using System;
using System.Linq;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class ArcaneSupplyStone : Item
{
    [Constructible]
    public ArcaneSupplyStone() : base(0xED4) { Name = "Arcane Supplies"; Hue = 0x482; Movable = false; }
    public override void OnDoubleClick(Mobile from)
    {
        if (!HavenShopAccess.CanUse(from, this)) { return; }
        from.CloseGump<HavenListGump>();
        from.SendGump(new HavenListGump(this, new Menu()));
    }

    internal sealed class Menu : ItemListMenu, IHavenShop
    {
        private static readonly string[] Names = ["Full Magery spellbook", "Full Necromancy spellbook", "Full Chivalry book", "Full Spellweaving book", "Full Bushido book", "Full Ninjitsu book", "Runebook", "Runic atlas — 48 locations", "Bag of sending — 30 charges", "Translocation powder — 10 doses", "Fortification powder — 10 uses", "Clothing bless deed", "Equipment bless deed", "Blank recall rune", "Reagent bundle — 100 of each", "Bandages — 500", "Arrows — 500", "Bolts — 500", "Recall scrolls — 50", "Potion bundle — 10 of each", "Resource Ledger — combine commodity deeds", "Resource satchel — 90% lighter resources", "White Fabled Fishing Net — Scalis chance", "World map — Soulbinder altar offering"];
        private static readonly int[] Prices = [500, 1500, 1000, 2000, 1000, 1000, 500, 5000, 2500, 1000, 5000, 10000, 50000, 50, 3000, 500, 1000, 1000, 2000, 1500, 1000, 15000, 25000, 500];
        public Menu() : base("Arcane supplies — wallet gold accepted", Names.Select((name, index) => new ItemListEntry($"{name} — {Prices[index]:N0} gold", 0xEFA)).ToArray()) { }
        public Item CreateItem(int index)
        {
            Item item = index switch
            {
                0 => new Spellbook(), 1 => new NecromancerSpellbook(), 2 => new BookOfChivalry(),
                3 => new SpellweavingBook(), 4 => new BookOfBushido(), 5 => new BookOfNinjitsu(),
                6 => new Runebook(), 7 => new HavenRunicAtlas(), 8 => new BagOfSending { Charges = 30 },
                9 => new PowderOfTranslocation(10), 10 => new PowderOfTemperament(10),
                11 => new ClothingBlessDeed(), 12 => new HavenEquipmentBlessDeed(), 13 => new RecallRune(),
                14 => ReagentBundle(), 15 => new Bandage(500), 16 => new Arrow(500), 17 => new Bolt(500),
                18 => new RecallScroll(50), 20 => new HavenResourceLedger(), 21 => new HavenResourceSatchel(), 22 => new FabledFishingNet(), 23 => new WorldMap(), _ => PotionBundle()
            };
            if (item is Spellbook book) { book.Content = book.BookCount == 64 ? ulong.MaxValue : (1UL << book.BookCount) - 1; }
            return item;
        }
        private static Bag ReagentBundle()
        {
            var bag = new Bag { Name = "reagent bundle" };
            bag.DropItem(new BlackPearl(100)); bag.DropItem(new Bloodmoss(100)); bag.DropItem(new Garlic(100));
            bag.DropItem(new Ginseng(100)); bag.DropItem(new MandrakeRoot(100)); bag.DropItem(new Nightshade(100));
            bag.DropItem(new SulfurousAsh(100)); bag.DropItem(new SpidersSilk(100));
            return bag;
        }
        private static Bag PotionBundle()
        {
            var bag = new Bag { Name = "adventurer's potion bundle" };
            for (var i = 0; i < 10; i++) { bag.DropItem(new GreaterHealPotion()); bag.DropItem(new GreaterCurePotion()); bag.DropItem(new TotalRefreshPotion()); }
            return bag;
        }        internal bool Buy(Mobile from, int index)
        {
            if (from.Backpack == null || index < 0 || index >= Prices.Length) { return false; }
            var item = CreateItem(index);
            if (!from.Backpack.CheckHold(from, item, false)) { item.Delete(); from.SendMessage("Make room in your backpack first."); return false; }
            if (!HavenEconomy.TryPay(from, Prices[index])) { item.Delete(); from.SendMessage($"You need {Prices[index]:N0} gold."); return false; }
            from.Backpack.DropItem(item);
            from.SendMessage($"Purchased {Names[index]} for {Prices[index]:N0} gold.");
            return true;
        }
        public override void OnResponse(NetState state, int index) => Buy(state.Mobile, index);
    }
}

[SerializationGenerator(0)]
public partial class HavenRunicAtlas : Container
{
    [Constructible]
    public HavenRunicAtlas() : base(0x22C5)
    {
        Name = "runic atlas"; Hue = 0x482; Weight = 3; LootType = LootType.Blessed; MaxItems = 3;
        for (var i = 1; i <= 3; i++) { DropItem(new Runebook { Description = $"Atlas chapter {i}", Movable = false }); }
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Backpack == null || !IsChildOf(from.Backpack)) { from.SendMessage("Keep the atlas in your backpack."); return; }
        from.CloseGump<AtlasGump>();
        from.SendGump(new AtlasGump(this));
    }
    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (from.Backpack == null || !IsChildOf(from.Backpack) || dropped is not RecallRune { Marked: true }) { return false; }
        foreach (var book in Items.OfType<Runebook>())
        {
            if (book.Entries.Count < 16) { return book.OnDragDrop(from, dropped); }
        }
        from.SendMessage("All 48 atlas locations are full.");
        return false;
    }
    private sealed class AtlasGump : Gump
    {
        private readonly HavenRunicAtlas _atlas;
        public AtlasGump(HavenRunicAtlas atlas) : base(40, 40)
        {
            _atlas = atlas;
            AddBackground(0, 0, 360, 255, 5054);
            AddLabel(25, 20, 0, "Runic atlas — 48 locations");
            var books = atlas.Items.OfType<Runebook>().ToArray();
            for (var i = 0; i < books.Length; i++)
            {
                AddButton(25, 65 + i * 42, 4005, 4007, i + 1);
                AddLabel(65, 67 + i * 42, 0, $"Chapter {i + 1}: {books[i].Entries.Count}/16 locations");
            }
            AddHtml(25, 200, 310, 40, "Drop marked runes onto the atlas.<BR>Open a chapter to recall, gate or remove runes.");
        }
        public override void OnResponse(NetState state, in RelayInfo info)
        {
            if (info.ButtonID < 1 || _atlas.Deleted || state.Mobile.Backpack == null || !_atlas.IsChildOf(state.Mobile.Backpack)) { return; }
            var books = _atlas.Items.OfType<Runebook>().ToArray();
            if (info.ButtonID <= books.Length) { books[info.ButtonID - 1].OnDoubleClick(state.Mobile); }
        }
    }
}

[SerializationGenerator(0)]
public partial class HavenEquipmentBlessDeed : Item
{
    [Constructible]
    public HavenEquipmentBlessDeed() : base(0x14F0) { Name = "equipment bless deed"; Weight = 1; LootType = LootType.Blessed; }
    internal bool Bless(Mobile from, Item item)
    {
        if (Deleted || from.Backpack == null || !IsChildOf(from.Backpack) || item?.Deleted != false || item.RootParent != from ||
            item is not (BaseWeapon or BaseArmor or BaseClothing or BaseJewel) || item is BaseClothing { CanBeBlessed: false } || item.LootType != LootType.Regular) { return false; }
        item.LootType = LootType.Blessed; Delete(); from.SendMessage("Your equipment is now blessed."); return true;
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Backpack != null && IsChildOf(from.Backpack)) { from.Target = new BlessTarget(this); }
    }
    private sealed class BlessTarget : Target
    {
        private readonly HavenEquipmentBlessDeed _deed;
        public BlessTarget(HavenEquipmentBlessDeed deed) : base(1, false, TargetFlags.None) => _deed = deed;
        protected override void OnTarget(Mobile from, object target)
        {
            if (target is not Item item || !_deed.Bless(from, item)) { from.SendMessage("Choose your own unblessed weapon, armor, clothing or jewelry."); }
        }
    }
}
