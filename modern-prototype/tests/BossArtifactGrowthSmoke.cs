using System;
using Server;
using Server.Items;
using Server.HavenPrototype;
public static class BossArtifactGrowthSmoke
{
 public static void Run(Action<string> log)
 {
  foreach(var type in HavenAdvancedGear.BossArtifacts)
  {
   var item=(Item)Activator.CreateInstance(type);var a=HavenAdvancedGear.Attributes(item);int damage=a.WeaponDamage,spell=a.SpellDamage,hp=a.RegenHits,mana=a.RegenMana;var weapon=item as BaseWeapon;int speed=a.WeaponSpeed,leech=weapon==null?0:weapon.WeaponAttributes.HitLeechMana;
   if(HavenAdvancedGear.AutoKind(item)!=7)throw new Exception("Not recognized "+type.Name);
   var record=HavenAdvancedGear.Attach(item,7);record.Gain(1900);
   if(record.Level!=20||a.WeaponDamage!=damage+19||a.SpellDamage!=spell+19||a.RegenHits!=hp+4||a.RegenMana!=mana+4)throw new Exception("Growth mismatch "+type.Name);
   if(weapon!=null&&(a.WeaponSpeed!=speed+8||weapon.WeaponAttributes.HitLeechMana!=Math.Max(100,Math.Max(20,leech)+12)))throw new Exception("Weapon growth "+type.Name);
   record.Apply();record.Gain(100);if(a.WeaponDamage!=damage+19||a.SpellDamage!=spell+19||HavenAdvancedGear.Attach(item,7)!=record)throw new Exception("Duplicate growth "+type.Name);
   item.Delete();record.Delete();
  }
  var existing=new Brightblade();var old=HavenAdvancedGear.Attach(existing,2);old.Gain(100);if(HavenAdvancedGear.Attach(existing,HavenAdvancedGear.AutoKind(existing))!=old||old.Kind!=2||old.Experience!=100)throw new Exception("Existing progression replaced");existing.Delete();old.Delete();
  log("PASS all 16 boss artifacts gain original level-20 damage, spell, regeneration and weapon bonuses; repeated application cannot stack; existing legendary progression retained");
 }
}
