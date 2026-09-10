using System;
using System.Collections.Generic;
using System.IO;
using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

// A public building with owned fixtures, so removal never sweeps unrelated world objects.
[SerializationGenerator(0)]
public partial class HavenCommunityCenter : Item
{
    public const int Width = 33;
    public const int Depth = 41;
    [SerializableField(0)] private List<Item> _fixtures = new();
    [SerializableField(1)] private List<Mobile> _residents = new();
    private Server.Regions.NoHousingRegion _region;
    internal static readonly HashSet<HavenCommunityCenter> Registry = new();
    private static int FloorHeight => TileData.ItemTable[0x519].CalcHeight;
    public Point3D Arrival => new(X + 16, Y + 37, Z + FloorHeight);
    [Constructible]
    public HavenCommunityCenter() : base(0xBD2) { Name = "Haven Commons"; Movable = false; }
    [AfterDeserialization] private void Register()
    {
        Registry.Add(this); _region?.Unregister();
        if (Map != null && Map != Map.Internal)
        {
            _region = new Server.Regions.NoHousingRegion("Haven Commons", Map, 90, new Rectangle3D(X - 1, Y - 1, -128, Width + 2, Depth + 2, 256));
            _region.Register();
        }
    }

    internal static bool CanBuild(Point3D origin, Map map)
    {
        if (map == null || map == Map.Internal) { return false; }
        for (var x = -1; x <= Width; x++)
        for (var y = -1; y <= Depth; y++)
        {
            var p = new Point3D(origin.X + x, origin.Y + y, origin.Z);
            if (!map.CanFit(p.X, p.Y, p.Z, 20, true, true) ||
                Server.Multis.BaseHouse.FindHouseAt(p, map, 20) != null) { return false; }
        }
        return true;
    }

    internal void Build()
    {
        if (Fixtures.Count != 0) { return; }
        Register();
        // Stone courtyard, columned perimeter, broad central aisle, four open entrances.
        for (var x = 0; x < Width; x++)
        for (var y = 0; y < Depth; y++)
        {
            Place(new Static(0x519), x, y, 0);
            if ((x == 0 || x == Width - 1) && Math.Abs(y - Depth / 2) > 2 ||
                (y == 0 || y == Depth - 1) && Math.Abs(x - Width / 2) > 2)
            { Place(new Static(0x21), x, y, 1); }
        }
        foreach (var x in new[] { 2, 10, 22, 30 })
        foreach (var y in new[] { 2, 38 })
        { Place(new Static(0xDB), x, y, 1); Place(new Static(0xB20) { Light = LightType.Circle300 }, x, y + 1, 1); }
        var trades = Enum.GetValues<HavenMarketTrade>();
        for (var i = 0; i < trades.Length; i++)
        {
            var stall = new HavenMarketStall(); Place(stall, 4 + i % 3 * 7, 4 + i / 3 * 5, 1); stall.Setup(trades[i]);
        }
        Place(new HavenTravelLibrary(), 27, 5, 1);
        Place(new HavenMarketDirectoryBoard(), 16, 35, 1);
        Place(new HavenFellowshipBoard(), 23, 27, 1);
        Place(new UOOfflineDungeonPortal(), 27, 8, 1);
        Place(new HavenRepairBench(), 27, 12, 1);
        Place(new AnvilEastAddon(), 27, 16, 1);
        Place(new SmallForgeAddon(), 29, 16, 1);
        Place(new SpinningWheelEastAddon(), 26, 20, 1);
        Place(new LoomEastAddon(), 29, 20, 1);
        Place(new HavenTrainingStone(), 27, 24, 1);
        for (var i = 0; i < 5; i++)
        { Place(new HavenPracticeChest { Difficulty = i * 20, Name = $"Practice locks and traps — {i * 20} to {i * 20 + 30} skill" }, 4 + i * 4, 31, 1); }
        Place(new Static(0xA97) { Name = "Training hall: use lockpicks; double-click to reset. Remove Trap requires 50 Lockpicking and Detect Hidden." }, 3, 29, 1);
        AddResident(new HavenTrainingSentinel(), 27, 30);
        AddResident(new Banker { Name = "Clara Whitfield" }, 8, 36);
        AddResident(new Provisioner { Name = "Owen Fairchild" }, 24, 36);
        AddResident(new HavenBankHealer { Name = "Elise Rowan" }, 12, 36);
        AddResident(new HavenPetHealer { Name = "Mara Finch" }, 20, 36);
        Place(new HavenCommonsReturnGate(), 16, 39, 1);
        foreach (var x in new[] { 4, 10, 22, 28 })
        { Place(new Static(0x11CA), x, 34, 1); Place(new WoodenBench(), x, 28, 1); }
        if (Location == HavenPirateEstate.CommonsSite && Map == Map.Trammel)
        {
            for (var x = 23; x <= 27; x++)
            for (var y = 78; y <= 106; y++) { Place(new Static(0x7CD), x, y, 0); }
            Place(new Static(0xB20) { Light = LightType.Circle300 }, 22, 80, 0);
        }
        this.MarkDirty();
    }
    private void Place(Item item, int x, int y, int z)
    { item.Movable = false; item.MoveToWorld(new Point3D(X + x, Y + y, Z + (z == 1 ? FloorHeight : z)), Map); Fixtures.Add(item); }
    private void AddResident(BaseCreature npc, int x, int y)
    { npc.CantWalk = true; npc.MoveToWorld(new Point3D(X + x, Y + y, Z + FloorHeight), Map); Residents.Add(npc); }

    internal static bool Travel(Mobile from)
    {
        if (from?.Deleted != false || from.Spell != null) { return false; }
        foreach (var center in Registry)
        {
            if (center.Deleted || center.Map == Map.Internal || center.Fixtures.Count == 0) { continue; }
            var p = center.Arrival;
            for (var dx = -2; dx <= 2; dx++)
            for (var dy = -2; dy <= 2; dy++)
            {
                var target = new Point3D(p.X + dx, p.Y + dy, p.Z);
                if (!center.Map.CanFit(target.X, target.Y, target.Z, 16, false, true)) { continue; }
                BaseCreature.TeleportPets(from, target, center.Map); from.MoveToWorld(target, center.Map); from.PlaySound(0x1FE); return true;
            }
        }
        from.SendMessage("Haven Commons is not available yet, or its arrival area is blocked."); return false;
    }
    public override void OnDelete()
    {
        Registry.Remove(this);
        _region?.Unregister(); _region = null;
        foreach (var item in Fixtures) { item?.Delete(); }
        foreach (var npc in Residents) { npc?.Delete(); }
        Fixtures.Clear(); Residents.Clear(); base.OnDelete();
    }
    public static void Initialize()
    {
        CommandSystem.Register("HavenCenterBuild", AccessLevel.GameMaster, e =>
        {
            if (Registry.Count != 0) { e.Mobile.SendMessage("Haven Commons already exists. Its saved building is preserved."); return; }
            var origin = new Point3D(e.Mobile.X - 16, e.Mobile.Y - 42, e.Mobile.Z);
            if (!CanBuild(origin, e.Mobile.Map)) { e.Mobile.SendMessage("The Commons needs a clear, level 35 by 43 area north of you, outside houses. Nothing was changed."); return; }
            var center = new HavenCommunityCenter(); center.MoveToWorld(origin, e.Mobile.Map);
            try { center.Build(); }
            catch { center.Delete(); throw; }
            e.Mobile.SendMessage("Haven Commons is built. Bank dungeon portals now offer travel here.");
        });
    }
}

[SerializationGenerator(0)]
public partial class HavenCommonsReturnGate : Item
{
    [Constructible] public HavenCommonsReturnGate() : base(0xF6C) { Name = "New Haven bank"; Movable = false; Hue = 0x482; }
    public override void OnDoubleClick(Mobile from)
    { if (from.Map == Map && from.InRange(this, 2) && from.Spell == null) { HavenRecovery.GoToBank(from); } }
}

[SerializationGenerator(0)]
public partial class HavenPracticeChest : LockableContainer
{
    [SerializableField(0)] private int _difficulty;
    [Constructible] public HavenPracticeChest() : base(0xE40) { Movable = false; Name = "Practice locks and traps"; Reset(); }
    internal void Reset()
    {
        RequiredSkill = Difficulty; LockLevel = Difficulty == 0 ? -1 : Difficulty;
        MaxLockLevel = Difficulty + 30; Locked = true;
        TrapType = TrapType.DartTrap; TrapPower = Difficulty; TrapLevel = 0;
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Map != Map || !from.InRange(this, 2)) { return; }
        Reset(); from.SendMessage("Practice lock and harmless trap reset. Use lockpicks or the Remove Trap skill on this chest.");
    }
    public override bool ExecuteTrap(Mobile from) { from.SendMessage("The practice trap clicks harmlessly."); return false; }
    public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage) => false;
    public override bool OnDragDrop(Mobile from, Item dropped) => false;
}

[SerializationGenerator(0)]
public partial class HavenTravelLibrary : Item
{
    [Constructible] public HavenTravelLibrary() : base(0xA97) { Name = "Haven rune library — towns and treasure sites"; Movable = false; }
    public override void OnDoubleClick(Mobile from)
    { if (from.Map == Map && from.InRange(this, 3)) { from.SendGump(new HavenListGump(this, new LibraryMenu(this), reopen: false)); } }
    internal static List<(string Name, Point3D Location, Map Map)> Destinations()
    {
        var entries = new List<(string, Point3D, Map)>
        {
            ("New Haven bank", HavenRecovery.BankLocation, Map.Trammel),
            ("Britain bank", new Point3D(1438, 1690, 0), Map.Trammel),
            ("Luna", new Point3D(989, 520, -50), Map.Malas),
            ("Umbra", new Point3D(1997, 1386, -85), Map.Malas)
        };
        var path = Path.Combine(Core.BaseDirectory, "Data", "treasure.cfg");
        if (!File.Exists(path)) { return entries; }
        var number = 0;
        foreach (var line in File.ReadLines(path))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !int.TryParse(parts[0], out var x) || !int.TryParse(parts[1], out var y) || x < 0 || y < 0 || x >= 5120 || y >= 4096) { continue; }
            number++;
            entries.Add(($"Treasure {number:D3} — {x}, {y} [Trammel]", new Point3D(x, y, 0), Map.Trammel));
            entries.Add(($"Treasure {number:D3} — {x}, {y} [Felucca]", new Point3D(x, y, 0), Map.Felucca));
        }
        return entries;
    }
    private sealed class LibraryMenu : ItemListMenu
    {
        private readonly HavenTravelLibrary _book;
        private readonly List<(string Name, Point3D Location, Map Map)> _places;
        private static ItemListEntry[] EntriesFor(List<(string Name, Point3D Location, Map Map)> places)
            => places.ConvertAll(p => new ItemListEntry(p.Name, 0x1F14)).ToArray();
        public LibraryMenu(HavenTravelLibrary book) : this(book, Destinations()) { }
        private LibraryMenu(HavenTravelLibrary book, List<(string Name, Point3D Location, Map Map)> places)
            : base("Rune library — towns and treasure coordinates", EntriesFor(places)) { _book = book; _places = places; }
        public override void OnResponse(NetState state, int index)
        {
            var from = state.Mobile;
            if (_book.Deleted || from.Map != _book.Map || !from.InRange(_book, 3) || !from.Alive || from.Spell != null || index < 0 || index >= _places.Count) { return; }
            var place = _places[index];
            for (var radius = 1; radius <= 6; radius++)
            for (var dx = -radius; dx <= radius; dx++)
            for (var dy = -radius; dy <= radius; dy++)
            {
                if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) { continue; }
                var p = new Point3D(place.Location.X + dx, place.Location.Y + dy, place.Map.GetAverageZ(place.Location.X + dx, place.Location.Y + dy));
                if (!place.Map.CanSpawnMobile(p) || Server.Multis.BaseHouse.FindHouseAt(p, place.Map, 16) != null) { continue; }
                BaseCreature.TeleportPets(from, p, place.Map); from.MoveToWorld(p, place.Map); from.PlaySound(0x1FE); return;
            }
            from.SendMessage("That landing is obstructed; choose another destination.");
        }
    }
}
