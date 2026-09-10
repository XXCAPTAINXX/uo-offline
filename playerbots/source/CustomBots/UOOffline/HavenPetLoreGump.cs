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

    internal static string Details(BaseCreature pet)
    {
        var lines = new List<string>();
        void Add(string heading, string value) => lines.Add($"<B>{WebUtility.HtmlEncode(heading)}</B><BR>{WebUtility.HtmlEncode(value)}");
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
            foreach (var id in abilities.Learned)
            { if (id >= 0 && id < HavenPetAbilities.Names.Length) { Add("Trained ability", HavenPetAbilities.Names[id]); } }
        }
        if (pet is HavenSnowBear) { Add("Colossal Rage", HavenSnowBear.RageDescription); }
        if (pet is HavenChelonian)
        { Add("Living Shell and cargo", "Walks on land, swims and fights at sea; owner-accessible cargo. Below half health, reduces melee damage by 20–35% depending on rarity."); }
        Add("Care and natural abilities", $"Self healing: {(pet.CanHeal ? "yes" : "no")}; owner healing: {(pet.CanHealOwner ? "yes" : "no")}; bard immunity: {(pet.BardImmune ? "yes" : "no")}. Food: {pet.FavoriteFood}. Pack instinct: {pet.PackInstinct}. Taming requirement: {pet.MinTameSkill:F1}.");
        return string.Join("<BR><BR>", lines);
    }
}

public sealed class HavenPetLoreGump : Gump
{
    private readonly BaseCreature _pet;
    internal HavenPetLoreGump(BaseCreature pet, int tab = 0) : base(45, 45)
    {
        _pet = pet;
        AddBackground(0, 0, 600, 490, 9270);
        AddHtml(25, 22, 550, 46, $"<BASEFONT COLOR=#FFFFFF><B>{WebUtility.HtmlEncode(pet.Name)}</B></BASEFONT>");
        AddButton(25, 80, 4005, 4007, 1); AddLabel(60, 82, 1152, "Abilities and training");
        AddButton(315, 80, 4005, 4007, 2); AddLabel(350, 82, 1152, "Creature lore");
        AddHtml(25, 125, 550, 290, tab == 1
            ? $"<BASEFONT COLOR=#FFFFFF>{WebUtility.HtmlEncode(HavenPetLore.Story(pet))}</BASEFONT>"
            : $"<BASEFONT COLOR=#FFFFFF>{HavenPetLore.Details(pet)}</BASEFONT>", false, true);
        AddButton(25, 443, 4014, 4016, 3); AddLabel(60, 445, 1152, "Animal Lore overview");
        AddButton(465, 443, 4017, 4019, 0); AddLabel(500, 445, 1152, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 0 || !HavenAnimalLoreGump.CanInspect(state.Mobile, _pet)) { return; }
        if (info.ButtonID == 3) { HavenAnimalLoreGump.DisplayTo(state.Mobile, _pet); return; }
        if (info.ButtonID is 1 or 2) { state.Mobile.SendGump(new HavenPetLoreGump(_pet, info.ButtonID - 1)); }
    }
}
