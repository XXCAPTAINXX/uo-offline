"""Organized cookhouse review; relocation of existing fixtures is not deployed."""
import argparse
import json
from pathlib import Path

tiles=[]
def add(name,art,x,y,z=0,**extra):
    tiles.append(dict(name=name,id=art,x=x,y=y,z=z,**extra))

# One room: a rear cooking/storage line and a front dining area. No freestanding
# props in the garden. Existing fixtures are to be relocated, not duplicated.
for x in range(90,99):
    for y in range(119,126):
        hearth=x in (91,92) and y in (120,121,122)
        add('floor',0x519 if hearth else 0x4A9,x,y)
for x in range(91,99):
    add('wall',0xE if x in (94,97) else 0x7,x,119)
    if x not in (94,95,98): add('front wall',0xE if x in (92,97) else 0x7,x,125)
for y in range(120,126):
    add('wall',0xF if y==123 else 0x8,90,y)
    if y!=125: add('front wall',0xF if y in (121,123) else 0x8,98,y)
add('corner',0x9,90,119)
add('front wall',0x6,98,125)
for x in (94,95): add('door lintel',0x142,x,125,17)
for x in range(91,100):
    rise=3*min(x-91,99-x)
    for y in range(120,127): add('roof',0x5C4 if x<95 else 0x5C3 if x>95 else 0x5C2,x,y,20+rise)
    for y in (119,125):
        for z in range(20,20+rise,3): add('gable',0x18,x,y,z)
for x in range(92,97):
    for y in (126,127):
        add('porch floor',0x4A9,x,y)
        add('porch roof',0x5CF,x+1,y+1,23 if y==126 else 20)
for x in (92,96): add('porch post',0x9,x,127)
for x in range(93,97): add('porch beam',0x142,x,127,17)

add('relocated oven',0x92C,91,120,relocateFrom=[96,122,0])
add('relocated oven',0x92B,91,121,relocateFrom=[96,123,0])
add('prep bench',0xB90,93,120)
add('bread board',0x103B,93,120,6)
add('water counter',0xB7E,95,120)
add('water pitcher',0xFF8,95,120,6)
add('provision shelf',0xA9D,97,120)
add('flour stock',0x1039,97,120,6)
add('covered provisions',0x9AC,97,120,12)
add('firewood basket',0x9AC,91,122)
add('stored firewood',0x1BDD,91,122,2,relocateFrom=[97,120,0])
add('dining table',0xB90,94,123,relocateFrom=[93,123,0])
add('meal',0x9D7,94,123,6,relocateFrom=[93,123,6])
add('dining bench',0xB2D,93,124,relocateFrom=[92,124,0])
add('dining bench',0xB2D,95,124,relocateFrom=[94,124,0])
# One garden bed beside the porch, fully bordered and kept outside the route.
for x in (100,101):
    for y in (126,127):
        add('herb soil',0x31F4+(x+y)%4,x,y)
        add('herbs',0xC85,x,y)
for x in (100,101,102):
    for y in (125,128): add('garden border',0x18,x,y)
for y in (126,127,128):
    for x in (99,102): add('garden side',0x19,x,y)
# A single approach from the harbor; no paving ring or scattered stepping tiles.
def land(art,x,y): add('approach',art,x,y,type='Land')
for y in (128,129,130):
    for x in (94,95): land(0x77,x,y)
    land(0x89,93,y)
    land(0x87,96,y)
for x in range(89,94): land(0x77,x,130)

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output',type=Path)
    args=parser.parse_args()
    args.output.write_text(json.dumps(tiles,indent=2)+'\n')
    print(f'{len(tiles)} cookhouse placements')
