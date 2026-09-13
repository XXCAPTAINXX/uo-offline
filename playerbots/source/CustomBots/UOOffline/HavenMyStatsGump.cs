using System;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.UOOffline;

public sealed class HavenMyStatsGump : Gump
{
    private readonly PlayerMobile _owner;
    private readonly int _page;
    public static void Initialize() => CommandSystem.Register("MyStats", AccessLevel.Player, e => DisplayTo(e.Mobile));
    public static void DisplayTo(Mobile mobile, int page = 0)
    {
        if (mobile is not PlayerMobile player || player.Deleted) { return; }
        player.CloseGump<HavenMyStatsGump>();
        player.SendGump(new HavenMyStatsGump(player, page));
    }
    internal HavenMyStatsGump(PlayerMobile owner, int page = 0) : base(30, 40)
    {
        _owner = owner; _page = Math.Clamp(page, 0, 1);
        AddBackground(0, 0, 560, 490, 5054);
        AddBackground(12, 12, 536, 466, 3000);
        AddLabel(25, 22, 0, $"{owner.Name ?? "Player"} - My stats");
        Button(25, 52, 1, "Overview"); Button(195, 52, 2, "Combat bonuses");
        if (_page == 0) { Overview(owner); } else { Combat(owner); }
        Button(25, 444, 3, "Refresh"); Button(420, 444, 0, "Close");
    }
    private void Overview(PlayerMobile p)
    {
        AddLabel(25, 91, 0, "Attribute"); AddLabel(205, 91, 0, "Base"); AddLabel(305, 91, 0, "Bonus"); AddLabel(420, 91, 0, "Total");
        Stat(116, "Strength", p.RawStr, p.Str); Stat(141, "Dexterity", p.RawDex, p.Dex); Stat(166, "Intelligence", p.RawInt, p.Int);
        Row(200, "Health", $"{p.Hits:N0} / {p.HitsMax:N0}");
        Row(223, "Stamina", $"{p.Stam:N0} / {p.StamMax:N0}");
        Row(246, "Mana", $"{p.Mana:N0} / {p.ManaMax:N0}");
        Row(275, "Base stat budget", $"{p.RawStr + p.RawDex + p.RawInt:N0} / {p.StatCap:N0}");
        Row(298, "Counted skill budget", $"{HavenFreeSkills.CountedTotal(p) / 10.0:F1} / {p.SkillsCap / 10.0:F1}");
        Row(321, "Followers", $"{p.Followers} / {p.FollowersMax}");
        Row(344, "Total luck", $"{p.Luck:N0} (Haven island: +{HavenNewcomerLuck.GetBonus(p):N0})");
        Row(367, "Chivalry tithing", $"{p.TithingPoints:N0}");
        AddHtml(25, 397, 510, 38, "Bonus is the effective change from gear and active effects after stat limits. Taming, Lore, Focus and Snooping do not count toward the skill budget.");
    }
    private void Combat(PlayerMobile p)
    {
        (string label, AosAttribute attribute)[] attributes =
        [
            ("Weapon damage", AosAttribute.WeaponDamage), ("Swing speed", AosAttribute.WeaponSpeed),
            ("Hit chance", AosAttribute.AttackChance), ("Defense chance", AosAttribute.DefendChance),
            ("Spell damage", AosAttribute.SpellDamage), ("Lower mana cost", AosAttribute.LowerManaCost),
            ("Lower reagent cost", AosAttribute.LowerRegCost), ("Faster casting", AosAttribute.CastSpeed),
            ("Cast recovery", AosAttribute.CastRecovery), ("Hit regeneration", AosAttribute.RegenHits),
            ("Stamina regeneration", AosAttribute.RegenStam), ("Mana regeneration", AosAttribute.RegenMana)
        ];
        for (var i = 0; i < attributes.Length; i++)
        {
            var column = i / 6; var y = 96 + i % 6 * 27; var x = 25 + column * 270;
            AddLabel(x, y, 0, attributes[i].label);
            var value = AosAttributes.GetValue(p, attributes[i].attribute);
            AddLabel(x + 194, y, 0, $"{value:N0}{(i < 7 ? "%" : "")}");
        }
        AddLabel(25, 272, 0, "Resistances (current)");
        AddLabel(25, 297, 0, $"Physical {p.PhysicalResistance}%    Fire {p.FireResistance}%    Cold {p.ColdResistance}%");
        AddLabel(25, 322, 0, $"Poison {p.PoisonResistance}%    Energy {p.EnergyResistance}%");
        if (p.Weapon is BaseWeapon weapon)
        {
            weapon.GetStatusDamage(p, out var min, out var max);
            AddLabel(25, 353, 0, $"Weapon damage range: {min} - {max}");
        }
        AddHtml(25, 391, 510, 43, "Bonuses combine equipment, sets and supported effects. Combat caps and target defenses still apply. Regeneration values are bonus points, not healing per second.");
    }
    private void Stat(int y, string name, int raw, int total)
    {
        AddLabel(25, y, 0, name); AddLabel(205, y, 0, $"{raw:N0}");
        AddLabel(305, y, total >= raw ? 0x3F : 0x22, $"{total - raw:+0;-0;0}"); AddLabel(420, y, 0, $"{total:N0}");
    }
    private void Row(int y, string label, string value) { AddLabel(25, y, 0, label); AddLabel(225, y, 0, value); }
    private void Button(int x, int y, int id, string label) { AddButton(x, y, 4005, 4007, id); AddLabel(x + 32, y + 2, 0, label); }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile != _owner || _owner.Deleted || info.ButtonID == 0) { return; }
        if (info.ButtonID is >= 1 and <= 3) { DisplayTo(_owner, info.ButtonID == 3 ? _page : info.ButtonID - 1); }
    }
}
