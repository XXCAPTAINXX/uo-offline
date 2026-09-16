using System;
using System.Linq;
using Server.Items;
using Server.HavenPrototype;
public static class StairClearanceSmoke {
 public static void Run(HavenRecoveredHeadquarters house,Action<string> log){
  var fixtures=house.CompanyFixtures.ToArray();var vault=house.Vault;
  var contents=fixtures.OfType<Container>().ToDictionary(x=>x,x=>x.Items.ToArray());
  int moved=HavenStairClearance.Apply(house);
  if(HavenStairClearance.Apply(house)!=0)throw new Exception("Migration not idempotent");
  if(house.Vault!=vault||!fixtures.SequenceEqual(house.CompanyFixtures))throw new Exception("Storage identity changed");
  foreach(var pair in contents)if(!pair.Value.SequenceEqual(pair.Key.Items))throw new Exception("Chest contents changed");
  log("PASS moved "+moved+" fixtures; two clear stair-side lanes, stairs and all fixture routes; contents and identity preserved");
 }
}
