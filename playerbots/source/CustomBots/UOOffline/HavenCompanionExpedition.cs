using System;
using ModernUO.Serialization;
using Server.Items;
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
        var owner = Companion.BoundOwner;
        if (owner.NetState != null && Return(owner, Core.Now)) { return; }
        _timer = Timer.DelayCall(TimeSpan.FromSeconds(30), CheckReturn);
    }
    internal bool Return(Mobile owner, DateTime now, double? tamingRoll = null)
    {
        if (Deleted || Claimed || Companion?.Deleted != false || owner?.Deleted != false || Companion.BoundOwner != owner ||
            owner.Map == null || owner.Map == Map.Internal) { return false; }
        var minutes = Math.Clamp((int)(now - Started).TotalMinutes, 0, 5);
        var companion = Companion;
        Claimed = true;
        _timer?.Stop();
        companion.AwardExpeditionProgress(minutes, now);
        companion.MoveToWorld(owner.Location, owner.Map);
        companion.ControlTarget = owner; companion.ControlOrder = OrderType.Follow;
        if (minutes > 0)
        {
            var loot = CreateLoot(Kind, minutes, owner, tamingRoll);
            if (companion.Backpack.TryDropItem(companion, loot, false)) { owner.SendMessage("Expedition loot is in your companion's pack."); }
            else if (owner.Backpack?.TryDropItem(owner, loot, false) == true) { owner.SendMessage("The shared pack is full; expedition loot is in your backpack."); }
            else { loot.MoveToWorld(owner.Location, owner.Map); owner.SendMessage("Both packs are full; expedition supplies are at your feet."); }
            owner.SendMessage($"Your companion returned with {Kind} rewards, skill training, +{minutes} Str/Dex/Int and {minutes * 10} gear experience.");
        }
        else { owner.SendMessage("Your companion returned. Expeditions earn rewards for each full minute away."); }
        Delete();
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
                var rarity = HavenTamingMissions.RollRarity(tamingRoll ?? Utility.RandomDouble());
                bag.DropItem(new HavenExpeditionPetClaim { Owner = owner, Kind = kind, Rarity = rarity });
                owner?.SendMessage($"Your companion found a {HavenPetRarity.RarityName(rarity)} {HavenTamingMissions.PetName(kind)}! Move the claim to your backpack to redeem it.");
            }
            return bag;
        }
        switch (kind)
        {
            case HavenExpeditionKind.Ore: bag.DropItem(new IronOre(minutes * 20)); break;
            case HavenExpeditionKind.Wood: bag.DropItem(new Log(minutes * 40)); break;
            case HavenExpeditionKind.Leather: bag.DropItem(new Leather(minutes * 20)); break;
            case HavenExpeditionKind.Reagents: bag.DropItem(new BagOfReagents(minutes * 10)); break;
            default:
                bag.DropItem(new Gold(minutes * Utility.RandomMinMax(200, 300)));
                bag.DropItem(new HavenMark(minutes));
                if (minutes >= 3) { bag.DropItem(OldHavenWarden.CreateBossGear(0)); }
                break;
        }
        return bag;
    }
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
