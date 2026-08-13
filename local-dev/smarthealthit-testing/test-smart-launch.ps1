<#
.SYNOPSIS
    Kjorer HELE SMART EHR Launch-kjeden (launch -> authorize -> callback) mot
    launch.smarthealthit.org med curl, uten a apne en nettleser.

.DESCRIPTION
    Se docs/TESTGUIDE-SMARTHEALTHIT.md for full forklaring av bakgrunnen.
    Kort fortalt: nettleserverktoyet Claude Code bruker er sandboxet mot
    `local.altinn.cloud` (behandles som internt nettverk), og
    launch.smarthealthit.org sin innebygde pasient-/behandlervelger krever
    interaktiv nettleser hvis ikke pasient/behandler er forhandsvalgt. Dette
    skriptet unngar begge problemene ved a:
      1) forhandsvelge en pasient- og behandler-ID i launch-payloaden (skipper
         den interaktive velgeren helt), og
      2) bruke curl.exe direkte (fungerer fra bade PowerShell og en vanlig
         terminal) i stedet for en ekte nettleser.

    Krever at det lokale miljoet allerede kjorer (se docs/HELHETLIG-FLYT.md):
    Altinn localtest-containerne (podman/docker) og appen selv (`dotnet run`
    i src/App) pa localhost:5005, tilgjengelig via loadbalanceren pa
    http://local.altinn.cloud:8000.

.PARAMETER ClientType
    Hvilken klientautentisering appen skal konfigureres for og launcheren
    skal simulere:
      - "public"          : ingen client-autentisering (SmartOnFhir:ClientSecret
                             og :ClientAssertionPrivateKeyPath begge tomme)
      - "secret"           : Confidential Symmetric / client_secret (Basic-auth).
                             Krever at SmartOnFhir:ClientSecret er satt (hvilken
                             som helst verdi — launcheren star i Loose-modus).
      - "private_key_jwt"  : Confidential Asymmetric / private_key_jwt.
                             Krever at SmartOnFhir:ClientAssertionPrivateKeyPath
                             peker pa en gyldig JWK-fil (se
                             generate-client-assertion-jwk.ps1).

.PARAMETER SimulatedError
    Valgfri verdi fra launch.smarthealthit.org sin "Simulated Error"-meny,
    f.eks. "auth_invalid_client_secret" eller "token_invalid_scope". Se
    docs/TESTGUIDE-SMARTHEALTHIT.md for full liste og hvilke som faktisk kan
    trigges uten interaktiv pasientvelger.

.PARAMETER PatientId
    FHIR Patient-ID pa launch.smarthealthit.org sin R4-server. Standard:
    henter den forste pasienten fra serveren automatisk.

.PARAMETER ProviderId
    FHIR Practitioner-ID som simulerer legen som apner appen. Standard:
    "4832580" (en av launcherens faste "Dr. FLEX Test"-identiteter).

.PARAMETER AppBaseUrl
    Base-URL til Altinn-appen slik den nas fra utsiden (loadbalanceren), ikke
    appens egen port. Standard: "http://local.altinn.cloud:8000".

.EXAMPLE
    .\test-smart-launch.ps1 -ClientType public
    Kjorer happy-path-testen (samme som den opprinnelige ende-til-ende-
    verifiseringen mot en public client).

.EXAMPLE
    .\test-smart-launch.ps1 -ClientType secret
    Verifiserer at client_secret/Basic-auth-utvekslingen fungerer. Krever at
    du forst har kjort:
      dotnet user-secrets set "SmartOnFhir:ClientSecret" "en-hvilken-som-helst-verdi"
    i src/App, og at appen er restartet.

.EXAMPLE
    .\test-smart-launch.ps1 -ClientType private_key_jwt
    Verifiserer private_key_jwt. Krever at du forst har kjort
    generate-client-assertion-jwk.ps1 og satt
    SmartOnFhir:ClientAssertionPrivateKeyPath, og at appen er restartet.

.EXAMPLE
    .\test-smart-launch.ps1 -ClientType public -SimulatedError token_invalid_token
    Simulerer at EPJ-ens token-endepunkt returnerer et ugyldig token.
    Forventet resultat: appen svarer 502 (ikke en ubehandlet exception),
    se docs/IMPLEMENTERING.md §13.
#>
param(
    [ValidateSet("public", "secret", "private_key_jwt")]
    [string]$ClientType = "public",

    [string]$SimulatedError = "",

    [string]$PatientId,

    [string]$ProviderId = "4832580",

    [string]$AppBaseUrl = "http://local.altinn.cloud:8000",

    [string]$Org = "digdir",

    [string]$App = "forer-legeerklaering"
)

$ErrorActionPreference = "Stop"

function ConvertTo-Base64Url([byte[]]$bytes) {
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

# --- 0. Forhandsvelg en pasient hvis ingen er oppgitt ---------------------
if (-not $PatientId) {
    Write-Host "Henter en pasient-ID fra launch.smarthealthit.org sin FHIR-server..."
    $patientBundle = Invoke-RestMethod -Uri "https://launch.smarthealthit.org/v/r4/fhir/Patient?_count=1"
    $PatientId = $patientBundle.entry[0].resource.id
    Write-Host "Bruker pasient: $PatientId ($($patientBundle.entry[0].resource.name[0].given -join ' ') $($patientBundle.entry[0].resource.name[0].family))"
}

# --- 1. Bygg launch=-payloaden ---------------------------------------------
# Skjema reverse-engineert fra launch.smarthealthit.org sin UI (se
# docs/TESTGUIDE-SMARTHEALTHIT.md for full indekstabell):
#   [0]  launch_type (0 = provider-ehr)
#   [1]  patient-ID
#   [2]  provider-ID
#   [3]  encounter-modus ("AUTO"/"MANUAL")
#   [4-6] ints, ikke i bruk her (encounter/npi-relatert)
#   [7-10] tomme strenger
#   [11] simulert feil (Simulated Error-verdien, eller tom streng)
#   [12-13] tomme strenger
#   [14] client_type (0 = Public, 1 = Confidential Symmetric, 2 = Confidential Asymmetric)
#   [15] validation_mode (1 = Loose — dette skriptet tester ALDRI Strict-modus,
#        se "Ikke i scope" i docs/TESTGUIDE-SMARTHEALTHIT.md)
#   [16] tom streng
$clientTypeIndex = switch ($ClientType) {
    "public"          { 0 }
    "secret"          { 1 }
    "private_key_jwt" { 2 }
}

$launchArray = @(
    0, $PatientId, $ProviderId, "AUTO", 0, 0, 0, "", "", "", "",
    $SimulatedError, "", "", $clientTypeIndex, 1, ""
)
$launchJson = "[" + (($launchArray | ForEach-Object {
    if ($_ -is [string]) { '"' + $_.Replace('"', '\"') + '"' } else { $_ }
}) -join ",") + "]"
$launchB64 = ConvertTo-Base64Url ([System.Text.Encoding]::UTF8.GetBytes($launchJson))

$iss = "https://launch.smarthealthit.org/v/r4/fhir"
$issEncoded = [System.Uri]::EscapeDataString($iss)
$launchUrl = "$AppBaseUrl/$Org/$App/smart/launch?iss=$issEncoded&launch=$launchB64"

Write-Host ""
Write-Host "Client-type:      $ClientType"
Write-Host "Simulert feil:    $(if ($SimulatedError) { $SimulatedError } else { '(ingen)' })"
Write-Host "Launch-payload:   $launchJson"
Write-Host "Launch-URL:       $launchUrl"
Write-Host ""

$workDir = Join-Path $env:TEMP "smart-launch-test-$(Get-Date -Format 'yyyyMMddHHmmss')"
New-Item -ItemType Directory -Force -Path $workDir | Out-Null
$appCookies = Join-Path $workDir "app-cookies.txt"
$extCookies = Join-Path $workDir "ext-cookies.txt"

# --- 2. Steg 1: treff appens /smart/launch ---------------------------------
$h1 = Join-Path $workDir "h1.txt"
curl.exe -s -c $appCookies -D $h1 -o "$workDir\b1.html" $launchUrl -w "STEG 1 (launch):    %{http_code}`n"
$loc1 = (Select-String -Path $h1 -Pattern "^Location:\s*(.+)$").Matches | ForEach-Object { $_.Groups[1].Value.Trim() } | Select-Object -First 1

if (-not $loc1) {
    Write-Host ""
    Write-Host "Ingen redirect fra /smart/launch — sjekk om appen kjorer (http://localhost:5005) og om loadbalanceren svarer pa $AppBaseUrl."
    Write-Host "Respons-headere:"
    Get-Content $h1
    exit 1
}

# --- 3. Steg 2: folg til launch.smarthealthit.org sin authorize-endepunkt --
$h2 = Join-Path $workDir "h2.txt"
curl.exe -s -c $extCookies -D $h2 -o "$workDir\b2.html" $loc1 -w "STEG 2 (authorize): %{http_code}`n"
$loc2 = (Select-String -Path $h2 -Pattern "^Location:\s*(.+)$").Matches | ForEach-Object { $_.Groups[1].Value.Trim() } | Select-Object -First 1

if (-not $loc2) {
    Write-Host ""
    Write-Host "Ingen redirect fra authorize-endepunktet. Dette skjer typisk hvis pasient- eller"
    Write-Host "provider-ID mangler/er tvetydig og launcheren viser en interaktiv velger i stedet —"
    Write-Host "sjekk at -PatientId og -ProviderId peker pa gyldige, entydige ID-er."
    exit 1
}

if ($loc2 -notmatch "/smart/callback") {
    Write-Host ""
    Write-Host "Uventet omdirigering (ikke tilbake til appens callback):"
    Write-Host $loc2
    exit 1
}

# --- 4. Steg 3: treff appens /smart/callback med koden ---------------------
$h3 = Join-Path $workDir "h3.txt"
curl.exe -s -c $appCookies -b $appCookies -D $h3 -o "$workDir\b3.html" $loc2 -w "STEG 3 (callback):  %{http_code}`n"

Write-Host ""
$hasAltinnSession = Select-String -Path $h3 -Pattern "^Set-Cookie:\s*AltinnStudioRuntime="
if ($hasAltinnSession) {
    Write-Host "RESULTAT: Suksess — AltinnStudioRuntime-cookie satt, token-exchange lyktes." -ForegroundColor Green
} else {
    Write-Host "RESULTAT: Ingen AltinnStudioRuntime-cookie funnet. Se responsen under:" -ForegroundColor Yellow
    Get-Content $h3
    Get-Content "$workDir\b3.html" -ErrorAction SilentlyContinue
}
Write-Host ""
Write-Host "Fullstendige mellomresultater ligger i: $workDir"
