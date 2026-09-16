using System;
using System.IO;
using System.Linq;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Engines.Shadowguard;

namespace Server
{
    // Isolated fresh-world evaluation only. Never install on a player server.
    public static class HavenEvaluation
    {
        private static int failures;
        private static readonly string Report = "../runtime-checks.log";
        public static void Initialize()
        {
            if (File.Exists("EVALUATION-ONLY"))
                EventSink.ServerStarted += () => Timer.DelayCall(TimeSpan.FromSeconds(2), Run);
        }
        private static void Check(string name, Action action)
        {
            try { action(); File.AppendAllText(Report, "PASS " + name + Environment.NewLine); }
            catch (Exception ex) { failures++; File.AppendAllText(Report, "FAIL " + name + ": " + ex + Environment.NewLine); }
        }
        private static void Require(bool value, string message)
        {
            if (!value) throw new Exception(message);
        }
        private static void Run()
        {
            bool reload = File.Exists("../saved-fixture.txt");
            File.AppendAllText(Report, "PHASE " + (reload ? "reload" : "fresh") + " " + DateTime.UtcNow.ToString("O") + Environment.NewLine);
            Check("modern expansion flags", () => Require(Core.SA && Core.HS && Core.TOL, "Later expansion flags disabled"));
            foreach (var map in new[] { Map.Felucca, Map.Trammel, Map.Ilshenar, Map.Malas, Map.Tokuno, Map.TerMur })
                Check("map data " + map.Name, () => { var tile = map.Tiles.GetLandTile(100, 100); Require(tile.ID >= 0, "No tile"); });
            if (reload)
            {
                Check("saved kitchen and contents reload", () => {
                    var serial = Int32.Parse(File.ReadAllText("../saved-fixture.txt"));
                    var kit = World.FindItem((Serial)serial) as DecorativeKitchenSet;
                    Require(kit != null, "Kitchen missing after reload");
                    Require(kit.FindItemsByType(typeof(Countertop), true).Length == 4, "Counters missing");
                    var cabinet = kit.FindItemByType(typeof(ChinaCabinet), true) as Container;
                    Require(cabinet != null && cabinet.GetAmount(typeof(Gold)) == 123, "Cabinet contents changed");
                });
                Check("Shadowguard controller reload", () => Require(ShadowguardController.Instance != null && ShadowguardController.Instance.Instances.Count >= 14, "Instances missing"));
            }
            else
            {
                DecorativeKitchenSet kit = null;
                Check("complete kitchen set and storage", () => {
                    kit = new DecorativeKitchenSet();
                    kit.MoveToWorld(new Point3D(1500, 1500, 0), Map.Trammel);
                    Require(kit.FindItemsByType(typeof(Countertop), true).Length == 4, "Expected four counters");
                    var cabinet = (Container)kit.FindItemByType(typeof(ChinaCabinet), true);
                    cabinet.DropItem(new Gold(123));
                    Require(cabinet.GetAmount(typeof(Gold)) == 123, "Storage failed");
                    Require(kit.FindItemByType(typeof(PieSafe), true) is Container, "Pie safe not a container");
                });
                foreach (DirectionType direction in new[] { DirectionType.South, DirectionType.East })
                {
                    Check("stove addon " + direction, () => {
                        var stove = new WoodStoveAddon(direction);
                        Require(stove.Components.Count == 1, "Invalid stove");
                        Require(TileData.ItemTable[stove.Components[0].ItemID].Name.Length > 0, "Missing stove tile metadata");
                        stove.Delete();
                    });
                    Check("wash basin water behavior " + direction, () => {
                        var basin = new WashBasinAddon(direction);
                        Require(basin.Components.Count == 3, "Expected three tile sink");
                        var water = basin.Components.OfType<WaterContainerComponent>().Single();
                        var dryArt = water.ItemID;
                        water.Quantity = 5;
                        Require(water.IsFull && water.ItemID != dryArt, "Fill not reflected");
                        water.Quantity = 0;
                        Require(water.IsEmpty && water.ItemID == dryArt, "Empty not reflected");
                        basin.Delete();
                    });
                }
                Check("Imbuing registered and weapon intensity", () => {
                    Require(SkillInfo.Table[(int)SkillName.Imbuing].Callback != null, "Missing skill callback");
                    var sword = new Longsword();
                    Require(SkillHandlers.Imbuing.GetMaxWeight(sword) > 0, "No intensity rules");
                    Require(SkillHandlers.Imbuing.GetMaxProps(sword) > 0, "No property rules");
                    sword.Delete();
                });
                var controller = new ShadowguardController();
                controller.MoveToWorld(new Point3D(501, 2192, 50), Map.TerMur);
                Check("Shadowguard instance allocation", () => Require(controller.Instances.Count >= 14, "Missing room instances"));
                var leader = new PlayerMobile { Name = "evaluation fixture", AccessLevel = AccessLevel.Player };
                int index = 0;
                foreach (EncounterType type in new[] { EncounterType.Bar, EncounterType.Orchard, EncounterType.Armory, EncounterType.Fountain, EncounterType.Belfry, EncounterType.Roof })
                {
                    Check("Shadowguard setup and tick " + type, () => {
                        var encounter = ShadowguardEncounter.ConstructEncounter(type);
                        encounter.Instance = controller.Instances[type == EncounterType.Roof ? 13 : index++];
                        encounter.PartyLeader = leader;
                        encounter.Setup();
                        encounter.CheckAddon();
                        encounter.OnTick();
                        if (type != EncounterType.Roof) Require(encounter.Addon != null && encounter.Addon.Components.Count > 0, "Room geometry missing");
                        if (type == EncounterType.Bar) Require(((BarEncounter)encounter).Pirates.Count >= 3, "Pirates missing");
                        if (type == EncounterType.Orchard) Require(((OrchardEncounter)encounter).Trees.Count == 16, "Puzzle trees missing");
                        encounter.ClearItems();
                    });
                }
                leader.Delete();
                Check("Blackthorn invasion starts with beacon and mobs", () => {
                    var invasion = new Engines.Blackthorn.InvasionController(Map.Trammel);
                    invasion.MoveToWorld(new Point3D(6250, 2300, 0), Map.Trammel);
                    invasion.BeginInvasion();
                    Require(invasion.Beacon != null && invasion.Spawn.Count > 0 && invasion.CurrentWave == 1, "Invasion not populated");
                    invasion.Cleanup();
                    invasion.Delete();
                });
                Check("Britannian ship moves over modern ocean map", () => {
                    var ship = new BritannianShip();
                    try {
                        bool placed = false;
                        for (int x = 100; x < 5000 && !placed; x += 200)
                            for (int y = 3500; y < 4000 && !placed; y += 100)
                            {
                                var p = new Point3D(x, y, -5);
                                if (ship.CanFit(p, Map.Trammel, ship.ItemID)) { ship.MoveToWorld(p, Map.Trammel); placed = true; }
                            }
                        Require(placed, "No fitting ocean site found");
                        var before = ship.Location;
                        ship.Anchored = false;
                        Require(ship.Move(Direction.North, 1, 1, false) && ship.Location != before, "Movement failed");
                    } finally { ship.Delete(); }
                });
                Check("custom house foundation constructs with components", () => {
                    var owner = new PlayerMobile();
                    var house = new HouseFoundation(owner, 0x13EC, 1000, 10);
                    Require(house.Components.List.Length > 0 && house.Owner == owner, "Missing foundation data or ownership");
                    house.Delete(); owner.Delete();
                });
                foreach (string name in new[] { "BritannianShip", "OrcishGalleon", "Osiredon", "CorgulTheSoulbinder", "Charydbis" })
                    Check("construct " + name, () => {
                        var type = ScriptCompiler.FindTypeByName(name);
                        Require(type != null, "Type missing");
                        var entity = Activator.CreateInstance(type) as IEntity;
                        Require(entity != null, "Not an entity");
                        entity.Delete();
                    });
                Check("save isolated world", () => {
                    Require(kit != null, "No fixture");
                    World.Save(false, false);
                    File.WriteAllText("../saved-fixture.txt", kit.Serial.Value.ToString());
                });
            }
            File.AppendAllText(Report, "COMPLETE failures=" + failures + Environment.NewLine);
            Timer.DelayCall(TimeSpan.FromSeconds(1), () => Core.Kill(false));
        }
    }
}
