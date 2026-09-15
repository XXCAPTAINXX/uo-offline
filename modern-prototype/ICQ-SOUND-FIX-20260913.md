# ICQ world-drop static fix

The installed TazUO ClassicUO.Assets.SoundOverrideLoader selects files named
`SoundOverrides/*.mp3` but reads their bytes without decoding. Its installed
ClassicUO.IO.Audio.UOSound passes those bytes to a mono 22050 Hz PCM16 player.
The previously installed real MP3 therefore played as static.

Install-IcqSound.ps1 converts the existing cleaned source to headerless signed
16-bit little-endian mono PCM at 22050 Hz, retaining the filename required by
this client's loader. It backs up the previous override before replacement.
Do not replace this file with a real MP3 or a WAV container for this client.
Recheck this workaround if the client sound loader is upgraded.

Installed at D:/Uo Offline/Haven-ServUO-Preview-Client/SoundOverrides/32766.mp3.
SHA256: 383735B345E77540A798D314C8225F206F6FA34665D32EAD300085E446293466.
Verified installed bytes match freshly decoded source exactly; no clipped samples.
The world-drop trigger remains sound 0x7FFE with its existing three-second debounce.
No server restart required. A full client restart is required to reload the cache.
In-game listening after that restart remains to be confirmed.
