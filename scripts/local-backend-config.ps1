# Literal dotenv subset only. Never dot-source/evaluate a dotenv file.
function Read-LocalPostgresEnvironment {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw 'Missing local .env file.' }
    $values = @{}
    $keys = @('POSTGRES_DB', 'POSTGRES_USER', 'POSTGRES_PASSWORD', 'POSTGRES_PORT')
    foreach ($line in [System.IO.File]::ReadAllLines($Path)) {
        if ($line -notmatch '^\s*(?:export\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*=(.*)$') { continue }
        $key = $Matches[1]
        if ($keys -cnotcontains $key) { continue }
        if ($values.ContainsKey($key)) { throw "Duplicate .env key: $key" }
        $value = $Matches[2].Trim()
        if ($value.StartsWith("'") -or $value.StartsWith('"')) {
            $quote = $value.Substring(0, 1)
            if ($value.Length -lt 2 -or -not $value.EndsWith($quote)) { throw "Invalid quoted .env value: $key" }
            $value = $value.Substring(1, $value.Length - 2)
            if ($quote -eq '"' -and ($value -match '\\' -or $value -match '\$(?:\{|[A-Za-z_])')) {
                throw "Use a single-quoted literal value without interpolation for $key."
            }
        } else {
            $value = ($value -replace '\s+#.*$', '').Trim()
            if ($value -match '\$(?:\{|[A-Za-z_])') { throw "Interpolation is not supported for $key." }
        }
        $values[$key] = $value
    }
    foreach ($key in $keys) {
        if (-not $values.ContainsKey($key) -or [string]::IsNullOrWhiteSpace($values[$key])) { throw "Missing .env key: $key" }
    }
    $port = 0
    if (-not [int]::TryParse($values['POSTGRES_PORT'], [ref]$port) -or $port -lt 1 -or $port -gt 65535) {
        throw 'POSTGRES_PORT must be between 1 and 65535.'
    }
    return $values
}

function New-LocalBackendConnectionString {
    param([Parameter(Mandatory = $true)][hashtable]$Values)
    # Standard builder quotes semicolons/quotes safely; do not interpolate credentials into SQL or commands.
    $builder = New-Object System.Data.Common.DbConnectionStringBuilder
    $builder['Host'] = '127.0.0.1'
    $builder['Port'] = [int]$Values['POSTGRES_PORT']
    $builder['Database'] = $Values['POSTGRES_DB']
    $builder['Username'] = $Values['POSTGRES_USER']
    $builder['Password'] = $Values['POSTGRES_PASSWORD']
    return $builder.ConnectionString
}
