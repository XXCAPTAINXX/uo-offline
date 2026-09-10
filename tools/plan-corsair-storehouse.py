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

# Complete timber/plaster perimeter. A two-tile loading doorway faces the path.
for x in range(94, 100):
    for y in (137, 143):
        add('wall', 0x13A if x in (95, 98) else 0x136, x, y)
for y in range(138, 144):
    add('wall', 0x13B if y == 140 else 0x137, 93, y)
    if y not in (140, 141):
        add('wall', 0x137, 99, y)
add('corner', 0x12A, 93, 137)
add('corner', 0x132, 99, 143)
add('corner', 0x134, 93, 143)
add('corner', 0x133, 99, 137)
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
            add('gable infill', 0x14A, x, y, z)

# Cargo stays against the rear wall; the centre and loading bay remain clear.
for x in (94, 95, 96):
    add('export crate', 0xE3D, x, 138)
add('stacked crate', 0xE3F, 94, 138, 3)
for y in (139, 140):
    add('provision barrel', 0xE77, 94, y)
add('net bench', 0xB49, 97, 142)
add('net mending', 0xDC8, 97, 142, 5)
add('rope stock', 0x14F8, 95, 142)
add('shipping desk', 0xB49, 98, 138)
add('manifest', 0xFF1, 98, 138, 5)
add('chair', 0xB2C, 98, 139)
add('entrance lantern', 0xB20, 100, 139)

if __name__ == '__main__':
    import argparse
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('output',type=Path)
    args=p.parse_args()
    args.output.write_text(json.dumps(tiles,indent=2)+'\n')
    print(f'{len(tiles)} native-art placements written to {args.output}')
