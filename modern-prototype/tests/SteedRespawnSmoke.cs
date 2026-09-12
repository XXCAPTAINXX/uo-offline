using System;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.HavenPrototype;
public static class SteedRespawnSmoke {
 public static void Run(Mobile owner, Action<bool,string> check) {
  var spawner=new HavenVampiricSteedSpawner();
  spawner.MoveToWorld(new Point3D(3675,2410,Map.Trammel.GetAverageZ(3675,2410)),Map.Trammel);
  spawner.Spawn();var first=spawner.GetSpawn().OfType<BaseCreature>().Single();
  first.SetControlMaster(owner);spawner.Spawn();
  var second=spawner.GetSpawn().OfType<BaseCreature>().Single();
  check(first!=second&&!first.Deleted&&first.ControlMaster==owner&&!second.Controlled,"steed replacement excludes and preserves tamed predecessor");
  spawner.Spawn();check(spawner.GetSpawn().Single()==second,"full steed habitat does not duplicate wild pet");
  second.Delete();spawner.Spawn();check(spawner.GetSpawn().OfType<BaseCreature>().Single()!=second,"dead steed replaced on next spawn tick");
  spawner.Delete();first.Delete();
 }
}
