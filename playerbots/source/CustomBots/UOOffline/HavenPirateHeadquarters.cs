using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.CustomBots;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Spells;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenPirateHeadquarters : HouseFoundation
{
    [SerializableField(0)] private List<Item> _companyFixtures = new();
    [SerializableField(1)] private GuildMasterChest _masterStorage;
    [SerializableField(2)] private HavenPirateEstate _estate;
    internal static readonly HashSet<HavenPirateHeadquarters> Registry = new();
    [Constructible]
    public HavenPirateHeadquarters(Mobile owner) : base(owner, 0x18A8, 5000, 5000)
    { Name = "R.E.C. - Rare Export Company"; Public = true; Price = 102280; Type = FoundationType.DarkWood; BuildPirateLayout(); Registry.Add(this); }
    public override HousePlacementEntry GetAosEntry()
    {
        foreach (var entry in HousePlacementEntry.ClassicHouses) { if (entry.Type == typeof(Castle)) { return entry; } }
        return base.GetAosEntry();
    }
    [AfterDeserialization] private void Register() { Registry.Add(this); }
    public override void OnDelete()
    { Registry.Remove(this); MasterStorage = null; Estate = null; CompanyFixtures.Clear(); base.OnDelete(); }
    public override int GetAosMaxSecures() => 10000;
    public override int GetAosMaxLockdowns() => 5000;
    internal bool CompanyAccess(Mobile from) => from?.Deleted == false && (IsOwner(from) || IsCoOwner(from) || IsGuildMember(from));
    internal void Furnish()
    {
        if (CompanyFixtures.Count != 0) { return; }
        Sign.Name = "R.E.C. - Rare Export Company headquarters";
        MasterStorage = new GuildMasterChest { Name = "R.E.C. receiving chest - automatic sorting" };
        Place(MasterStorage, 2, 0, 6);
        Chest(GuildStorageRole.Smithing, -9, -13, 6, new SmithHammer(), new MalletAndChisel());
        Chest(GuildStorageRole.Tailoring, 10, -10, 6, new SewingKit(), new Scissors());
        Chest(GuildStorageRole.Carpentry, -6, 0, 6, new DovetailSaw(), new FletcherTools());
        Chest(GuildStorageRole.Tinkering, 6, 0, 6, new TinkerTools());
        Chest(GuildStorageRole.Pantry, -10, 9, 6, new Skillet());
        Chest(GuildStorageRole.Taming, 11, 10, 6);
        Chest(GuildStorageRole.Alchemy, 6, 2, 26, new MortarPestle(), new Blowpipe());
        Chest(GuildStorageRole.Scribing, -6, 1, 26, new ScribesPen(), new MapmakersPen());
        Chest(GuildStorageRole.Resources, -4, 4, 6, new HavenResourceLedger());
        Chest(GuildStorageRole.Unsorted, 4, 4, 6);
        Chest(GuildStorageRole.Overflow, 6, 4, 6);
        Chest(GuildStorageRole.Armory, 5, -2, 46);
        Chest(GuildStorageRole.Treasury, 5, 2, 46);
        // Ground deck: smithy, sail loft, shipwright/tinker benches, galley and pet room.
        Place(new HavenSmallSoulForge(), -11, -12, 6);
        Place(new AnvilSouthAddon(), -10, -10, 6);
        Decor(0x1BDD, "Charcoal and dry firewood", -12, -13, 6);
        Decor(0xFAF, "Smith's workbench tools", -12, -10, 6);
        Place(new LoomSouthAddon(), 10, -13, 6);
        Place(new SpinningWheelSouthAddon(), 13, -10, 6);
        Decor(0x1766, "Bolts of sailcloth", 13, -13, 6);
        Decor(0x14F8, "Rigging rope", 12, -9, 6);
        Decor(0xB90, "Shipwright's workbench", -5, 2, 6);
        Decor(0xB90, "Tinker's workbench", 5, 2, 6);
        Place(new HavenRepairBench(), -2, 3, 6);
        Place(new StoneOvenSouthAddon(), -13, 10, 6);
        Place(new FlourMillSouthAddon(), -10, 12, 6);
        Place(new WaterTroughEastAddon(), -13, 13, 6);
        Decor(0xE77, "Freshwater cask", -12, 9, 6);
        Decor(0xE3D, "The galley's stores", -9, 10, 6);
        Place(new HavenHouseHitchingPost(), 13, 10, 6);
        Place(new WaterTroughEastAddon(), 13, 12, 6);
        Decor(0xF36, "Pet bedding", 10, 13, 6);
        // Middle deck: company council, alchemy and mapmaking.
        Place(new AlchemistTableSouthAddon(), 5, -1, 26);
        Decor(0xA9A, "Charts and sailing accounts", -6, 3, 26);
        Decor(0xB90, "Cartographer's desk", -4, 3, 26);
        Place(new MediumStoneTableSouthAddon(), 2, 3, 26);
        Decor(0xB2D, "Quartermaster's chair", 1, 2, 26);
        Decor(0xB2D, "Company officer's chair", 4, 2, 26);
        Decor(0x14EB, "R.E.C. trade routes", 2, 3, 32);
        Decor(0x1047, "The company export ledger", -4, 3, 32);
        Place(new HavenPirateCharter { Headquarters = this }, 5, -3, 26);
        // Upper deck: captain's quarters, guarded vault and sea lookouts.
        Place(new LargeBedSouthAddon(), -6, 1, 46);
        Decor(0xB90, "The captain's desk", -4, -3, 46);
        Decor(0x14EB, "An unfinished voyage chart", -4, -3, 52);
        Decor(0xB2D, "The captain's chair", -4, -2, 46);
        Decor(0xA4D, "Captain's sea chest", -2, 2, 46);
        Place(new SmallBedSouthAddon(), -13, -12, 46);
        Place(new SmallBedSouthAddon(), 11, -12, 46);
        Decor(0x14F5, "Lookout's spyglass", -11, 12, 46);
        Decor(0x14F5, "Harbor watch spyglass", 12, 12, 46);
        foreach (var z in new[] { 6, 26, 46 })
        {
            Place(new HavenPirateStair { Headquarters = this }, 0, 0, z);
            Decor(0xB20, "A shaded ship's lantern", 7, 4, z);
        }
        foreach (var p in new[] { (-14,-13,6),(14,-13,6),(-14,13,6),(14,13,6),(-14,-13,46),(14,-13,46),(-14,13,46),(14,13,46) })
        { Decor(0xB20, "R.E.C. watch lantern", p.Item1, p.Item2, p.Item3); }
        Decor(0x426, "R.E.C. - Rare Export Company", -3, 14, 26, 0x455);
        Decor(0x426, "Rare cargo. Fair shares. Safe harbor.", 3, 14, 26, 0x455);
        this.MarkDirty();
    }
    private void Chest(GuildStorageRole role, int x, int y, int z, params Item[] tools)
    {
        var chest = new GuildProfessionChest(role, MasterStorage);
        foreach (var tool in tools) { chest.DropItem(tool); }
        Place(chest, x, y, z);
    }
    private void Decor(int id, string name, int x, int y, int z, int hue = 0)
    { Place(new Static(id) { Name = name, Hue = hue, Light = id == 0xB20 ? LightType.Circle150 : LightType.Empty }, x, y, z); }
    private void Place(Item item, int x, int y, int z)
    {
        var point = new Point3D(X + x, Y + y, Z + z + 1);
        if (!IsInside(point, 16)) { item.Delete(); throw new InvalidOperationException($"Furnishing is outside the headquarters: {x},{y},{z}"); }
        item.MoveToWorld(point, Map); CompanyFixtures.Add(item);
        if (item is Container container)
        { container.IsSecure = true; container.Movable = false; Secures.Add(new SecureInfo(container, SecureLevel.Guild)); }
        else if (item is BaseAddon addon) { Addons.Add(addon); }
        else { item.IsLockedDown = true; item.Movable = false; LockDowns.Add(item); }
    }
    internal bool ChangeDeck(Mobile from, int deck)
    {
        if (!CompanyAccess(from) || !from.Alive || deck is < 0 or > 4 || from.Map != Map || !IsInside(from) ||
            from.Spell != null || SpellHelper.CheckCombat(from)) { return false; }
        var target = FloorDestinations[deck];
        for (var radius = 0; radius <= 2; radius++)
        for (var x = -radius; x <= radius; x++)
        for (var y = -radius; y <= radius; y++)
        {
            var p = new Point3D(X + target.X + x, Y + target.Y + y, Z + target.Z);
            if (!Map.CanSpawnMobile(p) || !IsInside(p, 16)) { continue; }
            BaseCreature.TeleportPets(from, p, Map); from.MoveToWorld(p, Map);
            from.SendMessage($"You arrive at {FloorNames[deck]}."); return true;
        }
        return false;
    }
}
[SerializationGenerator(0)]
public partial class HavenPirateStair : Item
{
    [SerializableField(0)] private HavenPirateHeadquarters _headquarters;
    [Constructible] public HavenPirateStair() : base(0x8A5) { Name = "R.E.C. rope ladder"; Movable = false; }
    public override void OnDoubleClick(Mobile from)
    {
        if (Headquarters?.Deleted != false || !Headquarters.Climb(from,this))
        { from.SendMessage("Move beside the ladder on this floor. The landing must be clear and you must be out of combat."); }
    }

    public override void OnDelete() { Headquarters = null; base.OnDelete(); }
}
[SerializationGenerator(0)]
public partial class HavenPirateCharter : Item
{
    [SerializableField(0)] private HavenPirateHeadquarters _headquarters;
    [Constructible] public HavenPirateCharter() : base(0xFF1) { Name = "Rare Export Company charter and house guide"; Movable = false; }
    public override void OnDoubleClick(Mobile from)
    {
        if (Headquarters?.Deleted != false || !Headquarters.CompanyAccess(from) || from.Map!=Map || !from.InRange(this,3)) { return; }
        from.SendMessage("R.E.C.: use the receiving chest to sort cargo into linked profession stores. Tools are in their workshop chests.");
        from.SendMessage("Ship's stairs reach all three decks and bring your nearby followers. Guild members share secured storage.");
        from.SendMessage("Use the character Guild menu to create or join Rare Export Company. This headquarters follows its owner's guild membership.");
    }
    public override void OnDelete() { Headquarters = null; base.OnDelete(); }
}
