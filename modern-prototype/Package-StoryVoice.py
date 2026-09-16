"""Package our original synthetic story WAVs for the installed TazUO raw-PCM loader."""
from pathlib import Path
import json, struct, wave
root = Path(__file__).parent / 'assets' / 'sounds' / 'story'
report = []
for i, path in enumerate(sorted(root.glob('jenna-beacon-*.wav'))):
    with wave.open(str(path), 'rb') as wav:
        assert (wav.getnchannels(), wav.getsampwidth(), wav.getframerate()) == (1, 2, 22050)
        data = wav.readframes(wav.getnframes())
    seconds = len(data) / 44100
    assert 1 < seconds < 30
    samples = struct.unpack('<%dh' % (len(data)//2), data)
    peak = max(abs(x) for x in samples)
    assert 100 < peak < 32760
    (root / ('%d.mp3' % (32750+i))).write_bytes(data)
    report.append(dict(file=path.name, sound=32750+i, seconds=round(seconds,2), peak=peak, voice='en-GB-SoniaNeural',
                       format='PCM16 mono 22050Hz; override is headerless despite .mp3 extension'))
assert len(report) == 8
(root / 'manifest.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report, indent=2))
