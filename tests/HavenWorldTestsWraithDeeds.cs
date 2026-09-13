using Server;
using Server.Items;
using Server.Mobiles;
using Server.Spells;
using Server.Spells.Necromancy;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsWraithDeeds
{
    public HavenWorldTestsWraithDeeds() => _ = new HavenWorldTests();

    [SkippableFact]
    public void CasterSelectsWraithOnceAndNativeSpellDamageLeechesAvailableMana()
    {
        TileDataRequirement.SkipIfMissing();
        var caster = new HavenCompanion { Role = HavenCompanionRole.Caster };
        var enemy = new Dragon();
        try
        {
            caster.Skills.Necromancy.Base = 100; caster.Skills.SpiritSpeak.Base = 100;
            caster.Mana = 100;
            var spell = Assert.IsType<WraithFormSpell>(caster.ChooseManaForm());
            TransformationSpellHelper.AddContext(caster,
                new TransformContext(new TransformTimer(caster, spell), typeof(WraithFormSpell), spell));
            Assert.Null(caster.ChooseManaForm());
            enemy.Mana = 10;
            SpellHelper.DoLeech(100, caster, enemy);
            Assert.Equal(110, caster.Mana); Assert.Equal(0, enemy.Mana);
            SpellHelper.DoLeech(100, caster, enemy);
            Assert.Equal(110, caster.Mana);
            caster.Role = HavenCompanionRole.Fighter; caster.ClearCasterForm();
            Assert.Null(TransformationSpellHelper.GetContext(caster));
            Assert.Null(caster.ChooseManaForm());
            caster.Role = HavenCompanionRole.Caster; caster.Mana = 16;
            Assert.Null(caster.ChooseManaForm());
        }
        finally { caster.ClearCasterForm(); caster.Delete(); enemy.Delete(); }
    }

    [SkippableFact]
    public void EveryTrialThemeGivesLightweightFilledDeedsForWavesAndBoss()
    {
        TileDataRequirement.SkipIfMissing();
        for (var theme = 0; theme < 3; theme++)
        {
            foreach (var boss in new[] { false, true })
            {
                var deed = HavenTrialTheme.ResourceDeed(theme, boss);
                var resource = deed.Commodity;
                try
                {
                    Assert.NotNull(resource); Assert.Equal(Map.Internal, resource.Map);
                    Assert.Equal(1.0, deed.Weight);
                    Assert.True(((ICommodity)resource).IsDeedable);
                    Assert.InRange(resource.Amount, boss ? 150 : 1, boss ? 350 : 12);
                }
                finally { deed.Delete(); }
                Assert.True(resource.Deleted);
            }
        }
    }
}
