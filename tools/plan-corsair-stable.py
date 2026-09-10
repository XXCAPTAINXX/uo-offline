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
        add('stable deck',0x4A9+(x+y)%4,x,y)
for x in (45,46):
    for y in (94,95): add('entrance paving',0x519+(x+y)%4,x,y)
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
