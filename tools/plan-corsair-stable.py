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
# Covered timber porch: two front posts support the lean-to, leaving the
# central two-tile entrance unobstructed.
for x in (45,46):
    for y in range(93,97):
        add('porch deck',0x4A9,x,y)
        add('porch roof',0x5C3,x,y,23 if x==45 else 20)
for y in (93,96): add('porch support',0x9,46,y)
# Native LAND transitions, not dirt statics placed on grass. These edits need
# matching server/client map generation when installed; this authoring preview
# does not modify either map. 0x89/0x87 feather opposite grass-facing edges.
def land(art,x,y):
    add('stable lane terrain',art,x,y)
    tiles[-1]['type']='Land'
for y in range(94,105):
    for x in (46,47):
        if x==46 and y<=96: continue
        land(0x76+(x+y)%3,x,y)
    land(0x89,45 if y>=97 else 46,y)
    land(0x87,48,y)
# Gravel joins packed dirt to the stone junction instead of isolated square
# paving tiles within the lane.
for x in (46,47): land(0xE1,x,104)
for x,y in ((45,100),(49,96),(49,101)):
    add('lane grass',0xCAC if y%2 else 0xCAD,x,y)
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
