using System;
using ModernUO.Serialization;
using Server.Collections;
using Server.Commands;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Targeting;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenTideSteed : SeaHorse
{
    [SerializableField(0)] private bool _previousSwimming;
    [SerializableField(1)] private Point3D _lastLand;
    [SerializableField(2)] private Map _lastLandMap;
    private Timer _pulse;
    private SOS _destination;
    private PathFollower _route;
    private DateTime _routeEnds;
    private Point3D _lastStep;
    private int _blocked;
    private SkillMod _fishingBonus;
    [Constructible]
    public HavenTideSteed()
    {
        Name = "a tidebound sea horse"; Hue = 0x482; CanSwim = true; Tamable = true; MinTameSkill = 70; ControlSlots = 1;
        SetStr(250); SetDex(190); SetInt(100); SetHits(350); SetDamage(5, 10);
        Backpack?.Delete(); AddItem(new StrongBackpack { Movable = false });
    }
    internal bool CanManage(Mobile from) => from?.Deleted == false && from.Alive &&
        (Rider == from || ControlMaster == from && from.Map == Map && from.InRange(this, 3));
    public override bool IsSnoop(Mobile from) => !CanManage(from);
    public override bool CheckNonlocalLift(Mobile from, Item item) => CanManage(from);
    public override bool CheckNonlocalDrop(Mobile from, Item item, Item target) => CanManage(from);
    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    { base.GetContextMenuEntries(from, ref list); PackAnimal.GetContextMenuEntries(this, from, ref list); }
    public override bool OnDragDrop(Mobile from, Item item)
    { if (CheckFeed(from, item)) { return true; } return CanManage(from) && Backpack.TryDropItem(from, item, false); }

    public static void RiderChanged(BaseMount mount, Mobile oldRider, Mobile newRider)
    { if (mount is HavenTideSteed steed) { steed.ChangeRider(oldRider, newRider); } }
    private void ChangeRider(Mobile oldRider, Mobile newRider)
    {
        StopRoute(); _pulse?.Stop(); _pulse = null;
        if (oldRider != null)
        {
            if (_fishingBonus != null) { oldRider.RemoveSkillMod(_fishingBonus); _fishingBonus = null; }
            oldRider.CanSwim = PreviousSwimming;
            if (!oldRider.CanSwim && oldRider.Map != null && oldRider.Map != Map.Internal && !oldRider.Map.CanSpawnMobile(oldRider.Location))
            {
                if (LastLandMap != null && LastLandMap != Map.Internal && LastLandMap.CanSpawnMobile(LastLand))
                { BaseCreature.TeleportPets(oldRider, LastLand, LastLandMap); oldRider.MoveToWorld(LastLand, LastLandMap); MoveToWorld(LastLand, LastLandMap); }
                else { HavenRecovery.GoToBank(oldRider); if (!Deleted) { MoveToWorld(oldRider.Location, oldRider.Map); } }
                oldRider.SendMessage("Your sea horse carries you to safety before you dismount.");
            }
        }
        if (newRider != null)
        {
            PreviousSwimming = newRider.CanSwim; LastLand = newRider.Location; LastLandMap = newRider.Map;
            RestoreRider();
        }
    }
    [AfterDeserialization]
    private void RestoreRider()
    {
        if (Rider == null || Deleted) { return; }
        Rider.CanSwim = true;
        if (_fishingBonus != null) { Rider.RemoveSkillMod(_fishingBonus); }
        _fishingBonus = new DefaultSkillMod(SkillName.Fishing, "HavenTideFishing", true, 10); Rider.AddSkillMod(_fishingBonus);
        _pulse?.Stop(); _pulse = Timer.DelayCall(TimeSpan.FromMilliseconds(400), TimeSpan.FromMilliseconds(400), Tick);
    }
    internal bool Navigate(Mobile from, SOS sos)
    {
        if (Rider != from || sos?.Deleted != false || !sos.IsChildOf(from.Backpack) || sos.TargetMap != from.Map || SpellHelper.CheckCombat(from) || from.Spell != null) { return false; }
        _destination = sos; _route = new PathFollower(from, sos.TargetLocation); _lastStep = from.Location; _blocked = 0;
        _routeEnds = Core.Now + TimeSpan.FromMinutes(15); from.SendMessage("Navigating toward the SOS. Moving manually, combat or Stop cancels the route."); return true;
    }
    internal void StopRoute() { _destination = null; _route = null; _blocked = 0; }
    private void Tick()
    {
        var rider = Rider;
        if (Deleted || rider == null) { _pulse?.Stop(); _pulse = null; StopRoute(); return; }
        if (rider.Map != Map.Internal && rider.Map.CanSpawnMobile(rider.Location)) { LastLand = rider.Location; LastLandMap = rider.Map; }
        if (_destination == null) { return; }
        if (_destination.Deleted || !_destination.IsChildOf(rider.Backpack) || _destination.TargetMap != rider.Map ||
            rider.Location != _lastStep || rider.NetState == null || !rider.Alive || rider.Spell != null || rider.Criminal || SpellHelper.CheckCombat(rider) || Core.Now >= _routeEnds)
        { StopRoute(); rider.SendMessage("SOS navigation stopped."); return; }
        if (rider.InRange(_destination.TargetLocation, 3)) { StopRoute(); rider.SendMessage("We have reached the wreck. Fish here to recover the SOS normally."); return; }
        _route.Follow(false, 3);
        _blocked = rider.Location == _lastStep ? _blocked + 1 : 0; _lastStep = rider.Location;
        if (_blocked >= 12) { StopRoute(); rider.SendMessage("The route is obstructed. Steer around the obstacle, then select the SOS again."); }
    }
    public override void OnDelete()
    { _pulse?.Stop(); _pulse = null; StopRoute(); base.OnDelete(); LastLandMap = null; _fishingBonus = null; }
    public static void Initialize()
    {
        CommandSystem.Register("tide", AccessLevel.Player, e =>
        { if (e.Mobile.Mount is HavenTideSteed steed) { e.Mobile.CloseGump<HavenTideGump>(); e.Mobile.SendGump(new HavenTideGump(steed)); } else { e.Mobile.SendMessage("Mount your tidebound sea horse first."); } });
    }
}

public sealed class HavenTideGump : Gump
{
    private readonly HavenTideSteed _steed;
    public HavenTideGump(HavenTideSteed steed) : base(70, 70)
    {
        _steed = steed; AddBackground(0, 0, 410, 295, 9270); AddLabel(25, 20, 1152, "Tidebound companion");
        AddLabel(25, 47, 2101, "Water travel · cargo · +10 Fishing while mounted");
        var labels = new[] { "Open cargo", "Navigate to an SOS...", "Stop navigation", "Use fishing pole", "Use a fishing net" };
        for (var i = 0; i < labels.Length; i++) { AddButton(25, 83 + i * 32, 4005, 4007, i + 1); AddLabel(65, 83 + i * 32, 1152, labels[i]); }
        AddButton(285, 254, 4017, 4019, 0); AddLabel(320, 254, 1152, "Close");
    }
    public override void OnResponse(NetState state, in RelayInfo info)
    {
        var from = state.Mobile;
        if (!_steed.CanManage(from) || info.ButtonID == 0) { return; }
        switch (info.ButtonID)
        {
            case 1: from.SendGump(new HavenTideCargoGump(_steed)); return;
            case 2: from.Target = new SosTarget(_steed); break;
            case 3: _steed.StopRoute(); break;
            case 4: if (from.Backpack.FindItemByType<FishingPole>() is { } pole) { pole.OnDoubleClick(from); } else { from.SendMessage("Keep a fishing pole in your backpack."); } break;
            case 5: if (from.Backpack.FindItemByType<SpecialFishingNet>() is { } net) { net.OnDoubleClick(from); } else { from.SendMessage("Keep a special fishing net in your backpack."); } break;
        }
        from.SendGump(new HavenTideGump(_steed));
    }
    private sealed class SosTarget : Target
    {
        private readonly HavenTideSteed _steed;
        public SosTarget(HavenTideSteed steed) : base(-1, false, TargetFlags.None) { _steed = steed; }
        protected override void OnTarget(Mobile from, object target)
        { if (target is not SOS sos || !_steed.Navigate(from, sos)) { from.SendMessage("Target an SOS in your pack on this facet while riding and out of combat."); } }
    }
}
