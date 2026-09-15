using Server.Items;
using ModernUO.Serialization;
namespace Server.UOOffline;
public static class HavenQuestGear
{
    internal static bool IsReward(Item item) => item is ArmsOfArmstrong or BulwarkLeggings or BraceletOfResilience or
        EscutcheonDeAriadne or EmberStaff or ClaspOfConcentration or ChurchillsWarMace or Heartseeker or HealersTouch or
        HallowedSpellbook or GlovesOfSafeguarding or TheDragonsTail or JocklesQuicksword or JacobsPickaxe or PhilosophersHat or
        RecarosRiposte or TunicOfGuarding or SilverSerpentBlade or RingOfTheSavant or TwilightJacket or WalkersLeggings or HavenQuestNecromancerBook;
    internal static void Grow(Item item, int levels, int milestones)
    {
        if (!IsReward(item) || item is not IAosItem gear) { return; }
        if (item is BaseWeapon)
        {
            gear.Attributes.WeaponDamage += levels * 2;
            gear.Attributes.WeaponSpeed += milestones * 5;
        }
        else
        {
            gear.Attributes.SpellDamage += levels;
            gear.Attributes.RegenMana += milestones;
            gear.Attributes.RegenHits += milestones;
        }
    }
}

[SerializationGenerator(0)]
public partial class HavenQuestNecromancerBook : NecromancerSpellbook
{
    [Constructible]
    public HavenQuestNecromancerBook() { Name = "Haven necromancer's spellbook"; }
}
