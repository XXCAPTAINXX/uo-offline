param(
    [string]$InstallPath = 'D:\Uo Offline\uo-modernuo',
    [string]$PayloadPath = (Join-Path $PSScriptRoot 'payload')
)

$ErrorActionPreference = 'Stop'
$installRoot = (Resolve-Path -LiteralPath $InstallPath).Path
$payloadRoot = (Resolve-Path -LiteralPath $PayloadPath).Path
$versionFile = Join-Path $installRoot 'uo-offline-version.json'
$engineRoot = Join-Path $installRoot 'ModernUO'
$distribution = Join-Path $engineRoot 'Distribution'
$assembly = Join-Path $distribution 'Assemblies\UOContent.dll'

if (-not (Test-Path -LiteralPath $versionFile) -or -not (Test-Path -LiteralPath $assembly)) {
    throw 'This is not a complete UO Offline installation.'
}
$version = Get-Content -LiteralPath $versionFile -Raw | ConvertFrom-Json
if ($version.Repo -ne 'XXCAPTAINXX/uo-offline' -or $version.Sha -ne '75bc52248f1856e5fd41ac83226f931b469b3181') {
    throw 'This prebuilt update requires the haven-rc4 release. Use the source installer for other versions.'
}

$running = Get-CimInstance Win32_Process | Where-Object {
    $_.Name -eq 'ModernUO.exe' -or ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match '(?i)ModernUO\.dll')
}
if ($running) {
    throw 'Save your world and shut down the ModernUO server before applying this update. No files were changed.'
}

$manifest = Get-Content -LiteralPath (Join-Path $payloadRoot 'manifest.json') -Raw | ConvertFrom-Json
$newAssembly = Join-Path $payloadRoot 'UOContent.dll'
if ((Get-FileHash -LiteralPath $newAssembly -Algorithm SHA256).Hash -ne $manifest.AssemblySha256) {
    throw 'The update assembly does not match its manifest.'
}

$backup = Join-Path $installRoot ('haven-world-backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $backup | Out-Null
Copy-Item -LiteralPath $assembly -Destination (Join-Path $backup 'UOContent.dll')
$pdb = Join-Path $distribution 'Assemblies\UOContent.pdb'
if (Test-Path -LiteralPath $pdb) { Copy-Item -LiteralPath $pdb -Destination $backup }
$saves = Join-Path $distribution 'Saves'
if (Test-Path -LiteralPath $saves) { Copy-Item -LiteralPath $saves -Destination (Join-Path $backup 'Saves') -Recurse }
$source = Join-Path $engineRoot 'Projects\UOContent\CustomBots'
if (Test-Path -LiteralPath $source) { Copy-Item -LiteralPath $source -Destination (Join-Path $backup 'CustomBots') -Recurse }

New-Item -ItemType Directory -Force -Path $source | Out-Null
Copy-Item -Path (Join-Path $payloadRoot 'CustomBots\*') -Destination $source -Recurse -Force
$nativeSource = Join-Path $payloadRoot 'NativeSource'
if (Test-Path -LiteralPath $nativeSource) {
    foreach ($file in Get-ChildItem -LiteralPath $nativeSource -File -Recurse) {
        $relative = $file.FullName.Substring($nativeSource.Length).TrimStart([char[]]'\/' )
        $target = Join-Path (Join-Path $engineRoot 'Projects\UOContent') $relative
        $saved = Join-Path (Join-Path $backup 'NativeSource') $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $saved) | Out-Null
        if (Test-Path -LiteralPath $target) { Copy-Item -LiteralPath $target -Destination $saved }
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }
}
Copy-Item -LiteralPath $newAssembly -Destination $assembly -Force
Copy-Item -LiteralPath (Join-Path $payloadRoot 'UOContent.pdb') -Destination $pdb -Force
Copy-Item -LiteralPath (Join-Path $payloadRoot 'manifest.json') -Destination (Join-Path $installRoot 'haven-world-update.json') -Force

Write-Host "Update installed. Previous files and saves are backed up at: $backup"
Write-Host 'Start UO Offline normally. World population runs automatically; use [WorldStatus to check progress.'
