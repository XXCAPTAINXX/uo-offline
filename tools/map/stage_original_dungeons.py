"""Stage selected original dungeon blocks from the user's modern client, preserving Haven terrain elsewhere."""
from pathlib import Path
import argparse, hashlib, json, re, shutil, struct, zlib

MASK=0xffffffff
def rot(x,n): return ((x<<n)|(x>>(32-n)))&MASK
def namehash(s):
    raw=s.encode('ascii'); a=b=c=(0xdeadbeef+len(raw))&MASK
    k=0; left=len(raw)
    while left>12:
        x,y,z=struct.unpack_from('<III',raw,k);a=(a+x)&MASK;b=(b+y)&MASK;c=(c+z)&MASK
        a=((a-c)&MASK)^rot(c,4);c=(c+b)&MASK
        b=((b-a)&MASK)^rot(a,6);a=(a+c)&MASK
        c=((c-b)&MASK)^rot(b,8);b=(b+a)&MASK
        a=((a-c)&MASK)^rot(c,16);c=(c+b)&MASK
        b=((b-a)&MASK)^rot(a,19);a=(a+c)&MASK
        c=((c-b)&MASK)^rot(b,4);b=(b+a)&MASK
        left-=12;k+=12
    tail=raw[k:]
    a=(a+int.from_bytes(tail[:4],'little'))&MASK;b=(b+int.from_bytes(tail[4:8],'little'))&MASK;c=(c+int.from_bytes(tail[8:12],'little'))&MASK
    if left:
        c=((c^b)-rot(b,14))&MASK;a=((a^c)-rot(c,11))&MASK;b=((b^a)-rot(a,25))&MASK
        c=((c^b)-rot(b,16))&MASK;a=((a^c)-rot(c,4))&MASK;b=((b^a)-rot(a,14))&MASK;c=((c^b)-rot(b,24))&MASK
    return (b<<32)|c

def unpack_map(path, entry_stem=None):
    raw=path.read_bytes()
    if struct.unpack_from('<I',raw)[0]!=0x50594d:raise ValueError('Not UOP')
    names={namehash(f'build/{(entry_stem or path.stem.lower())}/{i:08d}.dat'):i for i in range(512)}
    block=struct.unpack_from('<Q',raw,12)[0];entries={}
    while block:
        count,nxt=struct.unpack_from('<IQ',raw,block)
        for i in range(count):
            off,head,comp,size,h,adler,flag=struct.unpack_from('<QIIIQIH',raw,block+12+i*34)
            if not off:continue
            if h not in names:raise ValueError('Unexpected UOP map hash')
            part=raw[off+head:off+head+comp]
            if flag:part=zlib.decompress(part)
            if len(part)!=size:raise ValueError('UOP entry length mismatch')
            entries[names[h]]=part
        block=nxt
    if sorted(entries)!=list(range(len(entries))):raise ValueError('Missing map chunk')
    return b''.join(entries[i] for i in range(len(entries)))

def blocks(rects,height=512):
    return {x*height+y for ox,oy,w,h in rects for x in range(ox//8,(ox+w)//8) for y in range(oy//8,(oy+h)//8)}
def digest(p):return hashlib.file_digest(p.open('rb'),'sha256').hexdigest()

def stage(source,client,output):
    source,client,output=map(lambda p:Path(p).resolve(),(source,client,output))
    if output.exists():raise ValueError('Output must be new')
    output.mkdir(parents=True)
    # Keep all existing Haven and other-facet geometry unchanged outside the selected blocks.
    for p in source.iterdir():
        if p.is_file() and (p.suffix.lower() in ('.mul','.idx') or p.name.lower().startswith('cliloc.')):shutil.copy2(p,output/p.name)
    selections={1:[(6208,2304,320,512),(1472,1464,16,24)],5:[(0,1280,1024,1280)]}
    used_items=set();used_land=set();manifest={'source':str(source),'client':str(client),'regions':selections,'files':[]}
    for facet,rects in selections.items():
        chosen=blocks(rects)
        modern=unpack_map(client/f'map{facet}LegacyMUL.uop')
        if facet==5 and not (output/'map5.mul').exists():shutil.copy2(source/'map5.mul',output/'map5.mul')
        for suffix in ('', 'x'):
            name=f'map{facet}{suffix}.mul';base=output/name
            if not base.exists():shutil.copy2(output/f'map{facet}.mul',base)
            raw=bytearray(base.read_bytes())
            # Modern legacy UOP containers can carry one trailing padding block.
            if len(modern) not in (len(raw),len(raw)+196):raise ValueError(f'Map dimensions differ: {name}')
            for b in chosen:
                raw[b*196:(b+1)*196]=modern[b*196:(b+1)*196]
                used_land.update(struct.unpack_from('<H',modern,b*196+4+k*3)[0] for k in range(64))
            base.write_bytes(raw)
        modern_idx=(client/f'staidx{facet}.mul').read_bytes();modern_sta=(client/f'statics{facet}.mul').read_bytes()
        for suffix in ('','x'):
            idxname=f'staidx{facet}{suffix}.mul';staname=f'statics{facet}{suffix}.mul'
            if not (output/idxname).exists():shutil.copy2(source/f'staidx{facet}.mul',output/idxname)
            if not (output/staname).exists():shutil.copy2(source/f'statics{facet}.mul',output/staname)
            idx=bytearray((output/idxname).read_bytes());sta=bytearray((output/staname).read_bytes())
            for b in sorted(chosen):
                off,length,extra=struct.unpack_from('<iii',modern_idx,b*12)
                if off<0 or length<=0:struct.pack_into('<iii',idx,b*12,-1,-1,-1);continue
                part=modern_sta[off:off+length]
                if len(part)!=length or length%7:raise ValueError('Invalid statics block')
                used_items.update(t[0] for t in struct.iter_unpack('<HBBbH',part))
                struct.pack_into('<iii',idx,b*12,len(sta),length,extra);sta.extend(part)
            (output/idxname).write_bytes(idx);(output/staname).write_bytes(sta)
        for diff in ('mapdifl','stadifl'):
            p=source/f'{diff}{facet}.mul'
            if p.exists() and chosen.intersection(x[0] for x in struct.iter_unpack('<I',p.read_bytes())):raise ValueError('Diff overrides imported dungeon blocks')
    # Room scenery uses the documented ServUO layout; graphics remain local client assets.
    scenery=Path(__file__).resolve().parents[2]/'playerbots/source/CustomBots/UOOffline/HavenShadowScenery.cs'
    components=re.findall(r'\{\s*(\d+),\s*(-?\d+),\s*(-?\d+),\s*(-?\d+)\s*\}',scenery.read_text(encoding='utf-8'))
    if not components:raise ValueError('Missing checked-in Shadowguard component layouts')
    used_items.update(int(component[0]) for component in components)
    old=bytearray((source/'tiledata.mul').read_bytes());modern=(client/'tiledata.mul').read_bytes()
    land_size=512*(4+32*30);item_block=4+32*41
    if len(old)<land_size or len(modern)<land_size:raise ValueError('64-bit tiledata required')
    original_size=len(old)
    if len(old)<len(modern):old.extend(modern[len(old):])
    changed=[]
    for kind,ids,size,base in [('land',used_land,30,0),('item',used_items,41,land_size)]:
        for i in ids:
            off=base+(i//32)*(4+32*size)+4+(i%32)*size
            if kind=='land' and i==0:off=0 # 64-bit land record zero precedes the first header.
            if old[off:off+size]!=modern[off:off+size]:changed.append([kind,i])
            old[off:off+size]=modern[off:off+size]
    (output/'tiledata.mul').write_bytes(old)
    manifest['tiledata_updated_records']=changed;manifest['tiledata_original_bytes']=original_size
    for p in sorted(output.glob('*.mul')):manifest['files'].append({'name':p.name,'sha256':digest(p),'bytes':p.stat().st_size})
    (output/'haven-original-dungeons-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(json.dumps({'output':str(output),'regions':selections,'tiledata_records_changed':len(changed),'files':len(manifest['files'])},indent=2))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('source');p.add_argument('client');p.add_argument('output');a=p.parse_args();stage(a.source,a.client,a.output)
