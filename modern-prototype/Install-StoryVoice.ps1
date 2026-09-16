param([string]$ClientPath='D:/Uo Offline/Haven-ServUO-Preview-Client')
$ErrorActionPreference='Stop'
$source=Join-Path $PSScriptRoot 'assets/sounds/story'
$destination=Join-Path $ClientPath 'SoundOverrides'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$backup=Join-Path $ClientPath ('StorySoundBackup-'+(Get-Date -Format yyyyMMdd-HHmmss))
foreach($id in 32750..32763){
 $file=Join-Path $source "$id.mp3"
 $bytes=[IO.File]::ReadAllBytes($file)
 if($bytes.Length -lt 44100 -or $bytes.Length -gt 1323000 -or $bytes.Length%2 -ne 0){throw "Invalid story PCM: $id"}
 $target=Join-Path $destination "$id.mp3"
 if(Test-Path -LiteralPath $target){New-Item -ItemType Directory -Path $backup -Force | Out-Null;Copy-Item -LiteralPath $target -Destination $backup}
 Copy-Item -LiteralPath $file -Destination $target
}
Write-Output 'Installed story sounds 32750-32763. Restart the client to refresh its sound inventory. Drop chime 32766 was untouched.'
