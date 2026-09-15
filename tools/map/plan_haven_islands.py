"""Read-only ocean survey and reviewable layout. Never edits UO client data."""
import argparse
import json
import mmap
import struct
from pathlib import Path

WATER = set(range(0xA8, 0xAC)) | set(range(0x0136, 0x0138))

def survey(data, out):
    data, out = Path(data).resolve(), Path(out).resolve()
    if out == data or data in out.parents:
        raise ValueError("Output must be outside client data")
    out.mkdir(parents=True, exist_ok=True)
    diffs = set()
    for name in ("mapdifl1.mul", "stadifl1.mul"):
        path = data / name
        if path.exists():
            diffs.update(v[0] for v in struct.iter_unpack('<I', path.read_bytes()))
    with (data/'map1.mul').open('rb') as mf, (data/'staidx1.mul').open('rb') as sf:
        land = mmap.mmap(mf.fileno(), 0, access=mmap.ACCESS_READ)
        statics = mmap.mmap(sf.fileno(), 0, access=mmap.ACCESS_READ)
        def empty(x, y, width, height):
            for bx in range(x//8, (x+width-1)//8+1):
                for by in range(y//8, (y+height-1)//8+1):
                    block = bx*512+by
                    if block in diffs: return False
                    _, length, _ = struct.unpack_from('<iii', statics, block*12)
                    if length > 0: return False
                    for k in range(64):
                        tile, z = struct.unpack_from('<Hb', land, block*196+4+k*3)
                        if tile not in WATER or z != -5: return False
            return True
        sites = []
        for y in range(2800, 3700, 32):
            for x in range(3904, 4864, 32):
                if empty(x, y, 176, 176) and all(abs(x-s[0]) > 208 or abs(y-s[1]) > 208 for s in sites):
                    sites.append((x,y))
                    if len(sites) == 2: break
            if len(sites) == 2: break
        if len(sites) != 2: raise RuntimeError('No two clear ocean sites found')
        land.close(); statics.close()
    plan = {
        'status': 'surveyed proposal; world objects and boat routes require staged server checks',
        'facet': 'Trammel', 'survey_extent': [176,176], 'water_margin': 24,
        'commons': {'origin': sites[0], 'hall_offset': [64,58], 'hall_size':[33,41]},
        'pirate_estate': {'origin':sites[1], 'house_plot_offset':[48,48], 'house_plot_size':[40,40],
            'grove_offset':[105,50], 'mine_offset':[105,90], 'spawn_offset':[50,110],
            'dock_offset':[88,143], 'boat_water_reserved':[95,145,28,28]},
        'notes': ['Do not overwrite live terrain. Both client and server must use the same staged terrain.',
                  'No existing land, static structures, or land/static diff blocks overlap either surveyed footprint.',
                  'A private estate needs an owner-bound access gate and housing permissions; public mall remains separate.']}
    (out/'island-sites.json').write_text(json.dumps(plan, indent=2), encoding='utf-8')
    panels = []
    for i,(x,y) in enumerate(sites):
        px = 25+i*590
        shapes = f'<rect x="{px}" y="80" width="560" height="560" rx="18" fill="#174d62"/><path d="M{px+90} 220 Q{px+135} 110 {px+290} 125 Q{px+455} 120 {px+490} 300 Q{px+510} 485 {px+350} 540 Q{px+100} 570 {px+70} 400 Z" fill="#d3bc83" stroke="#76bcb1" stroke-width="14"/>'
        shapes += f'<text x="{px+280}" y="54" text-anchor="middle" class="title">{["Haven Commons","Corsair’s Rest"][i]}</text>'
        if i == 0:
            shapes += f'<rect x="{px+165}" y="220" width="210" height="245" rx="5" fill="#e5ddc9" stroke="#615b52" stroke-width="4"/><text x="{px+270}" y="275" text-anchor="middle">Merchant hall</text><text x="{px+270}" y="312" text-anchor="middle">Library · workshops</text><text x="{px+270}" y="349" text-anchor="middle">Training · services</text><text x="{px+270}" y="410" text-anchor="middle">Bank travel arrival</text>'
        else:
            shapes += f'<rect x="{px+130}" y="205" width="150" height="150" fill="#96ac78" stroke="#faf0ca" stroke-dasharray="8 5" stroke-width="3"/><text x="{px+205}" y="267" text-anchor="middle">40 × 40</text><text x="{px+205}" y="299" text-anchor="middle">house plot</text><circle cx="{px+365}" cy="235" r="45" fill="#486b42"/><text x="{px+365}" y="240" text-anchor="middle" fill="white">Grove</text><path d="M{px+310} 365 L{px+365} 290 L{px+425} 365 Z" fill="#7d8178"/><text x="{px+365}" y="397" text-anchor="middle">Ore outcrop</text><text x="{px+200}" y="445" text-anchor="middle">Pirate mini-spawn</text>'
        shapes += f'<rect x="{px+288}" y="515" width="24" height="90" fill="#9f724b"/><text x="{px+365}" y="596">Boat dock</text><text x="{px+280}" y="676" text-anchor="middle">Survey origin {x}, {y} · Trammel</text>'
        panels.append(shapes)
    svg = '<svg xmlns="http://www.w3.org/2000/svg" width="1200" height="740" viewBox="0 0 1200 740"><style>text{font:18px sans-serif;fill:#24373e}.title{font-size:28px;font-weight:700}</style><rect width="1200" height="740" fill="#f4efdf"/>' + ''.join(panels) + '<text x="600" y="718" text-anchor="middle">Layout proposal — terrain, house placement and sailing tests precede installation</text></svg>'
    (out/'haven-islands-layout.svg').write_text(svg, encoding='utf-8')
    print(json.dumps(plan, indent=2))

if __name__ == '__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('data'); parser.add_argument('output')
    args=parser.parse_args(); survey(args.data,args.output)
