using Server;
using Server.Engines.Doom;
using Server.Items;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class HavenWorldTestsDoomCleanup
{
    public HavenWorldTestsDoomCleanup() { _ = new HavenWorldTests(); }

    [SkippableFact]
    public void GauntletCleanupRequiresMatchingTypeEvenAtAnExplicitHeight()
    {
        TileDataRequirement.SkipIfMissing();
        const int x = 433;
        const int y = 326;
        var decoration = new Static(0x519);
        var gate = new ConfirmationMoongate();
        try
        {
            decoration.MoveToWorld(new Point3D(x, y, 4), Map.Malas);
            GenGauntlet.RemoveItem<ConfirmationMoongate>(x, y, 4);
            Assert.False(decoration.Deleted);

            gate.MoveToWorld(new Point3D(x, y, 8), Map.Malas);
            GenGauntlet.RemoveItem<ConfirmationMoongate>(x, y, 4);
            Assert.False(decoration.Deleted);
            Assert.False(gate.Deleted);

            GenGauntlet.RemoveItem<ConfirmationMoongate>(x, y, 8);
            Assert.True(gate.Deleted);
            Assert.False(decoration.Deleted);
        }
        finally
        {
            gate.Delete();
            decoration.Delete();
        }
    }

    [SkippableFact]
    public void GauntletCleanupWildcardHeightStillRemovesOnlyTheRequestedType()
    {
        TileDataRequirement.SkipIfMissing();
        const int x = 433;
        const int y = 326;
        var decoration = new Static(0x519);
        var gate = new ConfirmationMoongate();
        try
        {
            decoration.MoveToWorld(new Point3D(x, y, -1), Map.Malas);
            GenGauntlet.RemoveItem<ConfirmationMoongate>(x, y);
            Assert.False(decoration.Deleted);

            gate.MoveToWorld(new Point3D(x, y, 8), Map.Malas);
            GenGauntlet.RemoveItem<ConfirmationMoongate>(x, y);
            Assert.True(gate.Deleted);
            Assert.False(decoration.Deleted);
        }
        finally
        {
            gate.Delete();
            decoration.Delete();
        }
    }
}
