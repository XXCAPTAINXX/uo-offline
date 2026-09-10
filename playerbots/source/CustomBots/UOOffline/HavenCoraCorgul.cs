using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenCora : HavenScalis
{
    private DateTime _nextRift;
    private DateTime _nextBlink;
    [Constructible]
    public HavenCora()
    {
        Name = "Cora the Sorceress"; Body = 401; Female = true; HairItemID = 0x2045; HairHue = 452;
        CanSwim = false; CantWalk = false; SetHits(35000); SetStr(930); SetInt(930); SetDex(125);
        foreach (var type in new[] { ResistanceType.Physical, ResistanceType.Fire, ResistanceType.Cold, ResistanceType.Poison, ResistanceType.Energy }) { SetResistance(type, 60); }
        AddItem(new WildStaff { Movable = false, Hue = 1971 }); AddItem(new ThighBoots { Movable = false, Hue = 1910 });
        AddItem(new ChainLegs { Movable = false, Hue = 1936 }); AddItem(new LeatherBustierArms { Movable = false, Hue = 1947 });
        SetDamage(17, 25); Fame = 32000; Karma = -32000; ResetCora();
    }
    public override string CorpseName => "Cora's remains";
    protected override int FirstArtifact => 11;
    protected override int ArtifactCount => 5;
    protected override int RewardGold => 30000;
    protected override bool DropsSoulForge => false;
    [AfterDeserialization]
    private void ResetCora() { _nextRift = Core.Now + TimeSpan.FromSeconds(15); _nextBlink = Core.Now + TimeSpan.FromSeconds(30); }
    protected override void ExtraRewards(PlayerMobile player)
    { HavenFrontierSupport.Deliver(player, new TreasureMap(5, Map)); }
    protected override void UsePowers()
    {
        if (Home != Point3D.Zero && !InRange(Home, 28)) { Combatant = null; MoveToWorld(Home, Map); }
        if (Combatant is not Mobile target || !Engaged(target)) { return; }
        if (Core.Now >= _nextRift)
        {
            _nextRift = Core.Now + TimeSpan.FromSeconds(18);
            var count = 0;
            foreach (var enemy in GetMobilesInRange(12))
            {
                if (!Engaged(enemy)) { continue; }
                var rift = new HavenCoraRift { Cora = this, Armed = Core.Now + TimeSpan.FromSeconds(2), Expires = Core.Now + TimeSpan.FromSeconds(9) };
                rift.MoveToWorld(enemy.Location, Map); rift.Start();
                enemy.SendMessage("A violet rift forms beneath you. Move away before it drains your mana!");
                if (++count >= 5) { break; }
            }
        }
        if (Core.Now >= _nextBlink)
        {
            _nextBlink = Core.Now + TimeSpan.FromSeconds(30);
            var p = HavenBossLair.Floor(Map, target.Location, 2);
            if (p.HasValue && Utility.InRange(p.Value, Home, 28)) { FixedEffect(0x3728, 10, 12); MoveToWorld(p.Value, Map); }
        }
    }
}

[SerializationGenerator(0)]
public partial class HavenCoraRift : Item
{
    [SerializableField(0)] private HavenCora _cora;
    [SerializableField(1)] private DateTime _armed;
    [SerializableField(2)] private DateTime _expires;
    private Timer _timer;
    [Constructible]
    public HavenCoraRift() : base(0x398C) { Name = "Cora's draining rift"; Movable = false; Hue = 0x48E; }
    [AfterDeserialization]
    internal void Start() { _timer?.Stop(); _timer = Timer.DelayCall(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), Pulse); }
    internal void Pulse()
    {
        if (Deleted || Cora?.Deleted != false || !Cora.Alive || Core.Now >= Expires) { Delete(); return; }
        if (Core.Now < Armed) { return; }
        using var victims = PooledRefList<Mobile>.Create();
        foreach (var target in GetMobilesInRange(1)) { if (Cora.Engaged(target) && Math.Abs(target.Z - Z) < 12) { victims.Add(target); } }
        foreach (var target in victims)
        { target.Mana = Math.Max(0, target.Mana - 20); Cora.DoHarmful(target); AOS.Damage(target, Cora, 24, 0, 0, 0, 0, 100); }
    }
    public override void OnDelete() { _timer?.Stop(); _timer = null; Cora = null; base.OnDelete(); }
}

[SerializationGenerator(0)]
public partial class HavenCorgul : HavenScalis
{
    private DateTime _nextPull;
    private DateTime _nextGuards;
    [Constructible]
    public HavenCorgul()
    {
        Name = "Corgul the Soulbinder"; Body = 76; Hue = 0x455; BaseSoundID = 366;
        CanSwim = false; CantWalk = false; SetHits(50000); SetStr(1000); SetInt(600); SetDex(150); SetDamage(19, 26);
        foreach (var type in new[] { ResistanceType.Physical, ResistanceType.Fire, ResistanceType.Cold, ResistanceType.Poison, ResistanceType.Energy }) { SetResistance(type, 65); }
        ResetCorgul();
    }
    public override string CorpseName => "Corgul's remains";
    protected override int FirstArtifact => 4;
    protected override int ArtifactCount => 7;
    protected override int RewardGold => 50000;
    protected override bool DropsSoulForge => false;
    [AfterDeserialization]
    private void ResetCorgul() { _nextPull = Core.Now + TimeSpan.FromSeconds(20); _nextGuards = Core.Now + TimeSpan.FromSeconds(30); }
    protected override void ExtraRewards(PlayerMobile player)
    {
        HavenFrontierSupport.Deliver(player, new TreasureMap(6, Map));
        HavenFrontierSupport.Deliver(player, new ScrollofTranscendence(SkillName.Tactics, Map == Map.Felucca ? 1.0 : .5));
    }
    internal void Guards()
    {
        Eels.RemoveAll(m => m == null || m.Deleted || !m.Alive);
        for (var i = Eels.Count; i < 6; i++)
        {
            var near = new Point3D(Home.X + Utility.RandomMinMax(-12, 12), Home.Y + Utility.RandomMinMax(-12, 12), Home.Z);
            var p = HavenBossLair.Floor(Map, near, 3); if (!p.HasValue) { continue; }
            var guard = new HavenSoulbound { Boss = this, Home = p.Value, RangeHome = 15, Expires = Core.Now + TimeSpan.FromHours(1) };
            guard.Configure(i % 3); guard.MoveToWorld(p.Value, Map); Eels.Add(guard);
        }
        this.MarkDirty();
    }
    protected override void UsePowers()
    {
        if (Home != Point3D.Zero && !InRange(Home, 45)) { Combatant = null; MoveToWorld(Home, Map); }
        if (Combatant is not Mobile target || !Engaged(target)) { return; }
        if (Core.Now >= _nextGuards) { _nextGuards = Core.Now + TimeSpan.FromSeconds(45); Guards(); }
        if (Core.Now < _nextPull) { return; }
        _nextPull = Core.Now + TimeSpan.FromSeconds(22);
        using var victims = PooledRefList<Mobile>.Create();
        foreach (var enemy in GetMobilesInRange(12)) { if (Engaged(enemy)) { victims.Add(enemy); } }
        foreach (var enemy in victims)
        {
            var p = HavenBossLair.Floor(Map, new Point3D(X + Utility.RandomMinMax(-2, 2), Y + Utility.RandomMinMax(-2, 2), Z), 2);
            if (p.HasValue && !InRange(enemy, 2)) { enemy.MoveToWorld(p.Value, Map); }
            enemy.SendMessage("Corgul's soul vortex pulls you close!"); DoHarmful(enemy); enemy.FixedEffect(0x3728, 10, 12);
            AOS.Damage(enemy, this, 30, 0, 0, 30, 40, 30);
        }
    }
}

[SerializationGenerator(0)]
public partial class HavenSoulbound : HavenScalisEel
{
    [Constructible]
    public HavenSoulbound() { CanSwim = false; Body = 400; Hue = 0x455; SetHits(700); SetStr(180); SetDamage(7, 12); }
    internal void Configure(int role)
    {
        Name = role == 0 ? "a soulbound pirate" : role == 1 ? "a soulbound battle mage" : "a bound soul";
        if (role == 1) { AI = AIType.AI_Mage; SetInt(250); SetSkill(SkillName.Magery, 100); SetSkill(SkillName.EvalInt, 100); }
        else if (role == 2) { Body = 26; }
        else { AddItem(new Cutlass { Movable = false }); AddItem(new Shirt(0x455) { Movable = false }); }
    }
    public override Poison HitPoison => null;
    public override void GenerateLoot() { AddLoot(LootPack.Average); }
}
