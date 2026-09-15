using System;
using System.Linq;
using ModernUO.Serialization;

namespace Server.UOOffline;

public partial class HavenPirateHeadquarters
{
    internal void RefineBroadLadders()
    {
        if(Deleted || !HasCompound || Customizer!=null) { return; }
        var ladders=CompanyFixtures.OfType<HavenPirateStair>().Where(i=>!i.Deleted).ToArray();
        if(ladders.Length!=DirectLadderSites.Length) { throw new InvalidOperationException("Expected eight floor ladders; property was preserved."); }
        foreach(var ladder in ladders)
        {
            var site=new Point3D(ladder.X-X,ladder.Y-Y,ladder.Z-Z);
            if(Array.IndexOf(DirectLadderSites,site)<0) { throw new InvalidOperationException("A ladder has been moved; restore its position before updating its steps."); }
        }
        foreach(var ladder in ladders)
        {
            ladder.ItemID=HavenPirateStair.LadderArt;
            var step=CompanyFixtures.OfType<HavenPirateStairBase>().FirstOrDefault(i=>!i.Deleted && i.Ladder==ladder);
            if(step==null)
            {
                step=new HavenPirateStairBase { Ladder=ladder,Name=ladder.Name.Replace("ladder","steps",StringComparison.Ordinal) };
                // Same footprint as the ladder, leaving its approach and destination untouched.
                Place(step,ladder.X-X,ladder.Y-Y,ladder.Z-Z-1);
            }
        }
        this.MarkDirty();
    }
}

[SerializationGenerator(0)]
public partial class HavenPirateStairBase : Item
{
    [SerializableField(0)] private HavenPirateStair _ladder;
    [Constructible] public HavenPirateStairBase() : base(0x722) { Name="R.E.C. wooden ladder steps";Movable=false; }
    public override void OnDoubleClick(Mobile from)
    {
        if(Ladder?.Deleted!=false || Parent!=null || Ladder.Parent!=null || Map!=Ladder.Map || Location!=Ladder.Location)
        { from.SendMessage("These steps must stay attached to their house ladder.");return; }
        Ladder.OnDoubleClick(from);
    }
    public override void OnDelete() { Ladder=null;base.OnDelete(); }
}
