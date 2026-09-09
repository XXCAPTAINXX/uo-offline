using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenGearExperience : Item
{
    [SerializableField(0)] private int _experience;
    [SerializableField(1)] private int _appliedLevel = 1;
    private Timer _cleanup;
    internal static bool IsSpecial(Item gear) => HavenQuestGear.IsReward(gear) || gear is IAosItem && gear.RootParent is HavenCompanion ||
        gear is IStarterUpgradeable or IEvolvingStarterWeapon or ApprenticeGrimoire or
        HavenLevelingCape or HavenStarterSash or HavenChampionPendant or HavenSetRing or HavenConcordTalisman or
        HavenCompanionBlade or HavenCompanionBow or StarterFortuneEarrings or AstralFortuneEarrings or AstralWeaversRing or AstralGuardianMantle || HavenJewelrySets.BraceletTheme(gear) >= 0 ||
        gear is Longsword { Parent: HavenCompanion, Movable: false } && gear.GetType() == typeof(Longsword);
    [AfterDeserialization]
    private void CheckLegacyGear() => _cleanup = Timer.DelayCall(TimeSpan.FromSeconds(1), RemoveOrdinaryBonuses);
    internal void RemoveOrdinaryBonuses()
    {
        if (Parent is not Item gear || IsSpecial(gear)) { return; }
        if (gear is IAosItem aos)
        {
            aos.Attributes.Luck = Math.Max(0, aos.Attributes.Luck - Math.Max(0, AppliedLevel - 1) * 5);
            var milestones = AppliedLevel / 5;
            aos.Attributes.BonusStr = Math.Max(0, aos.Attributes.BonusStr - milestones);
            aos.Attributes.BonusDex = Math.Max(0, aos.Attributes.BonusDex - milestones);
            aos.Attributes.BonusInt = Math.Max(0, aos.Attributes.BonusInt - milestones);
        }
        Delete(); gear.InvalidateProperties();
    }
    public override void OnDelete() { _cleanup?.Stop(); _cleanup = null; base.OnDelete(); }
    public override bool IsVirtualItem => true;
    public int Level => Math.Min(20, 1 + Experience / 100);
    [Constructible]
    public HavenGearExperience() : base(1) { Name = "equipment progression"; Visible = false; Movable = false; Weight = 0; }
    internal static HavenGearExperience Find(Item gear)
    {
        foreach (var child in gear.Items) { if (child is HavenGearExperience progress) { return progress; } }
        return null;
    }
    internal static void Gain(Item gear, int amount)
    {
        if (gear.Deleted || gear is not IAosItem aos || amount <= 0) { return; }
        if (!IsSpecial(gear)) { Find(gear)?.RemoveOrdinaryBonuses(); return; }
        var progress = Find(gear);
        if (progress == null) { progress = new HavenGearExperience(); gear.AddItem(progress); }
        progress.Experience = Math.Min(1900, progress.Experience + amount);
        var levels = progress.Level - progress.AppliedLevel;
        if (levels > 0)
        {
            if (gear is not NewHavenAdventurersRobe) { aos.Attributes.Luck += levels * 5; }
            var milestones = progress.Level / 5 - progress.AppliedLevel / 5;
            aos.Attributes.BonusStr += milestones;
            aos.Attributes.BonusDex += milestones;
            aos.Attributes.BonusInt += milestones;
            HavenQuestGear.Grow(gear, levels, milestones);
            progress.AppliedLevel = progress.Level;
        }
        HavenAstralGrowth.Apply(gear, progress.Level);
        if (gear is HavenConcordTalisman talisman && gear.Parent is Mobile wearer) { talisman.UnlockFollower(wearer); }
        gear.InvalidateProperties();
    }
    public static void AddProperties(Item gear, IPropertyList list)
    {
        if (!IsSpecial(gear)) { return; }
        HavenCompanionGearGrowth.AddProperties(gear, list);
        var progress = Find(gear);
        if (gear is BaseWeapon evolved && HavenCompanionWeaponEvolution.Level(evolved) >= 20)
        { list.Add($"{"Companion unlock:"} {"all-monster slayer while wielded by your companion"}"); }
        if (progress == null) { list.Add($"{"Gear level:"} {1}/20"); return; }
        list.Add($"{"Gear level:"} {progress.Level}/20");
        list.Add($"{"Shared experience:"} {progress.Experience:N0} / 1,900");
    }
    internal static void GainEquipped(Mobile owner, int amount)
    {
        foreach (var item in owner.Items)
        {
            if (item is HavenLevelingCape cape) { cape.GainExperience(owner, amount); }
            else if (item is IEvolvingStarterWeapon weapon && item is BaseWeapon equipment)
            {
                StarterWeaponProgression.GainSharedExperience(weapon, equipment, owner, amount);
            }
            else if (item is ApprenticeGrimoire grimoire) { grimoire.GainSharedExperience(owner, amount); }
            else if (item is BaseWeapon or BaseArmor or BaseClothing or BaseJewel or Spellbook or BaseTalisman) { Gain(item, amount); }
            if (owner is HavenCompanion && item is BaseWeapon evolving) { HavenCompanionWeaponEvolution.Apply(evolving); }
            if (owner is HavenCompanion && item is Spellbook book) { HavenCompanionGearGrowth.ApplySpellbook(book); }
        }
    }
    public static void OnMonsterKilled(BaseCreature creature, Mobile player)
    {
        if (!HavenAstralRewards.Eligible(creature, player)) { return; }
        var amount = Math.Clamp(creature.HitsMax / 100, 1, 20);
        GainEquipped(player, amount);
        foreach (var companion in player.Map.GetMobilesInRange<HavenCompanion>(player.Location, 18))
        {
            if (companion.BoundOwner == player && !companion.IsDeadPet) { GainEquipped(companion, amount); }
        }
    }
}
