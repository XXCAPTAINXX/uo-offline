using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.CustomBots;
using Server.Engines.Doom;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

// Physical volunteers use ordinary combat, damage, loot rights and encounter completion.
// No elapsed-time callback creates dungeon rewards.
[SerializationGenerator(0)]
public partial class HavenDungeonCrew : Item
{
    [SerializableField(0)] private List<PlayerBot> _workers = new();
    [SerializableField(1)] private int _route;
    [SerializableField(2)] private DateTime _ends;
    [SerializableField(3)] private HavenFrontierBattle _battle;
    [SerializableField(4)] private HavenShadowChamber _chamber;
    [SerializableField(5)] private HavenAbyssMiniChamp _abyss;
    [SerializableField(6)] private int _room;
    [SerializableField(7)] private bool _started;
    [SerializableField(8)] private DateTime _nextRoom;
    [SerializableField(9)] private int _completedBefore;
    [SerializableField(10)] private long _abyssBefore;
    private Timer _timer;
    internal static readonly Dictionary<PlayerBot, HavenDungeonCrew> Registry = new();
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenDungeonCrew() : base(1) { Visible = false; Movable = false; Weight = 0; }
    internal static HavenDungeonCrew For(PlayerBot bot) => bot != null && Registry.TryGetValue(bot, out var crew) && !crew.Deleted ? crew : null;
    [AfterDeserialization]
    private void Recover()
    {
        // Public encounters restore independently. Resume only the saved workers, never another reward.
        foreach (var bot in Workers)
        {
            if (bot?.Deleted != false || HavenBotLoot.PlayerOwner(bot) != null) { continue; }
            Registry[bot] = this; bot.LifecycleExempt = true;
            bot.Behavior = new HavenDungeonBehavior();
        }
        Schedule();
    }
    private void Schedule()
    {
        _timer?.Stop();
        _timer = Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), () =>
        {
            try { if (!Deleted && Workers.Count > 0) { Tick(Workers[0]); } else if (!Deleted) { End(false); } }
            catch (Exception ex)
            {
                Server.Logging.LogFactory.GetLogger(typeof(HavenDungeonCrew)).Error(ex, "Dungeon crew stopped after an encounter error");
                End(false);
            }
        });
    }
    internal static bool Eligible(PlayerBot bot) => bot?.Deleted == false && bot.Alive && !bot.LifecycleExempt &&
        bot.Map != null && bot.Map != Map.Internal && !bot.LoggingOut && !bot.CorpseRunPending && !bot.Criminal && bot.Kills < 5 && bot.Combatant == null && bot.Party == null &&
        !BotPartyManager.IsInParty(bot) && !HavenGuildCrew.Retained(bot) && !DungeonRegistry.IsInDungeon(bot) &&
        bot.Behavior is TravelerBehavior or BankSitterBehavior or IdleBehavior &&
        bot.Class is BotClass.Warrior or BotClass.Mage or BotClass.Archer or BotClass.Bard &&
        Math.Max(bot.Skills.Tactics.Base, bot.Skills.Magery.Base) >= 90;

    internal static bool Dispatch(HavenMarketStall stall, int route)
    {
        if (stall?.Deleted != false || stall.Map == null || stall.Map == Map.Internal || route is < 0 or > 4) { return false; }
        foreach (var item in stall.Items) { if (item is HavenDungeonCrew { Deleted: false }) { return false; } }
        var crew = new HavenDungeonCrew { Route = route, Ends = Core.Now + TimeSpan.FromMinutes(45) };
        if (!crew.Destination(out var point, out var map) || HasPlayers(map, point, 45)) { crew.Delete(); return false; }
        var choices = new List<PlayerBot>();
        foreach (var bot in BehaviorTickManager.Registered)
        {
            if (Eligible(bot) && !HasPlayers(bot.Map, bot.Location, 18)) { choices.Add(bot); }
        }
        if (choices.Count < 3) { crew.Delete(); return false; }
        // Prefer varied roles when the local population has them.
        choices.Sort((a, b) => b.Skills.Magery.Base.CompareTo(a.Skills.Magery.Base));
        var start = Utility.Random(choices.Count);
        for (var i = 0; i < choices.Count && crew.Workers.Count < 3; i++)
        {
            var bot = choices[(start + i) % choices.Count];
            var spot = HavenOriginalDungeons.SpawnPoint(map, point, point);
            if (!map.CanFit(spot, 16, checkMobiles: false)) { continue; }
            crew.Workers.Add(bot); Registry[bot] = crew; bot.LifecycleExempt = true;
            // Travel to the entrance; all fighting and room traversal after arrival is physical.
            bot.MoveToWorld(spot, map); bot.Behavior = new HavenDungeonBehavior();
        }
        if (crew.Workers.Count < 3) { crew.End(false); return false; }
        stall.AddItem(crew); crew.Group(); crew.Schedule();
        crew.CompletedBefore = crew.ClearCount(); crew.AbyssBefore = crew.Abyss?.Completions ?? 0;
        if (route == 2) { crew.Started = true; }
        return true;
    }
    internal static bool HasPlayers(Map map, Point3D point, int range)
    {
        if (map == null || map == Map.Internal) { return false; }
        foreach (var player in map.GetMobilesInRange<PlayerMobile>(point, range))
        { if (player is not PlayerBot && player.Player && player.NetState != null && !player.Hidden) { return true; } }
        return false;
    }
    private bool Destination(out Point3D point, out Map map)
    {
        point = default; map = Map.Trammel;
        if (Route is 0 or 4)
        {
            foreach (var battle in HavenFrontierBattle.Registry)
            {
                if (battle.Deleted || battle.Pirate != (Route == 4) || battle.Active || Core.Now < battle.NextRun || HasPlayers(battle.Map, battle.Center, 55)) { continue; }
                Battle = battle; point = battle.Location; map = battle.Map; return true;
            }
        }
        else if (Route == 1)
        {
            foreach (var chamber in HavenShadowChamber.Registry)
            {
                if (chamber.Deleted || chamber.Room != HavenShadowRoom.Bar || chamber.Active) { continue; }
                Chamber = chamber; point = chamber.ExitLocation; map = chamber.Map; return true;
            }
        }
        else if (Route == 2 && HavenDoom.Controllers().Count == 6)
        { point = new Point3D(410, 465, -1); map = Map.Malas; return true; }
        else if (Route == 3)
        {
            foreach (var expedition in HavenAbyssExpedition.Registry)
            {
                foreach (var site in expedition.Sites)
                {
                    if (site?.Deleted != false || site.Active || HasPlayers(site.Map, site.Location, 55) ||
                        !HavenAbyssMiniChamp.ConnectedPoint(site.Location, site.Map, 18, 10, out point)) { continue; }
                    Abyss = site; map = site.Map; return true;
                }
            }
        }
        return false;
    }
    private void Group()
    {
        if (Workers.Count == 0) { return; }
        var leader = Workers[0]; if (HavenBotLoot.PlayerOwner(leader) != null) { return; }
        var party = Party.Get(leader) ?? new Party(leader); leader.Party = party;
        foreach (var bot in Workers) { if (bot?.Deleted == false && bot.Party == null) { party.Add(bot); } }
    }
    internal void Hold()
    {
        foreach (var bot in Workers)
        { if (bot?.Deleted == false) { bot.Combatant = null; bot.Warmode = false; bot.Behavior = new IdleBehavior(); } }
    }
    internal bool Join(PlayerMobile player)
    {
        var party = Party.Get(player);
        if (Workers.Count == 0 || player.Deleted || !player.Alive || party != null && party.Leader != player) { return false; }
        var capacity = Party.Capacity - (party?.Members.Count ?? 1) - (party?.Candidates.Count ?? 0);
        if (capacity < Workers.Count) { player.SendMessage("Your party needs room for the three adventurers. They will leave you the area."); End(false); return false; }
        // Leave a bot-reserved room before handing over. Never reset a room containing a real player.
        if (Chamber?.Active == true && OnlyBots(Chamber)) { Chamber.Finish(false); }
        var bots = Workers.ToArray();
        Party.Get(bots[0])?.Disband();
        party ??= new Party(player); player.Party = party;
        foreach (var bot in bots)
        {
            Registry.Remove(bot); bot.LifecycleExempt = false;
            if (bot.Deleted || !bot.Alive) { continue; }
            var spot = HavenOriginalDungeons.SpawnPoint(player.Map, player.Location, player.Location);
            bot.MoveToWorld(spot, player.Map); party.Add(bot); bot.Behavior = new PlayerGroupBehavior();
        }
        Workers.Clear(); Delete(); return true;
    }
    internal static bool OnlyBots(HavenShadowChamber chamber)
    {
        foreach (var member in chamber.Members) { if (member is PlayerMobile and not PlayerBot) { return false; } }
        return chamber.Members.Count > 0;
    }
    internal void Tick(PlayerBot bot)
    {
        if (Deleted || Workers.Count == 0) { return; }
        if (Core.Now >= Ends || Workers.Exists(m => m?.Deleted != false || !m.Alive)) { End(false); return; }
        if (Workers.Exists(m => HavenBotLoot.PlayerOwner(m) != null)) { End(false); return; }
        if (bot != Workers[0]) { return; }
        Group();
        if (Chamber?.Active == true && OnlyBots(Chamber))
        {
            foreach (var player in Chamber.Map.GetMobilesInRange<PlayerMobile>(Chamber.ExitLocation, 15))
            {
                if (player is not PlayerBot && player.Player && player.NetState != null && !player.Hidden)
                { Chamber.Finish(false); HavenDungeonCourtesy.OfferTo(bot, player); return; }
            }
        }
        if (HavenDungeonCourtesy.Waiting(bot)) { return; }
        if (Route is 0 or 4)
        {
            if (Battle?.Deleted != false) { End(false); return; }
            if (!Started)
            {
                if (HasPlayers(Battle.Map, Battle.Center, 50)) { End(false); return; }
                if (Battle.Active || !Battle.Start(bot)) { return; }
                Started = true;
                if (Battle.Pirate) { foreach (var member in Workers) { Battle.Board(member); member.Behavior = new HavenDungeonBehavior(); } }
            }
            else if (!Battle.Active)
            {
                if (NextRoom == DateTime.MinValue) { NextRoom = Core.Now + TimeSpan.FromMinutes(1); }
                else if (Core.Now >= NextRoom) { End(ClearCount() > CompletedBefore); }
            }
        }
        else if (Route == 1)
        {
            if (Chamber?.Active == true) { return; }
            if (Started)
            {
                var record = HavenFrontierRecord.Get(bot);
                if (Room == 5) { End(ClearCount() > CompletedBefore); return; }
                if ((record.Rooms & 1 << Room) == 0) { End(false); return; }
                Room++; Started = false; NextRoom = Core.Now + TimeSpan.FromSeconds(15);
            }
            if (Core.Now < NextRoom) { return; }
            foreach (var chamber in HavenShadowChamber.Registry)
            {
                if ((int)chamber.Room != Room || chamber.Deleted) { continue; }
                Chamber = chamber;
                if (chamber.Active || HasPlayers(chamber.Map, chamber.ExitLocation, 25)) { End(false); return; }
                if (chamber.Start(bot))
                { Started = true; foreach (var member in Workers) { member.Behavior = new HavenDungeonBehavior(); } }
                return;
            }
        }
        else if (Route == 3)
        {
            if (Abyss?.Deleted != false) { End(false); return; }
            if (!Started) { Started = Abyss.Active || Abyss.Start(); }
            else if (!Abyss.Active) { End(Abyss.Completions > AbyssBefore); }
        }
    }
    private int ClearCount()
    {
        var count = 0;
        foreach (var bot in Workers)
        {
            if (bot?.Deleted != false) { continue; }
            var record = HavenFrontierRecord.Get(bot);
            count += Route == 0 ? record.Rifts : Route == 4 ? record.Voyages : record.Roofs;
        }
        return count;
    }
    internal Point3D Goal(PlayerBot bot)
    {
        foreach (var corpse in bot.GetItemsInRange<Corpse>(12))
        { if (HavenBotLoot.CanLoot(bot, corpse, 12) && corpse.Items.Exists(i => i.Movable && !i.IsVirtualItem)) { return corpse.Location; } }
        if (Battle?.Deleted == false && Started) { return Battle.Center; }
        if (Chamber?.Active == true) { return Chamber.Arrival; }
        if (Abyss?.Deleted == false) { return Abyss.Location; }
        if (Route == 2)
        {
            GauntletSpawner active = null;
            foreach (var room in HavenDoom.Controllers())
            { if (room.State == GauntletSpawnerState.InProgress) { active = room; break; } }
            if (active != null)
            {
                foreach (var room in HavenDoom.Controllers())
                {
                    if (room == active || !room.RegionBounds.Contains(bot.Location)) { continue; }
                    return room.X switch
                    { 491 => new Point3D(472,432,-1), 482 => new Point3D(468,495,-1), 406 => new Point3D(408,503,-1),
                      335 => new Point3D(360,477,-1), 326 => new Point3D(361,429,-1), _ => active.Location };
                }
                if (active.RegionBounds.Contains(bot.Location)) { return active.Location; }
                return active.X switch
                { 491 => new Point3D(471,428,-1), 482 => new Point3D(462,494,-1), 406 => new Point3D(403,502,-1),
                  335 => new Point3D(357,476,-1), 326 => new Point3D(361,433,-1), _ => active.Location };
            }
        }
        return bot.Location;
    }
    internal Mobile Enemy(PlayerBot bot)
    {
        Mobile result = null; var distance = double.MaxValue;
        foreach (var creature in bot.Map.GetMobilesInRange<BaseCreature>(bot.Location, 28))
        {
            if (creature.Deleted || !creature.Alive || creature.Controlled || creature.Summoned || creature.Blessed || creature.IsInvulnerable ||
                creature is HavenShadowActor actor && actor.Chamber != Chamber ||
                creature is HavenFrontierEnemy enemy && enemy.Battle != Battle ||
                creature.Karma >= 0 && creature is not HavenShadowActor and not HavenFrontierEnemy || !bot.CanBeHarmful(creature, false) || !bot.InLOS(creature)) { continue; }
            var d = bot.GetDistanceToSqrt(creature); if (d < distance) { distance = d; result = creature; }
        }
        return result;
    }
    internal void End(bool completed)
    {
        if (Deleted) { return; }
        if (Parent is HavenMarketStall stall)
        { var job = HavenMarketExpansion.Get(stall); if (completed) { job.Completed++; } else { job.Failed++; } stall.NextWork = Core.Now + TimeSpan.FromMinutes(5); }
        Delete();
    }
    public override void OnDelete()
    {
        _timer?.Stop(); _timer = null;
        if (Chamber?.Active == true && OnlyBots(Chamber)) { Chamber.Finish(false); }
        foreach (var bot in Workers.ToArray())
        {
            if (bot == null) { continue; }
            Registry.Remove(bot);
            if (bot?.Deleted != false) { continue; }
            bot.LifecycleExempt = false;
            if (HavenBotLoot.PlayerOwner(bot) == null) { HavenDungeonCourtesy.Leave(bot); }
        }
        Workers.Clear(); base.OnDelete();
    }
}

public sealed class HavenDungeonBehavior : AdventurerBehavior
{
    public override string SerializableName => "HavenDungeon";
    private readonly HavenPartySupport _support = new();
    private readonly HavenDungeonNavigation _navigation = new();
    public override void OnDetached(PlayerBot bot) { _support.Cancel(); base.OnDetached(bot); }
    public override void Tick(PlayerBot bot)
    {
        var crew = HavenDungeonCrew.For(bot);
        if (crew == null) { bot.LifecycleExempt = false; bot.Behavior = new TravelerBehavior(); return; }
        ArrivalRange = 0;
        crew.Tick(bot);
        if (bot.Behavior != this || crew.Deleted || HavenDungeonCourtesy.Waiting(bot)) { HaltMovement(); return; }
        HavenBotLoot.Collect(bot);
        if (crew.Chamber?.Active == true && crew.Chamber.Participant(bot) && HavenShadowPuzzles.AssistBot(crew.Chamber, bot))
        { HaltMovement(); return; }
        if (bot.Combatant is not Mobile { Deleted: false, Alive: true }) { bot.Combatant = crew.Enemy(bot); }
        if (_support.Tick(bot)) { StopStepTimer(); return; }
        base.Tick(bot);
    }
    protected override bool IsPartyFriend(PlayerBot bot, Mobile other) => Party.Get(bot) != null && Party.Get(other) == Party.Get(bot);
    protected override bool WantsFreshFights => false;
    protected override bool PatrolRuns => true;
    protected override Point3D? SelectPatrolGoal(PlayerBot bot) => _navigation.Next(bot, HavenDungeonCrew.For(bot)?.Goal(bot) ?? bot.Location);
    protected override bool OnPatrolGoalReached(PlayerBot bot) => false;
    protected override bool PatrolGoalStale(PlayerBot bot, Point3D goal) => _navigation.Next(bot, HavenDungeonCrew.For(bot)?.Goal(bot) ?? bot.Location) != goal;
}
