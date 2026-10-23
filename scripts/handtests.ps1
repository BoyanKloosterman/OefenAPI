# Handmatige tests voor leerdoel 4, via de ZAP-proxy.
# Volgorde: start de app opnieuw (dan is de database schoon), laat ZAP
# met dezelfde sessie aan staan, en draai dan dit script.
# Elke response wordt met headers getoond, zodat je X-Request-Id en
# Access-Control-Allow-Origin ziet. Het History-ID haal je uit ZAP.

$ErrorActionPreference = "Stop"

$proxy = "http://127.0.0.1:8080"
$base = "http://127.0.0.1:5080"
$testRun = "leerdoel-4-hand"

$tmp = Join-Path $env:TEMP "oefenapi-handtests"
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

function New-Body {
    param([string]$Name, [string]$Json)
    $path = Join-Path $tmp $Name
    # UTF-8 zonder BOM, anders leest de API de JSON niet goed
    [System.IO.File]::WriteAllText($path, $Json)
    return $path
}

function Send-Request {
    param(
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Url,
        [string]$BodyFile,
        [string]$Token,
        [string]$ExtraHeader
    )

    $curlArgs = @(
        "-sS", "-i",
        "-x", $proxy,
        "-X", $Method,
        "-H", "Accept: application/json",
        "-H", "X-Test-Run: $testRun"
    )
    if ($Token) { $curlArgs += @("-H", "Authorization: Bearer $Token") }
    if ($ExtraHeader) { $curlArgs += @("-H", $ExtraHeader) }
    if ($BodyFile) { $curlArgs += @("-H", "Content-Type: application/json", "--data-binary", "@$BodyFile") }

    Write-Host ""
    Write-Host "=== $Label  ($(Get-Date -Format 'HH:mm:ss'))" -ForegroundColor Cyan
    $output = & curl.exe @curlArgs $Url
    if ($LASTEXITCODE -ne 0) { throw "curl stopte met code $LASTEXITCODE voor $Method $Url" }
    $output | ForEach-Object { Write-Host $_ }
    return $output
}

$onbekend = New-Body "onbekend.json" '{"username":"bestaat-niet","password":"FoutWachtwoord1!"}'
$foutWw   = New-Body "fout-ww.json"  '{"username":"account-a","password":"FoutWachtwoord1!"}'
$loginA   = New-Body "login-a.json"  '{"username":"account-a","password":"OefenAccountA!"}'
$rol      = New-Body "rol.json"      '{"role":"admin"}'

# API2: verschillende foutmelding bij onbekende gebruiker en fout wachtwoord
Send-Request -Label "API2 onbekende gebruiker" -Method POST -Url "$base/api/login" -BodyFile $onbekend | Out-Null
Send-Request -Label "API2 fout wachtwoord account-a" -Method POST -Url "$base/api/login" -BodyFile $foutWw | Out-Null

# Inloggen als account-a
$loginOutput = Send-Request -Label "Login account-a" -Method POST -Url "$base/api/login" -BodyFile $loginA
$json = ($loginOutput | Where-Object { $_ -match '^\s*\{' }) -join ""
$token = ($json | ConvertFrom-Json).token
if (-not $token) { throw "Geen token gekregen. Staan de app en ZAP aan?" }

# API1: client 3 hoort bij account-b
Send-Request -Label "API1 GET client 3 als account-a" -Method GET -Url "$base/api/clients/3" -Token $token | Out-Null

# API3: role meesturen bij de user van account-b
Send-Request -Label "API3 PUT user 2 met role admin als account-a" -Method PUT -Url "$base/api/users/2" -BodyFile $rol -Token $token | Out-Null

# API4: pageSize zonder bovengrens
Send-Request -Label "API4 bulkdata pageSize 500" -Method GET -Url "$base/api/bulkdata?page=1&pageSize=500" -Token $token | Out-Null

# API8: CORS met een Origin-header
Send-Request -Label "API8 GET / met Origin" -Method GET -Url "$base/" -ExtraHeader "Origin: https://example.test" | Out-Null

# API5: admin-endpoint zonder admin-rol. Als laatste, want account-b is daarna weg.
Send-Request -Label "API5 DELETE user 2 als account-a" -Method DELETE -Url "$base/api/admin/users/2" -Token $token | Out-Null

Write-Host ""
Write-Host "Klaar. Zoek de requests in de ZAP-History op X-Test-Run: $testRun en noteer de ID's in docs/testlog.md." -ForegroundColor Green
