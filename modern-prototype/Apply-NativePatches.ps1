[CmdletBinding()]
param([Parameter(Mandatory)][string]$ServerPath)
$ErrorActionPreference = 'Stop'
$patchRoot = Join-Path $PSScriptRoot 'patches'
$targetRoot = (Resolve-Path -LiteralPath $ServerPath).Path
foreach ($patch in Get-ChildItem -LiteralPath $patchRoot -Filter '*.patch' | Sort-Object Name) {
    & git -C $targetRoot apply --reverse --check $patch.FullName 2>$null
    if ($LASTEXITCODE -eq 0) { Write-Output "Already applied: $($patch.Name)"; continue }
    & git -C $targetRoot apply --check $patch.FullName
    if ($LASTEXITCODE) { throw "Native source mismatch: $($patch.Name)" }
    & git -C $targetRoot apply $patch.FullName
    if ($LASTEXITCODE) { throw "Could not apply: $($patch.Name)" }
}
