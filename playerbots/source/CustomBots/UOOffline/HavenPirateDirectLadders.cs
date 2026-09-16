using System;
using System.Linq;
using Server.Items;
using Server.CustomBots;
using Server.Mobiles;
using Server.Spells;

namespace Server.UOOffline;

public partial class HavenPirateHeadquarters
{
    internal static readonly Point3D[] DirectLadderSites =
    {
        new(-2,1,7), new(-9,-4,27), new(-9,-4,47), new(5,1,7),
        new(5,1,27), new(-5,11,7), new(-5,11,27), new(-7,-4,27)
    };
    internal static readonly Point3D[] DirectLadderLandings =
    {
        new(-9,-3,27), new(-2,2,7), new(-7,-3,27), new(5,2,27),
        new(5,2,7), new(-5,12,27), new(-5,12,7), new(-9,-3,47)
    };
    private static readonly string[] DirectLadderNames =
    {
        "R.E.C. ladder up to guild hall", "R.E.C. ladder down to courtyard",
        "R.E.C. ladder down to guild hall", "R.E.C. ladder up to workshop loft",
        "R.E.C. ladder down to workshop", "R.E.C. ladder up to crew loft",
        "R.E.C. ladder down to barn", "R.E.C. ladder up to captain's quarters"
    };
    internal void RefineDirectLadders()
    {
        if (Deleted || !HasCompound) { return; }
        if (CompanyFixtures.Any(i => !i.Deleted && i.Name == "R.E.C. direct ladders v4")) { RefineBroadLadders(); return; }
        if (Customizer != null) { throw new InvalidOperationException("Finish house customization before adjusting ladders."); }
        var ladders = CompanyFixtures.OfType<HavenPirateStair>().Where(i => !i.Deleted).ToArray();
        if (ladders.Length != 7) { throw new InvalidOperationException("Expected seven existing ladders; property was preserved."); }
        var newSite = DirectLadderSites[7];
        var barrelSite = new Point3D(-10,12,7);
        var chestSite = new Point3D(9,0,7);
        foreach (var site in new[] { newSite, barrelSite, chestSite, DirectLadderSites[1], DirectLadderSites[2] })
        {
            var point = new Point3D(X+site.X,Y+site.Y,Z+site.Z);
            if (!Map.CanFit(point,16,checkMobiles:false)) { throw new InvalidOperationException($"A ladder or storage destination is occupied at {point}; property was preserved."); }
        }
        var barrel = CompanyFixtures.FirstOrDefault(i => !i.Deleted && i.Name == "Stable feed barrel");
        var chest = CompanyFixtures.OfType<GuildProfessionChest>().FirstOrDefault(i => !i.Deleted && i.Role == GuildStorageRole.Resources);
        var movedLadders = ladders.Where(l => l.X-X == -5 && l.Y-Y == -4).ToDictionary(l => l,l => l.Location);
        var oldChest = chest?.Location ?? Point3D.Zero;
        var oldBarrel = barrel?.Location ?? Point3D.Zero;
        try
        {
            foreach (var entry in movedLadders) { entry.Key.MoveToWorld(new Point3D(X-9,Y-4,entry.Key.Z),Map); }
            chest?.MoveToWorld(new Point3D(X+chestSite.X,Y+chestSite.Y,Z+chestSite.Z),Map);
            barrel?.MoveToWorld(new Point3D(X+barrelSite.X,Y+barrelSite.Y,Z+barrelSite.Z),Map);
            foreach (var site in DirectLadderSites)
            {
                var approach = new Point3D(X+site.X,Y+site.Y+1,Z+site.Z);
                if (!IsInside(approach,16) || !Map.CanFit(approach,16,checkMobiles:false))
                { throw new InvalidOperationException($"Ladder approach is blocked at {approach}; furnishings were restored."); }
            }
        }
        catch
        {
            foreach (var entry in movedLadders) { entry.Key.MoveToWorld(entry.Value,Map); }
            chest?.MoveToWorld(oldChest,Map);
            barrel?.MoveToWorld(oldBarrel,Map);
            throw;
        }
        Place(new HavenPirateStair { Headquarters=this },newSite.X,newSite.Y,newSite.Z-1);
        foreach (var ladder in CompanyFixtures.OfType<HavenPirateStair>())
        {
            var index = Array.IndexOf(DirectLadderSites,new Point3D(ladder.X-X,ladder.Y-Y,ladder.Z-Z));
            if (index >= 0) { ladder.Name = DirectLadderNames[index]; }
        }
        Place(new Static(1) { Name="R.E.C. direct ladders v4", Visible=false },1,14,6);
        RefineBroadLadders();
        this.MarkDirty();
    }
    internal bool Climb(Mobile from, HavenPirateStair ladder)
    {
        if (ladder?.Deleted != false || ladder.Headquarters != this || !CompanyAccess(from) || !from.Alive ||
            Customizer != null || from.Map != Map || !from.InRange(ladder,2) || from.Z<ladder.Z || from.Z-ladder.Z>8 ||
            ((from.X != ladder.X || from.Y != ladder.Y) && !from.InLOS(ladder)) ||
            from.Spell != null || SpellHelper.CheckCombat(from)) { return false; }
        var index = Array.IndexOf(DirectLadderSites,new Point3D(ladder.X-X,ladder.Y-Y,ladder.Z-Z));
        if (index < 0) { return false; }
        var landing = DirectLadderLandings[index];
        var point = new Point3D(X+landing.X,Y+landing.Y,Z+landing.Z);
        if (!IsInside(point,16) || !Map.CanSpawnMobile(point)) { return false; }
        BaseCreature.TeleportPets(from,point,Map);
        from.MoveToWorld(point,Map);
        return true;
    }
}
