using System;
using System.Collections.Generic;
using System.Linq;
using Server.Commands;
using Server.Engines.Craft;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Spells;

namespace Server.HavenPrototype {

public partial class HavenEncounterJournal : Item
{
    public Mobile Owner;
    public bool Enabled = true;
    public int Tier = 1;
    public int Wins;
    public int Failures;
    public DateTime NextRoll;
    public Point3D LastPosition;
    public HavenWanderingEncounter Active;
    public int Kills;
    public int Points;
    public int EarnedPoints;
    public int HighestTier = 1;
    public List<string> History = new List<string>();
    public int Assists;
    
    [Constructable] public HavenEncounterJournal() : base(1) { Visible = false; Movable = false; Weight = 0; LootType = LootType.Blessed; }
    internal static HavenEncounterJournal Get(Mobile owner)
    {
        foreach (var item in owner.Items) { if (item is HavenEncounterJournal journal) { return journal; } }
        var created = new HavenEncounterJournal { Owner = owner, LastPosition = owner.Location, NextRoll = DateTime.UtcNow + TimeSpan.FromMinutes(Utility.RandomMinMax(12, 20)) };
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
        Active = null; NextRoll = DateTime.UtcNow + TimeSpan.FromMinutes(Utility.RandomMinMax(12, 20));
    }
    internal void Log(string message)
    {
        History.Add($"{DateTime.UtcNow:MM-dd HH:mm} UTC  {message}");
        while (History.Count > 20) { History.RemoveAt(0); }
        
    }
    internal void Earn(int amount)
    { amount = Math.Max(0, amount); Points += amount; EarnedPoints += amount; }
    internal void KillCredit(int tier) { Kills++; Earn(1 + tier / 5); }
    internal void ClearCredit(int tier, bool assisted)
    {
        
        HavenSovereigns.Award(Owner as PlayerMobile,null,"Roaming invasion tier "+tier,1+tier/5); var amount = 20 + tier * 5; Earn(amount);
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
            Item item = reward == 0 ? (Item)new AstralShard(25) : new RunicHammer(CraftResource.Agapite, 25);
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
            e.Mobile.CloseGump(typeof(HavenEncounterStatusGump)); e.Mobile.SendGump(new HavenEncounterStatusGump(journal));
        });
        Timer.DelayCall(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), () =>
        {
            if(!HavenPreview.Enabled)return;
            foreach (var state in NetState.Instances.ToArray())
            { if (state.Mobile is PlayerMobile player) { Get(player).Check(DateTime.UtcNow, Utility.RandomDouble()); } }
        });
    }

public HavenEncounterJournal(Serial serial):base(serial){}
public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Owner);w.Write(Enabled);w.Write(Tier);w.Write(Wins);w.Write(Failures);w.Write(NextRoll);w.Write(LastPosition);w.Write(Active);w.Write(Kills);w.Write(Points);w.Write(EarnedPoints);w.Write(HighestTier);w.Write(History.Count);foreach(var value in History)w.Write(value);w.Write(Assists);}
public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Owner=r.ReadMobile();Enabled=r.ReadBool();Tier=r.ReadInt();Wins=r.ReadInt();Failures=r.ReadInt();NextRoll=r.ReadDateTime();LastPosition=r.ReadPoint3D();Active=r.ReadItem() as HavenWanderingEncounter;Kills=r.ReadInt();Points=r.ReadInt();EarnedPoints=r.ReadInt();HighestTier=r.ReadInt();{int count=r.ReadInt();if(count<0||count>10000)throw new InvalidOperationException("Invalid encounter list");for(int i=0;i<count;i++){var value=r.ReadString();if(value!=null)History.Add(value);}}Assists=r.ReadInt();}
}

public partial class HavenWanderingEncounter : Item
{
    public HavenEncounterJournal Journal;
    public int Tier;
    public int Wave = 1;
    public int Kills;
    public DateTime Expires;
    public bool Finished;
    public List<HavenEncounterMob> Creatures = new List<HavenEncounterMob>();
    public List<Mobile> Participants = new List<Mobile>();
    private Timer _pulse;
    internal const int Boundary = 28;
    [Constructable] public HavenWanderingEncounter() : base(1) { Visible = false; Movable = false; }
    internal static bool CanStart(Mobile player) => player?.Deleted == false && player.Alive && player.Map != null && player.Map != Map.Internal &&
        !player.Region.IsPartOf(typeof(GuardedRegion)) && !player.Region.IsPartOf(typeof(DungeonRegion)) && BaseHouse.FindHouseAt(player.Location, player.Map, 20) == null &&
        player.Spell == null && !SpellHelper.CheckCombat(player) && player.Map.CanFit(player.Location, 16, false, false);
    internal static HavenWanderingEncounter Start(HavenEncounterJournal journal)
    {
        if (!journal.Enabled || journal.Active != null || !CanStart(journal.Owner)) { return null; }
        var encounter = new HavenWanderingEncounter { Journal = journal, Tier = Math.Max(1,Math.Min(20,journal.Tier)), Expires = DateTime.UtcNow + TimeSpan.FromMinutes(15) };
        encounter.MoveToWorld(journal.Owner.Location, journal.Owner.Map); journal.Active = encounter;
        journal.Owner.SendMessage(0x8A5, $"A roaming encounter gathers nearby! Difficulty {encounter.Tier}. Stay within 28 tiles to fight, or leave to retreat.");
        encounter._pulse = Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), encounter.Tick);
        encounter.Spawn(); return encounter;
    }
    // Restart interruptions cancel cleanly without lowering a player's difficulty.
    private void Recover() { Finish(false, false); }
    internal void Tick()
    {
        if (Finished || Deleted) { return; }
        var player = Journal?.Owner;
        if (player?.Deleted != false || !player.Alive || player.Map != Map || !player.InRange(Location, Boundary) || DateTime.UtcNow >= Expires)
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
            if (Region.Find(p,Map).IsPartOf(typeof(GuardedRegion)) || !Map.CanSpawnMobile(p) || BaseHouse.FindHouseAt(p, Map, 16) != null || !Map.LineOfSight(new Point3D(X, Y, Z + 16), new Point3D(x, y, p.Z + 16))) { continue; }
            var mob = new HavenEncounterMob(Tier, Wave == 4) { Encounter = this, Home = Location, RangeHome = 16 };
            Creatures.Add(mob); mob.MoveToWorld(p, Map); 
        }
    }
    internal int RequiredKills => 3 + Tier / 4;
    internal void Credit(Mobile player)
    {
        if (Finished || !(player is PlayerMobile) || player.Map != Map || !player.InRange(this, Boundary) || Participants.Contains(player)) { return; }
        Participants.Add(player); 
    }
    internal void Defeated(HavenEncounterMob mob)
    {
        if (Finished || !Creatures.Remove(mob)) { return; }
        foreach (var right in mob.GetLootingRights())
        {
            var player = right.m_Mobile;
            if (right.m_HasRight && player is PlayerMobile && player.Map == Map && player.InRange(this, Boundary))
            { Credit(player); HavenEncounterJournal.Get(player).KillCredit(Tier); }
        }
        mob.Encounter = null;
        if (Wave == 4) { Finish(true, false); return; }
        Kills++; if (Kills >= RequiredKills) { Wave++; Kills = 0; }
        
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
        Func<BaseCreature>[] kinds = {()=>new HavenEmberwing(),()=>new HavenMoonfang(),()=>new HavenFrostmane(),()=>new HavenVerdantLlama()};
        var pet = kinds[Utility.Random(kinds.Length)]();
        var rarity = forcedTier >= 0 ? forcedTier : Utility.RandomDouble() < .1 + Tier * .01 ? 3 : Utility.RandomBool() ? 2 : 1;
        HavenPetMissions.ApplyRarity(pet, rarity);
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
        var lease = new HavenEncounterPetLease { Pet = pet, Expires = DateTime.UtcNow + TimeSpan.FromMinutes(30) }; pet.Backpack.DropItem(lease); lease.Schedule();
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

public HavenWanderingEncounter(Serial serial):base(serial){}
public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Journal);w.Write(Tier);w.Write(Wave);w.Write(Kills);w.Write(Expires);w.Write(Finished);w.Write(Creatures.Count);foreach(var value in Creatures)w.Write(value);w.Write(Participants.Count);foreach(var value in Participants)w.Write(value);}
public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Journal=r.ReadItem() as HavenEncounterJournal;Tier=r.ReadInt();Wave=r.ReadInt();Kills=r.ReadInt();Expires=r.ReadDateTime();Finished=r.ReadBool();{int count=r.ReadInt();if(count<0||count>10000)throw new InvalidOperationException("Invalid encounter list");for(int i=0;i<count;i++){var value=r.ReadMobile() as HavenEncounterMob;if(value!=null)Creatures.Add(value);}}{int count=r.ReadInt();if(count<0||count>10000)throw new InvalidOperationException("Invalid encounter list");for(int i=0;i<count;i++){var value=r.ReadMobile() as Mobile;if(value!=null)Participants.Add(value);}}Timer.DelayCall(TimeSpan.Zero,Recover);}
}

public partial class HavenEncounterMob : BaseCreature
{
    public HavenWanderingEncounter Encounter;
    [Constructable] public HavenEncounterMob() : this(1, false) { }
    public HavenEncounterMob(int tier, bool boss) : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
    {
        Name = boss ? "the roaming invasion champion" : "a roaming invader"; Body = boss ? 79 : Utility.RandomList(7, 17, 18, 54);
        SetStr(80 + tier * 12); SetDex(70 + tier * 3); SetInt(40);
        SetHits(boss ? 350 + tier * 150 : 70 + tier * 25); SetDamage(boss ? 8 + tier / 2 : 2 + tier / 3, boss ? 14 + tier : 5 + tier / 2);
        SetSkill(SkillName.Wrestling, 40 + tier * 3); SetSkill(SkillName.Tactics, 40 + tier * 3);
        Karma = -1000; Fame = 1000; Tamable = false;
        PackGold(100 + tier * 50, 250 + tier * 100); PackItem(new Amber(Utility.RandomMinMax(1, 3)));
    }
    public override bool AlwaysMurderer { get { return true; } }
    public override bool CanBeHarmful(IDamageable target, bool message, bool ignoreOurBlessedness)
    {
        var mobile=target as Mobile;
        if(Encounter==null || Encounter.Deleted || mobile==null || mobile.Map!=Encounter.Map || !mobile.InRange(Encounter, HavenWanderingEncounter.Boundary) || mobile.Region.IsPartOf(typeof(GuardedRegion)) || BaseHouse.FindHouseAt(mobile.Location,mobile.Map,20)!=null)return false;
        return base.CanBeHarmful(target,message,ignoreOurBlessedness);
    }
    public override void OnThink()
    {
        if (Encounter?.Deleted == false && (Map != Encounter.Map || !InRange(Encounter.Location, 20)))
        { Combatant = null; MoveToWorld(Encounter.Location, Encounter.Map); }
        base.OnThink();
    }
    public override void OnDeath(Container corpse) { Encounter?.Defeated(this); base.OnDeath(corpse); }
    public override void OnDelete() { Encounter = null; base.OnDelete(); }

public HavenEncounterMob(Serial serial):base(serial){}
public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Encounter);}
public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Encounter=r.ReadItem() as HavenWanderingEncounter;}
}

public partial class HavenEncounterChest : MetalGoldenChest
{
    
    public List<Mobile> Claimants = new List<Mobile>();
    public int Tier;
    public DateTime Expires;
    private Timer _timer;
    [Constructable] public HavenEncounterChest() { Name = "Roaming encounter spoils"; Movable = false; Locked = false; }
    internal void Fill()
    {
        var gear=HavenEncounterBossLoot.Roll(Tier,Utility.RandomDouble(),Utility.Random(100));if(gear!=null)DropItem(gear);
        DropItem(new Gold(5000 + Tier * 1500)); DropItem(new Diamond(5 + Tier)); DropItem(new AstralShard(1 + Tier / 4));
        DropItem(HavenWorldDiscoveries.Clothing(Math.Min(.999, .60 + Tier * .02), Utility.Random(7), Utility.Random(10)));
        if (Utility.RandomDouble() < .25) { DropItem(new RunicHammer(Tier >= 12 ? CraftResource.Agapite : Tier >= 6 ? CraftResource.Bronze : CraftResource.DullCopper, 10 + Tier)); }
        if (Utility.RandomDouble() < .25 && Recipe.Recipes.Count > 0)
        { var recipes = new List<Recipe>(Recipe.Recipes.Values); DropItem(new RecipeScroll(recipes[Utility.Random(recipes.Count)].ID)); }
        if (Utility.RandomDouble() < .10) { DropItem(new SpecialFishingNet()); }
    }
    internal bool CanClaim(Mobile from) => from != null && Claimants.Contains(from);
    internal void BeginExpiry() { Expires = DateTime.UtcNow + TimeSpan.FromMinutes(20); Schedule(); }
    private void Schedule() { _timer?.Stop(); _timer = Timer.DelayCall(Expires > DateTime.UtcNow ? Expires - DateTime.UtcNow : TimeSpan.Zero, Delete); }
    public override void OnDoubleClick(Mobile from) { if (CanClaim(from)) { base.OnDoubleClick(from); } else { from.SendMessage("These spoils belong to the encounter's participants."); } }
    public override bool CheckLift(Mobile from, Item item, ref LRReason reason) => item != this && CanClaim(from) && base.CheckLift(from, item, ref reason);
    public override bool CheckItemUse(Mobile from, Item item) => CanClaim(from) && base.CheckItemUse(from, item);
    public override bool CheckTarget(Mobile from, Server.Targeting.Target target, object targeted) => CanClaim(from) && base.CheckTarget(from, target, targeted);
    public override void OnDelete() { _timer?.Stop(); _timer = null; Claimants.Clear(); base.OnDelete(); }

public HavenEncounterChest(Serial serial):base(serial){}
public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Claimants.Count);foreach(var value in Claimants)w.Write(value);w.Write(Tier);w.Write(Expires);}
public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();{int count=r.ReadInt();if(count<0||count>10000)throw new InvalidOperationException("Invalid encounter list");for(int i=0;i<count;i++){var value=r.ReadMobile() as Mobile;if(value!=null)Claimants.Add(value);}}Tier=r.ReadInt();Expires=r.ReadDateTime();Timer.DelayCall(TimeSpan.Zero,Schedule);}
}

public partial class HavenEncounterPetLease : Item
{
    public BaseCreature Pet;
    public DateTime Expires;
    private Timer _timer;
    
    [Constructable] public HavenEncounterPetLease() : base(1) { Visible = false; Movable = false; Weight = 0; LootType = LootType.Blessed; }
    internal void Schedule()
    {
        _timer?.Stop(); _timer = Timer.DelayCall(Expires > DateTime.UtcNow ? Expires - DateTime.UtcNow : TimeSpan.Zero, Expire);
    }
    internal void Expire()
    { var pet = Pet; Pet = null; if (pet?.Deleted == false && !pet.Controlled && pet.Owners.Count == 0) { pet.Delete(); } Delete(); }
    public override void OnDelete() { _timer?.Stop(); _timer = null; Pet = null; base.OnDelete(); }

public HavenEncounterPetLease(Serial serial):base(serial){}
public override void Serialize(GenericWriter w){base.Serialize(w);w.Write(0);w.Write(Pet);w.Write(Expires);}
public override void Deserialize(GenericReader r){base.Deserialize(r);r.ReadInt();Pet=r.ReadMobile() as BaseCreature;Expires=r.ReadDateTime();Timer.DelayCall(TimeSpan.Zero,Schedule);}
}

public sealed class HavenEncounterStatusGump : HavenStoneGump
{
    private readonly HavenEncounterJournal _journal;
    private readonly int _tab;
    public HavenEncounterStatusGump(HavenEncounterJournal journal, int tab = 0) : base(80, 80)
    {
        _journal = journal; _tab = Math.Max(0,Math.Min(2,tab));
        AddBackground(0, 0, 625, 470, 9270); AddLabel(25, 22, 1152, "Wanderer's Chronicle");
        AddLabel(330, 22, 2101, $"Encounter points: {journal.Points:N0}");
        var tabs = new[] { "Overview", "Recent log", "Rewards" };
        for (var i = 0; i < 3; i++) { FlatButton(25 + i * 195,60,180,10+i,tabs[i]); }
        if (_tab == 0)
        {
            AddLabel(25, 105, 1152, $"Current difficulty: {journal.Tier}/20     Highest unlocked: {journal.HighestTier}/20");
            AddLabel(25, 140, 2101, $"Cleared: {journal.Wins}     Assisted clears: {journal.Assists}     Retreats/failures: {journal.Failures}");
            AddLabel(25, 175, 2101, $"Credited kills: {journal.Kills:N0}     Lifetime points earned: {journal.EarnedPoints:N0}");
            AddLabel(25, 210, 1152, journal.Active != null ? $"Active: wave {journal.Active.Wave}/4, kills {journal.Active.Kills}/{journal.Active.RequiredKills}" : "No active encounter");
            AddLabel(25, 250, 2101, "Kills: 1 point + 1 per 5 difficulty levels. Clear: 20 + 5 per level.");
            AddLabel(25, 282, 2101, "Three waves, then a champion. Leave the 28-tile area to retreat.");
            AddLabel(25, 314, 2101, "Wins raise difficulty; defeats lower it. Logout/restart has no penalty.");
            AddLabel(25, 346, 2101, "No encounters while idle, in houses, dungeons or guarded towns.");
            AddLabel(25, 375, 2101, "Tier 5+: exclusive gear chance. Tier 12+: spellbooks join the pool.");
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
                AddButton(25, 150 + i * 50, 4005, 4007, 100 + i, GumpButtonType.Reply, 0);
                AddLabel(60, 150 + i * 50, 1152, HavenEncounterJournal.RewardNames[i]);
                AddLabel(480, 150 + i * 50, 2101, $"{HavenEncounterJournal.RewardCosts[i]:N0} pts");
            }
            AddLabel(25, 365, 2101, "Summoned pets are wild: you must tame them within 30 minutes.");
        }
        FlatButton(25,423,240,1,journal.Enabled ? "Disable encounters" : "Enable encounters");
        FlatButton(330,423,130,2,"Refresh");
        FlatButton(505,423,95,0,"Close");
    }
    public override void OnResponse(NetState state, RelayInfo info)
    {
        if (info.ButtonID == 0 || _journal.Deleted || _journal.Owner != state.Mobile) { return; }
        if (info.ButtonID == 1) { _journal.Enabled = !_journal.Enabled; if (!_journal.Enabled) { _journal.Active?.Finish(false, false); } }
        if (info.ButtonID >= 100 && info.ButtonID < 104)
        { state.Mobile.SendGump(new HavenEncounterRewardGump(_journal, info.ButtonID - 100)); return; }
        state.Mobile.SendGump(new HavenEncounterStatusGump(_journal, info.ButtonID >= 10 && info.ButtonID <= 12 ? info.ButtonID - 10 : _tab));
    }
}

public sealed class HavenEncounterRewardGump : HavenStoneGump
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
        AddButton(25, 170, 4005, 4007, 1, GumpButtonType.Reply, 0); AddLabel(60, 170, 1152, "Redeem");
        AddButton(355, 170, 4017, 4019, 0, GumpButtonType.Reply, 0); AddLabel(390, 170, 1152, "Cancel");
    }
    public override void OnResponse(NetState state, RelayInfo info)
    {
        if (_used || _journal.Deleted || _journal.Owner != state.Mobile) { return; } _used = true;
        if (info.ButtonID == 1) { state.Mobile.SendMessage(_journal.Redeem(state.Mobile, _reward) ? "Reward redeemed." : "Could not redeem: check points, pack space, and a safe outdoor location for pets."); }
        state.Mobile.SendGump(new HavenEncounterStatusGump(_journal, 2));
    }
}

}

