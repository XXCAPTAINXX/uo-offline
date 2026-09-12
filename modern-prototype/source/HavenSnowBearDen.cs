using System;
using System.Collections.Generic;
using Server;
using Server.Commands;
using Server.Mobiles;

namespace Server.HavenPrototype
{


public partial class HavenSnowBearDen : Item
{
    internal static readonly HashSet<HavenSnowBearDen> Registry = new HashSet<HavenSnowBearDen>();
    internal static readonly Point3D Site = new Point3D(942, 116, 0);
    public HavenSnowBear Bear;
    public DateTime NextSpawn;
    private Timer _timer;
    [Constructable]
    public HavenSnowBearDen() : base(0x1363) { Name = "Frostbound bear den"; Movable = false; }
    public static void Configure() => CommandSystem.Register("HavenSnowSetup", AccessLevel.Administrator, SetupCommand);
    private static void SetupCommand(CommandEventArgs args)
    {
        var den = Install();
        args.Mobile.SendMessage(den == null ? "The snowy clearing is blocked; nothing was installed." : "Frostbound den installed in northern Tokuno. Find it in the Wayfarer's Atlas, Tokuno towns.");
    }
    internal static HavenSnowBearDen Install()
    {
        foreach (var den in Registry) { if (!den.Deleted && den.Map == Map.Tokuno) { return den; } }
        var point = new Point3D(Site.X, Site.Y, Map.Tokuno.GetAverageZ(Site.X, Site.Y));
        if (!Map.Tokuno.CanSpawnMobile(point)) { return null; }
        var created = new HavenSnowBearDen(); created.MoveToWorld(point, Map.Tokuno); created.Register(); created.Tick(); return created;
    }
    
    private void Register()
    {
        Registry.Add(this); _timer?.Stop(); _timer = Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), Tick);
    }
    internal static bool TryFloor(Point3D center, Map map, out Point3D point)
    {
        point = default;
        // Cardinal flood fill keeps the bear on reachable snow rather than a nearby cliff ledge.
        var pending = new Queue<Point3D>(); var seen = new HashSet<Point2D>(); var choices = new List<Point3D>();
        pending.Enqueue(center); seen.Add(new Point2D(center.X, center.Y));
        while (pending.Count>0)
        {
            var current=pending.Dequeue();
            if (map.CanSpawnMobile(current) && Math.Max(Math.Abs(current.X - center.X), Math.Abs(current.Y - center.Y)) >= 3) { choices.Add(current); }
            for (var i = 0; i < 4; i++)
            {
                var x = current.X + (i == 0 ? 1 : i == 1 ? -1 : 0); var y = current.Y + (i == 2 ? 1 : i == 3 ? -1 : 0);
                if (Math.Abs(x - center.X) > 12 || Math.Abs(y - center.Y) > 12 || !seen.Add(new Point2D(x, y))) { continue; }
                var next = new Point3D(x, y, map.GetAverageZ(x, y));
                if (Math.Abs(next.Z - current.Z) <= 2 && map.CanFit(next, 16, false, false)) { pending.Enqueue(next); }
            }
        }
        if (choices.Count == 0) { return false; }
        point = choices[Utility.Random(choices.Count)]; return true;
    }
    internal void Tick()
    {
        if (Deleted || Map != Map.Tokuno) { return; }
        if (Bear != null && (Bear.Deleted || Bear.Controlled || Bear.Owners.Count > 0))
        { Bear = null; NextSpawn = DateTime.UtcNow + TimeSpan.FromSeconds(15); }
        if (Bear == null)
        {
            if (DateTime.UtcNow < NextSpawn || !TryFloor(Location, Map, out var point)) { return; }
            var bear = new HavenSnowBear(); var roll = Utility.RandomDouble(); var tier = roll < .50 ? 1 : roll < .85 ? 2 : 3;
            HavenPetMissions.ApplyRarity(bear, tier); bear.MinTameSkill = tier == 3 ? 120 : tier == 2 ? 110 : 100;
            bear.Home = Location; bear.RangeHome = 12; bear.MoveToWorld(point, Map); Bear = bear;
        }
        else if ((Bear.Map != Map || !Bear.InRange(this, 30)) && TryFloor(Location, Map, out var point))
        { Bear.MoveToWorld(point, Map); }
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Map == Map && from.InRange(this, 4))
        { from.SendMessage("Frostbound bears roam this clearing. Rare / Epic / Legendary require 100 / 110 / 120 taming. One bear returns about 15 seconds after the last is tamed or killed."); }
    }
    public HavenSnowBearDen(Serial serial):base(serial){}
    public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Bear);w.Write(NextSpawn);}
    public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Bear=r.ReadMobile() as HavenSnowBear;NextSpawn=r.ReadDateTime();if(NextSpawn>DateTime.UtcNow.AddSeconds(15))NextSpawn=DateTime.UtcNow.AddSeconds(15);Timer.DelayCall(TimeSpan.Zero,Register);}
    public override void OnDelete()
    {
        _timer?.Stop(); _timer = null; Registry.Remove(this);
        if (Bear?.Deleted == false && !Bear.Controlled && Bear.Owners.Count == 0) { Bear.Delete(); }
        Bear = null; base.OnDelete();
    }
}

}
