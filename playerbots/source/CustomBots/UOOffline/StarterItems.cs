using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

public interface IStarterUpgradeable
{
    int UpgradeTier { get; }
    int MaxUpgradeTier { get; }
    bool TryUpgrade(Mobile from);
}

[SerializationGenerator(0)]
public partial class NewHavenAdventurersRobe : BaseOuterTorso, IStarterUpgradeable
{
    [SerializableField(0)]
    [InvalidateProperties]
    private int _upgradeTier;

    [SerializableField(1)]
    private Mobile _boundTo;

    public override string DefaultName => "New Haven adventurer's robe";
    public int MaxUpgradeTier => 4;

    [Constructible]
    public NewHavenAdventurersRobe() : base(0x1F03, 0x59B)
    {
        LootType = LootType.Blessed;
        ApplyTier();
    }

    public void BindTo(Mobile mobile) => BoundTo = mobile;

    public override bool CanEquip(Mobile from) =>
        (BoundTo == null || BoundTo == from) && base.CanEquip(from);

    public bool TryUpgrade(Mobile from)
    {
        if (BoundTo != null && BoundTo != from)
        {
            from.SendMessage("This starter robe is bound to another character.");
            return false;
        }

        if (UpgradeTier >= MaxUpgradeTier)
        {
            from.SendMessage("This robe has reached its current maximum upgrade tier.");
            return false;
        }

        UpgradeTier++;
        ApplyTier();
        InvalidateProperties();
        from.SendMessage($"Your starter robe advances to upgrade tier {UpgradeTier}.");
        return true;
    }

    internal void ApplyTier()
    {
        Attributes.Luck = 50 + UpgradeTier * 25;
        Attributes.RegenHits = 1 + UpgradeTier / 2;
        Attributes.RegenMana = 1 + UpgradeTier / 2;
        Attributes.LowerManaCost = 3 + UpgradeTier;
        Attributes.BonusHits = UpgradeTier >= 3 ? 3 : 0;
        Attributes.BonusMana = UpgradeTier >= 2 ? 4 : 0;
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"Starter upgrade tier: {UpgradeTier}/{MaxUpgradeTier}");
        if (BoundTo != null)
        {
            list.Add($"Bound to: {BoundTo.Name}");
        }
    }
}

[SerializationGenerator(0)]
public partial class ApprenticeGrimoire : Spellbook
{
    [SerializableField(0)]
    [InvalidateProperties]
    private int _level;

    [SerializableField(1)]
    [InvalidateProperties]
    private int _experience;

    [SerializableField(2)]
    private Mobile _boundTo;

    public override string DefaultName => "apprentice grimoire";
    public const int MaxLevel = 20;

    [Constructible]
    public ApprenticeGrimoire() : base(ulong.MaxValue)
    {
        LootType = LootType.Blessed;
        Level = 1;
        ApplyLevelBonuses();
    }

    public void BindTo(Mobile mobile) => BoundTo = mobile;

    public void GainCastExperience(Mobile caster)
    {
        if (caster == null || caster != BoundTo || Level >= MaxLevel)
        {
            return;
        }

        Experience++;

        var needed = 20 + Level * 10;
        if (Experience < needed)
        {
            return;
        }

        Experience -= needed;
        Level++;
        ApplyLevelBonuses();
        InvalidateProperties();

        caster.SendMessage($"Your apprentice grimoire has reached level {Level}.");
    }

    private void ApplyLevelBonuses()
    {
        Attributes.Luck = 25 + Math.Min(Level, 10) * 10;
        Attributes.BonusMana = Math.Min(10, Level / 2);
        Attributes.LowerManaCost = Math.Min(10, Level / 2);
        Attributes.RegenMana = Math.Min(3, Level / 6);
        Attributes.CastRecovery = Level >= 10 ? 1 : 0;
        Attributes.CastSpeed = Level >= 20 ? 1 : 0;
    }

    public override bool CanEquip(Mobile from) =>
        (BoundTo == null || BoundTo == from) && base.CanEquip(from);

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"Evolution level: {Level}/{MaxLevel}");
        if (Level < MaxLevel)
        {
            list.Add($"Cast experience: {Experience}/{20 + Level * 10}");
        }

        if (BoundTo != null)
        {
            list.Add($"Bound to: {BoundTo.Name}");
        }
    }
}

[SerializationGenerator(0)]
public partial class AdventurersWallet : Item
{
    [SerializableField(0)]
    [InvalidateProperties]
    private long _balance;

    public override string DefaultName => "adventurer's wallet";

    [Constructible]
    public AdventurersWallet() : base(0xE79)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;
    }

    public override void OnDoubleClick(Mobile from)
    {
        var pack = from.Backpack;

        if (pack == null || !IsChildOf(pack))
        {
            from.SendMessage("The wallet must be in your backpack to deposit gold.");
            return;
        }

        var coins = new List<Gold>();
        foreach (var gold in pack.FindItemsByType<Gold>())
        {
            // Never sweep gold that somehow ended up inside this wallet item.
            if (!gold.Deleted)
            {
                coins.Add(gold);
            }
        }

        long deposited = 0;
        foreach (var gold in coins)
        {
            deposited += gold.Amount;
            gold.Delete();
        }

        if (deposited <= 0)
        {
            from.SendMessage($"Your wallet contains {Balance:N0} gold.");
            return;
        }

        Balance += deposited;
        InvalidateProperties();
        from.SendMessage($"{deposited:N0} gold deposited into your wallet. Balance: {Balance:N0}.");
    }

    public bool TrySpend(long amount)
    {
        if (amount <= 0 || Balance < amount)
        {
            return false;
        }

        Balance -= amount;
        InvalidateProperties();
        return true;
    }

    public void Deposit(long amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Balance += amount;
        InvalidateProperties();
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"Stored gold: {Balance:N0}");
        list.Add("Double-click to deposit backpack gold");
    }
}

public static class StarterProgression
{
    public static void OnSuccessfulSpellCast(Mobile caster)
    {
        if (caster == null)
        {
            return;
        }
        if (caster.FindItemOnLayer(Layer.OneHanded) is ApprenticeGrimoire grimoire && grimoire.BoundTo == caster)
        {
            grimoire.GainCastExperience(caster);
            return;
        }
        if (caster.Backpack != null)
        {
            foreach (var book in caster.Backpack.FindItemsByType<ApprenticeGrimoire>())
            {
                if (book.BoundTo == caster)
                {
                    book.GainCastExperience(caster);
                    return;
                }
            }
        }
    }
}
