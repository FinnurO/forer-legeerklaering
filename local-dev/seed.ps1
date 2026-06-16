# Seed HAPI FHIR med norske syntetiske testdata
# Kjør: .\seed.ps1

$baseUrl = "http://localhost:8080/fhir"

Write-Host "Venter på HAPI FHIR..." -ForegroundColor Yellow
$attempts = 0
do {
    Start-Sleep -Seconds 3
    $attempts++
    try { $r = Invoke-RestMethod "$baseUrl/metadata" -TimeoutSec 3; break } catch {}
    Write-Host "  Forsok $attempts..."
} while ($attempts -lt 20)
Write-Host "HAPI FHIR er klar." -ForegroundColor Green

function Put-Resource($resourceType, $id, $body) {
    try {
        Invoke-RestMethod -Method Put -Uri "$baseUrl/$resourceType/$id" `
            -ContentType "application/fhir+json" `
            -Body ($body | ConvertTo-Json -Depth 10) | Out-Null
        Write-Host "  OK: $resourceType/$id" -ForegroundColor Green
    } catch {
        Write-Host "  FEIL: $resourceType/$id - $_" -ForegroundColor Red
    }
}

Write-Host "`nSeeder testdata..." -ForegroundColor Yellow

Put-Resource "Patient" "sophie-salt" @{
    resourceType = "Patient"
    id = "sophie-salt"
    identifier = @(@{ system = "urn:oid:2.16.578.1.12.4.1.4.1"; value = "01039012345" })
    name = @(@{ family = "Salt"; given = @("Sophie") })
    birthDate = "1990-03-01"
    gender = "female"
}

Put-Resource "Practitioner" "lege-ola" @{
    resourceType = "Practitioner"
    id = "lege-ola"
    identifier = @(
        @{ system = "urn:oid:2.16.578.1.12.4.1.4.4"; value = "1234567" },
        @{ system = "urn:oid:2.16.578.1.12.4.1.4.1"; value = "01017512345" }
    )
    name = @(@{ family = "Nordmann"; given = @("Ola") })
}

Put-Resource "Organization" "sandvika-legesenter" @{
    resourceType = "Organization"
    id = "sandvika-legesenter"
    identifier = @(
        @{ system = "urn:oid:2.16.578.1.12.4.1.4.101"; value = "987654321" },
        @{ system = "urn:oid:2.16.578.1.12.4.1.2"; value = "8765432" }
    )
    name = "Sandvika Legesenter"
}

Put-Resource "Encounter" "enc-sophie-001" @{
    resourceType = "Encounter"
    id = "enc-sophie-001"
    status = "finished"
    class = @{ system = "http://terminology.hl7.org/CodeSystem/v3-ActCode"; code = "AMB" }
    subject = @{ reference = "Patient/sophie-salt" }
    participant = @(@{ individual = @{ reference = "Practitioner/lege-ola" } })
    serviceProvider = @{ reference = "Organization/sandvika-legesenter" }
    period = @{ start = "2026-06-15T10:00:00+02:00"; end = "2026-06-15T10:30:00+02:00" }
}

Put-Resource "Condition" "cond-sophie-001" @{
    resourceType = "Condition"
    id = "cond-sophie-001"
    clinicalStatus = @{
        coding = @(@{ system = "http://terminology.hl7.org/CodeSystem/condition-clinical"; code = "active" })
    }
    code = @{
        coding = @(@{
            system = "http://hl7.org/fhir/sid/icd-10"
            code = "R55"
            display = "Synkope og kollaps"
        })
    }
    subject = @{ reference = "Patient/sophie-salt" }
    encounter = @{ reference = "Encounter/enc-sophie-001" }
    recordedDate = "2026-06-15"
}

Put-Resource "Patient" "per-hansen" @{
    resourceType = "Patient"
    id = "per-hansen"
    identifier = @(@{ system = "urn:oid:2.16.578.1.12.4.1.4.1"; value = "12055589765" })
    name = @(@{ family = "Hansen"; given = @("Per") })
    birthDate = "1955-05-12"
    gender = "male"
}

Put-Resource "Patient" "anne-johansen" @{
    resourceType = "Patient"
    id = "anne-johansen"
    identifier = @(@{ system = "urn:oid:2.16.578.1.12.4.1.4.1"; value = "03117845231" })
    name = @(@{ family = "Johansen"; given = @("Anne") })
    birthDate = "1978-11-03"
    gender = "female"
}

Put-Resource "Patient" "kari-larsen" @{
    resourceType = "Patient"
    id = "kari-larsen"
    identifier = @(@{ system = "urn:oid:2.16.578.1.12.4.1.4.1"; value = "22076812345" })
    name = @(@{ family = "Larsen"; given = @("Kari") })
    birthDate = "1968-07-22"
    gender = "female"
}

Put-Resource "Patient" "olav-berg" @{
    resourceType = "Patient"
    id = "olav-berg"
    identifier = @(@{ system = "urn:oid:2.16.578.1.12.4.1.4.1"; value = "05024512345" })
    name = @(@{ family = "Berg"; given = @("Olav") })
    birthDate = "1945-02-05"
    gender = "male"
}

Put-Resource "Encounter" "enc-per-001" @{
    resourceType = "Encounter"
    id = "enc-per-001"
    status = "finished"
    class = @{ system = "http://terminology.hl7.org/CodeSystem/v3-ActCode"; code = "AMB" }
    subject = @{ reference = "Patient/per-hansen" }
    participant = @(@{ individual = @{ reference = "Practitioner/lege-ola" } })
    serviceProvider = @{ reference = "Organization/sandvika-legesenter" }
    period = @{ start = "2026-06-16T09:00:00+02:00"; end = "2026-06-16T09:20:00+02:00" }
}

Put-Resource "Encounter" "enc-anne-001" @{
    resourceType = "Encounter"
    id = "enc-anne-001"
    status = "finished"
    class = @{ system = "http://terminology.hl7.org/CodeSystem/v3-ActCode"; code = "AMB" }
    subject = @{ reference = "Patient/anne-johansen" }
    participant = @(@{ individual = @{ reference = "Practitioner/lege-ola" } })
    serviceProvider = @{ reference = "Organization/sandvika-legesenter" }
    period = @{ start = "2026-06-16T10:00:00+02:00"; end = "2026-06-16T10:30:00+02:00" }
}

Put-Resource "Encounter" "enc-kari-001" @{
    resourceType = "Encounter"
    id = "enc-kari-001"
    status = "finished"
    class = @{ system = "http://terminology.hl7.org/CodeSystem/v3-ActCode"; code = "AMB" }
    subject = @{ reference = "Patient/kari-larsen" }
    participant = @(@{ individual = @{ reference = "Practitioner/lege-ola" } })
    serviceProvider = @{ reference = "Organization/sandvika-legesenter" }
    period = @{ start = "2026-06-16T11:00:00+02:00"; end = "2026-06-16T11:15:00+02:00" }
}

Put-Resource "Encounter" "enc-olav-001" @{
    resourceType = "Encounter"
    id = "enc-olav-001"
    status = "finished"
    class = @{ system = "http://terminology.hl7.org/CodeSystem/v3-ActCode"; code = "AMB" }
    subject = @{ reference = "Patient/olav-berg" }
    participant = @(@{ individual = @{ reference = "Practitioner/lege-ola" } })
    serviceProvider = @{ reference = "Organization/sandvika-legesenter" }
    period = @{ start = "2026-06-16T13:00:00+02:00"; end = "2026-06-16T13:30:00+02:00" }
}

Write-Host "`nFerdig! Pasienter seeded:" -ForegroundColor Green
Write-Host "  sophie-salt  (enc-sophie-001)"
Write-Host "  per-hansen   (enc-per-001)"
Write-Host "  anne-johansen (enc-anne-001)"
Write-Host "  kari-larsen  (enc-kari-001)"
Write-Host "  olav-berg    (enc-olav-001)"
Write-Host "  Practitioner: lege-ola | Org: sandvika-legesenter"
