using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenLegendaryPetSkills : Item
{
    [SerializableField(0)] private List<SkillName> _boosted = new();
    [Constructible]
    public HavenLegendaryPetSkills() : base(1)
    { Visible = false; Movable = false; Weight = 0; Name = "legendary pet skill roll"; }
    public override bool IsVirtualItem => true;

    internal static HavenLegendaryPetSkills Roll(BaseCreature pet, double? chance = null, int? count = null, int? value = null)
    {
        if (pet?.Deleted != false || !HavenTamingMissions.IsCustomPet(pet) ||
            pet.Backpack?.FindItemByType<HavenPetRarity>()?.Tier != 3) { return null; }
        var existing = pet.Backpack.FindItemByType<HavenLegendaryPetSkills>();
        if (existing != null) { return existing; }
        var record = new HavenLegendaryPetSkills();
        pet.Backpack.DropItem(record); // Save unsuccessful rolls too; this is a once-per-pet opportunity.
        if ((chance ?? Utility.RandomDouble()) >= 0.50) { return record; }
        var eligible = new List<SkillName>();
        foreach (var skill in HavenPetTraining.TrainableSkills)
        { if (pet.Skills[skill].Base > 0) { eligible.Add(skill); } }
        var picks = Math.Min(eligible.Count, Math.Clamp(count ?? Utility.RandomMinMax(1, 3), 1, 3));
        for (var i = 0; i < picks; i++)
        {
            var index = Utility.Random(eligible.Count);
            var name = eligible[index]; eligible.RemoveAt(index);
            var skill = pet.Skills[name];
            var rolled = Math.Clamp(value ?? Utility.RandomMinMax(125, 150), 125, 150);
            skill.Cap = Math.Max(skill.Cap, rolled);
            skill.Base = Math.Max(skill.Base, rolled);
            record.Boosted.Add(name);
        }
        record.MarkDirty(); pet.InvalidateProperties();
        return record;
    }
    internal void AddProperties(BaseCreature pet, IPropertyList list)
    {
        if (Boosted.Count == 0) { list.Add($"{"Legendary skill roll:"} {"No over-cap skills rolled"}"); }
        foreach (var name in Boosted)
        {
            var skill = pet.Skills[name];
            list.Add($"{"Legendary skill:"} {skill.Name} {skill.Base:F1}{" / "}{skill.Cap:F1}");
        }
    }
}
