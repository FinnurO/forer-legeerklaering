<#
.SYNOPSIS
    Regenererer DOKUMENTASJON-KOMPLETT.md fra docs/-mappen, i samme rekkefølge som tabellen i README.md.

.DESCRIPTION
    Tidligere versjoner av DOKUMENTASJON-KOMPLETT.md var en manuelt kondensert oppsummering av
    hvert dokument og gikk ut av sync med kildefilene i docs/ etter hvert som de ble oppdatert
    (kvalitetssikring 2026-06-19, Del 3 — "kryssreferanser bør ryddes"). Dette skriptet gjør en
    ekte full sammenslåing, så samledokumentet alltid er i sync med kildefilene.

    Kjør på nytt etter enhver endring i docs/*.md. Rediger ikke DOKUMENTASJON-KOMPLETT.md direkte.

.EXAMPLE
    cd docs
    .\generate-samlet-dokumentasjon.ps1
#>

$repoRoot = Split-Path -Parent $PSScriptRoot
$docsDir  = Join-Path $repoRoot "docs"
$outFile  = Join-Path $repoRoot "DOKUMENTASJON-KOMPLETT.md"

# Rekkefølge og titler — må holdes i sync med dokumentasjonstabellen i README.md
$sections = @(
    @{ File = "KRAVSPESIFIKASJON-v0.6.md";      Title = "Kravspesifikasjon v0.6" }
    @{ File = "IMPLEMENTERING.md";               Title = "Implementeringsdetaljer" }
    @{ File = "SKJEMA-IS2569.md";                Title = "Skjemastruktur IS-2569" }
    @{ File = "PASIENTFLYT.md";                  Title = "Pasientflyt" }
    @{ File = "BESLUTNINGER.md";                 Title = "Åpne beslutninger" }
    @{ File = "RISIKOREGISTER.md";               Title = "Risikoregister" }
    @{ File = "VEIKART.md";                      Title = "Veikart" }
    @{ File = "SAMMENLIGNING-syk-inn.md";        Title = "Sammenligning: forer vs. syk-inn vs. NHN Førerrett-App" }
    @{ File = "NHN-DOKUMENTASJON.md";            Title = "NHN-dokumentasjon" }
    @{ File = "KARTLEGGING-kandidater.md";       Title = "Kartlegging av rapporteringsplikter" }
    @{ File = "STRATEGI.md";                     Title = "Strategi" }
)

function Get-Slug([int]$Number, [string]$Title) {
    $t = $Title.ToLowerInvariant()
    $t = $t -replace "[^\p{L}\p{N}\s-]", ""
    $t = $t -replace "\s+", "-"
    return "$Number-$t"
}

$today = Get-Date -Format "yyyy-MM-dd"

$toc = "## Innholdsfortegnelse`n`n"
for ($i = 0; $i -lt $sections.Count; $i++) {
    $n = $i + 1
    $slug = Get-Slug $n $sections[$i].Title
    $toc += "$n. [$($sections[$i].Title)](#$slug)`n"
}

$body = ""
foreach ($s in $sections) {
    $i = [array]::IndexOf($sections, $s)
    $n = $i + 1
    $path = Join-Path $docsDir $s.File
    if (-not (Test-Path $path)) {
        Write-Warning "Mangler fil: $path - hoppet over"
        continue
    }
    $content = Get-Content -Path $path -Raw -Encoding UTF8
    $body += "`n---`n`n# $n. $($s.Title)`n`n$content`n"
}

$header = @"
# forer-legeerklaering — Samlet dokumentasjon

**Generert:** $today
**Kilde:** ``docs/`` — rekkefølge etter tabell i README.md. Generert av ``docs/generate-samlet-dokumentasjon.ps1`` — kjør skriptet på nytt etter endringer i docs/*.md, rediger ikke denne filen direkte.

---

$toc
"@

Set-Content -Path $outFile -Value ($header + $body) -Encoding UTF8
Write-Host "Skrev $outFile"
