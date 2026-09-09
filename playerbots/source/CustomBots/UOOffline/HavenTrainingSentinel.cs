using ModernUO.Serialization;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenTrainingSentinel : BaseCreature
{
    [Constructible]
    public HavenTrainingSentinel() : base(AIType.AI_Animal, FightMode.None)
    {
        Name = "Haven training sentinel";
        Body = 14;
        Hue = 0x8A5;
        CantWalk = true;
        SetStr(100);
        SetDex(100);
        SetInt(100);
        SetHits(30000);
        SetDamage(0, 0);
        SetSkill(SkillName.Wrestling, 100);
        SetSkill(SkillName.MagicResist, 100);
        Fame = 0;
        Karma = 0;
        Tamable = false;
    }
    public override bool AlwaysAttackable => true;
    public override bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness) => false;
    public override bool OnBeforeDeath() { Hits = HitsMax; return false; }
    public override void GenerateLoot() { }
    public override void OnThink()
    {
        Combatant = null;
        Warmode = false;
        CantWalk = true;
        base.OnThink();
    }
}
