using System;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

internal static class HavenAbyssDrops
{
    internal static Type[] Ingredients(BaseCreature creature) => creature switch
    {
        FireDaemon or FireDaemonRenowned or Daemon or PitFiend or DevourerRenowned => new[] { typeof(DaemonClaw) },
        LavaSnake or LavaElemental or FireElementalRenowned => new[] { typeof(LavaSerpentCrust) },
        FairyDragon => new[] { typeof(FaeryDust), typeof(FeyWings) },
        Pixie or Wisp or DarkWisp or PixieRenowned => new[] { typeof(FaeryDust) },
        AcidSlug or AcidElemental or AcidElementalRenowned or CorrosiveSlime => new[] { typeof(VialOfVitriol), typeof(VoidOrb) },
        EnslavedGrayGoblin or EnslavedGreenGoblin or EnslavedGoblinKeeper or EnslavedGoblinScout or EnslavedGoblinMage or EnslavedGreenGoblinAlchemist or
            GrayGoblinMageRenowned or GreenGoblinAlchemistRenowned => new[] { typeof(GoblinBlood) },
        ClanSSW => new[] { typeof(ReflectiveWolfEye) },
        ClanCA or ClanCT or ClockworkScorpion or RakktaviRenowned => new[] { typeof(CrystallineBlackrock), typeof(ArcanicRuneStone) },
        Skeleton or SkeletalKnight or BoneKnight or BoneMagi or SkeletalMage or SkeletalLich or SkeletalDragonRenowned or AncientLichRenowned or
            Wraith or WailingBanshee or RottingCorpse or InterredGrizzle => new[] { typeof(UndyingFlesh) },
        _ => Array.Empty<Type>()
    };
    // Expedition caches provide the otherwise missing SA material sources on this ML shard.
    internal static Type[] CacheMaterials(int site) => site switch
    {
        0 or 9 or 12 => new[] { typeof(LavaSerpentCrust), typeof(DaemonClaw), typeof(DelicateScales) },
        1 => new[] { typeof(FaeryDust), typeof(FeyWings) },
        2 => new[] { typeof(DaemonClaw), typeof(VoidOrb) },
        3 or 5 => new[] { typeof(CrystallineBlackrock), typeof(ArcanicRuneStone) },
        4 => new[] { typeof(ReflectiveWolfEye), typeof(GoblinBlood) },
        6 => new[] { typeof(VialOfVitriol), typeof(BottleIchor) },
        7 or 11 => new[] { typeof(UndyingFlesh), typeof(SilverSnakeSkin) },
        8 => new[] { typeof(SeedOfRenewal), typeof(FeyWings) },
        10 => new[] { typeof(GoblinBlood), typeof(SpiderCarapace) },
        _ => Array.Empty<Type>()
    };
    internal static void Fill(BaseCreature creature, Type essence, bool renowned, double roll = -1)
    {
        if (creature.Corpse is not { Deleted: false } corpse) { return; }
        if (renowned || (roll < 0 ? Utility.RandomDouble() : roll) < .10)
        { var item = (Item)Activator.CreateInstance(essence); item.Amount = renowned ? 3 : 1; corpse.DropItem(item); }
        foreach (var ingredient in Ingredients(creature))
        {
            if (renowned || (roll < 0 ? Utility.RandomDouble() : roll) < .25)
            { corpse.DropItem((Item)Activator.CreateInstance(ingredient)); }
        }
    }
    internal static void AwardClear(Mobile player, HavenAbyssSite site)
    {
        if (player.Backpack == null) { player.AddItem(new Backpack()); }
        var essence = (Item)Activator.CreateInstance(site.Essence); essence.Amount = 5; player.Backpack.DropItem(essence);
        var gem = Utility.Random(8) switch
        {
            0 => (Item)new BlueDiamond(), 1 => new BrilliantAmber(), 2 => new DarkSapphire(), 3 => new EcruCitrine(),
            4 => new FireRuby(), 5 => new PerfectEmerald(), 6 => new Turquoise(), _ => new WhitePearl()
        };
        player.Backpack.DropItem(gem); player.Backpack.DropItem(new Gold(Utility.RandomMinMax(3000, 5000)));
        foreach (var type in CacheMaterials(Array.IndexOf(HavenAbyssCatalog.Sites, site)))
        {
            var material = (Item)Activator.CreateInstance(type); material.Amount = 2;
            player.Backpack.DropItem(material); HavenMarketProduction.Consign(player, material);
        }
        player.SendMessage($"Completed {site.Name}: five essences, local crafting materials, a rare gem and gold are in your pack.");
        HavenMarketProduction.Consign(player, essence); HavenMarketProduction.Consign(player, gem);
    }
}
