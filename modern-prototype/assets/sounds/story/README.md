# Jenna: English-accent neural dialogue

Original Haven dialogue synthesized with Microsoft en-GB-SoniaNeural through edge-tts. This replaces the temporary Windows Zira desktop voice. No actor cloning or book excerpts.

Install edge-tts and imageio-ffmpeg in a build-only Python environment. Build-StoryVoiceNeural.py (or Build-StoryVoice.ps1) generates thirteen genuine MP3 masters under encoded/, WAV masters, headerless game overrides, a manifest and the matching duration table in HavenStoryVoice.cs. Only the original dialogue text goes to the online synthesis service. Gameplay plays the installed files locally.

Install-StoryVoice.ps1 installs IDs 32750–32762. ICQ 32766 is untouched. IDs 32760–32762 now contain chapter-two dialogue; old versions are replaced by the installer. Fully restart TazUO after installing because it caches available overrides/audio.

The installed TazUO loader scans .mp3 filenames but plays raw signed 16-bit mono 22050Hz PCM. Numbered .mp3 files are therefore deliberately headerless PCM. The encoded/ MP3 and named WAV files are for ordinary playback.

Gift explanations play only after successful delivery. Missing starter gifts are retried on login near Jenna after the introduction, with journal collection as a fallback for full packs/follower slots. Received-gift buttons replay their explanations without giving duplicate rewards. Audio queues in order using measured clip lengths, skips duplicate queued lines, and clears queued playback when muted or disconnected. Current playing audio cannot be stopped by the server mute toggle.

Technical waveform/build/runtime checks are automated. Final voice quality and in-client listening are user-reviewed.

Game overrides receive a reproducible +3 dB voice gain from the unmodified WAV masters. Packaging rejects clipping; rebuilding never compounds the gain.
