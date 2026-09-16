using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenBossLair : Item
{
    internal static readonly HashSet<HavenBossLair> Registry = new();
    internal static readonly Point3D CoraHome = new(5457, 1808, 0);
    internal static readonly Point3D CorgulHome = new(6431, 1236, 10);
    internal static readonly Point3D AltarSite = new(2494, 918, 0);
    [SerializableField(0)] private int _kind;
    [SerializableField(1)] private HavenScalis _boss;
    [SerializableField(2)] private DateTime _nextSpawn;
    [SerializableField(3)] private Point3D _arrival;
    [SerializableField(4)] private List<Item> _fixtures = new();
    private Timer _timer;
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenBossLair() : base(1) { Visible = false; Movable = false; Name = "Haven encounter lair"; }
    public static void Initialize()
    {
        CommandSystem.Register("cora", AccessLevel.Player, e => Show(e.Mobile, 0));
        CommandSystem.Register("corgul", AccessLevel.Player, e => Show(e.Mobile, 1));
        Timer.DelayCall(TimeSpan.FromSeconds(30), () =>
        {
            try { Install(0); Install(1); }
            catch (Exception ex) { Server.Logging.LogFactory.GetLogger(typeof(HavenBossLair)).Error(ex, "Boss terrain preflight failed; encounter not installed"); }
        });
    }
    internal static HavenBossLair Find(int kind)
    { foreach (var lair in Registry) { if (!lair.Deleted && lair.Kind == kind) { return lair; } } return null; }
    internal static Point3D? Floor(Map map, Point3D near, int range)
    {
        if (map == null || map == Map.Internal) { return null; }
        for (var r = 0; r <= range; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) { continue; }
                    var x = near.X + dx; var y = near.Y + dy;
                    if (x < 0 || y < 0 || x >= map.Width || y >= map.Height) { continue; }
                    foreach (var z in new[] { near.Z, map.GetAverageZ(x, y), near.Z + 5, near.Z - 5, near.Z + 10 })
                    { var p = new Point3D(x, y, z); if (map.CanSpawnMobile(p)) { return p; } }
                }
            }
        }
        return null;
    }
    internal static HavenBossLair Install(int kind)
    {
        var existing = Find(kind); if (existing != null) { return existing; }
        var map = Map.Trammel; var home = kind == 0 ? CoraHome : CorgulHome;
        var spawn = Floor(map, home, 5) ?? throw new InvalidOperationException("Original boss chamber has no usable floor.");
        var arrival = Floor(map, new Point3D(home.X, home.Y + 23, home.Z), 8) ?? throw new InvalidOperationException("Boss entrance has no usable floor.");
        var altarPoint = kind == 1 ? Floor(map, AltarSite, 3) ?? throw new InvalidOperationException("Covetous altar site blocked.") : default;
        var exitPoint = Floor(map, new Point3D(arrival.X + 1, arrival.Y, arrival.Z), 2) ?? throw new InvalidOperationException("Island exit is blocked.");
        var lair = new HavenBossLair { Kind = kind, Arrival = arrival }; lair.MoveToWorld(spawn, map);
        if (kind == 1)
        {
            var altar = new HavenSoulbinderAltar(); altar.MoveToWorld(altarPoint, map); lair.Fixtures.Add(altar);
            var exit = new HavenSoulbinderExit(); exit.MoveToWorld(exitPoint, map); lair.Fixtures.Add(exit);
        }
        lair.Register(); lair.Pulse(); return lair;
    }
    [AfterDeserialization]
    private void Register()
    { Registry.Add(this); _timer?.Stop(); _timer = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), Pulse); }
    internal void Pulse()
    {
        if (Deleted || Boss is { Deleted: false, Alive: true } || Core.Now < NextSpawn) { return; }
        var point = Floor(Map, Location, 4); if (!point.HasValue) { return; }
        Boss = Kind == 0 ? new HavenCora() : new HavenCorgul(); Boss.Home = point.Value; Boss.RangeHome = Kind == 0 ? 15 : 25;
        Boss.MoveToWorld(point.Value, Map); if (Boss is HavenCorgul corgul) { corgul.Guards(); } this.MarkDirty();
    }
    internal static void Finished(HavenScalis boss)
    {
        foreach (var lair in Registry)
        { if (!lair.Deleted && lair.Boss == boss) { lair.Boss = null; lair.NextSpawn = Core.Now + TimeSpan.FromMinutes(15); lair.MarkDirty(); } }
    }
    internal static void Show(Mobile from, int kind)
    {
        if (from?.Deleted != false) { return; }
        from.CloseGump<HavenBossLairGump>(); from.SendGump(new HavenBossLairGump(kind));
    }
    public override void OnDelete()
    {
        Registry.Remove(this); _timer?.Stop(); _timer = null;
        Boss?.Delete(); Boss = null; foreach (var item in Fixtures) { item?.Delete(); } Fixtures.Clear(); base.OnDelete();
    }
}

public sealed class HavenBossLairGump : Gump
{
    private readonly int _kind;
    public HavenBossLairGump(int kind) : base(65, 65)
    {
        _kind = kind; var lair = HavenBossLair.Find(kind);
        AddBackground(0, 0, 530, 310, 9270); AddLabel(24, 20, 1152, kind == 0 ? "Cora the Sorceress" : "Corgul the Soulbinder");
        AddHtml(24, 58, 480, 145, kind == 0 ? "<BASEFONT COLOR=#FFFFFF>Covetous, final chamber. Cora creates violet mana-draining rifts and blinks toward her attackers. Move out of the marked rifts.<BR><BR>Earn gold, a treasure map and a chance at five evolving Covetous artifacts. This encounter does not install the separate endless Void Pool event.</BASEFONT>" : "<BASEFONT COLOR=#FFFFFF>The original Island of the Soulbinder holds Corgul and his guards. Beware his vortex pull.<BR><BR>Haven's altar is beside the Covetous entrance. Offer one treasure map and one world map for a three-hour island chart. The blood cost reduces you to 1 HP. Double-click the chart from safety to enter; nearby companions and pets follow.</BASEFONT>");
        AddLabel(24, 212, 2101, lair?.Boss is { Deleted: false, Alive: true } boss ? $"Alive — {boss.Hits:N0}/{boss.HitsMax:N0} HP" : "Recovering; normal respawn is 15 minutes.");
        AddButton(24, 258, 4005, 4007, 1); AddLabel(64, 258, 1152, "Refresh");
        AddButton(180, 258, 4005, 4007, 2); AddLabel(220, 258, 1152, kind == 0 ? "Chamber approach" : "Covetous altar");
        AddButton(437, 258, 4017, 4019, 0); AddLabel(470, 258, 1152, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 1) { HavenBossLair.Show(state.Mobile, _kind); }
        else if (info.ButtonID == 2)
        {
            var lair = HavenBossLair.Find(_kind);
            if (lair == null || !HavenFrontierSupport.Travel(state.Mobile, _kind == 0 ? lair.Arrival : HavenBossLair.AltarSite, Map.Trammel))
            { state.Mobile.SendMessage("Travel is unavailable here or during combat."); }
        }
    }
}

[SerializationGenerator(0)]
public partial class HavenSoulbinderAltar : Item
{
    [Constructible]
    public HavenSoulbinderAltar() : base(13807) { Name = "Soulbinder sacrificial altar"; Hue = 2075; Movable = false; }
    internal bool Offer(Mobile from)
    {
        if (Deleted || from?.Deleted != false || !from.Alive || from.Map != Map || !from.InRange(this, 4) || !from.InLOS(this) ||
            from.Hits < 2 || from.Backpack == null || from.Criminal || from.Spell != null || SpellHelper.CheckCombat(from) || HavenBossLair.Find(1) == null) { return false; }
        var treasure = from.Backpack.FindItemByType<TreasureMap>(); var world = from.Backpack.FindItemByType<WorldMap>();
        if (treasure == null || world == null) { return false; }
        var chart = new HavenSoulboundChart { Expires = Core.Now + TimeSpan.FromHours(3) };
        if (!from.Backpack.TryDropItem(from, chart, false)) { chart.Delete(); return false; }
        treasure.Delete(); world.Delete(); from.Hits = 1;
        from.SendMessage("Your maps are accepted. Recover your health, then use the island chart from your pack."); return true;
    }
    public override void OnDoubleClick(Mobile from)
    { if (from.Map == Map && from.InRange(this, 4)) { from.SendGump(new OfferGump(this)); } }
    private sealed class OfferGump : Gump
    {
        private readonly HavenSoulbinderAltar _altar;
        public OfferGump(HavenSoulbinderAltar altar) : base(80, 80)
        {
            _altar = altar; AddBackground(0, 0, 440, 190, 9270);
            AddHtml(24, 25, 390, 90, "<BASEFONT COLOR=#FFFFFF>Consume one treasure map (any condition) and one world map for a three-hour island chart.<BR><BR>The ritual reduces your health to 1 HP.</BASEFONT>");
            AddButton(24, 138, 4005, 4007, 1); AddLabel(64, 138, 1152, "Offer maps and blood"); AddButton(340, 138, 4017, 4019, 0);
        }
        public override void OnResponse(NetState state, in RelayInfo info)
        { if (info.ButtonID == 1 && !_altar.Offer(state.Mobile)) { state.Mobile.SendMessage("Check your maps, backpack space, health and distance. Nothing was consumed."); } }
    }
}

[SerializationGenerator(0)]
public partial class HavenSoulboundChart : Item
{
    [SerializableField(0)] private DateTime _expires;
    [Constructible]
    public HavenSoulboundChart() : base(0x14EC) { Name = "chart of the Island of the Soulbinder"; Weight = 1; LootType = LootType.Blessed; }
    internal bool Enter(Mobile from)
    {
        var lair = HavenBossLair.Find(1);
        return !Deleted && from?.Deleted == false && Core.Now < Expires && from.Backpack != null && IsChildOf(from.Backpack) && lair != null &&
            HavenFrontierSupport.Travel(from, lair.Arrival, Map.Trammel);
    }
    public override void OnDoubleClick(Mobile from)
    { if (!Enter(from)) { from.SendMessage("Keep an unexpired chart in your pack and use it out of combat."); } }
    public override void GetProperties(IPropertyList list)
    { base.GetProperties(list); list.Add($"{"Expires UTC:"} {Expires:yyyy-MM-dd HH:mm}"); list.Add($"{"Double-click from safety to enter the island"}"); }
}

[SerializationGenerator(0)]
public partial class HavenSoulbinderExit : Item
{
    [Constructible]
    public HavenSoulbinderExit() : base(0xF6C) { Name = "return to Covetous"; Movable = false; Hue = 0x48E; }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Map != Map || !from.InRange(this, 3)) { return; }
        var p = HavenBossLair.Floor(Map.Trammel, HavenBossLair.AltarSite, 3);
        if (p.HasValue) { HavenFrontierSupport.Return(from, p.Value, Map.Trammel); }
    }
}
