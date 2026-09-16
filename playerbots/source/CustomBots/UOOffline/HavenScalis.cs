using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.CustomBots;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

// Compatible High Seas encounter for this shard's ML combat engine.
[SerializationGenerator(0)]
public partial class HavenScalis : BaseCreature
{
    [SerializableField(0)] private HavenScalisHunt _hunt;
    [SerializableField(1)] private Dictionary<PlayerMobile, int> _contributions = new();
    [SerializableField(2)] private List<HavenScalisEel> _eels = new();
    [SerializableField(3)] private bool _rewarded;
    private DateTime _nextWave;
    private DateTime _nextEels;
    [Constructible]
    public HavenScalis() : base(AIType.AI_Mage, FightMode.Closest)
    {
        Name = "Osiredon the Scalis Enforcer"; Body = 1068; BaseSoundID = 589;
        CanSwim = true; CantWalk = true; Tamable = false;
        SetStr(850); SetDex(150); SetInt(600); SetHits(60000); SetMana(4000); SetDamage(19, 26);
        SetDamageType(ResistanceType.Physical, 40); SetDamageType(ResistanceType.Cold, 30); SetDamageType(ResistanceType.Energy, 30);
        SetResistance(ResistanceType.Physical, 75); SetResistance(ResistanceType.Fire, 70); SetResistance(ResistanceType.Cold, 80);
        SetResistance(ResistanceType.Poison, 75); SetResistance(ResistanceType.Energy, 70);
        SetSkill(SkillName.Magery, 120); SetSkill(SkillName.EvalInt, 110); SetSkill(SkillName.Meditation, 120);
        SetSkill(SkillName.Wrestling, 125); SetSkill(SkillName.Tactics, 125); SetSkill(SkillName.MagicResist, 120);
        Fame = 25000; Karma = -25000; ResetPowers();
    }
    public override string CorpseName => "the remains of Scalis";
    public override bool AlwaysAttackable => true;
    public override bool BardImmune => true;
    public override Poison PoisonImmune => Poison.Lethal;
    public override int TreasureMapLevel => 6;
    protected virtual int FirstArtifact => 0;
    protected virtual int ArtifactCount => 4;
    protected virtual int RewardGold => 40000;
    protected virtual bool DropsSoulForge => true;
    [AfterDeserialization]
    private void ResetPowers() { _nextWave = Core.Now + TimeSpan.FromSeconds(15); _nextEels = Core.Now + TimeSpan.FromSeconds(25); }
    public override void GenerateLoot() { AddLoot(LootPack.FilthyRich, 8); AddLoot(LootPack.Gems, 8); }
    internal void Credit(Mobile source, int amount)
    {
        if (Rewarded || amount <= 0 || Controlled || Summoned || NoKillAwards) { return; }
        var player = HavenFrontierSupport.Player(source);
        if (player is PlayerBot bot) { player = HavenBotLoot.PlayerOwner(bot) ?? bot; }
        if (player?.Deleted != false || source.Map != Map || !source.InRange(this, 30)) { return; }
        Contributions.TryGetValue(player, out var previous);
        Contributions[player] = Math.Min(HitsMax, previous + Math.Min(amount, HitsMax)); this.MarkDirty();
    }
    public override void OnDamage(int amount, Mobile from, bool willKill)
    { Credit(from, amount); base.OnDamage(amount, from, willKill); }
    internal bool Engaged(Mobile target)
    {
        if (target?.Deleted != false || !target.Alive || target == this || target is HavenScalisEel || target.Map != Map ||
            !InRange(target, 18) || !InLOS(target) || !CanBeHarmful(target, false)) { return false; }
        var player = HavenFrontierSupport.Player(target);
        return target == Combatant || player != null && Contributions.ContainsKey(player);
    }
    public override void OnThink()
    {
        base.OnThink();
        if (Deleted || !Alive || Map == null || Map == Map.Internal) { return; }
        UsePowers();
    }
    protected virtual void UsePowers()
    {
        if (Home != Point3D.Zero && !InRange(Home, 70) && HavenScalisHunt.SeaPoint(Map, Home))
        { Combatant = null; MoveToWorld(Home, Map); }
        if (Core.Now >= _nextWave)
        {
            _nextWave = Core.Now + TimeSpan.FromSeconds(18);
            using var victims = PooledRefList<Mobile>.Create();
            foreach (var target in GetMobilesInRange(12)) { if (Engaged(target)) { victims.Add(target); } }
            foreach (var target in victims)
            {
                DoHarmful(target); target.FixedEffect(0x36BD, 10, 12); target.PlaySound(0x307);
                AOS.Damage(target, this, Utility.RandomMinMax(25, 40), 0, 0, 60, 40, 0);
            }
        }
        if (Core.Now >= _nextEels && Combatant is Mobile enemy && Engaged(enemy))
        { _nextEels = Core.Now + TimeSpan.FromSeconds(25); SpawnEels(enemy); }
    }
    internal void SpawnEels(Mobile target)
    {
        if (!Engaged(target)) { return; }
        Eels.RemoveAll(e => e == null || e.Deleted || !e.Alive);
        for (var count = 0; count < 2 && Eels.Count < 6; count++)
        {
            for (var attempt = 0; attempt < 15; attempt++)
            {
                var p = new Point3D(target.X + Utility.RandomMinMax(-1, 1), target.Y + Utility.RandomMinMax(-1, 1), target.Z);
                if (!Map.CanSpawnMobile(p) && !HavenScalisHunt.SeaPoint(Map, p)) { continue; }
                var eel = new HavenScalisEel { Boss = this, Home = p, RangeHome = 12 };
                Eels.Add(eel); eel.MoveToWorld(p, Map); eel.Combatant = target; break;
            }
        }
        this.MarkDirty();
    }
    internal List<PlayerMobile> RewardRecipients()
    {
        var recipients = new List<PlayerMobile>();
        if (Controlled || Summoned || NoKillAwards) { return recipients; }
        foreach (var pair in Contributions)
        {
            var player = pair.Key;
            if (pair.Value >= 600 && player?.Deleted == false && player.Map == Map && player.InRange(this, 32)) { recipients.Add(player); }
        }
        return recipients;
    }
    internal void Award(double artifactRoll, double forgeRoll, int artifactChoice)
    {
        if (Rewarded) { return; }
        Rewarded = true;
        foreach (var player in RewardRecipients())
        {
            HavenFrontierSupport.Reward(player, RewardGold, 20, 10); ExtraRewards(player);
            if (artifactRoll < .25) { HavenFrontierSupport.Deliver(player, HavenScalisLoot.Artifact(FirstArtifact + Math.Abs(artifactChoice % ArtifactCount))); }
            if (DropsSoulForge && forgeRoll < .05) { HavenFrontierSupport.Deliver(player, new HavenSmallSoulForgeDeed()); }
            player.SendMessage($"{Name} is defeated! Your earned rewards have been delivered to your pack.");
            // Independent rolls for the next qualifying player, without duplicated pet/bot credit.
            artifactRoll = Utility.RandomDouble(); forgeRoll = Utility.RandomDouble(); artifactChoice = Utility.Random(ArtifactCount);
        }
    }
    protected virtual void ExtraRewards(PlayerMobile player)
    {
        HavenFrontierSupport.Deliver(player, new MessageInABottle(Map));
        HavenFrontierSupport.Deliver(player, new SpecialFishingNet());
        HavenFrontierSupport.Deliver(player, new FishingPole());
    }
    public override void OnDeath(Container corpse)
    {
        Award(Utility.RandomDouble(), Utility.RandomDouble(), Utility.Random(ArtifactCount));
        Hunt?.Finished(this); HavenBossLair.Finished(this); DeleteEels(); base.OnDeath(corpse);
    }
    private void DeleteEels()
    {
        foreach (var eel in Eels.ToArray()) { if (eel?.Deleted == false) { eel.Boss = null; eel.Delete(); } }
        Eels.Clear();
    }
    public override void OnDelete()
    {
        Hunt?.Finished(this); Hunt = null; HavenBossLair.Finished(this); DeleteEels(); Contributions.Clear(); base.OnDelete();
    }
}

[SerializationGenerator(0)]
public partial class HavenScalisEel : BaseCreature
{
    [SerializableField(0)] private HavenScalis _boss;
    [SerializableField(1)] private DateTime _expires;
    [Constructible]
    public HavenScalisEel() : base(AIType.AI_Melee, FightMode.Closest)
    {
        Name = "a parasitic eel"; Body = 52; Hue = 0x497; CanSwim = true; Tamable = false; NoKillAwards = true;
        SetStr(100); SetDex(160); SetInt(30); SetHits(150); SetDamage(4, 9);
        SetSkill(SkillName.Wrestling, 85); SetSkill(SkillName.Tactics, 80); Fame = 0; Karma = -1000;
        Expires = Core.Now + TimeSpan.FromMinutes(2);
    }
    public override bool AlwaysAttackable => true;
    public override Poison HitPoison => Poison.Regular;
    public override Poison PoisonImmune => Poison.Lethal;
    public override void GenerateLoot() { }
    public override void OnThink()
    {
        if (Boss?.Deleted != false || !Boss.Alive || Core.Now >= Expires) { Delete(); return; }
        base.OnThink();
    }
    public override void OnDelete() { Boss?.Eels.Remove(this); Boss = null; base.OnDelete(); }
}
