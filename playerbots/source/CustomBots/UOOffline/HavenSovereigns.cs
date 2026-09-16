using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ModernUO.Serialization;
using Server.Commands;
using Server.CustomBots;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.UOOffline;

// Attached to the character, like the encounter journal. No backpack or bank slots are used.
[SerializationGenerator(0)]
public partial class HavenSovereignAccount : Item
{
    [SerializableField(0)] private long _balance;
    [SerializableField(1)] private long _earned;
    [SerializableField(2)] private int _kills;
    [SerializableField(3)] private List<string> _achievements = new();
    [SerializableField(4)] private List<string> _history = new();
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenSovereignAccount() : base(1) { Visible = false; Movable = false; Weight = 0; }

    internal static HavenSovereignAccount Get(Mobile owner)
    {
        if (!HavenSovereigns.IsPlayer(owner)) { return null; }
        foreach (var item in owner.Items) { if (item is HavenSovereignAccount account) { return account; } }
        var created = new HavenSovereignAccount(); owner.AddItem(created); return created;
    }
    internal bool Award(string key, string title, int amount, bool notify = true)
    {
        if (amount <= 0 || Parent is not Mobile owner || !HavenSovereigns.IsPlayer(owner) ||
            key != null && Achievements.Contains(key) || Balance > long.MaxValue - amount || Earned > long.MaxValue - amount)
        { return false; }
        if (key != null) { Achievements.Add(key); }
        Balance += amount; Earned += amount;
        Log($"+{amount:N0}  {title}");
        if (notify) { owner.SendMessage(0x482, $"{title}: +{amount:N0} Sovereigns. Balance {Balance:N0}. Use [store."); }
        return true;
    }
    internal void Log(string text)
    {
        History.Add($"{Core.Now:MM-dd HH:mm} UTC | {text}");
        while (History.Count > 60) { History.RemoveAt(0); }
        this.MarkDirty();
    }
}

public static class HavenSovereigns
{
    private static readonly ConditionalWeakTable<BaseCreature, HashSet<Serial>> KillCredits = new();
    internal static bool IsPlayer(Mobile owner) => owner is PlayerMobile { Deleted: false } and not PlayerBot;
    public static void Initialize()
    {
        CommandSystem.Register("store", AccessLevel.Player, e => HavenSovereignStore.DisplayTo(e.Mobile));
        CommandSystem.Register("sovereigns", AccessLevel.Player, e => HavenSovereignStore.DisplayTo(e.Mobile));
        CommandSystem.Register("achievements", AccessLevel.Player, e => HavenSovereignStore.DisplayTo(e.Mobile, 1));
        Timer.DelayCall(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3), () =>
        {
            foreach (var state in NetState.Instances)
            { if (IsPlayer(state.Mobile) && state.Mobile.Alive) { CheckProgress(state.Mobile); } }
        });
    }
    internal static void CheckProgress(Mobile owner)
    {
        if (!IsPlayer(owner) || !owner.Alive || owner.Map == null || owner.Map == Map.Internal) { return; }
        var account = HavenSovereignAccount.Get(owner);
        var before = account.Earned;
        var countBefore = account.Achievements.Count;
        void Milestone(string key, string title, int amount) => account.Award(key, title, amount, false);
        for (var i = account.Achievements.Count - 1; i >= 0; i--)
        {
            var key = account.Achievements[i];
            if (!key.StartsWith("visit:", StringComparison.Ordinal)) { continue; }
            var upgraded = "visit2:" + key[6..];
            account.Award(upgraded, "Discovery reward increase: " + key[6..], key.Contains(":town:", StringComparison.Ordinal) ? 15 : 30, false);
            account.Achievements.RemoveAt(i); account.MarkDirty();
        }
        Milestone("welcome", "Welcome to Haven", 25);
        // Use the outer named town/dungeon, not each inn or dungeon sub-room.
        Region destination = null;
        for (var region = owner.Region; region != null; region = region.Parent)
        { if (region is TownRegion or DungeonRegion && !string.IsNullOrWhiteSpace(region.Name)) { destination = region; } }
        if (destination != null)
        {
            var kind = destination is DungeonRegion ? "dungeon" : "town";
            Milestone($"visit2:{owner.Map.MapID}:{kind}:{destination.Name}", $"Discovered {destination.Name} ({owner.Map.Name})", kind == "town" ? 25 : 50);
        }
        for (var i = 0; i < owner.Skills.Length; i++)
        {
            var skill = owner.Skills[i];
            if (skill.Base >= 50) { Milestone($"skill:{i}:50", $"Adept {skill.Name} (50)", 5); }
            if (skill.Base >= 100) { Milestone($"skill:{i}:100", $"Grandmaster {skill.Name} (100)", 25); }
        }
        if (owner.RawStr >= 100) { Milestone("stat:str:100", "100 base Strength", 10); }
        if (owner.RawDex >= 100) { Milestone("stat:dex:100", "100 base Dexterity", 10); }
        if (owner.RawInt >= 100) { Milestone("stat:int:100", "100 base Intelligence", 10); }
        if (!account.Achievements.Contains("pet:bond"))
        {
            foreach (var pet in owner.Map.GetMobilesInRange<BaseCreature>(owner.Location, 12))
            {
                if (!pet.Controlled || pet.ControlMaster != owner || pet.Summoned || pet is HavenCompanion) { continue; }
                Milestone("pet:first", "An animal companion", 15);
                if (pet.IsBonded) { Milestone("pet:bond", "A lasting bond", 25); break; }
            }
        }
        if (account.Earned > before)
        { owner.SendMessage(0x482, $"{account.Achievements.Count - countBefore} new achievement(s): +{account.Earned - before:N0} Sovereigns. Use [achievements or [store."); }
    }

    // Called by the normal kill-rights path, including owners credited for pet/companion damage.
    public static void OnMonsterKilled(BaseCreature creature, Mobile owner)
    {
        if (!IsPlayer(owner) || creature == null || creature.Controlled || creature.Summoned || creature.NoKillAwards ||
            creature.IsBonded || creature.Owners.Count != 0 || creature.IsInvulnerable || creature.Blessed ||
            creature is BaseVendor or HavenTrainingSentinel or HavenCompanion || creature.HitsMax < 100 || creature.Karma >= 0 ||
            owner.Map != creature.Map || !owner.InRange(creature, 18)) { return; }
        if (!KillCredits.GetOrCreateValue(creature).Add(owner.Serial)) { return; }
        var account = HavenSovereignAccount.Get(owner);
        if (account.Kills < int.MaxValue) { account.Kills++; }
        foreach (var milestone in KillMilestones)
        { if (account.Kills >= milestone) { account.Award($"kills:{milestone}", $"Defeated {milestone:N0} monsters", milestone == 1 ? 10 : milestone / 10 + 10); } }
        if (creature is BaseChampion or HavenScalis || creature.HitsMax >= 4000)
        { account.Award(null, $"Boss defeated: {creature.Name ?? creature.GetType().Name}", creature.HitsMax >= 20000 ? 100 : 50); }
    }
    private static readonly int[] KillMilestones = [1, 25, 100, 500, 1000, 5000, 10000];

    internal static void EncounterCleared(Mobile owner, int tier)
    {
        var account = HavenSovereignAccount.Get(owner);
        if (account == null) { return; }
        account.Award("encounter:first", "First wandering encounter cleared", 25);
        account.Award(null, $"Tier {tier} encounter cleared", 10 + Math.Clamp(tier, 1, 20) * 2);
    }
}
