using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenBossArtifact : Item
{
    [SerializableField(0)] private int _kind;
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenBossArtifact() : base(1) { Name = "encounter artifact"; Visible = false; Movable = false; Weight = 0; }
    internal static bool IsArtifact(Item item)
    { foreach (var child in item.Items) { if (child is HavenBossArtifact) { return true; } } return false; }
    internal static void Grow(Item item, int levels, int milestones)
    {
        if (!IsArtifact(item) || item is not IAosItem gear) { return; }
        gear.Attributes.WeaponDamage += levels; gear.Attributes.SpellDamage += levels;
        gear.Attributes.RegenHits += milestones; gear.Attributes.RegenMana += milestones;
        if (item is BaseWeapon weapon)
        { weapon.Attributes.WeaponSpeed += milestones * 2; weapon.WeaponAttributes.HitLeechMana += milestones * 3; }
    }
}

internal static class HavenScalisLoot
{
    internal static Item Artifact(int kind)
    {
        Item item;
        switch (kind)
        {
            case 0:
                var coral = new GoldBracelet { Name = "Enchanted Coral Bracelet", Hue = 0x48F };
                coral.Attributes.BonusHits = 5; coral.Attributes.RegenMana = 1; coral.Attributes.AttackChance = 5;
                coral.Attributes.DefendChance = 15; coral.Attributes.SpellDamage = 10; coral.Attributes.CastRecovery = 3; coral.Attributes.CastSpeed = 1;
                item = coral; break;
            case 1:
                var bracers = new LeatherArms { Name = "Leviathan Hide Bracers", Hue = 0x481 };
                bracers.Attributes.BonusInt = 6; bracers.Attributes.RegenStam = 2; bracers.Attributes.RegenMana = 2;
                bracers.Attributes.LowerManaCost = 8; bracers.Attributes.LowerRegCost = 10; Resists(bracers, 4, 9, 10, 13, 14);
                item = bracers; break;
            case 2:
                var wand = new MagicWand { Name = "Illustrious Wand of Thundering Glory", Hue = 0x47E, MinDamage = 11, MaxDamage = 15, Speed = 2.75f };
                wand.Attributes.SpellChanneling = 1; wand.Attributes.AttackChance = 5; wand.Attributes.WeaponDamage = 50;
                wand.WeaponAttributes.HitLightning = 40; wand.AosElementDamages.Chaos = 100; item = wand; break;
            case 3:
                var moon = new Scimitar { Name = "Smiling Moon Blade", Hue = 0x47E, MinDamage = 12, MaxDamage = 15, Speed = 2.5f, StrRequirement = 55, Layer = Layer.TwoHanded };
                moon.Attributes.WeaponDamage = 45; moon.Attributes.WeaponSpeed = 30; moon.WeaponAttributes.HitFireball = 45;
                moon.WeaponAttributes.HitLowerDefend = 40; moon.WeaponAttributes.HitLeechMana = 10; moon.WeaponAttributes.ResistEnergyBonus = 5;
                moon.AosElementDamages.Cold = 100; item = moon; break;
            case 4:
                var sash = new BodySash { Name = "Corgul's Enchanted Sash", Hue = 0x455 };
                sash.Attributes.BonusStam = 1; sash.Attributes.DefendChance = 5; item = sash; break;
            case 5:
            case 6:
                Spellbook book = kind == 5 ? new MysticSpellbook() : new NecromancerSpellbook();
                book.Name = kind == 5 ? "Corgul's Handbook on Mysticism" : "Corgul's Handbook on the Undead";
                book.Content = (1UL << book.BookCount) - 1; book.Attributes.RegenMana = 3; book.Attributes.DefendChance = 5;
                book.Attributes.LowerManaCost = 10; book.Attributes.LowerRegCost = 20; item = book; break;
            case 7:
                var ring = new GoldRing { Name = "Ring of the Soulbinder", Hue = 0x455 };
                ring.Attributes.RegenMana = 2; ring.Attributes.DefendChance = 15; ring.Attributes.SpellDamage = 10;
                ring.Attributes.CastRecovery = 3; ring.Attributes.CastSpeed = 1; ring.Attributes.LowerRegCost = 10; item = ring; break;
            case 8:
                var helm = new PlateHelm { Name = "Helm of Vengeance", Hue = 0x455 };
                helm.Attributes.RegenMana = 3; helm.Attributes.ReflectPhysical = 30; helm.Attributes.AttackChance = 7;
                helm.Attributes.LowerManaCost = 8; helm.Attributes.WeaponDamage = 10; Resists(helm, 11, 10, 14, 7, 8); item = helm; break;
            case 9:
                var peg = new Club { Name = "Rune Engraved Pegleg", Hue = 0x455, MinDamage = 10, MaxDamage = 14, Speed = 2.5f };
                peg.Attributes.RegenHits = 3; peg.Attributes.AttackChance = 5; peg.Attributes.WeaponSpeed = 30; peg.Attributes.WeaponDamage = 50;
                peg.WeaponAttributes.HitLightning = 40; peg.WeaponAttributes.HitLowerDefend = 40; peg.AosElementDamages.Chaos = 100; item = peg; break;
            case 10:
                var culling = new Scimitar { Name = "The Culling Blade", Hue = 0x455, MinDamage = 12, MaxDamage = 16, Speed = 3f };
                culling.Attributes.WeaponDamage = 50; culling.Attributes.WeaponSpeed = 20; culling.WeaponAttributes.HitLowerDefend = 40;
                culling.WeaponAttributes.HitLeechMana = 30; culling.WeaponAttributes.HitLeechStam = 30; culling.AosElementDamages.Chaos = 100; item = culling; break;
            case 11:
                var bow = new Bow { Name = "Blight of the Tundra", Hue = 0x481, Slayer = SlayerName.Repond };
                bow.Attributes.RegenStam = 10; bow.Attributes.WeaponDamage = 50; bow.Attributes.WeaponSpeed = 45;
                bow.Attributes.AttackChance = 15; bow.Attributes.BonusStr = 5; bow.WeaponAttributes.ResistColdBonus = 15; bow.AosElementDamages.Cold = 100; item = bow; break;
            case 12:
                var protection = new GoldBracelet { Name = "Bracelet of Protection", Hue = 0x48E };
                protection.Attributes.BonusHits = 5; protection.Attributes.RegenHits = 10; protection.Attributes.DefendChance = 5;
                // Later-era damage eater is represented by a working all-resistance bonus in this build.
                protection.Resistances.Physical = 3; protection.Resistances.Fire = 3; protection.Resistances.Cold = 3;
                protection.Resistances.Poison = 3; protection.Resistances.Energy = 3; item = protection; break;
            case 13:
                var bright = new Broadsword { Name = "Brightblade", Hue = 0x47E, MinDamage = 10, MaxDamage = 14, Speed = 2.5f, StrRequirement = 25 };
                bright.Attributes.WeaponDamage = 50; bright.Attributes.WeaponSpeed = 30; bright.Attributes.AttackChance = 15;
                bright.WeaponAttributes.HitLightning = 35; item = bright; break;
            case 14:
                var shield = new MetalShield { Name = "Hephaestus", Hue = 0x489 };
                shield.SkillBonuses.SetValues(0, SkillName.Parry, 10); shield.Attributes.SpellChanneling = 1;
                shield.Attributes.ReflectPhysical = 15; shield.Attributes.DefendChance = 15; shield.Attributes.CastSpeed = 1;
                shield.Attributes.LowerManaCost = 8; shield.ArmorAttributes.SelfRepair = 5; Resists(shield, 15, 1, 0, 0, 0); item = shield; break;
            case 15:
                var glasses = new ElvenGlasses { Name = "Prismatic Lenses", Hue = 0x48E };
                glasses.Attributes.RegenHits = 2; glasses.Attributes.RegenStam = 3; glasses.Attributes.WeaponDamage = 25;
                glasses.WeaponAttributes.HitLowerDefend = 30; Resists(glasses, 18, 4, 7, 17, 6); item = glasses; break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
        if (item is BaseWeapon weapon) { weapon.MaxHitPoints = 255; weapon.HitPoints = 255; }
        if (item is BaseArmor armor) { armor.MaxHitPoints = 255; armor.HitPoints = 255; }
        item.AddItem(new HavenBossArtifact { Kind = kind }); return item;
    }
    private static void Resists(BaseArmor armor, int physical, int fire, int cold, int poison, int energy)
    {
        armor.PhysicalBonus += physical - armor.PhysicalResistance; armor.FireBonus += fire - armor.FireResistance;
        armor.ColdBonus += cold - armor.ColdResistance; armor.PoisonBonus += poison - armor.PoisonResistance; armor.EnergyBonus += energy - armor.EnergyResistance;
    }
}

[SerializationGenerator(0)]
public partial class HavenSmallSoulForge : BaseAddon
{
    [Constructible]
    public HavenSmallSoulForge() { Name = "small soul forge"; AddComponent(new ForgeComponent(17607), 0, 0, 0); }
    public override BaseAddonDeed Deed => new HavenSmallSoulForgeDeed();
    public override void OnComponentUsed(AddonComponent component, Mobile from) => HavenAbyssArtifice.Open(this, from);
}

[SerializationGenerator(0)]
public partial class HavenSmallSoulForgeDeed : BaseAddonDeed
{
    [Constructible]
    public HavenSmallSoulForgeDeed() { Name = "small soul forge deed"; LootType = LootType.Blessed; }
    public override BaseAddon Addon => new HavenSmallSoulForge();
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Scalis rare reward"}");
        list.Add($"{"One-tile house forge:"} {"blacksmithing and Haven Abyss artifice"}");
        list.Add($"{"Double-click the placed forge to attune equipment"}");
    }
}
