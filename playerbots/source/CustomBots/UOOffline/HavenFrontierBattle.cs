using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenFrontierBattle : Item
{
    internal static readonly HashSet<HavenFrontierBattle> Registry = new();
    [SerializableField(0)] private bool _pirate;
    [SerializableField(1)] private int _phase;
    [SerializableField(2)] private List<HavenFrontierEnemy> _enemies = new();
    [SerializableField(3)] private List<PlayerMobile> _participants = new();
    [SerializableField(4)] private List<Item> _fixtures = new();
    [SerializableField(5)] private LargeBoat _vessel;
    [SerializableField(6)] private DateTime _deadline;
    [SerializableField(7)] private DateTime _nextRun;
    private Timer _timer;
    private Server.Regions.DungeonRegion _originalRegion;
    internal bool Original => !Pirate && X > 6200;
    internal Point3D Center => Pirate ? new Point3D(4090, 3580, -2) : Original ? new Point3D(6317, 2555, 0) : new Point3D(4888, 3416, 0);
    internal Point3D SafeExit => Pirate ? HavenChelonia.Landing : HavenFrontierSupport.RiftLanding;
    internal bool Active => Phase > 0;
    [Constructible]
    public HavenFrontierBattle() : base(0x1E5E) { Movable = false; Name = "Frontier expedition board"; }
    internal bool Nearby(Mobile m) => m?.Deleted == false && m.Map == Map && m.InRange(Center, Pirate ? 24 : 38);
    internal void Build(bool pirate)
    {
        Pirate = pirate; Name = pirate ? "Corsair patrol — boarding expeditions" : "Blackthorn — invasion dispatch";
        if (!pirate && !Original && Fixtures.Count == 0)
        {
            // Three small street fragments evoke the towns held inside Minax's rifts.
            for (var town = -1; town <= 1; town++)
            {
                for (var i = -10; i <= 10; i++)
                {
                    for (var width = -2; width <= 2; width++)
                    { Place(new Static(0x519), Center.X + town * 26 + width, Center.Y + i, -1); }
                    if (i % 4 == 0) { Place(new Static(0x80), Center.X + town * 26 + 8, Center.Y + i, 0); }
                }
                Place(new Static(0xB22) { Light = LightType.Circle300 }, Center.X + town * 26 - 5, Center.Y + 10, 0);
            }
        }
        Register(); this.MarkDirty();
    }
    private void Place(Item item, int x, int y, int z) { item.MoveToWorld(new Point3D(x, y, z), Map); Fixtures.Add(item); }
    private void Register()
    {
        Registry.Add(this); _timer?.Stop(); _timer = Timer.DelayCall(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5), Tick);
        _originalRegion?.Unregister(); _originalRegion = null;
        if (Original)
        {
            _originalRegion = new Server.Regions.DungeonRegion("Blackthorn's Dungeon", Map, 80, new Rectangle3D(6208,2304,-128,320,512,256));
            _originalRegion.Register();
        }
    }
    [AfterDeserialization(false)]
    internal void Recover() { if (!Deleted) { Register(); Cancel(); } }
    internal bool Start(Mobile from)
    {
        if (Deleted || Active || Core.Now < NextRun || from?.Deleted != false || !from.Alive || from.Map != Map ||
            !from.InRange(this, 4) || from.Criminal || from.Spell != null || Server.Spells.SpellHelper.CheckCombat(from)) { return false; }
        if (Pirate)
        {
            var vessel = new LargeBoat(); var water = new Point3D(Center.X, Center.Y, -5);
            if (!vessel.CanFit(water, Map, vessel.NorthID)) { vessel.Delete(); from.SendMessage("The patrol's water is occupied. Move any boat out of the marked waters first."); return false; }
            vessel.MoveToWorld(water, Map); vessel.Anchored = true; vessel.ShipName = "The Saltfang"; Vessel = vessel;
        }
        Phase = 1; Deadline = Core.Now + TimeSpan.FromMinutes(30); Participants.Clear(); SpawnWave();
        if (Pirate) { Board(from); } else { from.SendMessage("Defeat each captain's guards, then the captain. Clear both crews to expose the rift beacon. Three waves seal the breach."); }
        return true;
    }
    internal bool Board(Mobile from)
    {
        if (!Pirate || !Active || Vessel?.Deleted != false || from?.Deleted != false || !from.Alive ||
            from.Map != Map || !from.InRange(this, 4) || from.Criminal || Server.Spells.SpellHelper.CheckCombat(from)) { return false; }
        var point = new Point3D(Center.X, Center.Y + 3, Center.Z);
        if (!Map.CanFit(point, 16, checkMobiles: false)) { return false; }
        BaseCreature.TeleportPets(from, point, Map); from.MoveToWorld(point, Map);
        from.SendMessage("Boarded the Saltfang. Defeat the crew and captain, then destroy their warded cargo seal. The dispatch board can extract you, or use [voyage exit."); return true;
    }
    private void SpawnWave()
    {
        var groups = Pirate ? 1 : 2;
        for (var group = 0; group < groups; group++)
        {
            Spawn(1, group, Pirate ? 0 : (group == 0 ? -7 : 7), -2);
            for (var i = 0; i < 3; i++) { Spawn(0, group, Pirate ? i - 1 : (group == 0 ? -8 : 6) + i, Pirate ? i - 2 : i * 3); }
        }
        Spawn(2, -1, 0, Pirate ? -4 : -8);
    }
    private void Spawn(int role, int group, int dx, int dy)
    {
        var enemy = new HavenFrontierEnemy { Battle = this, Role = role, Group = group }; enemy.Setup();
        var shift = Pirate || Original ? 0 : (Phase - 2) * 26;
        enemy.Home = new Point3D(Center.X + shift + dx, Center.Y + dy, Center.Z); enemy.RangeHome = Pirate ? 4 : 10;
        if (Original) { enemy.Home = HavenOriginalDungeons.SpawnPoint(Map, enemy.Home, Center); }
        enemy.MoveToWorld(enemy.Home, Map); Enemies.Add(enemy);
    }
    internal void Credit(Mobile actor)
    {
        var source = actor is BaseCreature pet ? pet.GetMaster() : actor;
        if (source is Server.CustomBots.PlayerBot bot)
        {
            if (!Active || !Nearby(bot)) { return; }
            if (!Server.CustomBots.BotPlayerParty.InPlayerParty(bot) && !HavenGuildCrew.Retained(bot)) { Add(bot); }
            var group = Party.Get(bot);
            if (!Active || group == null) { return; }
            foreach (var member in group.Members)
            { if (member.Mobile is PlayerMobile human && human is not Server.CustomBots.PlayerBot && Nearby(human)) { Add(human); } }
            return;
        }
        var player = HavenFrontierSupport.Player(actor);
        if (!Active || player?.Deleted != false || !Nearby(player)) { return; }
        Add(player);
        var party = Party.Get(player);
        if (party == null) { return; }
        foreach (var member in party.Members)
        { if (member.Mobile is PlayerMobile mate && mate is not Server.CustomBots.PlayerBot && Nearby(mate)) { Add(mate); } }
    }
    private void Add(PlayerMobile player)
    { if (!Participants.Contains(player)) { Participants.Add(player); this.MarkDirty(); } }
    internal void Killed(HavenFrontierEnemy enemy)
    {
        if (!Active || !Enemies.Remove(enemy)) { return; }
        enemy.Battle = null;
        if (!Pirate && enemy.Role == 1)
        {
            foreach (var player in Participants) { if (Nearby(player)) { HavenFrontierRecord.Get(player).MinaxCredits++; } }
        }
        if (enemy.Role == 0)
        {
            var guards = false;
            foreach (var other in Enemies) { if (other.Role == 0 && other.Group == enemy.Group) { guards = true; break; } }
            if (!guards) { foreach (var captain in Enemies) { if (captain.Role == 1 && captain.Group == enemy.Group) { captain.Blessed = false; captain.Say("Face me, then!"); } } }
        }
        if (enemy.Role == 1)
        {
            var crews = false; foreach (var other in Enemies) { if (other.Role != 2) { crews = true; break; } }
            if (!crews) { foreach (var beacon in Enemies) { beacon.Blessed = false; beacon.Name = Pirate ? "an exposed cargo seal" : "an exposed rift beacon"; } }
        }
        if (enemy.Role == 2)
        {
            if (Phase >= (Pirate ? 2 : 3)) { Complete(); }
            else { Phase++; SpawnWave(); }
        }
        this.MarkDirty();
    }
    internal void Complete()
    {
        if (!Active) { return; }
        Phase = 0; // Claim the completion before delivering anything; stale deaths cannot pay again.
        foreach (var player in Participants)
        {
            if (!Nearby(player)) { continue; }
            var record = HavenFrontierRecord.Get(player); HavenFrontierSupport.Reward(player, Pirate ? 30000 : 40000, 15, 5);
            if (Pirate)
            {
                record.Voyages++; HavenFrontierSupport.Deliver(player, new HavenMaritimeCargo { Value = Utility.RandomMinMax(10, 20) });
                HavenFrontierSupport.Deliver(player, new CommodityDeed(new Board(Utility.RandomMinMax(300, 600))));
                HavenFrontierSupport.Deliver(player, new CommodityDeed(new IronIngot(Utility.RandomMinMax(150, 300))));
                player.SendMessage("The Saltfang is taken! Cargo and salvaged material deeds are in your pack. Redeem cargo at the island dispatch board.");
            }
            else { record.Rifts++; record.MinaxCredits += 12; player.SendMessage("Blackthorn's rift is sealed. Your Minax credits are available in [expeditions."); }
            if (player is Server.CustomBots.PlayerBot)
            {
                if (!Pirate && record.MinaxCredits > 0) { HavenMinaxCreditNote.Withdraw(player, Math.Min(60000, record.MinaxCredits)); }
                foreach (var item in player.Backpack.Items.ToArray())
                { if (item is HavenMinaxCreditNote or HavenMaritimeCargo) { HavenMarketProduction.Consign(player, item); } }
            }
        }
        Cleanup(); NextRun = Core.Now + TimeSpan.FromMinutes(5);
    }
    private void Tick()
    {
        if (Deleted || !Active) { return; }
        if (Core.Now >= Deadline) { Cancel(); return; }
        Vessel?.Refresh();
    }
    internal void Cancel()
    { Phase = 0; Cleanup(); var minimum = Core.Now + TimeSpan.FromMinutes(1); if (NextRun < minimum) { NextRun = minimum; } }
    private void Cleanup()
    {
        foreach (var enemy in Enemies) { if (enemy != null) { enemy.Battle = null; enemy.Delete(); } } Enemies.Clear();
        if (Vessel?.Deleted == false)
        {
            // Also rescue nonparticipants who boarded; never remove a hull beneath a visitor.
            var aboard = new List<Mobile>();
            foreach (var mobile in Map.GetMobilesInRange<Mobile>(Vessel.Location, 12))
            { if (Vessel.Contains(mobile.X, mobile.Y)) { aboard.Add(mobile); } }
            foreach (var mobile in aboard) { HavenFrontierSupport.Return(mobile, SafeExit); }
            var salvage = new List<Item>();
            foreach (var item in Map.GetItemsInRange<Item>(Vessel.Location, 12))
            { if (item.Parent == null && (item is Corpse || item.Movable) && Vessel.Contains(item.X, item.Y)) { salvage.Add(item); } }
            foreach (var item in salvage) { item.MoveToWorld(SafeExit, Map); }
            if (Vessel.Hold != null)
            {
                foreach (var item in Vessel.Hold.Items.ToArray()) { item.MoveToWorld(SafeExit, Map); }
            }
            foreach (var player in Participants) { if (player?.Map == Map.Internal) { HavenFrontierSupport.Return(player, SafeExit); } }
            Vessel.Delete(); Vessel = null;
        }
        Participants.Clear(); this.MarkDirty();
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Map != Map || !from.InRange(this, 4)) { return; }
        if (Active && Pirate) { Board(from); }
        else if (!Active && !Start(from)) { from.SendMessage("The expedition is recovering, blocked, or you are in combat. Try again shortly."); }
        else { from.SendMessage("The rift is already active. Join the battle north of this board; contributions from you and your pets count."); }
    }
    public override void OnDelete()
    {
        Cancel(); _timer?.Stop(); _timer = null; Registry.Remove(this);
        _originalRegion?.Unregister(); _originalRegion = null;
        foreach (var item in Fixtures) { item?.Delete(); } Fixtures.Clear(); base.OnDelete();
    }
}

[SerializationGenerator(0)]
public partial class HavenFrontierEnemy : BaseCreature
{
    [SerializableField(0)] private HavenFrontierBattle _battle;
    [SerializableField(1)] private int _role;
    [SerializableField(2)] private int _group;
    private DateTime _nextPower;
    [Constructible]
    public HavenFrontierEnemy() : base(AIType.AI_Melee, FightMode.Closest) { }
    internal void Setup()
    {
        var pirate = Battle.Pirate;
        Name = Role switch { 0 => pirate ? "a Saltfang corsair" : "a Minax legionary", 1 => pirate ? "Captain Sable Rook" : Group == 0 ? "a Minax spell captain" : "a Minax blade captain", _ => pirate ? "a warded cargo seal" : "Minax's warded rift beacon" };
        Body = Role == 2 ? 58 : 400; Hue = Role == 2 ? 0x489 : 0; Tamable = false; Blessed = Role != 0;
        SetStr(Role == 0 ? 170 : 500); SetDex(120); SetInt(Role == 0 ? 80 : 350);
        SetHits(Role == 0 ? 450 : Role == 1 ? 2200 : 3000); SetDamage(Role == 0 ? 6 : 14, Role == 0 ? 10 : 20);
        SetSkill(SkillName.Wrestling, 105); SetSkill(SkillName.Tactics, 105); SetSkill(SkillName.MagicResist, 100);
        SetResistance(ResistanceType.Physical, 45); SetResistance(ResistanceType.Fire, 45); SetResistance(ResistanceType.Cold, 40);
        SetResistance(ResistanceType.Poison, 55); SetResistance(ResistanceType.Energy, 40);
        if (Role == 2) { Frozen = true; SetDamage(0, 0); FightMode = FightMode.None; }
        else
        {
            AddItem(new Cutlass()); AddItem(new TricorneHat()); AddItem(new Shirt(pirate ? 0x455 : 0x489));
            if (Role == 1 && Group == 0 && !pirate) { AI = AIType.AI_Mage; SetSkill(SkillName.Magery, 110); SetSkill(SkillName.EvalInt, 110); }
        }
        Fame = Role == 1 ? 15000 : 2000; Karma = -Fame; _nextPower = Core.Now + TimeSpan.FromSeconds(10);
    }
    public override bool BardImmune => Role == 2;
    public override void GenerateLoot() { if (Role != 2) { AddLoot(Role == 1 ? LootPack.FilthyRich : LootPack.Average); } }
    public override void OnDamage(int amount, Mobile from, bool willKill)
    { if (amount > 0 && !Blessed) { Battle?.Credit(from); } base.OnDamage(amount, from, willKill); }
    public override void OnThink()
    {
        base.OnThink();
        if (Battle?.Active != true) { return; }
        if (Map != Battle.Map || !InRange(Home, Battle.Pirate ? 6 : 18)) { Combatant = null; MoveToWorld(Home, Battle.Map); }
        if (Role == 1 && !Blessed && Core.Now >= _nextPower && Combatant is Mobile target && InRange(target, 6) && CanBeHarmful(target, false))
        { _nextPower = Core.Now + TimeSpan.FromSeconds(12); AOS.Damage(target, this, 15, 50, 0, 0, 50, 0); }
    }
    public override void OnDeath(Container corpse)
    {
        Battle?.Killed(this); base.OnDeath(corpse);
        Timer.DelayCall(TimeSpan.FromSeconds(Role == 1 ? 180 : 45), () => { if (!corpse.Deleted) { corpse.Delete(); } });
    }
    public override void OnDelete() { Battle = null; base.OnDelete(); }
}

[SerializationGenerator(0)]
public partial class HavenMaritimeCargo : Item
{
    [SerializableField(0)] private int _value = 10;
    [Constructible]
    public HavenMaritimeCargo() : base(0x1EA5) { Name = "sealed maritime cargo"; Weight = 1; LootType = LootType.Blessed; }
    public override void GetProperties(IPropertyList list)
    { base.GetProperties(list); list.Add($"{"Turn-in:"} {Math.Clamp(Value, 1, 100)} {"doubloons at Chelonia's dispatch board"}"); }
    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack) || !from.Alive || Deleted) { return; }
        foreach (var board in HavenFrontierBattle.Registry)
        {
            if (!board.Pirate || board.Map != from.Map || !from.InRange(board, 4)) { continue; }
            var value = Math.Clamp(Value, 1, 100); Delete(); HavenFrontierRecord.Get(from).Doubloons += value;
            from.SendMessage($"Cargo delivered: {value} doubloons. Spend them with [expeditions."); return;
        }
        from.SendMessage("Take this cargo to Chelonia's corsair dispatch board.");
    }
}
