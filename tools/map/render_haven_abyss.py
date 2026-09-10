"""Read-only top-down Abyss survey from the server's actual MUL files."""
import argparse,json,struct
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
def render(data,sites,output):
    data=Path(data); sites=json.loads(Path(sites).read_text())
    raw=(data/'map5.mul').read_bytes(); idx=(data/'staidx5.mul').read_bytes(); statics=(data/'statics5.mul').read_bytes(); colors=(data/'radarcol.mul').read_bytes()
    x0,y0,w,h=320,80,760,890
    img=Image.new('RGB',(w,h)); height=[-129]*(w*h)
    def color(tile):
        if tile*2+2>len(colors):return (90,90,90)
        c=struct.unpack_from('<H',colors,tile*2)[0]
        return (((c>>10)&31)*255//31,((c>>5)&31)*255//31,(c&31)*255//31)
    for bx in range(x0//8,(x0+w+7)//8):
        for by in range(y0//8,(y0+h+7)//8):
            block=bx*512+by; off=block*196+4
            for sy in range(8):
                for sx in range(8):
                    x,y=bx*8+sx-x0,by*8+sy-y0
                    if not (0<=x<w and 0<=y<h):continue
                    tile,z=struct.unpack_from('<Hb',raw,off+(sy*8+sx)*3)
                    img.putpixel((x,y),color(tile));height[y*w+x]=z
            start,length,_=struct.unpack_from('<iii',idx,block*12)
            if start<0 or length<=0:continue
            for offset in range(start,start+length,7):
                tile,sx,sy,z,hue=struct.unpack_from('<HBBbH',statics,offset)
                x,y=bx*8+sx-x0,by*8+sy-y0
                if 0<=x<w and 0<=y<h and z>=height[y*w+x]:
                    img.putpixel((x,y),color(tile+0x4000));height[y*w+x]=z
    canvas=Image.new('RGB',(1120,970),'#f1eddf');canvas.paste(img,(20,55));draw=ImageDraw.Draw(canvas)
    font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',16);title=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',25)
    draw.text((20,12),'Stygian Abyss — current terrain and crafting sites',font=title,fill='#203640')
    for i,site in enumerate(sites):
        x,y,z=site['center'];px,py=x-x0+20,y-y0+55
        draw.ellipse((px-10,py-10,px+10,py+10),fill='#f9d36b',outline='black')
        draw.text((px-6,py-11),str(i+1),font=font,fill='black')
        draw.text((805,60+i*61),str(i+1)+'. '+site['name'],font=font,fill='#203640')
        draw.text((805,84+i*61),site['essence'].replace('Essence','Essence of '),font=font,fill='#52634f')
    draw.text((20,946),'Read-only map survey. Proposed spawn centers are markers; no live changes.',font=font,fill='#203640')
    canvas.save(output)
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('data');parser.add_argument('sites');parser.add_argument('output');a=parser.parse_args()
    render(a.data,a.sites,a.output)
