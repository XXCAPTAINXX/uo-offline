using System;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Mobiles;
using Server.Misc;
using Server.Spells;
using Server.Spells.Fourth;
using Server.Spells.Sixth;
using Server.Spells.Seventh;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsPetOverview
{
    public HavenWorldTestsPetOverview() => _ = new HavenWorldTests();
    [SkippableFact]
    public void TravelSpellsHaveNoMageryFailureAtZeroSkill()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile();
        try
        {
            owner.Skills.Magery.Base = 0;
            Spell[] spells = [new MarkSpell(owner), new RecallSpell(owner), new GateTravelSpell(owner)];
            foreach (var spell in spells)
            {
                spell.GetCastSkills(out var min, out var max);
                Assert.True(SkillCheck.Mobile_SkillCheckLocation(owner, SkillName.Magery, min, max));
            }
        }
        finally { owner.Delete(); }
    }
    [SkippableFact]
    public void LoreOverviewIsReadOnlyAndShowsBothHealingAndPoisoningWithSkillCaps()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile();
        var pet = new Dragon();
        var stamHandler = Mobile.StamRegenRateHandler;
        var manaHandler = Mobile.ManaRegenRateHandler;
        try
        {
            pet.Skills.Healing.Base = 45;
            pet.Skills.Healing.Cap = 110;
            Mobile.StamRegenRateHandler = _ => throw new InvalidOperationException("Lore must not train regeneration skills.");
            Mobile.ManaRegenRateHandler = _ => throw new InvalidOperationException("Lore must not train regeneration skills.");
            Assert.Null(HavenPetTraining.Find(pet));
            var gump = new HavenAnimalLoreGump(owner, pet);
            Assert.Null(HavenPetTraining.Find(pet));
            var labels = gump.Entries.OfType<GumpLabel>().Select(e => e.Text).ToArray();
            Assert.Contains("Healing", labels);
            Assert.Contains("Poisoning", labels);
            Assert.Contains("45.0/110.0", labels);
            foreach (var label in gump.Entries.OfType<GumpLabel>())
            {
                Assert.InRange(label.X, 0, 630); Assert.InRange(label.Y, 0, 660);
            }
        }
        finally { Mobile.StamRegenRateHandler = stamHandler; Mobile.ManaRegenRateHandler = manaHandler; pet.Delete(); owner.Delete(); }
    }
}
