using System;
using Server.Items;
using Server.Mobiles;
namespace Server.UOOffline;

internal sealed record HavenAbyssWave(Type[] Creatures, int[] Required);
internal sealed record HavenAbyssSite(string Name, Point3D Center, Type Essence, HavenAbyssWave[] Waves);
internal static class HavenAbyssCatalog
{
    internal static readonly HavenAbyssSite[] Sites =
    {
        new("Crimson Veins", new(974, 161, -10), typeof(EssencePrecision), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(FireAnt), typeof(LavaSnake), typeof(LavaLizard) }, new[] { 20, 10, 10 }),
            new(new Type[] { typeof(Efreet), typeof(FireGargoyle) }, new[] { 5, 5 }),
            new(new Type[] { typeof(LavaElemental), typeof(FireDaemon) }, new[] { 10, 5 }),
            new(new Type[] { typeof(FireElementalRenowned) }, new[] { 1 }),
        }),
        new("Fairy Dragon Lair", new(887, 273, 4), typeof(EssenceDiligence), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(FairyDragon) }, new[] { 25 }),
            new(new Type[] { typeof(Wyvern) }, new[] { 10 }),
            new(new Type[] { typeof(ForgottenServant) }, new[] { 10 }),
            new(new Type[] { typeof(WyvernRenowned) }, new[] { 1 }),
        }),
        new("Abyssal Lair", new(987, 328, 11), typeof(EssenceAchievement), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(GreaterMongbat), typeof(Imp) }, new[] { 20, 20 }),
            new(new Type[] { typeof(Daemon) }, new[] { 10 }),
            new(new Type[] { typeof(PitFiend) }, new[] { 5 }),
            new(new Type[] { typeof(DevourerRenowned) }, new[] { 1 }),
        }),
        new("Clan Ribbon", new(915, 501, -11), typeof(EssenceBalance), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(ClanRibbonPlagueRat), typeof(ClanRS) }, new[] { 10, 10 }),
            new(new Type[] { typeof(ClanRibbonPlagueRat), typeof(ClanRC) }, new[] { 10, 10 }),
            new(new Type[] { typeof(VitaviRenowned) }, new[] { 1 }),
        }),
        new("Clan Scratch", new(950, 552, -13), typeof(EssenceBalance), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(ClanSSW), typeof(ClanSS) }, new[] { 10, 10 }),
            new(new Type[] { typeof(ClanSSW), typeof(ClanSH) }, new[] { 10, 10 }),
            new(new Type[] { typeof(TikitaviRenowned) }, new[] { 1 }),
        }),
        new("Clan Chitter", new(980, 491, -11), typeof(EssenceBalance), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(ClockworkScorpion), typeof(ClanCA) }, new[] { 10, 10 }),
            new(new Type[] { typeof(ClockworkScorpion), typeof(ClanCT) }, new[] { 10, 10 }),
            new(new Type[] { typeof(RakktaviRenowned) }, new[] { 1 }),
        }),
        new("Passage of Tears", new(684, 579, -14), typeof(EssenceSingularity), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(AcidSlug), typeof(CorrosiveSlime) }, new[] { 10, 20 }),
            new(new Type[] { typeof(AcidElemental) }, new[] { 10 }),
            new(new Type[] { typeof(InterredGrizzle) }, new[] { 3 }),
            new(new Type[] { typeof(AcidElementalRenowned) }, new[] { 1 }),
        }),
        new("Lands of the Lich", new(530, 658, 9), typeof(EssenceDirection), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(Wraith), typeof(Spectre), typeof(Shade), typeof(Skeleton), typeof(Zombie) }, new[] { 5, 10, 5, 30, 20 }),
            new(new Type[] { typeof(BoneMagi), typeof(SkeletalMage), typeof(BoneKnight), typeof(SkeletalKnight), typeof(WailingBanshee) }, new[] { 5, 10, 10, 10, 10 }),
            new(new Type[] { typeof(SkeletalLich), typeof(RottingCorpse) }, new[] { 5, 20 }),
            new(new Type[] { typeof(AncientLichRenowned) }, new[] { 1 }),
        }),
        new("Secret Garden", new(434, 701, 29), typeof(EssenceFeeling), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(Pixie) }, new[] { 20 }),
            new(new Type[] { typeof(Wisp) }, new[] { 15 }),
            new(new Type[] { typeof(DarkWisp) }, new[] { 10 }),
            new(new Type[] { typeof(PixieRenowned) }, new[] { 1 }),
        }),
        new("Fire Temple Ruins", new(546, 760, -91), typeof(EssenceOrder), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(LavaSnake), typeof(LavaLizard), typeof(FireAnt) }, new[] { 20, 10, 10 }),
            new(new Type[] { typeof(LavaSerpent), typeof(HellCat), typeof(HellHound) }, new[] { 10, 10, 10 }),
            new(new Type[] { typeof(FireDaemon), typeof(LavaElemental) }, new[] { 5, 10 }),
            new(new Type[] { typeof(FireDaemonRenowned) }, new[] { 1 }),
        }),
        new("Enslaved Goblins", new(578, 799, -45), typeof(EssenceControl), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(EnslavedGrayGoblin), typeof(EnslavedGreenGoblin) }, new[] { 10, 15 }),
            new(new Type[] { typeof(EnslavedGoblinScout), typeof(EnslavedGoblinKeeper) }, new[] { 10, 10 }),
            new(new Type[] { typeof(EnslavedGoblinMage), typeof(EnslavedGreenGoblinAlchemist) }, new[] { 5, 5 }),
            new(new Type[] { typeof(GrayGoblinMageRenowned), typeof(GreenGoblinAlchemistRenowned) }, new[] { 1, 1 }),
        }),
        new("Skeletal Dragon", new(677, 824, -108), typeof(EssencePersistence), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(PatchworkSkeleton), typeof(Skeleton) }, new[] { 5, 15 }),
            new(new Type[] { typeof(BoneKnight), typeof(SkeletalKnight) }, new[] { 5, 5 }),
            new(new Type[] { typeof(BoneMagi), typeof(SkeletalMage) }, new[] { 5, 2 }),
            new(new Type[] { typeof(SkeletalLich) }, new[] { 2 }),
            new(new Type[] { typeof(SkeletalDragonRenowned) }, new[] { 1 }),
        }),
        new("Lava Caldera", new(578, 900, -72), typeof(EssencePassion), new HavenAbyssWave[]
        {
            new(new Type[] { typeof(LavaSnake), typeof(LavaLizard), typeof(FireAnt) }, new[] { 10, 10, 20 }),
            new(new Type[] { typeof(LavaSerpent), typeof(HellCat), typeof(HellHound) }, new[] { 10, 10, 10 }),
            new(new Type[] { typeof(FireDaemon), typeof(LavaElemental) }, new[] { 5, 10 }),
            new(new Type[] { typeof(FireDaemonRenowned) }, new[] { 1 }),
        }),
    };
}
