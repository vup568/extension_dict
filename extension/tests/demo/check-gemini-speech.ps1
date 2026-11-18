#requires -Version 5.1
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$speechKey = $env:Speech__Gemini__ApiKey
if ([string]::IsNullOrWhiteSpace($speechKey)) {
    $secureKey = Read-Host 'Nhập Gemini API key để thử một từ mẫu' -AsSecureString
    $speechKey = [System.Net.NetworkCredential]::new('', $secureKey).Password
}
if ([string]::IsNullOrWhiteSpace($speechKey)) { throw 'Chưa nhập Gemini API key.' }

# Match the current provider request and use a synthetic word only.
$speechRequest = @{
    model = 'gemini-3.8-flash-lite-tts'
    store = $false
    input = @(@{ type = 'user_input'; content = @(@{ type = 'text'; text = 'ねこ' }) })
    response_format = @{ type = 'audio'; mime_type = 'audio/wav'; sample_rate = 24000 }
    generation_config = @{ speech_config = @(@{ voice = 'Kore' }) }
} | ConvertTo-Json -Depth 8 -Compress

# HttpClient preserves error bodies in both Windows PowerShell and PowerShell 7.
Add-Type -AssemblyName System.Net.Http
$speechHandler = [System.Net.Http.HttpClientHandler]::new()
$speechHandler.AllowAutoRedirect = $false
$speechClient = [System.Net.Http.HttpClient]::new($speechHandler)
$speechClient.Timeout = [TimeSpan]::FromSeconds(35)
$speechClient.MaxResponseContentBufferSize = 5MB
$speechContent = [System.Net.Http.StringContent]::new($speechRequest, [System.Text.Encoding]::UTF8, 'application/json')
$speechResponse = $null
try {
    if (-not $speechClient.DefaultRequestHeaders.TryAddWithoutValidation('x-goog-api-key', $speechKey)) {
        'API_KEY_HEADER_INVALID'
        return
    }
    $speechResponse = $speechClient.PostAsync('https://generativelanguage.googleapis.com/v1beta/interactions', $speechContent).GetAwaiter().GetResult()
    $speechStatus = [int]$speechResponse.StatusCode
    $speechBody = $speechResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()
} catch {
    'GEMINI_CONNECTION_FAILED'
    return
} finally {
    if ($null -ne $speechResponse) { $speechResponse.Dispose() }
    $speechContent.Dispose()
    $speechClient.Dispose()
}

try {
    $speechJson = ConvertFrom-Json -InputObject $speechBody
} catch {
    "Gemini HTTP $speechStatus | UNEXPECTED_RESPONSE"
    return
}
if ($speechJson -isnot [pscustomobject]) {
    "Gemini HTTP $speechStatus | UNEXPECTED_RESPONSE"
    return
}

if ($speechStatus -lt 200 -or $speechStatus -ge 300) {
    $speechError = $speechJson.error
    if ($speechError -isnot [pscustomobject]) {
        "Gemini HTTP $speechStatus"
        return
    }
    $knownReasons = 'API_KEY_INVALID', 'API_KEY_SERVICE_BLOCKED', 'API_KEY_HTTP_REFERRER_BLOCKED', `
        'API_KEY_IP_ADDRESS_BLOCKED', 'SERVICE_DISABLED', 'BILLING_DISABLED', 'UNAUTHENTICATED', `
        'PERMISSION_DENIED', 'INVALID_ARGUMENT', 'FAILED_PRECONDITION', 'NOT_FOUND', 'RESOURCE_EXHAUSTED', `
        'authentication', 'permission_denied', 'model_not_found', 'invalid_request', 'rate_limit_exceeded'
    $reason = @(
        $speechError.details | ForEach-Object { $_.reason }
        $speechError.status
        $speechError.type
        $speechError.code
    ) | Where-Object { $_ -in $knownReasons } | Select-Object -First 1
    if ($reason) { "Gemini HTTP $speechStatus | $reason" }
    else { "Gemini HTTP $speechStatus" }
    if ($speechError.message -is [string]) {
        $detail = $speechError.message.Replace($speechKey, '[đã ẩn khóa]')
        $detail = $detail -replace '(?i)(?:AIza[A-Za-z0-9_-]{20,}|AQ\.[A-Za-z0-9_-]{15,}|Bearer\s+\S+)', '[đã ẩn khóa]'
        $detail = ($detail -replace '[\p{C}\s]+', ' ').Trim()
        if ($detail.Length -gt 360) { $detail = $detail.Substring(0, 360) + '…' }
        if ($detail) { "Chi tiết: $detail" }
    }
    return
}

$audio = @($speechJson.steps | Where-Object { $_.type -eq 'model_output' } |
    ForEach-Object { $_.content } | Where-Object { $_.type -eq 'audio' -and $_.data })
if ($speechJson.status -eq 'completed' -and $audio.Count -eq 1 -and $audio[0].mime_type -eq 'audio/wav') {
    "Gemini HTTP $speechStatus | AUDIO_WAV_RECEIVED"
} else {
    "Gemini HTTP $speechStatus | UNEXPECTED_AUDIO_RESPONSE"
}
