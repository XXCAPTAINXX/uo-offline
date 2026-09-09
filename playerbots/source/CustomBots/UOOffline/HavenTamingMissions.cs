using ModernUO.Serialization;
using Server.Mobiles;
using Server.SkillHandlers;

namespace Server.UOOffline;

public static class HavenTamingMissions
{
    public static bool IsTaming(HavenExpeditionKind kind) => kind is >= HavenExpeditionKind.TamePackHorse and <= HavenExpeditionKind.TameStormscale;
    internal static int RollRarity(double roll) => roll < 0.70 ? 0 : roll < 0.92 ? 1 : roll < 0.99 ? 2 : 3;
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
        _ => null
    };
}

[SerializationGenerator(1)]
public partial class HavenExpeditionPetClaim : Item
{
    [SerializableField(0)] private Mobile _owner;
    [SerializableField(1)] private HavenExpeditionKind _kind;
    [SerializableField(2)] private int _rarity;
    private void MigrateFrom(V0Content content) { _owner = content.Owner; _kind = content.Kind; }
    [Constructible]
    public HavenExpeditionPetClaim() : base(0x14F0) { Weight = 1; LootType = LootType.Blessed; Hue = 0x59B; }
    public override string DefaultName => $"{HavenPetRarity.RarityName(Rarity)} pet claim: {HavenTamingMissions.PetName(Kind)}";
    public override void OnDoubleClick(Mobile from) => Claim(from);
    internal bool Claim(Mobile from)
    {
        if (Deleted || from?.Deleted != false || from != Owner || !from.Alive || from.Backpack == null ||
            !IsChildOf(from.Backpack) || from.Map == null || from.Map == Map.Internal || !HavenTamingMissions.IsTaming(Kind)) { return false; }
        var pet = HavenTamingMissions.CreatePet(Kind);
        if (from.Followers + pet.ControlSlots > from.FollowersMax || !pet.SetControlMaster(from))
        {
            pet.Delete();
            from.SendMessage("Free enough follower slots before claiming this pet. Your claim is preserved.");
            return false;
        }
        AnimalTaming.ScaleSkills(pet, pet is GreaterDragon ? 0.72 : 0.90);
        if (pet.StatLossAfterTame) { AnimalTaming.ScaleStats(pet, 0.50); }
        HavenPetRarity.Apply(pet, Rarity);
        pet.Owners.Add(from);
        pet.Loyalty = BaseCreature.MaxLoyalty;
        pet.ControlTarget = from;
        pet.ControlOrder = OrderType.Follow;
        pet.MoveToWorld(from.Location, from.Map);
        from.SendMessage("Your companion's pet has joined you. Your own skills govern its obedience; bonding works normally.");
        Delete();
        return true;
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Double-click in your backpack to claim. Owner:"} {Owner?.Name ?? "none"}");
    }
    public override void OnDelete() { Owner = null; base.OnDelete(); }
}
