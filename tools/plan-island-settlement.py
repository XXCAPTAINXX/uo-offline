"""Native-art settlement manifest. Routes are continuous and terminate at named destinations."""
from pathlib import Path
import json,struct
root=Path(__file__).resolve().parents[2]
terrain=(root/'verification/HavenHouseTestData/map1.mul').open('rb')
def dry(x,y):
    ax=4128+x;ay=2800+y
    terrain.seek((ax//8*512+ay//8)*196+4+(ay%8*8+ax%8)*3)
    return struct.unpack('<Hb',terrain.read(3))[1]==0
groups=[]; paving={}
def add(name, tiles):
    groups.append(dict(name='R.E.C. '+name,house=False,tiles=[dict(id=i,x=x,y=y,z=z) for i,x,y,z in tiles]))
def one(name,i,x,y,z=0):add(name,[(i,x,y,z)])
def route(points, width=1):
    for (x,y),(xx,yy) in zip(points,points[1:]):
        assert x==xx or y==yy, 'Use explicit right-angle junctions'
        for a in range(min(x,xx),max(x,xx)+1):
            for b in range(min(y,yy),max(y,yy)+1):
                for dx in range(-width,width+1):
                    for dy in range(-width,width+1):paving[a+dx,b+dy]=0x519+(a+b)%4
def rectangle(name,i,x1,y1,x2,y2,z=0):
    add(name,[(i,x,y,z) for x in range(x1,x2+1) for y in range(y1,y2+1)])
# A single main street, with short spurs to work areas. No ornamental loops.
route([(68,87),(78,87),(78,119),(82,119),(82,134),(87,134),(87,136)])
route([(68,86),(68,87)],0) # exact connection to the house's southern entrance
route([(78,105),(60,105),(60,97)]) # central garden aisle
route([(60,105),(47,105),(47,94),(44,94)]) # stable, north of the old corsair camp
route([(78,89),(96,89),(96,83)]) # workshop yard
route([(78,112),(104,112),(104,100),(112,100)]) # orchard and ore bank
route([(104,100),(90,100),(90,75),(100,75)]) # grove entrance, outside the workshop
route([(68,87),(44,87),(44,52),(54,52),(54,40)],0) # narrow trail to trial clearing
route([(82,130),(92,130),(92,124)]) # kitchen courtyard
route([(87,136),(102,136),(102,140),(98,140)]) # warehouse's open eastern frontage
# Arrival square joins the board, home landing, main street and gate approach.
for x in range(76,86):
    for y in range(125,134):paving[x,y]=0x519+(x+y)%4
# Retain the existing raised notice platform and the original dock surface.
for p in list(paving):
    x,y=p
    if 75<=x<=79 and 123<=y<=126 or 85<=x<=89 and y>=135:paving.pop(p)
for (x,y),i in sorted(paving.items()):one('connected paving',i,x,y)

def nearroute(x,y,r=1):return any((x+a,y+b) in paving for a in range(-r,r+1) for b in range(-r,r+1))
# Small, complete enclosures; the gate aligns with the path into the garden.
for x in range(50,75):
    if not 66<=x<=70:one('garden fence',0x3B4,x,88)
    if not 58<=x<=62:one('garden fence',0x3B4,x,106)
for y in range(89,106):
    if not 103<=y<=105:one('garden fence',0x3B5,50,y)
    if not 103<=y<=105:one('garden fence',0x3B5,74,y)
for x,y,w,h in [(55,90,4,4),(61,92,4,4),(53,98,4,4),(66,95,4,4)]:
    add('cultivated soil',[(0x31F4+(a+b)%4,x+a,y+b,0) for a in range(w) for b in range(h)])
add('garden tool corner',[(0xB49,72,103,0),(0xF39,72,103,5),(0x9AC,72,102,0),(0xE77,73,102,0)])
for x,y in [(51,90),(51,94),(51,99),(72,90),(72,94),(67,103)]:one('garden herbs',0xC85 if x%2 else 0xC8A,x,y)

def shed(name,x1,y1,x2,y2):
    rectangle(name+' deck',0x4A9,x1,y1,x2,y2)
    for x,y in [(x1,y1),(x2,y1),(x1,y2),(x2,y2)]:one(name+' post',0x9,x,y)
    # A simple low timber awning with consistent elevation; no disconnected roof slopes.
    rectangle(name+' awning',0x4A9,x1,y1,x2,y2,20)
    for x in range(x1,x2+1):one(name+' back rail',0x3B4,x,y1)
    for y in range(y1+1,y2):one(name+' side rail',0x3B5,x1,y)

shed('stable',38,91,43,97)
add('stable tack',[(0xE77,39,92,0),(0x100C,40,92,0),(0xF34,41,92,0),(0x14F8,39,96,0)])
shed('workshop yard',92,77,100,82)
add('shipwright supplies',[(0x1BDD,93,78,0),(0x1BDD,94,78,0),(0xE3D,95,78,0),(0xE77,99,78,0),(0xB49,99,80,0),(0xF39,99,80,5)])
shed('cargo store',93,137,100,143)
add('export cargo',[(0xE3D,94,138,0),(0xE3F,94,138,3),(0xE3D,95,138,0),(0xE77,99,138,0),(0xE77,99,139,0),(0x14F8,94,142,0)])
add('net repair bench',[(0xB49,99,142,0),(0xDC8,99,142,5),(0xDBF,98,142,0)])
# Board remains at its existing location, under a supported canopy at the square's edge.
for x,y in [(75,121),(79,121),(75,124),(79,124)]:one('notice shelter post',0x9,x,y)
rectangle('notice shelter awning',0x4A9,75,121,79,124,20)
rectangle('notice shelter back',0x3B4,75,121,79,121)
# Kitchen's existing oven, table and stools remain. Tie them together with one courtyard.
for x in range(89,100):
    for y in range(120,129):
        if (x,y) not in paving:one('kitchen courtyard',0x519+(x+y)%4,x,y)
for x in range(89,100):one('kitchen garden wall',0x3B4,x,119)
for y in range(120,129):one('kitchen garden wall',0x3B5,100,y)
add('kitchen provisions',[(0xE77,98,126,0),(0xE77,98,127,0),(0x9AC,97,127,0)])
add('kitchen supper table',[(0xB90,93,126,0),(0x9D7,93,126,6),(0x99B,93,126,6),(0xB2C,93,127,0)])
add('orchard apiary',[(0x91A,101,116,0),(0x91A,103,116,0),(0xC85,101,117,0),(0xC85,103,117,0)])
# Deliberate coastal belts. Single-piece palms avoid mismatched trunk/crown artwork.
for cx,cy in [(36,72),(36,84),(36,96),(39,124),(48,136),(61,144),(113,140),(128,125),(139,106),(140,89),(133,71),(124,38),(103,32)]:
    for dx,dy,i in [(0,0,0xC96),(3,2,0xC95),(-2,3,0xC99),(1,4,0xCA1),(-1,1,0xCAD)]:
        x,y=cx+dx,cy+dy
        if dry(x,y) and not nearroute(x,y,2):one('coastal planting',i,x,y)
# Low borders frame the approach; sight lines to doors and the board stay open.
for x,y in [(72,91),(72,95),(81,94),(81,99),(81,104),(75,114),(75,117),(86,125),(86,129),(71,127)]:
    if not nearroute(x,y):add('street border',[(0xC83,x,y,0),(0xC8A,x,y+1,0)])
for x,y in [(70,90),(80,99),(80,108),(84,120),(86,133),(101,134),(46,80),(46,58),(102,103)]:
    if not nearroute(x,y,0):one('street lantern',0xB20,x,y)
add('harbor battery',[(0xE6C,73,135,0),(0xE74,74,135,0),(0xE77,73,134,0),(0x14F7,72,135,0)])

deck={(t['x'],t['y']) for g in groups if g['name'].endswith(' deck') for t in g['tiles']}
groups=[g for g in groups if not(g['name']=='R.E.C. connected paving' and (g['tiles'][0]['x'],g['tiles'][0]['y']) in deck)]
endpoints=[(68,86),(60,97),(44,94),(96,83),(112,100),(100,75),(54,40),(92,124),(98,140),(82,128),(87,136)]
out=root/'haven-fixes/playerbots/source/CustomBots/UOOffline/HavenIslandSettlementPlan.cs'
lines=['namespace Server.UOOffline;','','internal static partial class HavenIslandDecoration','{','    internal static readonly Group[] SettlementPlan =','    {']
for g in groups:
    ts=', '.join(f"new(0x{t['id']:X}, {t['x']}, {t['y']}, {t['z']})" for t in g['tiles'])
    lines.append(f'        new("{g["name"]}", false, new Tile[] {{ {ts} }}),')
lines+=['    };','    internal static readonly Point3D[] SettlementDestinations = { '+', '.join(f'new({x}, {y}, 0)' for x,y in endpoints)+' };','}']
out.write_text('\n'.join(lines)+'\n')
(root/'artifacts/island-settlement-plan.json').write_text(json.dumps(groups,indent=2))
print(len(groups),'groups;',sum(len(g['tiles']) for g in groups),'tiles')
