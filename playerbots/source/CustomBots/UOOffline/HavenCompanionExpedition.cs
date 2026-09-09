using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Gumps;
using Server.Mobiles;

namespace Server.UOOffline;

public enum HavenExpeditionKind { Grind, Ore, Wood, Leather, Reagents, TamePackHorse, TameHorse, TameOstard, TameBeetle, TameDragon, TameWhiteWyrm, TameEmberwing, TameMoonfang, TameStormscale, TameFrostmane, TameVerdantLlama, TameStormhorn }

[SerializationGenerator(0)]
public partial class HavenCompanionExpedition : Item
{
    [SerializableField(0)] private HavenCompanion _companion;
    [SerializableField(1)] private DateTime _started;
    [SerializableField(2)] private HavenExpeditionKind _kind;
    [SerializableField(3)] private bool _claimed;
    private Timer _timer;
    public override bool IsVirtualItem => true;
    public DateTime Due => Started + TimeSpan.FromMinutes(5);
    public string Status => Core.Now >= Due ? "Ready to return" : $"{Kind}: {Math.Ceiling((Due - Core.Now).TotalMinutes)} min left";
    [Constructible]
    public HavenCompanionExpedition() : base(1) { Visible = false; Movable = false; Weight = 0; Name = "companion expedition"; }

    internal static bool Start(HavenCompanion companion, Mobile owner, HavenExpeditionKind kind)
    {
        if (companion?.Deleted != false || owner?.Deleted != false || companion.BoundOwner != owner || companion.IsDeadPet ||
            !owner.Alive || companion.IsStabled || companion.Expedition != null || !Enum.IsDefined(kind) || companion.Backpack == null ||
            owner.Map == null || owner.Map == Map.Internal || companion.Map != owner.Map || !owner.InRange(companion, 18)) { return false; }
        if (HavenTamingMissions.IsTaming(kind) && !HavenTamingMissions.CanStart(companion, kind))
        {
            owner.SendMessage($"This mission requires {HavenTamingMissions.Requirement(kind):F1} base Animal Taming AND Animal Lore on your companion.");
            return false;
        }
        var trip = new HavenCompanionExpedition { Companion = companion, Started = Core.Now, Kind = kind };
        companion.Backpack.DropItem(trip);
        companion.PrepareForExpedition();
        companion.Internalize();
        trip.Schedule();
        owner.CloseGump<HavenCompanionGump>();
        owner.CloseGump<HavenCompanionAfkGump>();
        owner.SendMessage("Your companion left on a five-minute expedition. Use [companion and Tasks to check progress or return early.");
        return true;
    }

    [AfterDeserialization]
    private void Schedule()
    {
        _timer?.Stop();
        if (Deleted) { return; }
        if (Claimed) { Delete(); return; }
        var delay = Due - Core.Now;
        _timer = Timer.DelayCall(delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1), CheckReturn);
    }
    private void CheckReturn()
    {
        if (Companion?.Deleted != false || Companion.BoundOwner?.Deleted != false) { Delete(); return; }
        var now = Core.Now;
        if (now < Due)
        {
            Schedule();
            return;
        }
        var owner = Companion.BoundOwner;
        if (owner.NetState != null && Return(owner, now, automatic: true)) { return; }
        _timer = Timer.DelayCall(TimeSpan.FromSeconds(30), CheckReturn);
    }
    internal bool Return(Mobile owner, DateTime now, double? tamingRoll = null, bool automatic = false)
    {
        if (Deleted || Claimed || Companion?.Deleted != false || owner?.Deleted != false || Companion.BoundOwner != owner ||
            owner.Map == null || owner.Map == Map.Internal) { return false; }
        // Timer-wheel callbacks can arrive before the wall-clock deadline. Never
        // turn an automatic completion into an early return with no pet reward.
        if (automatic && now < Due)
        {
            return false;
        }
        var minutes = Math.Clamp((int)(now - Started).TotalMinutes, 0, 5);
        var companion = Companion;
        var idleMissions = companion.Backpack.FindItemByType<HavenCompanionIdleMissions>();
        var idleTrip = idleMissions?.Owns(this) == true;
        Claimed = true;
        _timer?.Stop();
        companion.AwardExpeditionProgress(minutes, now);
        companion.MoveToWorld(owner.Location, owner.Map);
        companion.ControlTarget = owner; companion.ControlOrder = OrderType.Follow;
        if (minutes > 0)
        {
            var loot = CreateLoot(Kind, minutes, owner, tamingRoll);
            if (idleTrip) { companion.Backpack.DropItem(loot); owner.SendMessage("Idle expedition rewards are in your companion's pack."); }
            else if (companion.Backpack.TryDropItem(companion, loot, false)) { owner.SendMessage("Expedition loot is in your companion's pack."); }
            else if (owner.Backpack?.TryDropItem(owner, loot, false) == true) { owner.SendMessage("The shared pack is full; expedition loot is in your backpack."); }
            else { loot.MoveToWorld(owner.Location, owner.Map); owner.SendMessage("Both packs are full; expedition supplies are at your feet."); }
            owner.SendMessage($"Your companion returned with {Kind} rewards, skill training, +{minutes} Str/Dex/Int and {minutes * 10} gear experience.");
        }
        else { owner.SendMessage("Your companion returned. Expeditions earn rewards for each full minute away."); }
        var stillIdle = idleMissions?.Finished(this, now) == true;
        Delete();
        if (!stillIdle && owner.NetState != null) { HavenCompanionGump.DisplayTo(owner, companion); }
        return true;
    }
    internal static Bag CreateLoot(HavenExpeditionKind kind, int minutes, Mobile owner = null, double? tamingRoll = null)
    {
        var bag = new Bag { Name = $"{kind} expedition supplies" };
        if (HavenTamingMissions.IsTaming(kind))
        {
            if (minutes < 5) { owner?.SendMessage("Taming missions need the full five minutes to return a pet."); }
            else
            {
                var rarity = HavenTamingMissions.IsCustomMission(kind) ? HavenTamingMissions.RollRarity(tamingRoll ?? Utility.RandomDouble()) : 0;
                bag.DropItem(new HavenExpeditionPetClaim { Owner = owner, Kind = kind, Rarity = rarity });
                var bonus = TamingBonus(Utility.RandomDouble());
                if (bonus != null) { bag.DropItem(bonus); owner?.SendMessage("Your companion also found a useful taming supply!"); }
                owner?.SendMessage($"Your companion found a {HavenTamingMissions.PetName(kind)}! The claim in the shared pack shows its details.");
            }
            return bag;
        }
        switch (kind)
        {
            case HavenExpeditionKind.Ore: bag.DropItem(Deed(new IronIngot(minutes * 20))); break;
            case HavenExpeditionKind.Wood: bag.DropItem(Deed(new Log(minutes * 40))); break;
            case HavenExpeditionKind.Leather: bag.DropItem(Deed(new Leather(minutes * 20))); break;
            case HavenExpeditionKind.Reagents:
                var reagents = new BagOfReagents(minutes * 10);
                foreach (var item in reagents.Items.ToArray()) { bag.DropItem(Deed(item)); }
                reagents.Delete(); break;
            default:
                bag.DropItem(new Gold(minutes * Utility.RandomMinMax(200, 300)));
                bag.DropItem(new HavenMark(minutes));
                if (minutes >= 3) { bag.DropItem(OldHavenWarden.CreateBossGear(0)); }
                break;
        }
        return bag;
    }
    private static CommodityDeed Deed(Item resource)
    {
        var deed = new CommodityDeed();
        if (!deed.SetCommodity(resource))
        { deed.Delete(); resource.Delete(); throw new InvalidOperationException("Mission resource must be deedable."); }
        return deed;
    }
    internal static Item TamingBonus(double roll) => roll switch
    {
        < 0.08 => new HavenBondingPotion(),
        < 0.16 => new HavenPetLeash(),
        < 0.25 => new HavenPetPowerScroll(Utility.RandomList(SkillName.Wrestling, SkillName.Tactics, SkillName.Anatomy, SkillName.Healing, SkillName.MagicResist), 105),
        _ => null
    };
    public override void OnDelete() { _timer?.Stop(); _timer = null; Companion = null; base.OnDelete(); }
}

public partial class HavenCompanion
{
    internal HavenCompanionExpedition Expedition => Backpack?.FindItemByType<HavenCompanionExpedition>();
    internal void PrepareForExpedition()
    {
        StopTamingAssist(); ClearSongs(); CancelCompanionSpell();
        Combatant = null; ControlTarget = null; ControlOrder = OrderType.Stay;
    }
    internal void AwardExpeditionProgress(int minutes, DateTime now)
    {
        UpdateTraining(now);
        if (minutes <= 0) { return; }
        TrainingMinutes += minutes * 4;
        ApplyGrowth();
        RawStr = (int)Math.Min(100000000L, (long)RawStr + minutes);
        RawDex = (int)Math.Min(100000000L, (long)RawDex + minutes);
        RawInt = (int)Math.Min(100000000L, (long)RawInt + minutes);
        HavenGearExperience.GainEquipped(this, minutes * 10);
    }
}
