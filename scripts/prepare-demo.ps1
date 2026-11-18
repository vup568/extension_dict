#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$EnvFile = (Join-Path (Split-Path -Parent $PSScriptRoot) '.env'),
    [switch]$ValidateOnly,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'local-backend-config.ps1')
. (Join-Path $PSScriptRoot 'local-recovery-tools.ps1')
$projectRoot = Split-Path -Parent $PSScriptRoot
$EnvFile = (Resolve-Path -LiteralPath $EnvFile).Path
$values = Read-LocalPostgresEnvironment -Path $EnvFile
if ($values['POSTGRES_DB'] -ceq 'jp_dict_demo') {
    throw 'The original .env database must differ from jp_dict_demo. No database was changed.'
}
$corpusPath = Join-Path $projectRoot '.sdd/specs/feat-conjugation-explanation/corpus/expanded-20261013.3/cases.json'
if ((Get-FileHash -LiteralPath $corpusPath -Algorithm SHA256).Hash.ToLowerInvariant() -cne
    'ca3e3ff71b4d97a96e33c70695d5cfb067244bb6cf63a7d3340cd901dd7883c4') {
    throw 'The frozen demo corpus does not match its approved hash. No database was changed.'
}
if ($ValidateOnly) {
    Write-Output 'Demo configuration valid: separate local jp_dict_demo, frozen 17 fixtures. No services started or database changed.'
    return
}

$docker = Resolve-LocalRecoveryExecutable -Name docker
$dotnet = Resolve-LocalRecoveryExecutable -Name dotnet
if (-not $SkipBuild) {
    Invoke-LocalRecoveryProcess -FileName $dotnet -Arguments @('build', 'src/interface/WebApi.csproj', '-c', 'Release', '--no-restore') -WorkingDirectory $projectRoot
}
# Child-only Compose environment; .env and caller connection overrides remain unchanged.
Invoke-LocalRecoveryProcess -FileName $docker -Arguments @('compose', '--project-directory', $projectRoot, '--env-file', $EnvFile, '--profile', 'nlp', 'up', '-d', '--no-build', '--pull', 'never', '--wait', 'db', 'tokenizer') -WorkingDirectory $projectRoot -Environment $values

# Fixed SQL identifier only; credentials stay in the container environment, never command arguments/output.
$existsSql = "SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname='jp_dict_demo');"
$psql = @('compose', '--project-directory', $projectRoot, '--env-file', $EnvFile, 'exec', '-T', 'db', 'sh', '-c', 'exec psql -X -v ON_ERROR_STOP=1 -At -U "$POSTGRES_USER" -d "$POSTGRES_DB"')
$exists = (Invoke-LocalRecoveryProcess -FileName $docker -Arguments $psql -WorkingDirectory $projectRoot -Environment $values -InputText $existsSql -ReturnOutput).Trim()
if ($exists -ceq 'f') {
    Invoke-LocalRecoveryProcess -FileName $docker -Arguments $psql -WorkingDirectory $projectRoot -Environment $values -InputText 'CREATE DATABASE jp_dict_demo;'
} elseif ($exists -cne 't') { throw 'Could not verify isolated demo database existence.' }

$demoValues = $values.Clone()
$demoValues['POSTGRES_DB'] = 'jp_dict_demo'
$seedEnvironment = @{
    ConnectionStrings__DefaultConnection = New-LocalBackendConnectionString -Values $demoValues
    LocalDemoConfirmed = '1'
    DemoCorpusPath = $corpusPath
}
Invoke-LocalRecoveryProcess -FileName $dotnet -Arguments @('src/interface/bin/Release/net10.0/WebApi.dll', '--seed-demo-dictionary') -WorkingDirectory $projectRoot -Environment $seedEnvironment
Write-Output 'Demo jp_dict_demo ready: 17 synthetic dictionary fixtures. Original dictionary data is untouched; no real dictionary was imported.'
