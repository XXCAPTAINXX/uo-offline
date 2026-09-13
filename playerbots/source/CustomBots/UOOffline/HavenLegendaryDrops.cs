using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenLegendaryArtifact : Item
{
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenLegendaryArtifact() : base(1) { Name = "Legendary Artifact"; Visible = false; Movable = false; Weight = 0; }
    internal static bool IsLegendary(Item item)
    {
        foreach (var child in item.Items) { if (child is HavenLegendaryArtifact) { return true; } }
        return false;
    }
}

public static class HavenLegendaryDrops
{
    internal static double Chance(BaseCreature creature, Mobile player)
    {
        var basis = creature.HitsMax >= 5000 ? 0.05 : creature is OldHavenWarden or HavenTrialCreature { TrialStage: 4 } ? 0.02 :
            creature.HitsMax >= 1000 ? 0.01 : creature.HitsMax >= 300 ? 0.003 : 0.001;
        return basis * (1 + Math.Clamp(player.Luck, 0, 5000) / 5000.0);
    }
    internal static bool TryAward(BaseCreature creature, Mobile player, double roll)
    {
        if (!HavenAstralRewards.Eligible(creature, player) || player.Map == null || player.Map == Map.Internal || roll >= Chance(creature, player)) { return false; }
        var item = Create();
        if (player.Backpack?.TryDropItem(player, item, false) == true)
        {
            player.SendMessage(0x489, "You found a Legendary Artifact! It is in your backpack. Equip it to earn shared experience.");
        }
        else if (creature.Corpse is { Deleted: false } corpse)
        {
            corpse.DropItem(item);
            player.SendMessage(0x489, "You found a Legendary Artifact! Your backpack is full; look in the creature's corpse.");
        }
        else
        {
            item.MoveToWorld(player.Location, player.Map);
            player.SendMessage(0x489, "You found a Legendary Artifact! Your backpack is full; it is at your feet.");
        }
        return true;
    }
    internal static Item Create()
    {
        var item = Utility.Random(5) == 0 ? new Spellbook(ulong.MaxValue) : Loot.RandomArmorOrShieldOrWeaponOrJewelry();
        switch (item)
        {
            case BaseWeapon weapon:
                BaseRunicTool.ApplyAttributesTo(weapon, false, 0, 8, 90, 100);
                weapon.Attributes.WeaponDamage = Math.Max(40, weapon.Attributes.WeaponDamage);
                weapon.Attributes.WeaponSpeed = Math.Max(20, weapon.Attributes.WeaponSpeed);
                weapon.WeaponAttributes.HitLeechMana = Math.Max(40, weapon.WeaponAttributes.HitLeechMana);
                weapon.WeaponAttributes.HitLeechHits = Math.Max(40, weapon.WeaponAttributes.HitLeechHits);
                break;
            case BaseArmor armor: BaseRunicTool.ApplyAttributesTo(armor, false, 0, 8, 90, 100); break;
            case BaseHat hat: BaseRunicTool.ApplyAttributesTo(hat, false, 0, 8, 90, 100); break;
            case BaseJewel jewel: BaseRunicTool.ApplyAttributesTo(jewel, false, 0, 8, 90, 100); break;
            case Spellbook book:
                BaseRunicTool.ApplyAttributesTo(book, false, 0, 6, 90, 100);
                book.Attributes.SpellDamage = Math.Max(30, book.Attributes.SpellDamage);
                book.Attributes.LowerManaCost = Math.Max(8, book.Attributes.LowerManaCost);
                break;
        }
        if (item is IAosItem gear)
        {
            gear.Attributes.RegenHits = Math.Max(2, gear.Attributes.RegenHits);
            gear.Attributes.RegenMana = Math.Max(2, gear.Attributes.RegenMana);
        }
        item.AddItem(new HavenLegendaryArtifact());
        item.InvalidateProperties();
        return item;
    }
    internal static void Grow(Item item, int levels, int milestones)
    {
        if (!HavenLegendaryArtifact.IsLegendary(item) || item is not IAosItem gear) { return; }
        if (item is BaseWeapon weapon)
        {
            gear.Attributes.WeaponDamage += levels * 2;
            gear.Attributes.WeaponSpeed += milestones * 5;
            weapon.WeaponAttributes.HitLeechMana += milestones * 5;
            weapon.WeaponAttributes.HitLeechHits += milestones * 5;
        }
        else
        {
            gear.Attributes.SpellDamage += levels;
            gear.Attributes.WeaponDamage += levels;
            gear.Attributes.RegenHits += milestones;
            gear.Attributes.RegenMana += milestones;
        }
    }
}
