"""Package our original synthetic story WAVs for the installed TazUO raw-PCM loader."""
from pathlib import Path
import json, math, struct, wave
root = Path(__file__).parent / 'assets' / 'sounds' / 'story'
report = []
gain_db = 3.0
gain = 10 ** (gain_db / 20.0)
for i, path in enumerate(sorted(root.glob('jenna-beacon-*.wav'))):
    with wave.open(str(path), 'rb') as wav:
        assert (wav.getnchannels(), wav.getsampwidth(), wav.getframerate()) == (1, 2, 22050)
        data = wav.readframes(wav.getnframes())
    seconds = len(data) / 44100
    assert 1 < seconds < 30
    samples = struct.unpack('<%dh' % (len(data)//2), data)
    peak = max(abs(x) for x in samples)
    assert 100 < peak < 32760
    amplified = [round(sample * gain) for sample in samples]
    output_peak = max(abs(sample) for sample in amplified)
    assert output_peak < 32760, 'Voice boost would clip: ' + path.name
    data = struct.pack('<%dh' % len(amplified), *amplified)
    (root / ('%d.mp3' % (32750+i))).write_bytes(data)
    report.append(dict(file=path.name, sound=32750+i, seconds=round(seconds,2), peak=output_peak, source_peak=peak, gain_db=gain_db, voice='en-GB-SoniaNeural',
                       format='PCM16 mono 22050Hz; override is headerless despite .mp3 extension'))
assert len(report) == 13
(root / 'manifest.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report, indent=2))
