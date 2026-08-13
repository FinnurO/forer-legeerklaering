<#
.SYNOPSIS
    Genererer et RSA-nokkelpar for private_key_jwt-klientautentisering (RFC 7523) og
    eksporterer det som to separate JWK-JSON-filer.

.DESCRIPTION
    Brukes til a teste SmartOnFhir:ClientAssertionPrivateKeyPath (se
    docs/TESTGUIDE-SMARTHEALTHIT.md). Genererer et 2048-bit RSA-nokkelpar med
    .NET sin innebygde System.Security.Cryptography.RSA — ingen eksterne
    avhengigheter.

    Den PRIVATE JWK-en (inneholder d, p, q, dp, dq, qi) skrives KUN til en fil
    UTENFOR dette repoet. Den er hemmelig pa lik linje med et passord — den skal
    ALDRI limes inn i en chat-samtale, en commit, eller noe annet sted som
    havner i versjonskontroll.

    Den OFFENTLIGE JWK-en (kun kty/use/alg/kid/n/e) er trygg a dele — den skal
    limes inn i launch.smarthealthit.org sin "Client Registration & Validation"
    -fane nar Client Type = Confidential Asymmetric og Client Identity
    Validation = Strict (for faktisk signaturverifisering — se testguiden).

.PARAMETER PrivateKeyOutputPath
    Full sti til en fil UTENFOR repoet hvor den private JWK-en skal skrives.
    Standard: en .local-secrets-mappe i brukerens hjemmeomrade.

.PARAMETER PublicKeyOutputPath
    Full sti til hvor den offentlige JWK-en skal skrives. Standard: samme mappe
    som skriptet kjores fra.

.EXAMPLE
    .\generate-client-assertion-jwk.ps1
    Genererer nokkelpar med standardstier.

.EXAMPLE
    .\generate-client-assertion-jwk.ps1 -PrivateKeyOutputPath "D:\hemmelig\min-nokkel.json"
    Genererer nokkelpar og skriver den private delen til en egen-valgt, lokal sti.
#>
param(
    [string]$PrivateKeyOutputPath = (Join-Path $env:USERPROFILE ".local-secrets\forer-legeerklaering-smart-client-assertion-key.json"),
    [string]$PublicKeyOutputPath = (Join-Path $PSScriptRoot "smart-client-assertion-public-jwk.json")
)

function ConvertTo-Base64Url([byte[]]$bytes) {
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$rsa = [System.Security.Cryptography.RSA]::Create(2048)
$p = $rsa.ExportParameters($true)

$kid = [Guid]::NewGuid().ToString("N")

$publicJwk = [ordered]@{
    kty = "RSA"
    use = "sig"
    alg = "RS384"
    kid = $kid
    n   = ConvertTo-Base64Url $p.Modulus
    e   = ConvertTo-Base64Url $p.Exponent
}

$privateJwk = [ordered]@{
    kty = "RSA"
    use = "sig"
    alg = "RS384"
    kid = $kid
    n   = ConvertTo-Base64Url $p.Modulus
    e   = ConvertTo-Base64Url $p.Exponent
    d   = ConvertTo-Base64Url $p.D
    p   = ConvertTo-Base64Url $p.P
    q   = ConvertTo-Base64Url $p.Q
    dp  = ConvertTo-Base64Url $p.DP
    dq  = ConvertTo-Base64Url $p.DQ
    qi  = ConvertTo-Base64Url $p.InverseQ
}

$privateDir = Split-Path -Parent $PrivateKeyOutputPath
if (-not (Test-Path $privateDir)) {
    New-Item -ItemType Directory -Force -Path $privateDir | Out-Null
}

($privateJwk | ConvertTo-Json) | Out-File -Encoding utf8 -FilePath $PrivateKeyOutputPath
($publicJwk | ConvertTo-Json) | Out-File -Encoding utf8 -FilePath $PublicKeyOutputPath

Write-Host "kid: $kid"
Write-Host "Privat JWK (HEMMELIG, aldri i git): $PrivateKeyOutputPath"
Write-Host "Offentlig JWK (trygg a dele/registrere): $PublicKeyOutputPath"
Write-Host ""
Write-Host "Neste steg:"
Write-Host "  cd src/App"
Write-Host "  dotnet user-secrets set `"SmartOnFhir:ClientAssertionPrivateKeyPath`" `"$PrivateKeyOutputPath`""
