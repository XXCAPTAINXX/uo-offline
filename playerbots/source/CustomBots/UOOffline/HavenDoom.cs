using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.Engines.Doom;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.UOOffline;

public static class HavenDoom
{
    public static void Initialize()
    {
        CommandSystem.Register("HavenDoomStatus", AccessLevel.GameMaster, e => e.Mobile.SendMessage($"Doom gauntlet controllers: {Controllers().Count}/6."));
        CommandSystem.Register("HavenDoomSetup", AccessLevel.GameMaster, e =>
        {
            var controllers = Controllers();
            if (controllers.Count != 0) { e.Mobile.SendMessage($"Doom has {controllers.Count} controllers. Existing encounters have been preserved; inspect partial setups manually."); return; }
            EnsureGauntlet(e);
            e.Mobile.SendMessage($"Doom gauntlet installed: {Controllers().Count} controllers. The native room sequence and artifact rewards are active.");
        });
    }
    internal static bool EnsureGauntlet(CommandEventArgs args)
    {
        if (Controllers().Count != 0) { return false; }
        GenGauntlet.GenGauntlet_OnCommand(args);
        return Controllers().Count == 6;
    }
    internal static List<GauntletSpawner> Controllers()
    {
        var found = new List<GauntletSpawner>();
        foreach (var item in Map.Malas.GetItemsInRange<GauntletSpawner>(new Point3D(410, 465, -1), 130)) { if (!item.Deleted) { found.Add(item); } }
        return found;
    }
    internal static bool IsArtifact(Item item)
    {
        var type = item.GetType();
        foreach (var candidate in DemonKnight.ArtifactRarity10) { if (candidate == type) { return true; } }
        foreach (var candidate in DemonKnight.ArtifactRarity11) { if (candidate == type) { return true; } }
        return false;
    }
    internal static bool IsBoss(BaseCreature creature) => creature is DemonKnight or DarknightCreeper or FleshRenderer or Impaler or ShadowKnight or AbysmalHorror;
    internal static void AwardRecipe(BaseCreature creature, Mobile player)
    {
        if (!IsBoss(creature) || creature.Map != Map.Malas || creature.Summoned || creature.Controlled || creature.NoKillAwards ||
            player is not PlayerMobile || player.Backpack == null || player.Map != creature.Map || !player.InRange(creature, 24) || Utility.RandomDouble() >= 0.10) { return; }
        var count = DemonKnight.ArtifactRarity10.Length + DemonKnight.ArtifactRarity11.Length;
        var index = Utility.Random(count);
        var recipe = new HavenDoomRecipe { ArtifactType = index < DemonKnight.ArtifactRarity10.Length ? DemonKnight.ArtifactRarity10[index] : DemonKnight.ArtifactRarity11[index - DemonKnight.ArtifactRarity10.Length] };
        player.Backpack.DropItem(recipe);
        HavenMarketProduction.Consign(player, recipe);
        player.SendMessage("You earned a Doom reforging recipe. It upgrades its matching artifact and unlocks equipment evolution.");
    }
    internal static bool Reforged(Item item)
    { foreach (var child in item.Items) { if (child is HavenDoomReforging) { return true; } } return false; }
    internal static void Grow(Item item, int levels, int milestones)
    {
        if (!Reforged(item) || item is not IAosItem gear) { return; }
        gear.Attributes.WeaponDamage += levels;
        gear.Attributes.SpellDamage += levels;
        gear.Attributes.RegenMana += milestones;
        gear.Attributes.RegenHits += milestones;
        if (item is BaseWeapon weapon)
        { weapon.Attributes.WeaponSpeed += milestones * 2; weapon.WeaponAttributes.HitLeechMana += milestones * 3; }
    }
}

[SerializationGenerator(0)]
public partial class HavenDoomReforging : Item
{
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenDoomReforging() : base(1) { Name = "Doom reforging"; Visible = false; Movable = false; Weight = 0; }
}

[SerializationGenerator(0)]
public partial class HavenDoomRecipe : Item
{
    [SerializableField(0)] private Type _artifactType;
    [Constructible]
    public HavenDoomRecipe() : base(0x2831) { Name = "Doom reforging recipe"; Weight = 1; _artifactType = typeof(LegacyOfTheDreadLord); }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Artifact:"} {ArtifactType?.Name ?? "Invalid recipe"}");
        list.Add($"{"Requires:"} {"100 crafting skill, 100 iron ingots, 20 diamonds"}");
        list.Add($"{"Adds:"} {"+10 damage and spell damage, +2 health/mana regeneration; evolves to level 20"}");
    }
    internal bool Upgrade(Mobile from, Item item)
    {
        if (Deleted || from?.Backpack == null || !IsChildOf(from.Backpack) || item?.Deleted != false ||
            !item.IsChildOf(from.Backpack) || item.GetType() != ArtifactType || !HavenDoom.IsArtifact(item) || HavenDoom.Reforged(item) || item is not IAosItem gear) { return false; }
        var skill = item is BaseWeapon or BaseShield ? SkillName.Blacksmith : item is BaseJewel ? SkillName.Tinkering : item is BaseClothing or BaseHat ? SkillName.Tailoring : SkillName.Blacksmith;
        if (from.Skills[skill].Base < 100 || from.Backpack.GetAmount(typeof(IronIngot)) < 100 || from.Backpack.GetAmount(typeof(Diamond)) < 20) { return false; }
        from.Backpack.ConsumeTotal(typeof(IronIngot), 100); from.Backpack.ConsumeTotal(typeof(Diamond), 20);
        item.AddItem(new HavenDoomReforging());
        gear.Attributes.WeaponDamage += 10; gear.Attributes.SpellDamage += 10;
        gear.Attributes.RegenHits += 2; gear.Attributes.RegenMana += 2;
        item.InvalidateProperties(); Delete(); return true;
    }
    public override void OnDoubleClick(Mobile from)
    { if (from.Backpack != null && IsChildOf(from.Backpack)) { from.Target = new ReforgeTarget(this); from.SendMessage("Target the matching Doom artifact in your pack. The recipe, 100 iron ingots, and 20 diamonds are consumed on success."); } }
    private sealed class ReforgeTarget : Target
    {
        private readonly HavenDoomRecipe _recipe;
        public ReforgeTarget(HavenDoomRecipe recipe) : base(-1, false, TargetFlags.None) { _recipe = recipe; }
        protected override void OnTarget(Mobile from, object targeted)
        { from.SendMessage(targeted is Item item && _recipe.Upgrade(from, item) ? "Your artifact has been reforged and can now evolve." : "Check the artifact, crafting skill and materials. Nothing was consumed."); }
    }
}
