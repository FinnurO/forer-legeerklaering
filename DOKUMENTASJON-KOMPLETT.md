# forer-legeerklaering — Samlet dokumentasjon

**Generert:** 2026-06-18  
**Kilde:** `docs/` — rekkefølge etter tabell i README.md

---

## Innholdsfortegnelse

1. [Kravspesifikasjon v0.6](#1-kravspesifikasjon-v06)
2. [Implementeringsdetaljer](#2-implementeringsdetaljer)
3. [Skjemastruktur IS-2569](#3-skjemastruktur-is-2569)
4. [Pasientflyt](#4-pasientflyt)
5. [Åpne beslutninger](#5-åpne-beslutninger)
6. [Veikart](#6-veikart)
7. [Sammenligning: forer vs. syk-inn vs. NHN Førerrett-App](#7-sammenligning)
8. [NHN-dokumentasjon](#8-nhn-dokumentasjon)
9. [Kartlegging av rapporteringsplikter](#9-kartlegging)
10. [Strategi](#10-strategi)

---
---

# 1. Kravspesifikasjon v0.6

# Kravspesifikasjon: SMART on FHIR-integrasjon med Altinn Studio
## Legeerklæring for førerrett — v0.6

**Dato:** 2026-06-15
**Status:** PoC gjennomført — grunnleggende prefill fungerer
**Forfatter:** Digitaliseringsdirektoratet

---

## 1. Bakgrunn og formål

Legeerklæring for førerrett er et offentlig skjema som legen fyller ut og sender inn til Helsedirektoratet/Statens vegvesen. I dag gjøres dette manuelt, ofte ved å kopiere informasjon fra EPJ-systemet (elektronisk pasientjournal) inn i et separat skjema.

**Formålet med dette initiativet** er å vise at Altinn Studio kan brukes som skjema-plattform for helsefaglig dokumentasjon der:

1. Legen er allerede innlogget i EPJ-systemet (f.eks. DIPS Arena)
2. Kontekstuell data (pasient, konsultasjon, diagnose) hentes automatisk fra EPJ via FHIR
3. Legen kontrollerer, supplerer og sender inn via Altinn
4. PDF-kvittering kan skrives tilbake til EPJ-journalen

### 1.1 Strategisk relevans

- Reduserer dobbeltregistrering for helsepersonell
- Gjenbruker Altinns eksisterende infrastruktur for signering, arkivering og distribusjon
- Etablerer mønster som kan brukes for andre helseskjemaer (sykmelding, henvisninger, attester)
- Følger internasjonale standarder: SMART App Launch IG v2.2.0, FHIR R4

---

## 2. Avgrensninger

### 2.1 MVP-scope

| I scope | Ikke i scope |
|---|---|
| SMART App Launch 2.0 (EHR Launch) | SMART Backend Services |
| Read-only FHIR (prefill) | FHIR Write / FHIR-innsending |
| Konfidensielt klient (server-side) | Dynamic Client Registration |
| EHR Launch-flyt | Standalone Launch |
| Én pasient, én konsultasjon | Multi-pasient-arbeidsflyt |
| Norske FHIR-basisprofiler (no-basis) | CDS Hooks |
| Altinn-innlogging via ID-porten | PKI-signering av lege (kvalifisert) |
| PDF-generering og arkivering i Altinn | DocumentReference writeback til EPJ |

**Uten denne listen** vil prosjektet typisk bli dratt mot SMART Backend Services, CDS Hooks og FHIR Write — som alle er separate integrasjonsprosjekter.

### 2.2 Suksesskriterier

| Kriterium | Målbar definisjon |
|---|---|
| SMART EHR Launch | Legen starter appen fra EPJ med `iss` + `launch`-parameter; OAuth-flyten fullføres uten manuell innlogging i FHIR |
| Pasientdata prefylt | Navn og fødselsnummer er automatisk utfylt fra `Patient`-ressurs |
| Legedata prefylt | HPR-nummer og legens navn er automatisk utfylt fra `Practitioner`-ressurs |
| Konsultasjonskontekst | Minst én klinisk ressurs (`Encounter` eller `Condition`) er hentet og brukt |
| Innsending | Skjemaet kan fylles ut og sendes inn via Altinn; PDF-kvittering genereres |
| Feiltoleranse | Dersom én FHIR-ressurs mangler, åpnes skjemaet med tomme felt — ikke feilmelding |

---

## 3. Aktører

| Aktør | Rolle | System |
|---|---|---|
| Lege | Fyller ut og sender inn skjema | EPJ + Altinn |
| EPJ-system | Starter SMART-launch, tilbyr FHIR API | DIPS Arena / tilsvarende |
| Altinn Platform | Infrastruktur for skjema, signering, arkiv | Altinn 3 |
| Altinn-appen | Applikasjonslaget — SMART-integrasjon + skjema | .NET 8 / Altinn Studio |
| Helsedirektoratet | Tjenesteeier, mottar innsending | — |

---

## 4. Arkitektur

### 4.1 Overordnet prinsipp

```
EPJ → SMART Launch → Altinn App → FHIR API → Prefill → Skjema → Innsending
```

**Nøkkelprinsipp:** FHIR brukes **utelukkende til forhåndsutfylling**. Access token lagres **aldri** i nettleseren — kun server-side i ASP.NET Core session.

**BFF-mønster — dette er arkitekturvalget:** Altinn-appen fungerer som konfidensielt klient og mellomlag. All token-utveksling og alle FHIR-kall skjer server-side. En browser-direkte-FHIR-tilnærming er **avvist** fordi:
- SMART-tokenet ville ligget eksponert i nettleseren
- Det er ikke mulig å beskytte `client_secret` i en SPA
- Loggings- og audit-krav forutsetter server-side kontroll

### 4.2 Autentisering — to separate lag

| Lag | System | Formål |
|---|---|---|
| Altinn-autentisering | ID-porten / JWT | Legen som Altinn-bruker (person) |
| SMART-autentisering | EPJ OAuth2 / SMART | Tilgang til FHIR-ressurser i EPJ |

Disse to er **ikke** koblet — Altinn vet ikke om SMART-tokenet, og EPJ vet ikke om Altinn-innloggingen.

### 4.3 Dataflyt

1. EPJ omdirigerer legens nettleser til `/smart/launch?iss=...&launch=...`
2. Appen oppdager EPJs SMART-konfigurasjon (`/.well-known/smart-configuration` eller CapabilityStatement)
3. PKCE-utfordring genereres; nettleser sendes til EPJs `/auth`-endepunkt
4. EPJ utsteder autorisasjonskode; nettleser sendes til `/smart/callback`
5. Appen veksler kode mot token **server-side**
6. Token + FHIR-kontekst lagres i server-session
7. Nettleser videresendes til Altinn-skjemaet
8. `IDataProcessor.ProcessDataRead` kalles → `FhirPrefillService` henter FHIR-data og fyller ut datamodellen
9. Lege kontrollerer prefylt skjema, fyller ut gjenværende felt, sender inn

---

## 5. Datamodell

### 5.1 Feltgrupper og FHIR-kilde

| Feltgruppe | Felt | FHIR-ressurs | Norsk OID |
|---|---|---|---|
| **Pasient** | Fnr | Patient.identifier | `urn:oid:2.16.578.1.12.4.1.4.1` |
| | Fornavn / Etternavn / Fødselsdato / Kjønn | Patient.name / birthDate / gender | — |
| **Lege** | HPR-nummer | Practitioner.identifier | `urn:oid:2.16.578.1.12.4.1.4.4` |
| | Fornavn / Etternavn | Practitioner.name | — |
| **Lege (foretrukket)** | HPR + org + rolle | PractitionerRole → Practitioner + Organization | — |
| **Virksomhet** | Orgnr / HER-id / Navn | Organization.identifier / name | `4.101` / `4.2` |
| **Konsultasjon** | Dato | Encounter.period.start | — |
| **Diagnose** | Kode / Tekst | Condition.code.coding | ICD-10 |
| **Erklæring** | Kjøretøygruppe / Er skikket / Vilkår / Merknad | (lege velger/fyller ut) | — |

---

## 6. SMART on FHIR — tekniske krav

### 6.1 Scopes som kreves

```
openid profile fhirUser launch launch/patient launch/encounter offline_access
patient/Patient.read patient/Encounter.read patient/Condition.read patient/Observation.read
user/Practitioner.read user/Organization.read user/PractitionerRole.read
```

`user/`-prefix brukes for behandlerdata (`Practitioner`, `Organization`, `PractitionerRole`) siden disse tilhører *behandleren*, ikke pasienten.

### 6.2 Sikkerhetskrav

- PKCE påkrevd (S256)
- Konfidensielt klient — `client_secret` brukes ved token-utveksling
- Token lagres **kun** server-side
- `iss`-validering mot `AllowedIssuerList` i konfig
- State-parameter for CSRF-beskyttelse

### 6.3 SSO for lege: HelseID og Altinn

HelseID bruker ID-porten som rot-identitetstjeneste. SSO skjer naturlig via felles `pid`-claim:

```
Lege i EPJ:     ID-porten → HelseID → EPJ (token med pid + hpr_number)
Lege i Altinn:  ID-porten → Altinn  (token med pid)
                               ↑ Samme pid (fnr) — samme person
```

BFF-en validerer HelseID-tokenet direkte mot HelseID JWKS. Ingen ny plattformavtale er nødvendig.

### 6.4 Åpne problemer

| Problem | Status | Prioritet |
|---|---|---|
| ERR_TOO_MANY_REDIRECTS ved full OAuth-flyt | Uløst — workaround: `/smart/dev-login` | Høy |
| DocumentReference writeback til EPJ | Ikke implementert | Medium |
| HelseID-token validering i BFF | Ikke implementert | Høy |
| Issuer-allowlist er tom | Konfig-gap | Høy (prod) |

---

## 7. Personvern og behandlingsgrunnlag

### 7.1 Behandlingsgrunnlag

| Hjemmel | Grunnlag |
|---|---|
| Helsepersonelloven § 45 | Legen kan innhente helseopplysninger nødvendig for forsvarlig hjelp |
| Pasientjournalloven § 6 | Databehandling nødvendig for å yte helsehjelp |
| Vegtrafikkloven / førerkortforskriften | Legen er pålagt å vurdere helsekrav for førerrett |
| GDPR art. 9 nr. 2 h | Behandling nødvendig for medisinsk diagnose eller helsetjenester |

### 7.2 Dataflyt

```
EPJ-system (behandlingsansvarlig for FHIR-data)
    │  FHIR-oppslag (read-only)
    ▼
Altinn-appen / BFF (databehandler under Digdir)
    │  Prefill — data i minne, aldri lagret i Altinn Storage
    ▼
Altinn-skjema → Helsedirektoratet / Statens vegvesen (behandlingsansvarlig)
```

### 7.3 Dataminimering

Appen skal **aldri** be om `MedicationStatement`, `AllergyIntolerance`, `Immunization`, `DiagnosticReport` eller andre ressurser utover det IS-2569 krever.

---

## 8. Kjøretøygrupper og kodeverk

| Kode | Beskrivelse |
|---|---|
| A–T | Kjøretøygrupper (A, B, BE, C1, C, D, S, T m.fl.) |
| ICD-10 | Diagnosekoder — `urn:oid:2.16.578.1.12.4.1.1.7110` |

---
---

# 2. Implementeringsdetaljer

# Implementeringsdetaljer og beste praksis
## SMART on FHIR + Altinn Studio — Legeerklæring førerrett

**Dato:** 2026-06-15
**Basert på:** PoC med Altinn App API 8.6.4, HAPI FHIR R4, SMART App Launch IG v2.2.0

---

## 1. Komponentoversikt

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
│ WSL2 / PODMAN                                       │
│  nginx :8000 | localtest :5101 | HAPI FHIR :8080   │
└─────────────────────────────────────────────────────┘
```

---

## 2. HAPI FHIR R4 (testserver)

Lokal testserver som simulerer EPJ-systemets FHIR API. Kjøres som container i Podman.

### Testdata (seed.ps1)

| Ressurs | ID | Innhold |
|---|---|---|
| `Patient` | `sophie-salt` | Fnr 01039012345 |
| `Patient` | `per-hansen` | Fnr 01054512345 |
| `Patient` | `anne-johansen` | Fnr 01065512345 |
| `Patient` | `kari-larsen` | Fnr 01075512345 |
| `Patient` | `olav-berg` | Fnr 01085512345 |
| `Practitioner` | `lege-ola` | HPR 1234567, fnr 01017512345 |
| `PractitionerRole` | `role-lege-ola` | Fastlege, allmennmedisin |
| `Organization` | `sandvika-legesenter` | Orgnr 987654321, HER-id 8765432 |
| `Encounter` | `enc-{pasient}-001` | Én per pasient |
| `Condition` | `cond-{pasient}-001` | ICD-10: R55 Synkope |

> **Bruk alltid `OlaNordmann` (UserId 12345) når du logger inn i Altinn Local Test.**

---

## 3. SMART Auth Mock

Node.js/Express-server (`local-dev/smart-mock/server.js`, port 9090).

| Endepunkt | Formål |
|---|---|
| `GET /.well-known/smart-configuration` | SMART discovery |
| `GET /auth` | Simulerer EPJ-innlogging, utsteder kode umiddelbart |
| `POST /token` | Veksler kode → access token med SMART-claims |
| `GET /epj` | EPJ-simulator med Digdir-design — velg pasient, start launch |

---

## 4. Port-oversikt

| Port | Tjeneste | Tilgjengelig fra |
|---|---|---|
| 8000 | nginx | Nettleser |
| 5005 | .NET App | nginx (via 172.30.80.1) |
| 5101 | Altinn Local Test | .NET App (via localhost) |
| 8080 | HAPI FHIR | .NET App, SMART Mock |
| 9090 | SMART Auth Mock | .NET App, nettleser |

**hosts-fil:** `127.0.0.1  local.altinn.cloud`

---

## 5. Altinn .NET App — nøkkelfiler

| Fil | Formål |
|---|---|
| `controllers/SmartLaunchController.cs` | SMART EHR Launch-flyt + `/smart/dev-login` |
| `services/FhirPrefillService.cs` | `IDataProcessor` — henter og mapper FHIR-data |
| `models/ForerLegeerklaeringModel.cs` | Datamodell |
| `config/process/process.bpmn` | «Signer og send inn»: én signing task (Task_1) |

### Token-livssyklus

```
EPJ utsteder token
    ↓
SmartLaunchController.Callback() → lagrer i session + IMemoryCache
    ↓
FhirPrefillService.ProcessDataRead() → leser session → FHIR-kall → prefyller skjema
    ├─ Token utløpt → tom prefill, legen fyller inn manuelt
    └─ Session utløpt (30 min) → ny SMART-launch nødvendig
```

### Program.cs — kritisk middleware-rekkefølge

```csharp
app.UseSession();                      // MÅ komme FØR
app.UseAltinnAppCommonConfiguration(); // Altinn-middleware
```

---

## 6. Beste praksis

| Tema | Regel |
|---|---|
| Session | Alltid `await session.LoadAsync()` før `session.GetString()` |
| AllowAnonymous | `[AllowAnonymous]` på hele `SmartLaunchController` — ellers redirect-loop |
| CookieSecurePolicy | `SameAsRequest` lokalt, `Always` i produksjon |
| Nginx URL-params | `://` strippes — bruk `DefaultIss`/`DefaultLaunch` i konfig som fallback |
| Token-lagring | Aldri i URL, localStorage, sessionStorage eller response-body |

---

## 7. Kjente fallgruver

| Symptom | Årsak | Løsning |
|---|---|---|
| Alle FHIR-felt tomme, ingen feil | `ProcessDataRead` ikke kalt | Sjekk at `IDataProcessor` er registrert i DI |
| "No SMART session found" | Session-cookie ikke sendt | Sjekk `SameSite`, `SecurePolicy`, `HttpOnly` |
| Connection refused port 8080 | Feil IP fra .NET-app | Bruk `localhost:8080` fra Windows-prosesser |
| ERR_TOO_MANY_REDIRECTS | Mangler `[AllowAnonymous]` | Legg til attributt på controller |
| Session tom etter LoadAsync | UseSession etter Altinn-middleware | Flytt `app.UseSession()` til FØR `UseAltinnAppCommonConfiguration()` |

---

## 8. Defensiv FHIR-henting

```csharp
private async Task<JsonDocument?> TryGetFhirResource(HttpClient client, string url, string name)
{
    try { return JsonDocument.Parse(await client.GetStringAsync(url)); }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
    { _logger.LogWarning("FHIR {Name} not found: {Url}", name, url); return null; }
    catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
    { _logger.LogWarning("FHIR {Name} access denied: {Url}", name, url); return null; }
    catch (Exception ex)
    { _logger.LogError(ex, "FHIR {Name} fetch failed: {Url}", name, url); return null; }
}
```

---

## 9. Teststrategi

| Miljø | Altinn | HelseID | FHIR | Testdata |
|---|---|---|---|---|
| **Lokalt** | Local Test :8000 | SMART mock | HAPI FHIR lokal | `seed.ps1` |
| **TT02** | tt02.altinn.no | HelseID test | EPJ-leverandørens testsystem | Tenor + SyntPop |
| **Produksjon** | altinn.no | HelseID prod | EPJ prod | Ekte data |

### TT02-overgang: sjekkliste

- [ ] Velg syntetisk lege i SyntPop med HPR-godkjenning og FLR-pasienter
- [ ] Finn pasient på legens liste (`GET /api/flr/doctor/{hpr_nummer}`)
- [ ] Verifiser Tenor-kompatibilitet for begge fnr
- [ ] Oppdater `seed.ps1` med Tenor-fnr
- [ ] Konfigurer HelseID TTT (`Pid`, `HprNumber`)
- [ ] Test Altinn TT02-innlogging via ID-porten test
- [ ] Deploy til TT02 med oppdatert `appsettings.json`

---

## 10. HelseID-integrasjon: kom i gang

```json
{
  "HelseID": {
    "Authority": "https://helseid-sts.test.nhn.no",
    "ClientId": "<din-klient-id>",
    "PrivateKeyJwk": "<din-private-nøkkel-som-JWK-json>"
  }
}
```

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

| HelseID-claim | Skjemafelt |
|---|---|
| `helseid://claims/identity/pid` | (synk-nøkkel med Altinn-sesjon) |
| `helseid://scopes/hpr/hpr_number` | `LegeHprNummer` |
| `name` | `LegeNavn` |

---
---

# 3. Skjemastruktur IS-2569

# Helseattest førerett — Skjemastruktur og feltanalyse
## Blankett IS-2569 (Helsedirektoratet, 22.05.2017)

**Juridisk hjemmel:** Førerkortforskriften vedlegg 1, helsepersonelloven § 4 og § 15

---

## Feltdekning i PoC

| Status | Antall felt |
|---|---|
| Dekket av FHIR-prefill | ~12 felt |
| Delvis dekket | 3 felt |
| Mangler i datamodellen | ~30 felt |

---

## Helsekategorier 1–16

For hver kategori krysser legen av Ja eller Nei.

| Kategori | Beskrivelse | Status |
|---|---|---|
| 1 | Enkel synstest (synsstyrke, synsfelt, synsfunksjon) | Mangler |
| 2 | Hørsel (gruppe 3) | Mangler |
| 3 | Kognitiv svekkelse | Mangler |
| 4 | Nevrologiske sykdommer | Mangler |
| 5 | Epilepsi eller epilepsilignende anfall | Mangler |
| 6 | Bevissthetstap av annen årsak | Mangler |
| 7 | Søvnsykdommer | Mangler |
| 8 | Hjerte- og karsykdommer | Mangler |
| 9 | Diabetes | Mangler |
| 10 | Psykiske lidelser | Mangler |
| 11 | Bruk av midler som påvirker kjøreevnen | Mangler |
| 12 | Respirasjonssvikt | Mangler |
| 13 | Nyresykdommer | Mangler |
| 14 | Svekket førlighet | Mangler |
| 15 | Andre sykdommer og helsesvekkelser | Mangler |
| 16 | Oppsummering spørsmål 2–15 | Mangler |

---

## Hva datamodellen dekker

```
ForerLegeerklaeringModel

Pasient_Fnr / Fornavn / Etternavn / Fodselsdato / Kjonn  ← FHIR Patient
Lege_HPR / Fornavn / Etternavn                           ← FHIR Practitioner
Virksomhet_Navn / Orgnr / HerId                          ← FHIR Organization
Konsultasjon_Dato                                        ← FHIR Encounter
Diagnose_Kode / Tekst                                    ← FHIR Condition
Forer_Kjoretoygruppe / ErSkikket / Merknad / Vilkar      ← Legen velger/fyller ut
```

### Hva som mangler for produksjonsklar løsning

1. Formål og førerkortgruppe (gruppe 1/2/3)
2. Helsekategoriene 1–16 (~22 Ja/Nei-felt)
3. Konklusjon per gruppe (12 felt + 4 varighetstall)
4. Vilkår — 6 faste avkrysninger

**Total estimert utvidelse:** fra 18 til ~70 felt.

---
---

# 4. Pasientflyt

# Pasientflyt: Egenerklæring og legeattestprosessen — førerrett

**Dato:** 2026-06-16
**Status:** Arkitekturforslag — utenfor PoC-scope, men nødvendig å adressere

---

## 1. Bakgrunn og problem

I dagens papirbaserte flyt fyller pasienten ut egenerklæring (NA-0201) på papir og medbringer den til konsultasjonen. Legen taster informasjonen manuelt inn i EPJ/skjema.

**Målet:** Pasienten fyller ut egenerklæringen digitalt *før* konsultasjonen. Legen ser svaret allerede prefylt når de åpner IS-2569 i Altinn-appen.

---

## 2. NA-0201 Del 2 — 17 helsespørsmål

| Nr | Spørsmål | Trigger |
|---|---|---|
| 1–2 | Syn | Synsattest |
| 3–17 | Nevrologisk, besvimelse, søvn, hjerte, diabetes, psykisk, ADHD, legemidler, rus, lunger, nyre, førlighet, andre | Helseattest IS-2569 |

---

## 3. Foreslått digital flyt

```
Pasient                    helsenorge.no / Altinn      EPJ / Altinn-app (lege)
   │── Bestiller time ──────────────────────────────►  │
   │                              │◄─ Dialogporten-dialog opprettes
   │◄─── Varsel (SMS/e-post) ─────│
   │── Fyller ut egenerklæring ───►│
   │── Signerer (ID-porten) ──────►│── Lagres som FHIR QuestionnaireResponse ──►│
   │                              │
   │                              │          ◄── SMART launch ─────────────────│
   │                              │          ◄── Henter QuestionnaireResponse ──│
   │                              │          Prefiller IS-2569, lege kompletter  │
   │                              │          Sender inn ───────────────────────►│ SVV/Hdir
```

---

## 4. Mapping: Egenerklæring → IS-2569

| Egenerklæring (NA-0201) | IS-2569 helsekategori |
|---|---|
| Spm. 1–2: Syn | Kategori 1: Syn |
| Spm. 4: Nevrologisk | Kategori 4: Nevrologiske sykdommer |
| Spm. 5–6: Besvimelse/epilepsi | Kategori 5–6: Epilepsi / bevissthetsforstyrrelser |
| Spm. 7: Søvnapné | Kategori 7: Søvnsykdommer |
| Spm. 8: Hjerte-/kar | Kategori 8: Hjerte- og karsykdommer |
| Spm. 9: Diabetes | Kategori 9: Diabetes |
| Spm. 10: Psykisk lidelse | Kategori 10: Psykiske lidelser |
| Spm. 12: Legemidler | Kategori 11: Legemidler |
| Spm. 13: Rus | Kategori 11: Misbruk av rusmidler |
| Spm. 15: Nyresvikt | Kategori 13: Nyresykdommer |
| Spm. 16: Førlighet | Kategori 14: Svekket førlighet |

---

## 5. Scope og prioritering

| Fase | Innhold |
|---|---|
| **PoC (nå)** | Papirflyt — pasienten medbringer NA-0201 på papir |
| **v1.0** | Digital egenerklæring i Altinn-app, manuell link-distribusjon |
| **v2.0** | Dialogporten-integrasjon: EPJ oppretter dialog ved timebestilling |
| **v3.0** | Fullstendig FHIR QuestionnaireResponse-flyt med samtykke og Tillitsrammeverk |

---
---

# 5. Åpne beslutninger

# Åpne beslutninger og uavklarte designvalg

---

## C-1: Hvem er «parten» i Altinn-instansen?

| Alternativ | Beskrivelse | Konsekvens |
|---|---|---|
| **A — Legen** | Legen er part og signatar | Enklest teknisk. Instansen lever i legens innboks. |
| **B — Pasienten** | Legen fyller ut på vegne av pasienten | Krever delegering. Mer korrekt juridisk. |
| **C — Legekontoret (org)** | Instansen tilhører virksomheten | Krever systemtilgang-autorisasjon. |

**Status:** Uavklart — PoC bruker alternativ A.

---

## C-2: HelseID — tokenvalidering i BFF

Konkrete oppgaver: `AddJwtBearer` mot HelseID JWKS, `pid`-claim-verifisering, `assurance_level >= high`, audit-logging av `hpr_number`.

**Status:** Klient registrert 2026-06-17 — unblokket. Kode mangler.

---

## C-3: Mottaksarkitektur — hvem er tjenesteeier?

| Alternativ | Beskrivelse |
|---|---|
| **A — Digdir** | Digdir eier tjenesten, tar imot via Altinn storage |
| **B — Statens vegvesen** | SVV som tjenesteeier og mottaker |
| **C — Helsedirektoratet** | Hdir som nasjonal koordinator |
| **D — EPJ (DocumentReference)** | Erklæringen skrives tilbake til pasientjournalen |

**Status:** Uavklart — PoC bruker Digdir som placeholder.

---

## C-4: Rettslig grunnlag og DPIA

Åpne spørsmål: Er Digdir behandlingsansvarlig eller databehandler? DPIA-krav (art. 35)? Databehandleravtale med EPJ-leverandør?

**Status:** Uavklart.

---

## C-5: Fullstendig IS-2569 og NA-0201

- PoC: Kun grunndata — ferdig
- v1.0: Full IS-2569 — krever feltmapping med Helsedirektoratet
- v2.0: Digital NA-0201 — krever ny avtale og tjenesteutvikling

**Status:** Utsatt til v1.0.

---

## C-6: Altinn Studio vs. Helsenorge — plattformvalg

NHN har allerede IS-2569 i produksjon på Helsenorge-plattformen.

| Alternativ | Fordeler | Ulemper |
|---|---|---|
| **A — Altinn Studio** | Signering, arkiv, XACML, ID-porten gratis | Mottaksarkitektur til SVV uavklart |
| **B — Helsenorge** | HelseID nativt; SVV-integrasjon; NHN i prod | Digdir er ikke eier |
| **C — Hybridmodell** | Altinn signering + Helsenorge innbyggerflyt | Mer kompleksitet |

NHNs Slack `ext-utv-hn-forerrett` tilbyr samarbeid.

**Status:** Åpen.

---

## C-7: DSOP + SMART on FHIR mot privat sektor (forsikring)

Forsikringsbransjen er stor mottaker av legeerklæringer. DSOP-rammeverket er etablert. Spørsmålet er om Digdir bør initiere en SMART on FHIR-flyt mot forsikring.

**Status:** Åpen. Se KARTLEGGING-kandidater.md G1/G2.

---

## Beslutningslogg

| Dato | Beslutning | Status |
|---|---|---|
| 2026-06-16 | PoC bruker legen som part (C-1 alt. A) | Midlertidig |
| 2026-06-17 | HelseID-klient registrert (C-2) | Unblokket |
| 2026-06-16 | Digdir som tjenesteeier-placeholder (C-3 alt. A) | Midlertidig |
| 2026-06-16 | DPIA krever juridisk ressurs (C-4) | Åpen |
| 2026-06-16 | Full IS-2569 utsettes til v1.0 (C-5) | Planlagt |
| 2026-06-17 | Altinn Studio vs. Helsenorge — plattformvalg (C-6) | Åpen |
| 2026-06-17 | DSOP + SMART on FHIR mot forsikring (C-7) | Åpen |

---
---

# 6. Veikart

# Veikart — `forer-legeerklaering` og SMART on FHIR på Altinn

**Sist oppdatert:** 2026-06-18

Dette veikart dekker **Spor A (Førerrett PoC)** og **Spor B (generisk plattform)**. Se [STRATEGI.md](docs/STRATEGI.md) for nasjonal roadmap og samarbeidsmodell.

---

## Overordnet retning

1. Løfte `forer-legeerklaering` til **produksjonsklar referansearkitektur** (Spor A — fase 1–3)
2. Ekstrahere det generiske laget som bibliotek andre etater kan bruke (Spor B — fase 4)

---

## Fase 1 — Produksjonsklar SMART-klient

| Tiltak | Beskrivelse |
|---|---|
| **Tokenvalidering** | Valider access token-signatur mot EPJ-ens JWKS-endepunkt |
| **Refresh-håndtering** | Bakgrunns-refresh i `FhirPrefillService` |
| **Issuer-allowlist** | Fyll `SmartOnFhir:AllowedIssuerList` med kjente norske EPJ-er |
| **Distribuert sesjon** | Bytt `AddDistributedMemoryCache()` med Redis/Valkey |
| **ERR_TOO_MANY_REDIRECTS** | Diagnostiser og løs redirect-loopen i full OAuth-flyt |

**Estimat:** 2–3 uker med tilgang til EPJ-testmiljø.

---

## Fase 2 — Writeback til EPJ

| Tiltak | Beskrivelse |
|---|---|
| **DocumentReference** | Skriv tilbake til EPJ med PDF-referanse etter innsending |
| **QuestionnaireResponse** | Strukturert skjemadata med kanonisk Questionnaire-URL |
| **Idempotens** | PUT med klient-tildelt id + GET-sjekk mot duplikater |

**Estimat:** 1–2 uker etter fase 1.

---

## Fase 3 — Testing

| Tiltak | Beskrivelse |
|---|---|
| **e2e røyktest** | Playwright: `dev-login` → prefill → utfylling → signering → arkivert |
| **Unit-test: FhirPrefillService** | Fixture-JSON for Patient/Practitioner/Encounter |
| **Unit-test: SmartLaunchController** | State-mismatch, issuer-allowlist, PKCE-generering |
| **Integrasjonstest: writeback** | DocumentReference + QuestionnaireResponse mot HAPI FHIR |

**Estimat:** 1–2 uker.

---

## Fase 4 — NuGet-pakke: `Digdir.SmartOnFhir`

```
Digdir.SmartOnFhir/
├── SmartLaunchHandler        # EHR Launch: discovery, PKCE, state, redirect
├── SmartCallbackHandler      # Authorization code → token exchange
├── SmartTokenStore           # Abstraksjon over sesjon/cache
├── FhirHttpClientFactory     # HttpClient med SMART token injisert
├── TokenValidator            # JWKS-validering av access token
├── SmartOptions              # Konfigurasjon: ClientId, AllowedIssuers, osv.
└── Extensions/AddSmartOnFhir()
```

**Estimat:** 2–3 uker for v0.1.

---

## Prioritert rekkefølge

```
Fase 1: SMART-klient (tokenvalidering, refresh, allowlist, sesjon)
    ↓
Fase 2: Writeback (DocumentReference + QuestionnaireResponse)
    ↓
Fase 3: Testing (e2e + unit + integrasjon)
    ↓
Fase 4: NuGet-pakke Digdir.SmartOnFhir
    ↓
Fase 5: Full IS-2569, HelseID, mottaksarkitektur (etter menneskelige avklaringer)
```

---

## Nasjonal satsing (etter fase 4)

```
Fase 1–3: Produksjonsklar PoC (Spor A)
    ↓
Fase 4: NuGet-pakke Digdir.SmartOnFhir (Spor B initiell)
    ↓
Nasjonal fase 3: Pilot med NAV / Helfo / Helsedirektoratet
    ↓
Nasjonal fase 4: SMART som standard integrasjonsmønster i Altinn
```

Se [STRATEGI.md](docs/STRATEGI.md) for fullstendig beskrivelse.

---
---

# 7. Sammenligning

# Sammenligning: `forer-legeerklaering` vs. `syk-inn` vs. NHN Førerrett-App

> **Viktig kontekst:** NHN Førerrett-App er produksjonsimplementasjonen av *nøyaktig det samme domenet* — IS-2569 for Statens vegvesen.

---

## 1. Kort oppsummering

| | `forer-legeerklaering` | `syk-inn` | NHN Førerrett-App |
|---|---|---|---|
| Domene | Legeerklæring førerrett (IS-2569) | Sykmelding til NAV | Legeerklæring førerrett (IS-2569) |
| Eier | Digitaliseringsdirektoratet | NAV | NHN / Helsenorge |
| Plattform | Altinn Studio / .NET 8 | Next.js / React / TypeScript | Helsenorge-plattformen |
| Status | PoC gjennomført | Produksjon | Produksjon |
| SMART-implementasjon | Håndskrevet i én controller | `@navikt/smart-on-fhir`-pakke | NHN-plattform |
| Autentisering | Altinn/ID-porten (HelseID planlagt) | HelseID via Wonderwall | HelseID (obligatorisk) |
| IS-2569-dekning | ~4 av 17+ helsekategorier | N/A | Komplett med betinget logikk |
| Egenerklæring (NA-0201) | Planlagt | N/A | Implementert |
| Writeback til EPJ | Ikke implementert | Ja | Ja |
| Automatiserte tester | Ingen | 23 unit + 24 Playwright e2e | Ukjent |

---

## 2. Plattformvalg — det viktigste skillet

| Plattform | App | Hva plattformen gir gratis |
|---|---|---|
| **Altinn Studio** | `forer-legeerklaering` | Signering (BPMN), arkiv, PDF, XACML, ID-porten |
| **Helsenorge** | NHN Førerrett-App | HelseID, skjemakatalog, innbyggerportal, SVV-integrasjon, NA-0201 |
| **Egenutviklet (NAIS)** | `syk-inn` | Full kontroll, Valkey, NAV-regelmotor |

---

## 3. Sikkerhet

| Tema | `forer` | `syk-inn` | NHN Førerrett-App |
|---|---|---|---|
| Token forlater nettleser | Nei (BFF) ✔ | Nei (BFF) ✔ | Nei (antatt) ✔ |
| PKCE + state | Ja, 256-bit ✔ | Ja ✔ | Ja ✔ |
| Tokenvalidering | **Nei** (erkjent gap) | Ja ✔ | Ja (HelseID) ✔ |
| Refresh token | Etterspørres, ikke håndtert | `autoRefresh` ✔ | Ukjent |
| Issuer-allowlist | **Tom** | Kjente servere per miljø ✔ | HelseID only ✔ |
| Audit-logging | Ikke implementert | OTel + Loki ✔ | Påkrevd per NHN-guide ✔ |

---

## 4. Konklusjon

- **`forer-legeerklaering`** beviser at Altinn Studio kan brukes som helseskjemaplattform med FHIR-prefill.
- **`syk-inn`** er malen for produksjonsklar SMART on FHIR-klient i .no-sektoren.
- **NHN Førerrett-App** er produksjonsimplementasjonen av nøyaktig det samme domenet på Helsenorge-plattformen.

Strategisk spørsmål: Er Altinn Studio rett plattform for IS-2569, eller skal Digdir bidra til NHNs eksisterende løsning? Se BESLUTNINGER.md C-6.

---
---

# 8. NHN-dokumentasjon

# NHN-dokumentasjon — SMART App Launch + Førerrett-App

**Kilde:** Helsenorge Confluence | **Hentet:** 2026-06-17

---

## 1. Implementasjonsguide SMART App Launch Framework

NHNs guide dekker **Use Case 4**: tredjepartsapplikasjoner for helsepersonell startet fra EPJ — nøyaktig PoC-ens brukstilfelle.

### Autorisasjonsflyt

Identisk med PoC-ens implementasjon: EPJ åpner app med `iss` + `launch` → SMART-discovery → redirect til autorisasjonsendepunkt → kode → token-utveksling server-side → FHIR-kall med Bearer-token.

### Sikkerhetskrav

| Krav | Detalj |
|---|---|
| Transport | TLS påkrevd |
| `state`-parameter | Minimum 122-bit entropi, bundet til brukersesjon |
| Konfidensielle klienter | HTTP Basic auth (`client_id:client_secret`) |
| Autorisasjonskoder | Kort levetid (~1 minutt) |
| Audit-logging | Påkrevd for alle tilgangsbeslutninger |
| Risikovurdering | Påkrevd før implementasjon |

PoC-en bruker `RandomNumberGenerator.GetBytes(32)` = 256 bit for `state` — tilfredsstiller kravet.

---

## 2. Smart-On-FHIR Førerrett-App (NHNs produksjonsapp)

### Kjernefunksjoner

- Automatisk datafylling fra EPJ via FHIR
- Betinget beslutningslogikk for alle helsekategorier
- Beslutningsstøtte fra Førerkortveileder i sanntid
- Egenerklæring (NA-0201) — legen ser pasientens selvrapporterte svar
- Elektronisk innsending til Statens vegvesen

### Outputs

| Dokument | Mottaker |
|---|---|
| Journalnotat | Arkiveres i EPJ |
| Legekopi (PDF) | Legen |
| Borgerkopi (PDF) | Innbygger via Helsenorge |
| Elektronisk innsending | Statens vegvesen (automatisk) |

### Teknisk plattform

- HelseID obligatorisk
- Helsenorge-plattformen
- «Smart register» — leger må registreres før tilgang gis
- Kontakt: Slack `ext-utv-hn-forerrett`

---

## 3. Nøkkelobservasjoner

NHNs guide **bekrefter** at vår SMART EHR Launch-implementasjon er korrekt og i tråd med norsk sektornorm.

**Strategisk implikasjon:** NHNs løsning og Digdirs PoC er **komplementære, ikke konkurrerende**. NHN bruker Helsenorge; Digdir bruker Altinn Studio. Rollefordelingen bør avklares — se BESLUTNINGER.md C-6.

---
---

# 9. Kartlegging

# Kartlegging av rapporteringsplikter for helsepersonell

Grunnlag for vurdering av forenklingspotensial. Versjon per juni 2026.

**SoF-verdi:** Antatt verdi av en SMART on FHIR-app: **Høy** / Middels / Lav

---

## A. Sentrale / nasjonale helseregistre

| ID | Oppgave | Mottaker | Innsamling | SoF-verdi |
|---|---|---|---|---|
| A1 | Norsk pasientregister (NPR) | Helsedirektoratet | Automatisk | Lav |
| A2 | Kommunalt pasient- og brukerregister (KPR) | FHI | Delvis | Middels |
| A3 | Medisinsk fødselsregister (MFR) | FHI | Automatisk | Lav |
| A4 | Dødsmelding / Dødsårsaksregisteret | FHI | Automatisk | Lav |
| A5 | MSIS – smittsom sykdom | FHI + kommunelege | Delvis | Middels |
| A7 | Tuberkuloseregister | FHI + TB-koordinator | Manuell | Middels |
| A10 | Kreftregisteret (klinisk melding) | Kreftregisteret | Delvis | **Høy** |
| A11 | Hjerte- og karregisteret (HKR) | FHI | Delvis | Middels |

## B. Medisinske kvalitetsregistre

| ID | Oppgave | Innsamling | SoF-verdi |
|---|---|---|---|
| B1 | Nasjonale medisinske kvalitetsregistre (~50+) | Delvis | **Høy** |

## C. Hendelser, skader og svikt

| ID | Oppgave | Innsamling | SoF-verdi |
|---|---|---|---|
| C1 | Varsel om alvorlig hendelse | Manuell | Middels |
| C2 | Bivirkningsmelding legemiddel | Manuell | **Høy** |
| C3 | Hendelse med medisinsk utstyr | Manuell | Middels |
| C4 | Pasientskade – tilleggsinfo til NPE | Manuell | Middels |

## D. Meldeplikt til andre myndigheter

| ID | Oppgave | Mottaker | Innsamling | SoF-verdi |
|---|---|---|---|---|
| D1 | Melding/opplysninger til barnevernet | Kommunal barneverntjeneste | Manuell | Middels |
| D4 | Førerkort: helseattest IS-2569 | Statens vegvesen | Manuell | **Høy** (er pilot) |
| D5 | Førerkort: melding om manglende helsekrav | Statsforvalteren | Manuell | **Høy** |
| D6 | Melding om arbeidsrelatert sykdom (154B) | Arbeidstilsynet | Manuell | **Høy** |
| D7 | Helseerklæring arbeidsdykking | Arbeidstilsynet | Manuell | Middels |
| D9 | Sertifikatattester (sjøfart, luftfart, jernbane) | Diverse tilsyn | Manuell | Middels |

## E. NAV / trygd / refusjon

| ID | Oppgave | Innsamling | SoF-verdi |
|---|---|---|---|
| E1 | Sykmelding | Automatisk | Middels |
| E2 | Legeerklæringer/uttalelser (L-takster) | Delvis | **Høy** |
| E7 | Blåreseptsøknad / individuell stønad | Delvis | Middels |

## F. Attester til kommune, fylke, utdanning og dagligliv

| ID | Oppgave | Innsamling | SoF-verdi |
|---|---|---|---|
| F1 | TT-kort (tilrettelagt transport) | Manuell | **Høy** |
| F2 | HC-kort (parkering forflytningshemmede) | Manuell | **Høy** |
| F3 | Legeerklæring ved skolefravær | Manuell | Middels |
| F4 | Sykmelding/nedsatt funksjonsevne for studenter (Lånekassen) | Manuell | **Høy** |

## G. Privat sektor – forsikring og andre

| ID | Oppgave | Innsamling | SoF-verdi |
|---|---|---|---|
| G1 | Erklæring ved forsikringstegning | Manuell | **Høy** |
| G2 | Erklæring til erstatnings-/skadesak | Manuell | **Høy** |
| G3 | Sakkyndig-/behandlererklæring | Manuell | Middels |

---

## SMART on FHIR — topp-kandidater

| ID | Oppgave | Hvorfor høy verdi |
|---|---|---|
| D4/D5 | Førerkort-attest og -melding | Allerede SoF-pilot; mal for resten |
| D6 | Arbeidsrelatert sykdom (154B) | Papir/post i dag; kan forhåndsutfylles |
| B1 | Medisinske kvalitetsregistre | Største kilde til dobbeltregistrering |
| A10 | Kreftregisteret (klinisk melding) | Strukturert melding, høyt dobbeltarbeid |
| C2 | Bivirkningsmelding | Kan forhåndsutfylles fra legemiddelliste |
| E2 | NAV-legeerklæringer | Strukturert Questionnaire-flyt; prototype finnes |
| F1/F2/F4 | TT-kort, HC-kort, Lånekassen | Strukturerte attester, manuelle og spredt |
| G1/G2 | Forsikringserklæringer | Stort privat volum; krever samtykke |

---

## DSOP + SMART on FHIR mot privat sektor

DSOP er en etablert modell der finansnæringen og offentlige etater digitaliserer dataflyt. Forsikringsbransjen er allerede stor mottaker av legeerklæringer via Helsenettet.

**Anbefalt rekkefølge:** Førerrett-mønsteret (D4) → offentlige attester (D6, E2, F1/F2/F4) → DSOP-basert privat flyt (G1/G2) når samtykke- og tillitstjenestene er modne.

Se BESLUTNINGER.md C-7.

---
---

# 10. Strategi

# Strategi — SMART on FHIR for Altinn Studio

**Sist oppdatert:** 2026-06-18
**Eier:** Digitaliseringsdirektoratet (Digdir)

---

## Tre nøkkelbudskap

### 1 — SMART on FHIR fungerer i Altinn Studio
Bevist gjennom legeerklæring for førerrett: FHIR-prefill, SMART EHR Launch, BFF-mønster, Altinn-signering — alt fungerer i et lokalt testmiljø.

### 2 — Løsningen kan generaliseres til mange helseskjemaer
Samme arkitektur kan brukes for sykmelding, attester, refusjonskrav, henvisninger og alle andre skjemaer der legen allerede har dataene i EPJ-et.

### 3 — Neste steg er et samarbeid mellom Digdir, NHN, Helsedirektoratet og EPJ-leverandørene
Målet er et felles integrasjonsmønster — ikke konkurrerende løsninger.

---

## Produktvisjon

> **Altinn Studio som generisk SMART on FHIR-plattform for offentlig sektor** — der tjenesteeiere kan bygge helsefaglige skjemaer med automatisk prefill fra EPJ, uten å bygge SMART-integrasjonen selv hver gang.

Dette er det egentlige prosjektet. Legeerklæring for førerrett er demonstrasjonscaset.

---

## To spor

### Spor A — Førerrett PoC

**Formål:** Bevise at Altinn Studio fungerer som SMART on FHIR-klient.

**Suksesskriterier:** FHIR-prefill → IS-2569 utfylt og signert → innsending → writeback til EPJ.

**Status:** FHIR-prefill, signering og dev-innlogging verifisert. Se VEIKART.md for gjenstående arbeid.

**Avgrensning:** Spor A er caset — ikke plattformen.

---

### Spor B — Altinn Health Integration Framework

**Formål:** Gjøre Altinn Studio til en generisk SMART on FHIR-plattform for tjenesteeiere.

**Hva dette handler om:** Sykmelding, spesialistattest, refusjonskrav (Helfo), NAV-helseskjemaer, kommunale helsetjenesteskjemaer, spesialisthenvisning.

**Leveranse:** NuGet-pakken `Digdir.SmartOnFhir` (VEIKART.md fase 4).

**Avhengighet:** Spor B starter etter at Spor A har produsert et stabilt, produksjonsklar SMART-klient.

---

## Samarbeidsmodell

| Aktør | Eier og leverer |
|---|---|
| **Digdir** | Altinn Studio-komponenter, SMART-wrapper (`Digdir.SmartOnFhir`), referanseimplementasjon, Altinn-integrasjon |
| **NHN** | HelseID, HelseAPI, tillitsrammeverk, sikker EPJ-tilkobling |
| **EPJ-leverandører** | SMART EHR Launch, FHIR API, klientregistrering i HelseID |
| **Tjenesteeiere** | Skjemadefinisjon, forretningsregler, mottaksarkitektur |

---

## Gevinsthypotese

| Gevinst | Estimat | Begrunnelse |
|---|---|---|
| Redusert registreringstid for legen | 20–50 % | Data hentes fra EPJ — ingen dobbeltregistrering |
| Færre feil i skjemadata | Høy | Strukturert FHIR-data vs. manuell inntasting |
| Gjenbruk av Altinn-infrastruktur | Høy | Signering, arkiv, PDF, tilgangsstyring — ferdig |
| Leverandøruavhengighet | Høy | Åpne standarder (FHIR R4, SMART App Launch IG) |
| Nye skjemaer uten ny SMART-implementasjon | Betydelig | Spor B: én pakke for alle Altinn-apper |
| Redusert tid til ny skjematjeneste | Estimert 60–80 % | vs. grønn-mark-implementasjon |

*Tallene er hypoteser — ikke målte resultater. Gevinstmåling bør inngå i en pilot (nasjonal roadmap fase 3).*

---

## Nasjonal roadmap

| Fase | Innhold | Leveranse |
|---|---|---|
| **1 — Referanseimplementasjon** | Førerrett PoC produksjonsklar: tokenvalidering, refresh, writeback, testing | Spor A ferdig. Digdir alene. |
| **2 — Generisk SMART-komponent** | `Digdir.SmartOnFhir` publisert, integrasjonsguide for EPJ-leverandører | Spor B initiell. Digdir + 1–2 EPJ-leverandører. |
| **3 — Pilotsamarbeid** | Pilot med NAV/Helfo/Helsedirektoratet. HelseID aktivert. Gevinstmåling. | Andre skjematype på plattformen. |
| **4 — Nasjonal standard** | SMART som anbefalt mønster i Altinn Studio. Mottaksarkitektur og juridisk avklart. | Alle fire aktørgrupper involvert. |

---

## Strategisk risiko og posisjonering

**Risiko:** NHN har allerede IS-2569 i produksjon på Helsenorge-plattformen. Initiativet kan oppfattes som konkurrerende.

**Posisjonering:** Dette er ikke en konkurrent til NHN Førerrett-App. Initiativet handler om Altinn Studio som plattform.

**Budskapet til NHN:**
> Vi bygger ikke en bedre førerrett-app. Vi bygger infrastrukturen som gjør at tjenesteeiere som ikke har Helsenorge-plattformen tilgjengelig, kan bruke Altinn Studio med FHIR-prefill — og at EPJ-leverandører bare trenger én standardintegrasjon.

---

## Åpne strategiske spørsmål

| ID | Spørsmål | Blokkerer |
|---|---|---|
| C-6 | Altinn Studio vs. Helsenorge-plattformen — rollefordeling | Nasjonal fase 3 |
| C-3 | Mottaksarkitektur — hvem er tjenesteeier? | Nasjonal fase 3 |
| C-4 | Rettslig grunnlag og DPIA | Nasjonal fase 3 |
| C-7 | DSOP + SMART on FHIR mot privat sektor | Nasjonal fase 4 |
