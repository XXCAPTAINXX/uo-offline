using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.Commands;
using Server.Engines.Spawners;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Spells;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenPirateEstate : Item
{
    internal static readonly Point3D Site = new(4128, 2800, 0);
    internal static readonly Point3D CommonsSite = new(3968, 2858, 0);
    internal static readonly HashSet<HavenPirateEstate> Registry = new();
    [SerializableField(0)] private Mobile _owner;
    [SerializableField(1)] private List<Item> _fixtures = new();
    private EstateRegion _region;
    [Constructible] public HavenPirateEstate() : base(0xBD2) { Name = "Corsair's Rest"; Movable = false; Visible = false; }
    [AfterDeserialization] private void Register()
    {
        Registry.Add(this); _region?.Unregister();
        if (Map != null && Map != Map.Internal) { _region = new EstateRegion(this); _region.Register(); }
    }
    internal static bool TerrainReady()
        => Map.Trammel.CanSpawnMobile(new Point3D(Site.X + 68, Site.Y + 68, 0)) && HavenCommunityCenter.CanBuild(CommonsSite, Map.Trammel);
    internal void Build(Mobile owner)
    {
        if (Fixtures.Count != 0) { return; }
        Owner = owner; Register();
        for (var x = 98; x <= 122; x += 6)
        for (var y = 44; y <= 68; y += 8)
        { Place(new Static(0xCCA) { Name = "Corsair's grove" }, x, y); }
        for (var x = 106; x <= 118; x += 3)
        for (var y = 88; y <= 97; y += 3)
        { Place(new Static(0x53B) { Name = "Island ore outcrop" }, x, y); }
        Place(new Static(0x134F), 109, 86); Place(new Static(0x1363), 115, 86);
        for (var x = 85; x <= 89; x++)
        for (var y = 135; y <= 164; y++) { Place(new Static(0x7CD), x, y); }
        for (var x = 90; x <= 100; x++)
        for (var y = 159; y <= 162; y++) { Place(new Static(0x7CD), x, y); }
        foreach (var y in new[] { 137, 146, 155, 164 })
        { Place(new Static(0x14F8), 84, y); Place(new Static(0x14F8), 90, y); }
        Place(new Static(0xB20) { Light = LightType.Circle300 }, 83, 139);
        Place(new Static(0xE3F) { Name = "Weathered pirate chest" }, 53, 115);
        Place(new Static(0x426) { Name = "The old captain's pennant", Hue = 0x455 }, 45, 113);
        Place(new Static(0xE77), 43, 110);
        var pirates = new Spawner(3, TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(90), 0,
            new Rectangle3D(X + 43, Y + 106, -5, 16, 14, 20), nameof(HavenIslandCorsair));
        Place(pirates, 50, 112);
        var wildlife = new Spawner(4, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(4), 0,
            new Rectangle3D(X + 100, Y + 42, -5, 25, 28, 20), "Hind", "Sheep");
        Place(wildlife, 112, 56);
        Place(new HavenCommonsReturnGate(), 80, 132);
        var key = new HavenEstateChart { Estate = this }; owner.BankBox.DropItem(key);
        this.MarkDirty();
    }
    private void Place(Item item, int x, int y)
    { item.Movable = false; item.MoveToWorld(new Point3D(X + x, Y + y, 0), Map); Fixtures.Add(item); }
    internal bool Travel(Mobile from)
    {
        if (Deleted || from != Owner || !from.Alive || from.Criminal || from.Spell != null || SpellHelper.CheckCombat(from) ||
            Map == null || Map == Map.Internal || !SpellHelper.CheckTravel(from, TravelCheckType.RecallFrom, out _)) { return false; }
        for (var radius = 0; radius <= 3; radius++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                for (var dy = -radius; dy <= radius; dy++)
                {
                    if (radius > 0 && Math.Abs(dx) != radius && Math.Abs(dy) != radius) { continue; }
                    var point = new Point3D(X + 80 + dx, Y + 128 + dy, 0);
                    if (!Map.CanSpawnMobile(point) || Server.Multis.BaseHouse.FindHouseAt(point, Map, 16) != null ||
                        !SpellHelper.CheckTravel(from, Map, point, TravelCheckType.RecallTo, out _)) { continue; }
                    BaseCreature.TeleportPets(from, point, Map); from.MoveToWorld(point, Map); from.PlaySound(0x1FE); return true;
                }
            }
        }
        from.SendMessage("Your island landing is obstructed."); return false;
    }
    internal static bool GoHome(Mobile from)
    {
        foreach (var estate in Registry) { if (!estate.Deleted && estate.Owner == from) { return estate.Travel(from); } }
        return false;
    }
    public override void OnDelete()
    { _region?.Unregister(); _region = null; Registry.Remove(this); foreach (var item in Fixtures) { item?.Delete(); } Fixtures.Clear(); Owner = null; base.OnDelete(); }
    private sealed class EstateRegion : BaseRegion
    {
        private readonly HavenPirateEstate _estate;
        public EstateRegion(HavenPirateEstate estate) : base("Corsair's Rest", estate.Map, 90, new Rectangle3D(estate.X, estate.Y, -128, 176, 176, 256)) { _estate = estate; }
        public override bool AllowHousing(Mobile from, Point3D p)
            => from == _estate.Owner && p.X >= _estate.X + 48 && p.X < _estate.X + 88 && p.Y >= _estate.Y + 48 && p.Y < _estate.Y + 88;
    }
    public static void Initialize()
    {
        CommandSystem.Register("home", AccessLevel.Player, e =>
        { if (!GoHome(e.Mobile)) { e.Mobile.SendMessage("No accessible island home was found. You must own the island, be alive, and be outside combat and travel restrictions."); } });
        Timer.StartTimer(TimeSpan.FromSeconds(35), () =>
        { foreach (var estate in Registry) { estate.DecorateSettlement(); estate.EnsureHomeTrial(); estate.EnsureHomePatrol(); } });
        CommandSystem.Register("HavenIslandsBuild", AccessLevel.GameMaster, e =>
        {
            if (Registry.Count != 0 || HavenCommunityCenter.Registry.Count != 0) { e.Mobile.SendMessage("An island or Commons already exists; saved structures were preserved."); return; }
            if (!TerrainReady()) { e.Mobile.SendMessage("The staged island terrain is not installed or the Commons site is blocked. Nothing was changed."); return; }
            var center = new HavenCommunityCenter(); var estate = new HavenPirateEstate();
            try { center.MoveToWorld(CommonsSite, Map.Trammel); center.Build(); estate.MoveToWorld(Site, Map.Trammel); estate.Build(e.Mobile); }
            catch { center.Delete(); estate.Delete(); throw; }
            e.Mobile.SendMessage("The Commons and Corsair's Rest are built. Your island chart is in your bank; bank portals lead to the Commons.");
        });
    }
}

[SerializationGenerator(0)]
public partial class HavenEstateChart : Item
{
    [SerializableField(0)] private HavenPirateEstate _estate;
    [Constructible] public HavenEstateChart() : base(0x14EB) { Name = "Chart to Corsair's Rest"; Weight = 1; LootType = LootType.Blessed; }
    public override void OnDoubleClick(Mobile from)
    { if (IsChildOf(from.Backpack) && Estate?.Travel(from) != true) { from.SendMessage("Only this island's owner can use its chart, while alive and outside combat, casting and travel restrictions."); } }
}

[SerializationGenerator(0)]
public partial class HavenIslandCorsair : Brigand
{
    [Constructible] public HavenIslandCorsair()
    {
        Name = "a stranded corsair"; SetStr(70, 90); SetDex(60, 80); SetInt(30, 40);
        SetHits(100, 140); SetDamage(3, 6); Home = new Point3D(4178, 2912, 0); RangeHome = 8;
        AddItem(new TricorneHat { Hue = 0x455 });
    }
}
