[CmdletBinding()]
param(
    [string]$EnvFile = (Join-Path (Split-Path -Parent $PSScriptRoot) '.env'),
    [string]$DatasetVersion = 'candidate-20261013.1',
    [switch]$ValidateOnly,
    [switch]$SkipDependencies
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'local-backend-config.ps1')
$projectRoot = Split-Path -Parent $PSScriptRoot
$EnvFile = (Resolve-Path -LiteralPath $EnvFile).Path
$values = Read-LocalPostgresEnvironment -Path $EnvFile
$connection = [Environment]::GetEnvironmentVariable('ConnectionStrings__DefaultConnection', 'Process')
if ([string]::IsNullOrWhiteSpace($connection)) { $connection = New-LocalBackendConnectionString -Values $values }
if ($ValidateOnly) {
    Write-Output 'Local configuration is valid. No services started or database changed.'
    return
}

# Restore all process variables on exit. Never persist secrets with setx or echo connection strings.
$updates = @{
    ConnectionStrings__DefaultConnection = $connection
    ConjugationDatasetVersion = $DatasetVersion
    GrpcTokenizerAddress = 'http://localhost:50051'
}
foreach ($key in $values.Keys) { $updates[$key] = $values[$key] }
$original = @{}
foreach ($key in $updates.Keys) { $original[$key] = [Environment]::GetEnvironmentVariable($key, 'Process') }
try {
    foreach ($key in $updates.Keys) { [Environment]::SetEnvironmentVariable($key, $updates[$key], 'Process') }
    if (-not $SkipDependencies) {
        # Existing images only; no implicit dependency/image builds or volume resets.
        & docker compose --project-directory $projectRoot --env-file $EnvFile --profile nlp up -d --no-build --wait db tokenizer
        if ($LASTEXITCODE -ne 0) { throw 'Docker services failed to start. Check Docker Desktop and existing images.' }
    }
    Write-Output 'Starting local backend. Its existing EF migrations will run on the configured database.'
    & dotnet run --project (Join-Path $projectRoot 'src/interface/WebApi.csproj') --no-restore --urls http://127.0.0.1:5000
    if ($LASTEXITCODE -ne 0) { throw 'Backend startup failed. Check PostgreSQL and configuration; no connection string is printed.' }
} finally {
    foreach ($key in $original.Keys) { [Environment]::SetEnvironmentVariable($key, $original[$key], 'Process') }
}
