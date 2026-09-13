using System;
using Server;
using Server.Items;
using Server.HavenPrototype;
public static class CompanionArmsSmoke {
 public static void Run(Action<string> log) {
  var c=new HavenCompanion();var book=new Spellbook();var sword=new Longsword();var bow=new Bow();
  try {
   c.Name="Jenna Ashford";c.Female=false;c.EnsureWardrobe();if(!c.Female||c.RawBody!=0x191||new Server.Spells.Necromancy.WraithFormSpell(c,null).Body!=0x191)throw new Exception("Female caster appearance");
   var b=HavenEquipmentEvolution.Attach(book,2);var s=HavenEquipmentEvolution.Attach(sword,1);var r=HavenEquipmentEvolution.Attach(bow,1);
   if(book.Attributes.SpellDamage!=75||sword.Attributes.WeaponDamage!=60||bow.Attributes.WeaponSpeed!=15)throw new Exception("Starting bonuses");
   b.Gain(1900);s.Gain(1900);r.Gain(1900);
   if(book.Attributes.SpellDamage!=150||book.Attributes.BonusMana!=100||book.Attributes.RegenMana!=12||sword.Attributes.WeaponDamage!=100||bow.Attributes.WeaponSpeed!=40)throw new Exception("Final bonuses");
   c.Backpack.DropItem(book);int before=AosAttributes.GetValue(c,AosAttribute.SpellDamage);c.AddItem(book);
   if(AosAttributes.GetValue(c,AosAttribute.SpellDamage)-before!=150)throw new Exception("Native equipped spell damage");
   book.Attributes.SpellDamage=180;b.Apply();
   if(book.Attributes.SpellDamage!=180||b.Experience!=1900||HavenEquipmentEvolution.Attach(book,2)!=b)throw new Exception("Existing bonus/XP preservation");
   log("PASS level 1/20 weapon and grimoire bonuses; native equipped SDI +150; existing higher bonuses and XP preserved");
  } finally {book.Delete();sword.Delete();bow.Delete();c.Delete();}
 }
}
