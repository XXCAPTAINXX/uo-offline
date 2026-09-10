using System;
using System.Collections.Generic;
using Server.CustomBots;
using Server.Items;
using Move = Server.Movement.Movement;
using MoveImpl = Server.Movement.MovementImpl;

namespace Server.UOOffline;

// A bounded corridor route feeds short goals into the existing movement/combat loop.
// It uses the current map and native elevation/diagonal checks, not the old T2A graph.
internal sealed class HavenDungeonNavigation
{
    private readonly Queue<Point3D> _points = new();
    private Point3D _goal;
    private Map _map;
    private DateTime _retry;
    internal Point3D Next(PlayerBot bot, Point3D goal)
    {
        while (_points.Count > 0 && bot.InRange(_points.Peek(), 1) && Math.Abs(bot.Z - _points.Peek().Z) < 5) { _points.Dequeue(); }
        if (_map != bot.Map || _goal != goal || _points.Count == 0 && Core.Now >= _retry)
        {
            _map = bot.Map; _goal = goal; _retry = Core.Now + TimeSpan.FromSeconds(30); _points.Clear();
            foreach (var point in Route(bot, goal)) { _points.Enqueue(point); }
        }
        return _points.Count > 0 ? _points.Peek() : goal;
    }
    internal static List<Point3D> Route(PlayerBot bot, Point3D goal)
    {
        var result = new List<Point3D>();
        if (bot.Map == null || bot.Map == Map.Internal || !bot.InRange(goal, 200)) { return result; }
        var start = bot.Location; var open = new PriorityQueue<Point3D, int>();
        var costs = new Dictionary<Point3D, int> { [start] = 0 }; var previous = new Dictionary<Point3D, Point3D>();
        open.Enqueue(start, 0); var visited = 0;
        var ignored = MoveImpl.AlwaysIgnoreDoors;
        try
        {
            MoveImpl.AlwaysIgnoreDoors = true;
            while (open.TryDequeue(out var point, out _) && ++visited <= 6000)
            {
                if (point.X == goal.X && point.Y == goal.Y && Math.Abs(point.Z - goal.Z) < 6)
                {
                    while (point != start) { result.Add(point); point = previous[point]; }
                    result.Reverse();
                    // Preserve enough intermediate points to avoid cutting through walls.
                    var shortGoals = new List<Point3D>();
                    for (var i = 7; i < result.Count; i += 8) { shortGoals.Add(result[i]); }
                    if (shortGoals.Count == 0 || shortGoals[^1] != result[^1]) { if (result.Count > 0) { shortGoals.Add(result[^1]); } }
                    return shortGoals;
                }
                for (var i = 0; i < 8; i++)
                {
                    var direction = (Direction)i; var x = point.X; var y = point.Y; Move.Offset(direction, ref x, ref y);
                    if (Math.Abs(x - start.X) > 200 || Math.Abs(y - start.Y) > 200 || !Move.CheckMovement(bot, bot.Map, point, direction, out var z)) { continue; }
                    var next = new Point3D(x, y, z); var locked = false;
                    foreach (var item in bot.Map.GetItemsAt(next)) { if (item is BaseDoor { Locked: true } door && Math.Abs(door.Z - z) < 20) { locked = true; break; } }
                    if (locked) { continue; }
                    var cost = costs[point] + 1;
                    if (costs.TryGetValue(next, out var old) && old <= cost) { continue; }
                    costs[next] = cost; previous[next] = point;
                    open.Enqueue(next, cost + Math.Max(Math.Abs(x - goal.X), Math.Abs(y - goal.Y)));
                }
            }
        }
        finally { MoveImpl.AlwaysIgnoreDoors = ignored; }
        return result;
    }
}
