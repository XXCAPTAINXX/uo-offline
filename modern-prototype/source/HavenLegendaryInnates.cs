using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.SkillHandlers;
using Server.Spells;
namespace Server.HavenPrototype {
 public static class HavenLegendaryInnates {
  internal static readonly SkillName[] Actions={SkillName.Healing,SkillName.Discordance,SkillName.Provocation,SkillName.Peacemaking,SkillName.Magery,SkillName.Necromancy,SkillName.Mysticism,SkillName.Spellweaving,SkillName.Chivalry,SkillName.Bushido,SkillName.Ninjitsu,SkillName.Poisoning,SkillName.DetectHidden,SkillName.Hiding};
  public static HashSet<SkillName> Granted(IEnumerable<SkillName> rolls){
   var skills=new HashSet<SkillName>(rolls);
   if(skills.Contains(SkillName.EvalInt))skills.Add(SkillName.Magery);
   if(skills.Contains(SkillName.SpiritSpeak))skills.Add(SkillName.Necromancy);
   if(skills.Contains(SkillName.Musicianship)&&!skills.Overlaps(new[]{SkillName.Discordance,SkillName.Peacemaking,SkillName.Provocation}))skills.Add(SkillName.Discordance);
   return skills;
  }
  public static HashSet<SkillName> RequiredSkills(IEnumerable<SkillName> rolls){
   var skills=Granted(rolls);
   if(skills.Overlaps(new[]{SkillName.Discordance,SkillName.Peacemaking,SkillName.Provocation}))skills.Add(SkillName.Musicianship);
   if(skills.Contains(SkillName.Magery)){skills.Add(SkillName.EvalInt);skills.Add(SkillName.Meditation);}
   if(skills.Contains(SkillName.Necromancy))skills.Add(SkillName.SpiritSpeak);
   if(skills.Contains(SkillName.Mysticism))skills.Add(SkillName.Focus);
   if(skills.Contains(SkillName.Healing))skills.Add(SkillName.Anatomy);
   if(skills.Contains(SkillName.Ninjitsu)){skills.Add(SkillName.Hiding);skills.Add(SkillName.Stealth);}
   if(skills.Contains(SkillName.Hiding))skills.Add(SkillName.Stealth);
   if(skills.Contains(SkillName.Bushido))skills.Add(SkillName.Parry);
   return skills;
  }
  public static void Repair(BaseCreature pet,IEnumerable<SkillName> rolls){if(pet.Backpack==null)pet.AddItem(new Backpack());foreach(var skill in RequiredSkills(rolls))Raise(pet,skill);}
  static void Raise(BaseCreature pet,SkillName name){var skill=pet.Skills[name];skill.Cap=Math.Max(100,skill.Cap);skill.Base=Math.Max(100,skill.Base);}
  public static string Describe(IEnumerable<SkillName> rolls){return "Free innate use: "+string.Join(", ",Granted(rolls).Where(s=>Actions.Contains(s)).Select(s=>s.ToString()))+". Supporting skills start at 100 in the wild and lose levels on taming; train them through use. Passive rolls work through normal combat rules. No training points or ability slots spent.";}
  internal static bool Active(BaseCreature pet){return pet!=null&&!pet.Deleted&&pet.Controlled&&!pet.Summoned&&pet.Alive&&!pet.IsDeadPet&&!pet.IsStabled&&!pet.Frozen&&!pet.Paralyzed&&pet.ControlMaster!=null&&pet.ControlMaster.Alive&&pet.Map!=null&&pet.Map!=Map.Internal&&pet.ControlMaster.Map==pet.Map&&pet.InRange(pet.ControlMaster,18)&&(!(pet is BaseMount)||((BaseMount)pet).Rider==null);}
  internal static bool Enemy(BaseCreature pet,Mobile enemy){var c=enemy as BaseCreature;return Active(pet)&&c!=null&&!c.Deleted&&c.Alive&&!c.IsDeadPet&&!c.Controlled&&!c.Summoned&&!c.Blessed&&!(c is BaseVendor)&&c.Map==pet.Map&&pet.InRange(c,10)&&pet.InLOS(c)&&pet.CanBeHarmful(c,false)&&pet.ControlMaster.CanBeHarmful(c,false)&&(pet.Combatant==c||pet.ControlMaster.Combatant==c||c.Combatant==pet||c.Combatant==pet.ControlMaster);}
  public static bool TryUse(BaseCreature pet,SkillName skill){
   if(!Active(pet)||pet.Spell!=null||pet.Target!=null)return false;
   if(skill==SkillName.Healing)return pet.CheckHeal();
   if(skill==SkillName.DetectHidden){if(!pet.UseSkill(skill))return false;if(pet.Target!=null)pet.Target.Invoke(pet,pet.Location);return true;}
   if(skill==SkillName.Hiding){return pet.Hits<pet.HitsMax/3&&!pet.Hidden&&pet.UseSkill(skill);}
   if(pet.ControlOrder==OrderType.Stay||pet.ControlOrder==OrderType.Stop)return false;
   var enemy=(pet.Combatant??pet.ControlMaster.Combatant) as Mobile;
   if(!Enemy(pet,enemy))return false;
   if(skill==SkillName.Poisoning){
    if(!pet.InRange(enemy,1)||enemy.Poisoned||!pet.CheckSkill(skill,0,150))return false;
    pet.DoHarmful(enemy);enemy.ApplyPoison(pet,pet.Skills.Poisoning.Value>=120?Poison.Lethal:Poison.Deadly);return true;
   }
   if(skill==SkillName.Discordance||skill==SkillName.Peacemaking||skill==SkillName.Provocation){
    var target=(BaseCreature)enemy;if(target.BardImmune)return false;
    int effect=0;if(skill==SkillName.Discordance&&Discordance.GetEffect(enemy,ref effect))return false;
    if(skill==SkillName.Peacemaking&&(target.BardPacified||pet.ControlMaster.Hits>=pet.ControlMaster.HitsMax*0.6))return false;
    BaseCreature other=null;if(skill==SkillName.Provocation){
     if(target.BardProvoked||target.Unprovokable)return false;
     other=HavenPetSignatures.NearbyCreatures(pet.Map,pet.Location,10).FirstOrDefault(c=>c!=target&&Enemy(pet,c)&&!c.BardImmune&&!c.Unprovokable&&!c.BardProvoked);if(other==null)return false;
    }
    var lute=pet.Backpack.FindItemByType(typeof(HavenCompanionLute),true) as HavenCompanionLute;if(lute==null){lute=new HavenCompanionLute();pet.Backpack.DropItem(lute);}lute.UsesRemaining=100;BaseInstrument.SetInstrument(pet,lute);
    pet.CheckSkill(SkillName.Musicianship,0,pet.Skills.Musicianship.Cap);
    if(skill==SkillName.Discordance)Discordance.OnPickedInstrument(pet,lute);else if(skill==SkillName.Peacemaking)Peacemaking.OnPickedInstrument(pet,lute);else Provocation.OnPickedInstrument(pet,lute);
    var first=pet.Target;if(first!=null)first.Invoke(pet,enemy);
    if(other!=null&&pet.Target!=null&&pet.Target!=first)pet.Target.Invoke(pet,other);
    return true;
   }
   Spell spell=null;
   switch(skill){
    case SkillName.Magery:spell=new Server.Spells.Sixth.EnergyBoltSpell(pet,null);break;
    case SkillName.Necromancy:spell=new Server.Spells.Necromancy.PainSpikeSpell(pet,null);break;
    case SkillName.Mysticism:spell=new Server.Spells.Mysticism.EagleStrikeSpell(pet,null);break;
    case SkillName.Spellweaving:spell=new Server.Spells.Spellweaving.WordOfDeathSpell(pet,null);break;
    case SkillName.Chivalry:spell=new Server.Spells.Chivalry.DivineFurySpell(pet,null);break;
    case SkillName.Bushido:spell=new Server.Spells.Bushido.Confidence(pet,null);break;
    case SkillName.Ninjitsu:spell=new Server.Spells.Ninjitsu.MirrorImage(pet,null);break;
   }
   if(spell==null||!spell.Cast())return false;
   int attempts=0;Timer timer=null;timer=Timer.DelayCall(TimeSpan.FromMilliseconds(50),TimeSpan.FromMilliseconds(50),()=>{
    if(!Active(pet)||++attempts>160){timer.Stop();return;}
    if(pet.Target!=null){if(Enemy(pet,enemy))pet.Target.Invoke(pet,enemy);else pet.Target.Cancel(pet,Server.Targeting.TargetCancelType.Canceled);timer.Stop();}
    else if(pet.Spell!=spell)timer.Stop();
   });return true;
  }
 }
}


