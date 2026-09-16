using System;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Server.UOOffline;
using Xunit;
namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsAbyssRestoration
{
    public HavenWorldTestsAbyssRestoration() { _ = new HavenWorldTests(); }
    [SkippableFact]
    public void RealRenownedDeathPaysParticipantsAndFillsItsCorpseOnce()
    {
        TileDataRequirement.SkipIfMissing();
        var previous=Mobile.CreateCorpseHandler; Mobile.CreateCorpseHandler=Corpse.Mobile_CreateCorpseHandler;
        var player=new PlayerMobile { Player=true,Body=0x190,RawStr=100,RawDex=100,RawInt=100 };player.AddItem(new Backpack());
        var helper=new PlayerMobile { Player=true,Body=0x190,RawStr=100,RawDex=100,RawInt=100 };helper.AddItem(new Backpack());
        var controller=new HavenAbyssMiniChamp();BaseCreature boss=null;
        try
        {
            Assert.True(HavenAbyssMiniChamp.TryPoint(HavenAbyssCatalog.Sites[0].Center,Map.TerMur,20,out var point));
            controller.Setup(0,point);player.MoveToWorld(point,Map.TerMur);helper.MoveToWorld(point,Map.TerMur);
            controller.Active=true;controller.Wave=3;controller.Kills.Add(0);controller.Spawn();boss=Assert.Single(controller.Creatures);
            boss.DamageEntries.Add(new DamageEntry(player) { DamageGiven=boss.HitsMax,LastDamage=Core.Now });
            boss.DamageEntries.Add(new DamageEntry(helper) { DamageGiven=boss.HitsMax,LastDamage=Core.Now });
            boss.Kill();Assert.Equal(1,controller.Completions);Assert.Equal(5,player.Backpack.GetAmount(typeof(EssencePrecision)));
            Assert.Equal(5,helper.Backpack.GetAmount(typeof(EssencePrecision)));Assert.NotNull(boss.Corpse);
            Assert.Equal(3,boss.Corpse.GetAmount(typeof(EssencePrecision)));
            HavenAbyssMiniChamp.OnCreatureDeath(boss);Assert.Equal(1,controller.Completions);Assert.Equal(5,player.Backpack.GetAmount(typeof(EssencePrecision)));
        }
        finally { boss?.Corpse?.Delete();boss?.Delete();controller.Delete();player.Delete();helper.Delete();Mobile.CreateCorpseHandler=previous; }
    }
    [SkippableFact]
    public void ExpeditionIsIdempotentHasWorkingRoutesAndWalkableBridge()
    {
        TileDataRequirement.SkipIfMissing();
        var expedition = HavenAbyssExpedition.Install(); Assert.NotNull(expedition);
        var p = new PlayerMobile { Player = true, Body = 0x190, RawStr = 100, RawDex = 100, RawInt = 100 };
        p.AddItem(new Backpack()); p.MoveToWorld(HavenRecovery.BankLocation,Map.Trammel);
        try
        {
            Assert.Same(expedition, HavenAbyssExpedition.Install()); Assert.Equal(13, expedition.Sites.Count);
            for(var i=0;i<13;i++) { Assert.True(expedition.Travel(p,i),$"Route {i}"); Assert.Equal(Map.TerMur,p.Map); }
            p.MoveToWorld(new Point3D(526,759,-92),Map.TerMur); p.Direction=Direction.South;
            for(var y=760;y<=773;y++)
            {
                var blocks=new List<string>();
                foreach(var tile in Map.TerMur.Tiles.GetStaticTiles(526,y))
                { blocks.Add($"{tile.ID:X} z{tile.Z} {TileData.ItemTable[tile.ID].Name} {TileData.ItemTable[tile.ID].Flags}"); }
                Assert.True(p.Move(Direction.South),$"Bridge blocked entering y={y} from {p.Location}: {string.Join(";",blocks)}");
                Assert.Equal(y,p.Y);
            }
        }
        finally { p.Delete(); expedition.Delete(); }
        Assert.Empty(HavenAbyssExpedition.Registry);
    }
    [SkippableFact]
    public void ArtificeConsumesOnlyOnSuccessAndEveryMaterialHasASourceAndUse()
    {
        TileDataRequirement.SkipIfMissing(); var p=new PlayerMobile { Player=true, Body=0x190, RawStr=100, RawDex=100, RawInt=100 }; p.AddItem(new Backpack());
        p.MoveToWorld(new Point3D(527,758,-92),Map.TerMur); var forge=new HavenAbyssArtifice(); forge.MoveToWorld(new Point3D(528,757,-92),Map.TerMur);
        var sword=new Longsword(); p.Backpack.DropItem(sword); p.Backpack.DropItem(new EssencePrecision(8)); p.Backpack.DropItem(new LavaSerpentCrust(2)); p.Backpack.DropItem(new DaemonClaw(2));
        try
        {
            Assert.True(forge.CanUse(p)); Assert.False(forge.Apply(p,sword,0)); Assert.Equal(8,p.Backpack.GetAmount(typeof(EssencePrecision)));
            p.Skills.Blacksmith.Base=80; Assert.True(forge.Apply(p,sword,0)); Assert.Equal(5,sword.Attributes.WeaponDamage);
            Assert.Equal(0,p.Backpack.GetAmount(typeof(EssencePrecision))); Assert.True(HavenAbyssArtifice.Attuned(sword));
            Assert.False(forge.Apply(p,sword,0)); Assert.False(HavenGearExperience.IsSpecial(sword));
            var sources=new HashSet<Type>(); var uses=new HashSet<Type>();
            for(var i=0;i<13;i++) { sources.Add(HavenAbyssCatalog.Sites[i].Essence); foreach(var type in HavenAbyssDrops.CacheMaterials(i)) { sources.Add(type); } }
            foreach(var recipe in HavenAbyssArtifice.Recipes) { uses.Add(recipe.Essence); uses.Add(recipe.First); uses.Add(recipe.Second); }
            foreach(var entry in HavenResourceCatalog.Entries)
            {
                if(entry.Group!="Abyss") { continue; } var material=entry.Create(1);
                Assert.Contains(material.GetType(),sources); Assert.Contains(material.GetType(),uses); material.Delete();
            }
        }
        finally { forge.Delete(); p.Delete(); }
    }
    [SkippableFact]
    public void EveryWaveCanFinishAndCompletionCreditsParticipantsOnce()
    {
        TileDataRequirement.SkipIfMissing();
        var player=new PlayerMobile { Player=true, Body=0x190, RawStr=100, RawDex=100, RawInt=100 }; player.AddItem(new Backpack());
        try
        {
            for(var site=0;site<HavenAbyssCatalog.Sites.Length;site++)
            {
                var def=HavenAbyssCatalog.Sites[site]; Assert.True(HavenAbyssMiniChamp.TryPoint(def.Center,Map.TerMur,20,out var point));
                var controller=new HavenAbyssMiniChamp(); controller.Setup(site,point); player.MoveToWorld(point,Map.TerMur);
                try
                {
                    Assert.True(controller.Start()); var kills=0;
                    while(controller.Active && kills<1000)
                    {
                        controller.Spawn(); Assert.NotEmpty(controller.Creatures);
                        foreach(var mob in controller.Creatures.ToArray())
                        {
                            mob.DamageEntries.Add(new DamageEntry(player) { DamageGiven=mob.HitsMax, LastDamage=Core.Now });
                            controller.Defeated(mob); var completed=controller.Completions;
                            controller.Defeated(mob); Assert.Equal(completed,controller.Completions); mob.Delete(); kills++;
                        }
                    }
                    Assert.False(controller.Active); Assert.Equal(1,controller.Completions); Assert.Empty(controller.Creatures);
                    Assert.Equal(5,player.Backpack.GetAmount(def.Essence));
                    player.Backpack.ConsumeTotal(def.Essence,5); Assert.False(controller.Start());
                }
                finally { controller.Delete(); }
            }
        }
        finally { player.Delete(); }
        Assert.Empty(HavenAbyssMiniChamp.Owners);
    }
    [SkippableFact]
    public void EveryMiniChampionHasRealCreatureTypesAndAUsableFloor()
    {
        TileDataRequirement.SkipIfMissing(); Assert.Equal(13, HavenAbyssCatalog.Sites.Length);
        var points = new List<string>();
        for (var i = 0; i < HavenAbyssCatalog.Sites.Length; i++)
        {
            var site = HavenAbyssCatalog.Sites[i];
            Assert.True(HavenAbyssMiniChamp.TryPoint(site.Center, Map.TerMur, 20, out var point), $"No floor: {site.Name} at {site.Center}");
            points.Add($"{site.Name}: {point}");
            var controller = new HavenAbyssMiniChamp();
            try
            {
                controller.Setup(i, point); Assert.True(controller.Start(), $"No first wave at {site.Name}");
                Assert.NotEmpty(controller.Creatures);
                foreach(var wave in site.Waves)
                {
                    Assert.Equal(wave.Creatures.Length, wave.Required.Length);
                    foreach(var type in wave.Creatures)
                    {
                        var mob = (BaseCreature)Activator.CreateInstance(type);
                        try { Assert.True(mob.HitsMax > 0, type.Name); Assert.True(mob.DamageMax > 0, type.Name); } finally { mob.Delete(); }
                    }
                }
            }
            finally { controller.Delete(); }
        }
        System.IO.File.WriteAllLines(@"E:\(Offline UO)\uo-offline-haven-rc4\artifacts\abyss-verified-sites.txt",points);
    }
    [SkippableFact]
    public void MaterialDeedsAndWeightReductionRetainTypeAndAmount()
    {
        TileDataRequirement.SkipIfMissing(); var pack = new Backpack(); var bag = new HavenResourceSatchel(); pack.DropItem(bag);
        try
        {
            foreach(var entry in HavenResourceCatalog.Entries)
            {
                if(entry.Group!="Abyss") { continue; }
                var material=entry.Create(125); Assert.True(HavenResourceCatalog.Index(material)>=0,entry.Name);
                Assert.True(HavenResourceSatchel.Accepts(material),entry.Name);
                var deed=new CommodityDeed(); Assert.True(deed.SetCommodity(material)); Assert.Same(material,deed.Commodity);
                Assert.Equal(125,deed.Commodity.Amount); deed.Delete();
            }
            var essence=new EssencePassion(1000); var before=pack.TotalWeight; bag.DropItem(essence);
            Assert.Equal(before+(essence.PileWeight+9)/10,pack.TotalWeight);
        }
        finally { pack.Delete(); }
    }
}
