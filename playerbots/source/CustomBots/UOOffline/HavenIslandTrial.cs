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
    protected virtual string TrialName => "Haven's island trial";
    protected virtual int ChooseTheme() => Utility.Random(3);
    protected virtual int RoamingRange => 18;
    public static readonly Point3D Site = new(3474, 2723, 25);

    [Constructible]
    public HavenIslandTrial() : base(0x1F2A)
    {
        Name = "Haven's island trial"; Hue = 0x482; Movable = false;
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
    private void Resume() { Name = TrialName; if (Stage > 0) { StartPulse(); } }
    private void StartPulse()
    {
        _pulse?.Stop();
        _pulse = Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), Tick);
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (!from.Alive || from.Map != Map || !from.InRange(this, 3) || !from.InLOS(this)) { return; }
        if (Stage > 0) { from.SendMessage($"{HavenTrialTheme.Label(HavenTrialTheme.Get(this))} trial: stage {Stage}/4, {Kills}/6 defeated. The final stage is the champion."); return; }
        if (Core.Now < ReadyAt) { from.SendMessage("The island trial will be ready again shortly (two minutes between victories)."); return; }
        HavenTrialTheme.Set(this, ChooseTheme());
        HavenTrialParticipants.Get(this).Clear();
        Stage = 1; Kills = 0; LastActivity = Core.Now;
        from.SendMessage($"The {HavenTrialTheme.Label(HavenTrialTheme.Get(this))} trial begins! Defeat six creatures in each of three waves, then the champion. Creatures carry matching crafting resources.");
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
            if (creature.Map != Map || !creature.InRange(Location, RoamingRange))
            {
                if (FindSite(Location, out var home)) { creature.MoveToWorld(home, Map); }
            }
        }
        var desired = Stage == 4 ? 1 : Math.Min(3, 6 - Kills);
        for (var attempt = 0; attempt < 48 && Creatures.Count < desired; attempt++)
        {
            var preferred = new Point3D(X + Utility.RandomMinMax(-5, 5), Y + Utility.RandomMinMax(-5, 5), Z);
            if (!FindSite(preferred, out var point) || !Utility.InRange(point,Location,RoamingRange) || !Map.LineOfSight(new Point3D(X, Y, Z + 14), new Point3D(point.X, point.Y, point.Z + 14))) { continue; }
            var creature = new HavenTrialCreature(Stage, HavenTrialTheme.Get(this)) { Trial = this, Home = Location, RangeHome = 8 };
            Creatures.Add(creature); creature.MoveToWorld(point, Map); this.MarkDirty();
        }
    }
    internal void Defeated(HavenTrialCreature creature)
    {
        if (Deleted || Stage == 0 || creature.Trial != this || !Creatures.Remove(creature)) { return; }
        LastActivity = Core.Now;
        HavenTrialParticipants.Get(this).Record(this, creature);
        if (Stage == 4)
        {
            HavenTrialParticipants.Get(this).Award(HavenTrialTheme.Get(this));
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
        foreach (var item in Items) { if (item is HavenTrialParticipants participants) { participants.Clear(); } }
    }
    public override void OnDelete() { Reset(); base.OnDelete(); }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Beginner champion trial: three short waves and one boss"}");
        list.Add($"{"Theme:"} {HavenTrialTheme.Label(HavenTrialTheme.Get(this))}{"; matching wood, metal or hides"}");
        list.Add($"{"Rewards: 25,000-40,000 gold, five 105/110 scrolls, 20 Haven marks, 5 Astral shards"}");
        list.Add($"{"Each participant receives rewards directly in their backpack"}");
        list.Add($"{"Also includes one Alacrity and one 0.5-2.0 Transcendence scroll per participant"}");
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
    public HavenTrialCreature(int stage = 1, int theme = 0) : base(AIType.AI_Melee)
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
        AddItem(new HavenTrialTheme(theme));
        HavenTrialTheme.Dress(this, theme);
    }
    public override bool AlwaysAttackable => true;
    public override void GenerateLoot()
    {
        if (m_Spawning || TrialStage == 4) { return; }
        PackGold(30, 60);
        PackItem(HavenTrialTheme.ResourceDeed(HavenTrialTheme.Get(this), TrialStage == 4));
    }
    public override void OnDeath(Container corpse)
    {
        Trial?.Defeated(this); base.OnDeath(corpse);
        if (TrialStage < 4 && corpse is Corpse remains) { remains.BeginDecay(TimeSpan.FromSeconds(30)); }
    }
    public override void OnAfterDelete() { Trial = null; base.OnAfterDelete(); }
}
