using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.Engines.Craft;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Spells;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenEncounterJournal : Item
{
    [SerializableField(0)] private Mobile _owner;
    [SerializableField(1)] private bool _enabled = true;
    [SerializableField(2)] private int _tier = 1;
    [SerializableField(3)] private int _wins;
    [SerializableField(4)] private int _failures;
    [SerializableField(5)] private DateTime _nextRoll;
    [SerializableField(6)] private Point3D _lastPosition;
    [SerializableField(7)] private HavenWanderingEncounter _active;
    [SerializableField(8)] private int _kills;
    [SerializableField(9)] private int _points;
    [SerializableField(10)] private int _earnedPoints;
    [SerializableField(11)] private int _highestTier = 1;
    [SerializableField(12)] private List<string> _history = new();
    [SerializableField(13)] private int _assists;
    public override bool IsVirtualItem => true;
    [Constructible] public HavenEncounterJournal() : base(1) { Visible = false; Movable = false; Weight = 0; }
    internal static HavenEncounterJournal Get(Mobile owner)
    {
        foreach (var item in owner.Items) { if (item is HavenEncounterJournal journal) { return journal; } }
        var created = new HavenEncounterJournal { Owner = owner, LastPosition = owner.Location, NextRoll = Core.Now + TimeSpan.FromMinutes(Utility.RandomMinMax(12, 20)) };
        owner.AddItem(created); return created;
    }
    internal void Check(DateTime now, double roll)
    {
        var player = Owner;
        if (!Enabled || Active != null || player?.Deleted != false) { return; }
        var moved = LastPosition != player.Location; LastPosition = player.Location;
        if (!moved || now < NextRoll || player.NetState == null || !HavenWanderingEncounter.CanStart(player)) { return; }
        NextRoll = now + TimeSpan.FromMinutes(Utility.RandomMinMax(12, 20));
        if (roll < .5) { HavenWanderingEncounter.Start(this); }
    }
    internal void Outcome(bool success, bool penalty)
    {
        var completedTier = Active?.Tier ?? Tier;
        if (success) { Wins++; Tier = Math.Min(20, Tier + 1); HighestTier = Math.Max(HighestTier, Tier); }
        else if (penalty) { Failures++; Tier = Math.Max(1, Tier - 1); }
        Log($"Tier {completedTier}: {(success ? "cleared" : penalty ? "retreated / failed" : "cancelled (no penalty)")}");
        Active = null; NextRoll = Core.Now + TimeSpan.FromMinutes(Utility.RandomMinMax(12, 20));
    }
    internal void Log(string message)
    {
        History.Add($"{Core.Now:MM-dd HH:mm} UTC  {message}");
        while (History.Count > 20) { History.RemoveAt(0); }
        this.MarkDirty();
    }
    internal void Earn(int amount)
    { amount = Math.Max(0, amount); Points += amount; EarnedPoints += amount; }
    internal void KillCredit(int tier) { Kills++; Earn(1 + tier / 5); }
    internal void ClearCredit(int tier, bool assisted)
    {
        HavenSovereigns.EncounterCleared(Owner, tier);
        var amount = 20 + tier * 5; Earn(amount);
        if (assisted) { Assists++; Log($"Assisted tier {tier} clear; +{amount} points"); }
        else { Log($"Clear bonus: +{amount} encounter points"); }
    }
    internal static readonly string[] RewardNames = { "25 Astral shards", "Agapite runic hammer (25 uses)", "Summon a rare custom pet (rarity varies)", "Summon a Legendary custom pet" };
    internal static readonly int[] RewardCosts = { 250, 500, 1000, 5000 };
    internal bool Redeem(Mobile from, int reward)
    {
        if (Deleted || from != Owner || !from.Alive || from.Backpack == null || reward < 0 || reward >= RewardCosts.Length || Points < RewardCosts[reward]) { return false; }
        if (reward >= 2)
        {
            if (Active != null || !HavenWanderingEncounter.CanStart(from)) { from.SendMessage("Summon pets outdoors, outside houses and guarded towns, after your encounter ends."); return false; }
            var anchor = new HavenWanderingEncounter { Journal = this, Tier = Tier };
            anchor.MoveToWorld(from.Location, from.Map);
            var pet = anchor.SpawnPet(reward == 3 ? 3 : -1); anchor.Delete();
            if (pet == null) { return false; }
        }
        else
        {
            Item item = reward == 0 ? new AstralShard(25) : new RunicHammer(CraftResource.Agapite, 25);
            if (!from.Backpack.TryDropItem(from, item, true)) { item.Delete(); return false; }
        }
        Points -= RewardCosts[reward]; Log($"Redeemed: {RewardNames[reward]} (-{RewardCosts[reward]})"); return true;
    }
    public override void OnDelete() { var active = Active; Active = null; Owner = null; active?.Delete(); base.OnDelete(); }
    public static void Initialize()
    {
        CommandSystem.Register("encounters", AccessLevel.Player, e =>
        {
            var journal = Get(e.Mobile);
            if (e.ArgString.Equals("off", StringComparison.OrdinalIgnoreCase)) { journal.Enabled = false; journal.Active?.Finish(false, false); }
            else if (e.ArgString.Equals("on", StringComparison.OrdinalIgnoreCase)) { journal.Enabled = true; }
            e.Mobile.CloseGump<HavenEncounterStatusGump>(); e.Mobile.SendGump(new HavenEncounterStatusGump(journal));
        });
        Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), () =>
        {
            foreach (var state in NetState.Instances)
            { if (state.Mobile is PlayerMobile player && player is not Server.CustomBots.PlayerBot) { Get(player).Check(Core.Now, Utility.RandomDouble()); } }
        });
    }
}

[SerializationGenerator(0)]
public partial class HavenWanderingEncounter : Item
{
    [SerializableField(0)] private HavenEncounterJournal _journal;
    [SerializableField(1)] private int _tier;
    [SerializableField(2)] private int _wave = 1;
    [SerializableField(3)] private int _kills;
    [SerializableField(4)] private DateTime _expires;
    [SerializableField(5)] private bool _finished;
    [SerializableField(6)] private List<HavenEncounterMob> _creatures = new();
    [SerializableField(7)] private List<Mobile> _participants = new();
    private Timer _pulse;
    internal const int Boundary = 28;
    [Constructible] public HavenWanderingEncounter() : base(1) { Visible = false; Movable = false; }
    internal static bool CanStart(Mobile player) => player?.Deleted == false && player.Alive && player.Map != null && player.Map != Map.Internal &&
        !player.Region.IsPartOf<GuardedRegion>() && BaseHouse.FindHouseAt(player.Location, player.Map, 20) == null &&
        player.Spell == null && !SpellHelper.CheckCombat(player) && player.Region.AllowSpawn() && player.Map.CanFit(player.Location, 16, checkMobiles: false);
    internal static HavenWanderingEncounter Start(HavenEncounterJournal journal)
    {
        if (!journal.Enabled || journal.Active != null || !CanStart(journal.Owner)) { return null; }
        var encounter = new HavenWanderingEncounter { Journal = journal, Tier = Math.Clamp(journal.Tier, 1, 20), Expires = Core.Now + TimeSpan.FromMinutes(15) };
        encounter.MoveToWorld(journal.Owner.Location, journal.Owner.Map); journal.Active = encounter;
        journal.Owner.SendMessage(0x8A5, $"A roaming encounter gathers nearby! Difficulty {encounter.Tier}. Stay within 28 tiles to fight, or leave to retreat.");
        encounter._pulse = Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), encounter.Tick);
        encounter.Spawn(); return encounter;
    }
    // Restart interruptions cancel cleanly without lowering a player's difficulty.
    [AfterDeserialization] private void Recover() { Finish(false, false); }
    internal void Tick()
    {
        if (Finished || Deleted) { return; }
        var player = Journal?.Owner;
        if (player?.Deleted != false || !player.Alive || player.Map != Map || !player.InRange(Location, Boundary) || Core.Now >= Expires)
        { Finish(false, true); return; }
        if (player.NetState == null) { Finish(false, false); return; }
        Spawn();
    }
    internal void Spawn()
    {
        if (Finished) { return; }
        for (var i = Creatures.Count - 1; i >= 0; i--) { if (Creatures[i]?.Deleted != false) { Creatures.RemoveAt(i); } }
        var target = Wave == 4 ? 1 : Math.Min(3, RequiredKills - Kills);
        var offset = Utility.Random(289);
        for (var attempt = 0; attempt < 289 && Creatures.Count < target; attempt++)
        {
            var cell = (offset + attempt) % 289;
            var x = X + cell % 17 - 8; var y = Y + cell / 17 - 8; var p = new Point3D(x, y, Map.GetAverageZ(x, y));
            if (!Map.CanSpawnMobile(p) || BaseHouse.FindHouseAt(p, Map, 16) != null || !Map.LineOfSight(new Point3D(X, Y, Z + 16), new Point3D(x, y, p.Z + 16))) { continue; }
            var mob = new HavenEncounterMob(Tier, Wave == 4) { Encounter = this, Home = Location, RangeHome = 16 };
            Creatures.Add(mob); mob.MoveToWorld(p, Map); this.MarkDirty();
        }
    }
    internal int RequiredKills => 3 + Tier / 4;
    internal void Credit(Mobile player)
    {
        if (Finished || player is not PlayerMobile || player is Server.CustomBots.PlayerBot || player.Map != Map || !player.InRange(this, Boundary) || Participants.Contains(player)) { return; }
        Participants.Add(player); this.MarkDirty();
    }
    internal void Defeated(HavenEncounterMob mob)
    {
        if (Finished || !Creatures.Remove(mob)) { return; }
        foreach (var right in BaseCreature.GetLootingRights(mob.DamageEntries, mob.HitsMax))
        {
            var player = right.m_Mobile;
            if (right.m_HasRight && player is PlayerMobile && player is not Server.CustomBots.PlayerBot && player.Map == Map && player.InRange(this, Boundary))
            { Credit(player); HavenEncounterJournal.Get(player).KillCredit(Tier); }
        }
        mob.Encounter = null;
        if (Wave == 4) { Finish(true, false); return; }
        Kills++; if (Kills >= RequiredKills) { Wave++; Kills = 0; }
        this.MarkDirty();
    }
    internal void Finish(bool success, bool penalty)
    {
        if (Finished) { return; } Finished = true;
        var owner = Journal?.Owner;
        if (success && Participants.Count == 0) { success = false; penalty = false; }
        Journal?.Outcome(success, penalty);
        if (success)
        {
            var chest = new HavenEncounterChest { Tier = Tier };
            foreach (var participant in Participants)
            {
                if (!participant.Deleted)
                { chest.Claimants.Add(participant); HavenEncounterJournal.Get(participant).ClearCredit(Tier, participant != owner); }
            }
            chest.Fill(); chest.MoveToWorld(Location, Map); chest.BeginExpiry();
            owner?.SendMessage(0x8A5, "Encounter cleared! Open the spoils chest. Your next encounter will be harder.");
            if (Utility.RandomDouble() < Math.Min(.35, .05 + Tier * .015)) { SpawnPet(); }
        }
        else { owner?.SendMessage(penalty ? "The encounter disperses. The next one will be easier." : "The encounter was cancelled without a difficulty penalty."); }
        Delete();
    }
    internal BaseCreature SpawnPet(int forcedTier = -1)
    {
        var kinds = new[] { HavenExpeditionKind.TameEmberwing, HavenExpeditionKind.TameMoonfang, HavenExpeditionKind.TameFrostmane, HavenExpeditionKind.TameVerdantLlama, HavenExpeditionKind.TameStormhorn };
        var pet = HavenTamingMissions.CreatePet(kinds[Utility.Random(kinds.Length)]);
        var rarity = forcedTier >= 0 ? forcedTier : Utility.RandomDouble() < .1 + Tier * .01 ? 3 : Utility.RandomBool() ? 2 : 1;
        HavenPetRarity.Apply(pet, rarity);
        Point3D? spot = null;
        for (var dx = -3; dx <= 3 && spot == null; dx++)
        for (var dy = -3; dy <= 3 && spot == null; dy++)
        {
            var p = new Point3D(X + dx, Y + dy, Map.GetAverageZ(X + dx, Y + dy));
            if (Map.CanSpawnMobile(p) && BaseHouse.FindHouseAt(p, Map, 16) == null) { spot = p; }
        }
        if (spot == null) { pet.Delete(); return null; }
        pet.MoveToWorld(spot.Value, Map);
        if (pet.Backpack == null) { pet.AddItem(new Backpack()); }
        var lease = new HavenEncounterPetLease { Pet = pet, Expires = Core.Now + TimeSpan.FromMinutes(30) }; pet.Backpack.DropItem(lease); lease.Schedule();
        Journal?.Owner?.SendMessage(0x8A5, "A rare custom animal has appeared. You have 30 minutes to tame it; ordinary taming requirements apply.");
        return pet;
    }
    public override void OnDelete()
    {
        _pulse?.Stop(); _pulse = null;
        foreach (var mob in Creatures) { if (mob != null) { mob.Encounter = null; mob.Delete(); } }
        Creatures.Clear(); Participants.Clear();
        if (Journal?.Active == this) { Journal.Active = null; }
        Journal = null; base.OnDelete();
    }
}

[SerializationGenerator(0)]
public partial class HavenEncounterMob : BaseCreature
{
    [SerializableField(0)] private HavenWanderingEncounter _encounter;
    [Constructible] public HavenEncounterMob() : this(1, false) { }
    public HavenEncounterMob(int tier, bool boss) : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = boss ? "the roaming invasion champion" : "a roaming invader"; Body = boss ? 79 : Utility.RandomList(7, 17, 18, 54);
        SetStr(80 + tier * 12); SetDex(70 + tier * 3); SetInt(40);
        SetHits(boss ? 350 + tier * 150 : 70 + tier * 25); SetDamage(2 + tier / 3, 5 + tier / 2);
        SetSkill(SkillName.Wrestling, 40 + tier * 3); SetSkill(SkillName.Tactics, 40 + tier * 3);
        Karma = -1000; Fame = 1000; Tamable = false;
        PackGold(100 + tier * 50, 250 + tier * 100); PackItem(new Amber(Utility.RandomMinMax(1, 3)));
    }
    public override void OnThink()
    {
        if (Encounter?.Deleted == false && (Map != Encounter.Map || !InRange(Encounter.Location, 20)))
        { Combatant = null; MoveToWorld(Encounter.Location, Encounter.Map); }
        base.OnThink();
    }
    public override void OnDeath(Container corpse) { Encounter?.Defeated(this); base.OnDeath(corpse); }
    public override void OnDelete() { Encounter = null; base.OnDelete(); }
}

[SerializationGenerator(0)]
public partial class HavenEncounterChest : MetalGoldenChest
{
    public override bool IsDecoContainer => false;
    [SerializableField(0)] private List<Mobile> _claimants = new();
    [SerializableField(1)] private int _tier;
    [SerializableField(2)] private DateTime _expires;
    private Timer _timer;
    [Constructible] public HavenEncounterChest() { Name = "Roaming encounter spoils"; Movable = false; Locked = false; }
    internal void Fill()
    {
        DropItem(new Gold(5000 + Tier * 1500)); DropItem(new Diamond(5 + Tier)); DropItem(new AstralShard(1 + Tier / 4));
        DropItem(HavenWorldDiscoveries.Clothing(Math.Min(.999, .60 + Tier * .02), Utility.Random(7)));
        if (Utility.RandomDouble() < .25) { DropItem(new RunicHammer(Tier >= 12 ? CraftResource.Agapite : Tier >= 6 ? CraftResource.Bronze : CraftResource.DullCopper, 10 + Tier)); }
        if (Utility.RandomDouble() < .25 && Recipe.Recipes.Count > 0)
        { var recipes = new List<Recipe>(Recipe.Recipes.Values); DropItem(new RecipeScroll(recipes[Utility.Random(recipes.Count)])); }
        if (Utility.RandomDouble() < .10) { DropItem(Utility.Random(3) switch { 0 => new HavenGoldenShovel(), 1 => new HavenEndlessBandage(), _ => new HavenTideSteedDeed() }); }
    }
    internal bool CanClaim(Mobile from) => from != null && Claimants.Contains(from);
    internal void BeginExpiry() { Expires = Core.Now + TimeSpan.FromMinutes(20); Schedule(); }
    [AfterDeserialization] private void Schedule() { _timer?.Stop(); _timer = Timer.DelayCall(Expires > Core.Now ? Expires - Core.Now : TimeSpan.Zero, Delete); }
    public override void OnDoubleClick(Mobile from) { if (CanClaim(from)) { base.OnDoubleClick(from); } else { from.SendMessage("These spoils belong to the encounter's participants."); } }
    public override bool CheckLift(Mobile from, Item item, ref LRReason reason) => item != this && CanClaim(from) && base.CheckLift(from, item, ref reason);
    public override bool CheckItemUse(Mobile from, Item item) => CanClaim(from) && base.CheckItemUse(from, item);
    public override bool CheckTarget(Mobile from, Server.Targeting.Target target, object targeted) => CanClaim(from) && base.CheckTarget(from, target, targeted);
    public override void OnDelete() { _timer?.Stop(); _timer = null; Claimants.Clear(); base.OnDelete(); }
}

[SerializationGenerator(0)]
public partial class HavenEncounterPetLease : Item
{
    [SerializableField(0)] private BaseCreature _pet;
    [SerializableField(1)] private DateTime _expires;
    private Timer _timer;
    public override bool IsVirtualItem => true;
    [Constructible] public HavenEncounterPetLease() : base(1) { Visible = false; Movable = false; Weight = 0; }
    [AfterDeserialization] internal void Schedule()
    {
        _timer?.Stop(); _timer = Timer.DelayCall(Expires > Core.Now ? Expires - Core.Now : TimeSpan.Zero, Expire);
    }
    internal void Expire()
    { var pet = Pet; Pet = null; if (pet?.Deleted == false && !pet.Controlled && pet.Owners.Count == 0) { pet.Delete(); } Delete(); }
    public override void OnDelete() { _timer?.Stop(); _timer = null; Pet = null; base.OnDelete(); }
}

public sealed class HavenEncounterStatusGump : Gump
{
    private readonly HavenEncounterJournal _journal;
    private readonly int _tab;
    public HavenEncounterStatusGump(HavenEncounterJournal journal, int tab = 0) : base(80, 80)
    {
        _journal = journal; _tab = Math.Clamp(tab, 0, 2);
        AddBackground(0, 0, 625, 470, 9270); AddLabel(25, 22, 1152, "Wanderer's Chronicle");
        AddLabel(330, 22, 2101, $"Encounter points: {journal.Points:N0}");
        var tabs = new[] { "Overview", "Recent log", "Rewards" };
        for (var i = 0; i < 3; i++) { AddButton(25 + i * 195, 60, 4005, 4007, 10 + i); AddLabel(60 + i * 195, 60, _tab == i ? 1152 : 2101, tabs[i]); }
        if (_tab == 0)
        {
            AddLabel(25, 105, 1152, $"Current difficulty: {journal.Tier}/20     Highest unlocked: {journal.HighestTier}/20");
            AddLabel(25, 140, 2101, $"Cleared: {journal.Wins}     Assisted clears: {journal.Assists}     Retreats/failures: {journal.Failures}");
            AddLabel(25, 175, 2101, $"Credited kills: {journal.Kills:N0}     Lifetime points earned: {journal.EarnedPoints:N0}");
            AddLabel(25, 210, 1152, journal.Active is { } active ? $"Active: wave {active.Wave}/4, kills {active.Kills}/{active.RequiredKills}" : "No active encounter");
            AddLabel(25, 250, 2101, "Kills: 1 point + 1 per 5 difficulty levels. Clear: 20 + 5 per level.");
            AddLabel(25, 282, 2101, "Three waves, then a champion. Leave the 28-tile area to retreat.");
            AddLabel(25, 314, 2101, "Wins raise difficulty; defeats lower it. Logout/restart has no penalty.");
            AddLabel(25, 346, 2101, "No encounters while idle, in houses or guarded towns.");
        }
        else if (_tab == 1)
        {
            var lines = new List<string>();
            for (var i = journal.History.Count - 1; i >= 0; i--) { lines.Add(journal.History[i]); }
            AddHtml(25, 105, 575, 285, $"<BASEFONT COLOR=#FFFFFF>{(lines.Count == 0 ? "Your encounter history will appear here." : string.Join("<BR><BR>", lines))}</BASEFONT>", false, true);
        }
        else
        {
            AddLabel(25, 105, 2101, "Select a reward to review it before spending points.");
            for (var i = 0; i < HavenEncounterJournal.RewardNames.Length; i++)
            {
                AddButton(25, 150 + i * 50, 4005, 4007, 100 + i);
                AddLabel(60, 150 + i * 50, 1152, HavenEncounterJournal.RewardNames[i]);
                AddLabel(480, 150 + i * 50, 2101, $"{HavenEncounterJournal.RewardCosts[i]:N0} pts");
            }
            AddLabel(25, 365, 2101, "Summoned pets are wild: you must tame them within 30 minutes.");
        }
        AddButton(25, 423, 4005, 4007, 1); AddLabel(60, 423, 1152, journal.Enabled ? "Disable encounters" : "Enable encounters");
        AddButton(330, 423, 4005, 4007, 2); AddLabel(365, 423, 1152, "Refresh");
        AddButton(505, 423, 4017, 4019, 0); AddLabel(540, 423, 1152, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (info.ButtonID == 0 || _journal.Deleted || _journal.Owner != state.Mobile) { return; }
        if (info.ButtonID == 1) { _journal.Enabled = !_journal.Enabled; if (!_journal.Enabled) { _journal.Active?.Finish(false, false); } }
        if (info.ButtonID >= 100 && info.ButtonID < 104)
        { state.Mobile.SendGump(new HavenEncounterRewardGump(_journal, info.ButtonID - 100)); return; }
        state.Mobile.SendGump(new HavenEncounterStatusGump(_journal, info.ButtonID is >= 10 and <= 12 ? info.ButtonID - 10 : _tab));
    }
}

public sealed class HavenEncounterRewardGump : Gump
{
    private readonly HavenEncounterJournal _journal;
    private readonly int _reward;
    private bool _used;
    public HavenEncounterRewardGump(HavenEncounterJournal journal, int reward) : base(100, 100)
    {
        _journal = journal; _reward = reward;
        AddBackground(0, 0, 525, 225, 9270); AddLabel(25, 25, 1152, HavenEncounterJournal.RewardNames[reward]);
        AddLabel(25, 65, 2101, $"Cost: {HavenEncounterJournal.RewardCosts[reward]:N0} points · Balance: {journal.Points:N0}");
        AddLabel(25, 100, 2101, reward >= 2 ? "Spawns a wild pet nearby. Taming is still required." : "The reward will be placed directly in your backpack.");
        AddButton(25, 170, 4005, 4007, 1); AddLabel(60, 170, 1152, "Redeem");
        AddButton(355, 170, 4017, 4019, 0); AddLabel(390, 170, 1152, "Cancel");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        if (_used || _journal.Deleted || _journal.Owner != state.Mobile) { return; } _used = true;
        if (info.ButtonID == 1) { state.Mobile.SendMessage(_journal.Redeem(state.Mobile, _reward) ? "Reward redeemed." : "Could not redeem: check points, pack space, and a safe outdoor location for pets."); }
        state.Mobile.SendGump(new HavenEncounterStatusGump(_journal, 2));
    }
}
