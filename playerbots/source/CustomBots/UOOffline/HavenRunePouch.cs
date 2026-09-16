using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Spells;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenRunePouch : Item
{
    [SerializableField(0)] private int _runes;
    [Constructible]
    public HavenRunePouch() : base(0xE79)
    { Name = "Wayfarer's rune pouch"; Hue = 0x482; Weight = 1; LootType = LootType.Blessed; }
    internal bool CanUse(Mobile from) => !Deleted && from?.Deleted == false && from.Alive &&
        from.Backpack != null && IsChildOf(from.Backpack);
    internal bool Store(Mobile from, Item item)
    {
        if (!CanUse(from) || Runes >= 60000 || item?.Deleted != false || item.GetType() != typeof(RecallRune) ||
            item is not RecallRune { Marked: false, House: null, TargetMap: null, Name: null, Hue: 0, Amount: 1, LootType: LootType.Regular } rune ||
            !string.IsNullOrEmpty(rune.Description) || rune.Target != Point3D.Zero || rune.Items.Count != 0 ||
            !(rune.IsChildOf(from.Backpack) || from.Holding == rune)) { return false; }
        Runes++;
        rune.Delete();
        InvalidateProperties();
        return true;
    }
    internal int StorePack(Mobile from)
    {
        if (!CanUse(from)) { return 0; }
        var count = 0;
        // Snapshot before deleting deposited runes from nested containers.
        var runes = new List<Item>();
        foreach (var rune in from.Backpack.FindItemsByType<RecallRune>()) { runes.Add(rune); }
        foreach (var rune in runes) { if (Store(from, rune)) { count++; } }
        return count;
    }
    internal int Withdraw(Mobile from, int amount)
    {
        if (!CanUse(from) || amount is < 1 or > 100 || Runes < amount) { return 0; }
        var withdrawn = 0;
        for (var i = 0; i < amount; i++)
        {
            var rune = new RecallRune();
            if (!from.Backpack.TryDropItem(from, rune, false)) { rune.Delete(); break; }
            Runes--; withdrawn++;
        }
        InvalidateProperties();
        return withdrawn;
    }
    internal bool MarkRune(Mobile from, Func<bool> finishCast)
    {
        if (!CanUse(from) || Runes <= 0) { from?.SendMessage("Keep a rune pouch with blank runes in your backpack."); return false; }
        if (!SpellHelper.CheckTravel(from, TravelCheckType.Mark, out var failure)) { failure.SendMessageTo(from); return false; }
        if (SpellHelper.CheckMulti(from.Location, from.Map, !Core.AOS)) { from.SendLocalizedMessage(501942); return false; }
        var rune = new RecallRune();
        var delivered = false;
        try
        {
            if (!from.Backpack.CheckHold(from, rune, false))
            { from.SendMessage("Make one free backpack slot for the marked rune."); return false; }
            if (!finishCast() || !CanUse(from) || Runes <= 0) { return false; }
            rune.Mark(from);
            if (!from.Backpack.TryDropItem(from, rune, false)) { return false; }
            Runes--;
            delivered = true;
            InvalidateProperties();
            from.SendMessage("The pouch places your newly marked rune in your backpack.");
            return true;
        }
        finally { if (!delivered) { rune.Delete(); } }
    }

    public override bool OnDragDrop(Mobile from, Item dropped) => Store(from, dropped);
    public override void OnDoubleClick(Mobile from)
    {
        if (CanUse(from)) { Open(from); }
        else { from.SendMessage("Keep the rune pouch in your backpack."); }
    }
    private void Open(Mobile from)
    { from.CloseGump<RunePouchGump>(); from.SendGump(new RunePouchGump(this)); }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Blank recall runes:"}{" "}{Runes:N0}{" / "}{60000:N0}");
        list.Add($"{"Cast Mark on this pouch to receive a marked rune."}");
    }
    public static void Initialize() => CommandSystem.Register("rune", AccessLevel.Player, e =>
    {
        var pouch = e.Mobile.Backpack?.FindItemByType<HavenRunePouch>();
        if (pouch == null) { e.Mobile.SendMessage("Rune pouches are sold at Arcane Supplies."); return; }
        e.Mobile.SendMessage(pouch.Withdraw(e.Mobile, 1) == 1 ? "A blank recall rune is in your backpack." : "No rune withdrawn. Check the pouch balance and backpack space.");
    });
    private sealed class RunePouchGump : Gump
    {
        private readonly HavenRunePouch _pouch;
        internal RunePouchGump(HavenRunePouch pouch) : base(50, 50)
        {
            _pouch = pouch;
            AddBackground(0, 0, 480, 270, 9270);
            AddLabel(25, 22, 1152, "Wayfarer's rune pouch");
            AddLabel(25, 62, 2101, $"Blank runes: {pouch.Runes:N0} / 60,000");
            AddButton(25, 103, 4005, 4007, 1); AddLabel(63, 103, 1152, "Take 1");
            AddButton(175, 103, 4005, 4007, 2); AddLabel(213, 103, 1152, "Take 10");
            AddButton(25, 144, 4005, 4007, 3); AddLabel(63, 144, 1152, "Store blank runes from my pack");
            AddHtml(25, 180, 430, 45, "<BASEFONT COLOR=#FFFFFF>Cast Mark on this pouch to receive a marked rune.<BR>Marked and personalized runes stay in your backpack.</BASEFONT>");
            AddLabel(25, 231, 2101, "[rune withdraws one blank rune");
            AddButton(355, 231, 4017, 4019, 0); AddLabel(393, 231, 1152, "Close");
        }

        public override void OnResponse(NetState state, in RelayInfo info)
        {
            var from = state.Mobile;
            if (!_pouch.CanUse(from) || info.ButtonID == 0) { return; }
            if (info.ButtonID == 3) { from.SendMessage($"Stored {_pouch.StorePack(from)} blank runes."); }
            else if (info.ButtonID is 1 or 2) { from.SendMessage($"Withdrew {_pouch.Withdraw(from, info.ButtonID == 1 ? 1 : 10)} blank recall runes."); }
            _pouch.Open(from);
        }
    }
}
