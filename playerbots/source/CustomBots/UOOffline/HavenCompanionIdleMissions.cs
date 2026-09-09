using System;
using ModernUO.Serialization;
using Server.Accounting;
using Server.Commands;
using Server.Items;
using Server.Gumps;
using Server.Network;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenCompanionIdleMissions : Item
{
    [SerializableField(0)] private HavenCompanion _companion;
    [SerializableField(1)] private bool _enabled = true;
    [SerializableField(2)] private int _cycle;
    [SerializableField(3)] private HavenCompanionExpedition _activeTrip;
    [SerializableField(4)] private bool _afk;
    private Timer _timer;
    private DateTime _lastActivity = Core.Now;
    private long _move, _action, _skill, _spell;
    private bool _observed;
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenCompanionIdleMissions() : base(1)
    { Visible = false; Movable = false; Weight = 0; Name = "companion idle missions"; }

    public static void Initialize()
    {
        CommandSystem.Register("CompanionAuto", AccessLevel.Player, e =>
        {
            if (e.Mobile.Account is not Account account ||
                !Serial.TryParse(account.GetTag($"HavenCompanion:{e.Mobile.Serial}"), null, out var serial) ||
                World.FindMobile(serial) is not HavenCompanion companion || companion.BoundOwner != e.Mobile)
            { e.Mobile.SendMessage("Claim your companion with [companion first."); return; }
            var record = Ensure(companion);
            if (record == null) { return; }
            var option = e.ArgString.Trim();
            if (option.Equals("on", StringComparison.OrdinalIgnoreCase)) { record.Enabled = true; }
            else if (option.Equals("off", StringComparison.OrdinalIgnoreCase)) { record.Enabled = false; }
            record.NoteActivity(Core.Now);
            record.Tick(Core.Now);
            e.Mobile.SendGump(new HavenCompanionAfkGump(record));
            e.Mobile.SendMessage($"Idle missions: {(record.Enabled ? "ON" : "OFF")}. Five idle minutes starts the cycle; moving returns your helper. Use [companionauto on or off.");
        });
    }

    internal static HavenCompanionIdleMissions Ensure(HavenCompanion companion)
    {
        if (companion?.Deleted != false || companion.Backpack == null || companion.BoundOwner == null) { return null; }
        var record = companion.Backpack.FindItemByType<HavenCompanionIdleMissions>();
        if (record != null) { return record; }
        record = new HavenCompanionIdleMissions { Companion = companion };
        companion.Backpack.DropItem(record); record.Schedule(); return record;
    }

    [AfterDeserialization]
    private void Schedule()
    {
        _timer?.Stop(); _timer = null;
        if (!Deleted) { _timer = Timer.DelayCall(TimeSpan.FromSeconds(5), Pulse); }
    }
    private void Pulse()
    { Tick(Core.Now); Schedule(); }
    internal void NoteActivity(DateTime now) { _lastActivity = now; Afk = false; }
    internal void SetAfk(bool value)
    {
        NoteActivity(Core.Now); Tick(Core.Now);
        Afk = value;
        if (value) { Enabled = true; }
        Tick(Core.Now);
    }
    internal bool Owns(HavenCompanionExpedition trip) => trip != null && ActiveTrip == trip;

    internal static HavenExpeditionKind BestTaming(HavenCompanion companion)
    {
        var best = HavenExpeditionKind.Grind;
        var requirement = -1.0;
        foreach (var kind in Enum.GetValues<HavenExpeditionKind>())
        {
            if (HavenTamingMissions.CanStart(companion, kind) && HavenTamingMissions.Requirement(kind) > requirement)
            { best = kind; requirement = HavenTamingMissions.Requirement(kind); }
        }
        return best;
    }
    internal HavenExpeditionKind NextMission() => Cycle % 6 == 5 ? BestTaming(Companion) : (HavenExpeditionKind)(Cycle % 6);

    internal void Tick(DateTime now) => Tick(now, Companion?.BoundOwner?.NetState != null);
    internal void Tick(DateTime now, bool connected)
    {
        var owner = Companion?.BoundOwner;
        if (Companion?.Deleted != false || owner?.Deleted != false) { Delete(); return; }
        // Only observe a connected player in the world. Offline training continues separately.
        var active = !connected || owner.Map == null || owner.Map == Map.Internal ||
            !owner.Alive || owner.Warmode || owner.Combatant != null || owner.Spell != null || owner.Target != null ||
            owner.Hits < owner.HitsMax || owner.Poisoned;
        if (!_observed || _move != owner.LastMoveTime || _action != owner.NextActionTime ||
            _skill != owner.NextSkillTime || _spell != owner.NextSpellTime || active)
        { NoteActivity(now); }
        _observed = true; _move = owner.LastMoveTime; _action = owner.NextActionTime;
        _skill = owner.NextSkillTime; _spell = owner.NextSpellTime;
        if (ActiveTrip?.Deleted == true) { ActiveTrip = null; }
        var idle = Enabled && (Afk || now - _lastActivity >= TimeSpan.FromMinutes(5));
        if (ActiveTrip != null)
        {
            if ((!idle || now >= ActiveTrip.Due) && connected && owner.Map != Map.Internal)
            { ActiveTrip.Return(owner, now, automatic: now >= ActiveTrip.Due); }
            return;
        }
        if (!idle || Companion.Expedition != null || Companion.IsDeadPet || Companion.IsStabled ||
            Companion.Combatant != null || Companion.Hits < Companion.HitsMax ||
            Companion.Backpack.TotalItems >= Companion.Backpack.MaxItems - 15 ||
            Companion.Backpack.TotalWeight >= Companion.Backpack.MaxWeight - 50) { return; }
        if (HavenCompanionExpedition.Start(Companion, owner, NextMission()))
        { ActiveTrip = Companion.Expedition; Cycle = (Cycle + 1) % 6; }
    }
    internal bool Finished(HavenCompanionExpedition trip, DateTime now)
    {
        if (!Owns(trip)) { return false; }
        ActiveTrip = null;
        return Enabled && (Afk || now - _lastActivity >= TimeSpan.FromMinutes(5));
    }
    public override void OnDelete()
    { _timer?.Stop(); _timer = null; Companion = null; ActiveTrip = null; base.OnDelete(); }
}

public class HavenCompanionAfkGump : Gump
{
    private readonly HavenCompanionIdleMissions _record;
    public HavenCompanionAfkGump(HavenCompanionIdleMissions record) : base(100, 100)
    {
        _record = record;
        AddBackground(0, 0, 390, 255, 9270);
        AddLabel(20, 20, 0, "Companion AFK missions");
        AddHtml(20, 52, 350, 70, "Cycle: loot, ore, wood, leather, reagents, best eligible pet.<BR>Moving or fighting brings your companion back.<BR>Gathered materials arrive as commodity deeds.");
        AddButton(20, 135, 4005, 4007, 1);
        AddLabel(55, 137, 0, record.Afk || record.ActiveTrip != null ? "Leave AFK / return now" : "Enter AFK now");
        AddButton(20, 172, 4005, 4007, 2);
        AddLabel(55, 174, 0, record.Enabled ? "Auto after 5 idle minutes: ON" : "Auto after 5 idle minutes: OFF");
        AddButton(20, 212, 4005, 4007, 0); AddLabel(55, 214, 0, "Close");
    }
    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_record.Deleted || _record.Companion?.BoundOwner != sender.Mobile || info.ButtonID == 0) { return; }
        if (info.ButtonID == 1) { _record.SetAfk(!_record.Afk && _record.ActiveTrip == null); }
        else if (info.ButtonID == 2)
        { _record.Enabled = !_record.Enabled; if (!_record.Enabled) { _record.SetAfk(false); } }
        sender.Mobile.CloseGump<HavenCompanionAfkGump>();
        if (_record.ActiveTrip == null) { sender.Mobile.SendGump(new HavenCompanionAfkGump(_record)); }
    }
}
