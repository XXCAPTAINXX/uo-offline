using System;
using ModernUO.Serialization;
using Server.Items;
using Server.Gumps;
using Server.Mobiles;

namespace Server.UOOffline;

public enum HavenExpeditionKind { Grind, Ore, Wood, Leather, Reagents, TamePackHorse, TameHorse, TameOstard, TameBeetle, TameDragon, TameWhiteWyrm, TameEmberwing, TameMoonfang, TameStormscale, TameFrostmane, TameVerdantLlama, TameStormhorn, MalasReagents, DoomBones, AbyssEssences, AbyssIngredients }

[SerializationGenerator(0)]
public partial class HavenCompanionExpedition : Item
{
    [SerializableField(0)] private HavenCompanion _companion;
    [SerializableField(1)] private DateTime _started;
    [SerializableField(2)] private HavenExpeditionKind _kind;
    [SerializableField(3)] private bool _claimed;
    private Timer _timer;
    public override bool IsVirtualItem => true;
    public int DurationMinutes
    {
        get
        {
            foreach(var item in Items)
            { if(item is HavenMissionContract contract && HavenMissionDuration.Valid(contract.Minutes)) { return contract.Minutes; } }
            return 5;
        }
    }
    public DateTime Due => Started + TimeSpan.FromMinutes(DurationMinutes);
    public string Status => Core.Now >= Due ? "Ready to return" : $"{Kind}: {Math.Ceiling((Due - Core.Now).TotalMinutes)} min left";
    [Constructible]
    public HavenCompanionExpedition() : base(1) { Visible = false; Movable = false; Weight = 0; Name = "companion expedition"; }

    internal static bool Start(HavenCompanion companion, Mobile owner, HavenExpeditionKind kind, int duration = 0)
    {
        if(duration==0) { duration=HavenMissionDuration.Selected(companion); }
        if(!HavenMissionDuration.Valid(duration)) { return false; }
        if (companion?.Deleted != false || owner?.Deleted != false || companion.BoundOwner != owner || companion.IsDeadPet ||
            !owner.Alive || companion.IsStabled || companion.Expedition != null || !Enum.IsDefined(kind) || companion.Backpack == null ||
            owner.Map == null || owner.Map == Map.Internal || companion.Map != owner.Map || !owner.InRange(companion, 18)) { return false; }
        if(!HavenRegionalMissions.CanStart(companion,kind))
        { owner.SendMessage($"This route needs {HavenRegionalMissions.Requirement(kind)} combat AND magic resistance skill.");return false; }
        if (HavenTamingMissions.IsTaming(kind) && !HavenTamingMissions.CanStart(companion, kind))
        {
            owner.SendMessage($"This mission requires {HavenTamingMissions.Requirement(kind):F1} base Animal Taming AND Animal Lore on your companion.");
            return false;
        }
        HavenMissionJournal.Begin(companion,Core.Now);
        var trip = new HavenCompanionExpedition { Companion = companion, Started = Core.Now, Kind = kind };
        trip.AddItem(new HavenMissionContract { Minutes=duration });
        HavenMissionDuration.Select(companion,duration);
        companion.Backpack.DropItem(trip);
        companion.PrepareForExpedition();
        companion.Internalize();
        trip.Schedule();
        owner.CloseGump<HavenCompanionGump>();
        owner.CloseGump<HavenCompanionAfkGump>();
        owner.CloseGump<HavenMissionDurationGump>();
        owner.SendMessage($"Your companion left on a {duration}-minute expedition. Use [companion and Tasks to check progress or return early.");
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
    internal bool Return(Mobile owner, DateTime now, double? tamingRoll = null, bool automatic = false, bool offline = false)
    {
        if(offline && (owner?.NetState!=null || Companion?.Backpack?.FindItemByType<HavenCompanionGearAssignment>()?.Running!=true ||
            now<Due || Companion.Backpack.TotalItems>=Companion.Backpack.MaxItems-30 ||
            Companion.Backpack.TotalWeight>=Companion.Backpack.MaxWeight-5000)) { return false; }
        if (Deleted || Claimed || Companion?.Deleted != false || owner?.Deleted != false || Companion.BoundOwner != owner ||
            (!offline && (owner.Map == null || owner.Map == Map.Internal))) { return false; }
        // Timer-wheel callbacks can arrive before the wall-clock deadline. Never
        // turn an automatic completion into an early return with no pet reward.
        if (automatic && now < Due)
        {
            return false;
        }
        var minutes = Math.Clamp((int)(now - Started).TotalMinutes, 0, DurationMinutes);
        var complete=now>=Due;
        var percent=complete ? HavenMissionDuration.BonusPercent(DurationMinutes) : 100;
        var training=HavenMissionDuration.Scale(minutes,percent);
        var companion = Companion;
        var journal=HavenMissionJournal.Begin(companion,now);
        var idleMissions = companion.Backpack.FindItemByType<HavenCompanionIdleMissions>();
        var idleTrip = idleMissions?.Owns(this) == true;
        Claimed = true;
        _timer?.Stop();
        companion.AwardExpeditionProgress(training, now);
        if(!offline)
        {
            companion.MoveToWorld(owner.Location, owner.Map);
            companion.ControlTarget = owner; companion.ControlOrder = OrderType.Follow;
        }
        if (minutes > 0)
        {
            var loot = CreateLoot(Kind, minutes, owner, tamingRoll, companion, percent, complete);
            var overflow = false; var ground = false;
            foreach (var item in loot.Items.ToArray())
            {
                if (HavenResourceLedger.StoreMissionReward(companion, item, journal)) { continue; }
                var destination="Companion shared pack";
                if (idleTrip || offline) { companion.Backpack.DropItem(item); }
                else if (!companion.Backpack.TryDropItem(companion, item, false))
                {
                    overflow = true;
                    destination="Your backpack";
                    if (owner.Backpack?.TryDropItem(owner, item, false) != true)
                    { item.MoveToWorld(owner.Location, owner.Map); ground = true;destination="Ground at return location"; }
                }
                journal.Receipt(item,destination);
            }
            loot.Delete();
            owner.SendMessage(ground ? "Some mission rewards are at your feet because both packs are full." :
                overflow ? "Mission rewards are in the shared pack, with overflow in your backpack." :
                "Mission rewards are in your companion's pack; supported resource deeds go into a carried Resource Ledger.");
            owner.SendMessage($"Your companion returned from {Kind}: {minutes} minutes worked, +{percent-100}% completion bonus, +{training} Str/Dex/Int and {training * 10} gear experience.");
        }
        else { owner.SendMessage("Your companion returned. Expeditions earn rewards for each full minute away."); }
        HavenCompanionGold.Consolidate(companion.Backpack);
        var stillIdle = idleMissions?.Finished(this, now) == true;
        journal.Completed(Kind,minutes,training,complete);
        if(!stillIdle&&!offline) { journal.Finish(now,complete?"Mission completed":"Returned early"); }
        Delete();
        if (!stillIdle && owner.NetState != null) { HavenCompanionGump.DisplayTo(owner, companion); }
        return true;
    }
    internal static Bag CreateLoot(HavenExpeditionKind kind, int minutes, Mobile owner = null, double? tamingRoll = null, HavenCompanion companion = null, int percent=100, bool complete=true)
    {
        minutes=Math.Clamp(minutes,0,60); percent=Math.Clamp(percent,100,125);
        var bag = new Bag { Name = $"{kind} expedition supplies" };
        if(minutes<=0) { return bag; }
        if(HavenRegionalMissions.IsRegional(kind)) { HavenRegionalMissions.Add(bag,kind,minutes,percent);return bag; }
        if (HavenTamingMissions.IsTaming(kind))
        {
            if (minutes < 5 || !complete) { owner?.SendMessage("Taming missions must finish their selected duration to return a pet."); }
            else
            {
                var rarity=0; var rolls=HavenMissionDuration.SearchRolls(minutes,percent);
                for(var i=0;i<rolls;i++)
                {
                    if(HavenTamingMissions.IsCustomMission(kind)) { rarity=Math.Max(rarity,HavenTamingMissions.RollRarity(tamingRoll ?? Utility.RandomDouble())); }
                    var bonus=TamingBonus(Utility.RandomDouble());
                    if(bonus!=null) { bag.DropItem(bonus); }
                }
                bag.DropItem(new HavenExpeditionPetClaim { Owner = owner, Kind = kind, Rarity = rarity });
                owner?.SendMessage($"Your companion found a {HavenTamingMissions.PetName(kind)}! The claim in the shared pack shows its details.");
            }
            return bag;
        }
        switch (kind)
        {
            case HavenExpeditionKind.Ore:
            case HavenExpeditionKind.Wood:
            case HavenExpeditionKind.Leather:
                HavenMissionResources.Add(bag, kind, minutes, HavenMissionResources.Skill(companion, kind), percent:percent); break;
            case HavenExpeditionKind.Reagents:
                var reagents = new BagOfReagents(HavenMissionDuration.Scale(minutes*10,percent));
                foreach (var item in reagents.Items.ToArray()) { bag.DropItem(Deed(item)); }
                reagents.Delete(); break;
            default:
                if(minutes<=0) { break; }
                bag.DropItem(new Gold(HavenMissionDuration.Scale(minutes*Utility.RandomMinMax(200,300),percent)));
                bag.DropItem(new HavenMark(HavenMissionDuration.Scale(minutes,percent)));
                if(minutes>=3)
                {
                    var gearCount=Math.Max(1,HavenMissionDuration.Scale(minutes,percent)/5);
                    for(var i=0;i<gearCount;i++) { bag.DropItem(OldHavenWarden.CreateBossGear(0)); }
                }
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
        < 0.25 => new PowerScroll(Utility.RandomList(SkillName.Wrestling, SkillName.Tactics, SkillName.Anatomy, SkillName.Healing, SkillName.MagicResist), 105),
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
