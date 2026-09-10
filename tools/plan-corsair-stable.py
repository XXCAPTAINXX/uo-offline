"""Native-art stable review layout; writes placements without changing a world."""
import argparse
import json
from pathlib import Path

tiles=[]
def add(name, art, x, y, z=0):
    tiles.append(dict(name=name,id=art,x=x,y=y,z=z))

# Two stalls on the west, a three-tile working aisle, and an eastern entrance
# aligned with the settlement path. Timber walls match the approved chandlery.
for x in range(38,45):
    for y in range(91,99):
        add('stable deck',0x4AB if (x,y) in ((42,94),(43,95)) else 0x4A9,x,y)
# A timber threshold keeps the doorway dry. The stable lane is packed earth,
# joining the paved main route at y=105. Its shoulders vary with foot traffic.
for y in (94,95): add('timber threshold',0x4A9,45,y)
lane={(46,y) for y in range(94,105)} | {(47,y) for y in range(94,105)}
lane.update((48,y) for y in (95,96,100,103,104))
for x,y in sorted(lane): add('stable dirt lane',0x31F4+(x*3+y)%4,x,y)
for x,y in ((46,93),(48,94),(48,97),(48,99),(48,102)):
    add('worn dirt shoulder',0x914,x,y)
for x,y in ((45,97),(45,100),(49,96),(49,101)):
    add('lane grass',0xCAC if y%2 else 0xCAD,x,y)
# Retain just two stones where the lane meets the town paving.
for x,y in ((46,104),(48,104)): add('reused stepping stone',0x519,x,y)
add('tracked straw',0xF35,43,94)
add('foundation flowers',0xC85,38,99)
add('foundation grasses',0xCAD,39,99)
add('foundation stone',0x1364,39,100)
add('signpost grass',0xCAC,45,91)
for x in range(39,45):
    add('wall',0xE if x in (40,43) else 0x7,x,91)
    if x!=44: add('wall',0x7,x,98)
for y in range(92,99):
    add('wall',0xF if y in (92,96) else 0x8,38,y)
    if y not in (94,95,98): add('wall',0x8,44,y)
add('corner',0x9,38,91)
add('corner',0x6,44,98)
for y in (94,95): add('door lintel',0x143,44,y,17)
for x in range(39,46):
    rise=3*min(x-39,45-x)
    for y in range(92,100):
        add('roof',0x5C4 if x<42 else 0x5C3 if x>42 else 0x5C2,x,y,20+rise)
    for y in (91,98):
        for z in range(20,20+rise,3): add('gable infill',0x18,x,y,z)

# Complete matching rails enclose each stall. Real native LightWoodGate doors
# replace the former gaps. The preview shows them closed to make the two pens
# legible; the world installer must instantiate doors, not immovable statics.
for x in (39,40): add('stall divider',0x836,x,94)
add('stall divider corner',0x835,41,94)
for y in (92,95,97): add('stall front rail',0x837,41,y)
for number,y in ((1,93),(2,96)):
    add(f'stall {number} gate',0x843,41,y)
    tiles[-1].update(type='LightWoodGate',facing='NorthCCW',open=False)
for y in (92,95,96):
    add('light straw bedding',0xF35,40,y)
# Feed stays in each back corner, away from the openings and working aisle.
for y in (92,96):
    add('fresh hay',0x100C,39,y)
    add('feed basket',0x9AC,39,y+1)
# Supplies are grouped at the head of the aisle, leaving the length clear.
add('feed barrel',0xE77,43,93)
add('tack shelf',0xA9D,43,92)
add('tack saddle',0xF37,43,92,12)
add('tack rope',0x14F8,43,92,6)
# Paired native WaterTroughSouthAddon components; installation should use the
# working addon rather than two decorative statics.
add('water trough west',0xB43,42,97)
add('water trough east',0xB44,43,97)

# A proper supported sign sits beside, not in, the two-tile entrance.
add('stable signpost',0x9,45,92)
add('sign bracket',0xB98,45,92,5)
add('R.E.C. Stables',0xBD0,45,92,5)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output',type=Path)
    args=parser.parse_args()
    args.output.write_text(json.dumps(tiles,indent=2)+'\n')
    print(f'{len(tiles)} stable placements written')
