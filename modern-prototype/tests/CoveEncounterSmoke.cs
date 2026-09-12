using System;
using System.IO;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Accounting;
using Server.HavenPrototype;
public static class CoveEncounterSmoke {
 public static void Run(){
  var stones=World.Items.Values.OfType<HavenServiceStone>().Where(s=>s.X>3900).ToArray();var positions=stones.Select(s=>s.Location).ToArray();HavenStarterHub.Ensure();if(stones.Where((s,i)=>s.Deleted||s.Location!=positions[i]).Any())throw new Exception("Plaza maintenance moved island services");
  var haven=HavenMiniChamp.Find();var camp=HavenCoveEncounter.BuildTest();
  if(HavenMiniChamp.Find()!=haven)throw new Exception("Cove displaced Haven camp lookup");
  var owner=new PlayerMobile{Player=true,Body=0x190,RawStr=250};owner.AddItem(new Backpack());var account=new Account("cove-"+Guid.NewGuid().ToString("N"),Guid.NewGuid().ToString("N"));account[0]=owner;owner.MoveToWorld(camp.Location,camp.Map);
  var pet=new Horse();pet.SetControlMaster(owner);pet.MoveToWorld(owner.Location,owner.Map);
  if(!camp.Begin(owner,1)||camp.Remaining!=5)throw new Exception("Cove cannot start five-enemy wave");
  using(var stream=new MemoryStream()){var writer=new BinaryFileWriter(stream,true);camp.Serialize(writer);writer.Flush();stream.Position=0;camp.Deserialize(new BinaryFileReader(new BinaryReader(stream)));}if(!camp.Active||camp.Stage!=0||camp.Remaining!=5)throw new Exception("Cove active state serialization failed");
  var spawn=typeof(HavenMiniChamp).GetMethod("SpawnWave",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
  for(int stage=0;stage<4;stage++){
   var foes=World.Mobiles.Values.OfType<HavenCoveEnemy>().Where(m=>!m.Deleted).ToArray();
   if(camp.Stage!=stage||foes.Length!=(stage==3?1:5))throw new Exception("Incorrect cove wave "+stage);
   camp.Credit(foes[0],pet,1);
   foreach(var foe in foes)foe.Kill();
   if(stage<3)spawn.Invoke(camp,null);
  }
  if(camp.Active||HavenMarks.Balance(owner)!=20||HavenMiniChamp.Wins(owner)!=1)throw new Exception("Missing cove participant rewards");
  if(owner.Backpack.FindItemByType(typeof(Cannonball),true)==null||owner.Backpack.FindItemByType(typeof(PowderCharge),true)==null||owner.Backpack.FindItemByType(typeof(FuseCord),true)==null)throw new Exception("Missing island ship rewards");
  if(camp.Begin(owner,1))throw new Exception("Cove cooldown bypassed");
  File.AppendAllText("island-foundation.log","PASS cove waves/boss/pet credit/Marks/ship supplies/cooldown/serialization/Haven and service isolation\n");
  pet.Delete();camp.Delete();owner.Delete();
 }
}
