"""Build islands in a NEW isolated client-data directory; never modify source files."""
import argparse
import hashlib
import json
import math
import shutil
import struct
from pathlib import Path

def coastline(key, x, y):
    """Signed distance in tiles: negative is land, positive is navigable ocean."""
    dx, dy = x-88, y-88
    angle = math.atan2(dy, dx)
    phase = 0.7 if key == 'commons' else 0.0
    radius = min(64, 62 + 3*math.sin(3*angle+phase) + 2*math.cos(5*angle-phase))
    shore = math.hypot(dx, dy) - radius
    if key == 'pirate_estate':
        # A southern inlet alongside the existing boarding pier. Its eastern
        # headland shelters the basin; the southern channel remains open.
        basin = (1-math.hypot((x-116)/21, (y-143)/17))*17
        channel = min(x-99, 128-x, y-143)
        shore = max(shore, basin, channel)
    return shore

def stage(source, plan_file, output):
    source, output = Path(source).resolve(), Path(output).resolve()
    if output.exists() or source == output or source in output.parents:
        raise ValueError('Use a new output directory outside the source client data')
    plan = json.loads(Path(plan_file).read_text(encoding='utf-8'))
    raw = bytearray((source/'map1.mul').read_bytes())
    base_hash = hashlib.sha256(raw).hexdigest()
    alternate = source/'map1x.mul'
    alternate_raw = bytearray(alternate.read_bytes()) if alternate.exists() else None
    alternate_hash = hashlib.sha256(alternate_raw).hexdigest() if alternate_raw is not None else None
    static_index = (source/'staidx1.mul').read_bytes()
    diffs = set()
    for name in ('mapdifl1.mul', 'stadifl1.mul'):
        path = source/name
        if path.exists(): diffs.update(v[0] for v in struct.iter_unpack('<I', path.read_bytes()))
    changed = 0
    # Preflight every changed tile before any files are created.
    for key in ('commons', 'pirate_estate'):
        ox, oy = plan[key]['origin']
        for x in range(24,153):
            for y in range(24,153):
                distance = coastline(key, x, y)
                if distance >= 0: continue
                if key == 'pirate_estate' and 95 <= x < 123 and 145 <= y < 173: continue
                wx, wy = ox+x, oy+y
                block = (wx//8)*512+wy//8
                if block in diffs or struct.unpack_from('<iii', static_index, block*12)[1] > 0:
                    raise ValueError(f'Existing static or diff block at {wx},{wy}; survey must be repeated')
                off = block*196+4+((wy%8)*8+wx%8)*3
                tile,z = struct.unpack_from('<Hb',raw,off)
                if tile not in {0xA8,0xA9,0xAA,0xAB,0x136,0x137} or z != -5:
                    raise ValueError(f'Non-ocean tile at {wx},{wy}; survey must be repeated')
                # Broad grass interior and a gently rising sand beach. No cliffs at house plot.
                new_tile = 0x16 if distance > -9 else 0x3
                new_z = min(0, -5 + math.ceil(-distance))
                if alternate_raw is not None:
                    alt_tile, alt_z = struct.unpack_from('<Hb', alternate_raw, off)
                    if alt_tile not in {0xA8,0xA9,0xAA,0xAB,0x136,0x137} or alt_z != -5:
                        raise ValueError(f'Alternate map has non-ocean at {wx},{wy}')
                    struct.pack_into('<Hb',alternate_raw,off,new_tile,new_z)
                struct.pack_into('<Hb',raw,off,new_tile,new_z); changed += 1
    output.mkdir(parents=True)
    # Test runtime reads only these files; all are independent copies.
    for item in source.iterdir():
        if item.is_file() and (item.name.startswith(('map','statics','staidx','stadif','tiledata','multi','radarcol','cliloc'))):
            shutil.copy2(item, output/item.name)
    (output/'map1.mul').write_bytes(raw)
    # Preserve unrelated differences in the alternate map; patch only the island tiles.
    if alternate_raw is not None: (output/'map1x.mul').write_bytes(alternate_raw)
    manifest = {'source_map_sha256':base_hash,'staged_map_sha256':hashlib.sha256(raw).hexdigest(),
                'source_alternate_sha256':alternate_hash,
                'tiles_changed':changed,'source':str(source),'output':str(output),
                'status':'isolated cove terrain prototype, not deployed','coastline':'irregular shore with southern sheltered inlet','plan':plan}
    (output/'haven-islands-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in manifest.items() if k != 'plan'},indent=2))

if __name__ == '__main__':
    p=argparse.ArgumentParser();p.add_argument('source');p.add_argument('plan');p.add_argument('output');a=p.parse_args()
    stage(a.source,a.plan,a.output)
