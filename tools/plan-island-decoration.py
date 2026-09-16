from pathlib import Path
import json,random,math
root=Path(__file__).resolve().parents[2]
groups=[];paths=set()
def group(name,entries,house=False):
    groups.append(dict(name=name,house=house,tiles=[dict(id=i,x=x,y=y,z=z) for i,x,y,z in entries]))
def one(name,i,x,y,z=0,house=False):group(name,[(i,x,y,z)],house)
def path(points,width=1):
    for (ax,ay),(bx,by) in zip(points,points[1:]):
        n=max(abs(bx-ax),abs(by-ay))
        for s in range(n+1):
            x=round(ax+(bx-ax)*s/max(n,1));y=round(ay+(by-ay)*s/max(n,1))
            for dx in range(-width,width+1):
                for dy in range(-width,width+1):
                    if abs(dx)+abs(dy)<=width:paths.add((x+dx,y+dy))
# Broad, bent routes lead somewhere; the central arrival square remains open.
path([(68,85),(70,91),(78,97),(81,109),(80,120),(82,130),(87,136)],2)
path([(76,96),(68,103),(60,106)],1)
path([(81,109),(94,112),(105,104),(109,100)],1)
path([(94,112),(99,121),(92,132),(87,136)],1)
path([(87,98),(92,87),(94,76),(98,69),(111,61)],1)
path([(47,85),(42,78),(43,63),(45,53),(53,45),(64,44)],1)
for x,y in sorted(paths):one('Worn shellstone footpath',0x519+(x*3+y)%4,x,y)
def nearpath(x,y,r=2):return any((x+dx,y+dy) in paths for dx in range(-r,r+1) for dy in range(-r,r+1))
def planting(x,y,kind=0):
    if nearpath(x,y):return
    ids=[(0xCCC,0xCCE),(0xCD0,0xCD1),(0xCDA,0xCDB),(0xC95,),(0xC96,),(0xCAA,)]
    tree=ids[kind%len(ids)]
    group('Saltwind grove',[(i,x,y,0) for i in tree])
    for dx,dy,i in [(-1,1,0xCA1),(1,0,0xC9F),(0,2,0xCC4),(-2,-1,0xCAD)]:
        if not nearpath(x+dx,y+dy,0):one('Wild coastal understory',i,x+dx,y+dy)
# Asymmetric clumps around the outside, with gaps for movement and views.
clusters=[(36,91,4),(39,103,3),(48,124,5),(59,134,4),(70,140,4),(106,134,5),(120,123,4),(134,108,4),(138,91,3),(133,75,5),(129,58,3),(113,38,4),(96,38,5),(36,70,3),(41,52,4)]
for cx,cy,k in clusters:
    for j,(dx,dy) in enumerate([(0,0),(4,3),(-3,5),(3,-4),(-5,-2)]):planting(cx+dx,cy+dy,k+j)
# Fill out the existing grove without turning it into a regular grid.
for j,(x,y) in enumerate([(101,43),(108,48),(116,45),(124,50),(101,59),(110,62),(120,65),(125,72),(113,75)]):
    for dx,dy,i in [(0,0,0xC99),(-2,1,0xCA1),(2,0,0xCC4),(-1,-2,0xC8A)]:one('Grove floor planting',i,x+dx,y+dy)
# Four cultivated beds around the existing working crops, each with a clean access aisle.
for x,y,w,h in [(55,90,4,4),(61,92,4,4),(53,98,4,4),(66,95,4,4)]:
    for dx in range(w):
        for dy in range(h):one('Kitchen garden soil',0x31F4+(dx+dy)%4,x+dx,y+dy)
for x,y in [(52,91),(52,94),(59,89),(65,89),(70,93),(70,101),(61,101)]:
    one('Garden herbs and flowers',0xC85 if x%2 else 0xC8A,x,y)
for x in range(51,72):
    if x not in range(59,63):one('Kitchen garden split-rail fence',0x3B4,x,88)
for y in range(89,101):one('Kitchen garden split-rail fence',0x3B5,50,y)
group('Gardeners work corner',[(0xB49,72,103,0),(0xF39,72,103,5),(0x9AC,73,103,0),(0x1500,74,104,0),(0xE77,73,101,0)])
group('Orchard apiary',[(0x91A,102,116,0),(0x91A,104,117,0),(0xC85,101,117,0),(0xC83,105,118,0)])
# Low fences and modest stone outcrops anchor the resource areas.
for x,y in [(104,86),(109,84),(116,83),(121,87),(122,94),(120,99)]:
    group('Weathered ore bank',[(0x1363,x,y,0),(0x1367,x+1,y,0),(0xCA5,x,y+1,0)])
group('Prospectors rest',[(0xE3D,124,96,0),(0xE77,124,97,0),(0xF39,125,97,0),(0x1BDD,126,96,0),(0xA58,126,98,0)])
# Open-air crew mess, with actual assembled tables and supported tabletop props.
def table(name,x,y,house=False,z=0):
    group(name,[(0xB34,x,y,z),(0xB35,x+1,y,z),(0xB36,x+2,y,z),(0x9D7,x,y,z+6),(0x99B,x+1,y,z+6),(0xFFB,x+2,y,z+6)],house)
    one('Crew bench',0xB2C,x+1,y+1,z,house)
table('Harbor supper table',94,126)
group('Rum and provisions',[(0xE77,97,128,0),(0xE77,98,128,0),(0x99B,97,128,5),(0xE3D,98,129,0),(0x9AC,97,130,0)])
group('Driftwood campfire',[(0xDE3,91,128,0),(0xA55,90,130,0),(0xA56,93,130,0),(0x1BDD,92,127,0)])
# Small roofed dockside warehouse. No walls across its open working frontage.
for x in range(93,100):
    for y in range(138,144):one('Chandlery timber deck',0x4A9+(x+y)%4,x,y)
for x,y in [(93,138),(99,138),(93,143),(99,143)]:one('Chandlery roof post',0x9,x,y)
for x in range(93,100):
    for y in range(138,144):one('Chandlery shingled awning',0x5B5 if x<96 else 0x5B4 if x>96 else 0x5B3,x,y,20+3*min(x-93,99-x))
for x,y in [(94,139),(95,139),(98,139),(98,142)]:
    group('R.E.C. bonded export cargo',[(0xE3D,x,y,0),(0xE3F,x,y,3),(0x14F8,x,y,6)])
group('Chandlers barrel rack',[(0xE77,94,142,0),(0xE77,95,142,0),(0xE77,94,142,5)])
group('Fishing net repair bench',[(0xB49,89,141,0),(0xDC8,89,141,5),(0xDBF,90,141,0),(0x9CE,89,142,0)])
for x,y in [(84,142),(90,151),(91,158),(97,162)]:
    one('Harbor rope coil',0x14F8,x,y)
group('The mornings catch',[(0xE3D,94,160,0),(0x9CC,94,160,3),(0x9CE,95,160,0),(0xDCA,95,161,0)])
# Signal station faces the ocean; useful open space remains around the patrol board.
group('Southern signal battery',[(0xE6C,75,137,0),(0xE74,74,137,0),(0xE77,73,137,0),(0x14F7,75,139,0)])
# Lamps are supported, outside the path rather than in its centre.
for x,y in [(66,88),(74,96),(84,105),(78,116),(84,130),(90,136),(100,141),(102,107),(48,102),(43,82)]:
    while (x,y) in paths:x-=1
    group('Lantern along the trade route',[(0xA1F,x,y,0),(0xA15,x,y,10)])
# Front garden borders frame the guild house, not its entrances.
for cx,cy in [(55,86),(60,88),(82,86),(87,78),(88,67),(49,73)]:
    for dx,dy,i in [(0,0,0xC99),(1,1,0xC83),(-1,1,0xC8A),(2,0,0xC8C),(-1,-1,0xCA4)]:
        if not nearpath(cx+dx,cy+dy,0):one('Guild garden border',i,cx+dx,cy+dy)
# Interior details: coherent corners; do not alter the custom-house design.
table('Galley breakfast table',-8,-10,True,7)
for x,y,z,i,name in [(-5,-13,7,0xA97,'Galley crockery shelf'),(-13,-10,7,0xE77,'Salted provisions'),(-5,-8,7,0x11CA,'Galley herb pot'),(-13,-12,27,0xA97,'Recovered navigators books'),(-13,-10,27,0xA98,'Company archives'),(-4,-12,27,0x11C8,'Council room greenery'),(-4,-6,27,0xE77,'Council rum cask'),(-13,-12,47,0xA4D,'Captains traveling trunk'),(-4,-12,47,0xA97,'Captains nautical library'),(-4,-10,47,0x11CA,'Captains flowering plant'),(-13,-9,47,0xB2F,'Captains reading chair'),(-12,10,27,0xE3F,'Crew sea chest'),(-10,10,27,0xE3F,'Crew sea chest'),(-8,10,27,0xE77,'Crew water barrel'),(-11,12,27,0xA58,'Spare crew bedding'),(13,-12,27,0xA98,'Sail loft reference shelf'),(13,-9,27,0x11CA,'Apothecary herbs'),(12,1,27,0xB49,'Navigators spare chart desk'),(11,1,33,0x14EB,'Coastal chart')]:
    # Elevated chart belongs on its own supported desk.
    if name=='Coastal chart':continue
    one(name,i,x,y,z,True)
group('Chartmakers desk',[(0xB49,10,-3,27),(0x14EB,10,-3,32),(0x1047,10,-3,32)],True)
# Complete woven rugs (nine-piece native art), not isolated rug corners.
for cx,cy,z in [(-9,-10,47),(-7,-11,27),(-9,10,27)]:
    group('Handwoven sailors rug',[(0xAAA,cx-1,cy-1,z),(0xAAB,cx,cy-1,z),(0xAAC,cx+1,cy-1,z),(0xAAD,cx-1,cy,z),(0xAAE,cx,cy,z),(0xAAF,cx+1,cy,z),(0xAB0,cx-1,cy+1,z),(0xAB1,cx,cy+1,z),(0xAB2,cx+1,cy+1,z)],True)
# Remove duplicate plan tiles. Bound terrain objects to actual level dry land.
mapfile=root/'verification/HavenHouseTestData/map1.mul'
import struct
with mapfile.open('rb') as f:
    def land(x,y):
        ax=4128+x;ay=2800+y;f.seek((ax//8*512+ay//8)*196+4+(ay%8*8+ax%8)*3)
        return struct.unpack('<Hb',f.read(3))
    clean=[];seen=set()
    for g in groups:
        if not g['house'] and any(land(t['x'],t['y'])[1]!=0 for t in g['tiles']):continue
        tiles=[]
        for t in g['tiles']:
            key=(g['house'],t['id'],t['x'],t['y'],t['z'])
            if key not in seen:tiles.append(t);seen.add(key)
        if tiles:clean.append(dict(g,tiles=tiles))
groups=clean
(root/'artifacts/island-decoration-plan.json').write_text(json.dumps(groups,indent=2))
# Source data is the reviewed manifest, not a runtime external file dependency.
lines=['namespace Server.UOOffline;','', 'internal static partial class HavenIslandDecoration','{','    internal static readonly Group[] Plan =','    {']
for g in groups:
    ts=', '.join(f"new(0x{t['id']:X}, {t['x']}, {t['y']}, {t['z']})" for t in g['tiles'])
    lines.append(f'        new("{g["name"]}", {str(g["house"]).lower()}, new Tile[] {{ {ts} }}),')
lines+=['    };','}']
out=root/'haven-fixes/playerbots/source/CustomBots/UOOffline/HavenIslandDecorationPlan.cs';out.write_text('\n'.join(lines)+'\n')
print(len(groups),'groups',sum(len(g['tiles']) for g in groups),'tiles',len({t['id'] for g in groups for t in g['tiles']}),'distinct art pieces')
