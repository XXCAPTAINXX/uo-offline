using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.Spawners;
using Server.Gumps;
using Server.Items;
using Server.Menus.ItemLists;
using Server.Mobiles;
using Server.Multis;
using Server.Multis.Deeds;
using Server.Network;
using Server.Logging;

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

        from.CloseGump<HavenListGump>();
        from.SendGump(new HavenListGump(this, new StarterSupplyMenu()));
    }

    internal sealed class StarterSupplyMenu : ItemListMenu, IHavenShop
    {
        private static readonly ItemListEntry[] MenuEntries =
        [
            new("Cleanup trash bag - free", 0xE76, 0x455),
            new("Adventurer's wallet - free", 0xE79),
            new("New Haven starter robe - free", 0x1F03, 0x59B),
            new("Full apprentice grimoire - free", 0xEFA, 0x482),
            new("Apprentice blade - free", 0xF61),
            new("Apprentice fencer - free", 0x1401),
            new("Apprentice mace - free", 0x1407),
            new("Apprentice bow - free", 0x13B2),
            new("Champion progression archive - free", 0x2259, 0x489),
            new("Peerless key vault - free", 0x9A8, 0x497),
            new("Starter Fortune Earrings - free", 0x1087, 0x501),
            new("Blessed Travel Book - free", 0x22C5),
            new("Leveling cape - free", 0x1515, 0x59B),
            new("Evolving sash - free", 0x1541, 0x59B)
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

            if (index == 12) { HavenLevelingCape.Claim(from); return; }
            if (index == 13) { HavenStarterSash.Claim(from); return; }
            var item = CreateItem(index);
            if (item == null)
            {
                return;
            }
            if (!from.Backpack.CheckHold(from, item, false)) { item.Delete(); from.SendMessage("Make room in your backpack."); return; }
            BindStarterItem(item, from);
            from.Backpack.DropItem(item);
            from.SendMessage("Your free starter item is in your backpack.");
        }

        public Item CreateItem(int index) => index switch
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
                10 => new StarterFortuneEarrings(),
                11 => new OfflineTravelBook(),
                12 => new HavenLevelingCape(),
                13 => new HavenStarterSash(),
                _ => null
            };
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
        if (from.Map != Map || !from.InRange(GetWorldLocation(), 3))
        {
            from.SendMessage("You are too far away to use the upgrade stone.");
            return;
        }
        from.CloseGump<HavenUpgradeGump>();
        from.SendGump(new HavenUpgradeGump(this, from));
    }

    internal void ApplyUpgrade(Mobile from, int expectedTier)
    {
        if (Deleted || from.Map != Map || !from.InRange(GetWorldLocation(), 3))
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

        if (robe.BoundTo != null && robe.BoundTo != from)
        {
            from.SendMessage("That starter robe is bound to another character.");
            return;
        }

        if (robe.UpgradeTier >= robe.MaxUpgradeTier)
        {
            from.SendMessage("That starter robe is already fully upgraded.");
            return;
        }

        if (robe.UpgradeTier != expectedTier)
        {
            from.SendMessage("The upgrade price has changed. Review the updated cost first.");
            OnDoubleClick(from);
            return;
        }

        var markCost = robe.UpgradeTier + 1;
        var goldCost = markCost * 5000;
        var paidWithMarks = HavenEconomy.TryPayMarks(from, markCost);

        if (!paidWithMarks && !HavenEconomy.TryPay(from, goldCost))
        {
            from.SendMessage(
                $"The next robe upgrade requires {markCost} Haven mark(s) or {goldCost:N0} gold."
            );
            return;
        }

        if (!robe.TryUpgrade(from) && paidWithMarks)
        {
            pack.DropItem(new HavenMark(markCost));
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

        from.CloseGump<HavenListGump>();
        from.SendGump(new HavenListGump(this, new RewardMenu()));
    }

    internal sealed class RewardMenu : ItemListMenu, IHavenShop
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
            new("Night bracelet", 0x1086, 0x455),
            new("Champion pendant - 250 Haven marks only", 0x1088, 0x489)
        ];

        public RewardMenu() : base(
            $"Bracelets: {MarkCost} marks / {GoldFallbackCost:N0} gold. Pendant: 250 marks.",
            MenuEntries
        )
        {
        }

        public Item CreateItem(int index) => index == 9 ? new HavenChampionPendant() : SpecialBraceletFactory.Create(index);

        public override void OnResponse(NetState state, int index) => Buy(state.Mobile, index);

        internal static void Buy(Mobile from, int index)
        {
            var pack = from?.Backpack;

            if (pack == null || index < 0 || index >= MenuEntries.Length)
            {
                return;
            }

            var reward = index == 9 ? new HavenChampionPendant() : SpecialBraceletFactory.Create(index);
            if (reward == null) { return; }
            if (!pack.CheckHold(from, reward, false)) { reward.Delete(); from.SendMessage("Make room in your backpack."); return; }
            var markCost = index == 9 ? 250 : MarkCost;
            var paidWithMarks = HavenEconomy.TryPayMarks(from, markCost);
            if (!paidWithMarks && (index == 9 || !HavenEconomy.TryPay(from, GoldFallbackCost)))
            {
                reward.Delete();
                from.SendMessage(index == 9 ? "The champion pendant costs 250 Haven marks; gold cannot buy it." : "You need 15 Haven marks or 25,000 gold for a bracelet.");
                return;
            }
            pack.DropItem(reward);
            from.SendMessage(
                paidWithMarks
                    ? $"You exchange {markCost} Haven marks for {reward.DefaultName}."
                    : $"You purchase {reward.DefaultName} for {GoldFallbackCost:N0} gold."
            );
        }
    }
}

public static class HavenEconomy
{
    public static bool TryPayMarks(Mobile from, int amount)
    {
        if (from?.Backpack == null || amount <= 0) { return false; }
        var wallet = from.Backpack.FindItemByType<AdventurersWallet>();
        var walletMarks = (int)Math.Min(amount, Math.Max(0, wallet?.HavenMarks ?? 0));
        var looseMarks = amount - walletMarks;
        if (from.Backpack.GetAmount(typeof(HavenMark)) < looseMarks) { return false; }
        if (looseMarks > 0 && !from.Backpack.ConsumeTotal(typeof(HavenMark), looseMarks)) { return false; }
        if (walletMarks > 0) { wallet.HavenMarks -= walletMarks; }
        return true;
    }
    public static bool TryPay(Mobile from, int amount)
    {
        if (from?.Backpack == null || amount <= 0)
        {
            return false;
        }

        var wallet = from.Backpack.FindItemByType<AdventurersWallet>();
        var walletGold = (int)Math.Min(amount, Math.Max(0, wallet?.Balance ?? 0));
        var backpackGold = Math.Min(amount - walletGold, from.Backpack.GetAmount(typeof(Gold)));
        var bankGold = amount - walletGold - backpackGold;
        // Check the complete payment before removing any wallet or loose gold.
        // All payment operations run together on the game thread.
        if (bankGold > 0 && !Banker.Withdraw(from, bankGold))
        {
            return false;
        }
        if (backpackGold > 0) { from.Backpack.ConsumeTotal(typeof(Gold), backpackGold); }
        if (walletGold > 0) { wallet.TrySpend(walletGold); }
        return true;
    }
}

public static class StarterProvisioner
{
    public static void Configure() =>
        CommandSystem.Register("StarterKit", AccessLevel.Player, e => StarterBundleClaims.Claim(e.Mobile));

    public static void Provision(Mobile mobile)
    {
        if (mobile?.Backpack == null || !mobile.Player)
        {
            return;
        }

        // Recovery for staff characters skipped by older releases. Never replace
        // existing progression items or repeatedly hand out starter house deeds.
        if (mobile.Backpack.FindItemByType<NewHavenAdventurersRobe>() != null ||
            mobile.FindItemOnLayer(Layer.OuterTorso) is NewHavenAdventurersRobe ||
            mobile.Backpack.FindItemByType<ApprenticeGrimoire>() != null ||
            mobile.FindItemOnLayer(Layer.OneHanded) is ApprenticeGrimoire)
        {
            mobile.SendMessage("You already have starter progression equipment. Use a supply stone for replacements.");
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
        mobile.Backpack.DropItem(new StarterFortuneEarrings());
        mobile.Backpack.DropItem(new HavenStarterSash());
        mobile.Backpack.DropItem(new HavenLevelingCape { BoundTo = mobile });
        if (weapon is ApprenticeBow)
        {
            mobile.Backpack.DropItem(new Arrow(100));
        }
        mobile.SendMessage("Your Haven starter equipment is in your backpack.");
        StarterBundleClaims.MarkClaimed(mobile);
    }

    internal static Item SelectStarterWeapon(Mobile mobile)
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
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(HavenContentBootstrap));
    internal static readonly Point2D HavenWildernessSteed = new(3675, 2410);
    private static readonly Point2D NewHavenSupply = new(3501, 2574);
    private static readonly Point2D NewHavenUpgrade = new(3504, 2571);
    private static readonly Point2D NewHavenRewards = new(3508, 2571);
    private static readonly Point2D NewHavenHitchingPost = new(3513, 2580);
    private static readonly Point2D NewHavenDungeonPortal = new(3499, 2579);
    private static readonly Point2D OldHavenBoss = new(3670, 2587);
    private static readonly Point2D OldHavenSteed = new(3690, 2525);

    private static readonly Point2D BritainSupply = new(1428, 1697);
    private static readonly Point2D BritainUpgrade = new(1430, 1697);
    private static readonly Point2D BritainRewards = new(1432, 1697);
    private static readonly Point2D BritainHitchingPost = new(1434, 1697);
    private static readonly Point2D BritainDungeonPortal = new(1436, 1697);
    private static readonly Point2D FeluccaSteed = new(1388, 1498);

    public static void Initialize() =>
        Timer.DelayCall(TimeSpan.FromSeconds(15), EnsureContent);

    public static void EnsureContent()
    {
        HavenRecovery.EnsureServices();
        RemoveBrokenNewHavenDungeonPortal();

        // Intended ML-era hub.
        EnsureHavenPlaza();
        EnsureSpawner<OldHavenBossSpawner>(Map.Trammel, OldHavenBoss);
        RelocateOldHavenSteeds();
        EnsureSpawner<VampiricSteedSpawner>(Map.Trammel, HavenWildernessSteed);
        foreach (var bossSpawner in Map.Trammel.GetItemsInRange<OldHavenBossSpawner>(AtSurface(Map.Trammel, OldHavenBoss), 2))
        {
            logger.Information("Old Haven Warden spawner at {Location}: running={Running}, spawned={Count}, next={Next}", bossSpawner.Location, bossSpawner.Running, bossSpawner.Spawned.Count, bossSpawner.NextSpawn);
        }

        // Also retain the established Felucca bank services.
        EnsureItem<StarterSupplyStone>(Map.Felucca, BritainSupply);
        EnsureItem<HavenUpgradeStone>(Map.Felucca, BritainUpgrade);
        EnsureItem<SpecialRewardStone>(Map.Felucca, BritainRewards);
        EnsureItem<FreePetHitchingPost>(Map.Felucca, BritainHitchingPost);
        EnsureItem<UOOfflineDungeonPortal>(Map.Felucca, BritainDungeonPortal);
        EnsureSpawner<VampiricSteedSpawner>(Map.Felucca, FeluccaSteed);
    }

    internal static void RelocateOldHavenSteeds()
    {
        var map = Map.Trammel;
        var preferred = AtSurface(map, HavenWildernessSteed);
        if (!HavenRecovery.FindLocation(preferred, out var destination, 8))
        {
            throw new InvalidOperationException("No walkable location for the Haven wilderness steeds.");
        }
        var oldLocation = AtSurface(map, OldHavenSteed);
        var spawners = new List<VampiricSteedSpawner>();
        var steeds = new HashSet<VampiricSteed>();
        foreach (var spawner in map.GetItemsInRange<VampiricSteedSpawner>(oldLocation, 2))
        {
            spawners.Add(spawner);
            foreach (var spawn in spawner.Spawned.Keys)
            {
                if (spawn is VampiricSteed steed && !steed.Controlled && !steed.Summoned) { steeds.Add(steed); }
            }
        }
        foreach (var steed in map.GetMobilesInRange<VampiricSteed>(oldLocation, 100))
        {
            if (!steed.Controlled && !steed.Summoned) { steeds.Add(steed); }
        }
        foreach (var spawner in spawners) { spawner.MoveToWorld(preferred, map); }
        foreach (var steed in steeds)
        {
            steed.Combatant = null;
            steed.Home = destination;
            steed.MoveToWorld(destination, map);
        }
        logger.Information("Haven steeds relocated: {Spawners} spawners, {Steeds} wild steeds; destination {Destination}", spawners.Count, steeds.Count, destination);
    }
    internal static void EnsureHavenPlaza()
    {
        ArrangeHavenItem<StarterSupplyStone>(NewHavenSupply);
        ArrangeHavenItem<HavenUpgradeStone>(NewHavenUpgrade);
        ArrangeHavenItem<SpecialRewardStone>(NewHavenRewards);
        ArrangeHavenItem<ArcaneSupplyStone>(new Point2D(3504, 2583));
        ArrangeHavenItem<HavenTrainingStone>(new Point2D(3508, 2583));
        ArrangeHavenItem<FreePetHitchingPost>(NewHavenHitchingPost);
        ArrangeHavenItem<UOOfflineDungeonPortal>(NewHavenDungeonPortal);
        ArrangeHavenItem<HavenRepairBench>(new Point2D(3502, 2581));
        var oldDecorations = new List<Item>();
        foreach (var item in Map.Trammel.GetItemsInRange<Item>(new Point3D(3490, 2582, 20), 30))
        {
            if (item.Name == "Haven welcome garden") { oldDecorations.Add(item); }
        }
        foreach (var item in oldDecorations) { item.Delete(); }
        EnsureHavenDecoration(new Point3D(3501, 2582, 14), false);
        EnsureHavenDecoration(new Point3D(3509, 2570, 14), false);
    }

    public static void EnsureBankServices(Map map, Point3D bank)
    {
        if (map == Map.Trammel && Utility.InRange(bank, new Point3D(3490, 2582, 20), 20))
        {
            EnsureHavenPlaza();
            return;
        }
        EnsureBankItem<StarterSupplyStone>(map, bank, -4, 3);
        EnsureBankItem<HavenUpgradeStone>(map, bank, -2, 3);
        EnsureBankItem<SpecialRewardStone>(map, bank, 0, 3);
        EnsureBankItem<FreePetHitchingPost>(map, bank, 2, 3);
        EnsureBankItem<UOOfflineDungeonPortal>(map, bank, 4, 3);
        EnsureBankItem<HavenRepairBench>(map, bank, -2, 5);
        EnsureBankItem<ArcaneSupplyStone>(map, bank, 2, 5);
        EnsureBankItem<HavenTrainingStone>(map, bank, 4, 5);
    }

    private static void ArrangeHavenItem<T>(Point2D position) where T : Item, new()
    {
        var preferred = new Point3D(position.X, position.Y, 14);
        var found = new List<T>();
        foreach (var item in Map.Trammel.GetItemsInRange<T>(new Point3D(3495, 2580, 20), 30))
        {
            if (!item.Deleted && item.GetType() == typeof(T)) { found.Add(item); }
        }
        if (found.Count > 0 && found[0].X == position.X && found[0].Y == position.Y) { return; }
        if (!HavenRecovery.FindLocation(preferred, out var location, 1)) { return; }
        var service = found.Count == 0 ? new T() : found[0];
        service.Movable = false;
        service.MoveToWorld(location, Map.Trammel);
    }

    private static void EnsureHavenDecoration(Point3D preferred, bool lamp)
    {
        foreach (var item in Map.Trammel.GetItemsInRange<Item>(preferred, 1))
        {
            if (item.Name == "Haven square flowers") { return; }
        }
        if (!HavenRecovery.FindLocation(preferred, out var location, 1)) { return; }
        Item decoration = lamp ? new LampPost1 { Burning = true } : new PottedPlant1();
        decoration.Name = "Haven square flowers";
        decoration.Movable = false;
        decoration.MoveToWorld(location, Map.Trammel);
    }

    private static void EnsureBankItem<T>(Map map, Point3D bank, int dx, int dy) where T : Item, new()
    {
        foreach (var existing in map.GetItemsInRange<T>(bank, 18))
        {
            if (!existing.Deleted)
            {
                return;
            }
        }
        var preferred = new Point3D(bank.X + dx, bank.Y + dy, bank.Z);
        for (var radius = 0; radius <= 8; radius++)
        {
            for (var x = -radius; x <= radius; x++)
            {
                for (var y = -radius; y <= radius; y++)
                {
                    if (Math.Max(Math.Abs(x), Math.Abs(y)) != radius)
                    {
                        continue;
                    }
                    var px = preferred.X + x;
                    var py = preferred.Y + y;
                    var z = preferred.Z;
                    if (!map.CanSpawnMobile(px, py, z))
                    {
                        z = map.GetAverageZ(px, py);
                        if (!map.CanSpawnMobile(px, py, z))
                        {
                            continue;
                        }
                    }
                    var item = new T { Movable = false };
                    item.MoveToWorld(new Point3D(px, py, z), map);
                    return;
                }
            }
        }
        throw new InvalidOperationException($"No walkable location for {typeof(T).Name} near {map} bank {bank}.");
    }

    private static void RemoveBrokenNewHavenDungeonPortal()
    {
        // The existing portal reported inside New Haven bank is inert. Remove
        // only explicitly named/typed dungeon portals from the bank interior;
        // ordinary moongates and teleporters are left alone.
        var bounds = new Rectangle2D(3479, 2565, 14, 16);
        var remove = new List<Item>();

        foreach (var item in Map.Trammel.GetItemsInBounds(bounds))
        {
            if (item.Deleted || item is UOOfflineDungeonPortal)
            {
                continue;
            }

            var typeName = item.GetType().Name;
            var displayName = item.Name ?? string.Empty;

            if (typeName.Contains("DungeonPortal", StringComparison.OrdinalIgnoreCase) ||
                displayName.Equals("Dungeon Portal", StringComparison.OrdinalIgnoreCase))
            {
                remove.Add(item);
            }
        }

        foreach (var item in remove)
        {
            item.Delete();
        }

        if (remove.Count > 0)
        {
            Console.WriteLine($"[uo-offline] removed {remove.Count} inert New Haven dungeon portal item(s)");
        }
    }

    private static Point3D AtSurface(Map map, Point2D p) =>
        new(p.X, p.Y, map.GetAverageZ(p.X, p.Y));

    private static void EnsureItem<T>(Map map, Point2D p) where T : Item, new()
    {
        var loc = AtSurface(map, p);
        foreach (var existing in map.GetItemsInRange<T>(loc, 2))
        {
            if (!existing.Deleted && existing.GetType() == typeof(T))
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
            if (!existing.Deleted && existing.GetType() == typeof(T))
            {
                return;
            }
        }

        var spawner = new T();
        spawner.MoveToWorld(loc, map);
        spawner.Respawn();
    }
}
