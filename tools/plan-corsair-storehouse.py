"""Author the first enclosed outbuilding; coordinates are relative to the estate.

Writes a tile manifest, not proprietary artwork. This is a review sample and does
not alter the live world. The next migration must preserve player property.
"""
import json
from pathlib import Path

tiles = []
def add(name, art, x, y, z=0):
    tiles.append(dict(name=name, id=art, x=x, y=y, z=z))

# Retain the cargo-store footprint and its eastern connection to the pier.
for x in range(93, 101):
    for y in range(137, 144):
        add('storehouse deck', 0x4A9 + (x+y)%4, x, y)

# Weathered timber siding suits a working shed. The two-tile loading doorway
# faces the path; window panels light the desk and repair bench.
for x in range(94, 100):
    for y in (137, 143):
        if x == 99 and y == 143:
            continue
        add('wall', 0xE if x in (95, 98) else 0x7, x, y)
for y in range(138, 144):
    add('wall', 0xF if y == 140 else 0x8, 93, y)
    if y not in (140, 141, 143):
        add('wall', 0x8, 99, y)
add('corner', 0x9, 93, 137)
add('corner', 0x6, 99, 143)
for y in (140, 141):
    add('loading lintel', 0x143, 99, y, 17)

# Native continuous roof slopes and ridge, with three-unit rises. Roof edges
# follow the original classic timber/plaster construction convention.
for x in range(94, 101):
    rise = 3*min(x-94, 100-x)
    for y in range(138, 145):
        add('roof', 0x5C4 if x<97 else 0x5C3 if x>97 else 0x5C2, x, y, 20+rise)
    for y in (137, 143):
        for z in range(20, 20+rise, 3):
            add('gable infill', 0x18, x, y, z)

# Cargo stays against the rear wall; the centre and loading bay remain clear.
for x in (94, 95):
    add('export crate', 0xE3D, x, 138)
add('stacked crate', 0xE3F, 94, 138, 3)
add('stacked crate', 0xE3E, 95, 138, 3)
for y in (139, 140):
    add('provision barrel', 0xE77, 94, y)
add('dry provisions', 0x1039, 94, 141)
add('spare boards', 0x1BDD, 94, 142)
add('rigging shelf', 0xA9D, 96, 138)
add('rigging shelf', 0xA9D, 97, 138)
add('shelf rope', 0x14F8, 96, 138, 6)
add('shelf cloth', 0xE34, 97, 138, 6)
add('shelf tools', 0x102A, 96, 138, 12)
add('shelf fittings', 0x104F, 97, 138, 12)
add('net bench', 0xB90, 96, 142)
add('net mending', 0xDC8, 96, 142, 6)
add('bench saw', 0x1034, 97, 142, 6)
add('repair stool', 0xB2C, 96, 141)
add('rope stock', 0x14F8, 95, 141)
add('shipping desk', 0xB49, 98, 138)
add('manifest', 0xFF1, 98, 138, 5)
add('ink and pen', 0xFBF, 98, 138, 6)
add('chair', 0xB2C, 98, 139)
add('sign support post', 0x9, 101, 138)
add('sign bracket', 0xB98, 101, 138, 5)
add('R.E.C. Chandlery & Exports', 0xBD0, 101, 138, 5)
add('wall lantern', 0xA1A, 99, 139, 10)
add('outgoing shipment', 0xE3D, 100, 142)
add('outgoing rigging', 0x14F8, 100, 142, 3)
add('dockside provision barrel', 0xE77, 100, 143)

if __name__ == '__main__':
    import argparse
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('output',type=Path)
    args=p.parse_args()
    args.output.write_text(json.dumps(tiles,indent=2)+'\n')
    print(f'{len(tiles)} native-art placements written to {args.output}')

