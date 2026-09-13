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
public partial class HavenScalisHunt : Item
{
    internal static readonly HashSet<HavenScalisHunt> Registry = new();
    internal static readonly Point3D RoamingWaters = new(4000, 3670, -5);
    [SerializableField(0)] private HavenScalis _boss;
    [SerializableField(1)] private DateTime _nextSpawn;
    private Timer _timer;
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenScalisHunt() : base(1) { Visible = false; Movable = false; Name = "Scalis ocean hunt"; }
    public static void Initialize()
    {
        CommandSystem.Register("scalis", AccessLevel.Player, e => Show(e.Mobile));
        Timer.DelayCall(TimeSpan.FromSeconds(30), () => { Ensure(Map.Trammel).Pulse(); });
    }
    internal static HavenScalisHunt Find(Map map)
    {
        foreach (var hunt in Registry) { if (!hunt.Deleted && hunt.Map == map) { return hunt; } }
        return null;
    }
    internal static HavenScalisHunt Ensure(Map map)
    {
        if (map != Map.Trammel && map != Map.Felucca) { return null; }
        var existing = Find(map); if (existing != null) { return existing; }
        var hunt = new HavenScalisHunt(); hunt.MoveToWorld(RoamingWaters, map); hunt.Register(); return hunt;
    }
    [AfterDeserialization]
    private void Register()
    {
        Registry.Add(this); _timer?.Stop();
        _timer = Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), Pulse);
    }
    internal static bool SeaPoint(Map map, Point3D p)
    {
        if (map == null || map == Map.Internal || p.X < 1 || p.Y < 1 || p.X >= map.Width - 1 || p.Y >= map.Height - 1) { return false; }
        var land = map.Tiles.GetLandTile(p.X, p.Y);
        return (land.ID is >= 0xA8 and <= 0xAB or >= 0x136 and <= 0x137) && Math.Abs(land.Z - p.Z) < 2 &&
            !SpellHelper.CheckMulti(p, map) && map.CanFit(p, 16, checkMobiles: true, requireSurface: false);
    }
    internal bool Spawn(Point3D near, Mobile fisher = null)
    {
        if (Deleted || Boss?.Deleted == false && Boss.Alive) { return false; }
        for (var i = 0; i < 120; i++)
        {
            var point = i == 0 ? near : new Point3D(near.X + Utility.RandomMinMax(-12, 12), near.Y + Utility.RandomMinMax(-12, 12), near.Z);
            if (!SeaPoint(Map, point)) { continue; }
            var boss = new HavenScalis { Hunt = this, Home = point, RangeHome = 40 };
            Boss = boss; boss.MoveToWorld(point, Map);
            if (fisher?.Deleted == false && fisher.Alive && fisher.Map == Map && fisher.InRange(boss, 18)) { boss.Combatant = fisher; }
            this.MarkDirty(); return true;
        }
        return false;
    }
    internal void Pulse()
    {
        if (Deleted || Map != Map.Trammel && Map != Map.Felucca || Boss?.Deleted == false && Boss.Alive || Core.Now < NextSpawn) { return; }
        if (!Spawn(RoamingWaters)) { NextSpawn = Core.Now + TimeSpan.FromMinutes(1); }
    }
    internal void Finished(HavenScalis boss)
    {
        if (Boss != boss) { return; }
        Boss = null; NextSpawn = Core.Now + TimeSpan.FromMinutes(15); this.MarkDirty();
    }
    internal static bool CheckNet(Mobile from)
    {
        var hunt = Find(from.Map);
        if (hunt?.Boss is not { Deleted: false, Alive: true } boss) { return true; }
        from.SendMessage($"Scalis is already roaming {boss.Map.Name} at {boss.X}, {boss.Y}. Your net has not been used. Type [scalis for details.");
        return false;
    }
    internal static bool NetResult(FabledFishingNet net, Point3D point, Map map, Mobile from, double roll)
    {
        if (from?.Deleted != false || map != Map.Trammel && map != Map.Felucca || net?.Deleted != false) { return false; }
        var hunt = Ensure(map);
        // Two nets may finish on the same tick. Refund the second rather than duplicate the boss.
        if (hunt.Boss is { Deleted: false, Alive: true })
        {
            Refund(from, net); CheckNet(from); return true;
        }
        if (from.Skills.Fishing.Base < 100 || roll >= .25) { return false; }
        if (!hunt.Spawn(point, from)) { Refund(from, net); from.SendMessage("The sea is obstructed. Your white net was returned."); return true; }
        from.SendMessage("Osiredon the Scalis Enforcer rises from the depths!"); net.Delete(); return true;
    }
    private static void Refund(Mobile from, FabledFishingNet net)
    {
        if (from?.Deleted == false)
        {
            if (from.Backpack == null) { from.AddItem(new Backpack()); }
            from.Backpack.DropItem(new FabledFishingNet());
        }
        net.Delete();
    }
    internal static void Show(Mobile from)
    {
        if (from?.Deleted != false) { return; }
        from.CloseGump<HavenScalisGump>(); from.SendGump(new HavenScalisGump(from));
    }
    public override void OnDelete()
    {
        _timer?.Stop(); _timer = null; Registry.Remove(this);
        var boss = Boss; Boss = null;
        if (boss?.Deleted == false) { boss.Hunt = null; boss.Delete(); }
        base.OnDelete();
    }
}

public sealed class HavenScalisGump : Gump
{
    public HavenScalisGump(Mobile from) : base(60, 60)
    {
        AddBackground(0, 0, 550, 370, 9270); AddLabel(24, 18, 1152, "Scalis — hunter of the deep");
        var map = from.Map == Map.Felucca ? Map.Felucca : Map.Trammel;
        var hunt = HavenScalisHunt.Find(map);
        var boss = hunt?.Boss;
        AddLabel(24, 52, 2101, boss is { Deleted: false, Alive: true } ? $"{map.Name}: {boss.X}, {boss.Y} — HP {boss.Hits:N0}/{boss.HitsMax:N0}" : $"{map.Name}: no Scalis is currently alive");
        AddHtml(24, 87, 500, 190, "<BASEFONT COLOR=#FFFFFF>Roams the waters south of Chelonia's long pier. Return here for his current coordinates.<BR><BR>White Fabled Fishing Nets: 25% summon chance with 100 Fishing when no Scalis is alive on the facet. Otherwise the net reports his location without being used. White nets also come from ancient SOS chests; Arcane Supplies sells them for 25,000 gold.<BR><BR>Each qualifying contributor earns 40,000 gold and fishing supplies, with independent chances of 25% for an evolving artifact and 5% for a small soul forge. [guide lists rewards and mechanics.</BASEFONT>");
        AddButton(24, 302, 4005, 4007, 1); AddLabel(64, 302, 1152, "Refresh");
        AddButton(160, 302, 4005, 4007, 2); AddLabel(200, 302, 1152, "Chelonia sea access");
        AddButton(420, 302, 4017, 4019, 0); AddLabel(460, 302, 1152, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 1) { HavenScalisHunt.Show(state.Mobile); }
        else if (info.ButtonID == 2)
        {
            if (HavenChelonia.Registry.Count == 0 || !HavenFrontierSupport.Travel(state.Mobile, HavenChelonia.Landing, Map.Trammel))
            { state.Mobile.SendMessage("Sea access is unavailable here, or you are in combat. Try again from a safe location."); }
        }
    }
}
