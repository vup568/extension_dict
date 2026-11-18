# One-time opt-in recovery. Default: backup and rehearse on a clone only.
[CmdletBinding()]
param(
    [string]$EnvFile = (Join-Path (Split-Path -Parent $PSScriptRoot) '.env'),
    [switch]$Apply
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Database recovery requires PowerShell 7 or newer. Normal startup still supports Windows PowerShell 5.' }
. (Join-Path $PSScriptRoot 'local-backend-config.ps1')
. (Join-Path $PSScriptRoot 'local-recovery-tools.ps1')
$projectRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$EnvFile = (Resolve-Path -LiteralPath $EnvFile).Path
$values = Read-LocalPostgresEnvironment -Path $EnvFile
$connection = New-LocalBackendConnectionString -Values $values
if ($values['POSTGRES_DB'] -cin @('postgres', 'template0', 'template1')) { throw 'Recovery refuses PostgreSQL system databases.' }
$override = [Environment]::GetEnvironmentVariable('ConnectionStrings__DefaultConnection', 'Process')
if (-not [string]::IsNullOrWhiteSpace($override) -and $override -cne $connection) {
    throw 'Recovery only targets the local Compose database. Clear the connection override before continuing.'
}
$backendDll = Join-Path $projectRoot 'src/interface/bin/Release/net10.0/WebApi.dll'
if (-not (Test-Path -LiteralPath $backendDll -PathType Leaf)) { throw 'Build the Release backend with existing restored dependencies before recovery.' }
$docker = Resolve-LocalRecoveryExecutable -Name docker
$dotnet = Resolve-LocalRecoveryExecutable -Name dotnet
# Child-only environment: the caller process/environment and .env are never modified.
$childEnvironment = @{}
foreach ($key in $values.Keys) { $childEnvironment[$key] = $values[$key] }
$childEnvironment['PGPASSWORD'] = $values['POSTGRES_PASSWORD']
$childEnvironment['JP_READING_POSTGRES_CONNECTION_STRING'] = $connection
$compose = @('compose', '--project-directory', $projectRoot, '--env-file', $EnvFile, 'exec', '-T', '-e', 'PGPASSWORD', 'db')
$initialMigration = '20260821075041_InitialCreate'
$latestMigration = '20261014022755_AddConjugationCandidate'
$oldTables = @('dictionary_entries', 'dictionary_senses', 'editorial_mappings', 'grammar_rules', 'kanji_records',
    'knowledge_release_manifests', 'knowledge_releases', 'learning_references', 'localized_glosses', 'readings',
    'resource_revisions', 'sense_applicabilities', 'source_manifests', 'source_records', 'user_external_logins', 'users', 'written_forms')

function Invoke-RecoverySql {
    param([Parameter(Mandatory)][string]$Database, [string]$Sql, [string]$SqlPath, [switch]$HashOutput)
    $arguments = $compose + @('psql', '--no-psqlrc', '--username', $values['POSTGRES_USER'], '--dbname', $Database,
        '--set', 'ON_ERROR_STOP=1', '--tuples-only', '--no-align', '--quiet')
    Invoke-LocalRecoveryProcess -FileName $docker -Arguments $arguments -WorkingDirectory $projectRoot -Environment $childEnvironment `
        -InputText $Sql -InputPath $SqlPath -HashOutput:$HashOutput -ReturnOutput:(-not $HashOutput)
}

function Get-RecoveryRowHash {
    param([Parameter(Mandatory)][string]$Database, [Parameter(Mandatory)][string]$Table, [switch]$IgnoreCurrentFlag)
    if ($oldTables -cnotcontains $Table) { throw 'Unexpected recovery table.' }
    $projection = 'to_jsonb(t)'
    if ($IgnoreCurrentFlag -and $Table -ceq 'knowledge_releases') { $projection = "to_jsonb(t) - 'is_current'" }
    $ordering = Get-LocalRecoveryOrderBy -Table $Table
    Invoke-RecoverySql -Database $Database -Sql "BEGIN TRANSACTION READ ONLY; SELECT ($projection)::text FROM public.`"$Table`" AS t ORDER BY $ordering; COMMIT;" -HashOutput
}

function Get-RecoverySnapshot {
    param([Parameter(Mandatory)][string]$Database, [switch]$IgnoreCurrentFlag)
    $countQueries = foreach ($table in $oldTables) { "SELECT '$table', count(*) FROM public.`"$table`"" }
    $countText = Invoke-RecoverySql -Database $Database -Sql ('BEGIN TRANSACTION READ ONLY; ' + ($countQueries -join ' UNION ALL ') + '; COMMIT;')
    $snapshot = @{}
    foreach ($line in $countText.Trim().Split("`n")) {
        $pieces = $line.Trim().Split('|')
        if ($pieces.Length -ne 2 -or $oldTables -cnotcontains $pieces[0] -or $pieces[1] -notmatch '^\d+$' -or $snapshot.ContainsKey($pieces[0])) {
            throw 'Unexpected recovery fingerprint metadata.'
        }
        $snapshot[$pieces[0]] = @{ Count = [long]$pieces[1]; Hash = (Get-RecoveryRowHash -Database $Database -Table $pieces[0] -IgnoreCurrentFlag:$IgnoreCurrentFlag) }
    }
    if ($snapshot.Count -ne $oldTables.Count) { throw 'Recovery requires all initial-schema tables.' }
    return $snapshot
}

function Invoke-RecoveryBaseline {
    param([Parameter(Mandatory)][string]$Database, [Parameter(Mandatory)][string]$ReferenceConnection)
    $targetValues = $values.Clone(); $targetValues['POSTGRES_DB'] = $Database
    $baselineEnvironment = $childEnvironment.Clone()
    $baselineEnvironment['ConnectionStrings__DefaultConnection'] = New-LocalBackendConnectionString -Values $targetValues
    $baselineEnvironment['LegacySchemaReferenceConnection'] = $ReferenceConnection
    $baselineEnvironment['LocalDatabaseRecoveryConfirmed'] = '1'
    Invoke-LocalRecoveryProcess -FileName $dotnet -Arguments @($backendDll, '--recover-legacy-schema') `
        -WorkingDirectory $projectRoot -Environment $baselineEnvironment
}

function Assert-RecoveryMigrationsComplete {
    param([Parameter(Mandatory)][string]$Database)
    $sql = 'SELECT "MigrationId" FROM public."__EFMigrationsHistory" ORDER BY "MigrationId";'
    $expected = @($initialMigration, '20260821084102_EnforceKnowledgeReleaseImmutability', '20261013020721_AddConjugationDatasets', $latestMigration)
    $history = (Invoke-RecoverySql -Database $Database -Sql $sql).Trim() -split '\r?\n'
    if (($history -join '|') -cne ($expected -join '|')) { throw 'Recovery migration history verification failed.' }
    $datasetReady = (Invoke-RecoverySql -Database $Database -Sql "SELECT EXISTS (SELECT 1 FROM conjugation_datasets WHERE version = 'candidate-20261013.1');").Trim()
    if ($datasetReady -cne 't') { throw 'Recovery dataset verification failed.' }
}

function Assert-RecoverySourceIdle {
    $sql = "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND pid <> pg_backend_pid() AND backend_type = 'client backend';"
    $activeClients = (Invoke-RecoverySql -Database $values['POSTGRES_DB'] -Sql $sql).Trim()
    if ($activeClients -cne '0') { throw 'Stop the local backend and disconnect other clients before applying database recovery.' }
}

$stage = 'preflight'
try {
    $identifier = [Guid]::NewGuid().ToString('N')
    $cloneName = "jp_recovery_clone_$identifier"; $referenceName = "jp_recovery_reference_$identifier"
    Assert-LocalRecoveryDatabaseName -Name $cloneName; Assert-LocalRecoveryDatabaseName -Name $referenceName
    $recoveryRoot = [System.IO.Path]::GetFullPath((Join-Path $projectRoot 'data/local-recovery'))
    $workspacePrefix = $projectRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    if (-not $recoveryRoot.StartsWith($workspacePrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid recovery output directory.' }
    $runDirectory = Join-Path $recoveryRoot "recovery-20261014-$identifier"
    foreach ($directory in @((Join-Path $projectRoot 'data'), $recoveryRoot)) {
        if ((Test-Path -LiteralPath $directory) -and ((Get-Item -LiteralPath $directory -Force).Attributes -band [System.IO.FileAttributes]::ReparsePoint)) {
            throw 'Recovery output must not use a redirected/junction directory.'
        }
    }
    [System.IO.Directory]::CreateDirectory($runDirectory) | Out-Null
    $backupPath = Join-Path $runDirectory 'database.dump'
    $initialSql = Join-Path $runDirectory 'initial-schema.sql'; $pendingSql = Join-Path $runDirectory 'pending-migrations.sql'
    Write-Output 'Preparing a private backup and isolated recovery rehearsal. Stop the local backend before applying recovery.'
    Assert-RecoverySourceIdle
    $stage = 'backup and initial fingerprints'
    $sourceSnapshot = Get-RecoverySnapshot -Database $values['POSTGRES_DB']
    Invoke-LocalRecoveryProcess -FileName $docker -Arguments ($compose + @('pg_dump', '--username', $values['POSTGRES_USER'], '--dbname', $values['POSTGRES_DB'], '--format=custom', '--no-owner', '--no-privileges')) `
        -WorkingDirectory $projectRoot -Environment $childEnvironment -OutputPath $backupPath
    $backupHash = (Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash.ToLowerInvariant()
    foreach ($database in @($cloneName, $referenceName)) {
        Invoke-RecoverySql -Database $values['POSTGRES_DB'] -Sql "CREATE DATABASE `"$database`";" | Out-Null
    }
    $stage = 'backup restore into clone'
    Invoke-LocalRecoveryProcess -FileName $docker -Arguments ($compose + @('pg_restore', '--username', $values['POSTGRES_USER'], '--dbname', $cloneName, '--no-owner', '--no-privileges', '--exit-on-error')) `
        -WorkingDirectory $projectRoot -Environment $childEnvironment -InputPath $backupPath
    $cloneBefore = Get-RecoverySnapshot -Database $cloneName
    Assert-LocalRecoverySnapshotsEqual -Expected $sourceSnapshot -Actual $cloneBefore
    $stage = 'official migration SQL generation'
    $efArguments = @('ef', 'migrations', 'script')
    $efOptions = @('--project', (Join-Path $projectRoot 'src/infra/Infrastructure.csproj'), '--startup-project', (Join-Path $projectRoot 'src/infra/Infrastructure.csproj'), '--no-build', '--configuration', 'Release')
    Invoke-LocalRecoveryProcess -FileName $dotnet -Arguments ($efArguments + @('0', $initialMigration) + $efOptions + @('--output', $initialSql)) -WorkingDirectory $projectRoot -Environment $childEnvironment
    Invoke-LocalRecoveryProcess -FileName $dotnet -Arguments ($efArguments + @($initialMigration, $latestMigration) + $efOptions + @('--output', $pendingSql)) -WorkingDirectory $projectRoot -Environment $childEnvironment
    Invoke-RecoverySql -Database $referenceName -SqlPath $initialSql | Out-Null
    $referenceValues = $values.Clone(); $referenceValues['POSTGRES_DB'] = $referenceName
    $referenceConnection = New-LocalBackendConnectionString -Values $referenceValues
    $stage = 'clone schema baseline'
    Invoke-RecoveryBaseline -Database $cloneName -ReferenceConnection $referenceConnection
    Assert-LocalRecoverySnapshotsEqual -Expected $cloneBefore -Actual (Get-RecoverySnapshot -Database $cloneName)
    $pendingExpected = Get-RecoverySnapshot -Database $cloneName -IgnoreCurrentFlag
    $stage = 'clone pending migrations and fingerprints'
    Invoke-RecoverySql -Database $cloneName -SqlPath $pendingSql | Out-Null
    Assert-LocalRecoverySnapshotsEqual -Expected $pendingExpected -Actual (Get-RecoverySnapshot -Database $cloneName -IgnoreCurrentFlag)
    Assert-RecoveryMigrationsComplete -Database $cloneName
    if ($Apply) {
        $stage = 'original pre-apply checks'
        Assert-RecoverySourceIdle
        if ((Get-FileHash -LiteralPath $backupPath -Algorithm SHA256).Hash.ToLowerInvariant() -cne $backupHash) { throw 'The verified recovery backup changed; stop and inspect it.' }
        Assert-LocalRecoverySnapshotsEqual -Expected $sourceSnapshot -Actual (Get-RecoverySnapshot -Database $values['POSTGRES_DB'])
        $stage = 'original schema baseline'
        Invoke-RecoveryBaseline -Database $values['POSTGRES_DB'] -ReferenceConnection $referenceConnection
        Assert-LocalRecoverySnapshotsEqual -Expected $sourceSnapshot -Actual (Get-RecoverySnapshot -Database $values['POSTGRES_DB'])
        $originalPendingExpected = Get-RecoverySnapshot -Database $values['POSTGRES_DB'] -IgnoreCurrentFlag
        $stage = 'original pending migrations and fingerprints'
        Invoke-RecoverySql -Database $values['POSTGRES_DB'] -SqlPath $pendingSql | Out-Null
        Assert-LocalRecoverySnapshotsEqual -Expected $originalPendingExpected -Actual (Get-RecoverySnapshot -Database $values['POSTGRES_DB'] -IgnoreCurrentFlag)
        Assert-RecoveryMigrationsComplete -Database $values['POSTGRES_DB']
        Write-Output 'PASS: original local Compose database recovered; old-row fingerprints/counts retained across baseline and official migrations.'
    } else {
        Write-Output 'PASS: backup restored and recovery verified on the clone only. The original schema/history was not changed. Use -Apply only for authorized local repair.'
    }
    Write-Output "Private backup directory: $runDirectory"
    Write-Output "Backup SHA256: $backupHash"
    Write-Output "Retained rehearsal databases: $cloneName; $referenceName. No dictionary import or database deletion was performed."
} catch {
    if ($_.Exception -is [System.Management.Automation.ParameterBindingException]) {
        Write-Output ("Recovery parameter binding check: " + $_.Exception.ParameterName + ", source line " + $_.InvocationInfo.ScriptLineNumber + ".")
    }
    Write-Error "Database recovery stopped at $stage ($($_.Exception.GetType().Name)). Private process output is suppressed; any backup and isolated rehearsal databases are retained. Do not reset the original volume." -ErrorAction Continue
    exit 1
}
