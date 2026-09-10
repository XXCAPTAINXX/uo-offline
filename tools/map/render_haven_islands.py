"""Render a review map from the staged MUL terrain, with planned facilities marked."""
import argparse
import json
import struct
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

def render(data, plan_file, output):
    data=Path(data); plan=json.loads(Path(plan_file).read_text(encoding='utf-8'))
    raw=(data/'map1.mul').read_bytes(); colors=(data/'radarcol.mul').read_bytes()
    canvas=Image.new('RGB',(1200,690),'#f3efdf'); draw=ImageDraw.Draw(canvas)
    font_path='C:/Windows/Fonts/segoeui.ttf'
    font=ImageFont.truetype(font_path,18); title=ImageFont.truetype(font_path,27)
    for panel,key in enumerate(('commons','pirate_estate')):
        ox,oy=plan[key]['origin']; px=35+panel*595; py=80; scale=3
        terrain=Image.new('RGB',(176,176))
        for x in range(176):
            for y in range(176):
                wx,wy=ox+x,oy+y; off=((wx//8)*512+wy//8)*196+4+((wy%8)*8+wx%8)*3
                tile=struct.unpack_from('<H',raw,off)[0]; color=struct.unpack_from('<H',colors,tile*2)[0]
                terrain.putpixel((x,y),(((color>>10)&31)*255//31,((color>>5)&31)*255//31,(color&31)*255//31))
        canvas.paste(terrain.resize((528,528),Image.Resampling.NEAREST),(px,py))
        draw.text((px,25),['Haven Commons','Corsair’s Rest'][panel],font=title,fill='#203640')
        def box(x,y,w,h,color):
            draw.rectangle((px+x*scale,py+y*scale,px+(x+w)*scale,py+(y+h)*scale),fill=color,outline='white',width=2)
        def label(x,y,text):
            tx,ty=px+x*scale,py+y*scale; bounds=draw.textbbox((tx,ty),text,font=font)
            draw.rectangle((bounds[0]-4,bounds[1]-2,bounds[2]+4,bounds[3]+3),fill='#f3efdf')
            draw.text((tx,ty),text,font=font,fill='#203640')
        if panel==0:
            box(64,58,33,41,'#c1b59b'); label(45,42,'Market · library · workshops')
            label(53,102,'Training and services'); box(87,136,5,28,'#a57b55')
            label(98,150,'Dock')
        else:
            box(48,48,40,40,'#869e68'); label(43,37,'Castle-sized house plot')
            box(98,44,24,24,'#375d3a'); label(108,29,'Grove')
            box(106,86,12,12,'#878777'); label(114,101,'Mine')
            box(43,106,16,14,'#c7795b'); label(28,124,'Pirate camp')
            box(85,135,5,29,'#a57b55'); box(90,159,10,3,'#a57b55'); label(104,151,'Dock')
        draw.text((px,624),f'Trammel survey origin: {ox}, {oy}',font=font,fill='#203640')
    draw.text((35,660),'Staged terrain with planned facilities marked — not installed in the live world',font=font,fill='#203640')
    canvas.save(output)

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('data');p.add_argument('plan');p.add_argument('output');a=p.parse_args()
    render(a.data,a.plan,a.output)
