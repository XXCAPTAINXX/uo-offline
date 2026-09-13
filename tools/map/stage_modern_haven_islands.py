"""Apply verified island land tiles to modern Trammel, preserving all other geometry."""
import hashlib,json,math,shutil,struct,sys
from pathlib import Path
from stage_original_dungeons import unpack_map

def stage(client,cove,output):
 client=Path(client).resolve();cove=Path(cove).resolve();output=Path(output).resolve()
 if output.exists() or client in output.parents or cove in output.parents:raise ValueError('Use a new independent output directory')
 manifest=json.loads((cove/'haven-islands-manifest.json').read_text());terrain=(cove/'map1.mul').read_bytes()
 if hashlib.sha256(terrain).hexdigest()!=manifest['staged_map_sha256']:raise ValueError('Cove hash mismatch')
 size=896*512*196
 maps={}
 for name,uop in [('map1.mul','map1LegacyMUL.uop'),('map1x.mul','map1xLegacyMUL.uop')]:
  decoded=unpack_map(client/uop,'map1legacymul')
  if len(decoded) not in (size,size+196):raise ValueError(f'Unexpected map length {uop}: {len(decoded)}')
  maps[name]=bytearray(decoded[:size])
 hashes={n:hashlib.sha256(v).hexdigest() for n,v in maps.items()}
 indexes=[(client/n).read_bytes() for n in ('staidx1.mul','staidx1x.mul')];diffs=set()
 for n in ('mapdifl1.mul','stadifl1.mul'):
  p=client/n
  if p.exists():diffs.update(x[0] for x in struct.iter_unpack('<I',p.read_bytes()))
 changed=0
 for key in ('commons','pirate_estate'):
  ox,oy=manifest['plan'][key]['origin']
  for x in range(24,153):
   for y in range(24,153):
    if math.hypot(x-88,y-88)>=64:continue
    wx,wy=ox+x,oy+y;block=(wx//8)*512+wy//8;off=block*196+4+((wy%8)*8+wx%8)*3
    desired=struct.unpack_from('<Hb',terrain,off)
    if desired[0] not in (3,0x16):continue
    if block in diffs or any(struct.unpack_from('<iii',idx,block*12)[1]>0 for idx in indexes):raise ValueError(f'Existing modern structures/diffs at {wx},{wy}')
    for name,raw in maps.items():
     tile,z=struct.unpack_from('<Hb',raw,off)
     if tile not in (0xA8,0xA9,0xAA,0xAB,0x136,0x137) or z!=-5:raise ValueError(f'Modern land collision {name} {wx},{wy}')
     struct.pack_into('<Hb',raw,off,*desired)
    changed+=1
 output.mkdir()
 for p in client.iterdir():
  if p.is_file() and p.name.lower().startswith(('map','static','staidx','stadif','tiledata','multi','radarcol','cliloc')):
   if p.name in ('map1LegacyMUL.uop','map1xLegacyMUL.uop'):continue
   shutil.copy2(p,output/p.name)
 for name,raw in maps.items():(output/name).write_bytes(raw)
 for name in ("mapdif1.mul","mapdifl1.mul","stadif1.mul","stadifl1.mul","stadifi1.mul"):
  if not (output/name).exists():(output/name).write_bytes(b"")
 result=dict(manifest,source=str(client),output=str(output),source_map_sha256=hashes['map1.mul'],staged_map_sha256=hashlib.sha256(maps['map1.mul']).hexdigest(),source_alternate_sha256=hashes['map1x.mul'],staged_alternate_sha256=hashlib.sha256(maps['map1x.mul']).hexdigest(),tiles_changed=changed,status='modern terrain with verified island overlay; not deployed')
 (output/'haven-islands-manifest.json').write_text(json.dumps(result,indent=2));print(json.dumps({k:v for k,v in result.items() if k!='plan'},indent=2))
if __name__=='__main__':stage(*sys.argv[1:])
