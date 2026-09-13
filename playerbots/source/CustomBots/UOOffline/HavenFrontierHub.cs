using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Logging;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenFrontierHub : Item
{
    internal static readonly HashSet<HavenFrontierHub> Registry = new();
    [SerializableField(0)] private List<Item> _owned = new();
    [Constructible]
    public HavenFrontierHub() : base(0x1E5E) { Name = "Frontier voyages — Shadowguard, Blackthorn and Chelonia"; Movable = false; }
    [AfterDeserialization] private void Register() { Registry.Add(this); }
    internal static bool TerrainReady()
    {
        foreach (var point in new[] { HavenFrontierSupport.ShadowLanding, HavenFrontierSupport.RiftLanding, HavenChelonia.Site, HavenChelonia.Landing })
        { if (!Map.Trammel.CanFit(point, 16, checkMobiles: false)) { return false; } }
        return true;
    }
    internal static bool WildAquatic(Mobile mobile) => mobile is BaseCreature creature &&
        creature is WaterElemental or SeaSerpent or DeepSeaSerpent or Dolphin or Kraken &&
        !creature.Controlled && !creature.Summoned && creature.ControlMaster == null && creature.Owners.Count == 0;
    private static bool Water(Point3D point)
    {
        var tile = Map.Trammel.Tiles.GetLandTile(point.X, point.Y);
        return tile.Z == -5 && tile.ID is 0xA8 or 0xA9 or 0xAA or 0xAB or 0x136 or 0x137;
    }
    private static bool Refuge(Point3D center, HashSet<Point3D> reserved, out Point3D destination)
    {
        for (var radius = 92; radius <= 132; radius += 4)
        {
            for (var step = 0; step < 32; step++)
            {
                var angle = step * Math.PI * 2 / 32;
                var point = new Point3D(center.X + (int)(Math.Cos(angle) * radius), center.Y + (int)(Math.Sin(angle) * radius), -5);
                if (!reserved.Contains(point) && Water(point) && Map.Trammel.CanFit(point, 16, checkMobiles: true, requireSurface: false))
                { reserved.Add(point); destination = point; return true; }
            }
        }
        destination = default; return false;
    }
    internal static string PreflightReason()
    {
        if (Registry.Count == 1) { return "Already installed"; }
        foreach (var point in new[] { HavenFrontierSupport.ShadowLanding, HavenFrontierSupport.RiftLanding, HavenChelonia.Site, HavenChelonia.Landing })
        { if (!Map.Trammel.CanFit(point, 16, checkMobiles: false)) { return $"Landing blocked at {point}; land {Map.Trammel.Tiles.GetLandTile(point.X, point.Y).ID}"; } }
        foreach (var origin in new[] { new Point2D(4672, 3104), new Point2D(4800, 3328), new Point2D(3904, 3488) })
        {
            var center = new Point3D(origin.X + 88, origin.Y + 88, 0);
            foreach (var item in Map.Trammel.GetItemsInRange<Item>(center, 88))
            { if (!item.Deleted && item.Parent == null) { return $"Item {item.GetType().Name} {item.Serial} at {item.Location}"; } }
            foreach (var mobile in Map.Trammel.GetMobilesInRange<Mobile>(center, 88))
            { if (!mobile.Deleted && !WildAquatic(mobile)) { return $"Mobile {mobile.GetType().Name} {mobile.Serial} at {mobile.Location}"; } }
        }
        return null;
    }
    internal static bool Install(bool checkDynamic = true)
    {
        if (Registry.Count == 1) { return HavenShadowChamber.Registry.Count == 6 && HavenFrontierBattle.Registry.Count == 2 && HavenChelonia.Registry.Count == 1; }
        if (Registry.Count != 0 || HavenShadowChamber.Registry.Count != 0 || HavenFrontierBattle.Registry.Count != 0 || HavenChelonia.Registry.Count != 0 || !TerrainReady()) { return false; }
        var relocations = new List<(BaseCreature Creature, Point3D Destination)>();
        if (checkDynamic)
        {
            var reserved = new HashSet<Point3D>();
            foreach (var origin in new[] { new Point2D(4672, 3104), new Point2D(4800, 3328), new Point2D(3904, 3488) })
            {
                var center = new Point3D(origin.X + 88, origin.Y + 88, 0);
                foreach (var item in Map.Trammel.GetItemsInRange<Item>(center, 88)) { if (!item.Deleted && item.Parent == null) { return false; } }
                foreach (var mobile in Map.Trammel.GetMobilesInRange<Mobile>(center, 88))
                {
                    if (mobile.Deleted) { continue; }
                    if (!WildAquatic(mobile)) { return false; }
                    if (Water(mobile.Location)) { continue; }
                    if (!Refuge(center, reserved, out var destination)) { return false; }
                    relocations.Add(((BaseCreature)mobile, destination));
                }
            }
        }
        var hub = new HavenFrontierHub();
        try
        {
            for (var i = 0; i < 6; i++)
            {
                var chamber = new HavenShadowChamber(); hub.Owned.Add(chamber);
                chamber.MoveToWorld(new Point3D(4722 + i % 3 * 38, 3169 + i / 3 * 40, 0), Map.Trammel); chamber.Build((HavenShadowRoom)i);
            }
            var chelonia = new HavenChelonia(); hub.Owned.Add(chelonia); chelonia.MoveToWorld(HavenChelonia.Site, Map.Trammel); chelonia.Build();
            var blackthorn = new HavenFrontierBattle(); hub.Owned.Add(blackthorn);
            blackthorn.MoveToWorld(HavenFrontierSupport.RiftLanding, Map.Trammel); blackthorn.Build(false);
            var pirates = new HavenFrontierBattle(); hub.Owned.Add(pirates);
            pirates.MoveToWorld(new Point3D(HavenChelonia.Landing.X + 5, HavenChelonia.Landing.Y, 0), Map.Trammel); pirates.Build(true);
            var entry = new HavenShadowEntrance(); hub.Owned.Add(entry); entry.MoveToWorld(HavenFrontierSupport.ShadowLanding, Map.Trammel);
            foreach (var point in new[] { HavenFrontierSupport.ShadowLanding, HavenFrontierSupport.RiftLanding })
            {
                var gate = new HavenCommonsReturnGate(); hub.Owned.Add(gate); gate.MoveToWorld(new Point3D(point.X + 3, point.Y, point.Z), Map.Trammel);
                var ankh = new AnkhWest(); hub.Owned.Add(ankh); ankh.MoveToWorld(new Point3D(point.X - 3, point.Y, point.Z), Map.Trammel);
            }
            hub.MoveToWorld(new Point3D(3988, 2893, 1), Map.Trammel); hub.Register(); hub.MarkDirty();
            foreach (var relocation in relocations)
            { relocation.Creature.Home = relocation.Destination; relocation.Creature.MoveToWorld(relocation.Destination, Map.Trammel); }
            LogFactory.GetLogger(typeof(HavenFrontierHub)).Information("Frontier setup complete: {Count} unowned aquatic creatures moved to open water.", relocations.Count);
            return true;
        }
        catch { hub.Delete(); throw; }
    }
    public static void Initialize()
    {
        CommandSystem.Register("HavenFrontiersSetup", AccessLevel.Administrator, e =>
        { e.Mobile.SendMessage(Install() ? "Frontier islands and encounters are installed; existing installations are preserved." : "Setup refused: matching terrain is missing, an area is occupied, or installation is partial."); });
        CommandSystem.Register("frontiers", AccessLevel.Player, e => Open(e.Mobile));
        CommandSystem.Register("shadowguard", AccessLevel.Player, e =>
        {
            foreach (var chamber in HavenShadowChamber.Registry)
            { if (chamber.Participant(e.Mobile)) { e.Mobile.SendGump(new HavenShadowMenu(e.Mobile, chamber)); return; } }
            if (Registry.Count == 1 && HavenFrontierSupport.Travel(e.Mobile, HavenFrontierSupport.ShadowLanding)) { e.Mobile.SendGump(new HavenShadowMenu(e.Mobile)); }
        });
        CommandSystem.Register("blackthorn", AccessLevel.Player, e => { if (Registry.Count == 1) { HavenFrontierSupport.Travel(e.Mobile, HavenFrontierSupport.RiftLanding); } });
        CommandSystem.Register("chelonia", AccessLevel.Player, e => { if (Registry.Count == 1) { HavenFrontierSupport.Travel(e.Mobile, HavenChelonia.Landing); } });
        CommandSystem.Register("voyage", AccessLevel.Player, e =>
        {
            foreach (var battle in HavenFrontierBattle.Registry)
            {
                if (!battle.Pirate) { continue; }
                if (e.ArgString.Equals("exit", StringComparison.OrdinalIgnoreCase) && battle.Nearby(e.Mobile)) { HavenFrontierSupport.Return(e.Mobile, HavenChelonia.Landing); }
                else { HavenFrontierSupport.Travel(e.Mobile, HavenChelonia.Landing); }
                return;
            }
        });
    }
    internal static void Open(Mobile from)
    {
        if (Registry.Count != 1) { from.SendMessage("The frontier expeditions are not installed yet."); return; }
        from.CloseGump<HavenFrontierTravel>(); from.SendGump(new HavenFrontierTravel());
    }
    public override void OnDoubleClick(Mobile from) { if (from.Map == Map && from.InRange(this, 4)) { Open(from); } }
    public override void OnDelete()
    { Registry.Remove(this); foreach (var item in Owned) { item?.Delete(); } Owned.Clear(); base.OnDelete(); }
}

[SerializationGenerator(0)]
public partial class HavenShadowEntrance : Item
{
    [Constructible] public HavenShadowEntrance() : base(0xE2D) { Name = "Shadowguard — enchanting crystal"; Movable = false; }
    public override void OnDoubleClick(Mobile from)
    { if (from.Map == Map && from.InRange(this, 4)) { from.CloseGump<HavenShadowMenu>(); from.SendGump(new HavenShadowMenu(from)); } }
}

public sealed class HavenFrontierTravel : Gump
{
    public HavenFrontierTravel() : base(70, 70)
    {
        AddBackground(0, 0, 475, 325, 9270); AddLabel(25, 22, 1152, "Frontier voyages");
        var labels = new[] { "Shadowguard — rooms and Roof", "Blackthorn — captains and rift beacons", "Chelonia — tide tortoises and corsair voyages", "Expedition journal and relic rewards" };
        for (var i = 0; i < labels.Length; i++) { AddButton(25, 70 + i * 48, 4005, 4007, i + 1); AddLabel(66, 70 + i * 48, 2101, labels[i]); }
        AddButton(354, 276, 4017, 4019, 0); AddLabel(393, 276, 1152, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (HavenFrontierHub.Registry.Count != 1 || info.ButtonID == 0) { return; }
        if (info.ButtonID == 4) { state.Mobile.SendGump(new HavenFrontierJournal(state.Mobile)); return; }
        var point = info.ButtonID switch { 1 => HavenFrontierSupport.ShadowLanding, 2 => HavenFrontierSupport.RiftLanding, _ => HavenChelonia.Landing };
        if (info.ButtonID is < 1 or > 3 || !HavenFrontierSupport.Travel(state.Mobile, point)) { state.Mobile.SendMessage("Travel is restricted or the landing is blocked."); }
    }
}
