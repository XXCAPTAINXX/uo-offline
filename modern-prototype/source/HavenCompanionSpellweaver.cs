using System;
using Server;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Spells.Spellweaving;
namespace Server.HavenPrototype {
 public partial class HavenCompanion {
  DateTime _nextForm,_nextRenewal;
  public void EnsureSpellweaverSkills(){Skills.Spellweaving.Base=Math.Max(Skills.Spellweaving.Base,100);Skills.Necromancy.Base=Math.Max(Skills.Necromancy.Base,100);Skills.SpiritSpeak.Base=Math.Max(Skills.SpiritSpeak.Base,100);}
  public bool MaintainSpellweaver(){var context=TransformationSpellHelper.GetContext(this);if(Role!=CompanionRole.Caster){if(context!=null&&context.Type==typeof(WraithFormSpell))TransformationSpellHelper.RemoveContext(this,true);return false;}if(Deleted||!Alive||IsDeadPet||OnMission||IsStabled||Spell!=null||Map==null||Map==Map.Internal)return false;if(context==null&&DateTime.UtcNow>=_nextForm&&Mana>=17){_nextForm=DateTime.UtcNow.AddSeconds(8);return new WraithFormSpell(this,null).Cast();}if(Hits<HitsMax*0.8&&Mana>=24&&DateTime.UtcNow>=_nextRenewal){_nextRenewal=DateTime.UtcNow.AddSeconds(90);return new CompanionRenewal(this).Cast();}return false;}
  sealed class CompanionRenewal:GiftOfRenewalSpell {public CompanionRenewal(Mobile caster):base(caster,null){}public override void OnCast(){Target(Caster);}}
 }
 public partial class HavenCompanionMageAI {
  protected override DateTime GetCastDelay(Spell spell){return DateTime.UtcNow+(spell==null?TimeSpan.FromSeconds(0.2):spell.GetCastDelay()+spell.GetCastRecovery()+TimeSpan.FromSeconds(0.1));}
  public override Spell ChooseSpell(IDamageable target){var mobile=target as Mobile;if(mobile!=null&&mobile.Alive&&_companion.Mana>=60&&mobile.Hits<mobile.HitsMax*0.25&&_companion.InRange(mobile,10)&&_companion.InLOS(mobile))return new CompanionWord(_companion,mobile);return base.ChooseSpell(target);}
  sealed class CompanionWord:WordOfDeathSpell {readonly Mobile _target;public CompanionWord(Mobile caster,Mobile target):base(caster,null){_target=target;}public override void OnCast(){if(_target!=null&&!_target.Deleted&&_target.Alive&&Caster.InRange(_target,10)&&Caster.InLOS(_target))Target(_target);else FinishSequence();}}
 }
}
