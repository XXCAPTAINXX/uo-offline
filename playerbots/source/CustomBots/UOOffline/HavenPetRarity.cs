using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenPetRarity : Item
{
    [SerializableField(0)] private int _tier;
    [Constructible]
    public HavenPetRarity() : base(1) { Visible = false; Movable = false; Weight = 0; Name = "pet rarity"; }
    public override bool IsVirtualItem => true;
    public static string RarityName(int tier) => tier switch { 1 => "Rare", 2 => "Epic", 3 => "Legendary", _ => "Common" };
    public static string Describe(int tier) => tier switch
    {
        1 => "+10% starting stats/HP; +5 combat skills, minimum 105 caps; strengthened species signature, no training cost",
        2 => "+20% starting stats/HP; +10 combat skills, minimum 110 caps; stronger signature and species-specific secondary effects, no training cost",
        3 => "Found with 1 follower slot; +30% starting stats/HP; +15 combat skills, minimum 120 caps; 50% chance of 1-3 existing skills at 125-150 with matching caps; strongest species signature, no training cost",
        _ => "Standard species stats and abilities"
    };
    public static void AddProperties(BaseCreature pet, IPropertyList list)
    {
        HavenPetSignatures.AddProperties(pet,list);
        var rarity = pet.Backpack?.FindItemByType<HavenPetRarity>();
        if (rarity != null && HavenTamingMissions.IsCustomPet(pet)) { list.Add($"{RarityName(rarity.Tier)} {"rarity:"} {Describe(rarity.Tier)}"); }
        pet.Backpack?.FindItemByType<HavenLegendaryPetSkills>()?.AddProperties(pet, list);
    }
    internal static void Apply(BaseCreature pet, int tier)
    {
        if (!HavenTamingMissions.IsCustomPet(pet)) { return; }
        tier = Math.Clamp(tier, 0, 3);
        if (tier == 0 || pet.Backpack?.FindItemByType<HavenPetRarity>() != null) { HavenPetAppearance.Refresh(pet); return; }
        if (pet.Backpack == null) { pet.AddItem(new Backpack()); }
        pet.Backpack.DropItem(new HavenPetRarity { Tier = tier });
        if (tier == 3 && !pet.Controlled) { pet.ControlSlots = 1; }
        pet.Name = $"{RarityName(tier)} {pet.Name}";
        pet.RawStr += pet.RawStr * tier / 10;
        pet.RawDex += pet.RawDex * tier / 10;
        pet.RawInt += pet.RawInt * tier / 10;
        pet.SetHits(pet.HitsMax + pet.HitsMax * tier / 10);
        foreach (var skill in new[] { SkillName.Wrestling, SkillName.Tactics, SkillName.MagicResist, SkillName.Magery, SkillName.EvalInt })
        {
            if (pet.Skills[skill].Base <= 0) { continue; }
            pet.Skills[skill].Cap = Math.Max(pet.Skills[skill].Cap, tier == 3 ? 120 : 100 + tier * 5);
            pet.Skills[skill].Base = Math.Min(pet.Skills[skill].Cap, pet.Skills[skill].Base + tier * 5);
        }
        if (tier == 3) { HavenLegendaryPetSkills.Roll(pet); }
        HavenPetAppearance.Refresh(pet);
    }
    public static void OnAttack(BaseCreature pet, Mobile defender)
    {
        if (!HavenTamingMissions.IsCustomPet(pet)) { return; }
        var rarity = pet.Backpack?.FindItemByType<HavenPetRarity>();
        if (rarity?.Tier == 3 && pet.Controlled) { HavenLegendaryPetSkills.Roll(pet); }
        HavenPetSignatures.OnAttack(pet,defender);
    }
}
