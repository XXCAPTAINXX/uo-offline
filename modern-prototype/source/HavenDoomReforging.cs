using System;
using System.Linq;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using Server.Engines.Points;

namespace Server.HavenPrototype
{
    public static class HavenDoomReforging
    {
        public static bool IsArtifact(Item item) { return item != null && DoomGauntlet.DoomArtifact.Contains(item.GetType()); }
        public static void Award(BaseCreature victim, Mobile player)
        {
            if (victim.Map != Map.Malas || !(victim is DemonKnight || victim is DarknightCreeper || victim is FleshRenderer || victim is Impaler || victim is ShadowKnight || victim is AbysmalHorror)) return;
            if (Utility.RandomDouble() < 0.04) HavenAdvancedRewards.Deliver(player, victim, Utility.RandomBool() ? (Item)new HavenGravefireScimitar() : new HavenGravefireMace());
            if (Utility.RandomDouble() >= 0.10) return;
            HavenAdvancedRewards.Deliver(player, victim, new HavenDoomRecipe(DoomGauntlet.DoomArtifact[Utility.Random(DoomGauntlet.DoomArtifact.Length)]));
        }
    }
    public class HavenDoomRecipe : Item
    {
        public string ArtifactName { get; private set; }
        [Constructable] public HavenDoomRecipe() : this(typeof(LegacyOfTheDreadLord)) { }
        public HavenDoomRecipe(Type type) : base(0x2831) { Name = "Doom reforging recipe"; Weight = 1; ArtifactName = type.FullName; }
        public HavenDoomRecipe(Serial serial) : base(serial) { }
        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list); list.Add("Artifact: " + ArtifactName.Split('.').Last());
            list.Add("Requires 100 crafting skill, 100 iron ingots, 20 diamonds");
            list.Add("Adds +10 weapon/spell damage, +2 hit/mana regen; evolves to level 20");
        }
        public bool Upgrade(Mobile p, Item item)
        {
            if (Deleted || p == null || !p.Alive || p.Backpack == null || !IsChildOf(p.Backpack) || !HavenResources.Accessible(p, this) ||
                item == null || item.Deleted || !item.IsChildOf(p.Backpack) || !HavenResources.Accessible(p, item) || item.GetType().FullName != ArtifactName ||
                !HavenDoomReforging.IsArtifact(item) || HavenAdvancedGear.Find(item) != null) return false;
            var a = HavenAdvancedGear.Attributes(item); if (a == null) return false;
            var skill = item is BaseWeapon || item is BaseShield ? SkillName.Blacksmith : item is BaseJewel ? SkillName.Tinkering : item is BaseClothing ? SkillName.Tailoring : SkillName.Blacksmith;
            var ingots = p.Backpack.FindItemsByType(typeof(IronIngot), true).Where(x => HavenResources.Accessible(p, x)).ToArray();
            var diamonds = p.Backpack.FindItemsByType(typeof(Diamond), true).Where(x => HavenResources.Accessible(p, x)).ToArray();
            if (p.Skills[skill].Base < 100 || ingots.Sum(x => (long)x.Amount) < 100 || diamonds.Sum(x => (long)x.Amount) < 20) return false;
            Consume(ingots, 100); Consume(diamonds, 20);
            a.WeaponDamage += 10; a.SpellDamage += 10; a.RegenHits += 2; a.RegenMana += 2;
            HavenAdvancedGear.Attach(item, 3); item.InvalidateProperties(); Delete(); return true;
        }
        static void Consume(Item[] items, int amount) { foreach (var item in items) { int take = Math.Min(item.Amount, amount); item.Consume(take); amount -= take; if (amount == 0) break; } }
        public override void OnDoubleClick(Mobile p)
        {
            if (p.Backpack == null || !IsChildOf(p.Backpack)) return;
            p.SendMessage("Target the matching artifact in your pack. Success consumes this recipe, 100 iron ingots and 20 diamonds."); p.Target = new ReforgeTarget(this);
        }
        class ReforgeTarget : Target
        {
            readonly HavenDoomRecipe _recipe; public ReforgeTarget(HavenDoomRecipe recipe) : base(-1, false, TargetFlags.None) { _recipe = recipe; }
            protected override void OnTarget(Mobile p, object target) { p.SendMessage(_recipe.Upgrade(p, target as Item) ? "Reforged. Equip your artifact to earn experience." : "Check the matching artifact, crafting skill and materials. Nothing was consumed."); }
        }
        public override void Serialize(GenericWriter w) { base.Serialize(w); w.Write(0); w.Write(ArtifactName); }
        public override void Deserialize(GenericReader r) { base.Deserialize(r); r.ReadInt(); ArtifactName = r.ReadString(); }
    }
}
