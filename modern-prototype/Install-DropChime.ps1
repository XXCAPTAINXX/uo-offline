param([string]$ClientPath='D:/Uo Offline/Haven-ServUO-Preview-Client')
$ErrorActionPreference='Stop'
$source=Join-Path $PSScriptRoot 'assets/sounds/treasure-drop-chime.pcm'
$bytes=[IO.File]::ReadAllBytes($source)
if($bytes.Length -lt 50714 -or $bytes.Length -gt 50716 -or $bytes.Length%2 -ne 0){throw 'Unexpected treasure chime PCM length.'}
$folder=Join-Path $ClientPath 'SoundOverrides'
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$target=Join-Path $folder '32766.mp3'
if(Test-Path -LiteralPath $target){Copy-Item -LiteralPath $target -Destination ($target+'.before-chime-'+(Get-Date -Format yyyyMMdd-HHmmss))}
[IO.File]::WriteAllBytes($target,$bytes)
Write-Output 'Treasure chime installed in place of ICQ. Fully restart TazUO to refresh cached audio. No server restart required.'
