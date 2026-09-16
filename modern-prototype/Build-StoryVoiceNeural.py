"""Build original Jenna dialogue with an English neural voice and TazUO PCM overrides.
Requires edge-tts and imageio-ffmpeg. Only the original dialogue is sent for synthesis.
"""
import argparse, asyncio, json, re, subprocess, sys, wave
from pathlib import Path
parser=argparse.ArgumentParser()
parser.add_argument('--dependencies')
parser.add_argument('--start-index',type=int,default=0)
parser.add_argument('--voice',default='en-GB-SoniaNeural')
args=parser.parse_args()
if args.dependencies: sys.path.insert(0,args.dependencies)
import edge_tts, imageio_ffmpeg
root=Path(__file__).parent
source=root/'source/HavenBeaconQuest.cs'
section=re.search(r'VoiceLines=\{(.*?)\};',source.read_text(),re.S).group(1)
lines=re.findall(r'"([^"\n]*)"',section)
assert len(lines)==13
output=root/'assets/sounds/story'
encoded=output/'encoded'
encoded.mkdir(exist_ok=True)
async def build():
    report=[]
    for i,line in enumerate(lines):
        mp3=encoded/f'jenna-beacon-{i+1:02}.mp3'
        wav=output/f'jenna-beacon-{i+1:02}.wav'
        if i >= args.start_index or not mp3.exists():
            await edge_tts.Communicate(line,args.voice,rate='-3%').save(str(mp3))
        subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-y','-loglevel','error','-i',str(mp3),'-ac','1','-ar','22050','-c:a','pcm_s16le',str(wav)],check=True)
        with wave.open(str(wav),'rb') as f:
            data=f.readframes(f.getnframes());seconds=len(data)/44100
        assert 1<seconds<30
        (output/f'{32750+i}.mp3').write_bytes(data)
        report.append(dict(file=wav.name,sound=32750+i,seconds=round(seconds,3),voice=args.voice,format='PCM16 mono 22050Hz; override headerless despite .mp3 extension'))
        print(f'Generated {i+1}/{len(lines)}: {seconds:.2f}s',flush=True)
    (output/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
    voice_source=root/'source/HavenStoryVoice.cs'
    text=voice_source.read_text()
    text=re.sub(r'VoiceSeconds=\{.*?\}', 'VoiceSeconds={'+','.join(str(x['seconds']) for x in report)+'}',text)
    voice_source.write_text(text)
asyncio.run(build())
subprocess.run([sys.executable,str(root/'Package-StoryVoice.py')],check=True)
