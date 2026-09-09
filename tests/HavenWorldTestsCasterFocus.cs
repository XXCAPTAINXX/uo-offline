using Server;
using Server.Mobiles;
using Server.Spells.Second;
using Server.Spells.Spellweaving;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsCasterFocus
{
    public HavenWorldTestsCasterFocus() => _ = new HavenWorldTests();
    [SkippableFact]
    public void CasterHasPermanentFocusAndUsesGroupDamageCureAndExecutionPriorities()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        var caster = new HavenCompanion { BoundOwner = owner, Role = HavenCompanionRole.Caster };
        var enemy = new Dragon(); var second = new Dragon();
        try
        {
            foreach (var mob in new Mobile[] {owner, caster, enemy, second}) { mob.MoveToWorld(HavenRecovery.BankLocation, Map.Trammel); }
            caster.Skills.Spellweaving.Base = 100; caster.Mana = caster.ManaMax;
            Assert.Equal(6, ArcanistSpell.GetFocusLevel(caster));
            Assert.Null(ArcanistSpell.FindArcaneFocus(caster));
            Assert.IsType<ThunderstormSpell>(caster.ChooseAttackSpell(enemy));
            enemy.Hits = enemy.HitsMax / 4;
            Assert.IsType<WordOfDeathSpell>(caster.ChooseAttackSpell(enemy));
            if (Poison.Regular == null) { PoisonKinds.Configure(); }
            owner.Poison = Poison.Regular;
            Assert.IsType<CureSpell>(caster.ChooseEmergencySpell(out var patient)); Assert.Same(owner, patient);
            caster.Role = HavenCompanionRole.Fighter;
            Assert.Equal(0, ArcanistSpell.GetFocusLevel(caster));
            caster.Role = HavenCompanionRole.Caster;
            Assert.Equal(6, ArcanistSpell.GetFocusLevel(caster));
        }
        finally { caster.Delete(); owner.Delete(); enemy.Delete(); second.Delete(); }
    }
}
