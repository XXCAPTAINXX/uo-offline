# Jenna: A Light That Answers
Original dialogue written for Haven, spoken by Microsoft Zira Desktop using System.Speech. This is a temporary synthetic female voice, not an actor performance or voice clone.

Build-StoryVoice.ps1 creates the WAVs; Package-StoryVoice.py validates and packages them. Install-StoryVoice.ps1 installs sound IDs32760–32764. The existing ICQ sound32766 is untouched.

The installed TazUO override loader scans `.mp3` names but plays their bytes as raw signed16-bit mono22050Hz PCM. Consequently the numbered files deliberately contain headerless PCM, not encoded MP3. Use the named WAV files for ordinary playback/editing. Real MP3 or RIFF headers in these overrides cause static.

Restart TazUO after installing. The quest journal provides replay and mute controls; subtitles are always available. Voice playback is private to the player. A22-second guard prevents overlapping clips; each clip is under12seconds. In-client listening still requires verification.
