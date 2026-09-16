using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Server.Mobiles;
namespace Server.HavenPrototype
{
 internal static class HavenPetAbilityPractice
 {
  sealed class Clock {public DateTime Next;}
  static readonly ConditionalWeakTable<BaseCreature,Clock> Clocks=new ConditionalWeakTable<BaseCreature,Clock>();
  internal static bool Attempt(BaseCreature pet,SkillName action,Mobile target){return AttemptAt(pet,action,target,DateTime.UtcNow);}
  internal static bool AttemptAt(BaseCreature pet,SkillName action,Mobile target,DateTime now)
  {
   if(!HavenPreview.Enabled||!HavenLegendaryInnates.Active(pet))return false;
   var record=HavenLegendaryPetSkills.Find(pet);
   if(record==null||!HavenLegendaryInnates.Granted(record.Boosted).Contains(action))return false;
   if(action==SkillName.Healing){if(!pet.IsHealing)return false;}
   else if(!HavenLegendaryInnates.Enemy(pet,target)||pet.ControlOrder==OrderType.Stop||pet.ControlOrder==OrderType.Stay)return false;
   var clock=Clocks.GetValue(pet,p=>new Clock());if(now<clock.Next)return false;clock.Next=now.AddSeconds(6);
   var allowed=HavenLegendaryInnates.RequiredSkills(record.Boosted);
   foreach(var name in HavenLegendaryInnates.RequiredSkills(new[]{action}).Where(allowed.Contains))
   {
    var skill=pet.Skills[name];if(skill.Lock!=SkillLock.Up||skill.Base>=skill.Cap)continue;
    // Native probabilistic gains, scaled to an actual attempt's current difficulty.
    pet.CheckSkill(name,skill.Value-25,skill.Value+25);
   }
   return true;
  }
 }
 internal sealed class HavenPetTrainingThunderstorm:Server.Spells.Spellweaving.ThunderstormSpell
 {
  public HavenPetTrainingThunderstorm(BaseCreature pet):base(pet,null){}
  public override System.Collections.Generic.IEnumerable<IDamageable> AcquireIndirectTargets(IPoint3D point,int range)
  {return base.AcquireIndirectTargets(point,range).Where(t=>t is Mobile&&HavenLegendaryInnates.Enemy(Caster as BaseCreature,(Mobile)t));}
 }

}
