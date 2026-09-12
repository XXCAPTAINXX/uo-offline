using System;
using System.Linq;
using System.Collections.Generic;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    // Internal records preserve the equipment itself, including durability and native attributes.
    public class HavenAdvancedGear : Item
    {
        static readonly Dictionary<int, HavenAdvancedGear> Records = new Dictionary<int, HavenAdvancedGear>();
        public Item Equipment; public int Experience, AppliedLevel = 1, Kind;
        public string OriginalName;
        public int Level { get { return Math.Min(20, 1 + Experience / 100); } }
        public static AosAttributes Attributes(Item item)
        {
            if (item is BaseWeapon) return ((BaseWeapon)item).Attributes;
            if (item is BaseArmor) return ((BaseArmor)item).Attributes;
            if (item is BaseClothing) return ((BaseClothing)item).Attributes;
            if (item is BaseJewel) return ((BaseJewel)item).Attributes;
            if (item is Spellbook) return ((Spellbook)item).Attributes;
            if (item is BaseTalisman) return ((BaseTalisman)item).Attributes;
            return null;
        }
        static readonly HashSet<string> QuestNames = new HashSet<string>(new[] { "ArmsOfArmstrong", "BulwarkLeggings", "BraceletOfResilience", "EscutcheonDeAriadne", "EmberStaff", "ClaspOfConcentration", "ChurchillsWarMace", "Heartseeker", "HealersTouch", "HallowedSpellbook", "GlovesOfSafeguarding", "TheDragonsTail", "JocklesQuicksword", "JacobsPickaxe", "PhilosophersHat", "RecarosRiposte", "TunicOfGuarding", "SilverSerpentBlade", "RingOfTheSavant", "TwilightJacket", "WalkersLeggings", "HavenQuestNecromancerBook" });
        public static int AutoKind(Item item)
        {
            if (item is IHavenAreaWeapon || item is IHavenWhirlwindWeapon) return 6;
            if (item is AstralWeaversRing || item is AstralGuardianMantle || item is AstralFortuneEarrings) return 1;
            if (QuestNames.Contains(item.GetType().Name)) return 4;
            if (item is IHavenShieldWarriorGear || HavenJewelrySets.BraceletTheme(item) >= 0 || item is HavenChampionPendant) return 5;
            return 0;
        }
        public static HavenAdvancedGear Find(Item item) { HavenAdvancedGear record; return item != null && Records.TryGetValue(item.Serial.Value, out record) && !record.Deleted ? record : null; }
        public static HavenAdvancedGear Attach(Item item, int kind)
        {
            if (item == null || item.Deleted || Attributes(item) == null || kind < 1 || kind > 6) return null;
            HavenGearDurability.Apply(item);
            return Find(item) ?? new HavenAdvancedGear(item, kind);
        }
        public HavenAdvancedGear(Item item, int kind) : base(1)
        {
            Equipment = item; Kind = kind; OriginalName = item.Name ?? item.GetType().Name;
            Visible = false; Movable = false; Internalize(); Records[item.Serial.Value] = this; Apply();
        }
        public HavenAdvancedGear(Serial serial) : base(serial) { }
        public void Gain(int amount) { if (amount <= 0) return; Experience = Math.Min(1900, Experience + Math.Min(1900, amount)); Apply(); }
        public void Apply()
        {
            if (Equipment == null || Equipment.Deleted) return;
            var a = Attributes(Equipment); if (a == null) return;
            int levels = Math.Max(0, Level - AppliedLevel), milestones = Math.Max(0, Level / 5 - AppliedLevel / 5), steps = Level - 1;
            a.Luck += levels * 5; a.BonusStr += milestones; a.BonusDex += milestones; a.BonusInt += milestones;
            var weapon = Equipment as BaseWeapon;
            if (Kind == 2 || Kind == 3 || Kind == 4 || Kind == 6) {
                if (weapon != null && Kind != 3) {
                    a.WeaponDamage += levels * 2; a.WeaponSpeed += milestones * 5;
                    if (Kind == 2 || Kind == 6) { weapon.WeaponAttributes.HitLeechMana += milestones * 5; weapon.WeaponAttributes.HitLeechHits += milestones * 5; }
                } else {
                    a.SpellDamage += levels; a.RegenHits += milestones; a.RegenMana += milestones;
                    if (Kind != 4) a.WeaponDamage += levels;
                    if (weapon != null) { a.WeaponSpeed += milestones * 2; weapon.WeaponAttributes.HitLeechMana += milestones * 3; }
                }
            }
            if (weapon != null) weapon.WeaponAttributes.HitLeechMana = Math.Max(weapon.WeaponAttributes.HitLeechMana, 20 + steps * 80 / 19);
            if (Equipment is AstralWeaversRing) {
                a.SpellDamage = Math.Max(a.SpellDamage, 30 + steps * 2); a.LowerManaCost = Math.Max(a.LowerManaCost, 10 + steps / 4);
                a.RegenMana = Math.Max(a.RegenMana, 3 + steps / 3); a.CastRecovery = Math.Max(a.CastRecovery, steps / 6);
            } else if (Equipment is AstralGuardianMantle) {
                a.RegenHits = Math.Max(a.RegenHits, 5 + steps / 3); a.RegenStam = Math.Max(a.RegenStam, steps / 3);
                a.DefendChance = Math.Max(a.DefendChance, 15 + steps / 2); a.BonusHits = Math.Max(a.BonusHits, steps * 2);
            } else if (Equipment is AstralFortuneEarrings) {
                a.RegenMana = Math.Max(a.RegenMana, 4 + steps / 3); a.Luck = Math.Max(a.Luck, 400 + steps * 20);
                a.WeaponDamage = Math.Max(a.WeaponDamage, steps); a.SpellDamage = Math.Max(a.SpellDamage, steps);
            }
            if (weapon != null) HavenAreaWeapons.Apply(weapon, Level);
            AppliedLevel = Level;
            Equipment.Name = (Kind == 2 ? "Legendary " : Kind == 3 ? "Reforged " : "") + OriginalName + " [level " + Level + "/20]";
            Equipment.InvalidateProperties();
        }
        public override void OnDelete() { if (Equipment != null && Find(Equipment) == this) Records.Remove(Equipment.Serial.Value); base.OnDelete(); }
        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), () => { foreach (var record in Records.Values.ToArray()) if (record.Equipment == null || record.Equipment.Deleted) record.Delete(); });
            EventSink.ServerStarted += () => { if (HavenPreview.Enabled) foreach (var item in World.Items.Values.ToArray()) if (AutoKind(item) > 0) Attach(item, AutoKind(item)); };
            EventSink.CreatureDeath += e => {
                var victim = e.Creature as BaseCreature; var owner = e.Killer == null ? null : e.Killer.GetDamageMaster(victim) ?? e.Killer;
                if (!HavenStarterGear.Eligible(victim, owner)) return;
                int xp = Math.Max(1, Math.Min(20, victim.HitsMax / 100));
                foreach (var item in owner.Items.ToArray()) { var record = Find(item) ?? Attach(item, AutoKind(item)); if (record != null) record.Gain(xp); }
                HavenAdvancedRewards.Award(victim, owner);
            };
        }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(Equipment); w.Write(Experience); w.Write(AppliedLevel); w.Write(Kind); w.Write(OriginalName); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); Equipment = r.ReadItem(); Experience = r.ReadInt(); AppliedLevel = r.ReadInt(); Kind = r.ReadInt(); OriginalName = r.ReadString(); if (Equipment != null) Records[Equipment.Serial.Value] = this; }
    }
    public class AstralShard : Item
    {
        [Constructable] public AstralShard() : this(1) { }
        [Constructable] public AstralShard(int amount) : base(0x1F19) { Name = "Astral shard"; Hue = 0x482; Stackable = true; Weight = 0; Amount = amount; }
        public override void OnDoubleClick(Mobile p) { var wallet = HavenWallet.Find(p); if (wallet != null) wallet.DepositShard(p, this); }
        public AstralShard(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
    public class AstralWeaversRing : GoldRing
    {
        [Constructable] public AstralWeaversRing() { Name = "Astral weaver's ring"; Hue = 0x482; LootType = LootType.Blessed; Attributes.SpellDamage = 30; Attributes.LowerManaCost = 10; Attributes.RegenMana = 3; SkillBonuses.SetValues(0, SkillName.Magery, 15); SkillBonuses.SetValues(1, SkillName.Spellweaving, 15); }
        public AstralWeaversRing(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
    public class AstralGuardianMantle : Cloak
    {
        [Constructable] public AstralGuardianMantle() { Name = "Astral guardian's mantle"; Hue = 0x482; LootType = LootType.Blessed; Attributes.RegenHits = 5; Attributes.DefendChance = 15; Attributes.Luck = 300; }
        public AstralGuardianMantle(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
    public class AstralFortuneEarrings : GoldEarrings
    {
        [Constructable] public AstralFortuneEarrings() { Name = "Astral fortune earrings"; Hue = 0x482; LootType = LootType.Blessed; Attributes.LowerRegCost = 100; Attributes.Luck = 400; Attributes.RegenMana = 4; }
        public AstralFortuneEarrings(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }
    public static class HavenAdvancedRewards
    {
        public static double LegendaryChance(BaseCreature c, Mobile p)
        {
            double basis = c.HitsMax >= 5000 ? 0.05 : c is HavenOldWarden ? 0.02 : c.HitsMax >= 1000 ? 0.01 : c.HitsMax >= 300 ? 0.003 : 0.001;
            return basis * (1 + Math.Max(0, Math.Min(5000, p.Luck)) / 5000.0);
        }
        public static Item CreateLegendary()
        {
            if (Utility.Random(5) == 0) return HavenAreaWeapons.Create(Utility.Random(8));
            Item item = Utility.Random(5) == 0 ? (Item)new Spellbook(ulong.MaxValue) : Loot.RandomArmorOrShieldOrWeaponOrJewelry();
            BaseRunicTool.ApplyAttributesTo(item, false, 0, item is Spellbook ? 6 : 8, 90, 100);
            var a = HavenAdvancedGear.Attributes(item);
            if (a != null) { a.RegenHits = Math.Max(2, a.RegenHits); a.RegenMana = Math.Max(2, a.RegenMana); }
            var weapon = item as BaseWeapon;
            if (weapon != null) { a.WeaponDamage = Math.Max(40, a.WeaponDamage); a.WeaponSpeed = Math.Max(20, a.WeaponSpeed); weapon.WeaponAttributes.HitLeechMana = Math.Max(40, weapon.WeaponAttributes.HitLeechMana); weapon.WeaponAttributes.HitLeechHits = Math.Max(40, weapon.WeaponAttributes.HitLeechHits); }
            if (item is Spellbook) { a.SpellDamage = Math.Max(30, a.SpellDamage); a.LowerManaCost = Math.Max(8, a.LowerManaCost); }
            HavenAdvancedGear.Attach(item, 2); return item;
        }
        public static void Deliver(Mobile player, BaseCreature victim, Item item)
        {
            if (player.Backpack != null && player.Backpack.TryDropItem(player, item, false)) { player.SendMessage("Found " + item.Name + " in your backpack."); return; }
            if (victim.Corpse != null && !victim.Corpse.Deleted) { victim.Corpse.DropItem(item); player.SendMessage("Your pack is full; " + item.Name + " is in the corpse."); }
            else { item.MoveToWorld(player.Location, player.Map); player.SendMessage("Your pack is full; " + item.Name + " is at your feet."); }
        }
        public static void Award(BaseCreature victim, Mobile player)
        {
            if (!HavenStarterGear.Eligible(victim, player)) return;
            if (Utility.RandomDouble() < LegendaryChance(victim, player)) Deliver(player, victim, CreateLegendary());
            double chance = victim.HitsMax >= 4000 ? 1 : victim.HitsMax >= 1000 ? 0.15 : 0.05;
            if (Utility.RandomDouble() < chance) Deliver(player, victim, new AstralShard(victim.HitsMax >= 4000 ? 3 : 1));
            HavenDoomReforging.Award(victim, player);
        }
    }
}
