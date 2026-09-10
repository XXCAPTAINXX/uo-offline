using System;
using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Commands;
using Server.CustomBots;
using Server.Engines.BulkOrders;
using Server.Engines.Craft;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

public enum HavenMarketTrade { Smith, Tailor, Carpenter, Tinker, Fletcher, Scribe, Alchemist, Cook, Adventurer, Gatherer, Artifacts, Jewelry, GearSets }

// Stock is held by a persistent stall, never by a temporary play-session bot.
[SerializationGenerator(0)]
public partial class HavenMarketStall : Item
{
    [SerializableField(0)] private Mobile _artisan;
    [SerializableField(1)] private bool _tailoring;
    [SerializableField(2)] private DateTime _nextWork;
    [SerializableField(3)] private DateTime _nextOrder;
    [SerializableField(4)] private SmallBOD _order;
    [SerializableField(5)] private List<Item> _stock = new();
    [SerializableField(6)] private List<int> _prices = new();
    [SerializableField(7)] private long _sales;
    [SerializableField(8)] private long _workSequence;
    [SerializableField(9)] private HavenMarketTrade _trade;
    [SerializableField(10)] private Type _product;
    [SerializableField(11)] private LargeBOD _largeOrder;
    private Timer _timer;
    internal static readonly HashSet<HavenMarketStall> Registry = new();

    [Constructible]
    public HavenMarketStall() : base(0x1E5E)
    { Name = "Haven artisans' market"; Movable = false; }

    public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
    { reject = LRReason.CannotLift; return false; }

    public override bool CheckItemUse(Mobile from, Item item) => item == this;
    public override bool CheckTarget(Mobile from, Server.Targeting.Target target, object targeted) => targeted == this;

    internal void Setup(bool tailor)
        => Setup(tailor ? HavenMarketTrade.Tailor : HavenMarketTrade.Smith);
    internal void Setup(HavenMarketTrade trade)
    {
        Trade = trade;
        Tailoring = trade == HavenMarketTrade.Tailor;
        var artisan = new HavenMarketArtisan { Name = HavenMarketProduction.Name(trade), Title = $"the {trade.ToString().ToLowerInvariant()}" };
        Artisan = artisan;
        foreach (var secondary in new[] { SkillName.Blacksmith, SkillName.Tailoring, SkillName.Carpentry, SkillName.Tinkering, SkillName.Fletching, SkillName.Inscribe, SkillName.Alchemy, SkillName.Cooking, SkillName.Magery, SkillName.ArmsLore })
        { artisan.Skills[secondary].Base = 100; artisan.Skills[secondary].Cap = 120; }
        var skill = HavenMarketProduction.System(trade)?.MainSkill ?? SkillName.ItemID;
        artisan.Skills[skill].Base = 100;
        artisan.Skills[skill].Cap = 120;
        artisan.MoveToWorld(new Point3D(X, Y + 1, Z), Map);
        artisan.Stall = this;
        NextWork = Core.Now + TimeSpan.FromMinutes(1);
        Schedule();
    }

    [AfterDeserialization]
    private void Schedule()
    {
        Registry.Add(this);
        _timer?.Stop();
        if (!Deleted) { _timer = Timer.DelayCall(TimeSpan.FromSeconds(30), Pulse); }
    }
    private void Pulse()
    {
        try { Work(Core.Now); }
        catch (Exception error)
        {
            NextWork = Core.Now + TimeSpan.FromMinutes(5); Product = null;
            Server.Logging.LogFactory.GetLogger(typeof(HavenMarketStall)).Error(error, "Artisan work failed; retry delayed.");
        }
        finally { Schedule(); }
    }
    internal void Work(DateTime now)
    {
        if (Deleted || Artisan?.Deleted != false || now < NextWork || Stock.Count >= 24) { return; }
        // A restart settles at most one work step; no offline production backlog.
        NextWork = now + TimeSpan.FromMinutes(1);
        WorkSequence++;
        if (Trade is not HavenMarketTrade.Smith and not HavenMarketTrade.Tailor)
        { HavenMarketProduction.Work(this); return; }
        if (Order == null && now >= NextOrder)
        {
            if (LargeOrder == null && Utility.RandomDouble() < 0.2)
            { LargeOrder = Tailoring ? new LargeTailorBOD() : new LargeSmithBOD(); AddItem(LargeOrder); }
            Order = Tailoring ? SmallTailorBOD.CreateRandomFor(Artisan) : SmallSmithBOD.CreateRandomFor(Artisan);
            NextOrder = now + TimeSpan.FromHours(6);
            if (Order != null) { AddItem(Order); }
        }
        if (Order != null && !Order.Deleted)
        {
            if (Order.Complete)
            {
                if (CanCombineLarge(Order))
                {
                    LargeOrder.EndCombine(Artisan, Order);
                    Artisan.Target?.Cancel(Artisan, Server.Targeting.TargetCancelType.Canceled);
                    if (LargeOrder.Complete) { ListItem(LargeOrder, Math.Max(10000, LargeOrder.ComputeGold() * 3)); LargeOrder = null; }
                }
                else { ListItem(Order, Math.Max(1500, Order.ComputeGold() * 3)); }
                Order = null;
                return;
            }
            CraftOrder();
        }
        else
        {
            HavenMarketProduction.Work(this);
        }
    }

    private void CraftOrder()
    {
        var system = Tailoring ? DefTailoring.CraftSystem : DefBlacksmithy.CraftSystem;
        CraftItem recipe = null;
        foreach (var candidate in system.CraftItems) { if (candidate.ItemType == Order.Type) { recipe = candidate; break; } }
        if (recipe == null || recipe.Recipe != null) { return; }
        var resource = ResourceFor(Order.Material, Tailoring);
        var chance = recipe.GetSuccessChance(Artisan, null, system, false, out var eligible);
        if (!eligible) { GainCraftSkill(); return; }
        foreach (var requirement in recipe.Resources)
        {
            var type = requirement.ItemType == typeof(IronIngot) || requirement.ItemType == typeof(Leather) ? resource.Type : requirement.ItemType;
            if (Artisan.Backpack.GetAmount(type) < requirement.Amount)
            {
                // One minute of a stated gathering job, not instant free BOD completion.
                var gathered = BotItemFactory.Create(type.FullName);
                if (gathered == null) { return; }
                if (!gathered.Stackable) { gathered.Delete(); return; }
                gathered.Amount = Math.Max(10, requirement.Amount);
                Artisan.Backpack.DropItem(gathered);
                return;
            }
        }
        foreach (var requirement in recipe.Resources)
        {
            var type = requirement.ItemType == typeof(IronIngot) || requirement.ItemType == typeof(Leather) ? resource.Type : requirement.ItemType;
            Artisan.Backpack.ConsumeTotal(type, requirement.Amount);
        }
        GainCraftSkill();
        if (Utility.RandomDouble() >= chance) { return; }
        var product = BotItemFactory.Create(Order.Type.FullName);
        if (product == null) { return; }
        var exceptional = Utility.RandomDouble() < recipe.GetExceptionalChance(system, chance, Artisan);
        switch (product)
        {
            case BaseWeapon weapon: weapon.Resource = resource.Resource; weapon.Quality = exceptional ? WeaponQuality.Exceptional : WeaponQuality.Regular; break;
            case BaseArmor armor: armor.Resource = resource.Resource; armor.Quality = exceptional ? ArmorQuality.Exceptional : ArmorQuality.Regular; break;
            case BaseClothing clothing: clothing.Resource = resource.Resource; clothing.Quality = exceptional ? ClothingQuality.Exceptional : ClothingQuality.Regular; break;
        }
        if (Order.RequireExceptional && !exceptional) { ListItem(product, Math.Max(100, BotAppraisal.Value(product))); return; }
        Artisan.Backpack.DropItem(product);
        // Let the native BOD validate type, material and quality and consume the item.
        Order.EndCombine(Artisan, product);
        Artisan.Target?.Cancel(Artisan, Server.Targeting.TargetCancelType.Canceled);
    }

    private bool CanCombineLarge(SmallBOD small)
    {
        if (LargeOrder?.Deleted != false || LargeOrder.AmountMax != small.AmountMax || LargeOrder.Material != small.Material ||
            LargeOrder.RequireExceptional && !small.RequireExceptional) { return false; }
        foreach (var entry in LargeOrder.Entries)
        { if (entry.Details.Type == small.Type && entry.Amount < LargeOrder.AmountMax) { return true; } }
        return false;
    }

    private void GainCraftSkill()
    {
        var skill = Artisan.Skills[Tailoring ? SkillName.Tailoring : SkillName.Blacksmith];
        skill.Base = Math.Min(skill.Cap, skill.Base + 0.02);
    }
    internal static HavenResourceCatalog.Entry ResourceFor(BulkMaterialType material, bool tailor)
    {
        var index = material switch
        {
            BulkMaterialType.DullCopper => 1, BulkMaterialType.ShadowIron => 2, BulkMaterialType.Copper => 3,
            BulkMaterialType.Bronze => 4, BulkMaterialType.Gold => 5, BulkMaterialType.Agapite => 6,
            BulkMaterialType.Verite => 7, BulkMaterialType.Valorite => 8,
            BulkMaterialType.Spined => 24, BulkMaterialType.Horned => 25, BulkMaterialType.Barbed => 26,
            _ => tailor ? 23 : 0
        };
        return HavenResourceCatalog.Entries[index];
    }
    internal bool ListItem(Item item, int price)
    {
        if (item?.Deleted != false || price <= 0 || Stock.Count >= 24 || Stock.Contains(item)) { return false; }
        AddItem(item); Stock.Add(item); Prices.Add(price); this.MarkDirty(); return true;
    }
    internal bool Buy(Mobile buyer, Item item, int quotedPrice, bool delivery = false)
    {
        if (Deleted || Map == null || Map == Map.Internal || buyer?.Deleted != false || !buyer.Alive || buyer.Backpack == null ||
            (delivery ? !Registry.Contains(this) || !HavenMarketDirectory.CanShop(buyer) : buyer.Map != Map || !buyer.InRange(this, 8))) { return false; }
        var index = Stock.IndexOf(item);
        if (index < 0 || index >= Prices.Count || item.Deleted || item.Parent != this || Prices[index] != quotedPrice ||
            !buyer.Backpack.CheckHold(buyer, item, false, true) || !HavenEconomy.TryPay(buyer, quotedPrice)) { return false; }
        Stock.RemoveAt(index); Prices.RemoveAt(index); this.MarkDirty();
        Sales += quotedPrice; buyer.Backpack.DropItem(item); return true;
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Map != Map || !from.InRange(this, 8)) { from.SendMessage("Come closer to the market stall."); return; }
        from.CloseGump<HavenMarketGump>(); from.SendGump(new HavenMarketGump(this, 0));
    }
    public override void OnDelete()
    {
        _timer?.Stop(); _timer = null; Registry.Remove(this);
        if (Artisan is HavenMarketArtisan artisan) { artisan.Stall = null; artisan.Delete(); }
        Artisan = null; Order = null; LargeOrder = null; Stock.Clear(); Prices.Clear(); base.OnDelete();
    }
}

[SerializationGenerator(0)]
public partial class HavenMarketArtisan : BaseCreature
{
    public override bool CheckNonlocalLift(Mobile from, Item item) => false;
    public override bool CheckItemUse(Mobile from, Item item) => from == this;
    [SerializableField(0)] private HavenMarketStall _stall;
    public override bool IsInvulnerable => true;
    [Constructible]
    public HavenMarketArtisan() : base(AIType.AI_Vendor, FightMode.None, 10, 1)
    { Body = 0x190; Hue = 0x83EA; CantWalk = true; AddItem(new Backpack()); AddItem(new Robe(0x489)); AddItem(new Sandals()); }
    public override void OnDoubleClick(Mobile from) { Stall?.OnDoubleClick(from); }
    public override void OnDelete() { Stall = null; base.OnDelete(); }
}

public class HavenMarketGump : Gump
{
    private readonly HavenMarketStall _stall;
    private readonly Item[] _items;
    private readonly int[] _prices;
    private readonly int _page;
    public HavenMarketGump(HavenMarketStall stall, int page) : base(60, 60)
    {
        _stall = stall; _page = Math.Clamp(page, 0, Math.Max(0, (stall.Stock.Count - 1) / 8));
        _items = stall.Stock.Skip(_page * 8).Take(8).ToArray();
        _prices = stall.Prices.Skip(_page * 8).Take(8).ToArray();
        AddBackground(0, 0, 540, 445, 9270);
        AddLabel(20, 15, 1152, $"{stall.Artisan?.Name ?? "Haven"} — artisan market");
        AddLabel(20, 42, 0, "Wallet gold accepted. Hover over an item for properties.");
        for (var i = 0; i < _items.Length; i++)
        {
            var item = _items[i]; var y = 76 + i * 37;
            AddItem(20, y, item.ItemID, item.Hue); AddItemProperty(item.Serial);
            AddLabelCropped(65, y, 310, 22, 0, item is SmallBOD bod ? $"{(bod.Complete ? "Completed" : "Partial")} order: {bod.AmountMax} {bod.Type?.Name}" : item.Name ?? BotAppraisal.NameFor(item));
            AddLabel(370, y, 0, $"{_prices[i]:N0}g"); AddButton(470, y, 4005, 4007, i + 1);
        }
        AddButton(20, 395, 4014, 4016, 100); AddLabel(55, 395, 0, "Previous");
        AddButton(180, 395, 4005, 4007, 101); AddLabel(215, 395, 0, "Next");
        AddButton(370, 395, 4017, 4019, 0); AddLabel(405, 395, 0, "Close");
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (_stall.Deleted || from.Map != _stall.Map || !from.InRange(_stall, 8) || info.ButtonID == 0) { return; }
        var page = _page;
        if (info.ButtonID == 100) { page--; }
        else if (info.ButtonID == 101) { page++; }
        else if (info.ButtonID > 0 && info.ButtonID <= _items.Length && !_stall.Buy(from, _items[info.ButtonID - 1], _prices[info.ButtonID - 1]))
        { from.SendMessage("That purchase could not complete. Check funds, space, and whether the item has sold."); }
        from.SendGump(new HavenMarketGump(_stall, page));
    }
}

public static class HavenMarketCommands
{
    internal static Point3D[] ProposeLocations(Map map)
    {
        // Search open ground on Haven island, away from the bank's existing service ring.
        for (var radius = 0; radius <= 240; radius += 8)
        {
            for (var x = 3450 - radius; x <= 3450 + radius; x += 8)
            {
                for (var y = 2520 - radius; y <= 2520 + radius; y += 8)
                {
                    if (radius > 0 && Math.Abs(x - 3450) != radius && Math.Abs(y - 2520) != radius) { continue; }
                    var locations = new Point3D[Enum.GetValues<HavenMarketTrade>().Length];
                    var valid = true;
                    for (var i = 0; i < locations.Length; i++)
                    {
                        var sx = x + i % 3 * 6; var sy = y + i / 3 * 6;
                        var z = map.GetAverageZ(sx, sy);
                        locations[i] = new Point3D(sx, sy, z);
                        if (sx < 3330 || sx > 3790 || sy < 2360 || sy > 3070 || z < 0 || !map.CanSpawnMobile(locations[i]) || !map.CanSpawnMobile(new Point3D(sx, sy + 1, z)) ||
                            Server.Multis.BaseHouse.FindHouseAt(locations[i], map, 16) != null)
                        { valid = false; break; }
                        // Leave a walkable approach to every counter.
                        if (!map.CanSpawnMobile(new Point3D(sx + 1, sy + 1, z))) { valid = false; break; }
                    }
                    if (valid) { return locations; }
                }
            }
        }
        return null;
    }
    public static void Initialize()
    {
        CommandSystem.Register("HavenMarketBuild", AccessLevel.GameMaster, e =>
        {
            foreach (var stall in HavenMarketStall.Registry) { if (!stall.Deleted) { e.Mobile.SendMessage("A Haven market already exists; use its stalls or relocate it deliberately."); return; } }
            var locations = ProposeLocations(Map.Trammel);
            if (locations == null) { e.Mobile.SendMessage("No safe market site was found near Haven. Use HavenMarketPlace in a clear area."); return; }
            var trades = Enum.GetValues<HavenMarketTrade>();
            for (var i = 0; i < locations.Length; i++) { var stall = new HavenMarketStall(); stall.MoveToWorld(locations[i], Map.Trammel); stall.Setup(trades[i]); }
            e.Mobile.SendMessage($"Haven market built near {locations[0].X}, {locations[0].Y} in Trammel.");
        });
        CommandSystem.Register("HavenMarketPlace", AccessLevel.GameMaster, e =>
        {
            var from = e.Mobile;
            if (from.Map == null || from.Map == Map.Internal) { return; }
            foreach (var stall in HavenMarketStall.Registry) { if (!stall.Deleted && stall.Map == from.Map && from.InRange(stall, 30)) { from.SendMessage("A market is already here."); return; } }
            var trades = Enum.GetValues<HavenMarketTrade>();
            var locations = new Point3D[trades.Length];
            for (var i = 0; i < locations.Length; i++) { locations[i] = new Point3D(from.X + (i % 3 - 1) * 6, from.Y + i / 3 * 6 + 4, from.Z); }
            foreach (var location in locations)
            {
                if (!from.Map.CanFit(location.X, location.Y, location.Z, 16, true, true) || !from.Map.CanFit(location.X, location.Y + 1, location.Z, 16, true, true))
                { from.SendMessage("Choose a clear area with room for the market stalls and their artisans."); return; }
            }
            for (var i = 0; i < locations.Length; i++) { var stall = new HavenMarketStall(); stall.MoveToWorld(locations[i], from.Map); stall.Setup(trades[i]); }
            from.SendMessage("Thirteen market stalls are open. Crafting and gathering begin in one minute; brokers sell earned loot, artifacts, jewelry and set pieces.");
        });
    }
}
