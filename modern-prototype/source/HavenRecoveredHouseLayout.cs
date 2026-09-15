using System;
using System.Linq;
using Server.Multis;

namespace Server.HavenPrototype {

public partial class HavenRecoveredHeadquarters
{
    internal void BuildPirateLayout()
    {
        var mcl = GetEmptyFoundation();
        if (mcl.Width != 31 || mcl.Height != 32) { throw new InvalidOperationException("Install the Haven large-plot client and server multi files first."); }
        foreach (var tile in mcl.List.ToArray())
        { if (tile.m_OffsetZ == 7) { mcl.Remove(tile.m_ItemID, tile.m_OffsetX, tile.m_OffsetY, tile.m_OffsetZ); } }
        void Tile(int id, int x, int y, int z) => mcl.Add(id, x, y, z);
        void Floor(int x1, int y1, int x2, int y2, int z)
        { for (var x = x1; x <= x2; x++) { for (var y = y1; y <= y2; y++) { Tile(0x4A9 + (x + y + 60) % 4, x, y, z); } } }
        void Room(int x1, int y1, int x2, int y2, int z, bool plaster, bool westDoor = false, bool northDoor = false)
        {
            for (var x = x1 + 1; x <= x2; x++)
            {
                if (!northDoor || Math.Abs(x - (x1 + x2) / 2) > 1) { Tile(plaster ? x % 3 == 0 ? 0x13A : 0x128 : x % 3 == 0 ? 0xE : 0x7, x, y1, z); }
                if (Math.Abs(x - (x1 + x2) / 2) > 1) { Tile(plaster ? x % 3 == 0 ? 0x13A : 0x128 : x % 3 == 0 ? 0xE : 0x7, x, y2, z); }
            }
            for (var y = y1 + 1; y <= y2; y++)
            {
                if (!westDoor || Math.Abs(y) > 1) { Tile(plaster ? y % 3 == 0 ? 0x13B : 0x129 : y % 3 == 0 ? 0xF : 0x8, x1, y, z); }
                Tile(plaster ? y % 3 == 0 ? 0x13B : 0x129 : y % 3 == 0 ? 0xF : 0x8, x2, y, z);
            }
            Tile(plaster ? 0x12A : 0x9, x1, y1, z);
        }
        void Roof(int x1, int y1, int x2, int y2, int baseZ, bool slate)
        {
            var ridge = (x1 + x2) / 2;
            for (var x = x1; x <= x2; x++)
            for (var y = y1; y <= y2; y++)
            { var z = baseZ + 3 * Math.Min(x - x1, x2 - x); Tile((slate ? 0x595 : 0x5B3) + (x < ridge ? 2 : x > ridge ? 1 : 0), x, y, z); }
        }
        // A paved yard with three separate footprints, rather than one full-plot building.
        for (var x = -14; x <= 15; x++)
        for (var y = -14; y <= 15; y++)
        {
            var indoor = x <= -3 && y <= -3 || x >= 4 && x <= 14 && y <= 3 || x <= -4 && y >= 4 && y <= 13;
            Tile(indoor ? 0x4A9 + (x + y + 60) % 4 : 0x519, x, y, 7);
        }
        // Captain's house: galley, guild council, captain's quarters. Three real usable floors.
        for (var z = 7; z <= 47; z += 20)
        { if (z > 7) { Floor(-13,-13,-3,-3,z); } Room(-14,-14,-3,-3,z,true); }
        Roof(-14,-14,-2,-2,67,false);
        // East lodge: forge, sailmaking and carpentry; charts/alchemy in the loft.
        Room(4,-13,14,3,7,false,true); Floor(5,-12,14,3,27); Room(4,-13,14,3,27,true,true);
        Roof(4,-13,14,4,47,true);
        // Barn and crew loft, with a wide entrance and an upper bridge toward the house.
        Room(-14,4,-4,13,7,false,false,true); Floor(-13,5,-4,13,27); Room(-14,4,-4,13,27,false,false,true);
        Roof(-14,4,-4,14,47,false);
        // Open galleries and bridges; the ground courtyard remains open to the sky.
        Floor(-13,-2,3,2,27); Floor(4,-1,4,1,27); Floor(-10,3,-7,4,27);
        Floor(-13,-2,0,2,47);
        for (var x = -13; x <= 3; x++) { if (x < -10 || x > -7) { Tile(0x12,x,2,27); } }
        for (var x = -13; x <= 0; x++) { Tile(0x12,x,2,47); }
        for (var y = -1; y <= 2; y++) { Tile(0x11,0,y,47); }
        for (var y = 3; y <= 4; y++) { Tile(0x11,-10,y,27); Tile(0x11,-7,y,27); }
        // Galley chimney and timber columns supporting the overhanging galleries.
        foreach (var x in new[] {-13,-5,2}) { Tile(0x9,x,2,7); }
        // Low courtyard boundary, broad entrance, and a sheltered cargo counter.
        for (var x = -14; x <= 15; x++) { if (Math.Abs(x) > 2) { Tile(0x12,x,15,7); } }
        for (var y = 4; y < 15; y++) { Tile(0x11,15,y,7); }
        for (var x = 0; x <= 4; x++) { Tile(0x4A9,x,6,27); }
        Tile(0x9,0,6,7); Tile(0x9,4,6,7);
        CurrentState = new DesignState(this, mcl); CurrentState.Revision = ++LastRevision;
        DesignState = new DesignState(CurrentState); BackupState = new DesignState(CurrentState);
    }
}

}