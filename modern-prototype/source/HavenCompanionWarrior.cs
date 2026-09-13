using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
namespace Server.HavenPrototype {
 public partial class HavenCompanion {
  DateTime _nextWarriorSweep;
  internal bool WarriorReady {get{return Role==CompanionRole.Warrior&&Alive&&!IsDeadPet&&!OnMission&&!IsStabled&&CanSupportRole(BoundOwner);}}
  internal bool WarriorEnemy(Mobile target){
   var enemy=target as BaseCreature;
   if(enemy==null||enemy.Deleted||!enemy.Alive||enemy.IsDeadPet||enemy.Controlled||enemy.Summoned||enemy.Map!=Map||!CanBeHarmful(enemy,false))return false;
   var victim=enemy.Combatant as Mobile;var pet=victim as BaseCreature;
   return enemy==Combatant||enemy==BoundOwner?.Combatant||victim==this||victim==BoundOwner||(pet!=null&&pet.Controlled&&(pet.ControlMaster==this||pet.ControlMaster==BoundOwner));
  }
  public override void AlterMeleeDamageTo(Mobile to,ref int damage){base.AlterMeleeDamageTo(to,ref damage);if(WarriorReady&&WarriorEnemy(to))damage=(int)Math.Min(int.MaxValue,Math.Ceiling(damage*1.25));}
  public override void AlterMeleeDamageFrom(Mobile from,ref int damage){base.AlterMeleeDamageFrom(from,ref damage);if(WarriorReady)damage=Math.Max(0,(int)Math.Ceiling(damage*0.85));}
  public override void OnGaveMeleeAttack(Mobile defender){
   base.OnGaveMeleeAttack(defender);
   if(!WarriorReady||!WarriorEnemy(defender)||DateTime.UtcNow<_nextWarriorSweep||Weapon is BaseRanged)return;
   _nextWarriorSweep=DateTime.UtcNow.AddSeconds(4);
   var targets=new List<Mobile>();var nearby=GetMobilesInRange(2);
   try{foreach(Mobile m in nearby)if(m!=defender&&WarriorEnemy(m)&&InLOS(m))targets.Add(m);}finally{nearby.Free();}
   if(targets.Count==0)return;
   FixedEffect(0x3728,10,15);PlaySound(0x23B);
   int damage=12+(int)(Math.Min(150,Skills.Tactics.Value)/5);
   foreach(var enemy in targets){if(!WarriorEnemy(enemy)||!InRange(enemy,2)||!InLOS(enemy))continue;DoHarmful(enemy);AOS.Damage(enemy,this,damage,100,0,0,0,0);}
  }
 }
}
