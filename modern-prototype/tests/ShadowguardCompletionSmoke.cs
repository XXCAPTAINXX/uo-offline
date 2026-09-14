using System;
using System.Linq;
using Server;
using Server.Mobiles;
using Server.Engines.Shadowguard;
public static class ShadowguardCompletionSmoke
{
 private class FastRoom : BarEncounter
 {
  public int Resets;
  public FastRoom(ShadowguardInstance instance):base(instance){}
  public override TimeSpan ResetDuration {get{return TimeSpan.FromSeconds(1);}}
  public override void Reset(bool expired=false){Resets++;}
 }
 public static void Run(Action<string> log,Action done)
 {
  foreach(var room in new ShadowguardEncounter[]{new BarEncounter(),new OrchardEncounter(),new ArmoryEncounter(),new FountainEncounter(),new BelfryEncounter(),new RoofEncounter()})
   if(room.ResetDuration!=TimeSpan.FromMinutes(5))throw new Exception("Wrong grace period: "+room.Encounter);
  log("PASS all five rooms and Roof allow five minutes after completion");
  var controller=ShadowguardController.Instance;var instance=controller.Instances.First(i=>!i.IsRoof&&!i.InUse);
  var owner=new PlayerMobile{Player=true};owner.MoveToWorld(instance.Center,Map.TerMur);
  var test=new FastRoom(instance){HasBegun=true};test.Participants.Add(owner);controller.AddEncounter(test);
  test.CompleteEncounter();test.CompleteEncounter();
  if(!test.Completed||!controller.HasCompletedEncounter(owner,EncounterType.Bar)||test.Resets!=0)throw new Exception("Immediate credit / premature reset");
  owner.MoveToWorld(controller.KickLocation,Map.TerMur);
  if(!controller.HasCompletedEncounter(owner,EncounterType.Bar))throw new Exception("Credit lost on departure");
  log("PASS completion credit granted immediately and retained after leaving the room");
  Timer.DelayCall(TimeSpan.FromSeconds(2),()=>{if(test.Resets!=1)log("FAIL expected one delayed reset, got "+test.Resets);else log("PASS completion remains delayed and repeated completion does not schedule duplicate resets");done();});
 }
}
