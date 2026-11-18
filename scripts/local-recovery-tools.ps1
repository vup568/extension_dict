# Recovery helpers require PowerShell 7; no native stdout/stderr is echoed.
function Resolve-LocalRecoveryExecutable {
    param([Parameter(Mandatory)][ValidateSet('docker', 'dotnet')][string]$Name)
    # Docker Desktop ships both an extensionless binary and docker.exe. Windows uses only the latter.
    return (Get-Command ($Name + '.exe') -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
}

function Invoke-LocalRecoveryProcess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$FileName,
        [Parameter(Mandatory)][string[]]$Arguments,
        [Parameter(Mandatory)][string]$WorkingDirectory,
        [hashtable]$Environment = @{},
        [string]$InputText,
        [string]$InputPath,
        [string]$OutputPath,
        [switch]$HashOutput,
        [switch]$ReturnOutput
    )
    if ($PSVersionTable.PSVersion.Major -lt 7) { throw 'Database recovery requires PowerShell 7 or newer.' }
    if (($OutputPath -and $HashOutput) -or ($OutputPath -and $ReturnOutput) -or ($HashOutput -and $ReturnOutput)) {
        throw 'Choose only one recovery output mode.'
    }
    if ($InputPath -and $InputText) { throw 'Choose only one recovery input mode.' }
    $process = [System.Diagnostics.Process]::new()
    $destination = $null; $inputStream = $null; $algorithm = $null; $failureExitCode = 'unavailable'; $started = $false
    try {
        $info = [System.Diagnostics.ProcessStartInfo]::new()
        $info.FileName = $FileName; $info.WorkingDirectory = $WorkingDirectory
        $info.UseShellExecute = $false; $info.CreateNoWindow = $true
        $info.RedirectStandardInput = $true; $info.RedirectStandardOutput = $true; $info.RedirectStandardError = $true
        $info.StandardInputEncoding = [System.Text.UTF8Encoding]::new($false)
        foreach ($argument in $Arguments) { $info.ArgumentList.Add($argument) }
        foreach ($key in $Environment.Keys) { $info.Environment[$key] = [string]$Environment[$key] }
        $process.StartInfo = $info
        if (-not $process.Start()) { throw 'Recovery process could not start.' }
        $started = $true
        $errorRead = $process.StandardError.ReadToEndAsync()
        if ($OutputPath) {
            $destination = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::Write, [System.IO.FileShare]::None)
            $outputRead = $process.StandardOutput.BaseStream.CopyToAsync($destination)
        } elseif ($HashOutput) {
            $algorithm = [System.Security.Cryptography.SHA256]::Create()
            $destination = [System.Security.Cryptography.CryptoStream]::new([System.IO.Stream]::Null, $algorithm, [System.Security.Cryptography.CryptoStreamMode]::Write, $true)
            $outputRead = $process.StandardOutput.BaseStream.CopyToAsync($destination)
        } else {
            $outputRead = $process.StandardOutput.ReadToEndAsync()
        }
        if ($InputPath) {
            $inputStream = [System.IO.File]::OpenRead($InputPath)
            $inputStream.CopyToAsync($process.StandardInput.BaseStream).GetAwaiter().GetResult() | Out-Null
        } elseif ($InputText) {
            $process.StandardInput.WriteAsync($InputText).GetAwaiter().GetResult() | Out-Null
        }
        $process.StandardInput.Close()
        $process.WaitForExit()
        $failureExitCode = [string]$process.ExitCode
        $outputRead.GetAwaiter().GetResult() | Out-Null
        $errorRead.GetAwaiter().GetResult() | Out-Null
        if ($process.ExitCode -ne 0) { throw "Recovery child process failed (exit code $($process.ExitCode)). Private output was suppressed." }
        if ($HashOutput) {
            $destination.FlushFinalBlock()
            return [Convert]::ToHexString($algorithm.Hash).ToLowerInvariant()
        }
        if ($ReturnOutput) { return $outputRead.Result }
    } catch {
        # Provider/native messages can contain credentials or data; never include them.
        if ($started) {
            try { if (-not $process.HasExited) { $process.Kill($true); $process.WaitForExit(5000) | Out-Null } } catch { }
        }
        throw "Recovery process failed ($($_.Exception.GetType().Name), exit code $failureExitCode). No private process output is printed."
    } finally {
        if ($inputStream) { $inputStream.Dispose() }
        if ($destination) { $destination.Dispose() }
        if ($algorithm) { $algorithm.Dispose() }
        $process.Dispose()
    }
}

function Get-LocalRecoveryOrderBy {
    param([Parameter(Mandatory)][ValidatePattern('^[a-z_]+$')][string]$Table)
    if ($Table -ceq 'knowledge_release_manifests') { return 't.release_id, t.manifest_id' }
    return 't.id'
}

function Assert-LocalRecoveryDatabaseName {
    param([Parameter(Mandatory)][string]$Name)
    if ($Name -cnotmatch '^jp_recovery_(?:clone|reference)_[a-f0-9]{32}$') { throw 'Invalid isolated recovery database name.' }
}

function Assert-LocalRecoverySnapshotsEqual {
    param([Parameter(Mandatory)][hashtable]$Expected, [Parameter(Mandatory)][hashtable]$Actual)
    if ($Expected.Count -ne $Actual.Count) { throw 'Recovery data verification failed; source data was not changed by this check.' }
    foreach ($name in $Expected.Keys) {
        if (-not $Actual.ContainsKey($name) -or $Expected[$name].Count -ne $Actual[$name].Count -or
            $Expected[$name].Hash -cne $Actual[$name].Hash) { throw 'Recovery data verification failed; stop and inspect the retained backup/clone.' }
    }
}
