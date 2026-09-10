"""L-shaped shipwright workshop review layout; no live-world mutations."""
import argparse
import json
from pathlib import Path

tiles=[]
def add(name,art,x,y,z=0,**extra):
    tiles.append(dict(name=name,id=art,x=x,y=y,z=z,**extra))

# Main carpentry room, offset sailmaking wing, and recessed southern porch.
floor={(x,y) for x in range(92,99) for y in range(76,83)}
floor|={(x,y) for x in range(98,105) for y in range(79,84)}
floor|={(x,y) for x in range(93,99) for y in range(83,86)}
for x,y in sorted(floor): add('workshop floor',0x4AB if (x,y) in ((96,82),(95,84)) else 0x4A9,x,y)
for x in range(93,99):
    add('wall',0xE if x in (94,97) else 0x7,x,76)
    if x not in (95,96,98): add('front wall',0xE if x==94 else 0x7,x,82)
for y in range(77,83):
    add('wall',0xF if y in (78,81) else 0x8,92,y)
    if y not in (80,81,82): add('front wall',0xF if y==78 else 0x8,98,y)
add('corner',0x9,92,76)
add('front wall',0x6,98,82)
for x in (95,96): add('door lintel',0x142,x,82,17)
for x in range(99,105):
    add('wall',0xE if x in (100,103) else 0x7,x,79)
    if x!=104: add('front wall',0xE if x in (100,102) else 0x7,x,83)
for y in range(80,84):
    if y!=83: add('front wall',0xF if y==81 else 0x8,104,y)
add('front wall',0x6,104,83)
add('front wall',0x8,98,83)

# Main ridge runs north/south; the shorter wing turns through ninety degrees.
for x in range(93,100):
    rise=3*min(x-93,99-x)
    for y in range(77,84):
        add('roof',0x5C4 if x<96 else 0x5C3 if x>96 else 0x5C2,x,y,20+rise)
    for y in (76,82):
        for z in range(20,20+rise,3): add('gable',0x18,x,y,z)
for y in range(80,85):
    rise=3*min(y-80,84-y)
    for x in range(100,106):
        add('roof',0x5D0 if y<82 else 0x5CF if y>82 else 0x5CE,x,y,20+rise)
    for z in range(20,20+rise,3): add('gable',0x19,104,y,z)
# Covered working porch with a continuous beam on two corner posts.
for x in range(94,100):
    for y in range(84,87): add('porch roof',0x5CF,x,y,26-3*(y-84))
for x in (93,98): add('porch post',0x9,x,85)
for x in range(94,99): add('porch beam',0x142,x,85,17)
# Earth approaches join the doorway and grove route; no paved perimeter.
# These are private-map edits, requiring matching server and client data.
def land(art,x,y): add('workshop approach terrain',art,x,y,type='Land')
for y in range(86,90):
    for x in (95,96): land(0x76+(x+y)%3,x,y)
    land(0x89,94,y)
    land(0x87,97,y)
# The grove bypass runs away from the walls instead of outlining the building.
for y in range(73,90):
    land(0x77,88,y)
    land(0x89,87,y)
    land(0x87,89,y)
for x in range(89,98):
    land(0x77,x,89)
    land(0x86,x,90)
for x in range(89,101):
    land(0x77,x,73)
    land(0x8A,x,72)
    land(0x86,x,74)
for y in (74,75): land(0x77,100,y)
for x,y in ((93,86),(98,87),(92,88),(99,77),(105,82)):
    add('path-edge grass',0xCAD,x,y)
add('porch corner flowers',0xC85,92,85)
add('wing foundation grass',0xCAC,101,84)

# Material stores against walls; a clear central route joins both workrooms.
for x in (93,94): add('lumber stock',0x1BDD,x,77)
add('fittings crate',0xE3D,95,77)
add('small fittings crate',0xE3F,95,77,3)
add('tool shelf',0xA9D,96,77)
add('saw',0x1034,96,77,6)
add('hammer',0x102A,96,77,12)
add('joinery table',0xB7D,94,80)
add('cut board',0x1BD7,94,80,6)
add('work stool',0xA2A,94,79)
add('hardware barrel',0xE77,93,81)
add('hardware crate',0xE3D,93,80)
add('sail cutting table',0xB90,101,81)
add('sailcloth',0x1765,101,81,6)
add('shears',0xF9E,101,81,6)
add('cloth shelf',0xA9D,103,80)
add('cloth reserve',0x1767,103,80,6)
add('sewing kit',0xF9D,103,80,12)
add('thread tray',0xFA0,103,80,6)
add('sailmakers stool',0xA2A,101,82)
add('cloth chest',0xE3D,100,80)
add('cloth bundle',0x1767,100,80,3)
add('net repair table',0xB90,94,84)
add('net repair',0xDC8,94,84,6)
add('rigging barrel',0xE77,97,84)
add('rigging coil',0x14F8,97,84,5)
# Use the existing porch corner post rather than a third, mismatched upright.
add('sign bracket',0xB97,98,85,7)
add('R.E.C. Shipwright & Sailmaker',0xBCF,98,85,7)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output',type=Path)
    args=parser.parse_args()
    args.output.write_text(json.dumps(tiles,indent=2)+'\n')
    print(f'{len(tiles)} workshop placements written')
