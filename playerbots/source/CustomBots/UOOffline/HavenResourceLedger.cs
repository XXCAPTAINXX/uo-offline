using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenResourceLedger : Item
{
    [SerializableField(0)] private List<long> _balances = new();
    [Constructible]
    public HavenResourceLedger() : base(0xFF1)
    { Name = "Resource Ledger"; Hue = 0x489; Weight = 1; LootType = LootType.Blessed; }
    internal bool CanUse(Mobile from) => !Deleted && from?.Deleted == false && from.Alive && from.Backpack != null && IsChildOf(from.Backpack);
    internal long Balance(int index) => index >= 0 && index < Balances.Count ? Balances[index] : 0;
    internal bool Absorb(Mobile from, CommodityDeed deed)
    {
        if (!CanUse(from) || deed?.Deleted != false || !(deed.IsChildOf(from.Backpack) || from.Holding == deed)) { return false; }
        var commodity = deed.Commodity;
        var index = HavenResourceCatalog.Index(commodity);
        if (index < 0 || commodity.Amount <= 0 || Balance(index) > long.MaxValue - commodity.Amount) { return false; }
        while (Balances.Count <= index) { Balances.Add(0); }
        Balances[index] += commodity.Amount; this.MarkDirty();
        deed.Delete(); // The deed owns and deletes its internalized commodity.
        InvalidateProperties(); return true;
    }
    internal int AbsorbPack(Mobile from)
    {
        if (!CanUse(from)) { return 0; }
        var deeds = new List<CommodityDeed>();
        foreach (var deed in from.Backpack.FindItemsByType<CommodityDeed>()) { deeds.Add(deed); }
        var count = 0; foreach (var deed in deeds) { if (Absorb(from, deed)) { count++; } }
        return count;
    }
    internal bool Withdraw(Mobile from, int index, int amount)
    {
        if (!CanUse(from) || index < 0 || index >= HavenResourceCatalog.Entries.Length ||
            amount < 1 || amount > 60000 || Balance(index) < amount) { return false; }
        var item = HavenResourceCatalog.Entries[index].Create(amount);
        var deed = new CommodityDeed();
        if (!deed.SetCommodity(item)) { deed.Delete(); item.Delete(); return false; }
        if (!from.Backpack.TryDropItem(from, deed, false)) { deed.Delete(); return false; }
        Balances[index] -= amount; this.MarkDirty(); InvalidateProperties(); return true;
    }
    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (dropped is CommodityDeed deed && Absorb(from, deed)) { Open(from); return true; }
        from.SendMessage("Keep the ledger in your pack and drop a supported, filled resource deed onto it."); return false;
    }
    public override void OnDoubleClick(Mobile from) => Open(from);
    internal void Open(Mobile from, int page = 0, int amount = 1000)
    {
        if (!CanUse(from)) { from.SendMessage("Keep the Resource Ledger in your backpack to use it."); return; }
        from.CloseGump<HavenResourceLedgerGump>(); from.SendGump(new HavenResourceLedgerGump(this, page, amount));
    }
}

public class HavenResourceLedgerGump : Gump
{
    private readonly HavenResourceLedger _book;
    private readonly int _page;
    public HavenResourceLedgerGump(HavenResourceLedger book, int page, int amount) : base(70, 70)
    {
        _book = book; _page = Math.Clamp(page, 0, (HavenResourceCatalog.Entries.Length - 1) / 8);
        AddBackground(0, 0, 540, 455, 9270);
        AddLabel(20, 18, 0, "Resource Ledger");
        AddHtml(20, 48, 500, 38, "Matching deeds combine into balances. Set an amount, then choose a resource to withdraw a new deed.");
        for (var row = 0; row < 8; row++)
        {
            var index = _page * 8 + row;
            if (index >= HavenResourceCatalog.Entries.Length) { break; }
            var entry = HavenResourceCatalog.Entries[index]; var y = 98 + row * 30;
            AddButton(20, y, 4005, 4007, 100 + index);
            AddLabel(55, y + 2, 0, entry.Name); AddLabel(315, y + 2, 0, $"{book.Balance(index):N0}");
        }
        AddLabel(20, 345, 0, "Withdraw amount (1–60,000):");
        AddBackground(255, 340, 135, 30, 9350); AddTextEntry(262, 345, 120, 22, 0, 0, amount.ToString());
        AddButton(20, 382, 4005, 4007, 1); AddLabel(55, 384, 0, "Absorb pack deeds");
        AddButton(285, 382, 4005, 4007, 2); AddLabel(320, 384, 0, "Add deed...");
        if (_page > 0) { AddButton(20, 418, 4014, 4016, 3); }
        AddLabel(90, 420, 0, $"Page {_page + 1} / {(HavenResourceCatalog.Entries.Length + 7) / 8}");
        if ((_page + 1) * 8 < HavenResourceCatalog.Entries.Length) { AddButton(245, 418, 4005, 4007, 4); }
        AddButton(400, 418, 4005, 4007, 0); AddLabel(435, 420, 0, "Close");
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (!_book.CanUse(from) || info.ButtonID == 0) { return; }
        var valid = int.TryParse(info.GetTextEntry(0), out var amount) && amount is >= 1 and <= 60000;
        if (info.ButtonID == 1) { from.SendMessage($"Absorbed {_book.AbsorbPack(from)} deeds. Unsupported deeds remain in your pack."); }
        else if (info.ButtonID == 2) { from.Target = new DeedTarget(_book); from.SendMessage("Select a filled resource deed in your backpack."); return; }
        else if (info.ButtonID >= 100)
        {
            if (!valid) { from.SendMessage("Enter a whole number from 1 to 60,000."); }
            else if (!_book.Withdraw(from, info.ButtonID - 100, amount)) { from.SendMessage("Check your resource balance and make room in your pack. Nothing was withdrawn."); }
        }
        _book.Open(from, _page + (info.ButtonID == 3 ? -1 : info.ButtonID == 4 ? 1 : 0), valid ? amount : 1000);
    }
    private sealed class DeedTarget : Target
    {
        private readonly HavenResourceLedger _book;
        public DeedTarget(HavenResourceLedger book) : base(-1, false, TargetFlags.None) { _book = book; }
        protected override void OnTarget(Mobile from, object targeted)
        { if (targeted is not CommodityDeed deed || !_book.Absorb(from, deed)) { from.SendMessage("That deed could not be absorbed; it was kept."); } _book.Open(from); }
    }
}
