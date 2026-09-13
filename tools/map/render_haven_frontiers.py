"""Draw a labelled survey from staged MUL terrain, not an in-game screenshot."""
from pathlib import Path
import argparse, json, struct
from PIL import Image, ImageDraw, ImageFont

def render(data, output):
    data=Path(data); plan=json.loads((data/'haven-frontiers-manifest.json').read_text())
    raw=(data/'map1.mul').read_bytes(); image=Image.new('RGB',(1260,550),'#eee9dd'); draw=ImageDraw.Draw(image)
    font=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
    title=ImageFont.truetype('C:/Windows/Fonts/seguisb.ttf',25)
    small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',13)
    names=['Shadowguard','Blackthorn rifts','Chelonia']
    keys=['shadowguard','blackthorn','chelonia']
    for n,key in enumerate(keys):
        ox,oy=plan[key]; px=25+n*415; py=83
        draw.text((px,20),names[n],font=title,fill='#223e40')
        draw.text((px,52),f'Trammel · origin {ox}, {oy}',font=small,fill='#54625b')
        for x in range(176):
            for y in range(176):
                wx,wy=ox+x,oy+y; block=(wx//8)*512+wy//8
                tile,z=struct.unpack_from('<Hb',raw,block*196+4+((wy%8)*8+wx%8)*3)
                color='#235d74' if tile in {0xA8,0xA9,0xAA,0xAB,0x136,0x137} else '#d8c48b' if tile==0x16 else '#666960' if tile==0x22 else '#81965c'
                draw.rectangle((px+x*2,py+y*2,px+x*2+1,py+y*2+1),fill=color)
        if n==0:
            for i,label in enumerate(['Bar','Orchard','Armory','Fountain','Belfry','Roof']):
                x=px+(50+i%3*38)*2; y=py+(65+i//3*40)*2
                draw.rectangle((x-32,y-32,x+32,y+32),fill='#c6c0af',outline='#494841',width=3)
                draw.text((x,y),label,font=small,fill='#24343b',anchor='mm')
        elif n==1:
            for i in range(3):
                x=px+(62+i*26)*2; y=py+88*2
                draw.rectangle((x-5,y-23,x+5,y+23),fill='#c6c0af')
                draw.text((x,y-38),str(i+1),font=font,fill='white',anchor='mm')
        else:
            draw.rectangle((px+86*2,py+133*2,px+90*2,py+160*2),fill='#9e7246')
            draw.ellipse((px+90*2,py+87*2,px+103*2,py+96*2),fill='#304f40',outline='#b8c879',width=2)
            draw.text((px+82*2,py+102*2),'Nesting valley',font=small,fill='#16342c',anchor='mm')
        draw.text((px,455),['Five room seals → four Roof bosses','Guards → captains → beacon, three waves','Amphibious tortoises · dock · corsair voyages'][n],font=small,fill='#223e40')
    draw.text((25,510),'Survey layout from staged terrain. Buildings and landmarks are annotated; this is not an in-game capture.',font=font,fill='#54625b')
    image.save(output)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('data');parser.add_argument('output');args=parser.parse_args();render(args.data,args.output)
