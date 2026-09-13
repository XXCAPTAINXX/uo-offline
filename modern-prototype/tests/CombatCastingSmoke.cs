using System;
using System.Linq;
using Server;
using Server.Items;
using Server.HavenPrototype;
public static class CombatCastingSmoke {
 public static void Run(Action<string> log){
  Item[] items={new HavenHooksShield(),new HavenStormguardShield(),new HavenIronwakeShield(),new HavenBulwarkShield(2),new HavenTidecallerRobe(),new HavenSageHat(),new HavenChannelerGloves(),new HavenManawalkBoots(),new HavenTidecastingRing(),new HavenDeepcastingBracelet(),new HavenTidecallerBook(),new HavenDrownedGrimoire()};
  try{foreach(var item in items){
   var record=HavenAdvancedGear.Find(item)??HavenAdvancedGear.Attach(item,HavenAdvancedGear.AutoKind(item));
   if(record==null)throw new Exception("No evolution: "+item.GetType().Name);
   var attrs=HavenAdvancedGear.Attributes(item);int luck=attrs.Luck;
   record.Gain(1900);if(record.Level!=20||attrs.Luck<=luck)throw new Exception("No level growth");
   int soul=item is BaseShield?((BaseShield)item).ArmorAttributes.SoulCharge:0;
   if(item is BaseShield&&soul<29)throw new Exception("Missing Soul Charge growth");
   string snapshot=attrs.Luck+","+attrs.SpellDamage+","+attrs.RegenMana+","+attrs.BonusInt+","+attrs.LowerManaCost;
   record.Apply();record.Gain(50);
   if(snapshot!=attrs.Luck+","+attrs.SpellDamage+","+attrs.RegenMana+","+attrs.BonusInt+","+attrs.LowerManaCost)throw new Exception("Repeat apply inflation");
   log("PASS "+item.GetType().Name+" level20; SoulCharge="+soul+"; stable repeated apply");
  }
  var catalog=new System.Collections.Generic.List<HavenShopEntry>();HavenCombatCastingGear.AddCatalog(catalog);if(catalog.Count!=10)throw new Exception("Missing catalog options");log("PASS all ten new catalog entries");
  }finally{foreach(var item in items){HavenAdvancedGear.Find(item)?.Delete();item.Delete();}}
 }
}
