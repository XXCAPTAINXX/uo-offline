using System;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Multis;

namespace Server.UOOffline;

// Bounded incremental A*: hull-sized collision checks, four-tile grid, no world scans.
internal sealed class HavenSeaNavigation
{
    private readonly PriorityQueue<Point2D, int> _open = new();
    private readonly Dictionary<Point2D, int> _cost = new();
    private readonly Dictionary<Point2D, Point2D> _parent = new();
    private readonly HashSet<Point2D> _closed = new();
    private readonly Point3D _start, _goal;
    private readonly int _range;
    public List<Point3D> Path { get; } = new();
    public bool Finished { get; private set; }
    public bool Failed { get; private set; }
    internal HavenSeaNavigation(Point3D start, Point3D goal, int range)
    {
        _start = start; _goal = goal; _range = range;
        var p = new Point2D(start.X, start.Y); _cost[p] = 0; _open.Enqueue(p, Distance(p));
    }
    private int Distance(Point2D p) => Math.Max(Math.Abs(p.X - _goal.X), Math.Abs(p.Y - _goal.Y));
    internal static bool Clear(BaseBoat boat, Point3D start, Point3D end)
    {
        var dx = end.X - start.X; var dy = end.Y - start.Y;
        if (dx != 0 && dy != 0 && Math.Abs(dx) != Math.Abs(dy)) { return false; }
        var count = Math.Max(Math.Abs(dx), Math.Abs(dy));
        for (var step = 1; step <= count; step++)
        {
            var p = new Point3D(start.X + Math.Sign(dx) * step, start.Y + Math.Sign(dy) * step, start.Z);
            if (p.X < 12 || p.Y < 12 || p.X >= 5100 || p.Y >= 4080 ||
                Utility.InRange(p, HavenScalisHunt.RoamingWaters, 85) || !boat.CanFit(p, boat.Map, boat.ItemID)) { return false; }
        }
        return true;
    }
    internal void Advance(BaseBoat boat)
    {
        if (Finished || Failed) { return; }
        var watch = Stopwatch.StartNew();
        for (var step = 0; step < 24 && watch.ElapsedMilliseconds < 5; step++)
        {
            if (_open.Count == 0 || _closed.Count >= 6000) { Failed = true; return; }
            var p = _open.Dequeue(); if (!_closed.Add(p)) { continue; }
            if (Distance(p) <= _range)
            {
                while (_parent.TryGetValue(p, out var previous)) { Path.Add(new Point3D(p.X, p.Y, _start.Z)); p = previous; }
                Path.Reverse(); Finished = true; return;
            }
            for (var d = 0; d < 8; d++)
            {
                var dx = 0; var dy = 0; Server.Movement.Movement.Offset((Direction)d, ref dx, ref dy);
                var next = new Point2D(p.X + dx * 4, p.Y + dy * 4);
                if (_closed.Contains(next) || !Clear(boat, new Point3D(p.X, p.Y, _start.Z), new Point3D(next.X, next.Y, _start.Z))) { continue; }
                var cost = _cost[p] + 4;
                if (_cost.TryGetValue(next, out var old) && old <= cost) { continue; }
                _cost[next] = cost; _parent[next] = p; _open.Enqueue(next, cost + Distance(next));
            }
        }
    }
}
