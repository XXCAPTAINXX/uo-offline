"""Stage new 19..31-tile custom foundations without changing source client files."""
import argparse, hashlib, json, struct, zlib
from pathlib import Path

FIRST=0x1800
def tiles(w,h):
    xmin,ymin=-((w-1)//2),-((h-1)//2); xmax,ymax=xmin+w-1,ymin+h-1
    result=[(1,0,0,0,0),(101,xmax,ymax,0,1),(102,xmin,ymin,0,1),(100,xmin,ymax,0,1),(99,xmax,ymin,0,1)]
    for x in range(xmin+1,xmax): result.extend([(99,x,ymin,0,1),(99,x,ymax,0,1)])
    for y in range(ymin+1,ymax): result.extend([(100,xmin,y,0,1),(100,xmax,y,0,1)])
    result.extend((0x31F4,x,y,7,1) for x in range(xmin+1,xmax+1) for y in range(ymin+1,ymax+1))
    return result

def uohash(name):
    # Jenkins lookup3/hashlittle2, the UOP filename hash used by the client and ModernUO.
    data=name.encode('ascii'); mask=0xffffffff
    def rot(v,k):return ((v<<k)|(v>>(32-k)))&mask
    a=b=c=(0xdeadbeef+len(data))&mask; k=0; left=len(data)
    while left>12:
        aa,bb,cc=struct.unpack_from('<III',data,k);a=(a+aa)&mask;b=(b+bb)&mask;c=(c+cc)&mask
        a=((a-c)&mask)^rot(c,4);c=(c+b)&mask
        b=((b-a)&mask)^rot(a,6);a=(a+c)&mask
        c=((c-b)&mask)^rot(b,8);b=(b+a)&mask
        a=((a-c)&mask)^rot(c,16);c=(c+b)&mask
        b=((b-a)&mask)^rot(a,19);a=(a+c)&mask
        c=((c-b)&mask)^rot(b,4);b=(b+a)&mask
        k+=12;left-=12
    if left:
        tail=data[k:]+b'\0'*(12-left);aa,bb,cc=struct.unpack('<III',tail)
        a=(a+aa)&mask;b=(b+bb)&mask;c=(c+cc)&mask
        c=((c^b)-rot(b,14))&mask;a=((a^c)-rot(c,11))&mask;b=((b^a)-rot(a,25))&mask
        c=((c^b)-rot(b,16))&mask;a=((a^c)-rot(c,4))&mask;b=((b^a)-rot(a,14))&mask;c=((c^b)-rot(b,24))&mask
    return (b<<32)|c

def uop_entries(raw):
    if raw[:4]!=b'MYP\0':raise ValueError('Not a UOP archive')
    block=struct.unpack_from('<Q',raw,12)[0]; seen=set(); records={};last=0
    while block:
        if block in seen:raise ValueError('Cyclic UOP chain')
        seen.add(block);count,nxt=struct.unpack_from('<IQ',raw,block);last=block
        for i in range(count):
            pos=block+12+i*34;off,head,comp,size,key,crc,zipflag=struct.unpack_from('<QIIIQIH',raw,pos)
            if off and comp and size:records[key]=(off,head,comp,size,zipflag)
        block=nxt
    return records,last

def stage(source,output):
    source,output=Path(source).resolve(),Path(output).resolve()
    if output.exists() or output==source or source in output.parents:raise ValueError('Use a new separate output folder')
    originals={n:(source/n).read_bytes() for n in ('multi.idx','multi.mul')}
    if (source/'MultiCollection.uop').exists(): originals['MultiCollection.uop']=(source/'MultiCollection.uop').read_bytes()
    idx=bytearray(originals['multi.idx']);mul=bytearray(originals['multi.mul']);defs=[]
    for w in range(19,32):
        for h in range(19,32):
            ident=FIRST+(w-19)*13+h-19
            if ident*12+12>len(idx):raise ValueError('Missing reserved index range')
            oldoff,oldsize,_=struct.unpack_from('<iii',idx,ident*12)
            if oldoff>=0 and oldsize>0:raise ValueError(f'Multi {ident:X} already occupied; refusing to replace it')
            parts=tiles(w,h);raw=b''.join(struct.pack('<HhhhQ',*t) for t in parts)
            struct.pack_into('<iii',idx,ident*12,len(mul),len(raw),0);mul.extend(raw);defs.append((ident,w,h,parts))
    files={'multi.idx':bytes(idx),'multi.mul':bytes(mul)}
    if 'MultiCollection.uop' in originals:
        raw=bytearray(originals['MultiCollection.uop']);records,last=uop_entries(raw)
        assert uohash('build/multicollection/000064.bin') in records,'UOP hash verification failed'
        block=len(raw);table=bytearray(struct.pack('<IQ',len(defs),0));payloads=bytearray();offset=block+12+len(defs)*34
        for ident,w,h,parts in defs:
            key=uohash(f'build/multicollection/{ident:06}.bin')
            if key in records:raise ValueError(f'UOP multi {ident:X} already occupied')
            payload=struct.pack('<II',ident,len(parts))+b''.join(struct.pack('<HhhhHI',t[0],t[1],t[2],t[3],0 if t[4] else 1,0) for t in parts)
            compressed=zlib.compress(payload)
            table.extend(struct.pack('<QIIIQIH',offset+len(payloads),0,len(compressed),len(payload),key,zlib.adler32(compressed),1));payloads.extend(compressed)
        struct.pack_into('<Q',raw,last+4,block);struct.pack_into('<I',raw,24,struct.unpack_from('<I',raw,24)[0]+len(defs))
        raw.extend(table);raw.extend(payloads);files['MultiCollection.uop']=bytes(raw)
        check,_=uop_entries(raw)
        assert len(check)==len(records)+len(defs)
        for key,entry in records.items(): assert check[key]==entry
    output.mkdir(parents=True)
    manifest={'source':str(source),'sizes':'19..31 in both dimensions; 169 plots','files':[]}
    for name,raw in files.items():
        (output/name).write_bytes(raw)
        manifest['files'].append({'name':name,'before':hashlib.sha256(originals[name]).hexdigest(),'after':hashlib.sha256(raw).hexdigest()})
    (output/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(json.dumps(manifest,indent=2))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('source');p.add_argument('output');a=p.parse_args();stage(a.source,a.output)
