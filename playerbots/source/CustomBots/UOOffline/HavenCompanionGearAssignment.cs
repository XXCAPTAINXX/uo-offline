using System;
using ModernUO.Serialization;
using Server.Accounting;
using Server.Commands;
using Server.Items;

namespace Server.UOOffline;

// An explicitly requested offline assignment; ordinary AFK mission behavior is unchanged.
[SerializationGenerator(0)]
public partial class HavenCompanionGearAssignment : Item
{
    [SerializableField(0)] private HavenCompanion _companion;
    [SerializableField(1)] private bool _running;
    [SerializableField(2)] private DateTime _nextReward;
    [SerializableField(3)] private int _completed;
    [SerializableField(4)] private string _status = "Not assigned";
    private Timer _timer;
    public override bool IsVirtualItem => true;
    [Constructible]
    public HavenCompanionGearAssignment() : base(1)
    { Visible=false; Movable=false; Weight=0; Name="offline companion gear assignment"; }

    internal static HavenCompanion Find(Mobile owner)
    {
        if(owner?.Account is Account account &&
            Serial.TryParse(account.GetTag($"HavenCompanion:{owner.Serial}"),null,out var serial) &&
            World.FindMobile(serial) is HavenCompanion companion && !companion.Deleted && companion.BoundOwner==owner)
        { return companion; }
        return null;
    }
    internal static HavenCompanionGearAssignment Begin(HavenCompanion companion, DateTime now)
    {
        if(companion?.Deleted!=false || companion.BoundOwner?.Deleted!=false || companion.Backpack==null)
        { throw new InvalidOperationException("An existing owned companion is required."); }
        var record=companion.Backpack.FindItemByType<HavenCompanionGearAssignment>();
        if(record?.Running==true) { return record; }
        if(companion.BoundOwner.NetState!=null)
        { throw new InvalidOperationException("Offline gear grinding starts after the owner logs out."); }
        if(record==null) { record=new HavenCompanionGearAssignment { Companion=companion }; companion.Backpack.DropItem(record); }
        var duration=HavenMissionRoute.Duration(companion);var kind=HavenMissionRoute.OfflineKind(companion);
        if(!HavenRegionalMissions.CanStart(companion,kind)) { throw new InvalidOperationException("The companion does not meet the selected route's skills."); }
        record.Running=true; record.NextReward=now+TimeSpan.FromMinutes(duration); record.Completed=0;
        if(companion.Expedition is { } trip && trip.Due>now) { record.NextReward=trip.Due+TimeSpan.FromMinutes(duration); }
        record.Status=$"{HavenRegionalMissions.Name(kind)} missions until your next login";
        HavenMissionJournal.Begin(companion,now);
        companion.PrepareForExpedition();
        record.Schedule();
        return record;
    }
    public static void Initialize()
    {
        CommandSystem.Register("CompanionGrind",AccessLevel.Player,e=>
        {
            var companion=Find(e.Mobile);
            var record=companion?.Backpack?.FindItemByType<HavenCompanionGearAssignment>();
            if(record==null) { e.Mobile.SendMessage("No offline gear assignment. Your normal companion missions are in Tasks."); return; }
            if(e.ArgString.Trim().Equals("off",StringComparison.OrdinalIgnoreCase)) { record.Stop("Stopped by owner"); }
            e.Mobile.SendMessage($"{record.Status}. Completed runs: {record.Completed}. Loot is in the shared pack.");
        });
    }
    [AfterDeserialization(false)]
    private void Schedule()
    {
        _timer?.Stop(); _timer=null;
        if(!Deleted && Running) { _timer=Timer.DelayCall(TimeSpan.FromSeconds(5),Pulse); }
    }
    private void Pulse() { Tick(Core.Now,Companion?.BoundOwner?.NetState!=null); Schedule(); }
    internal void Tick(DateTime now,bool connected)
    {
        if(!Running) { return; }
        var companion=Companion; var owner=companion?.BoundOwner;
        if(companion?.Deleted!=false || owner?.Deleted!=false || companion.Backpack==null) { Stop("Owner or companion unavailable"); return; }
        if(companion.Expedition is { } trip && !connected)
        {
            if(now<trip.Due) { return; }
            if(!trip.Return(owner,now,automatic:true,offline:true)) { Stop("Shared pack needs space for the previous expedition"); return; }
        }
        // At most one earned run per tick. Downtime catches up without monopolizing the game loop.
        if(now>=NextReward)
        {
            var pack=companion.Backpack;
            if(pack.TotalItems>=pack.MaxItems-20 || pack.TotalWeight>=pack.MaxWeight-5000)
            { Stop("Shared pack needs emptying"); return; }
            var kind=HavenMissionRoute.OfflineKind(companion);var duration=HavenMissionRoute.Duration(companion);
            var percent=HavenMissionDuration.BonusPercent(duration);var training=HavenMissionDuration.Scale(duration,percent);
            var journal=HavenMissionJournal.Begin(companion,now);
            var loot=HavenCompanionExpedition.CreateLoot(kind,duration,owner,companion:companion,percent:percent);
            foreach(var item in loot.Items.ToArray())
            {
                if(HavenResourceLedger.StoreMissionReward(companion,item,journal)) { continue; }
                pack.DropItem(item);journal.Receipt(item,"Companion shared pack");
            }
            loot.Delete();
            companion.AwardExpeditionProgress(training,now);
            companion.Hits=companion.HitsMax; companion.Stam=companion.StamMax; companion.Mana=companion.ManaMax;
            journal.Completed(kind,duration,training,true);
            Completed++; NextReward+=TimeSpan.FromMinutes(duration);
        }
        if(connected && owner.Map!=null && owner.Map!=Map.Internal)
        {
            Stop("Finished — owner returned");
            var afk=companion.Backpack.FindItemByType<HavenCompanionIdleMissions>();
            if(afk!=null) { afk.Enabled=false; afk.Afk=false; }
            // Completed offline expeditions keep the controlled companion internalized.
            // Shrunken pets have no control master and must still use their token.
            if(companion.Expedition==null && companion.Map==Map.Internal && !companion.IsStabled && companion.ControlMaster==owner)
            { companion.MoveToWorld(owner.Location,owner.Map); }
            HavenCompanions.ClaimOrRecall(owner);
            owner.SendMessage($"Your companion completed {Completed} gear runs. Evolving gear gained experience; found gear is in the shared pack.");
        }
    }
    internal void Stop(string reason)
    { Running=false;Status=reason;_timer?.Stop();_timer=null;if(Companion?.Deleted==false) { HavenMissionJournal.Find(Companion)?.Finish(Core.Now,reason); } }
    public override void OnDelete() { _timer?.Stop(); _timer=null; Companion=null; base.OnDelete(); }
}
