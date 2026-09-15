"""Refine a verified circular island prototype into irregular shores and a cove."""
import hashlib,json,math,shutil,struct,sys
from pathlib import Path
from stage_haven_islands import coastline

def refine(source,output):
 source=Path(source).resolve();output=Path(output).resolve()
 if output.exists() or source==output or source in output.parents:raise ValueError('Use a new output directory')
 manifest=json.loads((source/'haven-islands-manifest.json').read_text())
 raw=(source/'map1.mul').read_bytes()
 if hashlib.sha256(raw).hexdigest()!=manifest['staged_map_sha256']:raise ValueError('Prototype hash changed; repeat survey')
 plan=manifest['plan'];maps={name:bytearray((source/name).read_bytes()) for name in ('map1.mul','map1x.mul') if (source/name).exists()}
 alternate_source_hash=hashlib.sha256(maps["map1x.mul"]).hexdigest() if "map1x.mul" in maps else None
 changed=0
 for key in ('commons','pirate_estate'):
  ox,oy=plan[key]['origin']
  for x in range(24,153):
   for y in range(24,153):
    r=math.hypot((x-88)/64,(y-88)/64)
    if r>=1 or key=='pirate_estate' and 95<=x<123 and 145<=y<173:continue
    wx,wy=ox+x,oy+y;off=((wx//8)*512+wy//8)*196+4+((wy%8)*8+wx%8)*3
    expected=(0x16 if r>.80 else 3,min(0,-5+math.ceil((1-r)*50)))
    d=coastline(key,x,y)
    value=(0xA8,-5) if d>=0 else (0x16 if d>-9 else 3,min(0,-5+math.ceil(-d)))
    for name,data in maps.items():
     if struct.unpack_from('<Hb',data,off)!=expected:raise ValueError(f'Unexpected prototype tile {name} {wx},{wy}')
     struct.pack_into('<Hb',data,off,*value)
    if value!=expected:changed+=1
 output.mkdir()
 for item in source.iterdir():
  if item.is_file():shutil.copy2(item,output/item.name)
 for name,data in maps.items():(output/name).write_bytes(data)
 manifest.update(source=str(source),output=str(output),source_map_sha256=hashlib.sha256(raw).hexdigest(),staged_map_sha256=hashlib.sha256(maps['map1.mul']).hexdigest(),source_alternate_sha256=alternate_source_hash,staged_alternate_sha256=hashlib.sha256(maps['map1x.mul']).hexdigest() if 'map1x.mul' in maps else None,tiles_changed=changed,status='isolated cove refinement, not deployed',coastline='irregular shore, sheltered southeast basin and southern boat channel')
 (output/'haven-islands-manifest.json').write_text(json.dumps(manifest,indent=2))
 print(json.dumps({k:v for k,v in manifest.items() if k!='plan'},indent=2))
if __name__=='__main__':refine(*sys.argv[1:])
