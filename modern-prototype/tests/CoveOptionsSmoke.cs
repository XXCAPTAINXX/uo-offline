using System;
using System.Linq;
using System.Reflection;
using Server;
using Server.HavenPrototype;
public static class CoveOptionsSmoke {
 public static void Run(Action<string> log){
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
    foreach(var foe in foes){if(!(foe is HavenCoveEnemy)||foe.Map!=site.Map||!foe.InRange(site,7))throw new Exception("Wrong cove spawn");}
    if(theme==3&&foes.Select(f=>(int)typeof(HavenMiniEnemy).GetField("_lootTheme",flags).GetValue(f)).Distinct().Count()!=3)throw new Exception("Challenge missing loot variants");
    if(stage==3&&theme==3&&foes.Select(f=>f.Name).Distinct().Count()!=3)throw new Exception("Challenge duplicated bosses");
    log("PASS "+HavenCoveEncounter.CoveThemes[theme]+" stage "+stage+": "+foes.Length+" enemies; "+string.Join(", ",foes.Select(f=>f.Name).Distinct()));
    if(theme==3&&stage==3){
     var owner=World.FindMobile((Serial)37901);
     var before=new System.Collections.Generic.HashSet<Serial>(World.Items.Keys);
     ((System.Collections.Generic.HashSet<Mobile>)typeof(HavenMiniChamp).GetField("_participants",flags).GetValue(camp)).Add(owner);
     typeof(HavenMiniChamp).GetMethod("Win",flags).Invoke(camp,null);
     var rewards=World.Items.Values.Where(i=>!before.Contains(i.Serial)&&!i.Deleted).ToArray();
     if(rewards.OfType<Server.Items.BankCheck>().Sum(i=>i.Worth)!=40000)throw new Exception("Challenge gold mismatch");
     if(rewards.OfType<Server.Items.Cannonball>().Sum(i=>i.Amount)!=100)throw new Exception("Missing island challenge supplies");
     if(rewards.OfType<AstralShard>().Sum(i=>i.Amount)!=1)throw new Exception("Challenge shard mismatch");
     log("PASS challenge completion: 40,000 gold, four island supply sets and one Astral Shard");
    }
   }finally{camp.Delete();}
  }
 }
}


