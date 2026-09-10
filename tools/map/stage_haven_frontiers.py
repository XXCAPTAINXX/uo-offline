"""Stage three new island footprints from an existing Haven map; never edit source data."""
import argparse
import hashlib
import json
import math
import shutil
import struct
from pathlib import Path

WATER = {0xA8, 0xA9, 0xAA, 0xAB, 0x136, 0x137}

def stage(source, output):
    source, output = Path(source).resolve(), Path(output).resolve()
    if output.exists() or source == output or source in output.parents:
        raise ValueError('Output must be new and outside source data')
    maps = {name: bytearray((source/name).read_bytes()) for name in ('map1.mul', 'map1x.mul')}
    index = (source/'staidx1.mul').read_bytes()
    diffs = set()
    for name in ('mapdifl1.mul', 'stadifl1.mul'):
        if (source/name).exists():
            diffs.update(v[0] for v in struct.iter_unpack('<I', (source/name).read_bytes()))
    def clear(x, y):
        for bx in range(x//8, (x+175)//8+1):
            for by in range(y//8, (y+175)//8+1):
                block = bx*512+by
                if block in diffs or struct.unpack_from('<iii', index, block*12)[1] > 0:
                    return False
                for raw in maps.values():
                    for k in range(64):
                        tile, z = struct.unpack_from('<Hb', raw, block*196+4+k*3)
                        if tile not in WATER or z != -5:
                            return False
        return True
    sites = []
    for y in range(3104, 3680, 32):
        for x in range(3904, 4864, 32):
            if clear(x,y) and all(abs(x-a)>208 or abs(y-b)>208 for a,b in sites):
                sites.append((x,y))
                if len(sites)==3:
                    break
        if len(sites)==3:
            break
    if len(sites)!=3:
        raise ValueError('Three empty ocean sites not found; no files changed')
    before = {name:hashlib.sha256(raw).hexdigest() for name,raw in maps.items()}
    changed = 0
    for number,(ox,oy) in enumerate(sites):
        for x in range(18,159):
            for y in range(18,159):
                angle = math.atan2(y-88,x-88)
                radius = math.hypot((x-88)/69,(y-88)/65)
                edge = 1 + .035*math.sin(5*angle) + .02*math.cos(9*angle)
                if radius >= edge:
                    continue
                tile = 0x16 if radius > edge-.16 else 0x3
                # The tortoise island has a basalt crescent around a green nesting valley.
                if number==2 and .40<radius<.65 and x<88:
                    tile = 0x22
                z = min(0, -5+math.ceil((edge-radius)*50))
                wx,wy = ox+x,oy+y
                offset=((wx//8)*512+wy//8)*196+4+((wy%8)*8+wx%8)*3
                for raw in maps.values():
                    struct.pack_into('<Hb',raw,offset,tile,z)
                changed+=1
    output.mkdir(parents=True)
    for item in source.iterdir():
        if item.is_file() and item.name.startswith(('map','statics','staidx','stadif','tiledata','multi','radarcol','cliloc')):
            shutil.copy2(item,output/item.name)
    for name,raw in maps.items():
        (output/name).write_bytes(raw)
    manifest={'source':str(source),'source_hashes':before,'result_hashes':{n:hashlib.sha256(r).hexdigest() for n,r in maps.items()},
              'tiles_changed':changed,'shadowguard':sites[0],'blackthorn':sites[1],'chelonia':sites[2],
              'status':'staged only; dynamic-world overlap and server tests required'}
    (output/'haven-frontiers-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print(json.dumps(manifest,indent=2))

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('source');parser.add_argument('output');args=parser.parse_args()
    stage(args.source,args.output)
