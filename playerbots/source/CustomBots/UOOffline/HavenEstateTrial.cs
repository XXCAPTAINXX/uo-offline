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
    internal void EnsureHomeTrial()
    {
        if (Deleted || Fixtures.Count == 0 || Map != Map.Trammel || HomeTrial != null) { return; }
        var preferred = new Point3D(X + 36, Y + 92, 0);
        if (!HavenIslandTrial.FindSite(preferred, out var site)) { return; }
        var trial = new HavenEstateTrial(); trial.MoveToWorld(site, Map); Fixtures.Add(trial);
        Settle(new Static(0x426) { Name = "Blackwake landing - defeat three waves and their captain", Hue = 0x455 }, 32, 89);
        Settle(new Static(0xE3D) { Name = "Raider's stolen cargo" }, 34, 95);
        Settle(new Static(0xE77) { Name = "A salt-stained rum barrel" }, 38, 89);
        this.MarkDirty();
    }
}
