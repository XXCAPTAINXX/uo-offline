using System;
using System.Linq;
using Server;
using Server.Items;
namespace Server.HavenPrototype {
 public static class HavenBoardingShieldUpgrade {
  public static void Apply(MetalKiteShield s){s.Attributes.DefendChance=Math.Max(s.Attributes.DefendChance,15);s.Attributes.AttackChance=Math.Max(s.Attributes.AttackChance,10);s.Attributes.BonusHits=Math.Max(s.Attributes.BonusHits,15);s.Attributes.BonusStam=Math.Max(s.Attributes.BonusStam,10);s.Attributes.RegenHits=Math.Max(s.Attributes.RegenHits,3);s.Attributes.RegenMana=Math.Max(s.Attributes.RegenMana,3);s.Attributes.RegenStam=Math.Max(s.Attributes.RegenStam,3);s.Attributes.LowerManaCost=Math.Max(s.Attributes.LowerManaCost,5);s.PhysicalBonus=Math.Max(s.PhysicalBonus,10);s.FireBonus=Math.Max(s.FireBonus,10);s.ColdBonus=Math.Max(s.ColdBonus,10);s.PoisonBonus=Math.Max(s.PoisonBonus,10);s.EnergyBonus=Math.Max(s.EnergyBonus,10);s.SkillBonuses.SetValues(0,SkillName.Parry,Math.Max(10,s.SkillBonuses.GetBonus(0)));HavenEquipmentEvolution.Attach(s,0);s.InvalidateProperties();}
  public static void Initialize(){EventSink.ServerStarted+=()=>{if(HavenPreview.Enabled)foreach(var shield in World.Items.Values.OfType<MetalKiteShield>().Where(x=>!x.Deleted&&x.Name!=null&&x.Name.StartsWith("Boarding shield")&&x.Hue==0x489).ToArray())Apply(shield);};}
 }
}
