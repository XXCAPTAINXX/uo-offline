using System;
using System.Collections.Generic;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Engines.Shadowguard;

namespace Server.HavenPrototype
{
    public static class HavenFountainHelp
    {
        private static readonly int[] DX = { 0, 1, 0, -1 };
        private static readonly int[] DY = { -1, 0, 1, 0 };

        public static bool TryHelp(Mobile owner, HavenCompanion companion)
        {
            var room = ShadowguardController.GetEncounter(owner.Location, owner.Map) as FountainEncounter;
            if (room == null) return false;
            if (!HavenPreview.Enabled || !companion.CanOpenPack(owner) || !owner.Alive ||
                !room.HasBegun || room.Completed || !room.Participants.Contains(owner as PlayerMobile) ||
                room.ShadowguardCanals == null) return true;

            var spigot = room.ShadowguardCanals.OfType<ShadowguardSpigot>()
                .Where(s => !s.Deleted && (s.ItemID == 39922 || s.ItemID == 39909))
                .OrderBy(s => owner.GetDistanceToSqrt(s.Location)).FirstOrDefault();
            if (spigot == null) { owner.SendMessage("All four spigots are connected."); return true; }
            spigot.PublicOverheadMessage(MessageType.Regular, 53, false, "NEXT SPIGOT");
            if (!owner.InRange(spigot, 3) || !owner.InLOS(spigot))
            { companion.SayTo(owner, "Come within three tiles of the marked spigot and I'll arrange our canal pieces."); return true; }

            var route = FindRoute(room, spigot);
            if (route == null)
            { companion.SayTo(owner, "I need a clear route to a drain. Pick up loose canal pieces near this spigot and try again."); return true; }
            var pieces = room.ShadowguardCanals.OfType<ShadowguardCanal>()
                .Where(c => !c.Deleted && c.Movable &&
                    ((owner.Backpack != null && c.IsChildOf(owner.Backpack)) ||
                     (companion.Backpack != null && c.IsChildOf(companion.Backpack))))
                .Take(route.Count - 1).ToArray();
            if (pieces.Length < route.Count - 1)
            { companion.SayTo(owner, "This route needs " + (route.Count - 1) + " canal pieces. We have " + pieces.Length + ". Collect the pieces dropped by the water elementals into either backpack."); return true; }

            // Plan before moving anything. Only earned pieces belonging to this room qualify.
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i].Flow = FlowFor(i == 0 ? spigot.Location : route[i - 1], route[i], route[i + 1]);
                pieces[i].MoveToWorld(route[i], owner.Map);
            }
            room.UseSpigot(spigot, owner);
            companion.SayTo(owner, "The canal is connected. On to the next spigot!");
            return true;
        }

        public static List<Point3D> FindRoute(FountainEncounter room, ShadowguardSpigot spigot)
        {
            var start = new Point3D(spigot.X + (spigot.ItemID == 39909 ? 1 : 0),
                spigot.Y + (spigot.ItemID == 39922 ? 1 : 0), -20);
            var drains = new HashSet<Point3D>(room.ShadowguardCanals.OfType<ShadowguardDrain>()
                .Where(d => !d.Deleted).Select(d => d.Location));
            var queue = new Queue<Point3D>();
            var previous = new Dictionary<Point3D, Point3D>();
            queue.Enqueue(start); previous[start] = start;
            while (queue.Count > 0 && previous.Count < 4096)
            {
                var p = queue.Dequeue();
                if (drains.Contains(p))
                {
                    var path = new List<Point3D>();
                    for (;;) { path.Add(p); if (p == start) break; p = previous[p]; }
                    path.Reverse();
                    return path.Count > 1 ? path : null;
                }
                if (!Clear(room, p)) continue;
                for (int n = 0; n < 4; n++)
                {
                    var next = new Point3D(p.X + DX[n], p.Y + DY[n], -20);
                    if (!previous.ContainsKey(next) && room.Region.Contains(next))
                    { previous[next] = p; queue.Enqueue(next); }
                }
            }
            return null;
        }

        private static bool Clear(FountainEncounter room, Point3D p)
        {
            if (!room.Region.Contains(p) || !Map.TerMur.CanFit(p, 1, false, false)) return false;
            var items = Map.TerMur.GetItemsInRange(p, 0);
            try
            {
                foreach (Item item in items)
                    if (item.Z == p.Z && (item is ShadowguardCanal || item is ShadowguardSpigot)) return false;
            }
            finally { items.Free(); }
            return true;
        }

        public static Flow FlowFor(Point3D before, Point3D at, Point3D after)
        {
            bool north = before.Y < at.Y || after.Y < at.Y;
            bool south = before.Y > at.Y || after.Y > at.Y;
            bool east = before.X > at.X || after.X > at.X;
            bool west = before.X < at.X || after.X < at.X;
            if (north && south) return Flow.NorthSouth;
            if (east && west) return Flow.EastWest;
            // Native corner names describe the artwork, rather than its open sides.
            if (north && east) return Flow.SouthWestCorner;
            if (north && west) return Flow.SouthEastCorner;
            if (south && east) return Flow.NorthWestCorner;
            return Flow.NorthEastCorner;
        }
    }
}
