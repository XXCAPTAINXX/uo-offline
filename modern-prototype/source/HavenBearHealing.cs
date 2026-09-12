using System;
using Server.Mobiles;
namespace Server.HavenPrototype {
 public partial class HavenSnowBear {
  DateTime _nextNativeHeal;bool _nativeHealing;
  void EnsureNativeHealing(){Skills.Healing.Cap=Math.Max(120,Skills.Healing.Cap);Skills.Healing.Base=Math.Max(110,Skills.Healing.Base);Skills.Anatomy.Base=Math.Max(90,Skills.Anatomy.Base);}
  internal void SupportHealing(){
   if(!Controlled||IsDeadPet||!Alive||Rider!=null||_nativeHealing||DateTime.UtcNow<_nextNativeHeal)return;
   Mobile patient=ControlMaster;
   if(patient==null||!patient.Alive||patient.Map!=Map||!InRange(patient,12)||!InLOS(patient)||(!patient.Poisoned&&patient.Hits==patient.HitsMax))patient=this;
   if(patient.Poisoned||patient.Hits<patient.HitsMax)HealStart(patient);
  }
  public override void HealStart(Mobile patient){
   if(_nativeHealing||DateTime.UtcNow<_nextNativeHeal||!Controlled||IsDeadPet||!Alive||Rider!=null||patient==null||!patient.Alive||(patient!=this&&patient!=ControlMaster)||patient.Map!=Map||!InRange(patient,12)||!InLOS(patient))return;
   _nativeHealing=true;_nextNativeHeal=DateTime.UtcNow.AddSeconds(8);
   Timer.DelayCall(TimeSpan.FromSeconds(2),()=>{_nativeHealing=false;if(Deleted||!Alive||IsDeadPet||!Controlled||Rider!=null||patient.Deleted||!patient.Alive||(patient!=this&&patient!=ControlMaster)||patient.Map!=Map||!InRange(patient,12)||!InLOS(patient))return;base.Heal(patient);});
  }
 }
}
