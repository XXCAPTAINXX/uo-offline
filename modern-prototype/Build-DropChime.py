"""Original three-note treasure chime, packaged for TazUO's raw PCM loader."""
from pathlib import Path
import math
import struct
import wave

root = Path(__file__).parent / 'assets' / 'sounds'
root.mkdir(parents=True, exist_ok=True)
rate = 22050
duration = 1.15
notes = ((0.0, 1174.66), (0.13, 1479.98), (0.26, 1760.0))
samples = []
for i in range(round(duration * rate)):
    t = i / rate
    value = 0.0
    for start, frequency in notes:
        age = t - start
        if age < 0:
            continue
        attack = min(1.0, age / 0.006)
        envelope = attack * math.exp(-age * 6.0)
        value += envelope * (math.sin(2 * math.pi * frequency * age)
                             + 0.15 * math.sin(2 * math.pi * frequency * 2 * age))
    value *= min(1.0, max(0.0, (duration - t) / 0.10))
    samples.append(value)
peak = max(abs(value) for value in samples)
pcm = struct.pack('<%dh' % len(samples), *(round(value / peak * 14000) for value in samples))
assert len(pcm) % 2 == 0 and max(abs(x) for x in struct.unpack('<%dh' % len(samples), pcm)) <= 14000
with wave.open(str(root / 'treasure-drop-chime.wav'), 'wb') as output:
    output.setparams((1, 2, rate, 0, 'NONE', 'not compressed'))
    output.writeframes(pcm)
# TazUO expects headerless PCM despite the .mp3 extension.
(root / 'treasure-drop-chime.pcm').write_bytes(pcm)
print('Built original 1.15-second treasure chime: PCM16 mono 22050Hz, peak 14000, no clipping.')
