using System;
using System.Collections.Generic;
using System.Net;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

public static class HavenPetLore
{
    internal static string SignatureName(BaseCreature pet)
    {
        var description = HavenPetSignatures.Describe(pet);
        var end = description.IndexOf(" — ", StringComparison.Ordinal);
        return end < 0 ? "Native abilities" : description[..end];
    }

    internal static string Story(BaseCreature pet) => HavenPetSignatures.Kind(pet) switch
    {
        1 => "Emberwings nest in the warm stone above abandoned forges. Sailors once followed their drifting sparks through volcanic fog, believing each bird carried a coal from the world's first hearth. An Emberwing chooses its keeper slowly, but will shelter beside a familiar campfire long after the other animals have fled.",
        2 => "Moonfangs walk the old roads when the moon is hidden. Haven's shepherds leave an empty place beside the gate for them: a quiet bargain that the night's hunter will take only what stalks the flock. A keeper earns a Moonfang's trust by standing watch with it, never by calling it tame.",
        3 => "Frostmanes are said to descend from the horses of a caravan lost beneath the northern ice. Their breath shines in moonlight, and their tracks linger after the snow around them has melted. They remember safe paths through winter country, though no rider has learned where they go when they dream.",
        4 => "Verdant llamas carry the scent of rain and crushed leaves. The first returned to Haven with a seed pouch tangled in its wool, after every gardener in its village had gone. Their keepers still weave green thread into their harnesses to honor the small, stubborn lives these gentle travelers protect.",
        5 => "Stormscales bask on the cliffs before a squall. Fishermen listen for the crackle beneath their scales and bring the smaller boats ashore. One old chart names them the lighthouse keepers of a drowned kingdom; whatever the truth, they seem most content watching distant lights across dark water.",
        6 => "Stormhorns graze where fallen stars have silvered the grass. Their horns hum near old standing stones, answering a song no scholar has written down. They are curious about spellbooks and suspicious of mirrors. A patient companion may wake to find a Stormhorn guarding the last embers of a spent ritual.",
        7 => "The Frostbound bears keep dens beneath the wind-carved ridges. Hunters tell of a white guardian that stood over a lost child through three nights of snow, roaring only when the wolves approached. The northern villages leave fish at the treeline each spring, thanking a protector that never asked to be worshipped.",
        8 => "Ancient hellhounds haunted the Abyss before its newest halls were cut. Their collars bear no maker's mark, yet some still pause beside ruined doorways as though waiting for a familiar voice. Those who befriend one speak of an unexpected tenderness beneath the ash: a creature that remembers how to guard, even when it has forgotten whom.",
        9 => "Vampiric steeds were once whispered about along Haven's deserted lanes. They drink from neither trough nor stream, and their reflections trouble still water. A bonded steed turns its fierce hunger outward, guarding its rider with the same stubborn devotion that once kept it wandering the ruins.",
        10 => "The great Chelonians remember islands before they had names. Barnacles gather on their shells like constellations, and young turtles shelter in the quiet water behind them. Islanders tie scraps of sailcloth to the shore when one departs, hoping the slow voyager will carry their wishes beyond the horizon.",
        _ => "Every creature has a history beyond its training ledger. Watch where this one rests, what it eats, and how it answers danger; those small habits are the beginning of the story you will share."
    };

    internal readonly record struct Section(int Tab, string Heading, string Text);
    internal static string Details(BaseCreature pet)
    {
        var lines = new List<string>();
        foreach (var section in Sections(pet))
        { lines.Add($"<B>{WebUtility.HtmlEncode(section.Heading)}</B><BR>{WebUtility.HtmlEncode(section.Text)}"); }
        return string.Join("<BR><BR>", lines);
    }
    internal static List<Section> Sections(BaseCreature pet)
    {
        var lines = new List<Section>();
        void Add(string heading, string value)
        {
            var tab = heading.StartsWith("Rarity:", StringComparison.Ordinal) || heading == "Legendary skill rolls" ? 1 :
                heading is "Training" or "Learned Healing" or "Trained abilities" ? 2 : heading == "Care and natural abilities" ? 3 : 0;
            lines.Add(new Section(tab, heading, value));
        }
        if (HavenPetSignatures.Kind(pet) != 0) { Add("Species signature", HavenPetSignatures.Describe(pet)); }
        var defense = HavenPetDefenses.Describe(pet);
        if (defense.Length > 0) { Add("Innate defenses", defense); }
        var rarity = pet.Backpack?.FindItemByType<HavenPetRarity>();
        if (rarity != null && HavenTamingMissions.IsCustomPet(pet))
        { Add("Rarity: " + HavenPetRarity.RarityName(rarity.Tier), HavenPetRarity.Describe(rarity.Tier)); }
        var legendary = pet.Backpack?.FindItemByType<HavenLegendaryPetSkills>();
        if (legendary != null)
        {
            var skills = new List<string>();
            foreach (var name in legendary.Boosted)
            { var skill = pet.Skills[name]; skills.Add($"{skill.Name}: {skill.Base:F1} / {skill.Cap:F1}"); }
            Add("Legendary skill rolls", skills.Count == 0 ? "No over-cap skills rolled." : string.Join("; ", skills));
        }
        var training = HavenPetTraining.Find(pet);
        if (training != null)
        {
            Add("Training", $"{training.Status(pet)}. Progress {training.Progress / 100.0:F1}%; {training.Points:F1} points; slots {pet.ControlSlots}/{HavenPetTraining.MaxSlots(pet)}.");
            if (training.Healing > 0) { Add("Learned Healing", "Automatically heals itself and its owner."); }
        }
        var abilities = HavenPetAbilities.Find(pet);
        if (abilities != null)
        {
            var learned = new List<string>();
            foreach (var id in abilities.Learned)
            { if (id >= 0 && id < HavenPetAbilities.Names.Length) { learned.Add(HavenPetAbilities.Names[id]); } }
            if (learned.Count > 0) { Add("Trained abilities", string.Join("; ", learned)); }
        }
        if (pet is HavenSnowBear) { Add("Colossal Rage", HavenSnowBear.RageDescription); }
        if (pet is HavenChelonian)
        { Add("Living Shell and cargo", "Walks on land, swims and fights at sea; owner-accessible cargo. Below half health, reduces melee damage by 20–35% depending on rarity."); }
        Add("Care and natural abilities", $"Self healing: {(pet.CanHeal ? "yes" : "no")}; owner healing: {(pet.CanHealOwner ? "yes" : "no")}; bard immunity: {(pet.BardImmune ? "yes" : "no")}. Food: {pet.FavoriteFood}. Pack instinct: {pet.PackInstinct}. Taming requirement: {pet.MinTameSkill:F1}.");
        return lines;
    }
}

public sealed class HavenPetLoreGump : Gump
{
    private readonly BaseCreature _pet;
    private readonly int _tab;
    private readonly int _page;
    private const string Ink = "#181818";
    internal HavenPetLoreGump(BaseCreature pet, int tab = 0, int page = 0) : base(45, 45)
    {
        _pet = pet; _tab = Math.Clamp(tab, 0, 4);
        AddBackground(0, 0, 650, 530, 9270);
        AddBackground(10, 10, 630, 510, 3000);
        Text(28, 24, 594, 28, WebUtility.HtmlEncode(pet.Name), true);
        var rarity = pet.Backpack?.FindItemByType<HavenPetRarity>();
        var rank = HavenTamingMissions.IsCustomPet(pet) ? HavenPetRarity.RarityName(rarity?.Tier ?? 0) + "  |  " : "";
        AddLabel(28, 55, 0, $"{rank}{pet.ControlSlots} follower slot{(pet.ControlSlots == 1 ? "" : "s")}  |  {(pet.IsBonded ? "Bonded" : pet.Controlled ? "Tamed" : "Wild")}");
        var tabs = new[] { "Abilities", "Rarity", "Training", "Care", "Story" };
        for (var i = 0; i < tabs.Length; i++)
        {
            var x = 24 + i * 121;
            if (i == _tab) { AddBackground(x - 3, 82, 119, 32, 9200); }
            AddButton(x, 88, 4005, 4007, 10 + i); AddLabel(x + 36, 90, 0, tabs[i]);
        }
        var sections = new List<HavenPetLore.Section>();
        if (_tab == 4) { sections.Add(new HavenPetLore.Section(4, "A creature's story", HavenPetLore.Story(pet))); }
        else { foreach (var section in HavenPetLore.Sections(pet)) { if (section.Tab == _tab) { sections.Add(section); } } }
        if (sections.Count == 0)
        {
            sections.Add(new HavenPetLore.Section(_tab, tabs[_tab], _tab switch {
                1 => "This creature has no custom rarity bonuses.",
                2 => "No special training has been recorded yet. Open Animal Lore below to view its skills and available training.",
                _ => "This creature uses its natural species abilities. Open Animal Lore below for its full stats." }));
        }
        var pages = (sections.Count + 1) / 2; _page = Math.Clamp(page, 0, pages - 1);
        var count = Math.Min(2, sections.Count - _page * 2);
        var height = count == 1 ? 326 : 158;
        for (var i = 0; i < count; i++)
        {
            var section = sections[_page * 2 + i]; var y = 128 + i * 168;
            AddBackground(24, y, 602, height, 3000);
            Text(40, y + 12, 566, 25, WebUtility.HtmlEncode(section.Heading), true);
            var body = WebUtility.HtmlEncode(section.Text);
            if (_tab != 4) { body = body.Replace("; ", "<BR>").Replace(". ", ".<BR>"); }
            // Each card owns its overflow. Longer descriptions cannot cover the tabs or footer.
            AddHtml(40, y + 41, 566, height - 52, $"<BASEFONT COLOR={Ink}>{body}</BASEFONT>", false, true);
        }
        if (pages > 1)
        {
            if (_page > 0) { AddButton(28, 465, 4014, 4016, 20); AddLabel(65, 467, 0, "Previous"); }
            AddLabel(281, 467, 0, $"{_page + 1} / {pages}");
            if (_page + 1 < pages) { AddButton(516, 465, 4005, 4007, 21); AddLabel(553, 467, 0, "Next"); }
        }
        AddButton(28, 493, 4014, 4016, 3); AddLabel(65, 495, 0, "Animal Lore overview");
        AddButton(516, 493, 4017, 4019, 0); AddLabel(553, 495, 0, "Close");
    }
    private void Text(int x, int y, int width, int height, string encoded, bool bold)
        => AddHtml(x, y, width, height, $"<BASEFONT COLOR={Ink}>{(bold ? "<B>" : "")}{encoded}{(bold ? "</B>" : "")}</BASEFONT>");
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 0 || !HavenAnimalLoreGump.CanInspect(state.Mobile, _pet)) { return; }
        if (info.ButtonID == 3) { HavenAnimalLoreGump.DisplayTo(state.Mobile, _pet); return; }
        if (info.ButtonID is >= 10 and <= 14) { state.Mobile.SendGump(new HavenPetLoreGump(_pet, info.ButtonID - 10)); }
        else if (info.ButtonID is 20 or 21) { state.Mobile.SendGump(new HavenPetLoreGump(_pet, _tab, _page + (info.ButtonID == 20 ? -1 : 1))); }
    }
}
