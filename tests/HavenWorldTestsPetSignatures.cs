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
public class HavenWorldTestsPetSignatures
{
    public HavenWorldTestsPetSignatures() { _=new HavenWorldTests(); }
    private static PlayerMobile Owner()
    {
        var owner=new PlayerMobile { Player=true,Body=0x190,RawStr=100,RawDex=100,RawInt=100 };owner.AddItem(new Backpack());
        owner.MoveToWorld(new Point3D(4196,2868,0),Map.Trammel);owner.Hits=owner.HitsMax;return owner;
    }
    private static void Pet(BaseCreature pet,PlayerMobile owner,int tier=3)
    { HavenPetRarity.Apply(pet,tier);pet.SetControlMaster(owner);pet.MoveToWorld(owner.Location,owner.Map);pet.Hits=pet.HitsMax;pet.Mana=pet.ManaMax; }
    private static Dragon Enemy(Mobile owner,BaseCreature pet,int x=1)
    {
        var enemy=new Dragon();enemy.SetHits(2000);enemy.Hits=2000;enemy.MoveToWorld(new Point3D(owner.X+x,owner.Y,owner.Z),owner.Map);
        enemy.Combatant=pet;return enemy;
    }
    [SkippableFact]
    public void CinderwakeBurnsOnlyEngagedEnemiesOnTheGroundAndStopsWithItsSource()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var pet=new HavenEmberwing();Pet(pet,owner);var enemy=Enemy(owner,pet);var neutral=Enemy(owner,pet,2);neutral.Combatant=null;
        try
        {
            pet.Combatant=enemy;HavenPetAbilities.Think(pet);var field=HavenPetSignatures.Find(pet)?.Field;Assert.NotNull(field);
            foreach(var component in field.Components) { Assert.False(TileData.ItemTable[component.ItemID].Impassable); }
            var hits=enemy.Hits;field.Tick();Assert.True(enemy.Hits<hits);Assert.Equal(2000,neutral.Hits);
            enemy.MoveToWorld(new Point3D(owner.X+6,owner.Y,owner.Z),owner.Map);hits=enemy.Hits;field.Tick();Assert.Equal(hits,enemy.Hits);
            pet.Internalize();field.Tick();Assert.True(field.Deleted);Assert.Null(HavenPetSignatures.Find(pet).Field);
        }
        finally { pet.Delete();enemy.Delete();neutral.Delete();owner.Delete(); }
    }
    [SkippableFact]
    public void MoonMarksImprovePhysicalAttacksWithoutStackingAndCleanUpOnTransfer()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var other=Owner();var pet=new HavenMoonfang();Pet(pet,owner);var enemy=Enemy(owner,pet);
        try
        {
            var resist=enemy.PhysicalResistance;Assert.True(HavenPetSignatures.Activate(pet,enemy));Assert.Equal(resist-14,enemy.PhysicalResistance);
            Assert.False(HavenPetHex.Apply(pet,enemy,0,14,8));Assert.Equal(resist-14,enemy.PhysicalResistance);
            var hex=enemy.Backpack.FindItemByType<HavenPetHex>();Assert.NotNull(hex);pet.SetControlMaster(other);hex.Tick();Assert.True(hex.Deleted);Assert.Equal(resist,enemy.PhysicalResistance);
            Assert.Null(HavenPetTraining.Find(pet));
        }
        finally { pet.Delete();enemy.Delete();owner.Delete();other.Delete(); }
    }
    [SkippableFact]
    public void ChainTempestHitsMultipleEngagedEnemiesButNeverBystandersOrPets()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var pet=new HavenStormscale();Pet(pet,owner);var enemies=new List<Dragon>();
        for(var i=1;i<=5;i++) { enemies.Add(Enemy(owner,pet,i)); }
        var neutral=Enemy(owner,pet,2);neutral.Combatant=null;var ally=new Dog();ally.SetControlMaster(owner);ally.MoveToWorld(neutral.Location,owner.Map);ally.Hits=ally.HitsMax;
        try
        {
            pet.Combatant=enemies[0];var allyHits=ally.Hits;Assert.True(HavenPetSignatures.Activate(pet,enemies[0]));
            foreach(var enemy in enemies) { Assert.True(enemy.Hits<2000); }
            Assert.Equal(2000,neutral.Hits);Assert.Equal(allyHits,ally.Hits);Assert.False(HavenPetSignatures.Activate(pet,enemies[0]));
        }
        finally { foreach(var enemy in enemies) { enemy.Delete(); }pet.Delete();neutral.Delete();ally.Delete();owner.Delete(); }
    }
    [SkippableFact]
    public void GroveSupportsOwnedPetsWithoutMeleeAndCuresOnlyItsUnlockedPoisonTier()
    {
        TileDataRequirement.SkipIfMissing();using var poisons=new HavenPoisonTestScope();var owner=Owner();var other=Owner();var pet=new HavenVerdantLlama();Pet(pet,owner);
        var ally=new Dog();ally.SetControlMaster(owner);ally.MoveToWorld(owner.Location,owner.Map);var outsider=new Dog();outsider.SetControlMaster(other);outsider.MoveToWorld(owner.Location,owner.Map);
        try
        {
            owner.Hits=1;ally.Hits=1;outsider.Hits=1;owner.ApplyPoison(null,Poison.Greater);
            Assert.True(HavenPetSignatures.Support(pet));var field=HavenPetSignatures.Find(pet).Field;
            foreach(var component in field.Components) { Assert.False(TileData.ItemTable[component.ItemID].Impassable); }
            field.Tick();Assert.False(owner.Poisoned);Assert.True(owner.Hits>1);Assert.True(ally.Hits>1);Assert.Equal(1,outsider.Hits);
            Assert.Null(pet.Combatant);pet.Delete();Assert.True(field.Deleted);
        }
        finally { owner.Poison=null;pet.Delete();ally.Delete();outsider.Delete();owner.Delete();other.Delete(); }
    }
    [SkippableFact]
    public void StormhornSuppliesManaWhileBearProtectsAndHellhoundSuppressesHealing()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var horn=new HavenStormhorn();Pet(horn,owner);var bear=new HavenSnowBear();Pet(bear,owner);var hound=new HavenAncientHellhound();Pet(hound,owner);var enemy=Enemy(owner,bear);
        try
        {
            owner.Mana=0;var mana=horn.Mana;Assert.True(HavenPetSignatures.Support(horn));Assert.Equal(27,owner.Mana);Assert.Equal(mana-15,horn.Mana);
            enemy.Combatant=owner;Assert.True(HavenPetSignatures.Activate(bear,enemy));Assert.Same(bear,enemy.Combatant);
            var damage=100;bear.AlterMeleeDamageFrom(enemy,ref damage);Assert.Equal(76,damage);
            Assert.True(HavenPetSignatures.Activate(hound,enemy));Assert.True(MortalStrike.IsWounded(enemy));Assert.True(hound.CanHealOwner);
            bear.Internalize();damage=100;bear.AlterMeleeDamageFrom(enemy,ref damage);Assert.Equal(100,damage);
        }
        finally { MortalStrike.EndWound(enemy);horn.Delete();bear.Delete();hound.Delete();enemy.Delete();owner.Delete(); }
    }
    [SkippableFact]
    public void VampireRescueUsesActualDamageAndCannotAffectWildPetsOrPlayerTargets()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var other=Owner();var pet=new VampiricSteed();Pet(pet,owner);var enemy=Enemy(owner,pet);
        try
        {
            owner.Hits=1;var before=enemy.Hits;Assert.True(HavenPetSignatures.Activate(pet,enemy));
            Assert.InRange(owner.Hits-1,1,before-enemy.Hits);Assert.False(HavenPetSignatures.Activate(pet,other));
            pet.SetControlMaster(null);Assert.False(HavenPetSignatures.Activate(pet,enemy));
            Assert.False(HavenPetSignatures.Support(pet));
        }
        finally { pet.Delete();enemy.Delete();owner.Delete();other.Delete(); }
    }

    [SkippableFact]
    public void LegendaryElementalImmunityStopsDamageButNotMixedOrArmorIgnoringAttacks()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();
        BaseCreature[] pets=[new HavenEmberwing(),new HavenFrostmane(),new HavenStormscale()];
        try
        {
            for(var i=0;i<pets.Length;i++)
            {
                var pet=pets[i];Pet(pet,owner);var fire=i==0 ? 100 : 0;var cold=i==1 ? 100 : 0;var energy=i==2 ? 100 : 0;
                var hits=pet.Hits;
                Assert.Equal(0,AOS.Damage(pet,100,0,fire,cold,0,energy));Assert.Equal(hits,pet.Hits);
                Assert.True(AOS.Damage(pet,100,50,fire/2,cold/2,0,energy/2)>0);
                Assert.Equal(20,AOS.Damage(pet,20,true,0,fire,cold,0,energy));
                Assert.True(AOS.Damage(pet,null,20,false,0,fire,cold,0,energy,0,100)>0);
            }
        }
        finally { foreach(var pet in pets) { pet.Delete(); }owner.Delete(); }
    }

    [SkippableFact]
    public void InnateDefensesPreserveTrainingSeedsAndOrdinaryPetsKeepTheirDamageRules()
    {
        TileDataRequirement.SkipIfMissing();var frost=new HavenFrostmane();var bear=new HavenSnowBear();var ordinary=new Horse();
        try
        {
            frost.ColdResistSeed=78;Assert.Equal(78,frost.ColdResistance);HavenPetRarity.Apply(frost,2);
            Assert.Equal(90,frost.ColdResistance);Assert.Equal(78,frost.ColdResistSeed);
            Assert.Equal(78,HavenPetTraining.Value(frost,8));
            HavenPetRarity.Apply(bear,3);Assert.True(bear.PhysicalResistance>=80);Assert.True(bear.ColdResistance>=90);
            ordinary.SetHits(200);ordinary.Hits=200;ordinary.SetResistance(ResistanceType.Cold,100);
            Assert.Equal(1,AOS.Damage(ordinary,100,0,0,100,0,0));
        }
        finally { frost.Delete();bear.Delete();ordinary.Delete(); }
    }

    [SkippableFact]
    public void StormscaleFightsAtRangeTrainsAndRespectsStoppedFrozenAndHiddenTargets()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var pet=new HavenStormscale();Pet(pet,owner);var enemy=Enemy(owner,pet,5);
        try
        {
            var ai=Assert.IsType<HavenStormscaleAI>(pet.AIObject);var training=HavenPetTraining.Get(pet);Assert.True(training.Begin(owner,pet));
            pet.ControlOrder=OrderType.Attack;pet.ControlTarget=enemy;pet.Combatant=enemy;ai.Action=ActionType.Combat;
            var location=pet.Location;ai.DoOrderAttack();Assert.Equal(location,pet.Location);Assert.True(enemy.Hits<2000);
            Assert.True(training.Progress>0);Assert.False(ai.Fire(enemy));
            pet.ChangeAIType(AIType.AI_Melee);ai=Assert.IsType<HavenStormscaleAI>(pet.AIObject);
            pet.ControlOrder=OrderType.Stop;Assert.False(ai.Fire(enemy));pet.ControlOrder=OrderType.Attack;
            pet.Combatant=enemy;
            pet.Frozen=true;Assert.False(ai.Fire(enemy));pet.Frozen=false;enemy.Hidden=true;Assert.False(ai.Fire(enemy));enemy.Hidden=false;
            enemy.MoveToWorld(new Point3D(owner.X+1,owner.Y,owner.Z),owner.Map);
            ai.NextMove=Core.TickCount-1;var distance=pet.GetDistanceToSqrt(enemy);ai.DoActionCombat();
            Assert.True(pet.GetDistanceToSqrt(enemy)>distance);
        }
        finally { pet.Delete();enemy.Delete();owner.Delete(); }
    }

    [SkippableFact]
    public void LegendaryStormhornInterruptsAnActualCreatureSpell()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var pet=new HavenStormhorn();Pet(pet,owner);var enemy=Enemy(owner,pet);
        var spell=new Server.Spells.Sixth.EnergyBoltSpell(enemy,null);
        try
        {
            enemy.Mana=100;spell.State=Server.Spells.SpellState.Casting;enemy.Spell=spell;
            Assert.True(HavenPetSignatures.Activate(pet,enemy));Assert.False(spell.IsCasting);Assert.Null(enemy.Spell);
        }
        finally { spell.FinishSequence();pet.Delete();enemy.Delete();owner.Delete(); }
    }

    [SkippableFact]
    public void TrainingShowsInnateDefensesAndChargesOnlyForUsefulUpgrades()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var common=new HavenFrostmane();Pet(common,owner,0);var legendary=new HavenFrostmane();Pet(legendary,owner);
        try
        {
            var record=HavenPetTraining.Get(common);Assert.True(record.Begin(owner,common));record.Progress=10000;
            Assert.Equal(75,HavenPetTraining.DisplayValue(common,8));var points=record.PointsTenths;
            Assert.True(record.Upgrade(owner,common,8,1));Assert.Equal(76,common.ColdResistance);Assert.Equal(76,common.ColdResistSeed);Assert.Equal(points-30,record.PointsTenths);
            var high=HavenPetTraining.Get(legendary);Assert.True(high.Begin(owner,legendary));high.Progress=10000;points=high.PointsTenths;
            Assert.Equal(100,HavenPetTraining.DisplayValue(legendary,8));Assert.False(high.Upgrade(owner,legendary,8,1));Assert.Equal(points,high.PointsTenths);
        }
        finally { common.Delete();legendary.Delete();owner.Delete(); }
    }

    [SkippableFact]
    public void EveryCustomSpeciesHasFourDistinctTonesAndDyesSurviveAutomaticRefresh()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();
        Func<BaseCreature>[] create=[()=>new HavenEmberwing(),()=>new HavenMoonfang(),()=>new HavenFrostmane(),()=>new HavenVerdantLlama(),
            ()=>new HavenStormscale(),()=>new HavenStormhorn(),()=>new HavenSnowBear(),()=>new HavenAncientHellhound(),()=>new VampiricSteed()];
        try
        {
            foreach(var factory in create)
            {
                var hues=new HashSet<int>();
                for(var tier=0;tier<4;tier++)
                {
                    var pet=factory();
                    try { Pet(pet,owner,tier);Assert.True(hues.Add(pet.Hue));Assert.Equal(HavenPetAppearance.NaturalHue(pet),pet.Hue); }
                    finally { pet.Delete(); }
                }
            }
            var dyed=new HavenMoonfang();Pet(dyed,owner);var dye=new HavenPetDye();owner.Backpack.DropItem(dye);
            try
            {
                Assert.True(dye.Apply(owner,dyed,0));var selected=dyed.Hue;var stats=dyed.RawDex;
                dyed.Backpack.FindItemByType<HavenPetDyeRecord>().OriginalHue=0x47E; // legacy saved original color
                HavenPetAppearance.Refresh(dyed);Assert.Equal(selected,dyed.Hue);Assert.Equal(stats,dyed.RawDex);
                Assert.Equal(HavenPetAppearance.NaturalHue(dyed),dyed.Backpack.FindItemByType<HavenPetDyeRecord>().OriginalHue);
                dye=new HavenPetDye();owner.Backpack.DropItem(dye);Assert.True(dye.Apply(owner,dyed,8));
                Assert.Equal(HavenPetAppearance.NaturalHue(dyed),dyed.Hue);
            }
            finally { dye.Delete();dyed.Delete(); }
        }
        finally { owner.Delete(); }
    }

    [SkippableFact]
    public void LegendaryShimmerIsQuietDuringCombatStealthAndMounting()
    {
        TileDataRequirement.SkipIfMissing();var owner=Owner();var pet=new HavenFrostmane();Pet(pet,owner);var enemy=Enemy(owner,pet);
        try
        {
            pet.Hidden=true;Assert.False(HavenPetAppearance.Shimmer(pet));pet.Hidden=false;
            owner.Hidden=true;Assert.False(HavenPetAppearance.Shimmer(pet));owner.Hidden=false;
            pet.Combatant=enemy;Assert.False(HavenPetAppearance.Shimmer(pet));pet.Combatant=null;
            pet.Rider=owner;Assert.False(HavenPetAppearance.Shimmer(pet));pet.Rider=null;
            Assert.True(HavenPetAppearance.Shimmer(pet));Assert.False(HavenPetAppearance.Shimmer(pet));
            Assert.True(HavenPetSignatures.Find(pet).NextShimmer>Core.Now);
        }
        finally { pet.Delete();enemy.Delete();owner.Delete(); }
    }
}
