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
            progress.AppliedLevel = progress.Level;
        }
        gear.InvalidateProperties();
    }
    public static void AddProperties(Item gear, IPropertyList list)
    {
        var progress = Find(gear);
        if (progress == null) { return; }
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
            else if (item is ApprenticeGrimoire grimoire) { grimoire.GainCastExperience(owner); }
            else if (item is BaseWeapon or BaseArmor or BaseClothing or BaseJewel or Spellbook) { Gain(item, amount); }
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
