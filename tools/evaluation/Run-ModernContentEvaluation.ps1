[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Destination,
    [Parameter(Mandatory)][string]$ClientData,
    [string]$DotnetPath = 'dotnet',
    [ValidateRange(1024,65535)][int]$Port = 2699
)

# Fresh disposable world only. Does not read or modify an existing shard.
$ErrorActionPreference = 'Stop'
$pin = 'd76bf4443cf76d081ddaf8f57c87ff33749256af'
if (Test-Path -LiteralPath $Destination) { throw 'Destination must not exist. Choose a new evaluation directory.' }
$ClientData = (Resolve-Path -LiteralPath $ClientData).Path
foreach ($name in @('tiledata.mul','map0LegacyMUL.uop','map5LegacyMUL.uop','staidx5.mul','statics5.mul')) {
    if (!(Test-Path -LiteralPath (Join-Path $ClientData $name))) { throw "Missing modern Classic data: $name" }
}
if (Get-NetTCPConnection -State Listen -LocalPort $Port -ErrorAction SilentlyContinue) { throw "Port $Port is already in use." }
$null = Get-Command $DotnetPath -ErrorAction Stop
$null = Get-Command git -ErrorAction Stop
$root = (New-Item -ItemType Directory -Path $Destination).FullName
$repo = Join-Path $root 'ServUO'
& git init $repo
if ($LASTEXITCODE) { throw 'git init failed' }
& git -C $repo remote add origin https://github.com/ServUO/ServUO.git
if ($LASTEXITCODE) { throw 'git remote failed' }
& git -C $repo fetch --depth 1 origin $pin
if ($LASTEXITCODE) { throw 'Pinned source download failed' }
& git -C $repo checkout --detach FETCH_HEAD
if ($LASTEXITCODE) { throw 'Pinned source checkout failed' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'HavenEvaluation.cs') -Destination (Join-Path $repo 'Scripts/HavenEvaluation.cs')
Set-Content -LiteralPath (Join-Path $repo 'EVALUATION-ONLY') -Value 'Disposable evaluation. Never deploy this harness.'
Set-Content -LiteralPath (Join-Path $repo 'Config/Server.cfg') -Value "Name=Haven Modern Content Evaluation`nListen=127.0.0.1`nAddress=127.0.0.1`nPort=$Port"
Set-Content -LiteralPath (Join-Path $repo 'Config/DataPath.cfg') -Value "CustomPath=$ClientData"
Set-Content -LiteralPath (Join-Path $repo 'Config/Compiler.cfg') -Value 'Dynamic=False'
& $DotnetPath build (Join-Path $repo 'ServUO.sln') -c Release -p:Platform=x64 --nologo -v:quiet *> (Join-Path $root 'build.log')
if ($LASTEXITCODE) { throw "Build failed. See $root/build.log" }

foreach ($phase in @('fresh','reload')) {
    $proc = Start-Process -FilePath (Join-Path $repo 'ServUO.exe') -ArgumentList '-service' -WorkingDirectory $repo -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $root "$phase.log") -RedirectStandardError (Join-Path $root "$phase-error.log")
    try {
        $deadline = [DateTime]::UtcNow.AddSeconds(90)
        while (!$proc.WaitForExit(1000)) {
            if ([DateTime]::UtcNow -gt $deadline) { throw "Timed out in $phase phase." }
        }
    } finally {
        if (!$proc.HasExited) { $proc.Kill(); $proc.WaitForExit() }
    }
    $report = Get-Content -LiteralPath (Join-Path $root 'runtime-checks.log') -Raw
    if ($report -match '(?m)^FAIL ' -or $report -notmatch "PHASE $phase" -or $report -notmatch 'COMPLETE failures=0') {
        throw "Runtime evaluation failed. Inspect $root/runtime-checks.log and $phase.log."
    }
}
$lines = Get-Content -LiteralPath (Join-Path $root 'runtime-checks.log')
if (@($lines | Where-Object { $_ -eq 'COMPLETE failures=0' }).Count -ne 2) { throw 'Both phases did not complete.' }
$passes = @($lines | Where-Object { $_ -like 'PASS *' }).Count
Write-Output "Completed $passes passing checks. Candidate stopped. Evidence: $root"
