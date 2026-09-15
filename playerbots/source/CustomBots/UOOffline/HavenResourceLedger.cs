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
    public static void Initialize() => CommandSystem.Register("CompanionStore", AccessLevel.Player, e =>
    {
        try { e.Mobile.SendMessage($"Stored {StoreCompanionPack(e.Mobile)} resource deeds in your companion's ledger. Unsupported deeds were kept."); }
        catch (InvalidOperationException ex) { e.Mobile.SendMessage(ex.Message); }
    });
    private HavenCompanion Carrier => RootParent as HavenCompanion;
    internal bool CanUse(Mobile from) => !Deleted && from?.Deleted == false && from.Alive && from.Backpack != null &&
        (IsChildOf(from.Backpack) || CanUseCarrier(from));
    private bool CanUseCarrier(Mobile from) => Carrier is { Deleted: false } companion &&
        companion.BoundOwner == from && companion.Backpack != null && IsChildOf(companion.Backpack) &&
        companion.Map != null && companion.Map != Map.Internal && companion.Map == from.Map && from.InRange(companion, 18);
    internal long Balance(int index) => index >= 0 && index < Balances.Count ? Balances[index] : 0;
    internal bool Absorb(Mobile from, CommodityDeed deed)
    {
        if (!CanUse(from) || deed?.Deleted != false || !(deed.IsChildOf(from.Backpack) || from.Holding == deed ||
            (CanUseCarrier(from) && deed.IsChildOf(Carrier.Backpack)))) { return false; }
        return Store(deed);
    }
    private bool CanStore(CommodityDeed deed)
    {
        if (Deleted || deed?.Deleted != false) { return false; }
        var index = HavenResourceCatalog.Index(deed.Commodity);
        return index >= 0 && deed.Commodity.Amount > 0 && Balance(index) <= long.MaxValue - deed.Commodity.Amount;
    }
    private bool Store(CommodityDeed deed)
    {
        if (!CanStore(deed)) { return false; }
        var commodity = deed.Commodity;
        var index = HavenResourceCatalog.Index(commodity);
        if (index < 0 || commodity.Amount <= 0 || Balance(index) > long.MaxValue - commodity.Amount) { return false; }
        while (Balances.Count <= index) { Balances.Add(0); }
        Balances[index] += commodity.Amount; this.MarkDirty();
        deed.Delete(); // The deed owns and deletes its internalized commodity.
        InvalidateProperties(); return true;
    }
    internal static bool StoreMissionReward(HavenCompanion companion, Item item, HavenMissionJournal journal)
    {
        if (companion?.Deleted != false || companion.BoundOwner?.Deleted != false || companion.Backpack == null ||
            item is not CommodityDeed deed || deed.Deleted) { return false; }
        foreach (var book in companion.Backpack.FindItemsByType<HavenResourceLedger>())
        {
            if (!book.IsChildOf(companion.Backpack) || !book.CanStore(deed)) { continue; }
            // Record the actual material before Store deletes the filled deed. All work is on the game loop.
            journal?.Receipt(deed, "Companion Resource Ledger");
            return book.Store(deed);
        }
        return false;
    }
    internal int AbsorbPack(Mobile from)
    {
        if (!CanUse(from)) { return 0; }
        var deeds = new List<CommodityDeed>();
        var pack = CanUseCarrier(from) ? Carrier.Backpack : from.Backpack;
        foreach (var deed in pack.FindItemsByType<CommodityDeed>()) { deeds.Add(deed); }
        var count = 0; foreach (var deed in deeds) { if (Absorb(from, deed)) { count++; } }
        return count;
    }
    internal static int StoreCompanionPack(Mobile owner)
    {
        var companion = HavenCompanionGearAssignment.Find(owner);
        if (owner?.Deleted != false || companion?.Deleted != false || companion.BoundOwner != owner || companion.Backpack == null)
        { throw new InvalidOperationException("No owned companion pack found."); }
        if (companion.Backpack.FindItemByType<HavenResourceLedger>() == null)
        { throw new InvalidOperationException("The companion needs a Resource Ledger in its pack."); }
        var deeds = new List<CommodityDeed>();
        foreach (var deed in companion.Backpack.FindItemsByType<CommodityDeed>()) { deeds.Add(deed); }
        var count = 0;
        foreach (var deed in deeds)
        { if (deed.IsChildOf(companion.Backpack) && StoreMissionReward(companion, deed, null)) { count++; } }
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
    internal bool TransferAll(Mobile from, HavenResourceLedger destination, out int resourceTypes)
    {
        resourceTypes = 0;
        if (!CanUse(from) || destination == null || destination == this || !destination.CanUse(from) ||
            !destination.IsChildOf(from.Backpack)) { return false; }

        // Validate every balance before changing either book. Transfer on the game loop, without creating deeds.
        for (var index = 0; index < Balances.Count; index++)
        {
            var amount = Balance(index);
            var existing = destination.Balance(index);
            if (amount < 0 || existing < 0 || existing > long.MaxValue - amount) { return false; }
        }
        for (var index = 0; index < Balances.Count; index++)
        {
            var amount = Balance(index);
            if (amount == 0) { continue; }
            while (destination.Balances.Count <= index) { destination.Balances.Add(0); }
            destination.Balances[index] += amount;
            Balances[index] = 0;
            resourceTypes++;
        }
        if (resourceTypes > 0)
        {
            this.MarkDirty(); destination.MarkDirty();
            InvalidateProperties(); destination.InvalidateProperties();
        }
        return true;
    }
    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (dropped is CommodityDeed deed && Absorb(from, deed)) { Open(from); return true; }
        from.SendMessage("Keep the ledger in your pack or your nearby companion's pack and add a supported resource deed."); return false;
    }
    public override void OnDoubleClick(Mobile from) => Open(from);
    internal void Open(Mobile from, int page = 0, int amount = 1000)
    {
        if (!CanUse(from)) { from.SendMessage("Keep the ledger in your pack or your nearby companion's pack to use it."); return; }
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
        AddBackground(0, 0, 540, 455, 5054);
        AddBackground(10, 10, 520, 435, 3000);
        AddLabel(20, 18, 0, "Resource Ledger");
        AddButton(330, 16, 4005, 4007, 5); AddLabel(365, 18, 0, "Transfer all...");
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
        if (info.ButtonID == 1) { from.SendMessage($"Absorbed {_book.AbsorbPack(from)} deeds from the ledger carrier's pack. Unsupported deeds were kept."); }
        else if (info.ButtonID == 2) { from.Target = new DeedTarget(_book); from.SendMessage("Select a filled resource deed in your pack or the companion's pack carrying this ledger."); return; }
        else if (info.ButtonID == 5)
        {
            from.Target = new LedgerTransferTarget(_book);
            from.SendMessage("Select the Resource Ledger in your backpack to receive all stored resources from this book.");
            return;
        }
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
    private sealed class LedgerTransferTarget : Target
    {
        private readonly HavenResourceLedger _source;
        public LedgerTransferTarget(HavenResourceLedger source) : base(-1, false, TargetFlags.None) { _source = source; }
        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted is not HavenResourceLedger destination || !_source.TransferAll(from, destination, out var types))
            {
                from.SendMessage("Nothing transferred. Choose a different Resource Ledger in your backpack, stay near your companion, and check the destination has balance space.");
            }
            else if (types == 0) { from.SendMessage("This Resource Ledger has no stored resources to transfer."); }
            else { from.SendMessage($"Transferred all balances for {types} resource types into your Resource Ledger."); }
            _source.Open(from);
        }
        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType) => _source.Open(from);
    }
}
