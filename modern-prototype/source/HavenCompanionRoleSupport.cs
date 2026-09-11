using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.SkillMasteries;
using SupportParty=Server.Engines.PartySystem.Party;
namespace Server.HavenPrototype {
 public partial class HavenCompanion {
  internal double _roleTrainingMinutes;DateTime _nextRoleMinute,_nextRoleHelp,_nextSong,_nextMastery,_nextMasterySwitch,_nextHealerPulse;
  SkillName _bardMastery=SkillName.Alchemy;readonly HashSet<Mobile> _songRecipients=new HashSet<Mobile>();Spell _masteryCast;Mobile _masteryTarget;
  public void EnsureFreeFollowerSlots(){if(Deleted||ControlSlots==0)return;if(World.Loading){Timer.DelayCall(TimeSpan.Zero,EnsureFreeFollowerSlots);return;}RemoveFollowers();ControlSlots=0;ControlSlotsMin=0;ControlSlotsMax=0;AddFollowers();}
  internal void AwardRoleHelp(){if(BoundOwner?.NetState==null||DateTime.UtcNow<_nextRoleHelp)return;_nextRoleHelp=DateTime.UtcNow.AddSeconds(10);_roleTrainingMinutes+=1;}
  internal bool HealMostUrgent(){var patients=new List<Mobile>{this};var nearby=GetMobilesInRange(12);try{foreach(Mobile m in nearby){var pet=m as BaseCreature;if(m==BoundOwner||(pet!=null&&(pet.ControlMaster==this||pet.ControlMaster==BoundOwner))||(m.Player&&SupportParty.Get(BoundOwner)?.Contains(m)==true))patients.Add(m);}}finally{nearby.Free();}foreach(var patient in patients.Distinct().OrderByDescending(Urgency))if(SupportPatient(patient))return true;return false;}
  static double Urgency(Mobile m){var pet=m as BaseCreature;if(!m.Alive||pet?.IsDeadPet==true)return 400;if(m.Hits<m.HitsMax*0.25)return 350;if(m.Poisoned)return 300;return 100*(1-m.Hits/(double)Math.Max(1,m.HitsMax));}
  internal bool HealerRecoveryPulse(){if(Role!=CompanionRole.Healer||!CanSupportRole(BoundOwner)||DateTime.UtcNow<_nextHealerPulse||Mana<30)return false;var patients=new List<Mobile>();var nearby=GetMobilesInRange(6);try{foreach(Mobile m in nearby){var pet=m as BaseCreature;bool allied=m==this||m==BoundOwner||(m.Player&&SupportParty.Get(BoundOwner)?.Contains(m)==true)||(pet!=null&&(pet.ControlMaster==this||pet.ControlMaster==BoundOwner));if(allied&&!m.Deleted&&m.Alive&&pet?.IsDeadPet!=true&&m.Hits<m.HitsMax&&!m.Poisoned&&!Server.Items.MortalStrike.IsWounded(m)&&m.Map==Map&&InLOS(m))patients.Add(m);}}finally{nearby.Free();}if(patients.Count<2&&!(patients.Contains(BoundOwner)&&BoundOwner.Hits<BoundOwner.HitsMax*0.35))return false;_nextHealerPulse=DateTime.UtcNow.AddSeconds(20);Mana-=30;foreach(var patient in patients){patient.Heal(40+(int)(Skills.Healing.Base/5),this);patient.FixedEffect(0x376A,10,16);}PlaySound(0x1F2);AwardRoleHelp();CheckSkill(SkillName.Healing,0,125);return true;}
  internal string SelectSong(Mobile patient){if(Skills.Musicianship.Value<90)return "Encouragement";if(patient.Hits<patient.HitsMax*0.6)return "Recovery";if(patient.Mana<patient.ManaMax*0.5||patient.Skills.Magery.Value>patient.Skills.Tactics.Value)return "Arcane";return patient.Weapon is BaseRanged?"Battle":"Valor";}
  internal bool BardSong(Mobile patient){if(Role!=CompanionRole.Bard||_bardMastery!=SkillName.Alchemy||Skills.Musicianship.Value<80||Skills.Peacemaking.Value<80||!CanSupportRole(patient))return false;var skill=Math.Min(Skills.Musicianship.Value,Skills.Peacemaking.Value);int amount=3+(int)((skill-80)/10)+(int)(Math.Log(1+Math.Max(0,_roleTrainingMinutes)/60)/Math.Log(2));string song=SelectSong(patient);RemoveSong(patient);patient.AddStatMod(new StatMod(StatType.Str,"HavenCompanionSongStr",amount*((song=="Recovery"||song=="Valor")?2:1),TimeSpan.FromSeconds(60)));patient.AddStatMod(new StatMod(StatType.Dex,"HavenCompanionSongDex",amount*(song=="Battle"?2:1),TimeSpan.FromSeconds(60)));patient.AddStatMod(new StatMod(StatType.Int,"HavenCompanionSongInt",amount*(song=="Arcane"?2:1),TimeSpan.FromSeconds(60)));_songRecipients.Add(patient);return true;}
  static void RemoveSong(Mobile m){m.RemoveStatMod("HavenCompanionSongStr");m.RemoveStatMod("HavenCompanionSongDex");m.RemoveStatMod("HavenCompanionSongInt");}
  internal bool CanSupportRole(Mobile target){return !Deleted&&Alive&&!IsDeadPet&&!OnMission&&!IsStabled&&target!=null&&!target.Deleted&&target.Alive&&Map!=null&&Map!=Map.Internal&&target.Map==Map&&InRange(target,12)&&InLOS(target)&&(target==this||target==BoundOwner||(target.Player&&SupportParty.Get(BoundOwner)?.Contains(target)==true));}
  internal bool BardEnemy(Mobile target){var e=target as BaseCreature;return e!=null&&e.Alive&&!e.Deleted&&!e.IsDeadPet&&!e.Controlled&&!e.Summoned&&!e.BardImmune&&e.Karma<0&&e.Map==Map&&InRange(e,10)&&InLOS(e)&&CanBeHarmful(e,false);}
  internal SkillName ChooseBardMastery(Mobile enemy){if(Skills.Musicianship.Value<90)return SkillName.Alchemy;if(Skills.Peacemaking.Value>=90&&BoundOwner!=null&&BoundOwner.Hits<BoundOwner.HitsMax*0.7)return SkillName.Peacemaking;if(!TamingAssistActive&&Skills.Discordance.Value>=90&&BardEnemy(enemy)&&enemy.HitsMax>=500)return SkillName.Discordance;if(Skills.Provocation.Value>=90)return SkillName.Provocation;return Skills.Peacemaking.Value>=90?SkillName.Peacemaking:SkillName.Alchemy;}
  bool HasSong<T>() where T:SkillMasterySpell {return SkillMasterySpell.GetSpells(this)?.Any(x=>x is T&&x.Timer!=null)==true;}
  internal bool ThinkBardMasteries(){if(TamingAssistActive){if(_bardMastery==SkillName.Discordance)ClearRoleSupport();return false;}if(Role!=CompanionRole.Bard||!CanSupportRole(BoundOwner)||Spell!=null||Target!=null||DateTime.UtcNow<_nextMastery||Mana<30)return false;_nextMastery=DateTime.UtcNow.AddSeconds(3);var enemy=(Combatant??BoundOwner.Combatant) as Mobile;var desired=ChooseBardMastery(enemy);if(desired==SkillName.Alchemy)return false;
   if(_bardMastery!=desired&&(DateTime.UtcNow>=_nextMasterySwitch||_bardMastery==SkillName.Alchemy||(TamingAssistActive&&_bardMastery==SkillName.Discordance))){ClearBardMasteries();_bardMastery=desired;_nextMasterySwitch=DateTime.UtcNow.AddSeconds(30);foreach(var p in _songRecipients)if(!p.Deleted)RemoveSong(p);_songRecipients.Clear();}
   EnsureBardTools();var lute=Backpack.FindItemByType(typeof(HavenCompanionLute),true) as HavenCompanionLute;lute.UsesRemaining=100;BaseInstrument.SetInstrument(this,lute);SkillMasterySpell next=null;
   if(_bardMastery==SkillName.Provocation)next=!HasSong<InspireSpell>()?(SkillMasterySpell)new InspireSpell(this,null):!HasSong<InvigorateSpell>()?new InvigorateSpell(this,null):null;
   else if(_bardMastery==SkillName.Peacemaking)next=!HasSong<ResilienceSpell>()?(SkillMasterySpell)new ResilienceSpell(this,null):!HasSong<PerseveranceSpell>()?new PerseveranceSpell(this,null):null;
   else if(BardEnemy(enemy))next=!HasSong<TribulationSpell>()?(SkillMasterySpell)new TribulationSpell(this,null):!HasSong<DespairSpell>()?new DespairSpell(this,null):null;
   if(next==null||!next.Cast())return false;_masteryCast=next;_masteryTarget=next.PartyEffects?this:enemy;return true;
  }
  internal void ClearBardMasteries(){foreach(var spell in SkillMasterySpell.GetSpells(this)??new List<SkillMasterySpell>())if(spell is BardSpell&&spell.Timer!=null)spell.Expire();_bardMastery=SkillName.Alchemy;}
  internal void ClearRoleSupport(){foreach(var p in _songRecipients)if(!p.Deleted)RemoveSong(p);_songRecipients.Clear();ClearBardMasteries();if(_masteryCast!=null){if(Target!=null)Target.Cancel(this,Server.Targeting.TargetCancelType.Canceled);_masteryCast.Disturb(DisturbType.Kill);}_masteryCast=null;_masteryTarget=null;}
  internal void ThinkRoleSupport(){if(BoundOwner?.NetState==null||!CanSupportRole(BoundOwner)){ClearRoleSupport();return;}if(DateTime.UtcNow>=_nextRoleMinute){_nextRoleMinute=DateTime.UtcNow.AddMinutes(1);_roleTrainingMinutes+=1;}
   if(_masteryCast!=null){if(Role!=CompanionRole.Bard||(_masteryTarget!=this&&!BardEnemy(_masteryTarget))){ClearRoleSupport();return;}if(Target!=null){var target=Target;var recipient=_masteryTarget;_masteryCast=null;_masteryTarget=null;target.Invoke(this,recipient);}else if(Spell!=_masteryCast){_masteryCast=null;_masteryTarget=null;}}
   if(Role==CompanionRole.Healer){HealerRecoveryPulse();return;}if(Role!=CompanionRole.Bard)return;ThinkBardMasteries();foreach(var p in _songRecipients.ToArray())if(!CanSupportRole(p)){if(!p.Deleted)RemoveSong(p);_songRecipients.Remove(p);}
   if(_bardMastery==SkillName.Alchemy&&DateTime.UtcNow>=_nextSong&&Mana>=5){_nextSong=DateTime.UtcNow.AddSeconds(45);bool played=BardSong(BoundOwner);var party=SupportParty.Get(this);if(party!=null)foreach(var member in party.Members)if(member.Mobile.Player)played|=BardSong(member.Mobile);if(played){Mana-=5;PlaySound(0x45);AwardRoleHelp();}}
  }
 }
 public class HavenCompanionHealerAI:HavenCompanionAI {
  readonly HavenCompanion _healer;public HavenCompanionHealerAI(HavenCompanion c):base(c){_healer=c;}
  public override bool DoActionCombat(){_healer.Combatant=null;_healer.FocusMob=null;var owner=_healer.BoundOwner;if(owner!=null&&owner.Map==_healer.Map){_healer.ControlTarget=owner;return DoOrderFollow();}return true;}
  public override bool DoOrderAttack(){return DoActionCombat();}
 }
}
