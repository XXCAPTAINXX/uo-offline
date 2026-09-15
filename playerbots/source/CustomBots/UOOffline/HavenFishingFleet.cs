using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.CustomBots;
using Server.Engines.Harvest;
using Server.Engines.PartySystem;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.UOOffline;

public enum HavenSeaWork { Docked, Sailing, Fishing, Salvaging, Returning, Waiting }

[SerializationGenerator(0)]
public partial class HavenFishingFleet : Item
{
    internal static readonly HashSet<HavenFishingFleet> Registry = new();
    internal static readonly Point3D Harbor = new(4237, 2990, -5);
    [SerializableField(0)] private SmallBoat _boat;
    [SerializableField(1)] private List<HavenFishingSailor> _sailors = new();
    [SerializableField(2)] private List<Item> _cargo = new();
    [SerializableField(3)] private List<BaseCreature> _threats = new();
    [SerializableField(4)] private Point3D _home;
    [SerializableField(5)] private Point3D _goal;
    [SerializableField(6)] private HavenSeaWork _work;
    [SerializableField(7)] private DateTime _due;
    [SerializableField(8)] private DateTime _returnBy;
    [SerializableField(9)] private int _trips;
    [SerializableField(10)] private int _catches;
    [SerializableField(11)] private int _wrecks;
    [SerializableField(12)] private int _nets;
    [SerializableField(13)] private int _sold;
    [SerializableField(14)] private SOS _wreck;
    [SerializableField(15)] private List<Point3D> _trail = new();
    [SerializableField(16)] private List<Point3D> _course = new();
    [SerializableField(17)] private int _castCount;
    [SerializableField(18)] private string _status = "At harbor";
    private Timer _timer;
    private HavenSeaNavigation _planner;
    private DateTime _nextCast, _nextNet;
    private int _blockedSteps;
    private bool _detourHome;
    public override bool IsVirtualItem => true;
    internal HavenFishingSailor Captain => Sailors.Count > 0 ? Sailors[0] : null;
    [Constructible]
    public HavenFishingFleet() : base(1) { Visible = false; Movable = false; Name = "Haven fishing voyage"; }
    public static void Initialize()
    {
        CommandSystem.Register("fishfleet", AccessLevel.Player, e => { e.Mobile.CloseGump<HavenFishingFleetGump>(); e.Mobile.SendGump(new HavenFishingFleetGump()); });
        Timer.DelayCall(TimeSpan.FromSeconds(40), () =>
        {
            try { Ensure(); }
            catch (Exception ex) { Server.Logging.LogFactory.GetLogger(typeof(HavenFishingFleet)).Error(ex, "Fishing fleet installation failed"); }
        });
    }
    internal static void Ensure()
    {
        if (HavenChelonia.Registry.Count == 0) { return; }
        for (var index = Registry.Count; index < 2; index++)
        {
            var point = new Point3D(Harbor.X + index * 24, Harbor.Y, Harbor.Z);
            var boat = new SmallBoat();
            if (!boat.CanFit(point, Map.Trammel, boat.NorthID) || HavenDungeonCrew.HasPlayers(Map.Trammel, point, 20)) { boat.Delete(); return; }
            var fleet = new HavenFishingFleet { Boat = boat, Home = point, Work = HavenSeaWork.Docked, Due = Core.Now + TimeSpan.FromSeconds(15) };
            boat.MoveToWorld(point, Map.Trammel); boat.ShipName = index == 0 ? "The Silver Wake" : "The Wandering Tern";
            for (var role = 0; role < 2; role++)
            {
                var sailor = new HavenFishingSailor(role) { Fleet = fleet, LifecycleExempt = true };
                fleet.Sailors.Add(sailor); sailor.MoveToWorld(new Point3D(point.X, point.Y + (role == 0 ? 1 : -1), point.Z + 3), Map.Trammel);
                sailor.Behavior = new HavenFishingBehavior();
            }
            boat.Owner = fleet.Captain; boat.PPlank.Locked = true; boat.SPlank.Locked = true;
            fleet.Group(); fleet.Register();
        }
    }
    private void Group()
    {
        if (Captain == null || Sailors.Exists(s => s.Party != null)) { return; }
        var party = new Party(Captain); Captain.Party = party;
        for (var i = 1; i < Sailors.Count; i++) { party.Add(Sailors[i]); }
    }
    [AfterDeserialization]
    private void Register()
    {
        Registry.Add(this); _nextCast = Core.Now + TimeSpan.FromSeconds(10); _nextNet = Core.Now + TimeSpan.FromMinutes(2);
        foreach (var sailor in Sailors)
        { if (sailor?.Deleted == false) { sailor.Fleet = this; sailor.LifecycleExempt = true; sailor.Behavior = new HavenFishingBehavior(); } }
        _timer?.Stop(); _timer = Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), SafeTick);
    }
    private void SafeTick()
    {
        try { Tick(Core.Now); }
        catch (Exception ex)
        {
            Status = "Paused after an error; cargo preserved"; Work = HavenSeaWork.Waiting; Due = Core.Now + TimeSpan.FromMinutes(5);
            Server.Logging.LogFactory.GetLogger(typeof(HavenFishingFleet)).Error(ex, "Fishing voyage paused");
        }
    }
    internal static HavenFishingFleet For(Mobile m) => (m as HavenFishingSailor)?.Fleet;
    internal static bool DeliverCatch(Mobile from, Item item)
    {
        var fleet = For(from);
        if (fleet?.Deleted != false || item?.Deleted != false || fleet.Boat?.Deleted != false || !from.Alive ||
            from.Map != fleet.Boat.Map || !fleet.Boat.Contains(from)) { return false; }
        // Native fishing already rolled this catch. Deliver before it can merge with personal supplies.
        fleet.Catches++; if (item is LockableContainer) { fleet.Wrecks++; }
        fleet.Stow(item); fleet.MarkDirty(); return true;
    }
    internal static void Spawned(Mobile from, BaseCreature enemy)
    { var fleet = For(from); if (fleet?.Deleted == false && !fleet.Threats.Contains(enemy)) { fleet.Threats.Add(enemy); fleet.MarkDirty(); } }
    internal void Stow(Item item)
    {
        if (!Cargo.Contains(item)) { Cargo.Add(item); }
        HavenMarketProvenance.Attach(item, "Boat fishing and recovered sea loot", Captain?.Name ?? "Haven fishing crew");
        if (item is SOS or SpecialFishingNet or MessageInABottle) { Captain.Backpack.DropItem(item); }
        else { Boat.Hold.DropItem(item); }
        this.MarkDirty();
    }
    internal Mobile Passenger()
    {
        if (Boat?.Deleted != false) { return null; }
        foreach (var mobile in Boat.Map.GetMobilesInRange<Mobile>(Boat.Location, 10))
        {
            // Contains is a two-dimensional hull check: sea creatures beneath the deck are not passengers.
            if (mobile.Z < Boat.Z + 2 || mobile.Z > Boat.Z + 20 || !Boat.Contains(mobile)) { continue; }
            if (mobile is PlayerBot) { continue; }
            if (mobile.Player || mobile is BaseCreature { Controlled: true } or BaseCreature { Summoned: true }) { return mobile; }
        }
        return null;
    }
    internal bool Passengers() => Passenger() != null;
    internal void Tick(DateTime now)
    {
        if (Deleted || Boat?.Deleted != false || Captain?.Deleted != false) { return; }
        Boat.Refresh();
        if (Passengers() || Sailors.Exists(s => s?.Deleted == false && (HavenBotLoot.PlayerOwner(s) != null || HavenGuildCrew.Retained(s))))
        { Boat.StopMove(false); Status = "Paused for a passenger or player-owned crew"; return; }
        if (Work == HavenSeaWork.Waiting) { if (now < Due) { return; } BeginReturn(); }
        if (Work != HavenSeaWork.Docked && Sailors.Exists(s => s?.Deleted != false || !s.Alive || s.Hits < s.HitsMax / 3 || s.Map != Boat.Map || !Boat.Contains(s))) { BeginReturn(); }
        foreach (var sailor in Sailors)
        { if (sailor?.Deleted == false && sailor.Behavior is not HavenFishingBehavior) { sailor.Behavior = new HavenFishingBehavior(); } }
        if (Work != HavenSeaWork.Returning && Work != HavenSeaWork.Docked && (now >= ReturnBy || Boat.Hold.TotalWeight > 300 || Cargo.Count > 90)) { BeginReturn(); }
        if (Work == HavenSeaWork.Docked)
        {
            Unload();
            if (now < Due || Cargo.Count > 10) { Status = "At harbor; unloading or waiting for market space"; return; }
            foreach (var s in Sailors)
            {
                if (s.Deleted) { Status = "Missing crew; departure suspended"; return; }
                if (!s.Alive) { BotDeathManager.ResurrectBot(s, "returned to the fishing harbor"); }
                foreach (var corpse in Boat.Map.GetItemsInRange<Corpse>(Boat.Location, 12))
                { if (corpse.Owner == s && Boat.Contains(corpse)) { BotDeathManager.ReclaimCorpse(s, corpse); } }
                s.MoveToWorld(new Point3D(Boat.X, Boat.Y + (s == Captain ? 1 : -1), Boat.Z + 3), Boat.Map);
            }
            Group(); Trail.Clear(); Trail.Add(Boat.Location); Course.Clear(); CastCount = 0;
            Goal = new Point3D(Home.X + 100, Home.Y + 40, Home.Z); ReturnBy = now + TimeSpan.FromMinutes(18);
            Work = HavenSeaWork.Sailing; Status = "Sailing to fishing grounds"; _planner = null;
        }
        if (Work == HavenSeaWork.Returning)
        {
            if (Boat.InRange(Home, 1)) { Work = HavenSeaWork.Docked; Trips++; Due = now + TimeSpan.FromMinutes(2); Unload(); return; }
            if (Trail.Count > 0 && Boat.Location == Trail[^1]) { Trail.RemoveAt(Trail.Count - 1); }
            if (_detourHome || Trail.Count == 0) { Navigate(Home, 1); }
            else if (Sail(Trail[^1])) { _blockedSteps = 0; }
            else if (++_blockedSteps >= 3) { _detourHome = true; Course.Clear(); _planner = null; _blockedSteps = 0; }
            return;
        }
        if (Threat()) { Boat.StopMove(false); Status = "Crew defending the vessel"; return; }
        if (RecoverLoot()) { return; }
        if (Work == HavenSeaWork.Sailing)
        {
            if (Utility.InRange(Boat.Location, Goal, Wreck?.Deleted == false ? 35 : 4))
            { Work = Wreck?.Deleted == false ? HavenSeaWork.Salvaging : HavenSeaWork.Fishing; Due = now + TimeSpan.FromMinutes(3); _planner = null; Course.Clear(); }
            else
            {
                Navigate(Goal, Wreck?.Deleted == false ? 35 : 4);
                return;
            }
        }
        if (Work is HavenSeaWork.Fishing or HavenSeaWork.Salvaging)
        {
            Status = Work == HavenSeaWork.Salvaging ? "Fishing the SOS wreck site" : "Fishing in deep water";
            if (now >= Due)
            {
                if (TryWreck()) { return; }
                if (CastCount >= 60 || now + TimeSpan.FromMinutes(5) >= ReturnBy) { BeginReturn(); return; }
                Goal = new Point3D(Boat.X + 32, Boat.Y + 24, Boat.Z); Work = HavenSeaWork.Sailing; Course.Clear(); _planner = null; return;
            }
            TryNet(now);
            if (now >= _nextCast) { _nextCast = now + TimeSpan.FromSeconds(10); Fish(); }
        }
        this.MarkDirty();
    }
    internal void Navigate(Point3D goal, int range)
    {
        if (Course.Count == 0)
        {
            _planner ??= new HavenSeaNavigation(Boat.Location, goal, range);
            _planner.Advance(Boat);
            if (_planner.Failed)
            {
                _planner = null; Status = "No safe route; keeping earned cargo aboard";
                if (Work != HavenSeaWork.Returning) { BeginReturn(); }
                return;
            }
            if (!_planner.Finished) { Status = "Charting a safe sailing route"; return; }
            Course.AddRange(_planner.Path); _planner = null;
        }
        if (Course.Count == 0) { return; }
        if (Sail(Course[0])) { Course.RemoveAt(0); _blockedSteps = 0; }
        else if (++_blockedSteps >= 3)
        {
            // Another boat may have entered the old course; chart around its current position.
            Course.Clear(); _planner = null; _blockedSteps = 0;
            Status = "Recharting around an obstruction";
        }
    }
    internal bool Sail(Point3D target)
    {
        var dx = target.X - Boat.X; var dy = target.Y - Boat.Y;
        if (dx == 0 && dy == 0) { return true; }
        var direction = Utility.GetDirection(Boat.Location, target); var relative = (Direction)(((int)direction - (int)Boat.Facing + 8) & 7);
        var before = Boat.Location; Boat.RaiseAnchor(false);
        if (!HavenSeaNavigation.Clear(Boat, before, target) || !Boat.Move(relative, Math.Min(4, Math.Max(Math.Abs(dx), Math.Abs(dy))), 4, false))
        { Status = "Channel blocked; waiting for clear water"; return false; }
        if (Work != HavenSeaWork.Returning && (Trail.Count == 0 || Trail[^1] != Boat.Location)) { Trail.Add(Boat.Location); }
        this.MarkDirty(); return Boat.Location == target;
    }
    internal void BeginReturn()
    { if (Work == HavenSeaWork.Returning) { return; } Work = HavenSeaWork.Returning; Course.Clear(); _planner = null; _blockedSteps = 0; _detourHome = false; Status = "Returning to harbor with earned cargo"; }
    internal object WaterTarget()
    {
        if (Captain?.Deleted != false || Boat?.Deleted != false) { return null; }
        for (var dx = -4; dx <= 4; dx++)
        {
            for (var dy = -4; dy <= 4; dy++)
            {
                var x = Captain.X + dx; var y = Captain.Y + dy;
                if (Boat.Contains(x, y)) { continue; }
                var tile = Boat.Map.Tiles.GetLandTile(x, y); var p = new Point3D(x, y, tile.Z);
                if ((tile.ID is >= 0xA8 and <= 0xAB or >= 0x136 and <= 0x137) && Captain.InLOS(p)) { return new LandTarget(p, Boat.Map); }
            }
        }
        return null;
    }
    internal bool Fish()
    {
        if (Captain?.Alive != true || Captain.Spell != null || Threat() || !Boat.Contains(Captain)) { return false; }
        var pole = Captain.Backpack.FindItemByType<FishingPole>() ?? Captain.FindItemOnLayer(Layer.TwoHanded) as FishingPole;
        var target = WaterTarget(); if (pole == null || target == null) { return false; }
        CastCount++; Fishing.System.StartHarvesting(Captain, pole, target); return true;
    }
    internal bool TryWreck()
    {
        Wreck = null;
        foreach (var item in Cargo.ToArray())
        {
            if (item is MessageInABottle bottle && bottle.IsChildOf(Captain.Backpack) && bottle.TargetMap == Boat.Map)
            {
                // Native opening creates the actual SOS and preserves its rolled level/location.
                var before = new HashSet<Item>(Captain.Backpack.Items); bottle.OnDoubleClick(Captain); Cargo.Remove(bottle);
                foreach (var added in Captain.Backpack.Items) { if (added is SOS && !before.Contains(added) && !Cargo.Contains(added)) { Cargo.Add(added); } }
            }
        }
        foreach (var item in Cargo)
        {
            if (item is not SOS sos || sos.Deleted || !sos.IsChildOf(Captain.Backpack) || sos.TargetMap != Boat.Map || Boat.GetDistanceToSqrt(sos.TargetLocation) > 900 || Boat.InRange(sos.TargetLocation, 60)) { continue; }
            Wreck = sos; Goal = sos.TargetLocation; Work = HavenSeaWork.Sailing; Course.Clear(); _planner = null; return true;
        }
        return false;
    }
    internal bool TryNet(DateTime now)
    {
        if (now < _nextNet || Nets >= Trips + 1 || Threat() || Sailors.Exists(s => !s.Alive || s.Hits < s.HitsMax * .8) || HavenDungeonCrew.HasPlayers(Boat.Map, Boat.Location, 60)) { return false; }
        foreach (var item in Cargo)
        {
            if (item is not SpecialFishingNet net || net is FabledFishingNet || net.Deleted || !net.IsChildOf(Captain.Backpack)) { continue; }
            var target = WaterTarget(); if (target == null) { return false; }
            net.OnTarget(Captain, target);
            if (!net.InUse) { return false; }
            Nets++; _nextNet = now + TimeSpan.FromMinutes(3); _nextCast = now + TimeSpan.FromSeconds(25); return true;
        }
        return false;
    }
    internal bool Threat()
    {
        var result = false;
        foreach (var creature in Boat.Map.GetMobilesInRange<BaseCreature>(Boat.Location, 16))
        {
            if (creature.Deleted || !creature.Alive || creature.Controlled || creature.Summoned || creature.Karma >= 0 || creature.Blessed || creature is HavenScalis ||
                !Sailors.Exists(s => creature.Combatant == s || creature.Aggressors.Exists(a => a.Attacker == s) || creature.Aggressed.Exists(a => a.Defender == s))) { continue; }
            if (!Threats.Contains(creature)) { Threats.Add(creature); }
            foreach (var s in Sailors) { if (s.Alive && s.CanBeHarmful(creature, false) && s.InLOS(creature)) { s.Combatant = creature; result = true; } }
        }
        return result;
    }
    private bool RecoverLoot()
    {
        foreach (var corpse in Boat.Map.GetItemsInRange<Corpse>(Boat.Location, 14))
        {
            if (corpse.Owner is not BaseCreature creature || !Threats.Contains(creature) || !HavenBotLoot.CanLoot(Captain, corpse, 16)) { continue; }
            // A salvage crew hoists its own floating corpse loot; never another player's kill.
            foreach (var item in corpse.Items.ToArray())
            { if (!item.Deleted && item.Movable && !item.IsVirtualItem && corpse.CheckLoot(Captain, item)) { Stow(item); } }
        }
        Threats.RemoveAll(t => t == null || t.Deleted); return false;
    }
    internal void Unload()
    {
        var stall = HavenMarketExpansion.Find(HavenMarketTrade.Maritime);
        Cargo.RemoveAll(i => i == null || i.Deleted);
        if (stall == null || Captain == null) { return; }
        foreach (var item in Cargo.ToArray())
        {
            if (item.RootParent != Captain && !item.IsChildOf(Boat.Hold)) { continue; }
            if (item is Gold) { Captain.Backpack.DropItem(item); Cargo.Remove(item); continue; }
            var price = item is FabledFishingNet ? 20000 : item is SpecialFishingNet ? 3000 : item is SOS sos ? sos.IsAncient ? 15000 : 2500 : item is MessageInABottle ? 2000 : Math.Max(25, BotAppraisal.Value(item));
            if (!stall.ListItem(item, price)) { break; }
            Cargo.Remove(item); Sold++;
        }
        this.MarkDirty();
    }
    public override void OnDelete()
    {
        Registry.Remove(this); _timer?.Stop(); _timer = null; _planner = null;
        foreach (var sailor in Sailors)
        { if (sailor?.Deleted == false) { sailor.Fleet = null; sailor.LifecycleExempt = false; HavenFrontierSupport.Return(sailor, HavenChelonia.Landing, Map.Trammel); sailor.Behavior = new TravelerBehavior(); } }
        Sailors.Clear(); Cargo.Clear(); Threats.Clear(); Wreck = null; Boat = null; base.OnDelete();
    }
}

[SerializationGenerator(0)]
public partial class HavenFishingSailor : PlayerBot
{
    [SerializableField(0)] private HavenFishingFleet _fleet;
    [Constructible]
    public HavenFishingSailor(int role = 0) : base(role == 0 ? BotClass.Fisherman : BotClass.Mage, BotSkillTier.Expert)
    {
        Title = role == 0 ? "the deep-sea fisherman" : "the ship's mage";
        Skills.Fishing.Base = 100; Skills.Magery.Base = 100; Skills.EvalInt.Base = 100; Skills.Meditation.Base = 100;
        var robe = new Robe(0x482); robe.Attributes.LowerRegCost = 100; robe.Attributes.RegenMana = 5;
        FindItemOnLayer(Layer.OuterTorso)?.Delete(); EquipItem(robe);
        if (Backpack.FindItemByType<FishingPole>() == null && FindItemOnLayer(Layer.TwoHanded) is not FishingPole) { Backpack.DropItem(new FishingPole()); }
    }
    protected override bool OnMove(Direction d) => Fleet?.Deleted != false && base.OnMove(d);
    public override void OnDelete() { Fleet = null; base.OnDelete(); }
}

public sealed class HavenFishingBehavior : AdventurerBehavior
{
    public HavenFishingBehavior() { RangedCombat = true; SpellcasterMode = true; }
    public override string SerializableName => "HavenFishing";
    private readonly HavenPartySupport _support = new();
    protected override bool WantsFreshFights => false;
    protected override Point3D? SelectPatrolGoal(PlayerBot bot) => null;
    public override void OnDetached(PlayerBot bot) { _support.Cancel(); base.OnDetached(bot); }
    public override void Tick(PlayerBot bot)
    {
        if (HavenFishingFleet.For(bot)?.Deleted != false) { bot.Behavior = new TravelerBehavior(); return; }
        if (_support.Tick(bot)) { StopStepTimer(); return; }
        if (bot.Combatant is Mobile { Alive: true, Deleted: false }) { base.Tick(bot); }
        else { StopStepTimer(); }
    }
    protected override bool IsPartyFriend(PlayerBot bot, Mobile other) => Party.Get(bot) != null && Party.Get(other) == Party.Get(bot);
}

public sealed class HavenFishingFleetGump : Gump
{
    public HavenFishingFleetGump() : base(60, 60)
    {
        AddBackground(0, 0, 620, 355, 3000); AddLabel(24, 22, 0, "Haven fishing fleet"); var row = 0;
        foreach (var fleet in HavenFishingFleet.Registry)
        {
            if (fleet.Deleted || fleet.Boat?.Deleted != false) { continue; }
            var y = 65 + row++ * 105;
            AddLabel(24, y, 0, $"{fleet.Boat.ShipName} - {fleet.Boat.X}, {fleet.Boat.Y}");
            AddLabel(24, y + 25, 0, fleet.Status);
            AddLabel(24, y + 50, 0, $"Trips {fleet.Trips} | Catches {fleet.Catches} | SOS chests {fleet.Wrecks} | Nets {fleet.Nets} | Listings {fleet.Sold}");
        }
        AddLabel(24, 284, 0, "Earned goods are sold under Maritime in [market.");
        AddButton(24, 316, 4005, 4007, 1); AddLabel(62, 317, 0, "Refresh"); AddButton(500, 316, 4017, 4019, 0); AddLabel(538, 317, 0, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info) { if (info.ButtonID == 1) { state.Mobile.SendGump(new HavenFishingFleetGump()); } }
}
