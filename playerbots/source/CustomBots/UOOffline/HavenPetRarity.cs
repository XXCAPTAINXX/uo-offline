using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenPetRarity : Item
{
    [SerializableField(0)] private int _tier;
    private DateTime _nextAbility;
    [Constructible]
    public HavenPetRarity() : base(1) { Visible = false; Movable = false; Weight = 0; Name = "pet rarity"; }
    public override bool IsVirtualItem => true;
    public static string RarityName(int tier) => tier switch { 1 => "Rare", 2 => "Epic", 3 => "Legendary", _ => "Common" };
    internal static void Apply(BaseCreature pet, int tier)
    {
        tier = Math.Clamp(tier, 0, 3);
        if (tier == 0 || pet.Backpack?.FindItemByType<HavenPetRarity>() != null) { return; }
        if (pet.Backpack == null) { pet.AddItem(new Backpack()); }
        pet.Backpack.DropItem(new HavenPetRarity { Tier = tier });
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
    }
    public static void OnAttack(BaseCreature pet, Mobile defender)
    {
        var rarity = pet.Backpack?.FindItemByType<HavenPetRarity>();
        if (rarity != null) { HavenRarePetAbility.Activate(pet, defender, rarity.Tier, ref rarity._nextAbility); }
    }
}
