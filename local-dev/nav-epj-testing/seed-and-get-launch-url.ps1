<#
.SYNOPSIS
    Oppretter en testpasient + konsultasjon + diagnose i en lokalt kjorende nav-epj-instans,
    og skriver ut en ferdig launch-URL som kan limes inn i en nettleser.

.DESCRIPTION
    Se docs/NAV-EPJ-TESTMILJO.md for full forklaring. Dette skriptet gjor de samme HTTP-kallene
    som en lege ville trigget via nav-epj sitt eget frontend (som ikke er bygget/kjort i dette
    oppsettet - se testguiden for hvorfor det ikke er nodvendig).

    MA kjores pa nytt etter HVER restart av nav-epj-backenden, fordi Flyway kjorer "clean" ved
    hver oppstart i dev-modus (se testguiden) og Valkey (som holder "aktiv pasient for HPR")
    ikke er persistent over en container-restart.

.PARAMETER NavEpjBaseUrl
    Base-URL til den lokalt kjorende nav-epj-instansen. Standard: http://localhost:8090
    (se application-local.yaml i nav-epj-klonen - IKKE 8080, som allerede er i bruk av var
    egen HAPI FHIR-mock).

.PARAMETER OurAppLaunchUrl
    Full URL til var apps /smart/launch-endepunkt, slik den ma vaere registrert i nav-epj sin
    smart.clients-liste (application-local.yaml).

.PARAMETER PatientNavn
    Navn pa testpasienten som opprettes.

.PARAMETER PatientFnr
    Fodselsnummer for testpasienten. Standard er et Tenor-verifisert (MOD11-gyldig) syntetisk
    fodselsnummer som allerede brukes i local-dev/seed.ps1 for dette prosjektet - ikke en reell
    person.

.EXAMPLE
    .\seed-and-get-launch-url.ps1
    Oppretter testdata og skriver ut launch-URL-en. Kopier den inn i en nettleser for a se hele
    SMART-launch-flyten kjore i praksis, inkludert redirect gjennom var Altinn-app.
#>
param(
    [string]$NavEpjBaseUrl = "http://localhost:8090",
    [string]$OurAppLaunchUrl = "http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/launch",
    [string]$PatientNavn = "Test Testesen",
    [string]$PatientFnr = "21814497167"
)

$ErrorActionPreference = "Stop"

# Funn (se docs/NAV-EPJ-TESTMILJO.md, bug #6): PasientRepository.insert() bruker insertIgnore
# for selve pasient-raden (for a vaere idempotent pa fnr), men innsetter deretter ubetinget en
# rad i pasient_helsepersonell knyttet til den NYGENERERTE (og i konflikt-tilfellet aldri
# faktisk lagrede) pasient-ID-en -> 500 Internal Server Error (fremmednokkel-brudd) hver gang
# man kaller POST /api/patient to ganger med samme fnr innenfor samme nav-epj-prosesslevetid.
# Unngar bugen her ved a sjekke om pasienten allerede finnes forst, i stedet for a stole pa at
# APIet selv er idempotent.
Write-Host "Sjekker om testpasienten allerede finnes..."
$eksisterende = Invoke-RestMethod -Method Get -Uri "$NavEpjBaseUrl/api/patient"
$pasient = $eksisterende | Where-Object { $_.fnr -eq $PatientFnr } | Select-Object -First 1

if ($pasient) {
    Write-Host "  Fant eksisterende pasient, gjenbruker: $($pasient.id)"
} else {
    Write-Host "Oppretter testpasient..."
    $pasient = Invoke-RestMethod -Method Post -Uri "$NavEpjBaseUrl/api/patient" `
        -ContentType "application/json" `
        -Body (@{ navn = $PatientNavn; fnr = $PatientFnr } | ConvertTo-Json)
    Write-Host "  Pasient-ID: $($pasient.id)"
}

Write-Host "Oppretter/henter aktiv konsultasjon..."
$konsultasjon = Invoke-RestMethod -Method Post -Uri "$NavEpjBaseUrl/api/patients/$($pasient.id)/konsultasjoner" `
    -ContentType "application/json" -Body "{}"
Write-Host "  Konsultasjon-ID: $($konsultasjon.id)"

Write-Host "Legger til en diagnose (kreves for at Encounter-bygging i nav-epj ikke skal krasje)..."
$oppdater = @{
    konsultasjonId = $konsultasjon.id
    diagnoser      = @(@{ kode = "A97"; system = "ICPC2"; beskrivelse = "Ingen sykdom" })
    journalNotat   = "Rutinekontroll, ingen funn (testdata)."
    ferdigstill    = $false
} | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Patch -Uri "$NavEpjBaseUrl/api/patients/$($pasient.id)/konsultasjoner" `
    -ContentType "application/json" -Body $oppdater | Out-Null
Write-Host "  OK"

$encodedAppUrl = [System.Uri]::EscapeDataString($OurAppLaunchUrl)
$launchUrl = "$NavEpjBaseUrl/fhir/launch?url=$encodedAppUrl"

Write-Host ""
Write-Host "Testdata klar. Lim denne URL-en inn i en nettleser for a starte SMART-launchen:"
Write-Host ""
Write-Host "  $launchUrl" -ForegroundColor Green
Write-Host ""
Write-Host "Forventet resultat: du blir omdirigert flere ganger (nav-epj -> var app -> nav-epj -> var app)"
Write-Host "og ender opp inne i forer-legeerklaering-appen med en aktiv Altinn-sesjon."
