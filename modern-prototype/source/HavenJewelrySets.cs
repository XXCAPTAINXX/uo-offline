using System;
using Server;
using Server.Items;
using Server.Mobiles;

namespace Server.HavenPrototype
{
    public class HavenSetRing : GoldRing
    {
        public int Theme { get; private set; }
        [Constructable] public HavenSetRing() : this(0) { }
        [Constructable] public HavenSetRing(int theme)
        {
            Theme = Math.Max(0, Math.Min(8, theme));
            var bracelet = HavenJewelrySets.CreateBracelet(Theme);
            Name = HavenJewelrySets.Names[Theme] + " ring";
            Hue = bracelet.Hue;
            LootType = LootType.Blessed;
            foreach (AosAttribute attr in Enum.GetValues(typeof(AosAttribute))) Attributes[attr] = bracelet.Attributes[attr];
            foreach (AosElementAttribute attr in Enum.GetValues(typeof(AosElementAttribute))) Resistances[attr] = bracelet.Resistances[attr];
            for (int i = 0; i < 5; i++)
            {
                SkillName skill; double value;
                bracelet.SkillBonuses.GetValues(i, out skill, out value);
                if (value > 0) SkillBonuses.SetValues(i, skill, value);
            }
            bracelet.Delete();
        }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            var progress = HavenEquipmentEvolution.Find(this);
            list.Add("Equipment level: " + (progress == null ? 1 : progress.Level) + "/20; XP: " + (progress == null ? 0 : progress.Experience) + "/1900");
            list.Add("Matching bracelet: " + HavenJewelrySets.Descriptions[Theme]);
            list.Add("With Concord talisman: +250 Luck, +10% weapon/spell damage");
        }
        public HavenSetRing(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(Theme); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); Theme = Math.Max(0, Math.Min(8, r.ReadInt())); }
    }

    public class HavenConcordTalisman : BaseTalisman
    {
        [Constructable] public HavenConcordTalisman() : base(0x2F5A)
        {
            Name = "Haven talisman of concord"; Hue = 0x489; LootType = LootType.Blessed;
            Attributes.Luck = 300;
            Attributes.BonusStr = Attributes.BonusDex = Attributes.BonusInt = 10;
            Attributes.RegenHits = Attributes.RegenStam = Attributes.RegenMana = 3;
            Attributes.WeaponDamage = Attributes.SpellDamage = 20;
            Attributes.LowerManaCost = 5; Attributes.LowerRegCost = 20;
        }
        public void UnlockFollower(Mobile owner)
        {
            var progress = HavenEquipmentEvolution.Find(this);
            if (!(owner is PlayerMobile) || Parent != owner || progress == null || progress.Level != 20 || owner.FollowersMax >= 6) return;
            owner.FollowersMax = 6;
            owner.SendMessage("Your level-20 Concord talisman unlocks permanent follower capacity 6. This does not stack.");
        }
        public override void OnDoubleClick(Mobile from) { UnlockFollower(from); base.OnDoubleClick(from); }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            var progress = HavenEquipmentEvolution.Find(this);
            list.Add("Equipment level: " + (progress == null ? 1 : progress.Level) + "/20; XP: " + (progress == null ? 0 : progress.Experience) + "/1900");
            list.Add("Matching ring/bracelet: +250 Luck, +10% weapon/spell damage");
            list.Add("Level 20: equip and double-click to unlock follower capacity 6");
        }
        public HavenConcordTalisman(Serial serial) : base(serial) { }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); }
    }

    public static class HavenJewelrySets
    {
        public static readonly string[] Names = { "Vanguard", "Arcane Focus", "Wind", "Beastmaster", "Virtuoso", "Artisan", "Fortune", "Guardian", "Night" };
        public static readonly string[] Descriptions = { "+20% weapon damage, +2 hit regen", "+15% spell damage, +5% lower mana cost", "+10% swing speed, +3 stamina regen", "+150 Luck, +2 all regens", "+10% spell damage, +3 mana regen", "+250 Luck, +20% lower reagent cost", "+350 Luck", "+10% defense chance, +3 hit regen", "+3 all regens" };
        public static BaseJewel CreateBracelet(int theme)
        {
            switch (theme) {
                case 0: return new BraceletOfTheVanguard(); case 1: return new BraceletOfArcaneFocus();
                case 2: return new BraceletOfTheWind(); case 3: return new BraceletOfTheBeastmaster();
                case 4: return new BraceletOfTheVirtuoso(); case 5: return new BraceletOfTheArtisan();
                case 6: return new BraceletOfFortune(); case 7: return new BraceletOfTheGuardian();
                default: return new BraceletOfTheNight();
            }
        }
        public static int BraceletTheme(Item item)
        {
            if (item is BraceletOfTheVanguard) return 0; if (item is BraceletOfArcaneFocus) return 1;
            if (item is BraceletOfTheWind) return 2; if (item is BraceletOfTheBeastmaster) return 3;
            if (item is BraceletOfTheVirtuoso) return 4; if (item is BraceletOfTheArtisan) return 5;
            if (item is BraceletOfFortune) return 6; if (item is BraceletOfTheGuardian) return 7;
            return item is BraceletOfTheNight ? 8 : -1;
        }
        public static int GetBonus(Mobile wearer, AosAttribute attr)
        {
            if (wearer == null) return 0;
            var ring = wearer.FindItemOnLayer(Layer.Ring) as HavenSetRing;
            if (ring == null || BraceletTheme(wearer.FindItemOnLayer(Layer.Bracelet)) != ring.Theme) return 0;
            int result = 0;
            bool regen = attr == AosAttribute.RegenHits || attr == AosAttribute.RegenStam || attr == AosAttribute.RegenMana;
            switch (ring.Theme) {
                case 0: result = attr == AosAttribute.WeaponDamage ? 20 : attr == AosAttribute.RegenHits ? 2 : 0; break;
                case 1: result = attr == AosAttribute.SpellDamage ? 15 : attr == AosAttribute.LowerManaCost ? 5 : 0; break;
                case 2: result = attr == AosAttribute.WeaponSpeed ? 10 : attr == AosAttribute.RegenStam ? 3 : 0; break;
                case 3: result = attr == AosAttribute.Luck ? 150 : regen ? 2 : 0; break;
                case 4: result = attr == AosAttribute.SpellDamage ? 10 : attr == AosAttribute.RegenMana ? 3 : 0; break;
                case 5: result = attr == AosAttribute.Luck ? 250 : attr == AosAttribute.LowerRegCost ? 20 : 0; break;
                case 6: result = attr == AosAttribute.Luck ? 350 : 0; break;
                case 7: result = attr == AosAttribute.DefendChance ? 10 : attr == AosAttribute.RegenHits ? 3 : 0; break;
                case 8: result = regen ? 3 : 0; break;
            }
            if (wearer.FindItemOnLayer(Layer.Talisman) is HavenConcordTalisman)
                result += attr == AosAttribute.Luck ? 250 : attr == AosAttribute.WeaponDamage || attr == AosAttribute.SpellDamage ? 10 : 0;
            return result;
        }
    }
}
