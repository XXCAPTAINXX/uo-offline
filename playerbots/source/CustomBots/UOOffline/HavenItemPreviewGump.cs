using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Server.Gumps;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Network;

namespace Server.UOOffline;

public interface IHavenShop
{
    Item CreateItem(int index);
}

public sealed class HavenItemPreviewGump : Gump
{
    private readonly Item _stone;
    private readonly ItemListMenu _menu;
    private readonly int _index;
    private readonly int _page;

    public HavenItemPreviewGump(Item stone, ItemListMenu menu, int index, int page) : base(30, 30)
    {
        _stone = stone;
        _menu = menu;
        _index = index;
        _page = page;
        AddBackground(0, 0, 540, 460, 5054);
        AddBackground(12, 12, 516, 436, 3000);
        AddHtml(28, 24, 482, 50, $"<B>{menu.Entries[index].Name}</B>");
        // Preview the same factory used for purchases; never keep a world item
        // alive for an abandoned menu or grant it to the buyer.
        var preview = ((IHavenShop)menu).CreateItem(index);
        try
        {
            AddItem(35, 95, preview.ItemID, preview.Hue);
            AddHtml(105, 84, 390, 245, Describe(preview), false, true);
        }
        finally { preview?.Delete(); }
        AddHtml(28, 330, 482, 45, $"{menu.Question}");
        AddHtml(28, 375, 482, 20, menu is HavenAstralRewards.Menu ? "Astral purchases use the shard balance in this wallet." : "Marks use wallet then backpack; gold also uses your bank.");
        AddButton(28, 399, 4014, 4016, 2);
        AddLabel(66, 401, 0, "Back");
        AddButton(200, 399, 4005, 4007, 1);
        AddLabel(238, 401, 0, "Buy item");
        AddButton(390, 399, 4005, 4007, 0);
        AddLabel(428, 401, 0, "Close");
    }

    internal static string Tooltip(Item item) => string.Join("; ", Describe(item).Split("<BR>")
        .Where(line => !string.IsNullOrWhiteSpace(line)).OrderBy(line => line.StartsWith("Weight:") || line.StartsWith("Loot type:") ? 1 : 0));
    internal static string ShortStats(Item item) => string.Join("; ", Describe(item).Split("<BR>")
        .Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("Weight:") && !line.StartsWith("Loot type:")).Take(2));
    internal static string Describe(Item item)
    {
        var lines = new List<string> { $"Weight: {item.Weight:0.##} stones", $"Loot type: {item.LootType}" };
        if (item is IAosItem aos)
        {
            foreach (var attribute in Enum.GetValues<AosAttribute>())
            {
                var value = aos.Attributes[attribute];
                if (value == 0) { continue; }
                var label = attribute switch
                {
                    AosAttribute.LowerRegCost => "Lower Reagent Cost (%)",
                    AosAttribute.LowerManaCost => "Lower Mana Cost (%)",
                    AosAttribute.WeaponDamage => "Damage Increase (%)",
                    AosAttribute.AttackChance => "Hit Chance Increase (%)",
                    AosAttribute.DefendChance => "Defense Chance Increase (%)",
                    _ => Regex.Replace(attribute.ToString(), "([a-z])([A-Z])", "$1 $2")
                };
                lines.Add($"{label}: {value}");
            }
        }
        if (item is BaseJewel jewel)
        {
            for (var i = 0; i < 5; i++)
            {
                var bonus = jewel.SkillBonuses.GetBonus(i);
                if (bonus != 0) { lines.Add($"{jewel.SkillBonuses.GetSkill(i)}: +{bonus:0.#}"); }
            }
            foreach (var element in new[] { AosElementAttribute.Physical, AosElementAttribute.Fire, AosElementAttribute.Cold, AosElementAttribute.Poison, AosElementAttribute.Energy })
            {
                if (jewel.Resistances[element] != 0) { lines.Add($"{element} resistance: {jewel.Resistances[element]}"); }
            }
        }
        if (item is BaseWeapon weapon)
        {
            lines.Add($"Damage: {weapon.MinDamage}-{weapon.MaxDamage}; speed: {weapon.Speed}");
            lines.Add($"Strength required: {weapon.StrRequirement}");
            lines.Add("Binds to the buyer. Evolves through combat to level 20.");
        }
        lines.Add(item switch
        {
            HavenSetRing ring => $"Matching bracelet bonus: {HavenJewelrySets.Descriptions[ring.Theme]}. Add the Concord talisman for +250 Luck and +10% weapon/spell damage. This ring levels with shared experience.",
            HavenConcordTalisman => "With any matching Haven ring/bracelet pair: +250 Luck and +10% weapon/spell damage. Levels with shared experience.",
            NewHavenAdventurersRobe => "Binds to the buyer. Four upgrade tiers, purchased with Haven marks or gold.",
            ApprenticeGrimoire => "All 64 Magery spells. Binds to the buyer; evolves through spellcasting to level 20.",
            CleanupTrashBag => "Accepts eligible unwanted items; removes them after 3 minutes and awards cleanup points.",
            AdventurersWallet => "Double-click to collect backpack and nearby loose gold. Say withdraw 1000. Use [wallet for balances and Astral treasures.",
            ProgressionArchive => "Collects compatible progression scrolls, champion skulls, Haven marks, primers and binders.",
            PeerlessKeyVault => "Collects recognized Peerless keys from your backpack. Keys do not expire.",
            OfflineTravelBook => "Reusable travel book. Opens the moongate destination and facet selector from your backpack. No charges or reagents. Normal moongate eligibility applies; unavailable while dead, criminal, in combat or casting.",
            HavenMasteryManual => "Custom shard mastery: one 30-minute Warrior, Archer, Caster, Bard, Healer or Beastmaster stat focus. Requires 90 skill; scales at 100/110/120. Focuses do not stack.",
            HavenPetPowerScroll petScroll => $"Exchange for a standard {petScroll.Skill} {petScroll.Cap} Power Scroll. No special pet-only effect.",
            HavenBondingPotion => "Instantly bonds one living, bondable pet you own. Consumed only on success.",
            HavenPetDye => "Choose one of eight colors or restore your pet's original color. One use. Works on your pet or its shrunken token. Appearance only; stats and rarity stay the same.",
            HavenPetLeash => "Reusable portable pet shrinking tool. Keep it in your pack. Target your living pet within 3 tiles; no charges.",
            HavenHouseHitchingPost => "Place in a house you own or co-own, then lock it down. Reusable free pet shrinking, with the same safety rules as town posts.",
            HavenPetScrollBundle => "Six pet cap scrolls: Wrestling, Tactics, Anatomy, Magic Resist, Meditation and Focus. Double-click each scroll and target your pet.",
            PowerScroll scroll => $"Raises the {scroll.Skill} skill cap to {scroll.Value:0}. This raises the cap, not the current skill. Native total skill cap remains.",
            HavenLevelingCape cape => $"Level {cape.Level}/20; XP {cape.Experience:N0}. Wear it to gain shared monster and companion experience. Free starter cape; repairs and restoration are free.",
            HavenRunicAtlas => "48 marked locations in three 16-location chapters. Drop marked runes onto the atlas; use a chapter for normal recall, gate, charges and rune removal.",
            Runebook => "Stores 16 marked locations. Uses normal recall/gate spells or stored recall-scroll charges.",
            Spellbook book => $"Contains all {book.BookCount} spells for this school. Normal skill and expansion requirements apply.",
            HavenSmallSoulForgeDeed => "Place in your house: working forge and Haven Abyss artifice recipes. Requires normal recipe materials; not full native Imbuing.",
            WorldMap => "One world map plus one treasure map can be offered at the Soulbinder altar for a three-hour Corgul island chart. The ritual reduces health to 1 HP.",
            FabledFishingNet => "White net: 25% Scalis chance at 100 Fishing if none is alive on the facet. Otherwise reports his coordinates without being consumed. Normal deep-water rules apply.",
            BagOfSending bag => $"Sends eligible items to your bank. Charges: {bag.Charges}. Recharge with translocation powder.",
            PowderOfTranslocation => "Ten doses for recharging bags of sending. Native recharge limits apply.",
            PowderOfTemperament => "Ten uses of fortification powder to restore eligible equipment maximum durability.",
            ClothingBlessDeed => "Blesses one eligible piece of your clothing.",
            HavenEquipmentBlessDeed => "Blesses one eligible weapon, armor piece, clothing item or jewelry item belonging to you.",
            _ => ""
        });
        return string.Join("<BR>", lines);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (info.ButtonID == 0 || from == null) { return; }
        if (!HavenShopAccess.CanUse(from, _stone))
        {
            from.SendMessage("Return to the stone before buying.");
            return;
        }
        if (info.ButtonID == 1) { _menu.OnResponse(sender, _index); }
        if (info.ButtonID is 1 or 2) { from.SendGump(new HavenListGump(_stone, _menu, _page)); }
    }
}

public static class HavenShopAccess
{
    public static bool CanUse(Mobile from, Item anchor) => from?.Deleted == false && anchor?.Deleted == false &&
        (anchor is AdventurersWallet ? from.Backpack != null && anchor.IsChildOf(from.Backpack) :
        from.Map == anchor.Map && from.InRange(anchor.GetWorldLocation(), 3));
}
