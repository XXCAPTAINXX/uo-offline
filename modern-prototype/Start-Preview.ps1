[CmdletBinding()]
param([string]$ServerPath = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $ServerPath).Path
foreach ($required in 'ServUO.exe','preview-world-ready.txt','HAVEN-INTERACTIVE-PREVIEW') {
    if (!(Test-Path -LiteralPath (Join-Path $root $required))) { throw "Not a prepared preview: missing $required" }
}
foreach ($blocked in 'COMPANION-TEST-ONLY','HAVEN-PREVIEW-SETUP','PREVIEW-SAVE-AND-STOP') {
    if (Test-Path -LiteralPath (Join-Path $root $blocked)) { throw "Resolve the pending $blocked marker before interactive startup." }
}
$exe = Join-Path $root 'ServUO.exe'
$running = Get-Process -Name ServUO -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe }
if ($running) { Write-Output 'The preview server is already running.'; return }
$portLine = Get-Content -LiteralPath (Join-Path $root 'Config/Server.cfg') | Where-Object { $_ -match '^Port=' }
$port = [int]($portLine -replace '^Port=','')
if (Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue) { throw "Port $port belongs to another process." }
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$proc = Start-Process -FilePath $exe -ArgumentList '-service' -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $root "session-$stamp.log") -RedirectStandardError (Join-Path $root "session-$stamp-error.log")
$deadline = [DateTime]::UtcNow.AddSeconds(60)
while ([DateTime]::UtcNow -lt $deadline) {
    if ($proc.HasExited) { throw "Preview exited; inspect session-$stamp.log." }
    $listener = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Where-Object { $_.OwningProcess -eq $proc.Id }
    if ($listener) { Write-Output "Haven preview ready at 127.0.0.1:$port. Use the separate preview client."; return }
    Start-Sleep -Milliseconds 500
}
throw "Startup not confirmed after 60 seconds; inspect session-$stamp.log. The process was left intact."
