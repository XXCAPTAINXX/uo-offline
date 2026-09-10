using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Collections;
using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenChelonian : BaseCreature
{
    [Constructible]
    public HavenChelonian() : base(AIType.AI_Melee, FightMode.Aggressor)
    {
        Name = "a Chelonian tide tortoise"; Body = 1294; BaseSoundID = 0x5A;
        CanSwim = true; CantWalk = false; Tamable = true; MinTameSkill = 90; ControlSlots = 3;
        SetStr(400, 480); SetDex(180, 210); SetInt(160, 200); SetHits(650, 800); SetDamage(15, 21);
        SetDamageType(ResistanceType.Physical, 70); SetDamageType(ResistanceType.Cold, 30);
        SetResistance(ResistanceType.Physical, 65, 70); SetResistance(ResistanceType.Fire, 35, 45);
        SetResistance(ResistanceType.Cold, 65, 75); SetResistance(ResistanceType.Poison, 50, 60);
        SetResistance(ResistanceType.Energy, 40, 50); HavenRarePetAbility.Skills(this, 105);
        Skills.Anatomy.Base = 90;
        Backpack?.Delete(); AddItem(new StrongBackpack { Movable = false });
    }
    public override bool StatLossAfterTame => false;
    public override FoodType FavoriteFood => FoodType.Fish | FoodType.FruitsAndVeggies;
    public override int Meat => 4;
    public override string CorpseName => "a tide tortoise corpse";
    internal bool Manage(Mobile from) => from?.Deleted == false && from.Alive && Controlled &&
        from == ControlMaster && from.Map == Map && from.InRange(this, 3);
    public override bool IsSnoop(Mobile from) => !Manage(from);
    public override bool CheckNonlocalLift(Mobile from, Item item) => Manage(from);
    public override bool CheckNonlocalDrop(Mobile from, Item item, Item target) => Manage(from);
    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> entries)
    { base.GetContextMenuEntries(from, ref entries); PackAnimal.GetContextMenuEntries(this, from, ref entries); }
    public override bool OnDragDrop(Mobile from, Item item)
    { return CheckFeed(from, item) || Manage(from) && Backpack.TryDropItem(from, item, false); }
    public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
    {
        base.AlterMeleeDamageFrom(from, ref damage);
        if (Controlled && !IsDeadPet && Hits > 0 && Hits * 2 < HitsMax)
        { damage = damage * (80 - HavenPetSignatures.Tier(this) * 5) / 100; }
    }
    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);
        list.Add($"{"Amphibious:"} {"walks on land, swims and fights at sea; owner-accessible cargo"}");
        list.Add($"{"Living shell:"} {"below half health, reduces melee damage by 20–35% depending on rarity"}");
    }
}

[SerializationGenerator(0)]
public partial class HavenChelonia : Item
{
    internal static readonly Point3D Site = new(3992, 3576, 0);
    internal static readonly Point3D Landing = new(3992, 3621, 0);
    internal static readonly HashSet<HavenChelonia> Registry = new();
    [SerializableField(0)] private List<Item> _fixtures = new();
    [SerializableField(1)] private List<HavenChelonian> _tortoises = new();
    [SerializableField(2)] private DateTime _nextSpawn;
    private Timer _timer;
    [Constructible]
    public HavenChelonia() : base(0x1E5E) { Name = "Chelonia — sanctuary field guide"; Movable = false; }
    [AfterDeserialization]
    private void Register()
    {
        Registry.Add(this); _timer?.Stop();
        _timer = Timer.DelayCall(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), Tick);
    }
    internal void Build()
    {
        if (Fixtures.Count != 0) { return; }
        // Sparse volcanic rocks, coastal plants and nesting ground leave wide walking routes.
        for (var i = 0; i < 18; i++)
        {
            var angle = i * Math.PI * 2 / 18;
            var x = X + (int)(Math.Cos(angle) * 38); var y = Y + (int)(Math.Sin(angle) * 34);
            var item = new Static(i % 3 == 0 ? 0xC9E : i % 3 == 1 ? 0x1363 : 0xC93);
            item.MoveToWorld(new Point3D(x, y, Map.GetAverageZ(x, y)), Map); Fixtures.Add(item);
        }
        for (var y = 45; y <= 72; y++)
        {
            for (var x = -1; x <= 1; x++)
            { var plank = new Static(0x7C9); plank.MoveToWorld(new Point3D(X + x, Y + y, 0), Map); Fixtures.Add(plank); }
        }
        var gate = new HavenCommonsReturnGate(); gate.MoveToWorld(new Point3D(X + 3, Y + 44, 0), Map); Fixtures.Add(gate);
        Register(); Tick(); this.MarkDirty();
    }
    internal void Tick()
    {
        if (Deleted || Map != Map.Trammel) { return; }
        for (var i = Tortoises.Count - 1; i >= 0; i--)
        {
            var pet = Tortoises[i];
            if (pet == null || pet.Deleted || pet.Controlled || pet.Owners.Count > 0)
            { Tortoises.RemoveAt(i); NextSpawn = Core.Now + TimeSpan.FromMinutes(5); this.MarkDirty(); }
            else if (pet.Map != Map || !pet.InRange(this, 50))
            { pet.MoveToWorld(new Point3D(X + 6 + i * 3, Y + 4, 0), Map); }
        }
        if (Tortoises.Count >= 2 || Core.Now < NextSpawn) { return; }
        var point = new Point3D(X + 6 + Tortoises.Count * 3, Y + 4, 0);
        if (!Map.CanSpawnMobile(point)) { return; }
        var created = new HavenChelonian(); var roll = Utility.RandomDouble(); var tier = roll < .45 ? 1 : roll < .80 ? 2 : 3;
        HavenPetRarity.Apply(created, tier); created.MinTameSkill = 80 + tier * 10;
        created.Home = point; created.RangeHome = 12; created.MoveToWorld(point, Map); Tortoises.Add(created); this.MarkDirty();
    }
    public override void OnDoubleClick(Mobile from)
    {
        if (from.Map != Map || !from.InRange(this, 4)) { return; }
        from.SendMessage("Chelonia's tide tortoises fight on land and at sea. Rare/Epic/Legendary need 90/100/110 Taming. Feed fish or produce. Wild replacements return after five minutes; tamed pets are preserved.");
    }
    public override void OnDelete()
    {
        _timer?.Stop(); _timer = null; Registry.Remove(this);
        foreach (var item in Fixtures) { item?.Delete(); }
        foreach (var pet in Tortoises) { if (pet?.Deleted == false && !pet.Controlled && pet.Owners.Count == 0) { pet.Delete(); } }
        Fixtures.Clear(); Tortoises.Clear(); base.OnDelete();
    }
}
