using ModernUO.Serialization;
using Server.Items;

namespace Server.UOOffline;

[SerializationGenerator(0)]
public partial class HavenEstateTrial : HavenIslandTrial
{
    protected override string TrialName => "Blackwake landing - R.E.C. island mini champion";
    protected override int ChooseTheme() => 3;
    protected override int RoamingRange => 12;
    [Constructible]
    public HavenEstateTrial() { Name = TrialName; Hue = 0x66D; }
}

public partial class HavenPirateEstate
{
    internal HavenEstateTrial HomeTrial
    {
        get
        {
            foreach (var fixture in Fixtures) { if (fixture is HavenEstateTrial { Deleted: false } trial) { return trial; } }
            return null;
        }
    }
    internal bool MoveTrialToNorthwest()
    {
        var trial = HomeTrial;
        if (trial == null) { EnsureHomeTrial(); return HomeTrial != null; }
        var preferred = new Point3D(X + 70, Y + 36, 0);
        if (trial.InRange(preferred, 3)) { return true; }
        if (!HavenIslandTrial.FindSite(preferred, out var site)) { return false; }
        var dx = site.X - trial.X; var dy = site.Y - trial.Y;
        trial.MoveToWorld(site, Map);
        foreach (var creature in trial.Creatures)
        {
            if (creature?.Deleted != false) { continue; }
            creature.Home = site;
            if (HavenIslandTrial.FindSite(new Point3D(creature.X + dx, creature.Y + dy, site.Z), out var point)) { creature.MoveToWorld(point, Map); }
        }
        foreach (var fixture in Fixtures)
        {
            if (fixture?.Deleted == false && fixture is Static && fixture.Name is "Blackwake landing - defeat three waves and their captain" or "Raider's stolen cargo" or "A salt-stained rum barrel")
            { fixture.MoveToWorld(new Point3D(fixture.X + dx, fixture.Y + dy, fixture.Z), Map); }
        }
        this.MarkDirty(); return true;
    }
    internal void EnsureHomeTrial()
    {
        if (Deleted || Fixtures.Count == 0 || Map != Map.Trammel) { return; }
        if (HomeTrial != null) { MoveTrialToNorthwest(); return; }
        var preferred = new Point3D(X + 70, Y + 36, 0);
        if (!HavenIslandTrial.FindSite(preferred, out var site)) { return; }
        var trial = new HavenEstateTrial(); trial.MoveToWorld(site, Map); Fixtures.Add(trial);
        Settle(new Static(0x426) { Name = "Blackwake landing - defeat three waves and their captain", Hue = 0x455 }, 66, 33);
        Settle(new Static(0xE3D) { Name = "Raider's stolen cargo" }, 68, 39);
        Settle(new Static(0xE77) { Name = "A salt-stained rum barrel" }, 72, 33);
        this.MarkDirty();
    }
}

