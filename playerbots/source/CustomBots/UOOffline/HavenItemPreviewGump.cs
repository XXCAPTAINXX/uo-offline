using System;
using System.Collections.Generic;
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
        AddHtml(28, 375, 482, 20, "Gold payments use wallet first, then backpack and bank.");
        AddButton(28, 399, 4014, 4016, 2);
        AddLabel(66, 401, 0, "Back");
        AddButton(200, 399, 4005, 4007, 1);
        AddLabel(238, 401, 0, "Buy item");
        AddButton(390, 399, 4005, 4007, 0);
        AddLabel(428, 401, 0, "Close");
    }

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
            NewHavenAdventurersRobe => "Binds to the buyer. Four upgrade tiers, purchased with Haven marks or gold.",
            ApprenticeGrimoire => "All 64 Magery spells. Binds to the buyer; evolves through spellcasting to level 20.",
            CleanupTrashBag => "Accepts eligible unwanted items; removes them after 3 minutes and awards cleanup points.",
            AdventurersWallet => "Double-click to collect backpack gold. Stone purchases can spend the balance.",
            ProgressionArchive => "Collects compatible progression scrolls, champion skulls, Haven marks, primers and binders.",
            PeerlessKeyVault => "Collects recognized Peerless keys from your backpack. Keys do not expire.",
            OfflineTravelBook => "Reusable travel book. Opens the moongate destination and facet selector from your backpack. No charges or reagents. Normal moongate eligibility applies; unavailable while dead, criminal, in combat or casting.",
            _ => ""
        });
        return string.Join("<BR>", lines);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (info.ButtonID == 0 || from == null) { return; }
        if (_stone.Deleted || from.Map != _stone.Map || !from.InRange(_stone.Location, 3))
        {
            from.SendMessage("Return to the stone before buying.");
            return;
        }
        if (info.ButtonID == 1) { _menu.OnResponse(sender, _index); }
        if (info.ButtonID is 1 or 2) { from.SendGump(new HavenListGump(_stone, _menu, _page)); }
    }
}
