using System;
using ModernUO.Serialization;
using Server.Engines.Spawners;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Mobiles;
using Server.Multis;
using Server.Network;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenMark : Item
{
    public override string DefaultName => "a Haven mark";

    [Constructible]
    public HavenMark(int amount = 1) : base(0x14F0)
    {
        Stackable = true;
        Amount = Math.Max(1, amount);
        Hue = 0x489;
        Weight = 0.1;
    }
}

[SerializationGenerator(0)]
public partial class OldHavenWarden : BaseCreature
{
    public override string DefaultName => "the Old Haven warden";
    public override string CorpseName => "the remains of the Old Haven warden";

    [Constructible]
    public OldHavenWarden() : base(AIType.AI_Melee)
    {
        Body = 0x3CA;
        Hue = 0x455;
        BaseSoundID = 0x107;

        SetStr(145, 175);
        SetDex(70, 90);
        SetInt(55, 75);

        SetHits(425, 525);
        SetDamage(7, 13);

        SetDamageType(ResistanceType.Physical, 70);
        SetDamageType(ResistanceType.Cold, 30);

        SetResistance(ResistanceType.Physical, 25, 35);
        SetResistance(ResistanceType.Fire, 15, 25);
        SetResistance(ResistanceType.Cold, 30, 40);
        SetResistance(ResistanceType.Poison, 15, 25);
        SetResistance(ResistanceType.Energy, 15, 25);

        SetSkill(SkillName.MagicResist, 55.0, 70.0);
        SetSkill(SkillName.Tactics, 65.0, 75.0);
        SetSkill(SkillName.Wrestling, 65.0, 75.0);

        Fame = 3500;
        Karma = -3500;
        VirtualArmor = 28;
    }

    public override bool AlwaysAttackable => true;
    public override bool BleedImmune => true;

    public override void GenerateLoot()
    {
        PackGold(500, 900);
        PackItem(new HavenMark(Utility.RandomMinMax(1, 2)));
        AddLoot(LootPack.Average);

        // The bracelets are a meaningful early chase reward without making
        // the boss an end-game money printer.
        if (Utility.RandomDouble() < 0.15)
        {
            PackItem(SpecialBraceletFactory.CreateRandom());
        }
    }
}

public static class SpecialBraceletFactory
{
    public static Item CreateRandom() =>
        Utility.Random(9) switch
        {
            0 => new BraceletOfTheVanguard(),
            1 => new BraceletOfArcaneFocus(),
            2 => new BraceletOfTheWind(),
            3 => new BraceletOfTheBeastmaster(),
            4 => new BraceletOfTheVirtuoso(),
            5 => new BraceletOfTheArtisan(),
            6 => new BraceletOfFortune(),
            7 => new BraceletOfTheGuardian(),
            _ => new BraceletOfTheNight()
        };

    public static Item Create(int index) =>
        index switch
        {
            0 => new BraceletOfTheVanguard(),
            1 => new BraceletOfArcaneFocus(),
            2 => new BraceletOfTheWind(),
            3 => new BraceletOfTheBeastmaster(),
            4 => new BraceletOfTheVirtuoso(),
            5 => new BraceletOfTheArtisan(),
            6 => new BraceletOfFortune(),
            7 => new BraceletOfTheGuardian(),
            8 => new BraceletOfTheNight(),
            _ => null
        };
}

[SerializationGenerator(0)]
public partial class OldHavenBossSpawner : Spawner
{
    [Constructible]
    public OldHavenBossSpawner() : base(
        1,
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(15),
        0,
        default,
        nameof(OldHavenWarden)
    )
    {
    }
}

[SerializationGenerator(0)]
public partial class VampiricSteedSpawner : Spawner
{
    [Constructible]
    public VampiricSteedSpawner() : base(
        1,
        TimeSpan.FromMinutes(25),
        TimeSpan.FromMinutes(40),
        0,
        default,
        nameof(VampiricSteed)
    )
    {
    }
}

[SerializationGenerator(0)]
public partial class StarterSupplyStone : Item
{
    public override string DefaultName => "starter supplies";

    [Constructible]
    public StarterSupplyStone() : base(0xED4)
    {
        Movable = false;
        Hue = 0x59B;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.SendMessage("You are too far away to use the starter supply stone.");
            return;
        }

        from.SendMenu(new StarterSupplyMenu());
    }

    private sealed class StarterSupplyMenu : ItemListMenu
    {
        private static readonly ItemListEntry[] MenuEntries =
        [
            new("Cleanup trash bag - 100 gold", 0xE76, 0x455),
            new("Adventurer's wallet - 100 gold", 0xE79),
            new("New Haven starter robe - 500 gold", 0x1F03, 0x59B),
            new("Full apprentice grimoire - 500 gold", 0xEFA, 0x482),
            new("Apprentice blade - 250 gold", 0xF61),
            new("Apprentice fencer - 250 gold", 0x1401),
            new("Apprentice mace - 250 gold", 0x1407),
            new("Apprentice bow - 250 gold", 0x13B2),
            new("Champion progression archive - 250 gold", 0x2259, 0x489),
            new("Peerless key vault - 250 gold", 0x9A8, 0x497)
        ];

        public StarterSupplyMenu() : base("New Haven Starter Supplies", MenuEntries)
        {
        }

        public override void OnResponse(NetState state, int index)
        {
            var from = state.Mobile;
            if (from?.Backpack == null || index < 0 || index >= MenuEntries.Length)
            {
                return;
            }

            var price = index switch
            {
                0 or 1 => 100,
                2 or 3 => 500,
                _ => 250
            };

            if (!HavenEconomy.TryPay(from, price))
            {
                from.SendMessage($"You need {price:N0} gold for that item.");
                return;
            }

            Item item = index switch
            {
                0 => new CleanupTrashBag(),
                1 => new AdventurersWallet(),
                2 => new NewHavenAdventurersRobe(),
                3 => new ApprenticeGrimoire(),
                4 => new ApprenticeBlade(),
                5 => new ApprenticeFencer(),
                6 => new ApprenticeMace(),
                7 => new ApprenticeBow(),
                8 => new ProgressionArchive(),
                9 => new PeerlessKeyVault(),
                _ => null
            };

            if (item == null)
            {
                return;
            }

            BindStarterItem(item, from);
            from.Backpack.DropItem(item);
            from.SendMessage($"Purchased {item.DefaultName} for {price:N0} gold.");
        }
    }

    internal static void BindStarterItem(Item item, Mobile from)
    {
        switch (item)
        {
            case NewHavenAdventurersRobe robe:
                robe.BindTo(from);
                break;
            case ApprenticeGrimoire grimoire:
                grimoire.BindTo(from);
                break;
            case ApprenticeBlade blade:
                blade.BindTo(from);
                break;
            case ApprenticeFencer fencer:
                fencer.BindTo(from);
                break;
            case ApprenticeMace mace:
                mace.BindTo(from);
                break;
            case ApprenticeBow bow:
                bow.BindTo(from);
                break;
        }
    }
}

[SerializationGenerator(0)]
public partial class HavenUpgradeStone : Item
{
    public override string DefaultName => "starter gear upgrade stone";

    [Constructible]
    public HavenUpgradeStone() : base(0xED4)
    {
        Movable = false;
        Hue = 0x489;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.SendMessage("You are too far away to use the upgrade stone.");
            return;
        }

        var pack = from.Backpack;
        var robe = pack?.FindItemByType<NewHavenAdventurersRobe>();

        if (robe == null)
        {
            from.SendMessage("Place your New Haven adventurer's robe in your backpack first.");
            return;
        }

        var cost = robe.UpgradeTier + 1;
        if (pack.ConsumeTotal(typeof(HavenMark), cost) != true)
        {
            from.SendMessage($"The next robe upgrade requires {cost} Haven mark(s).");
            return;
        }

        if (!robe.TryUpgrade(from))
        {
            // This only happens for a bound/maxed robe. Refund the marks.
            pack.DropItem(new HavenMark(cost));
        }
    }
}

[SerializationGenerator(0)]
public partial class SpecialRewardStone : Item
{
    public override string DefaultName => "Haven special rewards";

    [Constructible]
    public SpecialRewardStone() : base(0xED4)
    {
        Movable = false;
        Hue = 0x8A5;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 3))
        {
            from.SendMessage("You are too far away to use the special reward stone.");
            return;
        }

        from.SendMenu(new RewardMenu());
    }

    private sealed class RewardMenu : ItemListMenu
    {
        private const int MarkCost = 15;
        private const int GoldFallbackCost = 25000;

        private static readonly ItemListEntry[] MenuEntries =
        [
            new("Vanguard bracelet", 0x1086, 0x972),
            new("Arcane Focus bracelet", 0x1086, 0x482),
            new("Wind bracelet", 0x1086, 0x47F),
            new("Beastmaster bracelet", 0x1086, 0x59B),
            new("Virtuoso bracelet", 0x1086, 0x489),
            new("Artisan bracelet", 0x1086, 0x96D),
            new("Fortune bracelet", 0x1086, 0x8A5),
            new("Guardian bracelet", 0x1086, 0x497),
            new("Night bracelet", 0x1086, 0x455)
        ];

        public RewardMenu() : base(
            $"Special Bracelets - {MarkCost} Haven marks or {GoldFallbackCost:N0} gold",
            MenuEntries
        )
        {
        }

        public override void OnResponse(NetState state, int index)
        {
            var from = state.Mobile;
            var pack = from?.Backpack;

            if (pack == null || index < 0 || index >= MenuEntries.Length)
            {
                return;
            }

            var paidWithMarks = pack.ConsumeTotal(typeof(HavenMark), MarkCost);
            if (!paidWithMarks && !HavenEconomy.TryPay(from, GoldFallbackCost))
            {
                from.SendMessage(
                    $"You need {MarkCost} Haven marks or {GoldFallbackCost:N0} gold for a bracelet."
                );
                return;
            }

            var reward = SpecialBraceletFactory.Create(index);
            if (reward == null)
            {
                if (paidWithMarks)
                {
                    pack.DropItem(new HavenMark(MarkCost));
                }
                return;
            }

            pack.DropItem(reward);
            from.SendMessage(
                paidWithMarks
                    ? $"You exchange {MarkCost} Haven marks for {reward.DefaultName}."
                    : $"You purchase {reward.DefaultName} for {GoldFallbackCost:N0} gold."
            );
        }
    }
}

public static class HavenEconomy
{
    public static bool TryPay(Mobile from, int amount)
    {
        if (from?.Backpack == null || amount <= 0)
        {
            return false;
        }

        var wallet = from.Backpack.FindItemByType<AdventurersWallet>();
        if (wallet?.Balance >= amount && wallet.TrySpend(amount))
        {
            return true;
        }

        if (from.Backpack.ConsumeTotal(typeof(Gold), amount))
        {
            return true;
        }

        return Banker.Withdraw(from, amount);
    }
}

public static class StarterProvisioner
{
    public static void Provision(Mobile mobile)
    {
        if (mobile?.Backpack == null || mobile.AccessLevel != AccessLevel.Player)
        {
            return;
        }

        var robe = new NewHavenAdventurersRobe();
        robe.BindTo(mobile);

        var grimoire = new ApprenticeGrimoire();
        grimoire.BindTo(mobile);

        var weapon = SelectStarterWeapon(mobile);
        StarterSupplyStone.BindStarterItem(weapon, mobile);

        var house = new SmallBrickHouseDeed
        {
            LootType = LootType.Blessed
        };

        mobile.Backpack.DropItem(robe);
        mobile.Backpack.DropItem(grimoire);
        mobile.Backpack.DropItem(weapon);
        mobile.Backpack.DropItem(new AdventurersWallet());
        mobile.Backpack.DropItem(new CleanupTrashBag());
        mobile.Backpack.DropItem(house);
        mobile.Backpack.DropItem(new Bandage(50));
    }

    private static Item SelectStarterWeapon(Mobile mobile)
    {
        var archery = mobile.Skills[SkillName.Archery].Value;
        var fencing = mobile.Skills[SkillName.Fencing].Value;
        var macing = mobile.Skills[SkillName.Macing].Value;
        var swords = mobile.Skills[SkillName.Swords].Value;

        if (archery >= fencing && archery >= macing && archery >= swords && archery > 0)
        {
            return new ApprenticeBow();
        }

        if (fencing >= macing && fencing >= swords && fencing > 0)
        {
            return new ApprenticeFencer();
        }

        if (macing >= swords && macing > 0)
        {
            return new ApprenticeMace();
        }

        return new ApprenticeBlade();
    }
}

public static class HavenContentBootstrap
{
    private static readonly Point2D NewHavenSupply = new(3481, 2582);
    private static readonly Point2D NewHavenUpgrade = new(3483, 2582);
    private static readonly Point2D NewHavenRewards = new(3485, 2582);
    private static readonly Point2D NewHavenHitchingPost = new(3487, 2582);
    private static readonly Point2D OldHavenBoss = new(3670, 2587);
    private static readonly Point2D OldHavenSteed = new(3690, 2525);

    private static readonly Point2D BritainSupply = new(1428, 1697);
    private static readonly Point2D BritainUpgrade = new(1430, 1697);
    private static readonly Point2D BritainRewards = new(1432, 1697);
    private static readonly Point2D BritainHitchingPost = new(1434, 1697);
    private static readonly Point2D FeluccaSteed = new(1388, 1498);

    public static void Initialize() =>
        Timer.DelayCall(TimeSpan.FromSeconds(15), EnsureContent);

    private static void EnsureContent()
    {
        // Intended ML-era hub.
        EnsureItem<StarterSupplyStone>(Map.Trammel, NewHavenSupply);
        EnsureItem<HavenUpgradeStone>(Map.Trammel, NewHavenUpgrade);
        EnsureItem<SpecialRewardStone>(Map.Trammel, NewHavenRewards);
        EnsureItem<FreePetHitchingPost>(Map.Trammel, NewHavenHitchingPost);
        EnsureSpawner<OldHavenBossSpawner>(Map.Trammel, OldHavenBoss);
        EnsureSpawner<VampiricSteedSpawner>(Map.Trammel, OldHavenSteed);

        // The current installer is Felucca-only. These fallbacks guarantee
        // every custom reward remains obtainable on a completely fresh install.
        EnsureItem<StarterSupplyStone>(Map.Felucca, BritainSupply);
        EnsureItem<HavenUpgradeStone>(Map.Felucca, BritainUpgrade);
        EnsureItem<SpecialRewardStone>(Map.Felucca, BritainRewards);
        EnsureItem<FreePetHitchingPost>(Map.Felucca, BritainHitchingPost);
        EnsureSpawner<VampiricSteedSpawner>(Map.Felucca, FeluccaSteed);
    }

    private static Point3D AtSurface(Map map, Point2D p) =>
        new(p.X, p.Y, map.GetAverageZ(p.X, p.Y));

    private static void EnsureItem<T>(Map map, Point2D p) where T : Item, new()
    {
        var loc = AtSurface(map, p);
        foreach (var existing in map.GetItemsInRange<T>(loc, 2))
        {
            if (!existing.Deleted)
            {
                return;
            }
        }

        var item = new T();
        item.MoveToWorld(loc, map);
    }

    private static void EnsureSpawner<T>(Map map, Point2D p) where T : Spawner, new()
    {
        var loc = AtSurface(map, p);
        foreach (var existing in map.GetItemsInRange<T>(loc, 2))
        {
            if (!existing.Deleted)
            {
                return;
            }
        }

        var spawner = new T();
        spawner.MoveToWorld(loc, map);
        spawner.Respawn();
    }
}
