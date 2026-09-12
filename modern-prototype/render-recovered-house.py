"""Render exported actual house tiles using the user's local modern UOP art."""
import argparse,csv,runpy,struct,zlib
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont

p=argparse.ArgumentParser();p.add_argument('manifest');p.add_argument('client');p.add_argument('output');a=p.parse_args()
helpers=runpy.run_path(str(Path(__file__).resolve().parents[1]/'tools/stage_house_plots.py'))
raw=(Path(a.client)/'artLegacyMUL.uop').read_bytes();records,_=helpers['uop_entries'](raw);cache={}
def art(i):
    if i in cache:return cache[i]
    key=helpers['uohash'](f'build/artlegacymul/{i+0x4000:08}.tga')
    if key not in records:raise ValueError(f'Missing modern art {i:X}')
    off,head,comp,size,flag=records[key];data=raw[off+head:off+head+comp]
    if flag:data=zlib.decompress(data)
    w,h=struct.unpack_from('<HH',data,4)
    if not(0<w<1500 and 0<h<1500):raise ValueError(f'Invalid dimensions {i:X}: {w},{h}')
    im=Image.new('RGBA',(w,h));px=im.load();start=8+h*2
    for y in range(h):
        at=start+struct.unpack_from('<H',data,8+y*2)[0]*2;x=0
        while True:
            skip,length=struct.unpack_from('<HH',data,at);at+=4
            if skip==length==0:break
            x+=skip
            for n in range(length):
                c=struct.unpack_from('<H',data,at)[0];at+=2
                if c and x<w:px[x,y]=(((c>>10)&31)*255//31,((c>>5)&31)*255//31,(c&31)*255//31,255)
                x+=1
    cache[i]=im;return im
tiles=[]
for row in csv.DictReader(Path(a.manifest).open()):tiles.append(dict(kind=row['kind'],**{k:int(row[k]) for k in ('id','x','y','z')}))
out=Path(a.output);out.mkdir(parents=True,exist_ok=True)
for mode in ('exterior','ground','upper'):
    im=Image.new('RGBA',(1850,1900),'#344b39')
    selected=[]
    for t in tiles:
        if t['id'] in (0,1):continue
        if mode=='ground' and t['z']>=27:continue
        if mode=='upper' and t['z']>=47:continue
        front=(t['kind']=='component' and t['id'] in (6,7,8,9,14,15) and (t['y'] in (1,2) or t['x'] in (2,8)))
        if mode!='exterior' and front:continue
        selected.append(t)
    for t in sorted(selected,key=lambda t:(t['x']+t['y'],t['x'],t['z'])):
        sprite=art(t['id']);x=925+(t['x']-t['y'])*22-sprite.width//2;y=1050+(t['x']+t['y'])*22-t['z']*4-sprite.height+44
        im.alpha_composite(sprite,(x,y))
    d=ImageDraw.Draw(im);f=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',24)
    d.text((25,22),'Recovered courtyard compound - '+mode+' | actual modern tile art; '+('full exterior' if mode=='exterior' else 'upper floors hidden'),font=f,fill='white')
    im.convert('RGB').save(out/(mode+'.jpg'),quality=93)
print('Rendered exterior and two cutaways from exported house tiles.')
