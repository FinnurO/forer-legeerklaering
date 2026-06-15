# FHIR Testdata — Legeerklæring førerrett PoC

## Start HAPI FHIR

```powershell
$env:ALTINN3LOCAL_PORT = "8000"
Set-Location C:\Users\jsf\source\app-localtest
docker-compose up -d hapi_fhir
```

HAPI FHIR starter på http://localhost:8080/fhir (tar ~30 sek første gang).

## Seed testdata

```powershell
.\fhir-testdata\seed.ps1
```

Oppretter følgende ressurser:

| Ressurs | ID | Innhold |
|---|---|---|
| Patient | `sophie-salt` | Fnr 01039012345, Sophie Salt |
| Practitioner | `lege-ola` | HPR 1234567, Ola Nordmann |
| Organization | `sandvika-legesenter` | Orgnr 987654321, Sandvika Legesenter |
| Encounter | `enc-sophie-001` | Konsultasjon 15.06.2026, Sophie hos Sandvika |
| Condition | `cond-sophie-001` | R55 Synkope og kollaps |

## Verifiser

```
http://localhost:8080/fhir/Patient/sophie-salt
http://localhost:8080/fhir/Encounter/enc-sophie-001
http://localhost:8080/fhir/Condition?patient=sophie-salt
```

## Neste steg: SMART auth-server

HAPI FHIR mangler SMART App Launch-støtte (OAuth2 + `/.well-known/smart-configuration`).
For full end-to-end test trengs en SMART-kompatibel auth-server som:

1. Svarer på `GET {iss}/.well-known/smart-configuration` med:
   ```json
   {
     "authorization_endpoint": "http://localhost:9090/auth",
     "token_endpoint": "http://localhost:9090/token",
     "capabilities": ["launch-ehr", "client-confidential-symmetric", "permission-patient"]
   }
   ```

2. Utsteder access tokens med SMART-claims:
   ```json
   {
     "patient": "sophie-salt",
     "encounter": "enc-sophie-001",
     "fhirUser": "http://localhost:8080/fhir/Practitioner/lege-ola"
   }
   ```

### Alternativ A — keycloak (robust, men tungt)
Legg til Keycloak i docker-compose med SMART on FHIR realm-konfigurasjon.

### Alternativ B — enkel Node.js mock (anbefalt for PoC)
En ~100-linjes Express-app som hardkoder tokens for testbrukerne.
Fil: `fhir-testdata/smart-mock/server.js`

Manuell SMART launch-URL (etter at auth-server er oppe):
```
http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/launch?iss=http://localhost:8080/fhir&launch=abc123
```
