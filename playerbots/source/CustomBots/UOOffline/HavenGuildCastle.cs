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
public partial class HavenGuildCastle : Castle
{
    [SerializableField(0)] private List<Item> _companyFixtures = new();
    [SerializableField(1)] private GuildMasterChest _masterStorage;
    [SerializableField(2)] private HavenPirateEstate _estate;
    internal static readonly HashSet<HavenGuildCastle> Registry = new();
    [Constructible]
    public HavenGuildCastle(Mobile owner) : base(owner)
    { Name = "R.E.C. - Rare Export Company"; Public = true; Price = 102280; Registry.Add(this); }
    public override HousePlacementEntry GetAosEntry()
    {
        foreach (var entry in HousePlacementEntry.ClassicHouses) { if (entry.Type == typeof(Castle)) { return entry; } }
        return base.GetAosEntry();
    }
    [AfterDeserialization] private void Register() { Registry.Add(this); }
    public override void OnDelete()
    { Registry.Remove(this); MasterStorage = null; Estate = null; CompanyFixtures.Clear(); base.OnDelete(); }
    internal bool CompanyAccess(Mobile from) => from?.Deleted == false && (IsOwner(from) || IsCoOwner(from) || IsGuildMember(from));
    internal static HavenGuildCastle Install(Mobile owner)
    {
        if (owner?.Deleted != false || owner.Account == null) { throw new InvalidOperationException("An existing player is required."); }
        foreach (var existing in Registry) { if (!existing.Deleted && existing.Owner == owner) { return existing; } }
        HavenPirateEstate estate = null;
        foreach (var candidate in HavenPirateEstate.Registry) { if (!candidate.Deleted && candidate.Owner == owner) { estate = candidate; break; } }
        if (estate == null) { throw new InvalidOperationException("No private island belongs to this player."); }
        var center = new Point3D(estate.X + 68, estate.Y + 68, 0);
        // HousePlacement intentionally bypasses collision checks for staff; protect this automated placement too.
        foreach (var item in estate.Map.GetItemsInRange(center, 19))
        {
            if (item.Visible && item.X >= center.X - 15 && item.X <= center.X + 15 &&
                item.Y >= center.Y - 15 && item.Y <= center.Y + 16)
            { throw new InvalidOperationException($"An existing item occupies the castle plot: {item.Serial}. It was preserved."); }
        }
        foreach (var mobile in estate.Map.GetMobilesInRange(center, 19))
        {
            if (mobile.X >= center.X - 15 && mobile.X <= center.X + 15 && mobile.Y >= center.Y - 15 && mobile.Y <= center.Y + 16)
            { throw new InvalidOperationException("Someone is on the castle plot. Placement waits until it is clear."); }
        }
        var result = HousePlacement.Check(owner, 0x7E, center, out var toMove);
        if (result != HousePlacementResult.Valid || toMove?.Count > 0)
        { throw new InvalidOperationException($"Castle plot is obstructed ({result}; {toMove?.Count ?? 0} entities). Existing possessions were preserved."); }
        var house = new HavenGuildCastle(owner) { Estate = estate };
        try { house.MoveToWorld(center, estate.Map); house.Furnish(); return house; }
        catch
        {
            // Only brand-new empty furnishing is rolled back; installation never moves player property.
            foreach (var item in house.CompanyFixtures.ToArray()) { item.Delete(); }
            house.Delete(); throw;
        }
    }
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
        Place(new HavenCompanyCharter { Headquarters = this }, 5, -3, 26);
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
            Place(new HavenCompanyLadder { Headquarters = this }, 0, 0, z);
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
        var point = new Point3D(X + x, Y + y, Z + z);
        if (!IsInside(point, 16)) { item.Delete(); throw new InvalidOperationException($"Furnishing is outside the castle: {x},{y},{z}"); }
        item.MoveToWorld(point, Map); CompanyFixtures.Add(item);
        if (item is Container container)
        { container.IsSecure = true; container.Movable = false; Secures.Add(new SecureInfo(container, SecureLevel.Guild)); }
        else if (item is BaseAddon addon) { Addons.Add(addon); }
        else { item.IsLockedDown = true; item.Movable = false; LockDowns.Add(item); }
    }
    internal bool ChangeDeck(Mobile from, int deck)
    {
        if (!CompanyAccess(from) || !from.Alive || deck is < 0 or > 2 || from.Map != Map || !IsInside(from) ||
            from.Spell != null || SpellHelper.CheckCombat(from)) { return false; }
        var z = Z + 6 + deck * 20;
        for (var x = -1; x <= 1; x++)
        {
            var p = new Point3D(X + x, Y + 1, z);
            if (!Map.CanSpawnMobile(p) || !IsInside(p, 16)) { continue; }
            BaseCreature.TeleportPets(from, p, Map); from.MoveToWorld(p, Map); return true;
        }
        return false;
    }
}
[SerializationGenerator(0)]
public partial class HavenCompanyLadder : Item
{
    [SerializableField(0)] private HavenGuildCastle _headquarters;
    [Constructible] public HavenCompanyLadder() : base(0x1DB2) { Name = "Ship's stair - choose a deck"; Movable = false; }
    public override void OnDoubleClick(Mobile from)
    {
        if (Headquarters?.Deleted != false || !Headquarters.CompanyAccess(from) || from.Map != Map || !from.InRange(this, 2) || Math.Abs(from.Z - Z) > 5) { return; }
        from.CloseGump<HavenCompanyDeckGump>(); from.SendGump(new HavenCompanyDeckGump(this));
    }
    public override void OnDelete() { Headquarters = null; base.OnDelete(); }
}
public sealed class HavenCompanyDeckGump : Gump
{
    private readonly HavenCompanyLadder _ladder;
    public HavenCompanyDeckGump(HavenCompanyLadder ladder) : base(40,40)
    {
        _ladder = ladder; AddBackground(0,0,420,235,5054); AddBackground(10,10,400,215,3000);
        AddLabel(25,24,0,"R.E.C. - Rare Export Company");
        var labels = new[] { "Workshops, stores and galley", "Guild hall, maps and alchemy", "Captain's quarters and treasury" };
        for (var i = 0; i < 3; i++) { AddButton(25,65+i*43,4005,4007,i+1); AddLabel(62,67+i*43,0,labels[i]); }
        AddButton(270,192,4005,4007,0); AddLabel(307,194,0,"Close");
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var from = sender.Mobile;
        if (info.ButtonID is < 1 or > 3 || _ladder.Deleted || _ladder.Headquarters?.Deleted != false ||
            from.Map != _ladder.Map || !from.InRange(_ladder,2) || Math.Abs(from.Z - _ladder.Z)>5) { return; }
        if (!_ladder.Headquarters.ChangeDeck(from,info.ButtonID-1)) { from.SendMessage("The stair is blocked, or you are in combat. Nothing moved."); }
    }
}
[SerializationGenerator(0)]
public partial class HavenCompanyCharter : Item
{
    [SerializableField(0)] private HavenGuildCastle _headquarters;
    [Constructible] public HavenCompanyCharter() : base(0xFF1) { Name = "Rare Export Company charter and house guide"; Movable = false; }
    public override void OnDoubleClick(Mobile from)
    {
        if (Headquarters?.Deleted != false || !Headquarters.CompanyAccess(from) || from.Map!=Map || !from.InRange(this,3)) { return; }
        from.SendMessage("R.E.C.: use the receiving chest to sort cargo into linked profession stores. Tools are in their workshop chests.");
        from.SendMessage("Ship's stairs reach all three decks and bring your nearby followers. Guild members share secured storage.");
        from.SendMessage("Use the character Guild menu to create or join Rare Export Company. This headquarters follows its owner's guild membership.");
    }
    public override void OnDelete() { Headquarters = null; base.OnDelete(); }
}
