using System.Collections.Generic;
using System.Linq;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Network;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenHomePatrolBoard : Item
{
    [Constructible]
    public HavenHomePatrolBoard() : base(0x1E5E)
    { Name = "R.E.C. Corsair patrol board"; Movable = false; }
    internal bool CanUse(Mobile from) => !Deleted && Parent == null && from?.Deleted == false && from.Alive &&
        from.Map == Map && from.InRange(this, 4);
    internal static HavenFrontierBattle Patrol
        => HavenFrontierBattle.Registry.FirstOrDefault(b => !b.Deleted && b.Pirate && b.Map == Map.Trammel);
    internal static bool Nearby(Mobile from)
    {
        if (from?.Map == null || from.Map == Map.Internal) { return false; }
        foreach (var board in from.Map.GetItemsInRange<HavenHomePatrolBoard>(from.Location, 4))
        { if (board.CanUse(from)) { return true; } }
        return false;
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (!CanUse(from)) { from.SendMessage("Come closer to the patrol board."); return; }
        from.CloseGump<HavenHomePatrolMenu>(); from.SendGump(new HavenHomePatrolMenu(this));
    }
}

public sealed class HavenHomePatrolMenu : Gump
{
    private readonly HavenHomePatrolBoard _board;
    public HavenHomePatrolMenu(HavenHomePatrolBoard board) : base(80, 80)
    {
        _board = board;
        AddBackground(0, 0, 440, 280, 9200);
        AddLabel(24, 20, 0, "R.E.C. CORSAIR PATROLS");
        AddLabel(24, 49, 0, "Saltfang voyages, cargo turn-ins and relics");
        var labels = new[] { "Begin patrol / join the current voyage", "Turn in all maritime cargo in your pack", "Expedition journal and relic rewards" };
        for (var i = 0; i < labels.Length; i++)
        { AddButton(24, 91 + i * 42, 4005, 4007, i + 1); AddLabel(63, 92 + i * 42, 0, labels[i]); }
        AddLabel(24, 219, 0, "[voyage exit returns to Chelonia; [home returns here.");
        AddButton(328, 246, 4017, 4019, 0); AddLabel(367, 246, 0, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;
        if (info.ButtonID == 0 || !_board.CanUse(from)) { return; }
        if (info.ButtonID == 1)
        {
            var patrol = HavenHomePatrolBoard.Patrol;
            if (patrol == null || !(patrol.Active ? patrol.Board(from, _board) : patrol.Start(from, _board)))
            { from.SendMessage("The patrol is unavailable, recovering, blocked, or you are in combat. Try again shortly."); }
            else { return; }
        }
        else if (info.ButtonID == 2)
        {
            var cargo = new List<HavenMaritimeCargo>();
            if (from.Backpack != null) { foreach (var item in from.Backpack.FindItemsByType<HavenMaritimeCargo>()) { cargo.Add(item); } }
            if (cargo.Count == 0) { from.SendMessage("You have no maritime cargo in your pack."); }
            else { foreach (var item in cargo) { item.OnDoubleClick(from); } }
        }
        else if (info.ButtonID == 3)
        { from.CloseGump<HavenFrontierJournal>(); from.SendGump(new HavenFrontierJournal(from)); return; }
        from.SendGump(new HavenHomePatrolMenu(_board));
    }
}

public partial class HavenPirateEstate
{
    internal HavenHomePatrolBoard HomePatrol => Fixtures.OfType<HavenHomePatrolBoard>().FirstOrDefault(b => !b.Deleted);
    internal void EnsureHomePatrol()
    {
        if (Deleted || Map != Map.Trammel || Fixtures.Count == 0 || HomePatrol != null) { return; }
        // Preflight the whole shelter before adding anything; leave player property alone.
        for (var x = 75; x <= 77; x++)
        for (var y = 123; y <= 126; y++)
        { if (!ClearSettlementSite(x, y)) { return; } }
        var placed = new List<Item>();
        void Add(Item item, int x, int y, int z = 0)
        { item.Movable = false; placed.Add(item); item.MoveToWorld(new Point3D(X + x, Y + y, z), Map); }
        try
        {
            for (var y = 123; y <= 125; y++)
            {
                Add(new Static(0x7) { Name = "Weathered dispatch shelter" }, 75, y);
                Add(new Static(0x4A9) { Name = "Dispatch shelter awning" }, 75, y, 20);
            }
            Add(new Static(0x9) { Name = "Timber awning post" }, 76, 123);
            Add(new Static(0x9) { Name = "Timber awning post" }, 76, 125);
            Add(new Static(0x4A9) { Name = "Dispatch shelter awning" }, 76, 123, 20);
            Add(new Static(0x4A9) { Name = "Dispatch shelter awning" }, 76, 124, 20);
            Add(new Static(0x4A9) { Name = "Dispatch shelter awning" }, 76, 125, 20);
            Add(new Static(0xE77) { Name = "Patrol supplies" }, 76, 126);
            Add(new Static(0xA25) { Name = "The watchkeeper's lantern", Light = LightType.Circle225 }, 76, 126, 10);
            Add(new Static(0x9) { Name = "Patrol noticeboard mounting post" }, 77, 124);
            Add(new HavenHomePatrolBoard(), 77, 124, 4);
            Fixtures.AddRange(placed); this.MarkDirty();
        }
        catch { foreach (var item in placed) { item.Delete(); } throw; }
    }
}
