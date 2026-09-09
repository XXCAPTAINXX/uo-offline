using System;
using ModernUO.Serialization;
using Server.Mobiles;
using Server.SkillHandlers;

namespace Server.UOOffline;

public static class HavenTamingMissions
{
    public static bool IsTaming(HavenExpeditionKind kind) => kind is >= HavenExpeditionKind.TamePackHorse and <= HavenExpeditionKind.TameStormhorn;
    internal static int RollRarity(double roll) => roll < 0.40 ? 0 : roll < 0.75 ? 1 : roll < 0.95 ? 2 : 3;
    public static bool IsCustomMission(HavenExpeditionKind kind) => kind is >= HavenExpeditionKind.TameEmberwing and <= HavenExpeditionKind.TameStormhorn;
    public static bool IsCustomPet(BaseCreature pet) => pet is HavenEmberwing or HavenMoonfang or HavenStormscale or HavenFrostmane or HavenVerdantLlama or HavenStormhorn or VampiricSteed;
    public static string PetName(HavenExpeditionKind kind) => kind switch
    {
        HavenExpeditionKind.TamePackHorse => "Pack horse",
        HavenExpeditionKind.TameHorse => "Horse",
        HavenExpeditionKind.TameOstard => "Forest ostard",
        HavenExpeditionKind.TameBeetle => "Giant beetle",
        HavenExpeditionKind.TameDragon => "Dragon",
        HavenExpeditionKind.TameWhiteWyrm => "White wyrm",
        HavenExpeditionKind.TameEmberwing => "Emberwing ostard",
        HavenExpeditionKind.TameMoonfang => "Moonfang wolf",
        HavenExpeditionKind.TameStormscale => "Stormscale drake",
        HavenExpeditionKind.TameFrostmane => "Frostmane steed",
        HavenExpeditionKind.TameVerdantLlama => "Verdant llama",
        HavenExpeditionKind.TameStormhorn => "Stormhorn kirin",
        _ => "Unknown pet"
    };
    public static double Requirement(HavenExpeditionKind kind) => kind switch
    {
        HavenExpeditionKind.TamePackHorse => 11.1,
        HavenExpeditionKind.TameHorse or HavenExpeditionKind.TameOstard or HavenExpeditionKind.TameBeetle => 29.1,
        HavenExpeditionKind.TameDragon => 93.9,
        HavenExpeditionKind.TameWhiteWyrm => 96.3,
        HavenExpeditionKind.TameEmberwing => 65,
        HavenExpeditionKind.TameMoonfang => 95,
        HavenExpeditionKind.TameStormscale => 110,
        HavenExpeditionKind.TameFrostmane => 70,
        HavenExpeditionKind.TameVerdantLlama => 80,
        HavenExpeditionKind.TameStormhorn => 105,
        _ => double.MaxValue
    };
    internal static bool CanStart(HavenCompanion companion, HavenExpeditionKind kind) =>
        IsTaming(kind) && companion.Skills.AnimalTaming.Base >= Requirement(kind) && companion.Skills.AnimalLore.Base >= Requirement(kind);
    internal static BaseCreature CreatePet(HavenExpeditionKind kind) => kind switch
    {
        HavenExpeditionKind.TamePackHorse => new PackHorse(),
        HavenExpeditionKind.TameHorse => new Horse(),
        HavenExpeditionKind.TameOstard => new ForestOstard(),
        HavenExpeditionKind.TameBeetle => new Beetle(),
        HavenExpeditionKind.TameDragon => new Dragon(),
        HavenExpeditionKind.TameWhiteWyrm => new WhiteWyrm(),
        HavenExpeditionKind.TameEmberwing => new HavenEmberwing(),
        HavenExpeditionKind.TameMoonfang => new HavenMoonfang(),
        HavenExpeditionKind.TameStormscale => new HavenStormscale(),
        HavenExpeditionKind.TameFrostmane => new HavenFrostmane(),
        HavenExpeditionKind.TameVerdantLlama => new HavenVerdantLlama(),
        HavenExpeditionKind.TameStormhorn => new HavenStormhorn(),
        _ => null
    };
}

[SerializationGenerator(2)]
public partial class HavenExpeditionPetClaim : Item
{
    [SerializableField(0)] private Mobile _owner;
    [SerializableField(1)] private HavenExpeditionKind _kind;
    [SerializableField(2)] private int _rarity;
    [SerializableField(3)] private BaseCreature _reservedPet;
    private void MigrateFrom(V1Content content) { _owner = content.Owner; _kind = content.Kind; _rarity = content.Rarity; }
    private void MigrateFrom(V0Content content) { _owner = content.Owner; _kind = content.Kind; }
    [Constructible]
    public HavenExpeditionPetClaim() : base(0x14F0) { Weight = 1; LootType = LootType.Blessed; Hue = 0x59B; }
    public override string DefaultName => HavenTamingMissions.IsCustomMission(Kind)
        ? $"{HavenPetRarity.RarityName(Rarity)} pet claim: {HavenTamingMissions.PetName(Kind)}"
        : $"Pet claim: {HavenTamingMissions.PetName(Kind)}";
    public override void OnDoubleClick(Mobile from) => Claim(from);
    internal BaseCreature Inspect(Mobile from)
    {
        if (Deleted || from?.Deleted != false || from != Owner || !from.Alive ||
            from.Backpack == null || !IsChildOf(from.Backpack) || !HavenTamingMissions.IsTaming(Kind)) { return null; }
        if (_reservedPet?.Deleted != false)
        {
            ReservedPet = HavenTamingMissions.CreatePet(Kind);
            AnimalTaming.ScaleSkills(_reservedPet, _reservedPet is GreaterDragon ? 0.72 : 0.90);
            if (_reservedPet.StatLossAfterTame) { AnimalTaming.ScaleStats(_reservedPet, 0.50); }
            HavenPetRarity.Apply(_reservedPet, Rarity);
            _reservedPet.Internalize();
        }
        if (Rarity == 3 && HavenTamingMissions.IsCustomPet(_reservedPet)) { _reservedPet.ControlSlots = 1; }
        HavenLegendaryPetSkills.Roll(_reservedPet);
        return _reservedPet;
    }
    public void InspectWithAnimalLore(Mobile from)
    {
        var pet = Inspect(from);
        if (pet == null) { from.SendMessage("Keep your own pet ticket in your backpack to inspect it."); return; }
        AnimalLoreGump.DisplayTo(from, pet);
        from.SendMessage($"This ticket holds {pet.Name}: {pet.ControlSlots} follower slots. These are its actual stats after taming.");
        if (HavenTamingMissions.IsCustomMission(Kind)) { from.SendMessage(HavenPetRarity.Describe(Rarity)); }
    }
    internal bool Claim(Mobile from)
    {
        if (Deleted || from?.Deleted != false || from != Owner || !from.Alive || from.Backpack == null ||
            !IsChildOf(from.Backpack) || from.Map == null || from.Map == Map.Internal || !HavenTamingMissions.IsTaming(Kind)) { return false; }
        var pet = Inspect(from);
        if (pet == null) { return false; }
        if (from.Followers + pet.ControlSlots > from.FollowersMax || !pet.SetControlMaster(from))
        {
            from.SendMessage("Free enough follower slots before claiming this pet. Your claim is preserved.");
            return false;
        }
        ReservedPet = null;
        pet.Owners.Add(from);
        pet.Loyalty = BaseCreature.MaxLoyalty;
        pet.BondingBegin = Core.Now - pet.BondingDelay - TimeSpan.FromSeconds(1);
        pet.ControlTarget = from;
        pet.ControlOrder = OrderType.Follow;
        pet.MoveToWorld(from.Location, from.Map);
        from.SendMessage("Your companion's pet has joined you. Feed it suitable food to bond immediately once you meet its normal taming requirement; the waiting period is already complete.");
        Delete();
        return true;
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Use Animal Lore on this ticket to inspect the pet before claiming."}");
        list.Add($"{"Double-click in your backpack to claim. Owner:"} {Owner?.Name ?? "none"}");
        if (HavenTamingMissions.IsCustomMission(Kind)) { list.Add($"{"Rarity benefits:"} {HavenPetRarity.Describe(Rarity)}"); }
    }
    public override void OnDelete() { _reservedPet?.Delete(); ReservedPet = null; Owner = null; base.OnDelete(); }
}
