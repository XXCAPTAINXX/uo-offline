using System;
using System.IO;
using System.Linq;
using Server;
using Server.Accounting;
using Server.Items;
using Server.Mobiles;
using Server.HavenPrototype;

public static class AdvancedGearSmoke
{
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); File.AppendAllText("advanced-gear-checks.log", "PASS " + name + "\n"); }
    public static void Initialize() { if (File.Exists("ADVANCED-GEAR-TEST-ONLY")) EventSink.ServerStarted += () => Timer.DelayCall(TimeSpan.FromSeconds(4), Run); }
    static void Run()
    {
        try {
            var saved = World.Mobiles.Values.OfType<PlayerMobile>().FirstOrDefault(x => x.Name == "Advanced gear fixture v3");
            if (saved != null) {
                var wallet = HavenWallet.Find(saved); Check(wallet != null && wallet.AstralShards == 40, "wallet shard balance persists");
                var mantle = saved.Backpack.FindItemByType(typeof(AstralGuardianMantle), true) as AstralGuardianMantle;
                Check(mantle != null && HavenAdvancedGear.Find(mantle).Level == 20 && mantle.Attributes.BonusHits == 38, "Astral item identity and progression persist");
                var artifact = saved.Backpack.FindItemByType(typeof(LegacyOfTheDreadLord), true);
                Check(artifact != null && HavenAdvancedGear.Find(artifact).Kind == 3 && HavenAdvancedGear.Find(artifact).Level == 20, "reforged native artifact persists");
                int damage = HavenAdvancedGear.Attributes(artifact).WeaponDamage; HavenAdvancedGear.Find(artifact).Apply();
                Check(HavenAdvancedGear.Attributes(artifact).WeaponDamage == damage, "reapplying restored record cannot duplicate bonuses");
                File.AppendAllText("advanced-gear-checks.log", "RELOAD COMPLETE\n"); Core.Kill(false); return;
            }
            var p = new PlayerMobile { Player = true, Name = "Advanced gear fixture v3", Body = 0x190, RawStr = 250, RawInt = 150 };
            p.AddItem(new Backpack()); new Account("advanced-gear-fixture-v3", Guid.NewGuid().ToString("N"))[0] = p;
            var stone = World.Items.Values.OfType<HavenServiceStone>().First(x => x.Service == 0);
            p.MoveToWorld(stone.Location, stone.Map);
            var walletNew = new HavenWallet(p); p.Backpack.DropItem(walletNew);
            var bag = new Bag(); p.Backpack.DropItem(bag); var shards = new AstralShard(60); bag.DropItem(shards);
            walletNew.OnDoubleClick(p); Check(shards.Deleted && walletNew.AstralShards == 60, "nested pack shards deposit into wallet");
            walletNew.OnDoubleClick(p); Check(walletNew.AstralShards == 60, "reopening cannot duplicate shard deposits");
            int index = HavenSupplyShops.Catalogs[2].FindIndex(x => x.Shards == 20);
            Check(index >= 0 && HavenSupplyShops.Buy(p, 2, index) && walletNew.AstralShards == 40, "Astral purchase spends only exact shard price");
            Check(p.Backpack.FindItemByType(typeof(AstralWeaversRing), true) != null, "purchased actual Astral ring delivered");
            int max = p.Backpack.MaxItems; p.Backpack.MaxItems = p.Backpack.TotalItems;
            Check(!HavenSupplyShops.Buy(p, 2, index) && walletNew.AstralShards == 40, "full pack purchase does not consume shards"); p.Backpack.MaxItems = max;
            int recruitIndex = HavenSupplyShops.Catalogs[2].FindIndex(x => x.FreeClaim != null);
            Check(recruitIndex >= 0 && HavenSupplyShops.Buy(p,2,recruitIndex), "first recruit piece is free");
            Check(!HavenSupplyShops.Buy(p,2,recruitIndex), "repeat recruit claim requires payment");
            for(int tier=0;tier<3;tier++)for(int piece=0;piece<5;piece++){var equipment=HavenShieldWarriorGear.Create(tier,piece);Check(HavenAdvancedGear.AutoKind(equipment)==5 && HavenAdvancedGear.Attributes(equipment)!=null,"shield warrior tier and piece supported");equipment.Delete();}
            var mantleNew = new AstralGuardianMantle(); p.Backpack.DropItem(mantleNew); var growth = HavenAdvancedGear.Attach(mantleNew, 1);
            growth.Gain(1900); int luck = mantleNew.Attributes.Luck; growth.Gain(100); growth.Apply();
            Check(growth.Level == 20 && mantleNew.Attributes.BonusHits == 38 && mantleNew.Attributes.Luck == luck, "Astral max growth is idempotent");
            var ordinary = new Longsword(); Check(HavenAdvancedGear.AutoKind(ordinary) == 0 && HavenAdvancedGear.Find(ordinary) == null, "ordinary gear does not acquire progression"); ordinary.Delete();
            var artifactNew = new LegacyOfTheDreadLord(); p.Backpack.DropItem(artifactNew); var recipe = new HavenDoomRecipe(typeof(LegacyOfTheDreadLord)); p.Backpack.DropItem(recipe);
            var ingots = new IronIngot(100); var diamonds = new Diamond(20); p.Backpack.DropItem(ingots); p.Backpack.DropItem(diamonds);
            Check(!recipe.Upgrade(p, artifactNew) && ingots.Amount == 100 && diamonds.Amount == 20, "insufficient crafting skill preserves recipe and materials");
            p.Skills.Blacksmith.Base = 100; int serial = artifactNew.Serial.Value;
            Check(recipe.Upgrade(p, artifactNew) && recipe.Deleted && ingots.Deleted && diamonds.Deleted && artifactNew.Serial.Value == serial, "reforge consumes exact materials and preserves artifact identity");
            var duplicate = new HavenDoomRecipe(typeof(LegacyOfTheDreadLord)); p.Backpack.DropItem(duplicate);
            Check(!duplicate.Upgrade(p, artifactNew) && !duplicate.Deleted, "duplicate reforging rejected");
            HavenAdvancedGear.Find(artifactNew).Gain(1900); Check(artifactNew.WeaponAttributes.HitLeechMana >= 100, "end game artifact reaches full mana leech chance");
            for (int i = 0; i < 20; i++) { var legendary = HavenAdvancedRewards.CreateLegendary(); Check(HavenAdvancedGear.Find(legendary) != null && HavenAdvancedGear.Find(legendary).Kind == 2, "Legendary drop receives evolution record"); legendary.Delete(); }
            for (int theme = 0; theme < 9; theme++) {
                var bracelet = HavenJewelrySets.CreateBracelet(theme); int baseLuck = bracelet.Attributes.Luck, baseStr = bracelet.Attributes.BonusStr;
                var braceletRecord = HavenAdvancedGear.Attach(bracelet, HavenAdvancedGear.AutoKind(bracelet)); braceletRecord.Gain(1900); braceletRecord.Apply();
                Check(bracelet.Attributes.Luck == baseLuck + 95 && bracelet.Attributes.BonusStr == baseStr + 4, "bracelet theme " + theme + " levels without losing base stats or stacking twice"); bracelet.Delete();
            }
            var equippedBracelet = new BraceletOfFortune(); p.AddItem(equippedBracelet);
            var monster = new Ogre { HitsMaxSeed = 4500 }; monster.Hits = monster.HitsMax; monster.MoveToWorld(p.Location, p.Map);
            monster.Damage(monster.Hits + 1, p);
            Check(HavenAdvancedGear.Find(equippedBracelet) != null && HavenAdvancedGear.Find(equippedBracelet).Experience == 20, "equipped bracelet earns XP from actual kill");
            Check(p.Backpack.FindItemsByType(typeof(AstralShard), true).Sum(x => x.Amount) == 3, "actual eligible boss kill awards three shards");
            World.Save(); File.AppendAllText("advanced-gear-checks.log", "FRESH COMPLETE\n"); Core.Kill(false);
        } catch (Exception e) { File.AppendAllText("advanced-gear-checks.log", "FAIL " + e + "\n"); Core.Kill(false); }
    }
}
