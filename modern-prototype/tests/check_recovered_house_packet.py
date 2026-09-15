"""Check that the native custom-house packet preserves every exported component."""
import csv,struct,zlib,sys
from pathlib import Path
root=Path(sys.argv[1]);tiles=[{k:int(r[k]) for k in ('id','x','y','z')} for r in csv.DictReader((root/'recovered-house-tiles.csv').open()) if r['kind']=='component']
xmin=min(t['x'] for t in tiles);xmax=max(t['x'] for t in tiles);ymin=min(t['y'] for t in tiles);ymax=max(t['y'] for t in tiles);height=ymax-ymin+1
raw=(root/'recovered-house-packet.bin').read_bytes();assert raw[0]==0xd8 and int.from_bytes(raw[1:3],'big')==len(raw)
at=18;actual=set()
for _ in range(raw[17]):
 plane,lo,clo,hi=raw[at:at+4];at+=4;size=lo|((hi&0xf0)<<4);compressed=clo|((hi&15)<<8);data=zlib.decompress(raw[at:at+compressed]);at+=compressed;assert len(data)==size
 if plane<0x20:
  for off in range(0,len(data),5):
   ident,x,y,z=struct.unpack_from('>Hbbb',data,off);actual.add((ident,x,y,z))
 else:
  p=plane&15;span=height if p==0 else height-2 if p<5 else height-1
  for index in range(len(data)//2):
   ident=int.from_bytes(data[index*2:index*2+2],'big')
   if ident:
    x,y=divmod(index,span);x+=xmin+(0<p<5);y+=ymin+(0<p<5);z=0 if p==0 else 7+20*((p-1)%4);actual.add((ident,x,y,z))
expected={(t['id'],t['x'],t['y'],t['z']) for t in tiles}
assert at==len(raw)
assert expected==actual, f'Missing {len(expected-actual)}: {list(expected-actual)[:12]}; extra {len(actual-expected)}'
print(f'PASS: {len(expected)} unique components survive native packet encoding, compression and decoding.')
