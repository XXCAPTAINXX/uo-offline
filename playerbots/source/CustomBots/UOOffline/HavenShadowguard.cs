using System;
using System.Collections.Generic;
using ModernUO.Serialization;
using Server.CustomBots;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Gumps;
using Server.Mobiles;
using Server.Regions;

namespace Server.UOOffline;

public enum HavenShadowRoom { Bar, Orchard, Armory, Fountain, Belfry, Roof }

[SerializationGenerator(0)]
public partial class HavenShadowChamber : Item
{
    internal static readonly HashSet<HavenShadowChamber> Registry = new();
    [SerializableField(0)] private HavenShadowRoom _room;
    [SerializableField(1)] private List<Item> _fixtures = new();
    [SerializableField(2)] private List<HavenShadowActor> _actors = new();
    [SerializableField(3)] private List<Mobile> _members = new();
    [SerializableField(4)] private List<Mobile> _followers = new();
    [SerializableField(5)] private List<Item> _puzzle = new();
    [SerializableField(6)] private Mobile _leader;
    [SerializableField(7)] private DateTime _ends;
    [SerializableField(8)] private int _progress;
    [SerializableField(9)] private int _supplies;
    [SerializableField(10)] private int _stage;
    private Timer _timer;
    private HavenShadowRegion _region;
    private DateTime _nextAssist;
    private readonly HashSet<Mobile> _assist = new();
    [Constructible]
    public HavenShadowChamber() : base(0xE2D) { Name = "Shadowguard chamber"; Movable = false; }
    internal bool Active => Leader != null;
    internal bool Original => Map == Map.TerMur;
    internal int Radius => Original ? Room == HavenShadowRoom.Roof ? 31 : 25 : 15;
    internal Point3D ExitLocation => Original ? HavenOriginalDungeons.ShadowEntrance : new Point3D(4760, 3242, 0);
    internal Point3D Arrival => Original ? Room switch
    {
        HavenShadowRoom.Fountain => new Point3D(X + 11, Y + 11, Z),
        HavenShadowRoom.Belfry => new Point3D(X + 15, Y + 1, Z),
        HavenShadowRoom.Roof => new Point3D(X - 8, Y - 8, Z),
        _ => Location
    } : new Point3D(X, Y + 11, Z);
    internal bool Inside(Mobile m) => m?.Deleted == false && m.Map == Map && m.InRange(this, Radius);
    internal bool Member(Mobile m) => Members.Contains(m) || Followers.Contains(m) || m is BaseCreature pet && Members.Contains(pet.GetMaster());
    internal bool Participant(Mobile m) => Active && Member(m) && Inside(m);
    internal void Build(HavenShadowRoom room)
    {
        Room = room; Name = $"Shadowguard — {room}";
        if (Original && Fixtures.Count == 0)
        {
            HavenShadowScenery.Build(this);
            var exit = new HavenShadowNode { Chamber = this, Kind = 9, Name = "Exit Shadowguard" };
            exit.MoveToWorld(new Point3D(Arrival.X + 1, Arrival.Y + 1, Arrival.Z), Map); Fixtures.Add(exit);
        }
        else if (Fixtures.Count == 0)
        {
            for (var x = -16; x <= 16; x++)
            {
                for (var y = -16; y <= 16; y++)
                {
                    var edge = Math.Abs(x) == 16 || Math.Abs(y) == 16;
                    var item = new Static(edge ? 0x80 : 0x519) { Hue = room == HavenShadowRoom.Orchard ? 0 : 0x455 };
                    item.MoveToWorld(new Point3D(X + x, Y + y, edge ? Z : Z - 1), Map); Fixtures.Add(item);
                }
            }
            var exit = new HavenShadowNode { Chamber = this, Kind = 9, Name = "Exit Shadowguard" };
            exit.MoveToWorld(new Point3D(X + 2, Y + 11, Z), Map); Fixtures.Add(exit);
            if (room == HavenShadowRoom.Belfry)
            {
                for (var x = -5; x <= 5; x++)
                {
                    for (var y = -12; y <= -5; y++)
                    { var floor = new Static(0x519); floor.MoveToWorld(new Point3D(X + x, Y + y, Z + 12 - TileData.ItemTable[0x519].Height), Map); Fixtures.Add(floor); }
                }
            }
        }
        Register(); this.MarkDirty();
    }
    private void Register()
    {
        Registry.Add(this); _region?.Unregister();
        _region = new HavenShadowRegion(this); _region.Register();
        _timer?.Stop(); _timer = Timer.DelayCall(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), Tick);
    }
    [AfterDeserialization(false)]
    internal void Recover()
    {
        if (Deleted) { return; }
        Register();
        // Room qualification is saved separately. Interrupted fights never resume or pay twice.
        Finish(false);
    }
    internal bool Start(Mobile from)
    {
        if (Active || Deleted || from?.Deleted != false || !from.Alive || from.Map != Map ||
            !from.InRange(ExitLocation, 10) || from.Criminal || from.Spell != null) { return false; }
        var party = Party.Get(from);
        if (party != null && party.Leader != from) { from.SendMessage("Only your party leader can choose a room."); return false; }
        var group = new List<Mobile> { from };
        if (party != null)
        {
            foreach (var member in party.Members)
            {
                var mobile = member.Mobile;
                if (mobile != from && mobile?.Deleted == false && mobile.Alive && mobile.Map == from.Map && mobile.InRange(from, 12)) { group.Add(mobile); }
            }
        }
        foreach (var mobile in group)
        {
            if (Server.Spells.SpellHelper.CheckCombat(mobile)) { return false; }
            foreach (var chamber in Registry) { if (chamber.Active && chamber.Member(mobile)) { return false; } }
            if (Room == HavenShadowRoom.Roof && mobile is PlayerMobile && HavenFrontierRecord.Get(mobile).Rooms != 31)
            { from.SendMessage("Each player needs the five room seals before entering the Roof."); return false; }
        }
        Leader = from; Members.AddRange(group); Ends = Core.Now + TimeSpan.FromMinutes(30); Progress = 0; Supplies = 0; Stage = 0;
        foreach (var mobile in group)
        {
            foreach (var pet in mobile.Map.GetMobilesInRange<BaseCreature>(mobile.Location, 12))
            {
                if (pet.ControlMaster == mobile && pet.ControlOrder is OrderType.Follow or OrderType.Guard && !Followers.Contains(pet)) { Followers.Add(pet); }
            }
        }
        foreach (var follower in Followers) { follower.MoveToWorld(Arrival, Map); }
        foreach (var mobile in Members) { mobile.MoveToWorld(Arrival, Map); }
        SetupPuzzle(); this.MarkDirty(); return true;
    }
    private HavenShadowNode Node(int kind, int key, int x, int y, string name, int art = 0x1E5E, int z = 0)
    {
        var node = new HavenShadowNode { Chamber = this, Kind = kind, Key = key, Name = name, ItemID = art };
        node.MoveToWorld(new Point3D(X + x, Y + y, Z + z), Map); Puzzle.Add(node); return node;
    }
    internal HavenShadowActor Spawn(int role, int x, int y, int z = 0)
    {
        var actor = new HavenShadowActor { Chamber = this, Role = role }; actor.ConfigureRole(Members.Count);
        var preferred = new Point3D(X + x, Y + y, Z + z);
        actor.Home = Original ? HavenOriginalDungeons.SpawnPoint(Map, preferred, role == 5 ? preferred : Arrival) : preferred;
        actor.RangeHome = 12; actor.MoveToWorld(actor.Home, Map); Actors.Add(actor);
        return actor;
    }
    private void SetupPuzzle()
    {
        if (Original) { SetupOriginalPuzzle(); return; }
        switch (Room)
        {
            case HavenShadowRoom.Bar:
                Node(0, 0, 0, 6, "Bottle rack — take and throw at a pirate", 0x99B);
                for (var i = 0; i < 3; i++) { Spawn(0, -6 + i * 6, -4); } break;
            case HavenShadowRoom.Orchard:
                for (var i = 0; i < 16; i++)
                { Node(1, i, i % 2 == 0 ? -8 : 8, -11 + i / 2 * 3, HavenShadowPuzzles.TreeNames[i], 0xD01); }
                break;
            case HavenShadowRoom.Armory:
                Node(2, 0, 0, 5, "Purifying brazier — cleanse a corrupt phylactery", 0xE31);
                for (var i = 0; i < 3; i++) { Spawn(2, -7 + i * 7, -7); Spawn(1, -6 + i * 6, 0); } break;
            case HavenShadowRoom.Fountain:
                for (var lane = 0; lane < 2; lane++)
                {
                    for (var step = 0; step < 8; step++)
                    { Node(3, lane * 8 + step, lane == 0 ? -6 : 6, -7 + step * 2, $"Canal {lane + 1}, segment {step + 1} — empty", 0x1B7A); }
                }
                Node(4, 0, 0, -10, "Fountain valve — test both canals", 0x1008);
                for (var i = 0; i < 4; i++) { Spawn(3, -9 + i * 6, 3); } break;
            case HavenShadowRoom.Belfry:
                Node(5, 0, 0, 4, "Ring the belfry bell", 0x1C12); break;
            case HavenShadowRoom.Roof:
                Node(8, 0, 0, -13, "Minax's time rift", 0xF6C); Spawn(10, 0, -4); break;
        }
    }
    private void SetupOriginalPuzzle()
    {
        switch (Room)
        {
            case HavenShadowRoom.Bar:
                Node(0, 0, -12, 0, "Bottle rack — take and throw at a pirate", 0x99B);
                for (var i = 0; i < 3; i++) { Spawn(0, -15, -6 + i * 5); } break;
            case HavenShadowRoom.Orchard:
                var trees = HavenOriginalDungeons.TreeOffsets;
                for (var i = 0; i < 16; i++) { Node(1, i, trees[i].X, trees[i].Y, HavenShadowPuzzles.TreeNames[i], 0xD01); } break;
            case HavenShadowRoom.Armory:
                Node(2, 0, -2, 0, "Purifying brazier — cleanse a corrupt phylactery", 0xE31);
                for (var i = 0; i < 3; i++) { Spawn(2, 5, -7 - i * 5); Spawn(1, -12 - i * 3, -12); } break;
            case HavenShadowRoom.Fountain:
                for (var lane = 0; lane < 2; lane++)
                {
                    for (var step = 0; step < 8; step++)
                    { Node(3, lane * 8 + step, lane == 0 ? -12 + step * 3 : 12, lane == 0 ? 12 : -12 + step * 3, $"Canal {lane + 1}, segment {step + 1} — empty", 0x1B7A); }
                }
                Node(4, 0, 9, 12, "Fountain valve — test both canals", 0x1008);
                for (var i = 0; i < 4; i++) { Spawn(3, -18 + i * 12, 17); } break;
            case HavenShadowRoom.Belfry: Node(5, 0, 12, 0, "Ring the belfry bell", 0x1C12); break;
            case HavenShadowRoom.Roof: Node(8, 0, 0, -13, "Minax's time rift", 0xF6C); Spawn(10, 0, -4); break;
        }
    }
    internal void Killed(HavenShadowActor actor)
    {
        if (!Active || !Actors.Remove(actor)) { return; }
        actor.Chamber = null;
        switch (actor.Role)
        {
            case 0: if (++Progress == 3) { Finish(true); } break;
            case 1: Supplies++; break;
            case 2: if (++Progress == 3) { Finish(true); } break;
            case 3: Supplies += 4; break;
            case 4: Supplies++; break;
            case 5: Finish(true); break;
            case >= 10 and <= 13:
                Stage++;
                if (Stage == 4) { Finish(true); }
                else { Spawn(10 + Stage, 0, -4); }
                break;
        }
        this.MarkDirty();
    }
    internal bool OrderPuzzle(Mobile from, bool enable)
    {
        if (!Participant(from) || Room == HavenShadowRoom.Roof) { return false; }
        if (!enable)
        {
            _assist.Remove(from);
            foreach (var follower in Followers)
            { if (follower is HavenCompanion owned && owned.BoundOwner == from) { owned.ControlOrder = OrderType.Follow; owned.ControlTarget = from; } }
            return true;
        }
        HavenCompanion found = null;
        foreach (var follower in Followers)
        { if (follower is HavenCompanion companion && companion.BoundOwner == from && Inside(companion) && !companion.IsDeadPet && companion.Hits > 0) { found = companion; break; } }
        if (found == null) { from.SendMessage("Bring your living companion into this room first."); return false; }
        if (enable) { _assist.Add(from); } else { _assist.Remove(from); found.ControlOrder = OrderType.Follow; found.ControlTarget = from; }
        found.Say(enable ? "I'll handle the puzzle steps. We still need to defeat its guardians." : "I'll leave the puzzle to you."); return true;
    }
    internal void Tick()
    {
        if (!Active) { return; }
        var survivor = false;
        foreach (var member in Members)
        { if (member is PlayerMobile && Inside(member) && member.Alive && (member.NetState != null || member is PlayerBot bot && HavenDungeonCrew.For(bot) != null)) { survivor = true; break; } }
        if (!survivor || Core.Now >= Ends) { Finish(false); return; }
        foreach (var member in Members)
        { if (!Inside(member) && member.Map != Map.Internal) { _assist.Remove(member); } }
        if (Core.Now < _nextAssist) { return; }
        _nextAssist = Core.Now + TimeSpan.FromSeconds(3);
        foreach (var owner in new List<Mobile>(_assist))
        {
            foreach (var follower in Followers)
            {
                if (follower is HavenCompanion companion && companion.BoundOwner == owner && Participant(companion) && companion.Hits > 0 && !companion.IsDeadPet && !companion.Paralyzed && !companion.Frozen)
                { HavenShadowPuzzles.Assist(this, companion); break; }
            }
        }
    }
    internal void Finish(bool success)
    {
        var wasActive = Active; Leader = null; _assist.Clear();
        if (wasActive && success)
        {
            foreach (var member in Members)
            {
                if (member is not PlayerMobile player || !Inside(player)) { continue; }
                var record = HavenFrontierRecord.Get(player);
                if (Room != HavenShadowRoom.Roof) { record.Rooms |= 1 << (int)Room; player.SendMessage("Room complete. Your Shadowguard seal is recorded."); }
                else
                {
                    record.Rooms = 0; record.Roofs++; HavenFrontierSupport.Reward(player, 50000, 35, 10);
                    for (var i = 0; i < 3; i++) { HavenFrontierSupport.Deliver(player, PowerScroll.CreateRandomNoCraft(10, 20)); }
                    var chance = .25 + Math.Clamp(player.Luck, 0, 5000) / 20000.0;
                    if (Utility.RandomDouble() < chance) { HavenFrontierSupport.Deliver(player, HavenFrontierSupport.Relic(Utility.Random(3), "Shadowguard")); }
                    player.SendMessage("The time rift is sealed! Gold, scrolls, marks and Astral shards have been delivered. Your next Roof run requires fresh room seals.");
                }
            }
        }
        foreach (var member in Members)
        {
            if (member?.Corpse is { Deleted: false } corpse && corpse.Map == Map && corpse.InRange(new Point2D(X, Y), Radius))
            { corpse.MoveToWorld(ExitLocation, Map); }
            if (member != null && (Inside(member) || member.Map == Map.Internal)) { HavenFrontierSupport.Return(member, ExitLocation, Map); }
        }
        foreach (var follower in Followers)
        {
            if (follower is HavenCompanion companion) { companion.ControlOrder = OrderType.Follow; companion.ControlTarget = companion.BoundOwner; }
            if (Inside(follower)) { HavenFrontierSupport.Return(follower, ExitLocation, Map); }
        }
        Members.Clear(); Followers.Clear();
        foreach (var actor in Actors) { if (actor != null) { actor.Chamber = null; actor.Delete(); } }
        Actors.Clear();
        foreach (var item in Puzzle) { item?.Delete(); } Puzzle.Clear(); Progress = 0; Supplies = 0; Stage = 0; this.MarkDirty();
    }
    internal void Leave(Mobile member)
    {
        if (!Member(member)) { return; }
        if (member.Corpse is { Deleted: false } corpse && corpse.Map == Map && corpse.InRange(new Point2D(X, Y), Radius))
        { corpse.MoveToWorld(ExitLocation, Map); }
        _assist.Remove(member); Members.Remove(member); HavenFrontierSupport.Return(member, ExitLocation, Map);
        for (var i = Followers.Count - 1; i >= 0; i--)
        {
            if (Followers[i] is BaseCreature pet && pet.GetMaster() == member)
            {
                if (pet is HavenCompanion companion) { companion.ControlOrder = OrderType.Follow; companion.ControlTarget = member; }
                HavenFrontierSupport.Return(pet, ExitLocation, Map); Followers.RemoveAt(i);
            }
        }
        if (Members.Count == 0) { Finish(false); }
    }
    public override void OnDoubleClick(Mobile from)
    { if (Participant(from)) { from.CloseGump<HavenShadowMenu>(); from.SendGump(new HavenShadowMenu(from, this)); } }
    public override void OnDelete()
    {
        Finish(false); _timer?.Stop(); _timer = null; _region?.Unregister(); _region = null; Registry.Remove(this);
        foreach (var item in Fixtures) { item?.Delete(); } Fixtures.Clear(); base.OnDelete();
    }
}

internal sealed class HavenShadowRegion : DungeonRegion
{
    private readonly HavenShadowChamber _chamber;
    internal HavenShadowRegion(HavenShadowChamber chamber) : base($"Shadowguard {chamber.Serial}", chamber.Map, 110,
        new Rectangle3D(chamber.X - chamber.Radius - 1, chamber.Y - chamber.Radius - 1, -128, chamber.Radius * 2 + 3, chamber.Radius * 2 + 3, 256)) { _chamber = chamber; }
    public override bool AllowHousing(Mobile from, Point3D p) => false;
    private bool Allowed(Mobile mobile) => _chamber.Member(mobile) || mobile is HavenShadowActor actor && actor.Chamber == _chamber;
    public override bool AllowHarmful(Mobile from, Mobile target) => Allowed(from) && Allowed(target) && base.AllowHarmful(from, target);
    public override bool AllowBeneficial(Mobile from, Mobile target) => Allowed(from) && Allowed(target) && base.AllowBeneficial(from, target);
    public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        => m.AccessLevel >= AccessLevel.GameMaster || _chamber.Active && Allowed(m);
    public override void OnEnter(Mobile m)
    {
        base.OnEnter(m);
        if (m.AccessLevel >= AccessLevel.GameMaster || Allowed(m)) { return; }
        Timer.DelayCall(() =>
        {
            if (m.Deleted || _chamber.Deleted || !_chamber.Inside(m) || Allowed(m)) { return; }
            HavenFrontierSupport.Return(m, _chamber.ExitLocation, _chamber.Map);
        });
    }
}
