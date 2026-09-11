using System;
using System.Linq;
using Server;
using Server.Engines.Doom;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

public static class DoomNavalSmoke
{
    private static void Require(bool value) { if (!value) throw new Exception("Doom/naval assertion failed"); }
    public static void Run(Action<string, Action> check, bool reload)
    {
        if (reload)
        {
            check("Doom six-stage sequence survives world reload", () => {
                var stages = World.Items.Values.OfType<GauntletSpawner>().ToArray();
                Require(stages.Length == 6 && stages.Count(s => s.State == GauntletSpawnerState.InProgress) == 1);
                var first = stages.Single(s => s.TypeName == "DarknightCreeper");
                var cursor = first;
                for (int i = 0; i < 6; i++) { Require(cursor.Sequence != null); cursor = cursor.Sequence; }
                Require(cursor == first && first.Creatures.Any(m => !m.Deleted));
            });
            check("Britannian ship preserves owner and native hold cargo after reload", () => {
                var ship = World.Items.Values.OfType<BritannianShip>().Single();
                Require(ship.Owner != null && ship.Owner.Name == "Naval fixture" && ship.Map == Map.Trammel);
                Require(ship.GalleonHold != null && ship.GalleonHold.Items.OfType<Gold>().Sum(g => g.Amount) == 1234);
            });
            return;
        }
        check("Doom advances all six native boss stages and restarts its cycle", () => {
            GauntletSpawner.GenGauntlet_OnCommand(null);
            var stages = World.Items.Values.OfType<GauntletSpawner>().ToArray();
            Require(stages.Length == 6);
            var first = stages.Single(s => s.TypeName == "DarknightCreeper");
            var current = first;
            for (int stage = 0; stage < 6; stage++)
            {
                Require(current.State == GauntletSpawnerState.InProgress && current.Creatures.Count > 0);
                if (current.Door != null) Require(current.Door.Locked);
                // Use actual creature deaths; invoke the native timer callback synchronously.
                foreach (var creature in current.Creatures.ToArray()) creature.Kill();
                Require(current.HasCompleted);
                var next = current.Sequence;
                current.Slice();
                if (current.Door != null) Require(!current.Door.Locked);
                Require(next.State == GauntletSpawnerState.InProgress);
                current = next;
            }
            Require(current == first && stages.Count(s => s.State == GauntletSpawnerState.InProgress) == 1);
        });
        check("Britannian ship moves with native hold cargo intact", () => {
            var ship = new BritannianShip();
            var owner = new PlayerMobile { Name = "Naval fixture" };
            ship.Owner = owner;
            bool placed = false;
            for (int x = 100; x < 5000 && !placed; x += 200)
                for (int y = 3500; y < 4000 && !placed; y += 100)
                {
                    var point = new Point3D(x, y, -5);
                    if (ship.CanFit(point, Map.Trammel, ship.ItemID)) { ship.MoveToWorld(point, Map.Trammel); placed = true; }
                }
            Require(placed && ship.GalleonHold != null);
            ship.GalleonHold.DropItem(new Gold(1234));
            var before = ship.Location;
            ship.Anchored = false;
            Require(ship.Move(Direction.North, 1, 1, false) && ship.Location != before);
            Require(ship.GalleonHold.Items.OfType<Gold>().Sum(g => g.Amount) == 1234);
            ship.Anchored = true;
        });
    }
}

