# Implementeringsdetaljer og beste praksis
## SMART on FHIR + Altinn Studio — Legeerklæring førerrett

**Dato:** 2026-06-15  
**Basert på:** PoC med Altinn App API 8.6.4, HAPI FHIR R4, SMART App Launch IG v2.2.0

---

## Innhold

1. [Komponentoversikt](#1-komponentoversikt)
2. [Komponent 1: HAPI FHIR R4 (testserver)](#2-komponent-1-hapi-fhir-r4)
3. [Komponent 2: SMART Auth Mock](#3-komponent-2-smart-auth-mock)
4. [Komponent 3: Altinn Local Test](#4-komponent-3-altinn-local-test)
5. [Komponent 4: Altinn .NET App](#5-komponent-4-altinn-net-app)
6. [Nettverksruting](#6-nettverksruting)
7. [Beste praksis](#7-beste-praksis)
8. [Kjente fallgruver](#8-kjente-fallgruver)
9. [Test-endepunkt for lokal utvikling](#9-test-endepunkt-for-lokal-utvikling)
10. [Feilhåndtering — krav til robusthet](#10-feilhåndtering--krav-til-robusthet)
11. [Cache-strategi for FHIR-data](#11-cache-strategi-for-fhir-data)
12. [Proxy-sikkerhet og audit logging](#12-proxy-sikkerhet-og-audit-logging)
13. [Teststrategi og testmiljøer](#13-teststrategi)
    - [Teste mot launch.smarthealthit.org](#teste-mot-en-ekte-standardkompatibel-smart-server-launchsmarthealthitorg)
14. [HelseID-integrasjon: kom i gang](#14-helseid-integrasjon-kom-i-gang)
    - [14.1 HelseID EksternAPI (Oppgave/Skjema) — maskin-til-maskin, verifisert](#141-helseid-eksternapi-helsenorge-oppgaveskjema--maskin-til-maskin-verifisert)
15. [Referanser og inspirasjonskilder](#15-referanser-og-inspirasjonskilder)

---

## 1. Komponentoversikt

Løsningen består av fire separate komponenter som kommuniserer over nettverk. I lokalt utviklingsmiljø kjøres tre av dem i Podman-containere (via docker-compose), og to kjøres direkte på Windows.

```
┌─────────────────────────────────────────────────────┐
│ WINDOWS HOST                                        │
│  ┌──────────────────┐   ┌──────────────────────┐   │
│  │ Altinn .NET App  │   │  SMART Auth Mock      │   │
│  │ localhost:5005   │   │  localhost:9090        │   │
│  └──────────────────┘   └──────────────────────┘   │
└────────────────────────────────── 172.30.80.1 ──────┘
         ↑ port-forward :8000, :8080, :5101
┌─────────────────────────────────────────────────────┐
│ WSL2 / PODMAN (podman-machine-default)              │
│  ┌──────────┐ ┌──────────┐ ┌──────┐ ┌───────────┐  │
│  │  nginx   │ │  Local   │ │ PDF  │ │   HAPI    │  │
│  │  :80→    │ │  Test    │ │  3   │ │   FHIR    │  │
│  │  host    │ │  :5101   │ │:5031 │ │   :8080   │  │
│  │  :8000   │ │          │ │      │ │           │  │
│  └──────────┘ └──────────┘ └──────┘ └───────────┘  │
└─────────────────────────────────────────────────────┘
```

**Diagram:** Se [nettverksruting.svg](./nettverksruting.svg)

---

## 2. Komponent 1: HAPI FHIR R4

### Hva det er
[HAPI FHIR](https://hapifhir.io/) er en åpen kildekode Java-implementasjon av HL7 FHIR-standarden. Den fungerer som en fullstendig FHIR R4-server med RESTful API, søk, validering og persistens.

**I dette prosjektet** brukes HAPI som en enkel testserver som simulerer EPJ-systemets FHIR API. I produksjon erstattes denne av det ekte FHIR-endepunktet i fastlege-EPJ-et (f.eks. CGM Journal, Infodoc eller WebMed — ikke DIPS Arena, som er sykehus-EPJ og ikke relevant for denne fastlegebaserte bruksflyten).

### Hvor det kjøres
```yaml
# app-localtest/docker-compose.yml
hapi_fhir:
  image: hapiproject/hapi:latest
  ports:
    - "8080:8080"
  environment:
    - hapi.fhir.fhir_version=R4
    - hapi.fhir.allow_multiple_delete=true
```

### Testdata og rollemodell

**Kritisk:** Det er legen, ikke pasienten, som logger inn i Altinn. Testdataene må holdes konsistente på tvers av FHIR og Altinn Local Test via fødselsnummer som felles nøkkel.

| Person | Rolle | Fnr | Altinn-bruker | FHIR-ressurs |
|---|---|---|---|---|
| Ola Nordmann | **Lege (innlogget i Altinn)** | `01017512345` | `OlaNordmann` (UserId 12345) | `Practitioner/lege-ola` |
| Sophie Salt | **Pasient (kun i FHIR)** | `01039012345` | `SophieDDG` (UserId 1337) — **ikke bruk** | `Patient/sophie-salt` |

> **Bruk alltid `OlaNordmann` (UserId 12345) når du logger inn i Altinn Local Test — ikke `SophieDDG`.** Sophie Salt skal være pasient i skjemaet, ikke brukeren som fyller det ut.

Synkroniseringsnøkkelen er fødselsnummeret: samme fnr må finnes i Altinn Local Tests profilfil (`testdata/Profile/User/12345.json`) og i FHIR Practitioner-ressursens `identifier`-liste.

Testdata lastes inn via `local-dev/seed.ps1`. Scriptet bruker HTTP PUT for å opprette ressurser med kjente ID-er:

| Ressurs | ID | Innhold |
|---|---|---|
| `Patient` | `sophie-salt` | Fnr 01039012345, navn Sophie Salt — **pasient** |
| `Practitioner` | `lege-ola` | HPR 1234567 + fnr **01017512345**, navn Ola Nordmann — **lege** |
| `PractitionerRole` | `role-lege-ola` | Kobler lege til org: fastlege, allmennmedisin |
| `Organization` | `sandvika-legesenter` | Orgnr 987654321, HER-id 8765432 |
| `Encounter` | `enc-sophie-001` | Kobler pasient, lege og org |
| `Condition` | `cond-sophie-001` | ICD-10: R55 Synkope |

Kjør seedingen:
```powershell
cd local-dev
.\seed.ps1
```

### Nås fra
- **.NET-appen** (Windows): `http://localhost:8080/fhir`
- **SMART Mock** (Windows): `http://localhost:8080`
- **Nettleser** (diagnostikk): `http://localhost:8080/fhir/Patient/sophie-salt`

> **Merk:** `172.30.80.1:8080` brukes når containere skal nå HAPI. `.NET`-appen på Windows bruker `localhost:8080`.

---

## 3. Komponent 2: SMART Auth Mock

### Hva det er
En Node.js/Express-server som simulerer EPJ-systemets SMART autorisasjonsserver. Den implementerer de delene av [SMART App Launch IG v2.2.0](https://hl7.org/fhir/smart-app-launch/) som trengs for EHR Launch-flyten.

**I produksjon** erstattes denne av EPJ-leverandørens ekte SMART-server (f.eks. DIPS sin SMART-implementasjon). Mocken fjernes helt.

### Hvor det kjøres
```
Sti:  local-dev/smart-mock/server.js
Port: 9090 (Windows)
Start: node server.js
```

### Endepunkter

#### `GET /.well-known/smart-configuration`
SMART discovery-endepunkt. Returnerer metadata om auth-serveren:
```json
{
  "issuer": "http://localhost:9090",
  "authorization_endpoint": "http://localhost:9090/auth",
  "token_endpoint": "http://localhost:9090/token",
  "capabilities": ["launch-ehr", "client-confidential-symmetric", ...]
}
```
Altinn-appen kaller dette automatisk under `/smart/launch` for å finne auth- og token-URL-ene.

#### `GET /auth`
Simulerer EPJ-innloggingssiden. I en ekte EPJ vil legen måtte autentisere seg her. Mocken utsteder autorisasjonskode umiddelbart uten brukerinteraksjon.

Mottar: `redirect_uri`, `state`, `launch`, `code_challenge`  
Returnerer: Redirect til `redirect_uri?code=<kode>&state=<state>`

#### `POST /token`
Veksler autorisasjonskode mot access token med SMART-spesifikke claims:
```json
{
  "access_token": "mock-token-abc123",
  "token_type": "Bearer",
  "expires_in": 3600,
  "patient": "sophie-salt",
  "encounter": "enc-sophie-001",
  "fhirUser": "http://172.30.80.1:8080/fhir/Practitioner/lege-ola"
}
```
`patient` og `encounter` er SMART-spesifikke felt som forteller appen hvilken kontekst som gjelder.

`fhirUser` er definert i SMART App Launch IG v2.2.0 som et eget toppnivåfelt i tokenresponsen — og det er slik denne mocken returnerer det. **Merk:** Noen produksjons-EPJ-systemer returnerer `fhirUser` som en claim inne i `access_token` JWT-en (ikke som eget toppnivåfelt). Koden bør da dekode JWT-en server-side og lese ut `fhirUser`-claimet derfra. Se `SmartLaunchController.cs` → `TokenResponse.FhirUser`.

#### `GET /fhir/*`
Proxyer alle FHIR-kall videre til HAPI FHIR på `localhost:8080`. Brukes ikke av .NET-appen direkte (den går til HAPI direkte via `FhirBaseUrlOverride`), men nyttig for testing via nettleser.

### Konfigurasjon
```javascript
const HAPI_FHIR = "http://localhost:8080";   // Lokal HAPI
const HOST_IP = "172.30.80.1";              // Windows-host sett fra containere
// fhirUser-URL bruker HOST_IP slik at .NET-appen kan nå ressursen
// .NET-appen bruker FhirBaseUrlOverride (localhost:8080), ikke fhirUser-URL direkte
```

### Avhengigheter
```bash
cd local-dev/smart-mock
npm install    # express, http-proxy-middleware
node server.js
```

---

## 4. Komponent 3: Altinn Local Test

### Hva det er
[app-localtest](https://github.com/Altinn/app-localtest) er Altinns offisielle lokale testmiljø. Det simulerer Altinn-plattformen lokalt med docker-compose og gir et komplett miljø for app-utvikling uten tilgang til Altinn-skyen.

**I produksjon** erstattes dette av Altinn-plattformens tjenester i skyen. Appen deployes til Kubernetes i Altinn-miljøet.

### Containere

#### `localtest-loadbalancer` (nginx)
Fungerer som inngangspunkt for all trafikk på port 8000. Ruter basert på hostname og path:

| Request | Rutes til |
|---|---|
| `local.altinn.cloud:8000/{org}/{app}/api/...` | `172.30.80.1:5005` (.NET app) |
| `local.altinn.cloud:8000/{org}/{app}/` | `172.30.80.1:5005` (.NET app) |
| `local.altinn.cloud:8000/localtestresources/...` | `host.docker.internal` (localtest) |
| `local.altinn.cloud:8000/authentication/...` | `host.docker.internal` (localtest) |

Viktig konfigurasjon i `docker-compose.yml`:
```yaml
environment:
  - HOST_DOMAIN=172.30.80.1           # Windows-host IP (app kjører her)
  - INTERNAL_DOMAIN=host.docker.internal  # nginx→localtest (container-intern)
  - ALTINN3LOCAL_PORT=8000            # Eksponert port (rootless Podman ≥ 1024)
```

> **Fallgruve:** `HOST_DOMAIN` og `INTERNAL_DOMAIN` er forskjellige. nginx ruter appen til Windows-host-IP, og plattform-API-et til container-internt nettverk.

#### `localtest` (Altinn Platform)
Simulerer Altinn-plattformens API-er:
- **Storage** (`:5101/storage`): Instanser, data-elementer, prosess
- **Authentication** (`:5101/authentication`): JWT-validering, OpenID Connect
- **Authorization** (`:5101/authorization`): PDP-beslutninger (tillat/avslå)
- **Register** (`:5101/register`): Organisasjoner og personer
- **Profile** (`:5101/profile`): Brukerprofiler

Konfigurasjon som brukes av .NET-appen (`appsettings.Development.json`):
```json
"PlatformSettings": {
  "ApiStorageEndpoint": "http://localhost:5101/storage/api/v1/",
  "ApiAuthenticationEndpoint": "http://localhost:5101/authentication/api/v1/",
  "ApiAuthorizationEndpoint": "http://localhost:5101/authorization/api/v1/"
}
```

#### `localtest-pdf3`
Genererer PDF fra Altinn-skjemaer ved innsending. Bruker Chromium headless. Eksponeres på port 5300 på Windows-host.

#### `hapi-fhir`
HAPI FHIR R4-server (se komponent 1 over).

### Start/stopp
```powershell
# Starte (fra app-localtest-mappen)
# Brukes via Podman Desktop GUI, eller:
$env:ALTINN3LOCAL_PORT = "8000"
# Podman Desktop håndterer compose automatisk
```

---

## 5. Komponent 4: Altinn .NET App

### Hva det er
Selve Altinn-applikasjonen. Bygget med Altinn App API 8.6.4 på .NET 8. Denne er den eneste komponenten som vil eksistere i produksjon (de andre er enten plattform eller testes-erstattet).

**Sti:** `forer-legeerklaering/src/App/`

### Nøkkelfiler

| Fil | Formål |
|---|---|
| `Program.cs` | DI-registrering, middleware-oppsett |
| `controllers/SmartLaunchController.cs` | SMART EHR Launch-flyt |
| `services/FhirPrefillService.cs` | `IDataProcessor` — henter og mapper FHIR-data |
| `models/ForerLegeerklaeringModel.cs` | Datamodell (XML/JSON) |
| `ui/form/layouts/Side1.json` | Skjema-layout (Altinn UI) |
| `config/applicationmetadata.json` | App-metadata og datatype-konfig |
| `options/kjoretoygrupper.json` | Kodeverk for kjøretøygrupper |
| `appsettings.Development.json` | Lokal konfigurasjon |

### SmartLaunchController

**Rute:** `[Route("{org}/{app}/smart")]`  
**Auth:** `[AllowAnonymous]` — *kritisk*, Altinn JWT-middleware vil ellers blokkere

Endepunkter:

| Endepunkt | Formål |
|---|---|
| `GET /smart/launch` | EHR Launch entry point. Leser `iss`+`launch`, oppdager SMART-config, genererer PKCE, sender til auth |
| `GET /smart/callback` | OAuth callback. Veksler kode → token server-side, lagrer i session |
| `GET /smart/test-prefill` | **Kun lokal testing.** Bypasser OAuth, seeder session direkte med testdata |

### FhirPrefillService

Implementerer `IDataProcessor`. Kalles av Altinn når skjemadata leses (`ProcessDataRead`).

**Registrering i Program.cs:**
```csharp
services.AddTransient<IDataProcessor, FhirPrefillService>();
```

**Hva den gjør:**
1. Leser `smart_token` og `smart_fhir_context` fra server-session
2. Hvis session er tom: sjekker `IMemoryCache` (fallback)
3. Kaller HAPI FHIR for Patient, Practitioner, Encounter, Organization, Condition
4. Mapper FHIR-JSON til `ForerLegeerklaeringModel`

**Viktig:** `await session.LoadAsync()` må kalles eksplisitt før `session.GetString()`.

### Program.cs — middleware-rekkefølge

```csharp
// Registrering (rekkefølge er likegyldig)
services.AddMemoryCache();
services.AddDistributedMemoryCache();
services.AddSession(options => {
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // ikke Always!
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
services.AddTransient<IDataProcessor, FhirPrefillService>();

// Pipeline (rekkefølge er KRITISK)
app.UseSession();                      // MÅ komme FØR
app.UseAltinnAppCommonConfiguration(); // Altinn-middleware
```

### Start
```powershell
cd C:\Users\jsf\source\forer-legeerklaering\src\App
dotnet run
# Lytter på http://localhost:5005 og https://localhost:5006
```

### Token-livssyklus

SMART access token lever kun server-side i ASP.NET Core session. Her er hele livssyklusen:

```
EPJ utsteder token (expires_in: 3600s typisk)
        │
        ▼
SmartLaunchController.Callback()
  → Lagrer i session: smart_token, smart_fhir_context
  → Lagrer i IMemoryCache (fallback, AbsoluteExpiration = token-utløp)
        │
        ▼
FhirPrefillService.ProcessDataRead()
  → Leser session → gjør FHIR-kall → prefyller skjema
        │
        ├─ Token utløpt? → FHIR-kall returnerer 401
        │                  → Tom prefill, legen fyller inn manuelt
        │                  → Logger advarsel (ikke exception)
        │
        └─ Session utløpt (IdleTimeout 30 min)? → Ny SMART-launch nødvendig
```

**Refresh token:** `offline_access`-scope forespørres for å få refresh token. Ikke alle EPJ-systemer støtter dette. Refresh-logikk er **ikke implementert i PoC** — tokenet brukes til det utløper, deretter må legen gjøre en ny launch.

**Produksjonskrav for token-håndtering:**

| Krav | Implementering |
|---|---|
| Token aldri i nettleser | `HttpOnly` session cookie — ✓ implementert |
| Token utløp håndteres | Graceful degradation til tom prefill — ✓ implementert |
| Refresh token | `offline_access`-scope forespurt — **ikke** implementert i PoC |
| Session timeout | `IdleTimeout = 30 min` i `AddSession()` — ✓ implementert |
| Logout / token revocation | Ikke implementert — session ryddes ved nettleser-lukk |
| Token binding (DPoP) | Kreves av HelseID prod — ikke implementert i PoC |

**Hva skjer ved token-utløp under utfylling:** Legen er midt i skjemautfyllingen. FHIR-prefill er allerede gjort. Skjemaet er åpent. Token-utløp påvirker ikke det pågående arbeidet — dataene er allerede i Altinns datamodell. Legen kan fullføre og sende inn normalt.

---

## 6. Nettverksruting

Se [nettverksruting.svg](./nettverksruting.svg) for fullstendig diagram.

### Port-oversikt

| Port | Tjeneste | Tilgjengelig fra |
|---|---|---|
| 8000 | nginx (via Podman port-forward) | Nettleser, Windows |
| 5005 | .NET App (Windows) | nginx (via 172.30.80.1), localhost |
| 5101 | Altinn Local Test (container) | .NET App (via localhost) |
| 5300 | PDF Generator (via Podman port-forward) | .NET App |
| 8080 | HAPI FHIR (via Podman port-forward) | .NET App, SMART Mock |
| 9090 | SMART Auth Mock (Windows) | .NET App, nettleser |

### IP-adresse-logikk

| Fra → Til | IP som brukes |
|---|---|
| .NET App → HAPI FHIR | `localhost:8080` |
| .NET App → Altinn Local Test | `localhost:5101` |
| nginx-container → .NET App | `172.30.80.1:5005` |
| Altinn Local Test → .NET App | `172.30.80.1:5005` |
| Nettleser → Alt | `local.altinn.cloud:8000` (→ hosts-fil → localhost) |

### hosts-fil (Windows)
```
# C:\Windows\System32\drivers\etc\hosts
127.0.0.1  local.altinn.cloud
```

---

## 7. Beste praksis

### 7.1 Session og token-håndtering

```csharp
// Alltid last session eksplisitt før bruk
await session.LoadAsync();
var token = session.GetString("smart_token");

// Bruk IMemoryCache som fallback
if (string.IsNullOrEmpty(token) && session != null) {
    _memoryCache.TryGetValue("smart_fhir_" + session.Id, out CachedFhirData cached);
}
```

**Aldri** lagre access token i:
- URL-parametere
- localStorage / sessionStorage
- Response-body til nettleser

### 7.2 AllowAnonymous på SMART-endepunkter

Altinn registrerer JWT-cookie-autentisering som default for alle endepunkter. SMART-endepunkter må eksplisitt unntas:

```csharp
[AllowAnonymous]
[ApiController]
[Route("{org}/{app}/smart")]
public class SmartLaunchController : ControllerBase { ... }
```

Uten dette får du redirect-loop: Altinn sender uautentiserte requests til innlogging, som redirecter til SMART-launch, som redirecter til Altinn, osv.

### 7.3 CookieSecurePolicy i lokalt miljø

```csharp
// Lokalt (HTTP): SameAsRequest
// Produksjon (HTTPS): Always
options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
```

### 7.4 CapabilityStatement og graceful degradation

Les alltid `GET /fhir/metadata` ved oppstart og tilpass:

```csharp
// Sjekk om SMART-extensions finnes i CapabilityStatement
var meta = await client.GetStringAsync($"{fhirBase}/metadata");
// Hvis "http://fhir-registry.smarthealthit.org/StructureDefinition/capabilities"
// ikke finnes → fall tilbake til hardkodet SMART-konfig fra appsettings

// Sjekk om Encounter støttes
// Hvis ikke: forsøk GET /fhir/Encounter?patient={id}&status=in-progress
```

Dersom `/.well-known/smart-configuration` mangler men CapabilityStatement finnes:
```
GET /fhir/metadata → rest[0].security.extension[smarts-capabilities]
                                        → authorizationUrl, tokenUrl
```

### 7.5 Nginx og URL-parametere med `://`

Nginx i Altinn Local Test stripper query-parametere som inneholder `://` i verdien. Dette betyr at `?iss=http://localhost:9090` vil miste `iss`-parameteren.

**Løsning:** Lagre defaults i konfig og les fra dem hvis params er tomme:
```csharp
iss ??= _config["SmartOnFhir:DefaultIss"];
launch ??= _config["SmartOnFhir:DefaultLaunch"];
```

### 7.6 IDataProcessor.ProcessDataRead

- Kalles av `DataController.Get` ved hver GET til data-endepunktet
- Kjøres i kontekst av HTTP-requesten (session er tilgjengelig)
- Legg til logging på toppen for å bekrefte at metoden kalles:

```csharp
_logger.LogInformation("ProcessDataRead called for instance {Id}", instance?.Id);
```

### 7.7 FHIR URL-konstruksjon og PractitionerRole

Practitioner-URL fra SMART token (`fhirUser`) er en absolutt URL. Bruk den direkte:
```csharp
// fhirUser = "http://localhost:8080/fhir/Practitioner/lege-ola"
var json = await client.GetStringAsync(ctx.FhirUser);
```

Organization-referanse i Encounter er relativ. Bygg absolutt URL:
```csharp
var orgUrl = orgRef.GetString();
if (!orgUrl.StartsWith("http"))
    orgUrl = $"{ctx.FhirBaseUrl}/{orgUrl}";
```

**PractitionerRole (foretrukket i produksjon):** NAV krever `no-basis-PractitionerRole` som obligatorisk ressurs fordi den kobler legen direkte til organisasjon og rolle i én ressurs — mer robust enn `Encounter.serviceProvider`-kjeden. PoC-en bruker Practitioner + Encounter; i produksjon bør man forsøke PractitionerRole først:

```csharp
// Hent PractitionerRole med søk på practitioner-ID
var prRoleUrl = $"{ctx.FhirBaseUrl}/PractitionerRole?practitioner={practitionerId}&_include=PractitionerRole:organization";
var prRole = await TryGetFhirResource(client, prRoleUrl, "PractitionerRole");
// Fallback: hent Organization via Encounter.serviceProvider som i dag
```

---

## 8. Kjente fallgruver

| Symptom | Årsak | Løsning |
|---|---|---|
| Alle FHIR-felt tomme, ingen feil | `ProcessDataRead` ikke kalt | Sjekk at `IDataProcessor` er registrert i DI |
| "No SMART session found" i logg | Session-cookie ikke sendt | Sjekk `SameSite`, `SecurePolicy`, `HttpOnly` |
| Connection refused til port 8080 | Feil IP — bruker `172.30.80.1` fra .NET-app | Bruk `localhost:8080` fra Windows-prosesser |
| ERR_TOO_MANY_REDIRECTS | Mangler `[AllowAnonymous]` | Legg til attributt på controller |
| "no schema with key" | JSON Schema draft/2020-12 brukes | Bruk draft-07, fjern `$id`-feltet |
| "Could not find data type 'model'" | `layout-sets.json` peker på feil dataType | Sett `dataType` til eksakt ID fra `applicationmetadata.json` |
| Nginx fjerner `iss`-parameter | `://` i query-param-verdi | Bruk `DefaultIss` i konfig som fallback |
| `TimeSpan` not found i Program.cs | Mangler `using System;` | Legg til using øverst i filen |
| Session tom etter `await LoadAsync()` | `UseSession()` kalt etter Altinn-middleware | Flytt `app.UseSession()` til FØR `UseAltinnAppCommonConfiguration()` |

---

## 9. Test-endepunkt for lokal utvikling

For å teste FHIR-prefill uten full OAuth-flyt:

```
GET http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/test-prefill
```

Dette endepunktet:
1. Skriver mock-token og FHIR-kontekst direkte til server-session
2. Lagrer også i `IMemoryCache` (fallback)
3. Videresender til skjemaet

**Viktig:** Fjern eller beskytt dette endepunktet i produksjon!

```csharp
// Eksempel: deaktiver i produksjon
[HttpGet("test-prefill")]
public async Task<IActionResult> TestPrefill()
{
    if (!_env.IsDevelopment())
        return NotFound();
    // ...
}
```

---

## 10. Feilhåndtering — krav til robusthet

FHIR-kall kan feile på mange måter. Løsningen **må ikke krasje** når en ressurs mangler — legen skal alltid få opp skjemaet og kan fylle inn manuelt.

### Forventede feilscenarier

| Scenario | Årsak | Håndtering |
|---|---|---|
| `launch` finnes, men `patient` mangler i token | EPJ støtter ikke `launch/patient` | Forsøk `GET /fhir/Patient?identifier={fnr}` fra HelseID-kontekst, ellers tom |
| `Patient` returnerer 404 | Feil pasient-ID i token | Logg advarsel, la felt stå tomme |
| `Encounter` returnerer 404 eller 403 | Encounter-ID utløpt eller `launch/encounter` ikke støttet | Forsøk `GET /fhir/Encounter?patient={id}&status=in-progress`, ellers tom |
| `Condition.read` ikke autorisert (403) | Scope ikke innvilget av EPJ | Fang `HttpRequestException` med status 403, logg, hopp over |
| `fhirUser` mangler i token | EPJ inkluderer ikke `fhirUser` | Forsøk `/fhir/Practitioner/{id}` via annen kontekst, ellers tom |
| FHIR-server utilgjengelig (timeout) | Nettverksfeil, EPJ nede | Timeout etter 5 sek, skjema åpnes uten prefill |

### Mønster for defensiv FHIR-henting

```csharp
private async Task<JsonDocument?> TryGetFhirResource(HttpClient client, string url, string name)
{
    try
    {
        var json = await client.GetStringAsync(url);
        return JsonDocument.Parse(json);
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    {
        _logger.LogWarning("FHIR {Name} not found: {Url}", name, url);
        return null;
    }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
    {
        _logger.LogWarning("FHIR {Name} access denied (scope not granted): {Url}", name, url);
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "FHIR {Name} fetch failed: {Url}", name, url);
        return null;
    }
}
```

Scoped scope-nedgradering (hvis EPJ returnerer smalere scope enn forespurt):
```csharp
// Etter token-utveksling: sjekk hva EPJ faktisk innvilget
var grantedScope = tokenResponse["scope"]?.GetString() ?? "";
var hasEncounter = grantedScope.Contains("launch/encounter");
// Tilpass hva vi forsøker å hente
```

---

## 11. Cache-strategi for FHIR-data

### Hva som caches i dag

| Data | Lagring | Levetid |
|---|---|---|
| Access token | ASP.NET Core Session + IMemoryCache | Session-timeout (30 min) |
| FHIR-kontekst (patientId, encounterId) | Samme | Samme |
| FHIR-ressursdata (Patient, Practitioner...) | **Ikke cachet** — hentes ved hver `ProcessDataRead` | — |

### Vurdering

FHIR-data hentes på nytt ved **hver** GET mot data-endepunktet. Dette er akseptabelt i PoC der:
- Skjemaet åpnes én gang
- FHIR-kall er raske (lokal server)

I produksjon bør data caches per instans:
```csharp
// Cachet i IMemoryCache med instans-ID som nøkkel
var cacheKey = $"fhir_data_{instance.Id}";
if (!_memoryCache.TryGetValue(cacheKey, out ForerLegeerklaeringModel cached))
{
    cached = await FetchFromFhir(ctx);
    _memoryCache.Set(cacheKey, cached, TimeSpan.FromMinutes(15));
}
```

### Personvern

FHIR-data er helseopplysninger. Krav:
- **Ingen logging av helseopplysninger** (innhold i FHIR-ressurser) — logg kun ressurs-ID og HTTP-statuskoder
- **Ingen persistering til disk** — kun minne, og kun for sessionens varighet
- **Cache tømmes** når session utløper (IMemoryCache med AbsoluteExpiration = session-timeout)
- I distribuert deploy: bruk Redis med kryptering, ikke IDistributedMemoryCache uten kryptering

---

## 12. Proxy-sikkerhet og audit logging

BFF-mønsteret (Altinn-appen som FHIR-proxy) krever egne sikkerhetskrav i produksjon:

### Krav

| Krav | Beskrivelse |
|---|---|
| Audit logging | Alle FHIR-kall skal logges med: tidspunkt, legens HPR, pasientens fnr (som hash eller referanse), ressurstype, HTTP-status |
| Token forwarding | Access token videresendes **kun** til den EPJ-en det ble utstedt fra (`iss`-validering) |
| Token exchange | Vurder om legens HelseID-token bør brukes i stedet for EPJ-token der HelseAPI er integrasjonspunkt |
| Tilgangskontroll | Verifiser at EPJ-ets token faktisk tilhører den innloggede Altinn-brukeren (fnr-match via ID-porten og HelseID) |
| Rate limiting | Begrens antall FHIR-kall per session for å hindre misbruk |

### Minimalt audit-mønster

```csharp
_logger.LogInformation(
    "FHIR audit: hpr={Hpr} resource={Resource} id={Id} status={Status}",
    practitionerHpr, resourceType, resourceId, (int)response.StatusCode
);
// Aldri log fnr eller annet pasientidentifiserende i klartekst
```

### Digdir-kapabiliteter og helsesektorens tilsvarende løsninger

Altinn Studio er bygget på Digdirs fellesløsninger. Tabellen under viser relevansen for dette prosjektet og hva helsesektoren tilbyr som tilsvarende mekanismer.

#### Maskinporten

OAuth2 `client_credentials` for system-til-system-kommunikasjon uten bruker. Relevant dersom EPJ-leverandøren krever at Altinn-appen er pre-registrert som godkjent API-konsument — utover SMART-tokenet som identifiserer brukeren.

**Helsesektorens tilsvarende:** HelseID `client_credentials` grant type — identisk konsept, men administrert av NHN og begrenset til godkjente helsevirksomheter.

#### Altinn Autorisasjon

Altinns system for roller, rettigheter og delegering — hvem kan gjøre hva på vegne av hvem. I produksjon bør Altinn Autorisasjon brukes til å kontrollere at legen har rett til å sende inn legeerklæringen på vegne av pasienten.

**Helsesektorens tilsvarende — to lag:**

| Mekanisme | Hva den kontrollerer |
|---|---|
| HPR-autorisasjon | Hvem som er autorisert helsepersonell og for hvilke handlinger |
| NHN Tillitsrammeverk | Hvem som har tilgang til hvilke helsedata basert på behandlingsrelasjon og formål |

Tillitsrammeverket bæres som claims i HelseID-tokenet og er særlig relevant for at EPJ skal kunne ta en informert tilgangsbeslutning. Relevante claims fra TTT-modellen:

```
PractitionerAuthorizationCode    → autorisasjonstype (f.eks. "LE" for lege)
CareRelationshipPurposeOfUseCode → formål med datatilgangen
CareRelationshipHealthcareServiceCode → helsetjenestetype
PractitionerLegalEntityId        → virksomhetens org.nr.
```

**Viktig:** Altinn Autorisasjon og NHN Tillitsrammeverk er *komplementære*, ikke konkurrerende. Altinn kontrollerer skjemainnsendingen; Tillitsrammeverk kontrollerer FHIR-datatilgangen fra EPJ. Et produksjonssystem trenger begge.

#### Ressursregisteret

Altinns katalog over beskyttede ressurser. I produksjon bør "legeerklæring for førerrett" registreres her slik at Altinn Autorisasjon kan håndheve tilgangsstyringen formelt.

**Helsesektorens tilsvarende:** NHN selvbetjeningsportal (`selvbetjening.nhn.no`) — EPJ-leverandører registrerer API-er og konsumenter søker tilgang.

#### Prioritering

| Kapabilitet | PoC | TT02 | Produksjon |
|---|---|---|---|
| Maskinporten / HelseID client_credentials | Nei | Vurder | Avhenger av EPJ |
| Altinn Autorisasjon | Nei | Nei | **Ja** |
| NHN Tillitsrammeverk (claims i HelseID-token) | Nei | Ja | **Ja** |
| Ressursregisteret | Nei | Nei | **Ja** |

---

## 13. Teststrategi

### Testmiljøer og testdata — oversikt

Det finnes tre relevante miljøer med ulik testdata-infrastruktur. Testdata må henge **konsistent sammen** på tvers av alle systemer: fnr som brukes i Altinn-innlogging må finnes i Folkeregisteret test, i HelseID, i SyntPop og i FHIR-dataene.

| Miljø | Altinn | ID-porten | HelseID | FHIR/EPJ | Testdata-kilde |
|---|---|---|---|---|---|
| **Lokalt** | Local Test (port 8000) | Mocket (ingen) | SMART mock | HAPI FHIR lokal | Hardkodet i `seed.ps1` |
| **TT02** | `tt02.altinn.no` | ID-porten test | HelseID test | EPJ-leverandørens testsystem | **Tenor** (Skatteetaten) + **SyntPop** (NHN) |
| **Produksjon** | `altinn.no` | ID-porten prod | HelseID prod | EPJ prod | Ekte data |

#### Tenor — nasjonal testdata-infrastruktur

**Tenor** er Skatteetatens søkeverktøy for å finne syntetiske testpersoner som fungerer i *alle nasjonale testmiljøer*. Tenor-personene er synkronisert med:

- **ID-porten test** — kan logge inn som hvilken som helst Tenor-person via syntetisk fnr
- **Folkeregisteret test (FREG)** — Tenor-fnr finnes her og er gyldige
- **Altinn TT02** — bruker samme identitetsinfrastruktur som ID-porten test
- **HelseID test** — TestIDP og TTT bruker Tenor-kompatible fnr

SyntPop bygger videre på Tenor-populasjonen og legger til HPR- og FLR-data. En person med fnr X i Tenor er den **samme personen** i SyntPop, ID-porten test og Altinn TT02.

#### Konsistenskrav ved TT02-overgang

```
Tenor-person (fnr = T)
    │
    ├─► ID-porten test → logger inn i Altinn TT02 med fnr T
    │
    ├─► HelseID test (TTT/TestIDP) → token med pid = T
    │
    ├─► SyntPop → person med fnr T har HPR-nummer H og FLR-tilknytning
    │
    └─► FHIR (EPJ testsystem) → Practitioner.identifier.value = T
                                 Patient.identifier.value = pasientens fnr

Alle systemer må bruke SAMME fnr for SAMME person.
```

**Praktisk prosess for TT02-overgang:**

1. Finn en syntetisk lege i **SyntPop** med HPR-nummer og aktiv fastlegeavtale
2. Hent legens fnr — dette er også Tenor-fnr, gyldig i ID-porten test
3. Finn en pasient i SyntPop via `GET /api/flr/doctor/{hprnr}` — pasienter på denne legens liste
4. Oppdater `seed.ps1` med de faktiske fnr/HPR-numrene
5. Bruk legens fnr i HelseID TTT-token (`Pid` + `HprNumber`)
6. Logg inn i Altinn TT02 med legens fnr via ID-porten test

**Merk om Tenor-tilgang:** Tenor søke-UI er tilgjengelig på `https://tenor.skatteetaten.no/` (krever innlogging). Alternativt finnes Tenor-kompatible testpersoner via Skatteetatens test-API og via SyntPop.

### Lokalt (PoC)

| Verktøy | Formål |
|---|---|
| HAPI FHIR (lokal) | Testserver med syntetiske ressurser (se `seed.ps1`) |
| SMART Auth Mock | Simulerer EPJ-autorisasjonsserver |
| `/smart/test-prefill` | Bypasser OAuth for rask prefill-testing |

### Integrasjonstesting mot reelle SMART-servere

| Verktøy | URL | Formål |
|---|---|---|
| SMARTHealthIT Sandbox | https://launch.smarthealthit.org/ | Offentlig SMART EHR Launch-simulator med syntetiske pasienter |
| Inferno Test Suite | https://inferno.healthit.gov/ | Sertifiseringstesting av SMART App Launch-klienter |
| HAPI FHIR public | https://hapi.fhir.org/baseR4 | Offentlig FHIR R4-testserver |

### TT02-overgang: sjekkliste

Når PoC skal flyttes fra Local Test til TT02, må disse stegene gjennomføres **i denne rekkefølgen** fordi alle systemer må bruke konsistente testpersoner:

- [ ] **1. Velg syntetisk lege i SyntPop**
  - Logg inn på `syntpop.nhn.no` med HelseID (testmiljø)
  - Søk: `POST /api/search { "hpr": { "isGP": true }, "flr": { "hasGP": true } }`
  - Velg lege med HPR-godkjenning i allmennmedisin og aktive FLR-pasienter
  - Noter: `fnr_lege`, `hpr_nummer`, `navn`

- [ ] **2. Finn en pasient på legens liste**
  - `GET /api/flr/doctor/{hpr_nummer}` → liste over pasienter
  - Velg én pasient: noter `fnr_pasient`, `navn`

- [ ] **3. Verifiser at fnr er Tenor-kompatible**
  - Søk på `tenor.skatteetaten.no` — bekreft at begge fnr finnes der
  - Disse fnr-ene er gyldige i ID-porten test og Altinn TT02

- [ ] **4. Oppdater FHIR-testdata**
  - Oppdater `seed.ps1` (eller nytt TT02-seed-script) med ekte Tenor-fnr
  - Practitioner: `fnr = fnr_lege`, HPR-nummer = `hpr_nummer`
  - Patient: `fnr = fnr_pasient`
  - Behold samme FHIR-ressurs-IDer (`lege-ola`, `sophie-salt`) om mulig

- [ ] **5. Konfigurer HelseID TTT**
  - Generer TTT-token med `Pid = fnr_lege`, `HprNumber = hpr_nummer`
  - API-nøkkel fra `selvbetjening.test.nhn.no`

- [ ] **6. Test Altinn TT02-innlogging**
  - Logg inn via ID-porten test med `fnr_lege`
  - Bekreft at Altinn TT02 kjenner igjen personen og gir tilgang til appen

- [ ] **7. Deploy app til TT02**
  - `altinn studio deploy` til TT02-miljøet
  - Oppdater `appsettings.json` med TT02-endepunkter og HelseID test-issuer

### Testscenarioer som bør dekkes

| Scenario | Prioritet |
|---|---|
| Happy path: alle ressurser finnes og returneres | Kritisk |
| Encounter mangler i token | Høy |
| Condition.read ikke autorisert | Høy |
| Patient har ingen aktiv Encounter | Medium |
| fhirUser mangler i token | Medium |
| FHIR-server timeout | Medium |
| EPJ returnerer smalere scope enn forespurt | Høy |
| CapabilityStatement mangler SMART-extensions | Medium |

### Syntetiske pasienter og leger fra NHN SyntPop

**SyntPop** (`syntpop.nhn.no`) er NHNs syntetiske befolkningsregister — en komplett testversjon av folkeregisteret, HPR (Helsepersonellregisteret) og FLR (Fastlegeregisteret) med realistiske syntetiske fnr-er og HPR-numre. Dataene er ikke tilknyttet ekte personer.

API: `https://api.syntpop.nhn.no/` (krever HelseID- eller Azure AD-autentisering)

Relevante endepunkter for legeerklæring-scenariet:

| Endepunkt | Beskrivelse |
|---|---|
| `GET /api/persons` + `POST /api/search` | Søk etter syntetiske pasienter med filter |
| `GET /api/flr/patient/{nin}` | Slå opp en pasients fastlege (returnerer fastlegens HPR-nummer) |
| `GET /api/flr/doctor/{hprnr}` | Slå opp alle pasienter knyttet til en lege |
| `GET /api/hpr/persons/hprNr:{hprNr}/raw` | Rådata for helsepersonell: spesialitet, autorisasjoner |

#### Typisk arbeidsflyt for testdataforberedelse

```
1. Logg inn i SyntPop med HelseID (testmiljø)
2. Søk etter pasient med hasGP=true og ønsket kjønn/alder
   POST /api/search { "flr": { "hasGP": true } }
3. Hent pasientens fastlege:
   GET /api/flr/patient/{pasientFnr}  →  { gpHprNr: [1234567] }
4. Hent legedetaljer:
   GET /api/hpr/persons/hprNr:1234567/raw  →  navn, spesialitet, autorisasjoner
5. Bruk dataene til å:
   a) Oppdatere seed.ps1 med realistisk syntetisk pasient + lege
   b) Generere HelseID TTT-token med matching pid + hpr_number
```

#### FLR-data gir realistisk fastlege-relasjon

FLR (Fastlegeregisteret) linker pasient ↔ fastlege direkte. `AzureFlrIndex`-skjemaet inneholder:
- `gpHprNr[]` — HPR-numre til legens kontrakt
- `hasGP` — om pasienten har registrert fastlege
- `primaryHealthcareTeamId` — til-team-tilknytning

Dette gir et mer realistisk testscenario enn hardkodede verdier, særlig ved fremtidig testing mot NHNs FHIR-API der fastlegeforholdet valideres.

#### Kobling til HelseID Test Token Service

Kombinert med TTT (se seksjon 14) kan SyntPop-data brukes til å generere et komplett testsett:

```
SyntPop pasient: fnr = 13085012345, navn = "Kari Olsen"
SyntPop lege:    HPR = 7654321, navn = "Per Hansen", spesialitet = Allmennmedisin

→ FHIR seed: Patient/kari-olsen + Practitioner/per-hansen + PractitionerRole
→ HelseID TTT: { Pid: "01017512345", HprNumber: "7654321", Name: "Per Hansen" }
→ Altinn testbruker: fnr = 01017512345 (OlaNordmann i Local Test)
```

#### Merk: SyntPop er ikke en FHIR-server

Data fra SyntPop må konverteres til FHIR-ressurser og PUT inn i HAPI FHIR manuelt (via seed-script). Det er ingen direkte FHIR-integrasjon. På sikt kan seed-scriptet oppdateres til å hente data fra SyntPop API automatisk.

For globalt tilgjengelige syntetiske pasienter (ikke norsk): [Synthea](https://github.com/synthetichealth/synthea) genererer FHIR R4-bundles direkte.

### Teste mot en ekte, standardkompatibel SMART-server: launch.smarthealthit.org

**Status (2026-08-11): ✅ Full ende-til-ende-demo oppnådd.** Fra `launch.smarthealthit.org` gjennom innlogging, pasientvalg, SMART-callback, Altinn-innlogging og fullstendig FHIR-prefill (pasient, lege, virksomhet, konsultasjon) til et utfylt skjema klart til signering. Underveis ble **7 bugs** funnet og rettet — se funn #1–7 under. Dette er den mest grundig verifiserte enkeltstrekningen i hele PoC-en.

[SMART Launcher](https://launch.smarthealthit.org/) (fra SMART Health IT-prosjektet, `smart-on-fhir/smart-launcher-v2` på GitHub) er en offentlig, spec-compliant EHR-launch-simulator med syntetiske Synthea-pasienter. Siden hele OAuth-redirecten skjer i nettleseren (ikke server-til-server), kan den brukes til å teste vår faktiske `SmartLaunchController`-flyt lokalt — noe vi aldri hadde gjort før, siden `/smart/dev-login` alltid har vært workaround-veien.

**Slik gjør du det:**
1. Sørg for at hele det lokale miljøet kjører (Altinn localtest, HAPI FHIR, Altinn-appen — se README.md «Kom i gang»). SMART Auth Mock trengs *ikke* for denne testen.
2. Gå til [launch.smarthealthit.org](https://launch.smarthealthit.org/)
3. Launch Type: «Provider EHR Launch», FHIR Version: «R4»
4. La «Patient(s)» og «Provider(s)» stå tomme (gir en interaktiv pasientvelger/innlogging — mer realistisk enn en forhåndsvalgt pasient)
5. I feltet **«App's Launch URL»**, lim inn: `http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/launch`
6. Klikk **Launch** — dette åpner en lenke i formatet `<launch-url>?iss=...&launch=...`, som du åpner i din egen nettleser (ikke i et sandboxet/skybasert nettleserpanel — `local.altinn.cloud` blir ofte blokkert der som et internt nettverk)

**Funn #1 — `ERR_TOO_MANY_REDIRECTS` (løst i vår egen kode):** `SmartConfiguration`- og `TokenResponse`-klassene i `SmartLaunchController.cs` hadde ingen `[JsonPropertyName]`-attributter. `JsonSerializerOptions { PropertyNameCaseInsensitive = true }` løser kun store/små bokstaver — **ikke** `snake_case` (`authorization_endpoint`) → `PascalCase` (`AuthorizationEndpoint`). Feltene deserialiserte alltid til `null`, `BuildAuthorizationUrl` bygde en tom/relativ redirect-URL, og nettleseren tolket det som «last denne siden på nytt» — i det uendelige. Denne koden har vært ødelagt siden den ble skrevet; den ble aldri oppdaget fordi `/smart/dev-login` omgår hele discovery/redirect-stien. Rettet med eksplisitte `[JsonPropertyName("...")]` på begge klassene (både `authorization_endpoint`/`token_endpoint` og OAuth2-tokenfeltene `access_token`/`token_type`/`expires_in`/`refresh_token`, som har samme snake_case-problem).

**Funn #2 — manglende port i `redirect_uri` (løst i `app-localtest`, ikke vårt repo):** Etter funn #1 var rettet, kom en ny variant av samme feil: `redirect_uri` ble bygget uten port (`local.altinn.cloud` i stedet for `local.altinn.cloud:8000`), som ga `ERR_CONNECTION_REFUSED` på callback. Rotårsak: `app-localtest/loadbalancer/templates/nginx.conf.conf` bruker `proxy_set_header Host $host;` — en kjent nginx-fallgruve der `$host` **utelater porten**. Rettet lokalt til `proxy_set_header Host $http_host;` (som bevarer det klienten faktisk sendte) og verifisert med `podman restart localtest-loadbalancer`. Dette er en feil i Altinns egen delte `app-localtest`-infrastruktur (git-repo `C:\Users\jsf\source\app-localtest` hos Johann), ikke i `forer-legeerklaering` — bør vurderes rapportert til Altinn Studio-teamet, siden enhver Altinn-app som konstruerer sin egen eksterne redirect-URL fra `Request.Host` vil rammes.

**Funn #3 — manglende Altinn-sesjon etter SMART callback (mitigert i dev, IKKE løst for prod — se VEIKART.md fase 1):** Med bugs #1–2 rettet fullføres hele SMART-flyten korrekt — innlogging, pasientvalg, autorisasjonskode, callback, tokenutveksling. Men appen hadde da kun FHIR/SMART-kontekst, **ingen Altinn-sesjon**. Altinns eget `AltinnCore.Authentication.JwtCookie`-rammeverk (i `Altinn.App.Api`) forsøker å utfordre med en redirect til `{ApiAuthenticationEndpoint}authentication?goto=...` — et endepunkt som ikke finnes i denne versjonen av `app-localtest` (verifisert: `AuthenticationController` har kun `refresh`/`orgToken`/`appToken`, ingen ren `authentication`-rute, og «goto» finnes ikke i kildekoden i det hele tatt). Dette er sannsynligvis nøyaktig grunnen til at `/smart/dev-login` ble laget: den er den eneste stien som *både* logger inn i Altinn (via localtests test-token-endepunkt) *og* seeder FHIR-konteksten i ett steg.

**Midlertidig løsning (kun Development):** `Callback` sjekker nå om det finnes en Altinn-sesjon (`AltinnStudioRuntime`-cookie); hvis ikke, kalles samme localtest-testbruker-mekanikk som `/dev-login` bruker (utledet til en delt `EstablishLocaltestAltinnSessionAsync`-metode), som logger inn som Dr. Ola Nordmann. **Dette er ikke en produksjonsløsning** — den bruker localtests test-token-endepunkt, som ikke finnes utenfor lokal testing. Se VEIKART.md fase 1 for det egentlige forslaget: `/smart/callback` må i produksjon etablere en ekte Altinn-sesjon koblet til HelseID-identitet (§14), ikke en generisk ID-porten-utfordring.

**Funn #4 — `FhirBaseUrlOverride` kapret ekte eksterne servere:** `SmartOnFhir:FhirBaseUrlOverride` er satt i `appsettings.Development.json` for å peke vår egen lokale SMART-mock til riktig Docker-adresse. Koden brukte den ubetinget uansett `iss`, som betød at en ekte ekstern launch (smarthealthit.org) fikk FHIR-oppslagene sine omdirigert til vår egen tomme HAPI FHIR-mock — stille feil, ingen data funnet. Rettet: override brukes nå kun når `iss` ikke er `https://` (dvs. bare for vår egen `http`-mock).

**Funn #5 — kaskaderende bug fra funn #1:** Da `TokenResponse.AccessToken` fikk `[JsonPropertyName("access_token")]` (funn #1), endret det også hvordan tokenet *serialiseres* til sesjonen — fra `"AccessToken"` til `"access_token"`. `FhirPrefillService.TokenData` (en annen klasse, brukt til å lese tokenet tilbake ut av sesjonen i `ProcessDataRead`) hadde ingen tilsvarende attributt og forventet fortsatt `"AccessToken"`. Tokenet ble dermed `null` → `Bearer <null>` → **401 Unauthorized** på alle FHIR-kall, helt stille. Rettet med samme `[JsonPropertyName("access_token")]` på `TokenData`, pluss oppdatering av to steder som konstruerte et mock-token som anonymt objekt (`/dev-login`, `/test-prefill`) til samme nøkkelnavn.

**Funn #6 — `fhirUser` som relativ URL:** `FillPractitioner` antok `ctx.FhirUser` alltid var en full URL. `launch.smarthealthit.org` returnerte en relativ referanse (`Practitioner/<id>`), som gjorde at HTTP-kallet feilet stille (ugyldig URI). Rettet med samme mønster som allerede fantes for Organization-referanser: prepend `FhirBaseUrl` hvis verdien ikke starter med `http`.

**Funn #7 — `fhirUser` manglet som toppnivåfelt i token-responsen:** Selv med funn #4–6 rettet, forble legenavn tomt. `token.FhirUser` var tom fordi smarthealthit.org for denne launch-konfigurasjonen ikke inkluderte `fhirUser` som eget felt i token-svaret — kun som et `fhirUser`-claim inne i selve `access_token`-JWT-en. En kommentar i koden hadde allerede forutsett akkurat dette scenariet («noen EPJ-systemer returnerer det som JWT-claim i access_token i stedet») uten at fallbacken faktisk var implementert. Lagt til `TryExtractClaimFromJwt` — dekoder JWT-payloaden uten signaturvalidering (kun brukt til prefill, aldri til autorisasjonsbeslutninger) og henter ut `fhirUser`-claimet hvis toppnivåfeltet mangler.

### Writeback til EPJ — første vellykkede skrivetest (2026-08-11)

**Status: ✅ Writeback-mekanikken fungerer.** Lagt til skrivescope (`patient/DocumentReference.write` + `patient/DocumentReference.c`, både v1- og v2-stil) i `Launch()`, og et dev-only testendepunkt `GET /smart/test-writeback` som poster en minimal `DocumentReference` mot EPJ-ens FHIR-server med SMART-tokenet fra den aktive sesjonen.

**Resultat mot launch.smarthealthit.org:** `HTTP 201 Created`, begge skrivescopene innvilget uten innsnevring. Dette besvarer to spørsmål samtidig:
- **VEIKART.md fase 2 (writeback):** selve skrivemekanikken (Bearer-token + POST + FHIR-ressurs) fungerer beviselig mot en spec-compliant server. Gjenstår: PDF-generert innhold (ikke placeholder-tekst), idempotens via klient-tildelt id + PUT (VEIKART.md spesifiserer dette for den ferdige løsningen), og `QuestionnaireResponse` i tillegg til `DocumentReference`.
- **HACKATHON-EHIN-2026.md «scope-detektivarbeid» (Sølv):** token-responsens `scope`-felt ble lest ut og sammenlignet mot det som ble forespurt — ingen innsnevring skjedde for dette testet.

**Bifunn — mulig charset-problem, ikke undersøkt videre:** `DocumentReference.content[].attachment.title` (som inneholder norske tegn: «Legeerklæring førerrett») kom tilbake fra serveren som mojibake («LegeerklÃ¦ring fÃ¸rerrett»), mens `attachment.data` (base64, kun ASCII) var uendret. `StringContent` ble sendt med explicit UTF-8-encoding fra vår side, så dette tyder på at smarthealthit.org sin server mistolker charset ved mottak av request-body — ikke nødvendigvis representativt for en ekte fastlege-EPJ. **Bør verifiseres spesifikt** når writeback testes mot et ekte EPJ-system, siden norske tegn (æ/ø/å) er uunngåelige i skjemainnhold.

Se [SmartLaunchController.cs](../src/App/controllers/SmartLaunchController.cs) `TestWriteback()`-metoden for full implementasjon.

**Kan man se resultatet?** Ja — smarthealthit.org sin FHIR-server tillater ukryptert `GET` uten token. `curl https://launch.smarthealthit.org/v/r4/fhir/DocumentReference/<id>` (eller lim URL-en inn i en nettleser) viser ressursen som rå FHIR-JSON. Det finnes ikke noe klinisk visningsgrensesnitt på smarthealthit.org — det er et API-sandkasse, ikke en journal-UI — så «se resultatet» betyr i praksis å lese JSON-en direkte, ikke en visuell journalside.

### Viktig presisering — to ulike aktører og oppgaver må ikke blandes

Johann presiserte 2026-08-12: **innbyggers egenerklæring** (NA-0201) og **writeback til EPJ** er to helt forskjellige integrasjoner, ikke to varianter av samme ting:

| | Innbyggers egenerklæring | Writeback til EPJ |
|---|---|---|
| **Aktør** | Innbygger/pasient | Behandler (lege) |
| **Hvor fylles det ut** | Altinn (egen app, `forer-egenerklaring`) | — (data hentes/skrives via FHIR, ikke fylt ut på nytt) |
| **Protokoll** | *Ikke* SMART on FHIR — vanlig Altinn-skjema | SMART on FHIR |
| **Hvor havner resultatet** | Altinn innboks (alltid) + kvittering til Helsenorge (i tillegg) | EPJ-ens FHIR-server |
| **Relevant API** | Trolig Helsenorge DokumentAPI (se PASIENTFLYT.md) — ikke bekreftet | `DocumentReference`/`QuestionnaireResponse` via SMART-token (denne seksjonen) |

Dette skiller seg fra tidligere formuleringer i STRATEGI.md/PASIENTFLYT.md som til tider har omtalt begge som del av «SMART on FHIR-flyten» — kun writeback til EPJ er SMART on FHIR. Egenerklæringen er et rent Altinn-skjema med en kvitteringsmekanisme mot Helsenorge.

### Timing-bug funnet og rettet: writeback må ikke skje før innsending (2026-08-12)

Johann påpekte at writeback ikke bør skje før legen faktisk trykker «Send inn» — og det avdekket en **allerede eksisterende bug**, ikke bare et fremtidig hensyn for writeback-koden. `IDataProcessor.ProcessDataWrite` (som `ForerKonklusjonModel`-avledningen lå i, se BESLUTNINGER.md C-3) kjører på **hver autolagring** mens legen fyller ut skjemaet — ikke bare ved innsending. Det betyr SVV-konklusjonen ble skrevet/oppdatert gjentatte ganger under utfylling, potensielt med en halvferdig eller uferdig vurdering.

**Riktig hook er `IProcessTaskEnd.End(taskId, instance)`** — kjører når en prosessoppgave *avsluttes* (for oss: Task_1, signering/innsending). Verifiserte den eksakte metodesignaturen direkte mot den installerte `Altinn.App.Core` 8.6.4-pakken via refleksjon (GitHub sin `main`-gren viste en annen, nyere signatur på `IDataClient.GetFormData` enn det som faktisk er installert — verdt å huske for senere).

**Rettet:** `FhirPrefillService` implementerer nå `IProcessTaskEnd` i tillegg til `IDataProcessor`. `ProcessDataWrite` er en no-op. `End(taskId, instance)` sjekker `taskId == "Task_1"`, henter `ForerLegeerklaeringModel` via `IDataClient.GetFormData`, og avleder/lagrer `ForerKonklusjonModel` — først da. **Denne samme regelen gjelder for den fremtidige EPJ-writeback-implementasjonen** (VEIKART.md fase 2): den må også ligge i `End()`, ikke i `ProcessDataWrite` eller trigges av testendepunktet `test-writeback` (som er et manuelt, sesjonsbasert diagnoseverktøy — ikke koblet til prosesslivssyklusen, og skal aldri bli mønsteret for den ferdige implementasjonen).

---

## 14. HelseID-integrasjon: kom i gang

### Bakgrunn

HelseID autentiserer helsepersonell via ID-porten og beriker identiteten med helsefaglige claims (HPR-nummer, assurance-nivå, organisasjonstilknytning). Siden HelseID bruker ID-porten som identitetsrot, er `pid`-claimet (fnr) identisk med det Altinn mottar ved normal ID-porten-innlogging. SSO oppnås dermed via felles `pid` — ingen ny plattformavtale er nødvendig.

Testmiljøet (`selvbetjening.test.nhn.no`) er tilgjengelig uten formell leverandøravtale med NHN — du logger inn med ID-porten og registrerer klient selv.

### Steg 1: Registrer klient i HelseID testmiljø

1. Gå til `https://selvbetjening.test.nhn.no/`
2. Logg inn med din ID-porten-identitet
3. Opprett ny klient med disse verdiene:

| Felt | Verdi |
|---|---|
| Redirect URI | `http://localhost:5005/smart/helseid-callback` |
| Scopes | `openid profile helseid://scopes/identity/pid helseid://scopes/hpr/hpr_number helseid://scopes/identity/assurance_level` |
| Grant type | `authorization_code` |
| Auth method | `private_key_jwt` |

4. Generer JWK-nøkkelpar (RS256). Last ned privat nøkkel — lagres i `appsettings.Development.json` (aldri i git)

### Steg 2: Konfigurer app

```json
// appsettings.Development.json
{
  "HelseID": {
    "Authority": "https://helseid-sts.test.nhn.no",
    "ClientId": "<din-klient-id>",
    "PrivateKeyJwk": "<din-private-nøkkel-som-JWK-json>"
  }
}
```

### Steg 3: Token-validering i BFF

Legg til NuGet-pakke:
```
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

Legg til i `Program.cs`:
```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer("helseid", options =>
    {
        options.Authority = builder.Configuration["HelseID:Authority"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://helseid-sts.test.nhn.no",
            ValidateAudience = false,
            NameClaimType = "helseid://claims/identity/pid"
        };
    });
```

### Steg 4: Ekstraher claims i SmartLaunchController

```csharp
// Etter validering av HelseID-token:
var pid = principal.FindFirst("helseid://claims/identity/pid")?.Value;
var hprNumber = principal.FindFirst("helseid://scopes/hpr/hpr_number")?.Value;
var assuranceLevel = principal.FindFirst("helseid://claims/identity/assurance_level")?.Value;

// Synkroniser med Altinn-sesjon:
var altinnPid = HttpContext.User.FindFirst("urn:altinn:userid")?.Value;
if (pid != altinnPid)
{
    // Advarsel: HelseID-identity matcher ikke Altinn-sesjon
    _logger.LogWarning("HelseID pid {HelseIdPid} != Altinn pid {AltinnPid}", pid, altinnPid);
}

// Lagre i session for prefill:
session.SetString("LegerHprNummer", hprNumber ?? string.Empty);
```

### Steg 5: TestIDP for simulert brukerinnlogging

HelseID testmiljø har en "TestIDP" som lar deg simulere innlogging uten ekte testbrukere:
- Velg TestIDP på innloggingsskjermen
- Velg en testperson med HPR-nummer
- Tokenet inneholder `pid` og `hpr_number` som i produksjon

Se ferdigkonfigurerte eksempler: [NorskHelsenett/HelseID.Samples](https://github.com/NorskHelsenett/HelseID.Samples)

### Claims-kart

| HelseID-claim | Skjemafelt | OID / FHIR |
|---|---|---|
| `helseid://claims/identity/pid` | (synk-nøkkel) | `urn:oid:2.16.578.1.12.4.1.4.1` |
| `helseid://scopes/hpr/hpr_number` | `LegeHprNummer` | `urn:oid:2.16.578.1.12.4.1.4.4` |
| `name` | `LegeNavn` | — |
| `helseid://claims/identity/assurance_level` | (validering) | — |

### DPoP i produksjon

Produksjonsmiljøet krever DPoP (Demonstrating Proof-of-Possession) — et ekstra lag som binder access token til nøkkelpar og forhindrer replay-angrep. HelseID.Samples-repoet viser implementasjon. DPoP er ikke nødvendig i testmiljøet **for denne `authorization_code`-klienten** (SMART-launch, autentisering av legen).

**Presisering (2026-08-10):** dette gjelder *kun* denne konkrete klienttypen. Den separate `client_credentials`-klienten for Helsenorge EksternAPI (§14.1 under) trigget DPoP nonce-challenge selv i testmiljø — DPoP-kravet varierer altså per klient/grant type, ikke bare per miljø. Ikke anta at "ikke nødvendig i test" gjelder generelt.

---

## 14.1 HelseID EksternAPI (Helsenorge Oppgave/Skjema) — maskin-til-maskin, verifisert

**Status:** Autentisering verifisert 2026-08-10. Dette er en **separat integrasjon** fra §14 over — ikke SMART-launch/autentisering av legen, men maskin-til-maskin-kommunikasjon mot Helsenorges eksterne API for å sende oppgaver/skjema til *pasienten* (relevant for [PASIENTFLYT.md](PASIENTFLYT.md) alternativ B — digital NA-0201-egenerklæring via helsenorge.no).

**Hva det er:** `nhn:helsenorge.eksternapi/oppgave` og `nhn:helsenorge.eksternapi/skjema` er HelseID-scopes for Helsenorge EksternAPI — ikke SMART on FHIR. Autentisering skjer med OAuth2 `client_credentials` (ingen brukerredirect), `private_key_jwt`-klientautentisering og DPoP-bundet access token. Ifølge [Helsenorge sin dokumentasjon](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/3262578689/Nye+IP+adresser+for+EksternAPI+og+F+rerrett) deler EksternAPI infrastruktur/IP-adresser med NHNs egen produksjons-Førerrett-App.

**Registrert klient:** "Altinn Studio" i [selvbetjening.test.nhn.no](https://selvbetjening.test.nhn.no/), Client-ID `4f1fc480-72d9-4e31-b099-69b84fd5ba6b`, auth method `private_key_jwt` (RSA 4096).

**Verifisert oppsett:** bruk NHNs offisielle [`HelseID.Library.ClientCredentials`](https://github.com/NorskHelsenett/HelseID.Library) NuGet-pakke (v1.1.3, støtter `net8.0`) — den håndterer JWT-client-assertion-signering og DPoP-proof automatisk, ingen egen implementasjon nødvendig:

```csharp
var helseIdConfiguration = new HelseIdConfiguration
{
    ClientId = "4f1fc480-72d9-4e31-b099-69b84fd5ba6b",
    Scope = "nhn:helsenorge.eksternapi/oppgave nhn:helsenorge.eksternapi/skjema",
    IssuerUri = "https://helseid-sts.test.nhn.no",
};

builder.Services
    .AddHelseIdClientCredentials(helseIdConfiguration)
    .AddJwkForClientAuthentication(privateKeyJwk); // fra lokal fil, ALDRI i kildekode/git

var flow = host.Services.GetRequiredService<IHelseIdClientCredentialsFlow>();
var tokenResponse = await flow.GetTokenResponseAsync();
```

Se [local-dev/helseid-token-test/](../local-dev/helseid-token-test/) for en kjørbar røyktest og full begrunnelse for pakkevalget.

**Bekreftet i testmiljø:**
- Begge scopene (`oppgave`, `skjema`) innvilges uten `RejectedScope` med dagens single-tenant-oppsett (ingen `.AddHelseIdMultiTenant()` eller organisasjonsnummer sendt i token-forespørselen).
- Access token har svært kort levetid i test (60 sekunder) — hent nytt token rett før bruk.
- DPoP nonce-challenge (`400` → retry → `200`) trigges av `helseid-sts.test.nhn.no` for denne klienten og håndteres automatisk av biblioteket.

**Oppdatert 2026-08-11 — selve API-kallet er nå strukturelt verifisert:**

Sendte en faktisk `POST https://eksternapi.hn2.test.nhn.no/oppgave/v1/Task` med FHIR `Task`-payload. Fem funn, i rekkefølge:

1. Klienten «Altinn Studio» er registrert som **multi-tenant** i HelseID — token-forespørselen må bruke `.AddHelseIdMultiTenant()` og sende `OrganizationNumbers { ParentOrganization, ChildOrganization }` (`orgnr_parent` alene, uten multi-tenant-oppsett, ga en misvisende `401 EHSEC-110002` "token expired or invalid").
2. `Task.focus` er reelt obligatorisk (dokumentasjonen markerer kun `focus.type` eksplisitt som "mandatory").
3. For `focus.type = "Communication"` (informasjonsoppgave) er `Task.instantiatesUri` obligatorisk.
4. Med disse på plass validerer API-et hele FHIR-strukturen korrekt.
5. **Gjenstående blokker er ikke teknisk:** `400` — "Pasienten er ikke digitalt aktiv for tjeneste: OmradeHelsehjelp". Testet med to uavhengige, Tenor-verifiserte testpersoner (Høy Hai og Sart Maskin) — samme feil begge ganger, som utelukker at det er en enkeltpersonsfeil.

**Konklusjon etter videre undersøkelse (2026-08-11):** Dette lar seg ikke løse ved å prøve flere testpersoner eller mer kode. Helsenorge sin citizen-vendte portal (`https://helsenorge.hn2.test.nhn.no` og «bakdør»-varianten `https://tjenester.hn2.test.nhn.no/?pnr=...`) er IP-sperret uavhengig av URL-variant, og ifølge [Hvordan komme i gang](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/1348174733/Hvordan+komme+i+gang) gis testmiljøtilgang (portal + trolig provisjonering av «digitalt aktive» testpersoner) kun etter formell leverandørkontakt med NHN — ikke selvbetjent slik EksternAPI-autentiseringen er. Se [BESLUTNINGER.md C-6](BESLUTNINGER.md) for anbefalt neste steg (kontakt via `ext-utv-hn-forerrett`-kanalen eller `ide-ogbestillingsmottak@nhn.no`).

Se [local-dev/helsenorge-oppgave-test/](../local-dev/helsenorge-oppgave-test/) for full feilsøkingslogg og kjørbar kode.

**Fortsatt ikke utforsket:** skjemaoppgave (`focus.type = "Questionnaire"`) og `Bundle`-varianten (`POST .../oppgave/v1/Bundle`) — det er dette som faktisk trengs for NA-0201-egenerklæringen, se [PASIENTFLYT.md](PASIENTFLYT.md).

---

## 15. Referanser og inspirasjonskilder

### Standarder og spesifikasjoner

| Kilde | Beskrivelse | URL |
|---|---|---|
| SMART App Launch IG v2.2.0 | Primær spesifikasjon for SMART on FHIR EHR Launch, PKCE, scopes, token-parametre | https://hl7.org/fhir/smart-app-launch/ |
| HL7 FHIR R4 | Ressursdefinisjoner: Patient, Practitioner, Organization, Encounter, Condition, DocumentReference | https://hl7.org/fhir/R4/ |
| OAuth 2.0 RFC 6749 | Grunnleggende OAuth-flyt som SMART bygger på | https://datatracker.ietf.org/doc/html/rfc6749 |
| PKCE RFC 7636 | Proof Key for Code Exchange — code_verifier + code_challenge (S256) | https://datatracker.ietf.org/doc/html/rfc7636 |

### Norske FHIR-profiler og kodeverk

| Kilde | Beskrivelse | URL |
|---|---|---|
| no-basis (HL7 Norway) | Norske basisprofiler for FHIR R4: NoBasisPatient, NoBasisPractitioner m.fl. | https://hl7norway.github.io/basisprofil-no-R4/ |
| Volven / OID-register | Norske OID-er: fnr (`4.1`), HPR (`4.4`), orgnr (`4.101`), HER-id (`4.2`) | https://www.ehelse.no/kodeverk-og-terminologi/OID |
| Norsk FHIR-profil sykmelding (NAV) | Mønster for FHIR-basert skjemautfylling i norsk helsesektor, SMART on FHIR EHR Launch, strukturering av Condition/Encounter-ressurser | https://github.com/navikt/syk-dig-backend |
| FHIR4 NoBasisOrganization | HER-id og organisasjonsnummer i Organization.identifier | https://simplifier.net/hl7norwayno-basis |

### Altinn

| Kilde | Beskrivelse | URL |
|---|---|---|
| Altinn Studio dokumentasjon | IDataProcessor, datamodell, layout, options, deployment | https://docs.altinn.studio/ |
| app-localtest | Lokalt testmiljø (docker-compose, nginx, Altinn Platform) | https://github.com/Altinn/app-localtest |
| Altinn App API 8.6.4 | NuGet-pakker brukt i prosjektet (Altinn.App.Core, Altinn.App.Api) | https://www.nuget.org/packages/Altinn.App.Core |
| Altinn Studio URL-parametere | Dokumentasjon om query params og prefill via URL | https://docs.altinn.studio/altinn-studio/reference/ux/fields/prefill/ |

### HelseID

| Kilde | Beskrivelse | URL |
|---|---|---|
| HelseID utviklerportal | Dokumentasjon, protokoller, sikkerhetsprofil | https://utviklerportal.nhn.no/informasjonstjenester/helseid/ |
| HelseID selvbetjening test | Klientregistrering uten formell avtale | https://selvbetjening.test.nhn.no/ |
| HelseID testmiljø OIDC | Discovery-dokument for testmiljø | https://helseid-sts.test.nhn.no/.well-known/openid-configuration |
| NorskHelsenett/HelseID.Samples | Offisielle kodeeksempler (ASP.NET Core, BFF, token exchange, DPoP) | https://github.com/NorskHelsenett/HelseID.Samples |
| NHN SyntPop | Syntetisk befolkningsregister med HPR og FLR — realistiske testnr/HPR uten ekte personer | https://syntpop.nhn.no/ |

### Verktøy og biblioteker

| Kilde | Beskrivelse | URL |
|---|---|---|
| HAPI FHIR | Åpen kildekode FHIR R4-server brukt som lokal EPJ-mock | https://hapifhir.io/ |
| Express.js | Node.js-rammeverk for SMART Auth Mock | https://expressjs.com/ |
| Podman / Podman Desktop | Rootless containermotor (erstatning for Docker Desktop) | https://podman.io/ |

### Relaterte implementasjoner brukt som referanse

| Kilde | Beskrivelse |
|---|---|
| NAV syk-inn (ny sykmelding) | Autoritært norsk referansedokument for SMART on FHIR mot EPJ. Krever PractitionerRole, Encounter, no-basis-profiler. Sertifiseringsmodell for EPJ-leverandører. Krav: https://github.com/navikt/syk-inn/blob/main/docs/fhir/nav_requirements.md |
| DIPS Arena SMART-dokumentasjon | Referanseimplementasjon for SMART Auth Server-struktur (`/.well-known/smart-configuration`) — sykehus-EPJ, brukt kun som teknisk arkitekturreferanse. Selve bruksflyten (førerkortattest) er fastlege-EPJ. |
| SMARTHealthIT sandbox | Offentlig SMART-sandbox brukt for testing av OAuth-flyt og discovery |
