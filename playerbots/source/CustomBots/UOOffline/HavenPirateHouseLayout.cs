using System;
using System.Linq;
using Server.Multis;

namespace Server.UOOffline;

public partial class HavenPirateHeadquarters
{
    internal void BuildPirateLayout()
    {
        var mcl = GetEmptyFoundation();
        if (mcl.Width != 31 || mcl.Height != 32) { throw new InvalidOperationException("Install the Haven large-plot client and server multi files first."); }
        // These are ordinary custom-house components: the owner can edit them with the building tool.
        foreach (var tile in mcl.List.ToArray())
        { if (tile.OffsetZ == 7) { mcl.Remove(tile.ItemId, tile.OffsetX, tile.OffsetY, tile.OffsetZ); } }
        void Tile(int id, int x, int y, int z) => mcl.Add(id, x, y, z);
        void Floor(int x1, int y1, int x2, int y2, int z)
        { for (var x = x1; x <= x2; x++) { for (var y = y1; y <= y2; y++) { Tile(0x4A9 + (x + y + 60) % 4, x, y, z); } } }
        void Room(int x1, int y1, int x2, int y2, int z, bool frontDoor = true)
        {
            for (var x = x1 + 1; x <= x2; x++)
            {
                Tile(x % 4 == 0 ? 0xE : 0x7, x, y1, z);
                if (!frontDoor || Math.Abs(x - (x1 + x2) / 2) > 1) { Tile(x % 4 == 0 ? 0xE : 0x7, x, y2, z); }
            }
            for (var y = y1 + 1; y <= y2; y++)
            { Tile(y % 4 == 0 ? 0xF : 0x8, x1, y, z); Tile(y % 4 == 0 ? 0xF : 0x8, x2, y, z); }
            Tile(0x9, x1, y1, z);
        }
        // Cargo/workshop deck and a full gallery above, with a broad open entrance.
        Floor(-14, -14, 15, 15, 7);
        Room(-15, -15, 15, 14, 7);
        Floor(-14, -14, 15, 14, 27);
        // The guild hall is set back from the front: its balcony is an open promenade.
        Room(-15, -15, 15, 7, 27);
        // Upper deck has a central captain's cabin and two smaller rear lookout rooms.
        Floor(-14, -14, 15, 14, 47);
        Room(-8, -7, 8, 7, 47);
        Room(-15, -15, -9, -8, 47);
        Room(9, -15, 15, -8, 47);
        Floor(-7, -6, 8, 7, 67);
        Floor(-14, -14, -9, -8, 67);
        Floor(10, -14, 15, -8, 67);
        // Low timber bulwarks around the open decks, rather than a solid three-storey box.
        for (var x = -14; x <= 15; x++) { Tile(0x12, x, 14, 27); Tile(0x12, x, 14, 47); }
        for (var y = -14; y < 14; y++) { Tile(0x11, -15, y, 47); Tile(0x11, 15, y, 47); }
        for (var x = -8; x <= 8; x++) { Tile(0x12, x, -15, 47); }
        // Visual rigging posts, framing the company colors and lookout terrace.
        for (var z = 27; z <= 47; z += 20) { Tile(0x9, -9, 13, z); Tile(0x9, 9, 13, z); }
        CurrentState = new DesignState(this, mcl);
        CurrentState.Revision = ++LastRevision;
        DesignState = new DesignState(CurrentState); BackupState = new DesignState(CurrentState);
    }
}

