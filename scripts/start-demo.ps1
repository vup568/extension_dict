#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$EnvFile = (Join-Path (Split-Path -Parent $PSScriptRoot) '.env'),
    [switch]$ValidateOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'local-backend-config.ps1')
$EnvFile = (Resolve-Path -LiteralPath $EnvFile).Path
$values = Read-LocalPostgresEnvironment -Path $EnvFile
& (Join-Path $PSScriptRoot 'prepare-demo.ps1') -EnvFile $EnvFile -ValidateOnly:$ValidateOnly
if ($ValidateOnly) { return }

$demoValues = $values.Clone()
$demoValues['POSTGRES_DB'] = 'jp_dict_demo'
$priorConnection = [Environment]::GetEnvironmentVariable('ConnectionStrings__DefaultConnection', 'Process')
try {
    [Environment]::SetEnvironmentVariable('ConnectionStrings__DefaultConnection', (New-LocalBackendConnectionString -Values $demoValues), 'Process')
    Write-Output 'DEMO MODE: jp_dict_demo, 17 synthetic words only. Stop with Ctrl+C; use start-local.ps1 for the original database.'
    & (Join-Path $PSScriptRoot 'start-local.ps1') -EnvFile $EnvFile -DatasetVersion 'candidate-20261110.1' -SkipDependencies
} finally {
    [Environment]::SetEnvironmentVariable('ConnectionStrings__DefaultConnection', $priorConnection, 'Process')
}
