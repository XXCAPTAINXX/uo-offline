using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenCompanionBlade : Longsword
{
    [Constructible]
    public HavenCompanionBlade() { Name = "companion's evolving blade"; Hue = 0x489; LootType = LootType.Blessed; }
    public override bool CanEquip(Mobile from) => from is HavenCompanion && base.CanEquip(from);
}
[SerializationGenerator(0)]
public partial class HavenCompanionBow : Bow
{
    [Constructible]
    public HavenCompanionBow() { Name = "companion's evolving bow"; Hue = 0x489; LootType = LootType.Blessed; }
    public override bool CanEquip(Mobile from) => from is HavenCompanion && base.CanEquip(from);
}

public partial class HavenCompanion
{
    internal void UpgradeLegacyStarterBlade()
    {
        if (FindItemOnLayer(Layer.OneHanded) is not Longsword old || old.GetType() != typeof(Longsword) || old.Movable) { return; }
        var blade = new HavenCompanionBlade { Movable = false, MaxHitPoints = old.MaxHitPoints, HitPoints = old.HitPoints, Slayer = old.Slayer, Slayer2 = old.Slayer2 };
        foreach (var attribute in Enum.GetValues<AosAttribute>()) { blade.Attributes[attribute] = old.Attributes[attribute]; }
        foreach (var attribute in Enum.GetValues<AosWeaponAttribute>()) { blade.WeaponAttributes[attribute] = old.WeaponAttributes[attribute]; }
        var progress = HavenGearExperience.Find(old);
        if (progress != null) { blade.AddItem(progress); }
        old.Delete(); AddItem(blade);
    }
    internal void ClaimEvolvingArms(Mobile owner)
    {
        if (owner != BoundOwner || Backpack == null || Expedition != null || owner.Map != Map || !owner.InRange(this, 3)) { return; }
        if (Weapon is not HavenCompanionBlade && Backpack.FindItemByType<HavenCompanionBlade>() == null) { Backpack.DropItem(new HavenCompanionBlade()); }
        if (Weapon is not HavenCompanionBow && Backpack.FindItemByType<HavenCompanionBow>() == null) { Backpack.DropItem(new HavenCompanionBow()); }
        if (FindItemOnLayer(Layer.OneHanded) is not ApprenticeGrimoire && Backpack.FindItemByType<ApprenticeGrimoire>() == null)
        { Backpack.DropItem(new ApprenticeGrimoire { BoundTo = this }); }
        owner.SendMessage("Special evolving blade, bow and spellbook are available in the shared pack. Use Equip item to choose one.");
    }
}
