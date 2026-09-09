using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenIslandTrial : Item
{
    [SerializableField(0)] private int _stage;
    [SerializableField(1)] private int _kills;
    [SerializableField(2)] private List<HavenTrialCreature> _creatures = new();
    [SerializableField(3)] private DateTime _readyAt;
    [SerializableField(4)] private DateTime _lastActivity;
    private Timer _pulse;
    public static readonly Point3D Site = new(3474, 2723, 25);

    [Constructible]
    public HavenIslandTrial() : base(0x1F2A)
    {
        Name = "Haven's woodland trial"; Hue = 0x482; Movable = false;
    }
    public static void Initialize() => Timer.DelayCall(TimeSpan.FromSeconds(20), Ensure);
    internal static void Ensure()
    {
        foreach (var existing in Map.Trammel.GetItemsInRange<HavenIslandTrial>(Site, 12)) { return; }
        if (FindSite(Site, out var location)) { new HavenIslandTrial().MoveToWorld(location, Map.Trammel); }
    }
    internal static bool FindSite(Point3D preferred, out Point3D location)
    {
        if (HavenRecovery.FindLocation(preferred, out location, 8) && BaseHouse.FindHouseAt(location, Map.Trammel, 16) == null) { return true; }
        return false;
    }
    [AfterDeserialization]
    private void Resume() { if (Stage > 0) { StartPulse(); } }
    private void StartPulse()
    {
        _pulse?.Stop();
        _pulse = Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), Tick);
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (!from.Alive || from.Map != Map || !from.InRange(this, 3) || !from.InLOS(this)) { return; }
        if (Stage > 0) { from.SendMessage($"Woodland trial: stage {Stage}/4, {Kills}/6 defeated. The final stage is the champion."); return; }
        if (Core.Now < ReadyAt) { from.SendMessage("The woodland trial will be ready again shortly (two minutes between victories)."); return; }
        Stage = 1; Kills = 0; LastActivity = Core.Now;
        from.SendMessage("The woodland trial begins! Defeat six creatures in each of three waves, then the champion. Stay near this stone.");
        StartPulse(); Tick(); InvalidateProperties();
    }
    internal void Tick()
    {
        if (Deleted || Stage == 0 || Map == null || Map == Map.Internal) { return; }
        if (Core.Now - LastActivity > TimeSpan.FromMinutes(15)) { Reset(); return; }
        for (var i = Creatures.Count - 1; i >= 0; i--)
        {
            var creature = Creatures[i];
            if (creature == null || creature.Deleted) { Creatures.RemoveAt(i); continue; }
            if (creature.Map != Map || !creature.InRange(Location, 18))
            {
                if (FindSite(Location, out var home)) { creature.MoveToWorld(home, Map); }
            }
        }
        var desired = Stage == 4 ? 1 : Math.Min(3, 6 - Kills);
        for (var attempt = 0; attempt < 48 && Creatures.Count < desired; attempt++)
        {
            var preferred = new Point3D(X + Utility.RandomMinMax(-5, 5), Y + Utility.RandomMinMax(-5, 5), Z);
            if (!FindSite(preferred, out var point) || !Map.LineOfSight(new Point3D(X, Y, Z + 14), new Point3D(point.X, point.Y, point.Z + 14))) { continue; }
            var creature = new HavenTrialCreature(Stage) { Trial = this, Home = Location, RangeHome = 8 };
            Creatures.Add(creature); creature.MoveToWorld(point, Map); this.MarkDirty();
        }
    }
    internal void Defeated(HavenTrialCreature creature)
    {
        if (Deleted || Stage == 0 || creature.Trial != this || !Creatures.Remove(creature)) { return; }
        LastActivity = Core.Now;
        if (Stage == 4)
        {
            // Standard damage rights include damage credited through controlled pets.
            foreach (var right in BaseCreature.GetLootingRights(creature.DamageEntries, creature.HitsMax))
            {
                if (right.m_HasRight && right.m_Mobile is PlayerMobile player && player is not Server.CustomBots.PlayerBot &&
                    player.Map == Map && player.InRange(Location, 24))
                {
                    HavenAstralRewards.Award(player, 5);
                    player.SendMessage("Woodland trial complete! Five Astral shards awarded; loot the champion for gold, marks and five power scrolls.");
                }
            }
            Reset(); ReadyAt = Core.Now + TimeSpan.FromMinutes(2);
        }
        else
        {
            Kills++;
            if (Kills >= 6) { Stage++; Kills = 0; }
        }
        this.MarkDirty(); InvalidateProperties();
    }
    private void Reset()
    {
        Stage = 0; Kills = 0; _pulse?.Stop(); _pulse = null;
        foreach (var creature in Creatures) { if (creature?.Deleted == false) { creature.Trial = null; creature.Delete(); } }
        Creatures.Clear(); this.MarkDirty(); InvalidateProperties();
    }
    public override void OnDelete() { Reset(); base.OnDelete(); }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Beginner champion trial: three short waves and one boss"}");
        list.Add($"{"Rewards: 25,000-40,000 gold, five 105/110 scrolls, 20 Haven marks, 5 Astral shards"}");
        list.Add($"{"Stage:"} {Stage}/4\t{"Wave kills:"} {Kills}/6");
        list.Add($"{"Double-click to start or check progress"}");
    }
}

[SerializationGenerator(0)]
public partial class HavenTrialCreature : BaseCreature
{
    [SerializableField(0)] private HavenIslandTrial _trial;
    [SerializableField(1)] private int _trialStage;
    [Constructible]
    public HavenTrialCreature(int stage = 1) : base(AIType.AI_Melee)
    {
        TrialStage = Math.Clamp(stage, 1, 4);
        Name = TrialStage switch { 1 => "a restless woodland spirit", 2 => "a thorn guardian", 3 => "a woodland sentinel", _ => "Briarheart, the woodland champion" };
        Body = TrialStage == 4 ? 47 : TrialStage == 1 ? 8 : 47;
        Hue = TrialStage == 4 ? 0x489 : 0x59B;
        SetStr(60); SetDex(40); SetInt(20);
        SetHits(TrialStage == 4 ? 450 : 60 + TrialStage * 20);
        SetDamage(TrialStage == 4 ? 3 : 1, TrialStage == 4 ? 6 : 3);
        SetDamageType(ResistanceType.Physical, 100);
        SetSkill(SkillName.Wrestling, 30 + TrialStage * 5); SetSkill(SkillName.Tactics, 30);
        SetSkill(SkillName.MagicResist, 20);
        Fame = 500; Karma = -500; Tamable = false;
    }
    public override bool AlwaysAttackable => true;
    public override void GenerateLoot()
    {
        if (m_Spawning) { return; }
        PackGold(TrialStage == 4 ? 25000 : 30, TrialStage == 4 ? 40000 : 60);
        if (TrialStage == 4) { PackItem(new HavenMark(20)); for (var i = 0; i < 5; i++) { PackItem(PowerScroll.CreateRandomNoCraft(5, 10)); } }
    }
    public override void OnDeath(Container corpse) { Trial?.Defeated(this); base.OnDeath(corpse); }
    public override void OnAfterDelete() { Trial = null; base.OnAfterDelete(); }
}
