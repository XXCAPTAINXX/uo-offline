using System;
using System.Linq;
using Server;
using Server.Items;
using Server.HavenPrototype;
public static class CompanionClothingSmoke
{
 public static void Run(Action<string> log)
 {
  var companion=new HavenCompanion();
  try {
   companion.EnsureWardrobe();
   var garments=companion.Items.OfType<BaseClothing>().ToArray();
   if(garments.Length==0)throw new Exception("No garments");
   foreach(var garment in garments) {
    var record=HavenEquipmentEvolution.Find(garment);
    if(record==null||record.Kind!=5||garment.Attributes.SpellDamage<10)throw new Exception("Missing starter progression");
    int art=garment.ItemID,hue=garment.Hue;record.Gain(1900);
    if(record.Level!=20||garment.Attributes.SpellDamage<25||garment.Attributes.RegenMana<4||garment.Attributes.BonusMana<20)throw new Exception("Weak level 20 bonuses");
    companion.EnsureWardrobe();record.Apply();
    if(HavenEquipmentEvolution.Find(garment)!=record||record.Experience!=1900||garment.ItemID!=art||garment.Hue!=hue||!garment.Movable)throw new Exception("Appearance, XP or paperdoll regression");
    if(garment.Name.Split(new[]{"[level"},StringSplitOptions.None).Length!=2)throw new Exception("Repeated level suffix");
   }
   var sample=garments[0];int equipped=AosAttributes.GetValue(companion,AosAttribute.SpellDamage);
   companion.Backpack.DropItem(sample);
   int unequipped=AosAttributes.GetValue(companion,AosAttribute.SpellDamage);
   if(equipped-unequipped!=sample.Attributes.SpellDamage)throw new Exception("Native SDI does not track worn clothing");
   log("PASS starter clothing gets XP records; level 20 bonuses; repeated apply preserves XP, appearance and removable paperdoll; native spell damage includes only worn clothes");
  } finally {companion.Delete();}
 }
}
