using System;
using Server.Mobiles;
namespace Server.UOOffline;
public static class HavenProvisioner
{
    internal static readonly Point3D Site = new(3503, 2562, 21);
    public static void Initialize() => Timer.DelayCall(TimeSpan.FromSeconds(20), Ensure);
    internal static void Ensure()
    {
        foreach (var vendor in Map.Trammel.GetMobilesInRange<Provisioner>(Site, 5)) { return; }
        if (!HavenRecovery.FindLocation(Site, out var location, 3)) { return; }
        var provisioner = new Provisioner { Name = "Mara Wren", Female = true, Body = 0x191, Home = location, RangeHome = 2 };
        provisioner.MoveToWorld(location, Map.Trammel);
    }
}
