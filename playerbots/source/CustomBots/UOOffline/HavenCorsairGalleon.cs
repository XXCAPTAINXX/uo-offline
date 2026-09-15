using ModernUO.Serialization;
using Server.Multis;

namespace Server.UOOffline;

// A larger High Seas hull, retaining the saved patrol controller's LargeBoat base type.
[SerializationGenerator(0)]
public partial class HavenCorsairGalleon : LargeBoat
{
    [Constructible] public HavenCorsairGalleon() { Name = "Saltfang corsair galleon"; AlignDeck(); }
    public override int NorthID => 0x40;
    public override int EastID => 0x41;
    public override int SouthID => 0x42;
    public override int WestID => 0x43;
    public override int HoldDistance => 8;
    public override int TillerManDistance => -6;
    public override Point2D StarboardOffset => new(3,0);
    public override Point2D PortOffset => new(-3,0);
    public override Point3D MarkOffset => new(0,-2,18);
    [AfterDeserialization] private void AlignDeck()
    {
        if (PPlank != null) { PPlank.Z = Z + 15; }
        if (SPlank != null) { SPlank.Z = Z + 15; }
        if (Hold != null) { Hold.Z = Z + 18; }
        if (TillerMan != null) { TillerMan.Z = Z + 18; }
    }
    public override void UpdateComponents() { base.UpdateComponents(); AlignDeck(); }
}
