using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.HavenPrototype;
public static class CoveOptionsSmoke {
 public static void Run(Action<string> log){
  System.IO.File.WriteAllText("HAVEN-INTERACTIVE-PREVIEW","1");
  var trader=World.FindMobile((Serial)37901) as Server.Mobiles.PlayerMobile;
  var powder=new Server.Items.PowderCharge(137);trader.Backpack.DropItem(powder);
  double oldPoints=Server.Engines.Points.PointsSystem.RisingTide.GetPoints(trader);
  if(!HavenCargoExchange.Exchange(trader,powder)||powder.Amount!=37||Server.Engines.Points.PointsSystem.RisingTide.GetPoints(trader)!=oldPoints+100)throw new Exception("Surplus exchange incorrect");
  if(HavenCargoExchange.Exchange(trader,powder))throw new Exception("Insufficient supplies consumed");powder.Delete();
  var cargo=new Server.Items.MaritimeCargo(Server.Items.CargoQuality.Exalted);trader.Backpack.DropItem(cargo);
  if(!HavenCargoExchange.Exchange(trader,cargo)||!cargo.Deleted||HavenCargoExchange.Exchange(trader,cargo))throw new Exception("Cargo exchange replay");
  var unowned=new Server.Items.Cannonball(100);unowned.MoveToWorld(trader.Location,trader.Map);
  if(HavenCargoExchange.Exchange(trader,unowned))throw new Exception("Unowned supplies consumed");unowned.Delete();
  int originalMax=trader.Backpack.MaxItems;trader.Backpack.MaxItems=10000;
  try{Server.Engines.Points.PointsSystem.RisingTide.AwardPoints(trader,30000);
   for(int choice=0;choice<HavenCargoExchange.Costs.Length;choice++){
    double beforeBuy=Server.Engines.Points.PointsSystem.RisingTide.GetPoints(trader);
    if(!HavenCargoExchange.Buy(trader,choice)||Server.Engines.Points.PointsSystem.RisingTide.GetPoints(trader)!=beforeBuy-HavenCargoExchange.Costs[choice])throw new Exception("Reward purchase failed: "+choice);
   }
   if(HavenCargoExchange.Buy(trader,-1))throw new Exception("Invalid reward accepted");
   log("PASS all six fishing/pirate rewards purchased with exact deductions");
  }finally{trader.Backpack.MaxItems=originalMax;}
  log("PASS surplus partial stack, exact doubloon credit, insufficient/replayed/unowned rejection and cargo turn-in");
  var site=World.Items.Values.OfType<HavenCoveEncounter>().First(x=>!x.Deleted);
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  for(int theme=0;theme<4;theme++)for(int stage=0;stage<4;stage++){
   var camp=new HavenCoveEncounter();camp.MoveToWorld(site.Location,site.Map);
   try{
    typeof(HavenMiniChamp).GetField("_theme",flags).SetValue(camp,theme);
    typeof(HavenMiniChamp).GetField("_stage",flags).SetValue(camp,stage);
    typeof(HavenMiniChamp).GetMethod("SpawnWave",flags).Invoke(camp,null);
    var foes=((System.Collections.Generic.List<Mobile>)typeof(HavenMiniChamp).GetField("_foes",flags).GetValue(camp)).ToArray();
    int expected=stage==3?(theme==3?3:1):(theme==3?15:5);
    if(!camp.Active||foes.Length!=expected)throw new Exception("Wrong wave count "+theme+"/"+stage+": "+foes.Length);
    foreach(var foe in foes){if(Notoriety.Compute(World.FindMobile((Serial)37901),foe)!=Notoriety.Murderer)throw new Exception("Cove enemy is not red: "+foe.Name);if(!(foe is HavenCoveEnemy)||foe.Map!=site.Map||!foe.InRange(site,7))throw new Exception("Wrong cove spawn");}
    if(theme==3&&foes.Select(f=>(int)typeof(HavenMiniEnemy).GetField("_lootTheme",flags).GetValue(f)).Distinct().Count()!=3)throw new Exception("Challenge missing loot variants");
    if(stage==3&&theme==3&&foes.Select(f=>f.Name).Distinct().Count()!=3)throw new Exception("Challenge duplicated bosses");
    log("PASS "+HavenCoveEncounter.CoveThemes[theme]+" stage "+stage+": "+foes.Length+" enemies; "+string.Join(", ",foes.Select(f=>f.Name).Distinct()));
    var boundary=foes[0];boundary.MoveToWorld(new Point3D(camp.X+11,camp.Y,camp.Z),camp.Map);
    bool outward=(bool)typeof(HavenCoveEnemy).GetMethod("OnMove",flags).Invoke(boundary,new object[]{Direction.East});
    if(outward)throw new Exception("Cove enemy can leave damage boundary");
    boundary.MoveToWorld(new Point3D(camp.X+16,camp.Y,camp.Z),camp.Map);((HavenCoveEnemy)boundary).OnThink();
    if(!boundary.InRange(camp,11))throw new Exception("Old out-of-bounds enemy not recovered");
    if(theme==3&&stage==3){
     var owner=World.FindMobile((Serial)37901);
     var before=new System.Collections.Generic.HashSet<Serial>(World.Items.Keys);
     ((System.Collections.Generic.HashSet<Mobile>)typeof(HavenMiniChamp).GetField("_participants",flags).GetValue(camp)).Add(owner);
     typeof(HavenMiniChamp).GetMethod("Win",flags).Invoke(camp,null);
     var rewards=World.Items.Values.Where(i=>!before.Contains(i.Serial)&&!i.Deleted).ToArray();
     if(rewards.OfType<Server.Items.BankCheck>().Sum(i=>i.Worth)!=40000)throw new Exception("Challenge gold mismatch");
     if(rewards.OfType<Server.Items.MaritimeCargo>().Count()!=4)throw new Exception("Missing challenge cargo");
     if(rewards.Any(i=>i is Server.Items.Cannonball||i is Server.Items.PowderCharge||i is Server.Items.FuseCord))throw new Exception("Unwanted guaranteed munitions");
     if(rewards.OfType<AstralShard>().Sum(i=>i.Amount)!=1)throw new Exception("Challenge shard mismatch");
     log("PASS challenge completion: 40,000 gold, four maritime cargo crates and one Astral Shard");
    }
   }finally{camp.Delete();}
  }
 }
}







