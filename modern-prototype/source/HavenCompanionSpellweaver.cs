using System;
using System.Linq;
using System.Collections.Generic;
using Server.Mobiles;
using Server;
using Server.Items;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Spells.Spellweaving;
namespace Server.HavenPrototype {
 public partial class HavenCompanion {
  internal bool ValidCasterAreaTarget(Mobile target){
   var creature=target as BaseCreature;
   return Role==CompanionRole.Caster&&Alive&&!IsDeadPet&&!OnMission&&!IsStabled&&_owner!=null&&_owner.Alive&&creature!=null&&!creature.Deleted&&creature.Alive&&!creature.IsDeadPet&&!creature.Controlled&&!creature.Summoned&&(!creature.Tamable||creature.IsChampionSpawn)&&target.Map==Map&&_owner.Map==Map&&_owner.InRange(target,SupportRange)&&InRange(target,SupportRange)&&InLOS(target)&&CanSee(target)&&IsHostileNotoriety(target)&&_owner.CanBeHarmful(target,false)&&CanBeHarmful(target,false);
  }
  DateTime _nextForm,_nextRenewal,_nextOwnerRenewal,_nextLifeGift;
  public void EnsureArcaneFocus(){if(Backpack==null||Deleted)return;if(Role!=CompanionRole.Caster){foreach(var item in Backpack.FindItemsByType(typeof(HavenCompanionArcaneFocus),true))item.Delete();return;}var focus=ArcanistSpell.FindArcaneFocus(this);if(focus==null){focus=new HavenCompanionArcaneFocus();Backpack.DropItem(focus);}if(focus.StrengthBonus<6){focus.StrengthBonus=6;focus.InvalidateProperties();}if(DateTime.UtcNow-focus.CreationTime>TimeSpan.FromMinutes(5)){focus.CreationTime=DateTime.UtcNow;focus.LifeSpan=TimeSpan.FromHours(2);}}

  public void EnsureSpellweaverSkills(){Skills.Spellweaving.Base=Math.Max(Skills.Spellweaving.Base,100);Skills.Necromancy.Base=Math.Max(Skills.Necromancy.Base,100);Skills.SpiritSpeak.Base=Math.Max(Skills.SpiritSpeak.Base,100);}
  public bool MaintainSpellweaver(){EnsureArcaneFocus();var context=TransformationSpellHelper.GetContext(this);if(Role!=CompanionRole.Caster){if(context!=null&&context.Type==typeof(WraithFormSpell))TransformationSpellHelper.RemoveContext(this,true);return false;}if(Deleted||!Alive||IsDeadPet||OnMission||IsStabled||Spell!=null||Map==null||Map==Map.Internal)return false;if(context==null&&DateTime.UtcNow>=_nextForm&&Mana>=17){_nextForm=DateTime.UtcNow.AddSeconds(8);return new WraithFormSpell(this,null).Cast();}if(CanSupportRole(BoundOwner)&&BoundOwner.Hits<BoundOwner.HitsMax*0.85&&Mana>=34&&DateTime.UtcNow>=_nextOwnerRenewal){_nextOwnerRenewal=DateTime.UtcNow.AddSeconds(90);return new CompanionOwnerRenewal(this,BoundOwner).Cast();}if(CanSupportRole(BoundOwner)&&BoundOwner.Combatant==null&&Combatant==null&&Skills.Spellweaving.Value>=80&&Mana>=90&&DateTime.UtcNow>=_nextLifeGift){_nextLifeGift=DateTime.UtcNow.AddMinutes(2);return new CompanionLifeGift(this,BoundOwner).Cast();}if(Hits<HitsMax*0.8&&Mana>=24&&DateTime.UtcNow>=_nextRenewal){_nextRenewal=DateTime.UtcNow.AddSeconds(90);return new CompanionRenewal(this).Cast();}return false;}
  sealed class CompanionOwnerRenewal:GiftOfRenewalSpell {readonly Mobile _patient;public CompanionOwnerRenewal(HavenCompanion c,Mobile p):base(c,null){_patient=p;}public override void OnCast(){if(((HavenCompanion)Caster).Role==CompanionRole.Caster&&((HavenCompanion)Caster).CanSupportRole(_patient))Target(_patient);else FinishSequence();}}
  sealed class CompanionLifeGift:GiftOfLifeSpell {readonly Mobile _patient;public CompanionLifeGift(HavenCompanion c,Mobile p):base(c,null){_patient=p;}public override void OnCast(){if(((HavenCompanion)Caster).Role==CompanionRole.Caster&&((HavenCompanion)Caster).CanSupportRole(_patient))Target(_patient);else FinishSequence();}}
  sealed class CompanionRenewal:GiftOfRenewalSpell {public CompanionRenewal(Mobile caster):base(caster,null){}public override void OnCast(){Target(Caster);}}
 }
 public class HavenCompanionArcaneFocus:ArcaneFocus {
  public HavenCompanionArcaneFocus():base(TimeSpan.FromHours(2),6){Name="Companion's arcane focus";Movable=false;Weight=0;}
  public HavenCompanionArcaneFocus(Serial serial):base(serial){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Movable=false;}
 }
 public partial class HavenCompanionMageAI {
  protected override DateTime GetCastDelay(Spell spell){return DateTime.UtcNow+(spell==null?TimeSpan.FromSeconds(0.2):spell.GetCastDelay()+spell.GetCastRecovery()+TimeSpan.FromSeconds(0.1));}
  internal DateTime _nextWildfire;
  const int AutoManaReserve=40;
  bool CanAfford(ArcanistSpell spell){return _companion.Mana>=spell.ScaleMana(spell.RequiredMana)+AutoManaReserve;}
  internal int NearbyAreaEnemies(int range){return HavenPetSignatures.NearbyCreatures(_companion.Map,_companion.Location,range).Count(c=>_companion.ValidCasterAreaTarget(c));}
  public override Spell ChooseSpell(IDamageable target){
   if(_companion.Role!=CompanionRole.Caster||!_companion.Alive||_companion.IsDeadPet||_companion.OnMission||_companion.IsStabled)return null;
   int focus=ArcanistSpell.GetFocusLevel(_companion);
   if(_companion.Skills.Spellweaving.Value>=66&&NearbyAreaEnemies(5+focus)>0&&DateTime.UtcNow>=_nextWildfire){
    var fire=new CompanionWildfire(_companion,this);return CanAfford(fire)?fire:null;
   }
   if(_companion.Skills.Spellweaving.Value>=10&&NearbyAreaEnemies(3+focus)>0){
    var storm=new CompanionThunderstorm(_companion);return CanAfford(storm)?storm:null;
   }
   return base.ChooseSpell(target);
  }
  internal sealed class CompanionWildfire:WildfireSpell {
   readonly HavenCompanion _owner;readonly HavenCompanionMageAI _ai;
   public CompanionWildfire(HavenCompanion c,HavenCompanionMageAI ai):base(c,null){_owner=c;_ai=ai;}
   public override IEnumerable<IDamageable> AcquireIndirectTargets(IPoint3D p,int range){return base.AcquireIndirectTargets(p,range).Where(x=>_owner.ValidCasterAreaTarget(x as Mobile));}
   public override bool CheckSequence(){if(!base.CheckSequence())return false;double duration=Math.Max(1,_owner.Skills.Spellweaving.Value/24)+FocusLevel;_ai._nextWildfire=DateTime.UtcNow.AddSeconds(Math.Max(1,Math.Floor(duration)-3.75));return true;}
   public override void OnCast(){if(_owner.Role==CompanionRole.Caster&&!_owner.OnMission&&_owner.Alive&&!_owner.IsDeadPet&&_ai.NearbyAreaEnemies(5+FocusLevel)>0)Target(_owner.Location);else FinishSequence();}
  }
  internal sealed class CompanionThunderstorm:ThunderstormSpell {
   readonly HavenCompanion _owner;public CompanionThunderstorm(HavenCompanion c):base(c,null){_owner=c;}
   public override IEnumerable<IDamageable> AcquireIndirectTargets(IPoint3D p,int range){return base.AcquireIndirectTargets(p,range).Where(x=>_owner.ValidCasterAreaTarget(x as Mobile));}
   public override void OnCast(){if(_owner.Role==CompanionRole.Caster&&!_owner.OnMission&&_owner.Alive&&!_owner.IsDeadPet)base.OnCast();else FinishSequence();}
  }
 }
}
