[CmdletBinding()]
param([string]$ServerPath = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $ServerPath).Path
if (!(Test-Path -LiteralPath (Join-Path $root 'HAVEN-INTERACTIVE-PREVIEW'))) { throw 'Not an opted-in preview.' }
$exe = Join-Path $root 'ServUO.exe'
$processes = @(Get-Process -Name ServUO -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe })
if ($processes.Count -eq 0) { Write-Output 'Preview is already stopped.'; return }
Set-Content -LiteralPath (Join-Path $root 'PREVIEW-SAVE-AND-STOP') -Value 'Local operator requested a clean save and shutdown.'
foreach ($proc in $processes) { if (!$proc.WaitForExit(60000)) { throw 'Clean shutdown did not finish; no process was forcibly stopped.' } }
if (Test-Path -LiteralPath (Join-Path $root 'PREVIEW-SAVE-AND-STOP')) { throw 'Server exited without confirming the save request.' }
Write-Output 'Preview saved and stopped. Original Haven was not affected.'
