using System;
using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

public interface IEvolvingStarterWeapon
{
    int Level { get; set; }
    int Experience { get; set; }
    Mobile BoundTo { get; set; }
}

public static class StarterWeaponProgression
{
    public const int MaxLevel = 20;

    public static void Initialize(IEvolvingStarterWeapon progression, BaseWeapon weapon)
    {
        if (progression.Level <= 0)
        {
            progression.Level = 1;
        }

        ApplyBonuses(progression, weapon);
    }

    public static void GainExperience(
        IEvolvingStarterWeapon progression,
        BaseWeapon weapon,
        Mobile attacker,
        Mobile defender
    )
    {
        if (attacker == null || defender == null || !attacker.Player || defender.Player ||
            progression.BoundTo != attacker || progression.Level >= MaxLevel)
        {
            return;
        }

        GainSharedExperience(progression, weapon, attacker, 1);
    }

    internal static void GainSharedExperience(IEvolvingStarterWeapon progression, BaseWeapon weapon, Mobile attacker, int amount)
    {
        if (progression.BoundTo != attacker || progression.Level >= MaxLevel || amount <= 0) { return; }
        progression.Experience += amount;

        var needed = 20 + progression.Level * 10;
        if (progression.Experience < needed)
        {
            return;
        }

        progression.Experience -= needed;
        progression.Level++;
        ApplyBonuses(progression, weapon);
        weapon.InvalidateProperties();

        attacker.SendMessage($"{weapon.DefaultName} has reached level {progression.Level}.");
    }

    public static void ApplyBonuses(IEvolvingStarterWeapon progression, BaseWeapon weapon)
    {
        var level = Math.Clamp(progression.Level, 1, MaxLevel);
        HavenWeaponManaSustain.Apply(weapon, level);

        weapon.Attributes.WeaponDamage = Math.Min(20, level);
        weapon.Attributes.AttackChance = Math.Min(10, level / 2);
        weapon.Attributes.BonusStam = Math.Min(8, level / 3);

        if (level >= 10)
        {
            weapon.Attributes.RegenStam = 1;
        }

        if (level >= 15)
        {
            weapon.Attributes.RegenHits = 1;
        }
    }

    public static void AddProperties(IEvolvingStarterWeapon progression, IPropertyList list)
    {
        list.Add($"Evolution level: {progression.Level}/{MaxLevel}");

        if (progression.Level < MaxLevel)
        {
            list.Add($"Combat experience: {progression.Experience}/{20 + progression.Level * 10}");
        }

        if (progression.BoundTo != null)
        {
            list.Add($"Bound to: {progression.BoundTo.Name}");
        }
    }
}

[SerializationGenerator(0)]
public partial class ApprenticeBlade : Longsword, IEvolvingStarterWeapon
{
    [SerializableField(0)]
    [InvalidateProperties]
    private int _level;

    [SerializableField(1)]
    [InvalidateProperties]
    private int _experience;

    [SerializableField(2)]
    private Mobile _boundTo;

    public override string DefaultName => "apprentice blade";

    [Constructible]
    public ApprenticeBlade()
    {
        LootType = LootType.Blessed;
        StarterWeaponProgression.Initialize(this, this);
    }

    [AfterDeserialization] private void UpdateManaLeech() => HavenWeaponManaSustain.Apply(this, Level);

    public void BindTo(Mobile mobile) => BoundTo = mobile;

    public override bool CanEquip(Mobile from) =>
        (BoundTo == null || BoundTo == from) && base.CanEquip(from);

    public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
    {
        base.OnHit(attacker, defender, damageBonus);
        StarterWeaponProgression.GainExperience(this, this, attacker, defender);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        StarterWeaponProgression.AddProperties(this, list);
    }
}

[SerializationGenerator(0)]
public partial class ApprenticeFencer : Kryss, IEvolvingStarterWeapon
{
    [SerializableField(0)]
    [InvalidateProperties]
    private int _level;

    [SerializableField(1)]
    [InvalidateProperties]
    private int _experience;

    [SerializableField(2)]
    private Mobile _boundTo;

    public override string DefaultName => "apprentice fencer";

    [Constructible]
    public ApprenticeFencer()
    {
        LootType = LootType.Blessed;
        StarterWeaponProgression.Initialize(this, this);
    }

    [AfterDeserialization] private void UpdateManaLeech() => HavenWeaponManaSustain.Apply(this, Level);

    public void BindTo(Mobile mobile) => BoundTo = mobile;

    public override bool CanEquip(Mobile from) =>
        (BoundTo == null || BoundTo == from) && base.CanEquip(from);

    public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
    {
        base.OnHit(attacker, defender, damageBonus);
        StarterWeaponProgression.GainExperience(this, this, attacker, defender);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        StarterWeaponProgression.AddProperties(this, list);
    }
}

[SerializationGenerator(0)]
public partial class ApprenticeMace : WarMace, IEvolvingStarterWeapon
{
    [SerializableField(0)]
    [InvalidateProperties]
    private int _level;

    [SerializableField(1)]
    [InvalidateProperties]
    private int _experience;

    [SerializableField(2)]
    private Mobile _boundTo;

    public override string DefaultName => "apprentice mace";

    [Constructible]
    public ApprenticeMace()
    {
        LootType = LootType.Blessed;
        StarterWeaponProgression.Initialize(this, this);
    }

    [AfterDeserialization] private void UpdateManaLeech() => HavenWeaponManaSustain.Apply(this, Level);

    public void BindTo(Mobile mobile) => BoundTo = mobile;

    public override bool CanEquip(Mobile from) =>
        (BoundTo == null || BoundTo == from) && base.CanEquip(from);

    public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
    {
        base.OnHit(attacker, defender, damageBonus);
        StarterWeaponProgression.GainExperience(this, this, attacker, defender);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        StarterWeaponProgression.AddProperties(this, list);
    }
}

[SerializationGenerator(0)]
public partial class ApprenticeBow : Bow, IEvolvingStarterWeapon
{
    [SerializableField(0)]
    [InvalidateProperties]
    private int _level;

    [SerializableField(1)]
    [InvalidateProperties]
    private int _experience;

    [SerializableField(2)]
    private Mobile _boundTo;

    public override string DefaultName => "apprentice bow";

    [Constructible]
    public ApprenticeBow()
    {
        LootType = LootType.Blessed;
        StarterWeaponProgression.Initialize(this, this);
    }

    [AfterDeserialization] private void UpdateManaLeech() => HavenWeaponManaSustain.Apply(this, Level);

    public void BindTo(Mobile mobile) => BoundTo = mobile;

    public override bool CanEquip(Mobile from) =>
        (BoundTo == null || BoundTo == from) && base.CanEquip(from);

    public override void OnHit(Mobile attacker, Mobile defender, double damageBonus = 1)
    {
        base.OnHit(attacker, defender, damageBonus);
        StarterWeaponProgression.GainExperience(this, this, attacker, defender);
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        StarterWeaponProgression.AddProperties(this, list);
    }
}
