using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;
public static class EncounterHabitatSmoke
{
    public static void Run(Action<string> log)
    {
        var spawner = World.Items.Values.OfType<HavenEodonPetSpawner>().Single(s => s.Tiger);
        var tiger = spawner.GetSpawn().OfType<BaseCreature>().Single();
        tiger.Combatant = null; tiger.BardPacified = false;
        tiger.MoveToWorld(new Point3D(spawner.X + 63, spawner.Y, spawner.Z), spawner.Map);
        spawner.Spawn();
        if (!tiger.InRange(spawner.Location, 3) || spawner.GetSpawn().Single() != tiger) throw new Exception("Stray recovery failed or duplicated tiger");
        var player = new PlayerMobile(); player.AddItem(new Backpack()); player.MoveToWorld(spawner.Location, spawner.Map);
        var stranger = new PlayerMobile(); stranger.AddItem(new Backpack()); stranger.MoveToWorld(player.Location, player.Map);
        var encounter = new HavenWanderingEncounter(); encounter.MoveToWorld(player.Location, player.Map);
        var invader = new HavenEncounterMob(1, false) { Encounter = encounter }; invader.MoveToWorld(player.Location, player.Map);
        var chest = new HavenEncounterChest(); chest.Claimants.Add(player); chest.MoveToWorld(new Point3D(player.X + 5, player.Y, player.Z + 40), player.Map);
        var reward = new Diamond(5); chest.DropItem(reward);
        try
        {
            if (invader.CanBeHarmful(tiger, false, false)) throw new Exception("Invader attacks wild tamable");
            if (chest.Collect(stranger) != 0 || reward.Parent != chest) throw new Exception("Stranger stole loot");
            player.Backpack.DropItem(new Gold(1)); player.Backpack.MaxItems = 1;
            if (chest.Collect(player) != 0 || reward.Parent != chest) throw new Exception("Full pack lost loot");
            player.Backpack.MaxItems = 125;
            if (chest.Collect(player) != 1 || reward.Parent != player.Backpack || chest.Collect(player) != 0) throw new Exception("Collection lost or duplicated reward");
            player.MoveToWorld(new Point3D(chest.X + 29, chest.Y, chest.Z), chest.Map);
            if (chest.NearbyClaim(player)) throw new Exception("Out of range collection allowed");
            log("PASS same stray tiger returned without duplication; invader cannot harm wild tamable; raised chest loot collected by participant, full-pack retention, stranger/range denial and no duplicate collection");
        }
        finally { chest.Delete(); invader.Delete(); encounter.Delete(); player.Delete(); stranger.Delete(); }
    }
}
