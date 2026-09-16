using System;
using System.Linq;
using Server;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;

namespace UOContent.Tests;
[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsMenuStorage
{
    public HavenWorldTestsMenuStorage() => _ = new HavenWorldTests();

    [SkippableFact]
    public void BankExpansionPreservesContentsSupportsLargeDepositsAndKeepsItsLimit()
    {
        TileDataRequirement.SkipIfMissing();
        var owner = new PlayerMobile { Player = true, Body = 0x190 };
        try
        {
            var bank = owner.BankBox;
            var keepsake = new Bag(); bank.DropItem(keepsake); keepsake.DropItem(new Ruby(12));
            HavenBankStorage.Expand(owner);
            Assert.Same(bank, owner.BankBox); Assert.Equal(2000, bank.MaxItems); Assert.Equal(0, bank.MaxWeight);
            for (var i = 0; i < 150; i++)
            { var rune = new RecallRune(); Assert.True(bank.TryDropItem(owner, rune, false)); }
            Assert.Equal(152, bank.TotalItems);
            var extra = new RecallRune();
            try { Assert.False(bank.CheckHold(owner, extra, false, true, 2000, 0)); }
            finally { extra.Delete(); }
            HavenBankStorage.Expand(owner);
            Assert.Equal(12, keepsake.FindItemByType<Ruby>().Amount);
            bank.MaxItems = 3000; HavenBankStorage.Expand(owner); Assert.Equal(3000, bank.MaxItems);
            bank.MaxItems = 0; HavenBankStorage.Expand(owner); Assert.Equal(0, bank.MaxItems);
        }
        finally { owner.Delete(); }
    }

    [Fact]
    public void RegionalFooterHasSeparateRowsAndPetSkillsStayInsidePanel()
    {
        var companion = new HavenCompanion(); var pet = new GreyWolf();
        try
        {
            var regional = new HavenRegionalMissionGump(companion);
            var cycle = regional.Entries.OfType<GumpButton>().Single(b => b.ButtonID == 3);
            var close = regional.Entries.OfType<GumpButton>().Single(b => b.ButtonID == 0);
            Assert.True(close.Y - cycle.Y >= 40);
            foreach (var menu in Enumerable.Range(0, 5).Select(category => new HavenPetTrainingGump(pet, category: category)).Cast<Gump>().Append(regional))
            {
                var frame = menu.Entries.OfType<GumpBackground>().First();
                foreach (var button in menu.Entries.OfType<GumpButton>()) { Assert.True(button.Y + 22 <= frame.Height - 12); }
                foreach (var html in menu.Entries.OfType<GumpHtml>()) { Assert.True(html.Y + html.Height <= frame.Height - 12); }
            }
        }
        finally { companion.Delete(); pet.Delete(); }
    }

    [SkippableFact]
    public void PetLoreKeepsExactSignatureAndRarityDetailsAndDistinctSpeciesStories()
    {
        TileDataRequirement.SkipIfMissing();
        BaseCreature[] pets = [new HavenEmberwing(), new HavenMoonfang(), new HavenFrostmane(), new HavenVerdantLlama(),
            new HavenStormscale(), new HavenStormhorn(), new HavenSnowBear(), new HavenAncientHellhound(), new VampiricSteed(), new HavenChelonian()];
        try
        {
            Assert.Equal(pets.Length, pets.Select(HavenPetLore.Story).Distinct().Count());
            foreach (var pet in pets)
            {
                HavenPetRarity.Apply(pet, 3);
                var details = System.Net.WebUtility.HtmlDecode(HavenPetLore.Details(pet));
                Assert.Contains(HavenPetSignatures.Describe(pet), details);
                if (HavenTamingMissions.IsCustomPet(pet)) { Assert.Contains(HavenPetRarity.Describe(3), details); }
                Assert.True(HavenPetLore.SignatureName(pet).Length < 35);
                Assert.True(HavenPetLore.Story(pet).Length > 180);
                Assert.Contains(new HavenPetLoreGump(pet).Entries.OfType<GumpHtml>(), h => h.Scrollbar);
            }
        }
        finally { foreach (var pet in pets) { pet.Delete(); } }
    }
}
