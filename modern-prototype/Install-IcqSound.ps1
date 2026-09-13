param(
    [Parameter(Mandatory = $true)][string]$Ffmpeg,
    [string]$ClientPath = 'D:/Uo Offline/Haven-ServUO-Preview-Client'
)
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'assets/sounds/icq-uh-oh-clean.mp3'
$folder = Join-Path $ClientPath 'SoundOverrides'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$destination = Join-Path $folder '32766.mp3'
$temporary = Join-Path $folder '32766.pending'
# This TazUO loader selects *.mp3 but passes its bytes straight to the
# 22050 Hz mono PCM16 sound effect player. The extension is misleading:
# neither compressed MP3 bytes nor a WAV header belong in this override.
& $Ffmpeg -hide_banner -loglevel error -y -i $source -map_metadata -1 -ac 1 -ar 22050 -c:a pcm_s16le -f s16le $temporary
if ($LASTEXITCODE -ne 0) { throw 'ICQ sound conversion failed.' }
$size = (Get-Item -LiteralPath $temporary).Length
if ($size -lt 4410 -or $size -gt 441000 -or $size % 2 -ne 0) { throw 'Unexpected PCM sound length.' }
if (Test-Path -LiteralPath $destination) {
    Copy-Item -LiteralPath $destination -Destination ($destination + '.backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
}
Move-Item -LiteralPath $temporary -Destination $destination -Force
Write-Output ('Installed PCM16 mono 22050 Hz ICQ alert: {0:N2} seconds. Restart TazUO to reload its sound cache.' -f ($size / 44100))
Get-FileHash -LiteralPath $destination -Algorithm SHA256
