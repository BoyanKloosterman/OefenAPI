# Gewone voorbeeldrequests via de ZAP-proxy.
# ZAP moet luisteren op 127.0.0.1:8080. De API op 127.0.0.1:5080.

$ErrorActionPreference = "Stop"

$proxy = "http://127.0.0.1:8080"
$base = "http://127.0.0.1:5080"
$testRun = "voorbeeld-script"

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Url,
        [string]$BodyFile,
        [string]$Token
    )

    $curlArgs = @(
        "-sS",
        "-x", $proxy,
        "-X", $Method,
        "-H", "Accept: application/json",
        "-H", "X-Test-Run: $testRun"
    )

    if ($Token) {
        $curlArgs += @("-H", "Authorization: Bearer $Token")
    }

    if ($BodyFile) {
        $curlArgs += @("-H", "Content-Type: application/json", "--data-binary", "@$BodyFile")
    }

    $output = & curl.exe @curlArgs $Url
    if ($LASTEXITCODE -ne 0) {
        throw "curl stopte met code $LASTEXITCODE voor $Method $Url"
    }

    return $output
}

$loginFile = New-TemporaryFile
$updateFile = New-TemporaryFile

try {
    Set-Content -Path $loginFile -Value '{"username":"account-a","password":"OefenAccountA!"}' -Encoding ascii -NoNewline
    Set-Content -Path $updateFile -Value '{"displayName":"Account A","email":"account-a@example.test"}' -Encoding ascii -NoNewline

    $loginRaw = Invoke-Api -Method POST -Url "$base/api/login" -BodyFile $loginFile.FullName
    Write-Output $loginRaw

    $login = $loginRaw | ConvertFrom-Json
    if (-not $login.token) {
        throw "Geen token in het login-antwoord."
    }

    $token = [string]$login.token
    Write-Output (Invoke-Api -Method GET -Url "$base/api/clients/1" -Token $token)
    Write-Output (Invoke-Api -Method GET -Url "$base/api/clients?search=Kaas" -Token $token)
    Write-Output (Invoke-Api -Method GET -Url "$base/api/users/1" -Token $token)
    Write-Output (Invoke-Api -Method PUT -Url "$base/api/users/1" -BodyFile $updateFile.FullName -Token $token)
    Write-Output (Invoke-Api -Method GET -Url "$base/api/bulkdata?page=1&pageSize=20" -Token $token)
}
finally {
    Remove-Item -LiteralPath $loginFile.FullName, $updateFile.FullName -ErrorAction SilentlyContinue
}
