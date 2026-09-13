using System;
using System.Collections.Generic;
using Server.Items;
namespace Server.HavenPrototype {
 public interface IHavenCombatShield { int ShieldStyle {get;} }
 public interface IHavenCastingGear { int CastingStyle {get;} }
 public static class HavenCombatCastingGear {
  public static void Apply(Item item,int level){
   int s=Math.Max(0,level-1);var a=HavenAdvancedGear.Attributes(item);if(a==null)return;
   var shield=item as BaseShield;
   if(shield!=null&&(item is IHavenCombatShield||item is IHavenShieldWarriorGear)){
    int style=item is IHavenCombatShield?((IHavenCombatShield)item).ShieldStyle:1;
    shield.ArmorAttributes.SoulCharge=Math.Max(shield.ArmorAttributes.SoulCharge,(style==2?10:20)+s);
    a.DefendChance=Math.Max(a.DefendChance,10+s/3);a.LowerManaCost=Math.Max(a.LowerManaCost,5+s/6);
    a.RegenStam=Math.Max(a.RegenStam,2+s/6);
    if(style==1){a.AttackChance=Math.Max(a.AttackChance,10+s/4);a.BonusStam=Math.Max(a.BonusStam,10+s);}
    if(style==2){a.RegenHits=Math.Max(a.RegenHits,3+s/5);a.BonusHits=Math.Max(a.BonusHits,10+s);}
    if(item is HavenHooksShield){
     shield.ArmorAttributes.SoulCharge=Math.Max(shield.ArmorAttributes.SoulCharge,25+s);
     a.DefendChance=Math.Max(a.DefendChance,15+s/4);
     a.LowerManaCost=Math.Max(a.LowerManaCost,6+s/5);
     a.RegenMana=Math.Max(a.RegenMana,3+s/6);
     a.BonusStam=Math.Max(a.BonusStam,5+s/2);
    }
   }
   var caster=item as IHavenCastingGear;if(caster==null)return;
   switch(caster.CastingStyle){
    case 0:a.SpellDamage=Math.Max(a.SpellDamage,12+s);a.LowerRegCost=Math.Max(a.LowerRegCost,20+s/2);a.RegenMana=Math.Max(a.RegenMana,2+s/6);break;
    case 1:a.BonusInt=Math.Max(a.BonusInt,5+s/3);a.SpellDamage=Math.Max(a.SpellDamage,5+s/2);a.LowerRegCost=Math.Max(a.LowerRegCost,15);break;
    case 2:a.LowerManaCost=Math.Max(a.LowerManaCost,5+s/6);a.CastRecovery=Math.Max(a.CastRecovery,1+s/10);a.BonusMana=Math.Max(a.BonusMana,5+s);break;
    case 3:a.RegenMana=Math.Max(a.RegenMana,1+s/8);a.BonusHits=Math.Max(a.BonusHits,5+s);a.LowerRegCost=Math.Max(a.LowerRegCost,10+s/2);break;
    case 4:a.CastSpeed=Math.Max(a.CastSpeed,1);a.CastRecovery=Math.Max(a.CastRecovery,2);a.SpellDamage=Math.Max(a.SpellDamage,8+s);break;
    case 5:a.LowerManaCost=Math.Max(a.LowerManaCost,6+s/6);a.SpellDamage=Math.Max(a.SpellDamage,8+s);a.RegenMana=Math.Max(a.RegenMana,2+s/8);break;
    default:a.SpellDamage=Math.Max(a.SpellDamage,15+s);a.BonusMana=Math.Max(a.BonusMana,10+s);break;
   }
  }
  public static void AddCatalog(List<HavenShopEntry> list){
   list.Add(new HavenShopEntry("Stormguard basher shield",0,()=>new HavenStormguardShield(),"Levels 1-20: Soul Charge 20-39%, stamina and hit chance. Native Parrying mastery governs Shield Bash.",200));
      list.Add(new HavenShopEntry("Tidecaller robe",0,()=>new HavenTidecallerRobe(),"Evolving caster equipment, levels 1-20. Inspect properties for its casting bonuses.",150));
   list.Add(new HavenShopEntry("Navigator sage hat",0,()=>new HavenSageHat(),"Evolving caster equipment, levels 1-20. Inspect properties for its casting bonuses.",150));
   list.Add(new HavenShopEntry("Channeler gloves",0,()=>new HavenChannelerGloves(),"Evolving caster equipment, levels 1-20. Inspect properties for its casting bonuses.",150));
   list.Add(new HavenShopEntry("Manawalk boots",0,()=>new HavenManawalkBoots(),"Evolving caster equipment, levels 1-20. Inspect properties for its casting bonuses.",150));
   
   
   
   
  }
 }
 public class HavenTidecallerRobe:Robe,IHavenCastingGear {
  public int CastingStyle {get{return 0;}}
  [Constructable]public HavenTidecallerRobe(){Name="Tidecaller robe";Hue=0x53D;HavenAdvancedGear.Attach(this,5);}
  public HavenTidecallerRobe(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);} public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenSageHat:WizardsHat,IHavenCastingGear {
  public int CastingStyle {get{return 1;}}
  [Constructable]public HavenSageHat(){Name="Navigator sage hat";Hue=0x53D;HavenAdvancedGear.Attach(this,5);}
  public HavenSageHat(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);} public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenChannelerGloves:LeatherGloves,IHavenCastingGear {
  public int CastingStyle {get{return 2;}}
  [Constructable]public HavenChannelerGloves(){Name="Channeler gloves";Hue=0x53D;HavenAdvancedGear.Attach(this,5);}
  public HavenChannelerGloves(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);} public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenManawalkBoots:Boots,IHavenCastingGear {
  public int CastingStyle {get{return 3;}}
  [Constructable]public HavenManawalkBoots(){Name="Manawalk boots";Hue=0x53D;HavenAdvancedGear.Attach(this,5);}
  public HavenManawalkBoots(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);} public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenTidecastingRing:GoldRing,IHavenCastingGear {
  public int CastingStyle {get{return 4;}}
  [Constructable]public HavenTidecastingRing(){Name="Tidecasting ring";Hue=0x53D;HavenAdvancedGear.Attach(this,5);}
  public HavenTidecastingRing(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);} public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenDeepcastingBracelet:GoldBracelet,IHavenCastingGear {
  public int CastingStyle {get{return 5;}}
  [Constructable]public HavenDeepcastingBracelet(){Name="Deepcasting bracelet";Hue=0x53D;HavenAdvancedGear.Attach(this,5);}
  public HavenDeepcastingBracelet(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);} public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenTidecallerBook:Spellbook,IHavenCastingGear {
  public int CastingStyle {get{return 6;}}
  [Constructable]public HavenTidecallerBook():base(ulong.MaxValue){Name="Tidecaller spellbook";Hue=0x53D;HavenAdvancedGear.Attach(this,5);}
  public HavenTidecallerBook(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);} public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenDrownedGrimoire:NecromancerSpellbook,IHavenCastingGear {
  public int CastingStyle {get{return 7;}}
  [Constructable]public HavenDrownedGrimoire():base(ulong.MaxValue){Name="Drowned grimoire";Hue=0x53D;HavenAdvancedGear.Attach(this,5);}
  public HavenDrownedGrimoire(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);} public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenStormguardShield:MetalKiteShield,IHavenCombatShield {
  public int ShieldStyle {get{return 1;}}
  [Constructable]public HavenStormguardShield(){Name="Stormguard basher shield";Hue=0x972;HavenAdvancedGear.Attach(this,5);}
  public HavenStormguardShield(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);} public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
 public class HavenIronwakeShield:HeaterShield,IHavenCombatShield {
  public int ShieldStyle {get{return 2;}}
  [Constructable]public HavenIronwakeShield(){Name="Ironwake bulwark";Hue=0x972;HavenAdvancedGear.Attach(this,5);}
  public HavenIronwakeShield(Serial s):base(s){}
  public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);} public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();}
 }
}

