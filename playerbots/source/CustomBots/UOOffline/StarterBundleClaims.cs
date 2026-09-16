using System;
using ModernUO.Serialization;
using Server.Accounting;
using Server.Items;
using Server.Multis.Deeds;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class StarterFortuneEarrings : GoldEarrings
{
    public override string DefaultName => "Starter Fortune Earrings";

    [Constructible]
    public StarterFortuneEarrings()
    {
        Hue = 0x501;
        LootType = LootType.Blessed;
        Attributes.LowerRegCost = 100;
        Attributes.Luck = 200;
    }
}

public static class StarterBundleClaims
{
    private static string ClaimKey(Mobile from) => $"HavenStarterBundle:{from.Serial}";

    internal static void MarkClaimed(Mobile from) =>
        (from.Account as Account)?.SetTag(ClaimKey(from), "claimed");

    public static void Claim(Mobile from)
    {
        if (from?.Backpack == null || !from.Player || from.Account is not Account account)
        {
            from?.SendMessage("Log in with a player character to claim your starter bundle.");
            return;
        }
        if (account.GetTag(ClaimKey(from)) != null)
        {
            from.SendMessage("This character has already received its starter bundle. Supply stones sell replacements.");
            return;
        }

        var bundle = new Bag { Name = "New player bundle", LootType = LootType.Blessed };
        AddMissing<NewHavenAdventurersRobe>(from, bundle, () => new NewHavenAdventurersRobe());
        AddMissing<ApprenticeGrimoire>(from, bundle, () => new ApprenticeGrimoire());
        AddMissing<StarterFortuneEarrings>(from, bundle, () => new StarterFortuneEarrings());
        AddMissing<HavenStarterSash>(from, bundle, () => new HavenStarterSash());
        AddMissing<HavenLevelingCape>(from, bundle, () => new HavenLevelingCape { BoundTo = from });
        var weapon = StarterProvisioner.SelectStarterWeapon(from);
        var needsArrows = weapon is ApprenticeBow;
        if (!HasStarterWeapon(from))
        {
            StarterSupplyStone.BindStarterItem(weapon, from);
            bundle.DropItem(weapon);
        }
        else { weapon.Delete(); }
        if (needsArrows) { AddMissing<Arrow>(from, bundle, () => new Arrow(100)); }
        AddMissing<AdventurersWallet>(from, bundle, () => new AdventurersWallet());
        AddMissing<CleanupTrashBag>(from, bundle, () => new CleanupTrashBag());
        AddMissing<HavenRunePouch>(from, bundle, () => new HavenRunePouch());
        AddMissing<HavenFieldGuideBook>(from, bundle, () => new HavenFieldGuideBook());
        AddMissing<SmallBrickHouseDeed>(from, bundle, () => new SmallBrickHouseDeed { LootType = LootType.Blessed });
        AddMissing<Bandage>(from, bundle, () => new Bandage(50));

        if (bundle.Items.Count == 0)
        {
            bundle.Delete();
            MarkClaimed(from);
            from.SendMessage("Your starter equipment is already present. Your bundle is recorded as claimed.");
        }
        else if (from.Backpack.TryDropItem(from, bundle, false))
        {
            MarkClaimed(from);
            from.SendMessage("Your new player bundle is in your backpack. Existing starter items were kept.");
        }
        else
        {
            bundle.Delete();
            from.SendMessage("Make room in your backpack and claim again. Your bundle has not been used.");
        }
    }

    private static bool HasStarterWeapon(Mobile from) =>
        Has<ApprenticeBlade>(from) || Has<ApprenticeFencer>(from) || Has<ApprenticeMace>(from) || Has<ApprenticeBow>(from);

    private static bool Has<T>(Mobile from) where T : Item
    {
        foreach (var item in from.Items)
        {
            if (item is T && UsableBy(item, from)) { return true; }
        }
        foreach (var item in from.Backpack.FindItemsByType<T>())
        {
            if (UsableBy(item, from)) { return true; }
        }
        foreach (var item in from.BankBox.FindItemsByType<T>())
        {
            if (UsableBy(item, from)) { return true; }
        }
        return false;
    }

    private static bool UsableBy(Item item, Mobile from) => item switch
    {
        NewHavenAdventurersRobe robe => robe.BoundTo == null || robe.BoundTo == from,
        ApprenticeGrimoire book => book.BoundTo == null || book.BoundTo == from,
        IEvolvingStarterWeapon weapon => weapon.BoundTo == null || weapon.BoundTo == from,
        _ => true
    };

    private static void AddMissing<T>(Mobile from, Bag bundle, Func<Item> create) where T : Item
    {
        if (Has<T>(from)) { return; }
        var item = create();
        StarterSupplyStone.BindStarterItem(item, from);
        bundle.DropItem(item);
    }
}

