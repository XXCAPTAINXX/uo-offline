using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.SkillHandlers;
namespace Server.HavenPrototype
{
 public class HavenCompanionLute:Lute
 {
  public HavenCompanionLute(){Name="Companion's lute";Movable=false;LootType=LootType.Blessed;}
  public HavenCompanionLute(Serial serial):base(serial){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);}
  public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public partial class HavenCompanion
 {
  BaseCreature _tamingTarget;bool _calmingAnimal;DateTime _nextTamingPeace,_nextTamingAttempt,_nextBardCombat;
  internal bool TamingAssistActive {get{return _tamingTarget!=null;}}
  internal void EnsureBardTools(){if(Backpack==null)return;foreach(var name in new[]{SkillName.Musicianship,SkillName.Peacemaking,SkillName.Discordance,SkillName.Provocation})if(Skills[name].Base<75)Skills[name].Base=75;if(Backpack.FindItemByType(typeof(HavenCompanionLute),true)==null)Backpack.DropItem(new HavenCompanionLute());}
  public static void InitializeTamingEvents(){EventSink.TameCreature+=e=>{var c=e.Mobile as HavenCompanion;var animal=e.Creature as BaseCreature;if(c!=null&&animal!=null)c.FinishAssistedTame(animal);};}
  internal bool ContinuingAssistedTame(BaseCreature animal){return !Deleted&&!IsDeadPet&&Alive&&!OnMission&&_tamingTarget==animal&&BoundOwner!=null&&!BoundOwner.Deleted&&BoundOwner.Alive&&BoundOwner.Map==Map&&InRange(BoundOwner,18)&&Role==CompanionRole.Bard;}
  internal static bool ActivePeace(BaseCreature animal){return animal!=null&&animal.BardPacified&&animal.BardEndTime>DateTime.UtcNow;}
  internal bool CalmTamingTarget(BaseCreature animal){return ContinuingAssistedTame(animal)&&ActivePeace(animal);}
  string _tamingStatus;
  void TamingStatus(string status){if(_tamingStatus==status)return;_tamingStatus=status;if(BoundOwner!=null&&!BoundOwner.Deleted)BoundOwner.SendMessage("Tame assist: "+status);}
  internal bool StartTamingAssist(Mobile owner,BaseCreature animal){
   if(!CanCommand(owner)||Role!=CompanionRole.Bard||animal==null||animal.Deleted||!animal.Alive||!animal.Tamable||animal.Controlled||animal.Summoned||animal.BardImmune||animal.Map!=Map||!InRange(animal,12)||!InLOS(animal))return false;
   if(Skills.AnimalTaming.Value<animal.CurrentTameSkill||Skills.AnimalLore.Value<animal.CurrentTameSkill||Followers+animal.ControlSlots>FollowersMax){owner.SendMessage("I need higher Taming/Lore or more free follower slots.");return false;}
   if((Female&&!animal.AllowFemaleTamer)||(!Female&&!animal.AllowMaleTamer)||(animal is CuSidhe&&Race!=Race.Elf)){owner.SendMessage("This creature's tamer restrictions prevent me from taming it.");return false;}
   if(animal.Owners.Count>=BaseCreature.MaxOwners&&!animal.Owners.Contains(this)){owner.SendMessage("This creature has had too many owners to accept me.");return false;}
   if(_bardMastery!=SkillName.Peacemaking)ClearRoleSupport();
   var casting=Spell as Server.Spells.Spell;if(casting!=null)casting.Disturb(Server.Spells.DisturbType.Kill);
   if(Target!=null)Target.Cancel(this,TargetCancelType.Canceled);_masteryCast=null;_masteryTarget=null;
   EnsureBardTools();_tamingTarget=animal;_nextTamingPeace=_nextTamingAttempt=DateTime.UtcNow;Combatant=null;FocusMob=null;
   ControlOrder=OrderType.Follow;ControlTarget=animal;_tamingStatus=null;TamingStatus("approaching "+animal.Name+".");return true;
  }
  internal void StopTamingAssist(){_tamingTarget=null;_tamingStatus=null;}
  internal bool FinishAssistedTame(BaseCreature animal){if(!ContinuingAssistedTame(animal)||animal.ControlMaster!=this||Backpack==null)return false;var book=HavenPetBook.Ensure(BoundOwner);var ticket=book==null?null:HavenPetTicket.Store(animal,BoundOwner,book);if(ticket==null)Backpack.DropItem(new HavenCompanionAssignedPet{Companion=this,Owner=BoundOwner,Pet=animal,AutoMount=false});StopTamingAssist();ControlOrder=OrderType.Follow;ControlTarget=BoundOwner;BoundOwner.SendMessage(ticket==null?"Taming succeeded, but the pet book could not accept it. Reclaim the pet using Companion pets.":"Taming succeeded. Your pet is in [petbook, ready to claim, inspect or exchange.");return true;}
  internal void ThinkTamingAssist(){
   var animal=_tamingTarget;if(animal==null)return;
   if(!ContinuingAssistedTame(animal)||animal.Deleted||!animal.Alive||!animal.Tamable||animal.Map!=Map||!InRange(animal,18)){TamingStatus("stopped: stay nearby with a living, reachable wild animal.");StopTamingAssist();return;}
   if(animal.Controlled){if(animal.ControlMaster==this)FinishAssistedTame(animal);else {TamingStatus("stopped: the animal has another owner.");StopTamingAssist();}return;}
   Combatant=null;FocusMob=null;ControlOrder=OrderType.Follow;ControlTarget=animal;
   if(AnimalTaming.MustBeSubdued(animal)){TamingStatus("weaken this animal to 10% health first; I will resume automatically.");return;}
   if(!InRange(animal,3)||!InLOS(animal)){TamingStatus("moving into taming range; keep the route clear.");return;}
   if(Spell!=null||Target!=null)return;
   if(ActivePeace(animal)){TryAssistedTaming();return;}
   if(DateTime.UtcNow<_nextTamingPeace)return;
   if(animal.BardPacified&&animal.BardEndTime<=DateTime.UtcNow)animal.BardPacified=false;
   EnsureBardTools();var lute=Backpack.FindItemByType(typeof(HavenCompanionLute),true) as HavenCompanionLute;
   if(lute==null){TamingStatus("waiting for an instrument in my pack.");return;}
   _nextTamingPeace=DateTime.UtcNow.AddSeconds(3);lute.UsesRemaining=100;_calmingAnimal=true;
   try{TamingStatus("calming the animal.");CheckSkill(SkillName.Musicianship,0,Skills.Musicianship.Cap);Peacemaking.OnPickedInstrument(this,lute);if(Target!=null)Target.Invoke(this,animal);if(ActivePeace(animal))TryAssistedTaming();}finally{_calmingAnimal=false;}
  }
  void TryAssistedTaming(){
   var animal=_tamingTarget;if(animal==null||!ActivePeace(animal)||DateTime.UtcNow<_nextTamingAttempt||!InRange(animal,3)||!InLOS(animal))return;
   if(AnimalTaming.IsBeingTamed(animal))return;
   _nextTamingAttempt=DateTime.UtcNow.AddSeconds(3);
   TamingStatus("animal calmed; attempting tame. Failed skill rolls retry automatically.");
   AnimalTaming.BeginAssistedTame(this,animal);
  }
  public void RequestTamingAssist(Mobile owner){if(!IsOwner(owner))return;if(ShowAwayTimer(owner))return;if(TamingAssistActive){StopTamingAssist();owner.SendMessage("Taming assistance stopped.");return;}if(Role!=CompanionRole.Bard){owner.SendMessage("Choose the Bard role before using Tame assist.");return;}owner.Target=new AssistTarget(this);owner.SendMessage("Target a wild animal. I will peace it, tame it and return the actual animal as a claim ticket.");}
  class AssistTarget:Target{readonly HavenCompanion _c;public AssistTarget(HavenCompanion c):base(12,false,TargetFlags.None){_c=c;}protected override void OnTarget(Mobile owner,object target){if(!(target is BaseCreature)||!_c.StartTamingAssist(owner,(BaseCreature)target))owner.SendMessage("Cannot assist: check role, distance, tameability, Taming/Lore and free follower slots.");}}
  public override bool IsHarmfulCriminal(IDamageable target){if(_calmingAnimal&&target==_tamingTarget)return false;return base.IsHarmfulCriminal(target);}
  void ThinkBardCombat(){if(Role!=CompanionRole.Bard||TamingAssistActive||Spell!=null||Target!=null||IsDeadPet||!Alive||BoundOwner==null||DateTime.UtcNow<_nextBardCombat)return;var enemy=(BoundOwner.Combatant??Combatant) as BaseCreature;if(enemy==null||enemy.Deleted||!enemy.Alive||enemy.Controlled||enemy.Summoned||enemy.BardImmune||enemy.Karma>=0||enemy.Map!=Map||!InRange(enemy,10)||!InLOS(enemy)||!CanBeHarmful(enemy,false))return;EnsureBardTools();var lute=Backpack.FindItemByType(typeof(HavenCompanionLute),true) as HavenCompanionLute;lute.UsesRemaining=100;_nextBardCombat=DateTime.UtcNow.AddSeconds(3);if(Skills.Peacemaking.Value>=75&&BoundOwner.Hits<BoundOwner.HitsMax*0.4&&!enemy.BardPacified){CheckSkill(SkillName.Musicianship,0,Skills.Musicianship.Cap);Peacemaking.OnPickedInstrument(this,lute);if(Target!=null)Target.Invoke(this,enemy);if(enemy.BardPacified){Combatant=null;ControlTarget=BoundOwner;ControlOrder=OrderType.Follow;}return;}if(Skills.Provocation.Value>=75&&!enemy.BardProvoked&&!enemy.Unprovokable){foreach(var other in HavenPetSignatures.NearbyCreatures(Map,Location,10)){if(other==enemy||other.Unprovokable||other.BardProvoked||other.BardImmune||other.Controlled||other.Summoned||other.Karma>=0||(other.Combatant!=BoundOwner&&other.Combatant!=this)||!CanBeHarmful(other,false))continue;CheckSkill(SkillName.Musicianship,0,Skills.Musicianship.Cap);Provocation.OnPickedInstrument(this,lute);var first=Target;if(first!=null)first.Invoke(this,enemy);if(Target!=null&&Target!=first)Target.Invoke(this,other);return;}}int effect=0;if(!Discordance.GetEffect(enemy,ref effect)){Discordance.OnPickedInstrument(this,lute);if(Target!=null)Target.Invoke(this,enemy);}}
 }
 public static class HavenTamingEvents {public static void Initialize(){HavenCompanion.InitializeTamingEvents();}}
}
