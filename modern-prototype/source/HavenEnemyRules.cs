using Server.Mobiles;
namespace Server.HavenPrototype
{
 public static class HavenEnemyRules
 {
  public static bool StandGround(BaseCreature c){return HavenPreview.Enabled&&c!=null&&!c.Deleted&&!c.Controlled&&!c.Summoned&&!(c is BaseVendor)&&(c.Karma<0||c.AlwaysMurderer||c.IsChampionSpawn||c.FightMode==FightMode.Closest||c.Combatant!=null);}
 }
}
