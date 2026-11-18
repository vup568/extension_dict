#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$EnvFile = (Join-Path (Split-Path -Parent $PSScriptRoot) '.env'),
    [string]$Target = 'jp_dict_real_jaen_20261030_v3',
    [string]$RunId = 'jaen_20261030_v3',
    [string]$SourceLock = (Join-Path (Split-Path -Parent $PSScriptRoot) 'data/dictionary-import/audit-20261015/source-lock.ja-en-v3.json'),
    [string]$DatasetVersion = 'candidate-20261013.1',
    [switch]$SkipDependencies
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($Target -cnotmatch '^jp_dict_real_[a-z0-9][a-z0-9_]{0,40}$') {
    throw 'A dedicated jp_dict_real_ target database is required.'
}
. (Join-Path $PSScriptRoot 'local-backend-config.ps1')
$EnvFile = (Resolve-Path -LiteralPath $EnvFile).Path
$SourceLock = (Resolve-Path -LiteralPath $SourceLock).Path
$values = Read-LocalPostgresEnvironment -Path $EnvFile
$values['POSTGRES_DB'] = $Target
$updates = @{
    ConnectionStrings__DefaultConnection = New-LocalBackendConnectionString -Values $values
    DictionaryImport__LocalTarget = $Target
    DictionaryImport__LocalRunId = $RunId
    DictionaryImport__LocalSourceLock = $SourceLock
    DictionaryImport__LocalSourceLockSha256 = (Get-FileHash -LiteralPath $SourceLock -Algorithm SHA256).Hash.ToLowerInvariant()
}
$original = @{}
foreach ($key in $updates.Keys) { $original[$key] = [Environment]::GetEnvironmentVariable($key, 'Process') }
try {
    foreach ($key in $updates.Keys) { [Environment]::SetEnvironmentVariable($key, $updates[$key], 'Process') }
    Write-Output "Starting local dictionary database $Target. Integrity validation may take several minutes. Stop with Ctrl+C."
    & (Join-Path $PSScriptRoot 'start-local.ps1') -EnvFile $EnvFile -DatasetVersion $DatasetVersion -SkipDependencies:$SkipDependencies
} finally {
    foreach ($key in $original.Keys) { [Environment]::SetEnvironmentVariable($key, $original[$key], 'Process') }
}
