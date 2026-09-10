using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenAbyssMiniChamp : Item
{
    [SerializableField(0)] private int _siteId;
    [SerializableField(1)] private int _wave;
    [SerializableField(2)] private List<int> _kills = new();
    [SerializableField(3)] private List<BaseCreature> _creatures = new();
    [SerializableField(4)] private List<int> _species = new();
    [SerializableField(5)] private bool _active;
    [SerializableField(6)] private DateTime _nextStart;
    [SerializableField(7)] private DateTime _lastVisitor;
    [SerializableField(8)] private List<Mobile> _participants = new();
    [SerializableField(9)] private long _completions;
    private Timer _timer;
    internal static readonly Dictionary<BaseCreature, HavenAbyssMiniChamp> Owners = new();
    internal HavenAbyssSite Definition => SiteId >= 0 && SiteId < HavenAbyssCatalog.Sites.Length ? HavenAbyssCatalog.Sites[SiteId] : null;
    [Constructible] public HavenAbyssMiniChamp() : base(0xBD2) { Visible = false; Movable = false; Name = "Abyss mini-champion"; }
    internal void Setup(int id, Point3D point)
    { SiteId = id; MoveToWorld(point, Map.TerMur); Schedule(); }
    [AfterDeserialization]
    private void Recover()
    {
        // No saved encounter creates another set of actors or repeats a clear reward.
        Reset(); Schedule();
    }
    private void Schedule()
    { _timer?.Stop(); _timer = Timer.DelayCall(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), Pulse); }
    internal bool Start()
    {
        if (Deleted || Definition == null || Active || Core.Now < NextStart || Map == null || Map == Map.Internal) { return false; }
        Active = true; Wave = 0; Participants.Clear(); BeginWave(); LastVisitor = Core.Now; Spawn();
        if (Creatures.Count > 0) { return true; }
        Reset(); return false;
    }
    private void BeginWave()
    { Kills.Clear(); foreach (var _ in Definition.Waves[Wave].Required) { Kills.Add(0); } this.MarkDirty(); }
    internal void Pulse()
    {
        if (Deleted || Definition == null) { return; }
        var visitor = false;
        foreach (var mobile in GetMobilesInRange(45))
        { if (mobile is PlayerMobile { Alive: true } && !mobile.Hidden) { visitor = true; break; } }
        if (visitor) { LastVisitor = Core.Now; if (!Active) { Start(); } }
        if (!Active) { return; }
        if (Core.Now - LastVisitor > TimeSpan.FromMinutes(10)) { Reset(); return; }
        for (var i = Creatures.Count - 1; i >= 0; i--)
        {
            var creature = Creatures[i];
            if (creature?.Deleted != false)
            {
                if (creature != null) { Owners.Remove(creature); }
                Creatures.RemoveAt(i); Species.RemoveAt(i); this.MarkDirty();
            }
            else if (creature.Map != Map || !creature.InRange(Location, 40))
            { if (TryPoint(Location, Map, 20, out var point)) { creature.MoveToWorld(point, Map); } }
        }
        Spawn();
    }
    internal static bool TryPoint(Point3D center, Map map, int radius, out Point3D point, int minimum = 0)
    {
        point = default;
        if (map == null || map == Map.Internal) { return false; }
        for (var r = minimum; r <= radius; r++)
        {
            for (var dx = -r; dx <= r; dx++)
            {
                for (var dy = -r; dy <= r; dy++)
                {
                    if (r > 0 && Math.Abs(dx) != r && Math.Abs(dy) != r) { continue; }
                    var x = center.X + dx; var y = center.Y + dy;
                    if (x < 0 || y < 0 || x >= map.Width || y >= map.Height) { continue; }
                    // Prefer the center's floor; do not jump to another dungeon level.
                    if (map.CanSpawnMobile(x, y, center.Z - 3, center.Z + 3, false, false, out var z))
                    {
                        var candidate = new Point3D(x, y, z);
                        if (map.LineOfSight(new Point3D(center.X, center.Y, center.Z + 16), new Point3D(x, y, z + 16)))
                        { point = candidate; return true; }
                    }
                }
            }
        }
        return false;
    }
    internal void Spawn()
    {
        if (!Active || Definition == null || Wave >= Definition.Waves.Length) { return; }
        var wave = Definition.Waves[Wave]; var added = 0;
        for (var species = 0; species < wave.Creatures.Length && added < 4 && Creatures.Count < 12; species++)
        {
            var present = 0; foreach (var other in Species) { if (other == species) { present++; } }
            if (Kills[species] + present >= wave.Required[species]) { continue; }
            if (!ConnectedPoint(Location, Map, 18, 4, out var point)) { continue; }
            var mob = (BaseCreature)Activator.CreateInstance(wave.Creatures[species]);
            mob.Tamable = false; mob.Home = Location; mob.RangeHome = 20;
            Creatures.Add(mob); Species.Add(species); Owners[mob] = this; this.MarkDirty();
            mob.MoveToWorld(point, Map); added++;
        }
    }
    internal static bool ConnectedPoint(Point3D center, Map map, int radius, int minimum, out Point3D point)
    {
        point = default;
        if (map == null || map == Map.Internal) { return false; }
        var pending = new Queue<Point3D>(); var seen = new HashSet<Point2D>(); var choices = new List<Point3D>();
        pending.Enqueue(center); seen.Add(new Point2D(center.X, center.Y));
        while (pending.TryDequeue(out var current))
        {
            if (Math.Max(Math.Abs(current.X - center.X), Math.Abs(current.Y - center.Y)) >= minimum && map.CanSpawnMobile(current)) { choices.Add(current); }
            for (var i = 0; i < 4; i++)
            {
                var x = current.X + (i == 0 ? 1 : i == 1 ? -1 : 0); var y = current.Y + (i == 2 ? 1 : i == 3 ? -1 : 0);
                if (x < 0 || y < 0 || x >= map.Width || y >= map.Height || Math.Abs(x - center.X) > radius || Math.Abs(y - center.Y) > radius || !seen.Add(new Point2D(x, y))) { continue; }
                for (var z = current.Z - 2; z <= current.Z + 2; z++)
                {
                    var next = new Point3D(x, y, z);
                    if (map.CanFit(next, 16, checkMobiles: false)) { pending.Enqueue(next); break; }
                }
            }
        }
        if (choices.Count == 0) { return false; }
        point = choices[Utility.Random(choices.Count)]; return true;
    }
    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    public static void OnCreatureDeath(BaseCreature creature)
    {
        if (Owners.TryGetValue(creature, out var owner)) { owner.Defeated(creature); }
    }
    internal void Defeated(BaseCreature creature)
    {
        var index = Creatures.IndexOf(creature);
        if (!Active || index < 0 || index >= Species.Count) { return; }
        var species = Species[index]; Creatures.RemoveAt(index); Species.RemoveAt(index); Owners.Remove(creature);
        if (creature.Controlled || creature.Summoned || creature.Owners.Count > 0) { this.MarkDirty(); return; }
        foreach (var right in BaseCreature.GetLootingRights(creature.DamageEntries, creature.HitsMax))
        {
            if (right.m_HasRight && right.m_Mobile is PlayerMobile player && !player.Deleted && player.Map == Map && player.InRange(this, 45) &&
                !Participants.Contains(player)) { Participants.Add(player); }
        }
        HavenAbyssDrops.Fill(creature, Definition.Essence, Wave == Definition.Waves.Length - 1);
        Kills[species]++; this.MarkDirty(); LastVisitor = Core.Now;
        var completed = true;
        for (var i = 0; i < Kills.Count; i++) { if (Kills[i] < Definition.Waves[Wave].Required[i]) { completed = false; break; } }
        if (!completed) { return; }
        if (++Wave < Definition.Waves.Length) { BeginWave(); return; }
        // Commit completion and clear actor ownership before handing out rewards.
        Active = false; Completions++; NextStart = Core.Now + TimeSpan.FromMinutes(2);
        var recipients = Participants.ToArray(); Participants.Clear(); this.MarkDirty();
        foreach (var mobile in recipients)
        {
            if (mobile?.Deleted != false || mobile.Map != Map || !mobile.InRange(this, 60)) { continue; }
            HavenAbyssDrops.AwardClear(mobile, Definition);
        }
    }
    internal void Reset()
    {
        Active = false;
        foreach (var creature in Creatures)
        {
            if (creature == null) { continue; }
            Owners.Remove(creature);
            if (!creature.Deleted && !creature.Controlled && creature.Owners.Count == 0) { creature.Delete(); }
        }
        Creatures.Clear(); Species.Clear(); Kills.Clear(); Participants.Clear(); Wave = 0;
        NextStart = Core.Now + TimeSpan.FromSeconds(30); this.MarkDirty();
    }
    public override void OnDelete() { _timer?.Stop(); _timer = null; Reset(); base.OnDelete(); }
}
