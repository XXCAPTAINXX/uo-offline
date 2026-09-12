using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.HavenPrototype {

public static class HavenPetLore
{
    internal static string SignatureName(BaseCreature pet)
    {
        var description = HavenPetSignatures.Describe(pet);
        var end = description.IndexOf(" — ", StringComparison.Ordinal);
        return end < 0 ? "Native abilities" : description.Substring(0,end);
    }

    internal static string Story(BaseCreature pet) { switch(HavenPetSignatures.Kind(pet)){
case 1: return "Emberwings nest in the warm stone above abandoned forges. Sailors once followed their drifting sparks through volcanic fog, believing each bird carried a coal from the world's first hearth. An Emberwing chooses its keeper slowly, but will shelter beside a familiar campfire long after the other animals have fled.";
case 2: return "Moonfangs walk the old roads when the moon is hidden. Haven's shepherds leave an empty place beside the gate for them: a quiet bargain that the night's hunter will take only what stalks the flock. A keeper earns a Moonfang's trust by standing watch with it, never by calling it tame.";
case 3: return "Frostmanes are said to descend from the horses of a caravan lost beneath the northern ice. Their breath shines in moonlight, and their tracks linger after the snow around them has melted. They remember safe paths through winter country, though no rider has learned where they go when they dream.";
case 4: return "Verdant llamas carry the scent of rain and crushed leaves. The first returned to Haven with a seed pouch tangled in its wool, after every gardener in its village had gone. Their keepers still weave green thread into their harnesses to honor the small, stubborn lives these gentle travelers protect.";
case 5: return "Stormscales bask on the cliffs before a squall. Fishermen listen for the crackle beneath their scales and bring the smaller boats ashore. One old chart names them the lighthouse keepers of a drowned kingdom; whatever the truth, they seem most content watching distant lights across dark water.";
case 6: return "Stormhorns graze where fallen stars have silvered the grass. Their horns hum near old standing stones, answering a song no scholar has written down. They are curious about spellbooks and suspicious of mirrors. A patient companion may wake to find a Stormhorn guarding the last embers of a spent ritual.";
case 7: return "The Frostbound bears keep dens beneath the wind-carved ridges. Hunters tell of a white guardian that stood over a lost child through three nights of snow, roaring only when the wolves approached. The northern villages leave fish at the treeline each spring, thanking a protector that never asked to be worshipped.";
case 8: return "Ancient hellhounds haunted the Abyss before its newest halls were cut. Their collars bear no maker's mark, yet some still pause beside ruined doorways as though waiting for a familiar voice. Those who befriend one speak of an unexpected tenderness beneath the ash: a creature that remembers how to guard, even when it has forgotten whom.";
case 9: return "Vampiric steeds were once whispered about along Haven's deserted lanes. They drink from neither trough nor stream, and their reflections trouble still water. A bonded steed turns its fierce hunger outward, guarding its rider with the same stubborn devotion that once kept it wandering the ruins.";
case 10: return "The great Chelonians remember islands before they had names. Barnacles gather on their shells like constellations, and young turtles shelter in the quiet water behind them. Islanders tie scraps of sailcloth to the shore when one departs, hoping the slow voyager will carry their wishes beyond the horizon.";
default: return "Every creature has a history beyond its training ledger. Watch where this one rests, what it eats, and how it answers danger; those small habits are the beginning of the story you will share.";
    }}

    internal sealed class Section { public int Tab;public string Heading,Text;public Section(int tab,string heading,string text){Tab=tab;Heading=heading;Text=text;} }
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
                (heading == "Training" || heading == "Learned Healing" || heading == "Trained abilities") ? 2 : heading == "Care and natural abilities" ? 3 : 0;
            lines.Add(new Section(tab, heading, value));
        }
        if (HavenPetSignatures.Kind(pet) != 0) { Add("Species signature", HavenPetSignatures.Describe(pet)); }
        var defense = HavenLoreCompatibility.Defenses(pet);
        if (defense.Length > 0) { Add("Innate defenses", defense); }
        var rarity = HavenPetRarity.Find(pet);
        if (rarity != null && HavenPetSignatures.Kind(pet)!=0)
        { Add("Rarity: " + HavenPetRarity.Label(rarity.Tier), HavenLoreCompatibility.RarityDescription(rarity.Tier)); }
        var legendary = pet.Backpack?.FindItemByType(typeof(HavenLegendaryPetSkills),true) as HavenLegendaryPetSkills;
        if (legendary != null)
        {
            var skills = new List<string>();
            foreach (var name in legendary.Boosted)
            { var skill = pet.Skills[name]; skills.Add($"{skill.Name}: {skill.Base:F1} / {skill.Cap:F1}"); }
            Add("Legendary skill rolls", skills.Count == 0 ? "No over-cap skills rolled." : string.Join("; ", skills));
        }
        var training = HavenLoreCompatibility.Training(pet);
        if (training != null)
        {
            Add("Training", $"{training.Status(pet)}. Progress {training.Progress / 100.0:F1}%; {training.Points:F1} points; slots {pet.ControlSlots}/{HavenLoreCompatibility.MaxSlots(pet)}.");
            if (training.Healing > 0) { Add("Learned Healing", "Automatically heals itself and its owner."); }
        }
        var abilities=PetTrainingHelper.GetAbilityProfile(pet);
        if(abilities!=null){var learned=abilities.EnumerateAllAbilities().Select(x=>x.ToString()).ToArray();if(learned.Length>0)Add("Trained abilities",String.Join("; ",learned));}
        if (pet is HavenSnowBear) { Add("Colossal Rage", "Colossal Rage: below half health, melee damage +50% for 10 seconds; 30-second cooldown. Innate, no training slot."); }
        if (pet is HavenChelonian)
        { Add("Living Shell and cargo", "Walks on land, swims and fights at sea; owner-accessible cargo. Below half health, reduces melee damage by 20–35% depending on rarity."); }
        Add("Care and natural abilities", $"Native bandage healing: {((pet.HealChance > 0) ? "yes" : "no")}; bandage owner healing: {((pet.HealChance > 0) ? "yes" : "no")}; bard immunity: {(pet.BardImmune ? "yes" : "no")}. Food: {pet.FavoriteFood}. Pack instinct: {pet.PackInstinct}. Taming requirement: {pet.MinTameSkill:F1}.");
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
        _pet = pet; _tab = HavenLoreCompatibility.Clamp(tab, 0, 4);
        AddBackground(0, 0, 650, 530, 9270);
        AddBackground(10, 10, 630, 510, 3000);
        Text(28, 24, 594, 28, WebUtility.HtmlEncode(pet.Name), true);
        var rarity = HavenPetRarity.Find(pet);
        var rank = HavenPetSignatures.Kind(pet)!=0 ? HavenPetRarity.Label(rarity?.Tier ?? 0) + "  |  " : "";
        AddLabel(28, 55, 0, $"{rank}{pet.ControlSlots} follower slot{(pet.ControlSlots == 1 ? "" : "s")}  |  {(pet.IsBonded ? "Bonded" : pet.Controlled ? "Tamed" : "Wild")}");
        var tabs = new[] { "Abilities", "Rarity", "Training", "Care", "Story" };
        for (var i = 0; i < tabs.Length; i++)
        {
            var x = 24 + i * 121;
            if (i == _tab) { AddBackground(x - 3, 82, 119, 32, 9200); }
            HavenLoreCompatibility.Button(this,x,88,110,10+i,tabs[i]);
        }
        var sections = new List<HavenPetLore.Section>();
        if (_tab == 4) { sections.Add(new HavenPetLore.Section(4, "A creature's story", HavenPetLore.Story(pet))); }
        else { foreach (var section in HavenPetLore.Sections(pet)) { if (section.Tab == _tab) { sections.Add(section); } } }
        if (sections.Count == 0)
        {
            sections.Add(new HavenPetLore.Section(_tab, tabs[_tab], (_tab==1 ? "This creature has no custom rarity bonuses." : _tab==2 ? "No special training has been recorded yet. Open Animal Lore below to view its skills and available training." : "This creature uses its natural species abilities. Open Animal Lore below for its full stats.")));
        }
        var pages = (sections.Count + 1) / 2; _page = HavenLoreCompatibility.Clamp(page, 0, pages - 1);
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
            if (_page > 0) { HavenLoreCompatibility.Button(this,28,465,110,20,"Previous"); }
            AddLabel(281, 467, 0, $"{_page + 1} / {pages}");
            if (_page + 1 < pages) { HavenLoreCompatibility.Button(this,516,465,100,21,"Next"); }
        }
        HavenLoreCompatibility.Button(this,28,493,220,3,"Animal Lore overview");
        HavenLoreCompatibility.Button(this,516,493,100,0,"Close");
    }
    private void AddHtml(int x,int y,int w,int h,string text){base.AddHtml(x,y,w,h,text,false,false);}
    private void Text(int x, int y, int width, int height, string encoded, bool bold)
        => AddHtml(x, y, width, height, $"<BASEFONT COLOR={Ink}>{(bold ? "<B>" : "")}{encoded}{(bold ? "</B>" : "")}</BASEFONT>");
    public override void OnResponse(NetState state, RelayInfo info)
    {
        if (info.ButtonID == 0 || !HavenAnimalLoreGump.CanInspect(state.Mobile, _pet)) { return; }
        if (info.ButtonID == 3) { HavenAnimalLoreGump.DisplayTo(state.Mobile, _pet); return; }
        if (info.ButtonID >= 10 && info.ButtonID <= 14) { state.Mobile.SendGump(new HavenPetLoreGump(_pet, info.ButtonID - 10)); }
        else if (info.ButtonID == 20 || info.ButtonID == 21) { state.Mobile.SendGump(new HavenPetLoreGump(_pet, _tab, _page + (info.ButtonID == 20 ? -1 : 1))); }
    }
}

}
