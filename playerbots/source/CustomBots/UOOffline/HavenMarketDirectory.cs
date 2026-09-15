using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.CustomBots;
using Server.Engines.BulkOrders;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Spells;

namespace Server.UOOffline;

public static class HavenMarketDirectory
{
    internal readonly record struct Listing(HavenMarketStall Stall, Item Item, int Price, string Name);
    internal static bool CanShop(Mobile from) => from?.Deleted == false && from.Alive && from.Backpack != null &&
        from.Map != null && from.Map != Map.Internal && !from.Criminal && from.Spell == null && !SpellHelper.CheckCombat(from);
    internal static string Describe(Item item) => item switch
    {
        HavenMarketPetTicket ticket => ticket.DefaultName,
        HavenMinaxCreditNote note => $"{note.Amount:N0} Minax credits (redeemable note)",
        SmallBOD bod => $"{(bod.Complete ? "Completed" : "Partial")} BOD: {bod.AmountMax} {bod.Type?.Name} {bod.Material}{(bod.RequireExceptional ? " exceptional" : "")}",
        LargeBOD bod => $"{(bod.Complete ? "Completed" : "Partial")} large BOD: {bod.AmountMax} {bod.Material}{(bod.RequireExceptional ? " exceptional" : "")}",
        CommodityDeed deed when deed.Commodity != null => $"{deed.Commodity.Amount:N0} {deed.Commodity.Name ?? BotAppraisal.NameFor(deed.Commodity)} (deed)",
        _ => item.Name ?? BotAppraisal.NameFor(item)
    };
    internal static List<Listing> Search(string query, int category = -1)
    {
        query = (query ?? "").Trim(); if (query.Length > 60) { query = query[..60]; }
        var results = new List<Listing>();
        foreach (var stall in HavenMarketStall.Registry)
        {
            if (stall.Deleted || stall.Map == null || stall.Map == Map.Internal || category >= 0 && (int)stall.Trade != category) { continue; }
            for (var i = 0; i < stall.Stock.Count && i < stall.Prices.Count; i++)
            {
                var item = stall.Stock[i];
                if (item?.Deleted != false || item.Parent != stall || stall.Prices[i] <= 0) { continue; }
                var name = Describe(item);
                if (query.Length > 0 && !name.Contains(query, StringComparison.OrdinalIgnoreCase) &&
                    !item.GetType().Name.Contains(query, StringComparison.OrdinalIgnoreCase) && !stall.Trade.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)) { continue; }
                results.Add(new Listing(stall, item, stall.Prices[i], name));
            }
        }
        results.Sort((a, b) => { var price = a.Price.CompareTo(b.Price); return price != 0 ? price : a.Item.Serial.Value.CompareTo(b.Item.Serial.Value); });
        return results;
    }
    public static void Initialize() => CommandSystem.Register("market", AccessLevel.Player, e => Open(e.Mobile, e.ArgString));
    internal static void Open(Mobile from, string query = "", int category = -1, int page = 0)
    {
        if (!CanShop(from)) { from.SendMessage("Leave combat and finish casting before using the market."); return; }
        from.CloseGump<HavenMarketDirectoryGump>(); from.SendGump(new HavenMarketDirectoryGump(query, category, page));
    }
}

[SerializationGenerator(0)]
public partial class HavenMarketDirectoryBoard : Item
{
    [Constructible] public HavenMarketDirectoryBoard() : base(0x1E5E) { Name = "Haven Market — search all merchants"; Movable = false; }
    public override void OnDoubleClick(Mobile from)
    { if (from.Map == Map && from.InRange(this, 3)) { HavenMarketDirectory.Open(from); } }
}

public sealed class HavenMarketDirectoryGump : Gump
{
    private readonly string _query;
    private readonly int _category, _page;
    private readonly List<HavenMarketDirectory.Listing> _rows;
    public HavenMarketDirectoryGump(string query, int category, int page) : base(50, 50)
    {
        _query = (query ?? "").Trim(); if (_query.Length > 60) { _query = _query[..60]; }
        _category = Math.Clamp(category, -1, Enum.GetValues<HavenMarketTrade>().Length - 1);
        var all = HavenMarketDirectory.Search(_query, _category); _page = Math.Clamp(page, 0, Math.Max(0, (all.Count - 1) / 8));
        _rows = all.GetRange(_page * 8, Math.Min(8, all.Count - _page * 8));
        AddBackground(0, 0, 695, 525, 9270); AddLabel(25, 20, 1152, "Haven Market Directory");
        AddLabel(25, 48, 2101, "Real merchant stock · wallet gold · delivery to your backpack");
        AddImageTiled(25, 81, 400, 25, 2624); AddTextEntry(30, 82, 385, 23, 1152, 1, _query);
        AddButton(445, 82, 4005, 4007, 1); AddLabel(480, 82, 1152, "Search");
        AddButton(565, 82, 4005, 4007, 2); AddLabel(600, 82, 1152, "Clear");
        AddButton(25, 116, 4014, 4016, 3); AddLabel(63, 116, 2101, _category < 0 ? "All trades" : ((HavenMarketTrade)_category).ToString());
        AddButton(200, 116, 4005, 4007, 4); AddLabel(260, 116, 2101, $"{all.Count} listings · lowest price first");
        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i]; var y = 157 + i * 38;
            AddItem(25, y, row.Item.ItemID, row.Item.Hue); AddItemProperty(row.Item.Serial);
            AddLabelCropped(70, y, 385, 22, 1152, row.Name);
            AddLabelCropped(70, y + 18, 385, 19, 2101, row.Stall.Artisan?.Name ?? row.Stall.Trade.ToString());
            AddLabel(475, y, 1152, $"{row.Price:N0} gold"); AddButton(620, y, 4005, 4007, 100 + i);
        }
        if (_rows.Count == 0) { AddLabel(25, 165, 2101, "No matching stock. Merchants list goods as they earn or craft them."); }
        AddButton(25, 480, 4014, 4016, 5); AddLabel(62, 480, 1152, "Previous");
        AddLabel(225, 480, 2101, $"Page {_page + 1}/{Math.Max(1, (all.Count + 7) / 8)}");
        AddButton(400, 480, 4005, 4007, 6); AddLabel(437, 480, 1152, "Next");
        AddButton(580, 480, 4017, 4019, 0); AddLabel(617, 480, 1152, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile; if (info.ButtonID == 0 || !HavenMarketDirectory.CanShop(from)) { return; }
        if (info.ButtonID >= 100 && info.ButtonID < 100 + _rows.Count)
        { from.SendGump(new HavenMarketPurchaseGump(_rows[info.ButtonID - 100], _query, _category, _page)); return; }
        var category = _category; var page = _page; var query = _query;
        switch (info.ButtonID)
        {
            case 1: query = info.GetTextEntry(1) ?? ""; page = 0; break;
            case 2: query = ""; category = -1; page = 0; break;
            case 3: category = category <= -1 ? 12 : category - 1; page = 0; break;
            case 4: category = category >= 12 ? -1 : category + 1; page = 0; break;
            case 5: page--; break;
            case 6: page++; break;
            default: return;
        }
        HavenMarketDirectory.Open(from, query, category, page);
    }
}

public sealed class HavenMarketPurchaseGump : Gump
{
    private readonly HavenMarketDirectory.Listing _listing;
    private readonly string _query;
    private readonly int _category, _page;
    private bool _used;
    internal HavenMarketPurchaseGump(HavenMarketDirectory.Listing listing, string query, int category, int page) : base(90, 90)
    {
        _listing = listing; _query = query; _category = category; _page = page;
        AddBackground(0, 0, 535, 325, 9270); AddLabel(25, 20, 1152, "Review purchase");
        AddItem(25, 65, listing.Item.ItemID, listing.Item.Hue); AddItemProperty(listing.Item.Serial);
        AddLabelCropped(75, 62, 430, 22, 1152, listing.Name); AddLabel(75, 95, 2101, $"Price: {listing.Price:N0} gold");
        AddLabel(25, 135, 2101, "Hover over the item for its current properties.");
        AddLabel(25, 166, 2101, "Delivery is free. The price and stock are checked again when buying.");
        AddLabelCropped(25, 194, 480, 22, 2101, HavenMarketProvenance.Describe(listing.Item) ?? "Merchant stock");
        if (listing.Item is HavenMarketPetTicket)
        { AddButton(25, 230, 4005, 4007, 2); AddLabel(60, 230, 1152, "Inspect pet stats before buying"); }
        AddButton(25, 278, 4005, 4007, 1); AddLabel(60, 278, 1152, "Buy and deliver");
        AddButton(365, 278, 4017, 4019, 0); AddLabel(400, 278, 1152, "Back");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (_used) { return; } _used = true;
        if (info.ButtonID == 2 && HavenMarketDirectory.CanShop(state.Mobile) && !_listing.Stall.Deleted &&
            _listing.Item is HavenMarketPetTicket { Deleted: false, Pet: { Deleted: false } pet } &&
            _listing.Item.Parent == _listing.Stall && _listing.Stall.Stock.Contains(_listing.Item))
        {
            state.Mobile.SendGump(new HavenMarketPurchaseGump(_listing, _query, _category, _page));
            HavenAnimalLoreGump.DisplayTo(state.Mobile, pet);
            return;
        }
        if (info.ButtonID == 1)
        { state.Mobile.SendMessage(_listing.Stall.Buy(state.Mobile, _listing.Item, _listing.Price, true) ? "Purchase delivered to your backpack." : "Purchase could not complete. Check stock, price, funds, pack space and combat status."); }
        HavenMarketDirectory.Open(state.Mobile, _query, _category, _page);
    }
}
