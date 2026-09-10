[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Destination,
    [Parameter(Mandatory)][string]$ClientData,
    [string]$DotnetPath = 'dotnet',
    [ValidateRange(1024,65535)][int]$Port = 2699,
    [switch]$Test
)
$ErrorActionPreference = 'Stop'
if (Test-Path -LiteralPath $Destination) { throw 'Use a new destination; this builder never overwrites a shard.' }
$ClientData = (Resolve-Path -LiteralPath $ClientData).Path
foreach ($file in @('tiledata.mul', 'map0LegacyMUL.uop', 'map1LegacyMUL.uop', 'map5LegacyMUL.uop')) {
    if (!(Test-Path -LiteralPath (Join-Path $ClientData $file))) { throw "Missing modern Classic data: $file" }
}
if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) { throw "Port $Port is in use." }
$null = Get-Command git -ErrorAction Stop
$null = Get-Command $DotnetPath -ErrorAction Stop
$root = (New-Item -ItemType Directory -Path $Destination).FullName
& git init $root
if ($LASTEXITCODE) { throw 'git init failed' }
& git -C $root remote add origin https://github.com/ServUO/ServUO.git
if ($LASTEXITCODE) { throw 'git remote failed' }
& git -C $root fetch --depth 1 origin d76bf4443cf76d081ddaf8f57c87ff33749256af
if ($LASTEXITCODE) { throw 'Source download failed' }
& git -C $root checkout --detach FETCH_HEAD
if ($LASTEXITCODE) { throw 'Source checkout failed' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'source/HavenCompanion.cs') -Destination (Join-Path $root 'Scripts/HavenCompanion.cs')
if ($Test) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'tests/CompanionSmoke.cs') -Destination (Join-Path $root 'Scripts/CompanionSmoke.cs')
    Set-Content -LiteralPath (Join-Path $root 'COMPANION-TEST-ONLY') -Value 'Disposable test account and fixtures. Never deploy.'
}
Set-Content -LiteralPath (Join-Path $root 'Config/Server.cfg') -Value "Name=Haven Companion Prototype`nListen=127.0.0.1`nAddress=127.0.0.1`nPort=$Port"
Set-Content -LiteralPath (Join-Path $root 'Config/DataPath.cfg') -Value "CustomPath=$ClientData"
Set-Content -LiteralPath (Join-Path $root 'Config/Compiler.cfg') -Value 'Dynamic=False'
& $DotnetPath build (Join-Path $root 'ServUO.sln') -c Release -p:Platform=x64 --nologo -v:quiet *> (Join-Path $root 'prototype-build.log')
if ($LASTEXITCODE) { throw "Build failed: $root/prototype-build.log" }
if (!$Test) { Write-Output "Prototype built, not started. Run $root/ServUO.exe to set up a new test account. Use [c in game. Listener: 127.0.0.1:$Port"; return }
foreach ($phase in @('fresh','reload')) {
    $proc = Start-Process -FilePath (Join-Path $root 'ServUO.exe') -ArgumentList '-service' -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $root "$phase.log") -RedirectStandardError (Join-Path $root "$phase-error.log")
    try {
        $deadline = [DateTime]::UtcNow.AddSeconds(75)
        while (!$proc.WaitForExit(1000)) { if ([DateTime]::UtcNow -gt $deadline) { throw "Timed out in $phase" } }
    } finally {
        # Also stop a crash-restarted child, limited to this newly created directory.
        $testExe = Join-Path $root 'ServUO.exe'
        Get-Process -Name ServUO -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $testExe } | Stop-Process
    }
    $checks = Get-Content -LiteralPath (Join-Path $root 'companion-checks.log') -Raw
    if ($checks -match '(?m)^FAIL ' -or $checks -notmatch "PHASE $phase" -or $checks -notmatch 'COMPLETE failures=0') { throw "Tests failed. Inspect $root/companion-checks.log" }
}
$lines = Get-Content -LiteralPath (Join-Path $root 'companion-checks.log')
if (@($lines | Where-Object { $_ -eq 'COMPLETE failures=0' }).Count -ne 2) { throw 'Both phases did not complete' }
Write-Output ("Companion prototype passed {0} checks; test server stopped. Results: {1}" -f @($lines | Where-Object { $_ -like 'PASS *' }).Count, $root)
