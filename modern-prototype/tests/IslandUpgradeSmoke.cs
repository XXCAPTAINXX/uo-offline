using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.HavenPrototype;

public static class IslandUpgradeSmoke
{
    static IEnumerable<Item> Contents(Item item)
    {
        yield return item;
        foreach (var child in item.Items) foreach (var nested in Contents(child)) yield return nested;
    }
    public static void Run(bool reload, Action<string> log)
    {
        HavenRecoveredHeadquarters house;
        if (!reload)
        {
            var old = World.Items.Values.OfType<HavenIslandEstate>().Single(x => !x.Deleted);
            log("Original estate=" + old.Serial.Value + " owner=" + old.Owner.Serial.Value + " vault=" + old.Vault.Serial.Value);
            var contents = Contents(old.Vault).ToArray();
            var owner = old.Owner; var vault = old.Vault;
            var identities = contents.Select(i => i.Serial.Value + "," + ((i.Parent as Item)?.Serial.Value ?? 0)).ToArray();
            house = HavenIslandUpgrade.Apply(old);
            if (!old.Deleted || house.Owner != owner || house.Vault != vault || contents.Any(i => i.Deleted)) throw new Exception("Ownership or vault identity changed");
            if (!identities.SequenceEqual(Contents(house.Vault).Select(i => i.Serial.Value + "," + ((i.Parent as Item)?.Serial.Value ?? 0)))) throw new Exception("Nested contents changed");
            File.WriteAllLines("island-upgrade-identities.txt", new[] { house.Serial.Value.ToString(), owner.Serial.Value.ToString() }.Concat(identities));
        }
        else
        {
            var identities = File.ReadAllLines("island-upgrade-identities.txt");
            house = World.FindItem((Serial)int.Parse(identities[0])) as HavenRecoveredHeadquarters;
            if (house == null || house.Owner.Serial.Value != int.Parse(identities[1])) throw new Exception("Reload lost house owner");
            if (!identities.Skip(2).SequenceEqual(Contents(house.Vault).Select(i => i.Serial.Value + "," + ((i.Parent as Item)?.Serial.Value ?? 0)))) throw new Exception("Reload changed nested vault");
        }
        if (World.Items.Values.OfType<HavenIslandEstate>().Any(i => !i.Deleted)) throw new Exception("Old castle remains");
        house.CheckFloorAccess(); house.CheckWalkingRoutes();
        if (house.CompanyFixtures.Count != 70 || house.Stations.Count != 3 || house.Stations.Any(s => s.Estate != house)) throw new Exception("Missing fixtures or stores");
        var patrols = World.Items.Values.OfType<HavenIslandEncounters>().ToArray();
        if (patrols.Length != 3) throw new Exception("Missing patrols");
        foreach (var patrol in patrols)
        {
            if (patrol.Patrol.Count < 4) throw new Exception("Missing patrol route");
            for (int i = 0; i < patrol.Patrol.Count; i++)
            {
                var node = patrol.Patrol[i]; var next = patrol.Patrol[(i + 1) % patrol.Patrol.Count]; int z;
                if (node.NextPoint != next || !patrol.Safe(node.Location) || !Server.Movement.Movement.CheckMovement(node.Location, patrol.Map, node.Location, Utility.GetDirection(node, next), out z) || z != next.Z) throw new Exception("Broken patrol route");
            }
        }
        var approach = World.Items.Values.OfType<HavenCoveApproach>().Single(i => !i.Deleted);
        if (approach.Fixtures.OfType<HavenCoveBoard>().Single().Camp == null) throw new Exception("Cove board lost encounter");
        if (!reload) World.Save(false, false);
        log("PASS " + (reload ? "separate-process reload" : "migration") + ": custom compound, 70 fixtures, ladders, exact nested vault, 3 stores, 3 patrol loops and cove board");
    }
}
