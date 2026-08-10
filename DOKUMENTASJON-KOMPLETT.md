# forer-legeerklaering — Samlet dokumentasjon

**Generert:** 2026-08-10
**Kilde:** `docs/` — rekkefølge etter tabell i README.md. Generert av `docs/generate-samlet-dokumentasjon.ps1` — kjør skriptet på nytt etter endringer i docs/*.md, rediger ikke denne filen direkte.

---

## Innholdsfortegnelse

1. [Kravspesifikasjon v0.6](#1-kravspesifikasjon-v06)
2. [Implementeringsdetaljer](#2-implementeringsdetaljer)
3. [Skjemastruktur IS-2569](#3-skjemastruktur-is-2569)
4. [Pasientflyt](#4-pasientflyt)
5. [Åpne beslutninger](#5-åpne-beslutninger)
6. [Risikoregister](#6-risikoregister)
7. [Veikart](#7-veikart)
8. [Sammenligning: forer vs. syk-inn vs. NHN Førerrett-App](#8-sammenligning-forer-vs-syk-inn-vs-nhn-førerrett-app)
9. [NHN-dokumentasjon](#9-nhn-dokumentasjon)
10. [Kartlegging av rapporteringsplikter](#10-kartlegging-av-rapporteringsplikter)
11. [Strategi](#11-strategi)

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

Dette er eksplisitt innenfor scope for PoC og v1:

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
| — | Samtykke-/tilgangsmodell |
| — | Multifaktor-autentisering i EPJ |

**Uten denne listen** vil prosjektet typisk bli dratt mot SMART Backend Services, CDS Hooks og FHIR Write — som alle er separate integrasjonsprosjekter.

### 2.2 Suksesskriterier

PoC er vellykket når følgende er demonstrert ende-til-ende:

| Kriterium | Målbar definisjon |
|---|---|
| SMART EHR Launch | Legen starter appen fra EPJ med `iss` + `launch`-parameter; OAuth-flyten fullføres uten manuell innlogging i FHIR |
| Pasientdata prefylt | Navn og fødselsnummer er automatisk utfylt fra `Patient`-ressurs |
| Legedata prefylt | HPR-nummer og legens navn er automatisk utfylt fra `Practitioner`-ressurs |
| Konsultasjonskontekst | Minst én klinisk ressurs (`Encounter` eller `Condition`) er hentet og brukt |
| Innsending | Skjemaet kan fylles ut og sendes inn via Altinn; PDF-kvittering genereres |
| Feiltoleranse | Dersom én FHIR-ressurs mangler, åpnes skjemaet med tomme felt — ikke feilmelding |

**Akseptansetest:** PoC er bestått mot minimum én SMART-kompatibel server — `SMARTHealthIT Sandbox` i automatisert test, og lokal HAPI FHIR + SMART Auth Mock i manuell test.

---

## 3. Aktører

| Aktør | Rolle | System |
|---|---|---|
| Lege (fastlege) | Fyller ut og sender inn skjema | EPJ + Altinn |
| EPJ-system | Starter SMART-launch, tilbyr FHIR API | CGM, Infodoc, Pridok, WebMed, Aspit — *fastlege-EPJ-ene* (DIPS Arena er sykehus-EPJ og ikke relevant for denne bruksflyten) |
| Altinn Platform | Infrastruktur for skjema, signering, arkiv | Altinn 3 |
| Altinn-appen | Applikasjonslaget — SMART-integrasjon + skjema | .NET 8 / Altinn Studio |
| Statens vegvesen | Juridisk mottaker av konklusjonen | Altinn Events → SVVs mottakssystem |
| EPJ (writeback) | Mottaker av full attest | FHIR DocumentReference |

**Merk om EPJ-modenhet:** Fastlege-EPJ-er har varierende og til dels emergent FHIR-/SMART-modenhet. Modenheten drives nasjonalt gjennom **EPJ-løftet**. Påstandene om at «SMART on FHIR fungerer» gjelder mot lokal HAPI FHIR + SMART-mock — ikke mot reelle fastlege-EPJ-er. Verifikasjon mot minst én leverandørs FHIR-sandkasse gjenstår.

---

## 4. Arkitektur

### 4.1 Overordnet prinsipp

```
EPJ → SMART Launch → Altinn App → FHIR API → Prefill → Skjema → Innsending
                                                                      │
                                        ┌─────────────────────────────┤
                                        │                             │
                              Konklusjon (grønt/rødt)       Full attest
                              → SVV via Altinn Events        → EPJ via DocumentReference
```

**Nøkkelprinsipp:** FHIR brukes **utelukkende til forhåndsutfylling**. Altinns datamodell og innsendingsmekanisme er uendret. Access token lagres **aldri** i nettleseren — kun server-side i ASP.NET Core session.

**BFF-mønster (Backend For Frontend) — dette er arkitekturvalget:** Altinn-appen fungerer som konfidensielt klient og mellomlag. All token-utveksling og alle FHIR-kall skjer server-side. Nettleseren ser aldri access token og gjør aldri direkte FHIR-kall.

Dette er ikke ett av flere alternativer — det er det eneste valget i dette prosjektet. En browser-direkte-FHIR-tilnærming (f.eks. via `fhirclient-js`) er **avvist** fordi:
- SMART-tokenet ville ligget eksponert i nettleseren
- Det er ikke mulig å beskytte `client_secret` i en SPA
- Loggings- og audit-krav forutsetter server-side kontroll

Se: [Arkitekturoversikt](./arkitektur-oversikt.svg) | [Sekvensdiagram](./smart-launch-sekvens.svg) | [Nettverksruting](./nettverksruting.svg)

### 4.2 Autentisering — to separate lag

Løsningen har to uavhengige autentiseringssystemer som kjører parallelt:

| Lag | System | Formål |
|---|---|---|
| Altinn-autentisering | ID-porten / JWT | Legen som Altinn-bruker (person) |
| SMART-autentisering | EPJ OAuth2 / SMART | Tilgang til FHIR-ressurser i EPJ |

Disse to er **ikke** koblet — Altinn vet ikke om SMART-tokenet, og EPJ vet ikke om Altinn-innloggingen.

### 4.3 Dataflyt

1. EPJ omdirigerer legens nettleser til `/smart/launch?iss=...&launch=...`
2. Appen oppdager EPJs SMART-konfigurasjon (`/.well-known/smart-configuration` eller CapabilityStatement fra `GET /fhir/metadata`)
3. PKCE-utfordring genereres; nettleser sendes til EPJs `/auth`-endepunkt
4. EPJ utsteder autorisasjonskode; nettleser sendes til `/smart/callback`
5. Appen veksler kode mot token **server-side** (POST til EPJs `/token`)
6. Token + FHIR-kontekst (patientId, encounterId, fhirUser) lagres i server-session. `fhirUser` leveres per SMART-spec som eget felt i tokenresponsen; noen EPJ-systemer returnerer det i stedet som JWT-claim i access_token
7. Nettleser videresendes til Altinn-skjemaet
8. Altinn-frontend henter datamodell → `IDataProcessor.ProcessDataRead` kalles
9. `FhirPrefillService` leser session, kaller FHIR API, fyller ut datamodellen
10. Lege kontrollerer prefylt skjema, fyller ut gjenværende felt, sender inn

---

## 5. Datamodell

### 5.1 Feltgrupper og FHIR-kilde

| Feltgruppe | Felt | FHIR-ressurs | Norsk OID / element |
|---|---|---|---|
| **Pasient** | Fnr | Patient.identifier | `urn:oid:2.16.578.1.12.4.1.4.1` |
| | Fornavn | Patient.name[0].given[0] | — |
| | Etternavn | Patient.name[0].family | — |
| | Fødselsdato | Patient.birthDate | — |
| | Kjønn | Patient.gender | — |
| **Lege** | HPR-nummer | Practitioner.identifier | `urn:oid:2.16.578.1.12.4.1.4.4` |
| | Fornavn | Practitioner.name[0].given[0] | — |
| | Etternavn | Practitioner.name[0].family | — |
| **Lege (foretrukket)** | HPR + organisasjon + rolle | PractitionerRole → Practitioner + Organization | — |
| **Virksomhet** | Organisasjonsnummer | Organization.identifier | `urn:oid:2.16.578.1.12.4.1.4.101` |
| | HER-id | Organization.identifier | `urn:oid:2.16.578.1.12.4.1.2` |
| | Navn | Organization.name | — |
| **Konsultasjon** | Dato | Encounter.period.start | — |
| | Organisasjon | Encounter.serviceProvider.reference → Organization | — |
| **Diagnose** | Kode | Condition.code.coding[0].code | ICD-10 |
| | Tekst | Condition.code.coding[0].display | — |
| **Erklæring** | Kjøretøygruppe | (lege velger) | Altinn options |
| | Er skikket | (lege velger) | Boolean |
| | Vilkår | (lege fyller ut) | Fritekst |
| | Merknad | (lege fyller ut) | Fritekst |

**Merknad om PractitionerRole:** NAV krever `no-basis-PractitionerRole` som obligatorisk ressurs for sin sykmeldingsapplikasjon (syk-inn). PractitionerRole kobler legen direkte til sin rolle og organisasjon, og er mer robust enn å hente organisasjon via `Encounter.serviceProvider`. PoC-en bruker Practitioner + Encounter-kjeden; for produksjon anbefales PractitionerRole som primærkilde.

### 5.2 To separate datamodeller — konklusjon og full attest

Innsendingen bruker **to datatyper** i `applicationmetadata.json`:

| Datatype-id (`applicationmetadata.json`) | Klasse | Mottaker | Innhold |
|---|---|---|---|
| `ForerLegeerklaering` | `Altinn.App.Models.ForerLegeerklaeringModel` | EPJ (DocumentReference writeback, ikke implementert ennå) | Alle felt — komplett IS-2569 |
| `ForerKonklusjon` | `Altinn.App.Models.ForerKonklusjonModel` | Statens vegvesen via Altinn Events | Gruppe 1/2/3: skikket/ikke skikket + begrenset varighet + vilkår |

`ForerKonklusjonModel` populeres automatisk i `FhirPrefillService.ProcessDataWrite` ved innsending (upsert via `IDataClient`), avledet fra `ForerLegeerklaeringModel`. SVVs mottakssystem henter kun `ForerKonklusjon`-datatypen.

```csharp
// ForerKonklusjonModel.cs — faktisk implementasjon (src/App/models/, commit 38057a4)
public class ForerKonklusjonModel
{
    public string Pasient_Fnr { get; set; }
    public string Lege_HPR { get; set; }
    public string Gruppe1_Resultat { get; set; } // "skikket" | "ikke_skikket" | "begrenset" | ""
    public int?   Gruppe1_BegrensetAntallAar { get; set; }
    public string Gruppe2_Resultat { get; set; }
    public int?   Gruppe2_BegrensetAntallAar { get; set; }
    public string Gruppe3_Resultat { get; set; }
    public int?   Gruppe3_BegrensetAntallAar { get; set; }
    public string Vilkar { get; set; }
    public string Merknad { get; set; }
}
```

**Kjent forenkling:** `ForerLegeerklaeringModel` har i dag ett enkelt `Forer_Kjoretoygruppe`-felt og én `Forer_ErSkikket`-boolean. `DeriveKonklusjon()` mapper koden til riktig gruppe (gruppe 1: A/A1/A2/AM/B/B1/BE/S/T, gruppe 2: C/C1/CE/C1E, gruppe 3: D/D1/DE/D1E, se `FhirPrefillService.cs`) og setter *kun* den ene gruppens resultat — de to andre `GruppeX_Resultat`-feltene forblir tomme. Uavhengige per-gruppe-vurderinger (slik IS-2569 i prinsippet krever) er ikke støttet før `ForerLegeerklaeringModel` utvides — se [BESLUTNINGER.md C-5](BESLUTNINGER.md) (full IS-2569, planlagt v1.0).

### 5.3 Klassereferanse

```
Altinn.App.Models.ForerLegeerklaeringModel   — komplett skjema
Altinn.App.Models.ForerKonklusjonModel       — konklusjon til SVV
Namespace: Altinn.App.Models
XmlRoot: ForerLegeerklaering / ForerKonklusjon
```

---

## 6. SMART on FHIR — tekniske krav

### 6.1 Støttede flows

- **EHR Launch** (primær): EPJ starter appen med `iss` + `launch`
- **Standalone Launch** (ikke implementert): appen starter selvstendig

### 6.2 Scopes som kreves

```
openid profile fhirUser launch launch/patient launch/encounter offline_access
patient/Patient.read patient/Encounter.read patient/Condition.read patient/Observation.read
user/Practitioner.read user/Organization.read user/PractitionerRole.read
```

**Begrunnelse for `user/`-prefix på behandlerdata:** `patient/`-scopes gir kun tilgang til ressurser knyttet til den aktuelle pasienten i launch-konteksten. `Practitioner`, `Organization` og `PractitionerRole` tilhører *behandleren*, ikke pasienten — disse forespørres med `user/`-prefix. Pasientrelaterte ressurser (`Patient`, `Encounter`, `Condition`, `Observation`) forespørres med `patient/`.

Scopes er konfigurert i `SmartLaunchController.cs` og sendes som `scope`-parameter i authorize-requesten. Appen **skal ikke be om mer enn det som trengs** (prinsippet om minste privilegium). Dersom EPJ-et avviser et scope (returnerer `scope`-felt i tokenresponsen som er smalere enn forespurt), må appen degradere elegant — se seksjon om feilhåndtering i IMPLEMENTERING.md.

**Leverandørvariasjoner:** Ikke alle EPJ-systemer støtter alle scopes. Kjente utfordringer:

| Scope | Status i norske EPJ-er |
|---|---|
| `launch/encounter` | Ikke støttet av alle — kan mangle Encounter i token |
| `user/PractitionerRole.read` | Varierer — noen eksponerer `PractitionerRole`, andre bare `Practitioner` |
| `fhirUser` | Returneres enten som token-felt eller JWT-claim (se implementeringsguiden) |
| `offline_access` | Gir refresh token — EPJ-støtte varierer |

### 6.3 Sikkerhetskrav

- PKCE påkrevd (S256)
- Konfidensielt klient — `client_secret` brukes ved token-utveksling
- Token lagres **kun** server-side (ASP.NET Core session med `HttpOnly`, `SameSite=Lax`)
- `iss`-validering mot tillatt-liste (`AllowedIssuerList` i konfig)
- State-parameter for CSRF-beskyttelse

### 6.4 EPJ-variasjoner og kompatibilitetsstrategi

SMART App Launch-spesifikasjonen standardiserer launch-flyt og autentisering — **ikke** hvilke FHIR-ressurser som finnes, hvilke profiler som brukes, eller hvilke søk som støttes. Dette er den største praktiske risikoen i et multi-EPJ-scenario.

**Kjente norske EPJ-systemer og forventet SMART-støtte:**

| EPJ | SMART App Launch | FHIR R4 | HelseID-integrert | Encounter | PractitionerRole |
|---|---|---|---|---|---|
| DIPS Arena | Ja (eget SMART-lag) | R4 | Planlagt | Ja | Delvis |
| CGM Allmennlegesystem | Ukjent | Ukjent | Ukjent | Ukjent | Ukjent |
| Infodoc Plenario | Ukjent | Ukjent | Ukjent | Ukjent | Ukjent |
| Pridok | Ukjent | Ukjent | Ukjent | Ukjent | Ukjent |

> DIPS Arena er sykehus-EPJ og tatt med som teknisk referanse (mest dokumenterte SMART-lag i Norge) — ikke fordi det er relevant EPJ for denne, fastlegebaserte, bruksflyten. De tre øvrige er de aktuelle fastlege-EPJ-ene; SMART-modenheten deres er ikke kartlagt (jf. [KARTLEGGING-kandidater.md](KARTLEGGING-kandidater.md)).

**Strategi for håndtering av variasjoner:**

1. **CapabilityStatement-sjekk** (se seksjon 4.3, punkt 2): Les `GET /fhir/metadata` ved oppstart og tilpass oppførsel
2. **Ressursfallback**: Hvis `Encounter` mangler i token — forsøk søk via `Patient/$everything` eller `Encounter?patient={id}&status=in-progress`
3. **Profil-toleranse**: Ikke anta `identifier[0]` er fnr — søk etter OID `2.16.578.1.12.4.1.4.1` eksplisitt
4. **Graceful degradation**: Manglende ressurser betyr tomme felt, ikke feil — legen fyller inn manuelt

**NAVs sertifiseringsmodell:** NAV sertifiserer EPJ-er mot sine krav og vedlikeholder en liste over godkjente EPJ-systemer og versjoner. Applikasjonen sjekker ved oppstart om EPJ-et er sertifisert. Dette er et mønster Digdir/Helsedirektoratet bør vurdere for førerretterklæringen — alternativt å gjenbruke NAVs sertifiserte EPJ-liste som utgangspunkt.

**Det viktigste arkitekturspørsmålet:** Hvilken konkret EPJ er første integrasjonsmål? Arkitekturen er moden nok — den største gjenværende risikoen er hva DIPS, CGM, Infodoc eller Pridok faktisk eksponerer av FHIR R4 i praksis.

### 6.5 SMART App Lifecycle

Klientregistrering mot EPJ-leverandøren er ikke en engangsoperasjon:

| Hendelse | Konsekvens | Håndtering |
|---|---|---|
| Ny scope kreves | Ny klientregistrering eller re-consent | Planlegg scope-lista konservativt |
| Ny redirect URI | Oppdatering hos EPJ-leverandør (kan ta uker) | Fryse URI-er tidlig i prosjektet |
| Ny EPJ-versjon | SMART-server-URL kan endre seg | Bruk discovery (`/.well-known/smart-configuration`), ikke hardkod |
| Klienthemmelighet roteres | Koordinert deploy av ny `client_secret` | Secrets via Azure Key Vault, ikke konfig-fil |

**Registreringsprosess mot norske EPJ-leverandører** er per i dag manuell og leverandørspesifikk. Det finnes ingen nasjonal klientregistry (per 2026). Tidlig dialog med EPJ-leverandør er kritisk.

### 6.6 SSO for lege: HelseID og Altinn

Legene er allerede innlogget i EPJ via **HelseID** (NHNs OpenID Connect-leverandør for helsepersonell). Spørsmålet er: kan vi gjenbruke HelseID-sesjonen i Altinn, slik at legen slipper dobbel innlogging?

#### Nøkkelinnsikt: HelseID bruker ID-porten som rot-identitetstjeneste

**HelseID autentiserer selve brukeren via ID-porten.** HelseID er et lag på toppen — det legger til helsefaglige claims (HPR-nummer, organisasjon, assurance-nivå), men den underliggende identiteten er ID-porten-verifisert. Konsekvensen er:

```
Lege i EPJ:     ID-porten → HelseID → EPJ (token med pid + hpr_number)
Lege i Altinn:  ID-porten → Altinn  (token med pid)
                               ↑
                          Samme pid (fnr) — samme person
```

**SSO skjer naturlig gjennom den felles tillitskjeden.** Ingen ny plattformavtale mellom Digdir og NHN er nødvendig for at konsumenten (vår BFF) skal validere HelseID-tokens. NHN bekrefter at testmiljøet er tilgjengelig uten formell avtale — man logger inn med ID-porten og avgjør selv tillitsnivå.

#### HelseID tekniske egenskaper

Fra `https://helseid-sts.test.nhn.no/.well-known/openid-configuration`:

| Egenskap | Verdi |
|---|---|
| Issuer (test) | `https://helseid-sts.test.nhn.no` |
| Issuer (prod) | `https://helseid-sts.nhn.no` |
| Relevante claims | `helseid://claims/identity/pid` (fnr), `helseid://scopes/hpr/hpr_number` |
| Grant types | `authorization_code`, `refresh_token`, `token-exchange` (RFC 8693) |
| Auth method | `private_key_jwt` (RS256/PS256/ES256) |
| DPoP | Påkrevd i produksjon |
| TestIDP | Tilgjengelig i testmiljø — simulerer brukerinnlogging uten ekte testbrukere |

#### Arkitektur: BFF validerer HelseID-token uavhengig

Vår BFF trenger ikke Altinn-plattformen for å forstå HelseID. BFF-en validerer tokenet direkte mot HelseID JWKS:

```
EPJ SMART launch
      │
      ▼
BFF (SmartLaunchController)
  ├─ Mottar SMART access token fra EPJ
  ├─ Validerer token mot HelseID JWKS:
  │     https://helseid-sts.test.nhn.no/.well-known/openid-configuration
  ├─ Ekstraherer pid (fnr) → sammenligner med Altinn-sesjon
  ├─ Ekstraherer hpr_number → prefill HPR-felt i skjema
  └─ Lagrer i ASP.NET Core session (BFF-mønster)

Altinn-sesjon:
  └─ Lege logger inn via ID-porten (standard Altinn-flyt)
        pid = 01017512345 ← same fnr som HelseID-token
```

#### Testing uten formell avtale

NHN tilbyr testmiljø på `https://selvbetjening.test.nhn.no/` — tilgjengelig uten leverandøravtale:

1. **Logg inn** med ID-porten på `https://selvbetjening.test.nhn.no/`
2. **Registrer klient** — oppgi redirect URI for din app, velg scopes
3. **Generer JWK-nøkkelpar** — `private_key_jwt` brukt som client assertion
4. **Bruk TestIDP** — simulerer brukerinnlogging; velg testperson med HPR-nummer
5. **Klienteksempler** — [NorskHelsenett/HelseID.Samples](https://github.com/NorskHelsenett/HelseID.Samples) på GitHub med ferdigkonfigurerte testklienter

#### Claims som er relevante for legeerklæringen

| Claim | Type | Verdi (eksempel) | Bruk i skjema |
|---|---|---|---|
| `helseid://claims/identity/pid` | string | `01017512345` | Synkronisering med Altinn-sesjon |
| `helseid://scopes/hpr/hpr_number` | string | `1234567` | Prefill HPR-nummer |
| `helseid://claims/identity/assurance_level` | string | `high` | Validering av identitetsnivå |
| `name` / `family_name` | string | `Nordmann` | Prefill legens navn |

#### Scopes som må forespørres

```
openid
profile
helseid://scopes/identity/pid
helseid://scopes/hpr/hpr_number
helseid://scopes/identity/assurance_level
```

#### Feide-mønsteret vs. HelseID-mønsteret

| | Feide (udir) | HelseID (vår tilnærming) |
|---|---|---|
| `AppOidcProvider` | `"feide"` | Ikke nødvendig |
| Altinn-integrasjon | Krever Digdir/Sikt-avtale + UIDP | Ingen — BFF validerer direkte |
| Identitetsrot | Feide/OIDC | ID-porten (via HelseID) |
| SSO-mekanisme | Altinn-plattform | Felles `pid` (fnr) |
| Status | Registrert i Altinn | Mulig uten ny avtale |

Feide-mønsteret er altså ikke riktig sammenligningsgrunnlag. Vår tilnærming er enklere: BFF-en validerer HelseID-tokenet selv og bruker `pid`-claimen som bindeledd til Altinn-sesjonen.

#### NAVs posisjon

Fra NAVs "SMART on FHIR i ny sykmelding"-presentasjon (Standardiseringsutvalg 2-25): HelseID er ønsket langsiktig, men ikke inkludert i piloten. NAV bruker per nå ID-porten + EPJ SMART som separate kanaler. Vår tilnærming med BFF-sidig HelseID-validering er et realistisk mellomsteg mot full HelseID-integrasjon.

### 6.7 Åpne problemer

| Problem | Status | Prioritet |
|---|---|---|
| ERR_TOO_MANY_REDIRECTS ved full OAuth-flyt | Uløst — workaround: `/test-prefill` | Høy |
| DocumentReference writeback til EPJ | Ikke implementert | Medium |
| HelseID-token validering i BFF | Ikke implementert — neste steg | Høy |
| Issuer-allowlist er tom (ingen validering) | Konfig-gap | Høy (prod) |
| DPoP-støtte i HelseID prod | Krever DPoP-implementasjon i BFF | Medium (prod) |

---

## 7. Personvern og behandlingsgrunnlag

### 7.1 Behandlingsgrunnlag

Løsningen behandler helseopplysninger — en særskilt kategori personopplysninger etter GDPR art. 9. Behandlingsgrunnlaget er:

| Hjemmel | Grunnlag |
|---|---|
| **Helsepersonelloven § 45** | Legen kan innhente helseopplysninger som er nødvendige for å yte forsvarlig hjelp |
| **Pasientjournalloven § 6** | Databehandling er nødvendig for å yte helsehjelp |
| **Vegtrafikkloven / førerkorforskriften** | Legen er pålagt å vurdere helsekrav for førerrett |
| **GDPR art. 9 nr. 2 h** | Behandling er nødvendig for medisinsk diagnose eller yting av helsetjenester |

Pasienten gir eksplisitt samtykke i egenerklæringens signatur: *«Når det er krav om helseattest, gir jeg legen fullmakt til å innhente nødvendige og relevante helseopplysninger fra spesialist og tidligere fastlege uavhengig av taushetsplikt.»*

### 7.2 Dataflyt og behandlingsansvar

```
EPJ-system (behandlingsansvarlig for FHIR-data)
    │
    │  FHIR-oppslag (read-only, begrenset til konsultasjonskontekst)
    ▼
Altinn-appen / BFF (databehandler under Digdir)
    │
    │  Prefill av skjema — data i minne, aldri lagret i Altinn Storage
    ▼
Altinn-skjema (databehandler under Digdir)
    │
    │  Innsending av utfylt legeerklæring
    ▼
Helsedirektoratet / Statens vegvesen (behandlingsansvarlig for skjema)
```

**FHIR-data lagres ikke i Altinn.** Data hentes fra EPJ, brukes til å prefylle skjemaets datamodell i minnet, og forkastes. Det er kun legens ferdig utfylte IS-2569-skjema som arkiveres i Altinn.

### 7.3 Dataminimering

| Ressurs | Felt som hentes | Felt som ikke hentes |
|---|---|---|
| `Patient` | Navn, fødselsnummer, adresse | Diagnosehistorikk, legemidler, journalnotater |
| `Practitioner` | Navn, HPR-nummer | Privatadresse, personal-ID |
| `Organization` | Navn, orgnr, HER-id | Intern organisasjonsstruktur |
| `Encounter` | Dato, type | Behandlingsbeskrivelse, notater |
| `Condition` | ICD-10-kode, dato | Kliniske noter, behandlingsplan |

Appen skal **aldri** be om `MedicationStatement`, `AllergyIntolerance`, `Immunization`, `DiagnosticReport` eller andre ressurser utover det IS-2569 krever.

### 7.4 Logging og sporbarhet

Alle FHIR-oppslag skal kunne spores. Minimum audit-informasjon per oppslag:

| Felt | Beskrivelse |
|---|---|
| Tidspunkt | ISO 8601 |
| Legens HPR-nummer | Fra SMART-token / FHIR Practitioner |
| FHIR-ressurstype | Patient, Practitioner, osv. |
| HTTP-statuskode | 200, 404, 403... |
| SMART-issuer | EPJ-ets `iss`-verdi |

Fnr og andre direkte identifikatorer logges **ikke** i klartekst — kun hashes eller referanser.

### 7.5 Hva som ikke er avklart i PoC

Følgende personvernspørsmål krever avklaring av Helsedirektoratet, NHN og Digdir — og er eksplisitt **ikke** løst i PoC:

- Formell databehandleravtale mellom EPJ-leverandør og Digdir
- Risikovurdering (DPIA) for hele dataflyten
- Lagringstid for Altinn-arkivert legeerklæring
- Passering av helseopplysninger gjennom Altinns infrastruktur — er Digdir databehandler eller behandlingsansvarlig?
- Tilgangsstyring: kan alle Altinn-brukere med lege-rolle se alle legeerklæringer?

---

## 8. Digdir-kapabiliteter og helsesektorens tilsvarende løsninger

Altinn Studio bygger på Digdirs fellesløsninger. Nedenfor beskrives tre sentrale kapabiliteter, deres relevans for dette prosjektet, og hva helsesektoren tilbyr av tilsvarende mekanismer.

### 7.1 Maskinporten

**Hva:** OAuth2 `client_credentials`-løsning for system-til-system-kommunikasjon uten brukerinvolvering. En virksomhet autentiserer seg med virksomhetssertifikat og får et access token som gir tilgang til et API på vegne av virksomheten.

**Relevans for dette prosjektet:** Potensiell bruk dersom EPJ-leverandøren krever at Altinn-appen er pre-registrert og autentisert som godkjent API-konsument — i tillegg til SMART-tokenet som identifiserer brukeren. I PoC er dette ikke implementert; SMART dekker autentiseringen.

**Helsesektorens tilsvarende løsning:** HelseID `client_credentials` grant type. Systemautentisering uten bruker, innenfor NHNs tilgangsstyring. Konseptet er identisk med Maskinporten, men administreres av NHN og er begrenset til godkjente helsevirksomheter. EPJ-leverandører bruker dette for system-til-system-kall mot NHNs register-APIer.

### 7.2 Altinn Autorisasjon

**Hva:** Altinns tilgangsstyringssystem for roller, rettigheter og delegering. Kontrollerer hvem som kan gjøre hva på vegne av hvem: en lege som handler på vegne av en pasient, en legesekretær delegert til å sende inn skjemaer, osv.

**Relevans for dette prosjektet:** Direkte relevant i produksjon — Altinn Autorisasjon bør brukes til å kontrollere at legen har rett til å representere pasienten og sende inn legeerklæringen. Spørsmål som må avklares: hvilken Altinn-rolle kreves, og hvordan delegeres rettigheter fra pasient til lege?

**Helsesektorens tilsvarende løsninger:** To parallelle mekanismer:

| Mekanisme | Beskrivelse |
|---|---|
| **HPR-autorisasjon** | Hvem er autorisert helsepersonell og for hvilke handlinger. Kontrollerer *hvem som er lege*, ikke hvem som har lov til å representere en bestemt pasient. |
| **NHN Tillitsrammeverk** | Nasjonalt rammeverk for datadeling i helsesektoren. Definerer hvem som har tilgang til hvilke helsedata basert på tjenstlig behov, behandlingsrelasjon og formål. Bæres som claims i HelseID-token: `PractitionerAuthorizationCode`, `CareRelationshipPurposeOfUseCode`, `CareRelationshipHealthcareServiceCode`. |

Tillitsrammeverket er i praksis helsesektorens svar på Altinn Autorisasjon for kliniske data. I en komplett produksjonsflyt håndheves begge: Altinn Autorisasjon kontrollerer skjemainnsending, Tillitsrammeverk kontrollerer FHIR-datatilgang.

**Koblingen mellom de to:** Claims fra Tillitsrammeverk (behandlingsrelasjon, formål) bør følge med i HelseID-tokenet slik at EPJ kan ta en informert tilgangsbeslutning basert på om det foreligger et reelt behandlingsforhold.

### 7.3 Ressursregisteret

**Hva:** Altinns register over beskyttede ressurser og tjenester. En app eller et API registreres som en "ressurs" som Altinn Autorisasjon kan beskytte. Kobler ressurser til tilgangsregler og gjør dem synlige i Altinn-økosystemet.

**Relevans for dette prosjektet:** Produksjonssteg — "legeerklæring for førerrett" bør registreres som en ressurs i Ressursregisteret slik at Altinn Autorisasjon kan håndheve tilgangsstyringen formelt. Ikke nødvendig for PoC.

**Helsesektorens tilsvarende løsning:** NHNs selvbetjeningsportal (`selvbetjening.nhn.no`) — EPJ-leverandører registrerer sine API-er og konsumenter søker tilgang. Ikke identisk, men samme prinsipp: en sentral katalog over beskyttede ressurser med tilgangsstyring.

### 7.4 Oppsummering og prioritering

| Digdir-løsning | Tilsvarende i helsesektoren | Relevant for PoC | Relevant for prod |
|---|---|---|---|
| Maskinporten | HelseID `client_credentials` | Nei | Vurder — avhenger av EPJ-leverandørens krav |
| Altinn Autorisasjon | HPR-autorisasjon + NHN Tillitsrammeverk | Nei | Ja — tilgangsstyring for innsending |
| Ressursregisteret | NHN selvbetjening API-katalog | Nei | Ja — formaliser ressursen |

**Viktig observasjon:** Altinn Autorisasjon og NHN Tillitsrammeverk dekker ulike lag av samme problem. Et produksjonssystem trenger begge: Altinn for å autorisere *skjemainnsendingen*, NHN Tillitsrammeverk for å autorisere *FHIR-datauthentingen* fra EPJ. De er komplementære, ikke konkurrerende.

---

## 9. Kjøretøygrupper (kodeverk)


Altinn options-fil: `options/kjoretoygrupper.json`

| Kode | Beskrivelse |
|---|---|
| A | Motorsykkel |
| B | Personbil |
| BE | Personbil med tilhenger |
| C1 | Lett lastebil |
| C1E | Lett lastebil med tilhenger |
| C | Lastebil |
| CE | Lastebil med tilhenger |
| D1 | Minibuss |
| D | Buss |
| S | Snøscooter |
| T | Traktor |

---

## 10. Nasjonal profilstrategi

SMART App Launch standardiserer launch og autentisering. FHIR-profiler standardiserer data. **Dette er to separate problemer** — og interoperabilitet krever begge.

### 8.1 Norske basisprofiler (no-basis)

HL7 Norge vedlikeholder basisprofiler for FHIR R4 som norske systemer forventes å følge:

| Profil | Brukt i PoC | Beskrivelse |
|---|---|---|
| `NoBasisPatient` | Ja | fnr via OID `2.16.578.1.12.4.1.4.1` |
| `NoBasisPractitioner` | Ja | HPR-nummer via OID `2.16.578.1.12.4.1.4.4` |
| `NoBasisOrganization` | Ja | Orgnr via OID `2.16.578.1.12.4.1.4.101`, HER-id via `.4.2` |
| `NoBasisEncounter` | Delvis | Bruker standard R4-Encounter |
| `NoBasisCondition` | Nei | ICD-10-kodeverk benyttes, men profil ikke validert |

### 8.2 HelseAPI og nasjonal infrastruktur

| Initiativ | Relevans |
|---|---|
| **HelseAPI** (NHN) | Nasjonal FHIR-gateway. Sannsynlig fremtidig integrasjonspunkt i stedet for direktekobling mot EPJ. |
| **HelseID** (NHN) | Nasjonal identitetstjeneste for helsepersonell. Potensielt erstatning/supplement til EPJ-eget SMART-lag. Støtter PKCE og OpenID Connect. |
| **Pasientens legemiddelliste (PLL)** | Eksempel på nasjonal FHIR-tjeneste via HelseAPI — viser mønsteret som er i ferd med å etableres. |

### 8.3 Førerrett-spesifikke profiler

Per 2026 finnes det **ingen nasjonal FHIR-profil for legeerklæring førerrett**. Dette PoC-et bruker generiske no-basis-profiler. Anbefalingen for produksjonssetting er:

1. Definere en `NoFoerTrafikkLegeerklaeringComposition`-profil (basert på Composition eller Bundle)
2. Registrere i Simplifier.net under HL7 Norway
3. Koordinere med Helsedirektoratet (tjenesteeier) og Statens vegvesen (mottaker)

### 8.4 Kodeverk

| Kodeverk | OID / system | Brukt til |
|---|---|---|
| ICD-10 | `urn:oid:2.16.578.1.12.4.1.1.7110` | Diagnosekoder (Condition) |
| Kjøretøygrupper | (lokal) | Altinn options |
| Kjønn (administrativt) | `http://hl7.org/fhir/administrative-gender` | Patient.gender |

---

## 11. Referanser

### Standarder

| Dokument | Versjon | URL |
|---|---|---|
| SMART App Launch Implementation Guide | v2.2.0 | https://hl7.org/fhir/smart-app-launch/ |
| HL7 FHIR R4 | 4.0.1 | https://hl7.org/fhir/R4/ |
| OAuth 2.0 (RFC 6749) | — | https://datatracker.ietf.org/doc/html/rfc6749 |
| PKCE (RFC 7636) | — | https://datatracker.ietf.org/doc/html/rfc7636 |

### Norske referanser

| Dokument | URL |
|---|---|
| Norske FHIR-basisprofiler (no-basis R4) | https://hl7norway.github.io/basisprofil-no-R4/ |
| Volven OID-register (fnr, HPR, orgnr, HER-id) | https://www.ehelse.no/kodeverk-og-terminologi/OID |
| Norm for informasjonssikkerhet i helse og omsorg | https://www.ehelse.no/normen |

### Altinn

| Dokument | URL |
|---|---|
| Altinn Studio dokumentasjon | https://docs.altinn.studio/ |
| app-localtest (lokalt testmiljø) | https://github.com/Altinn/app-localtest |

### Relaterte implementasjoner

- **NAV syk-inn**: NAVs SMART on FHIR-applikasjon for ny digital sykmelding. Autoritært referansedokument for norsk SMART on FHIR mot EPJ-leverandører. Bruker samme arkitektur: BFF, konfidensielt klient, EHR Launch, server-side FHIR-kall.
  - Krav til EPJ-leverandører: https://github.com/navikt/syk-inn/blob/main/docs/fhir/nav_requirements.md
  - FHIR-ressursoversikt: https://github.com/navikt/syk-inn/blob/main/docs/fhir/_oversikt.md
- **SMARTHealthIT**: Referanseimplementasjon og sandbox for SMART App Launch

---

## 12. Endringslogg

| Versjon | Dato | Endring |
|---|---|---|
| v0.1 | 2026-05 | Første utkast — konsept og scope |
| v0.2 | 2026-05 | Datamodell og FHIR-mapping |
| v0.3 | 2026-05 | Arkitekturdiagram v0.3 |
| v0.4 | 2026-05 | SMART-flyt og sekvensdiagram |
| v0.5 | 2026-06 | Sikkerhetskrav og kjøretøygrupper |
| **v0.6** | **2026-06-15** | PoC-resultater innarbeidet. Kjente begrensninger dokumentert. SVG-diagrammer oppdatert. Nettverksruting lagt til. |
| **v0.6.1** | **2026-06-15** | BFF-mønster presisert. CapabilityStatement lagt til i discovery-flyt. fhirUser-forbehold (JWT-claim vs. tokenfelt) innarbeidet. |
| **v0.6.2** | **2026-06-15** | Eksplisitt MVP-avgrensning (seksjon 2.1). Scope governance og EPJ-variasjonstrategi (seksjon 6.2/6.4). SMART App Lifecycle (6.5). Nasjonal profilstrategi og HelseAPI/HelseID (seksjon 8). |
| **v0.6.3** | **2026-06-16** | PractitionerRole lagt til som foretrukket FHIR-ressurs (seksjon 5.1). NAVs sertifiseringsmodell for EPJ dokumentert (seksjon 6.4). Referanser oppdatert med navikt/syk-inn. Kilde: NAV Standardiseringsutvalg møte 2-25. |
| **v0.6.4** | **2026-06-16** | HelseID SSO-analyse lagt til (seksjon 6.6): tre alternativer (AppOidcProvider, token exchange, ID-porten fallback). Bekreftet at HelseID ikke er registrert som Altinn OIDC-leverandør per juni 2026. |
| **v0.6.5** | **2026-06-16** | SSO-analyse revidert etter møte med NHN: HelseID bruker ID-porten som identitetsrot — SSO skjer via felles `pid`-claim uten ny plattformavtale. BFF-sidig tokenvalidering er riktig tilnærming. TestIDP og selvbetjening.test.nhn.no dokumentert. |
| **v0.6.6** | **2026-06-16** | Ny seksjon 7: Digdir-kapabiliteter (Maskinporten, Altinn Autorisasjon, Ressursregisteret) med helsesektorens tilsvarende løsninger (HelseID client_credentials, HPR-autorisasjon, NHN Tillitsrammeverk). Seksjoner renummerert. |
| **v0.6.7** | **2026-06-16** | Ny fil `PASIENTFLYT.md`: arkitekturforslag for digital egenerklæring (NA-0201) med Dialogporten og helsenorge.no. Fullstendig feltstruktur for egenerklæringen. Mapping NA-0201 → IS-2569 helsekategorier. Faseplan v1.0–v3.0. |
| **v0.6.8** | **2026-06-16** | Fem forbedringer basert på ekstern gjennomgang: (A) Scope-korreksjon — `user/PractitionerRole.read` lagt til, begrunnelse for `user/` vs `patient/` dokumentert. (B) Suksesskriterier — ny seksjon 2.2 med 6 målbare kriterier og akseptansetest. (C) Eksplisitt BFF-valg — browser-direkte FHIR avvist med begrunnelse i seksjon 4. (D) Ny seksjon 7 Personvern og behandlingsgrunnlag — hjemmel, dataflyt, dataminimering, logging, åpne spørsmål. (E) Token-livssyklus — refresh, utløp, session timeout i IMPLEMENTERING. Seksjoner renummerert til 12. |


---

# 2. Implementeringsdetaljer

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
14. [HelseID-integrasjon: kom i gang](#14-helseid-integrasjon-kom-i-gang)
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

Produksjonsmiljøet krever DPoP (Demonstrating Proof-of-Possession) — et ekstra lag som binder access token til nøkkelpar og forhindrer replay-angrep. HelseID.Samples-repoet viser implementasjon. DPoP er ikke nødvendig i testmiljøet.

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


---

# 3. Skjemastruktur IS-2569

# Helseattest førerett — Skjemastruktur og feltanalyse
## Blankett IS-2569 (Helsedirektoratet, 22.05.2017)

**Kilde:** [Helseattest førerett (PDF)](https://www.helsedirektoratet.no/veiledere/forerkortveileder/dokumenter-forerkortveileder/Helseattest%20f%C3%B8rerett.pdf/_/attachment/inline/661710e2-a13e-4591-8a29-3c6dc2fe9fd2:051bc49cd8af2d7aba25f9c2c13d6ea601328d36/Helseattest%20f%C3%B8rerett.pdf)  
**Juridisk hjemmel:** Førerkortforskriften vedlegg 1 — Helsekrav, helsepersonelloven § 4 og § 15  
**Merk:** Helseattesten skrives ut til søker, som tar den med til trafikkstasjonen. Den må ikke være eldre enn 3 måneder.

---

## Feltdekning i PoC

| Status | Antall felt |
|---|---|
| Dekket av FHIR-prefill | ~12 felt |
| Delvis dekket | 3 felt |
| Mangler i datamodellen | ~30 felt |

PoC-en dekker FHIR-prefill-delen (pasient, lege, virksomhet, diagnose) fullt ut. Selve legeattestdelen — helsekategoriene 1–16 og konklusjonen — er ikke implementert i datamodellen. Se [implementeringsstatus](#implementeringsstatus) nederst.

---

## Side 1–2: Helseattest (utfylles av lege)

### Søkers personopplysninger

| Felt | Type | FHIR-kilde | Status i PoC |
|---|---|---|---|
| Etternavn, fornavn og mellomnavn | Tekst | `Patient.name` | Delvis — mellomnavn mangler eget felt |
| Fødselsnummer | Tekst | `Patient.identifier` (OID `2.16.578.1.12.4.1.4.1`) | Dekket |

### Legens tilknytning

| Felt | Type | Utfylles av | Status i PoC |
|---|---|---|---|
| Er søkers fastlege | Avkrysning | Lege | Mangler |
| Eventuell annen tilknytning (vikar, behandlende spesialist o.l.) | Fritekst | Lege | Mangler |

### Identitetsbekreftelse

| Felt | Type | Utfylles av | Status i PoC |
|---|---|---|---|
| Søkers identitet er kjent fra tidligere | Avkrysning | Lege | Mangler |
| Det er forevist akseptabel legitimasjon med navn, fødselsnummer/D-nummer og bilde | Avkrysning | Lege | Mangler |
| Jeg har lest søkers egenerklæring om helse | Avkrysning | Lege | Mangler |

### Helseattesten gjelder (formål)

Legen krysser av for hva attesten skal brukes til. Én eller flere kan velges:

| Alternativ | Type | Status i PoC |
|---|---|---|
| Førerkort første gang | Avkrysning | Mangler |
| Utvidelse | Avkrysning | Mangler |
| Fornyelse | Avkrysning | Mangler |
| Innbytte av utenlandsk førerkort | Avkrysning | Mangler |
| Tilbakelevering | Avkrysning | Mangler |
| Utrykningskompetanse | Avkrysning | Mangler |
| Kjøreseddel for drosje inntil 8 passasjerer | Avkrysning | Mangler |
| Kjøreseddel for buss | Avkrysning | Mangler |
| Godkjenning som trafikklærer | Avkrysning | Mangler |
| Godkjenning som førerprøvesensor | Avkrysning | Mangler |

### Førerkortgruppe

| Alternativ | Beskrivelse | Status i PoC |
|---|---|---|
| Gruppe 1 | Personbil, motorsykkel, moped | Delvis — PoC bruker kjøretøykoder (A, B osv.), ikke gruppe 1/2/3 |
| Gruppe 2 | Lastebil, buss (tung) | Delvis |
| Gruppe 3 | Drosje, buss (lett), utrykningskjøretøy | Delvis |

---

## Helsekategorier 1–16

For hver kategori krysser legen av Ja eller Nei. Dersom én eller flere av spørsmål 2–15 besvares med Ja, må spørsmål 16 også besvares. Alle konklusjoner og begrunnelser dokumenteres i søkers journal (journalforskriften § 8 bokstav p).

### 1. Enkel synstest
*(Forskriften og Veilederen lenket i original blankett)*

#### A. Synsstyrke

| Felt | Høyre øye | Venstre øye | Begge øyne | Status i PoC |
|---|---|---|---|---|
| Uten korreksjon | Tall | Tall | — | Mangler |
| Med korreksjon | Tall | Tall | — | Mangler |
| Korreksjonens styrke | Tall | Tall | — | Mangler |

#### B. Synsfelt

| Felt | Type | Status i PoC |
|---|---|---|
| Har søker normalt synsfelt vurdert ved Donders metode når begge øyne er i bruk? | Ja/Nei | Mangler |

#### C. Synsfunksjon

| Felt | Type | Status i PoC |
|---|---|---|
| Har søker en svekkelse av synsfunksjon som gjør vurdering av optiker eller øyelege nødvendig? | Ja/Nei | Mangler |

> **Merknad:** Dersom søker har dobbeltsyn, tap/reduksjon av synet på ett øye, problemer med mørke/vekslende lys, mistanke om nedsatt sidesyn eller progressiv øyesykdom, skal synsfunksjoner vurderes av optiker/øyelege (Helseattest førerett – syn, blankett IS-2571) før denne attesten skrives ut, eller attesten gis med forbehold om godkjent synsattest.

---

### 2. Hørsel *(gjelder bare førerkortgruppe 3)*

| Felt | Type | Status i PoC |
|---|---|---|
| Har søker en hørselssvekelse som medfører at talestemme ikke oppfattes på 4 meters avstand? | Ja/Nei | Mangler |

> Dersom hørselshjelpemiddel er nødvendig for førerett i gruppe 3, angis dette under vilkår i konklusjonen.

---

### 3. Kognitiv svekkelse

| Felt | Type | Status i PoC |
|---|---|---|
| Foreligger det en tilstand med kognitiv svekkelse som kan gi økt trafikksikkerhetsrisiko? | Ja/Nei | Mangler |

---

### 4. Nevrologiske sykdommer

| Felt | Type | Status i PoC |
|---|---|---|
| Har søker svekket balanse, koordinasjon eller psykomotoriske funksjoner som medfører økt trafikksikkerhetsrisiko? | Ja/Nei | Mangler |

---

### 5. Epilepsi eller epilepsilignende anfall

| Felt | Type | Status i PoC |
|---|---|---|
| a) Har søker eller har søker hatt epilepsi eller epilepsilignende anfall? | Ja/Nei | Mangler |
| b) Bruker eller har søker brukt anfallsforebyggende legemidler mot epilepsi innenfor siste 10 år? | Ja/Nei | Mangler |

---

### 6. Bevissthetstap og bevissthetsforstyrrelser av annen årsak

| Felt | Type | Status i PoC |
|---|---|---|
| Har søker hatt bevissthetstap eller bevissthetsforstyrrelse av annen årsak enn epilepsi, hjerte-/karsykdom eller diabetes? | Ja/Nei | Mangler |

---

### 7. Søvnsykdommer

| Felt | Type | Status i PoC |
|---|---|---|
| Har søker, eller har søker hatt, påtrengende søvnighet eller ukontrollerbar søvn som kan utgjøre en trafikksikkerhetsrisiko? | Ja/Nei | Mangler |

---

### 8. Hjerte- og karsykdommer

| Felt | Type | Status i PoC |
|---|---|---|
| Har eller har søker hatt hjerte- og karsykdom med fare for plutselig innsettende bevissthetspåvirkning? | Ja/Nei | Mangler |

---

### 9. Diabetes

| Felt | Type | Status i PoC |
|---|---|---|
| a) Har søker diabetes? | Ja/Nei | Mangler |
| b) Har søker følgetilstander av diabetes som kan gi økt trafikksikkerhetsrisiko? | Ja/Nei | Mangler |
| c) Bruker søker insulin eller andre legemidler som kan gi hypoglykemi? | Ja/Nei | Mangler |

---

### 10. Psykiske lidelser eller svekkelser

| Felt | Type | Status i PoC |
|---|---|---|
| Har søker psykisk lidelse eller svekkelse som medfører trafikksikkerhetsrisiko? | Ja/Nei | Mangler |

---

### 11. Bruk av midler som kan påvirke kjøreevnen

| Felt | Type | Status i PoC |
|---|---|---|
| Bruker eller har søker brukt alkohol, rusmidler eller legemidler i et omfang og på en måte som medfører økt trafikksikkerhetsrisiko? | Ja/Nei | Mangler |

---

### 12. Respirasjonssvikt

| Felt | Type | Status i PoC |
|---|---|---|
| Har søker en helsetilstand som gir risiko for pO2 lavere enn 7,3 kPa og/eller pCO2 høyere enn 6,7 kPa? | Ja/Nei | Mangler |

---

### 13. Nyresykdommer

| Felt | Type | Status i PoC |
|---|---|---|
| Har søker alvorlig kronisk nyresvikt, behov for dialyse eller har det vært utført nyretransplantasjon? | Ja/Nei | Mangler |

---

### 14. Svekket førlighet

| Felt | Type | Status i PoC |
|---|---|---|
| a) Mangler søker tilstrekkelig førlighet til trafikksikker føring av motorvogn? | Ja/Nei | Mangler |
| b) Hvis Ja på 14a: Er tilstanden stabil? | Ja/Nei | Mangler |

---

### 15. Andre sykdommer og helsesvekkelser

| Felt | Type | Status i PoC |
|---|---|---|
| Har fører annen eller generell helsesvekkelse, eventuelt flere sykdommer samtidig, der svekket helsetilstand utgjør en risiko for trafikksikkerheten? | Ja/Nei | Mangler |

---

### 16. Oppsummering av spørsmålene 2–15

*Besvares kun hvis ett eller flere av spørsmålene 2–15 er besvart med Ja.*

| Felt | Type | Status i PoC |
|---|---|---|
| Er helsekravene i vedlegg 1 likevel oppfylt, eventuelt med begrenset varighet og/eller særlige vilkår? | Ja/Nei | Mangler |

**Leges underskrift** (midtside) — signatur fra attestutstedende lege.

---

## Side 3: Konklusjon

Legen fyller ut helseattesten som medisinsk sakkyndig for trafikkstasjonen og for førerkortssøkeren. Legens sakkyndige erklæring er **ikke** et forvaltningsvedtak med klagerett — det er trafikkstasjonen som treffer vedtak om førerkortutstedelse.

### Konklusjon per førerkortgruppe

For hver av de fire alternativene krysser legen av for ett av tre utfall:

| Gruppe | Helsekrav ikke oppfylt | Helsekrav oppfylt — vanlig varighet | Helsekrav oppfylt — begrenset varighet (antall år) | Status i PoC |
|---|---|---|---|---|
| Førerkortgruppe 1 | Avkrysning | Avkrysning | Avkrysning + tall | Mangler |
| Førerkortgruppe 2 | Avkrysning | Avkrysning | Avkrysning + tall | Mangler |
| Førerkortgruppe 3 inkl. kjøreseddel for drosje | Avkrysning | Avkrysning | Avkrysning + tall | Mangler |
| Førerkortgruppe 3 inkl. utrykningskompetanse/kjøreseddel for buss | Avkrysning | Avkrysning | Avkrysning + tall | Mangler |

### Progresjonsvurdering

| Felt | Type | Status i PoC |
|---|---|---|
| Er det tatt hensyn til forventet progresjon av eventuelle helsesvekkelser ved anbefaling av varighet? | Ja/Nei | Mangler |

### Vilkår

Faste vilkår legen kan krysse av for:

| Vilkår | Type | Status i PoC |
|---|---|---|
| Optisk korreksjon må brukes under føring av motorvogn i gruppe 1, 2 og 3 | Avkrysning | Mangler |
| Optisk korreksjon må brukes under føring av motorvogn i gruppe 2 og 3 | Avkrysning | Mangler |
| Helseattest gis med forbehold om at det leveres godkjent Helseattest førerett – syn (IS-2571) | Avkrysning | Mangler |
| Hørselshjelpemiddel må brukes under føring av motorvogn (gruppe 3) | Avkrysning | Mangler |
| Protese/ortose (støtteskinne o.l.) må brukes under føring av motorvogn i gruppe 1, 2 og 3 | Avkrysning | Mangler |
| Ved Ja på spørsmål 14b (stabil førlighetssvekelse) vurderer trafikkstasjonen om førerett likevel kan gis (§ 41) | Avkrysning | Mangler |
| Særlige vilkår (fritekst) | Fritekst | Delvis — `Forer_Vilkar` i modellen |

### Signatur

| Felt | Type | Status i PoC |
|---|---|---|
| Leges underskrift og HPR-nummer | Signatur + tekst | Delvis — HPR fra FHIR, signatur ikke digital |

---

## Implementeringsstatus

### Hva PoC-datamodellen dekker

```
ForerLegeerklaeringModel (src/App/models/ForerLegeerklaeringModel.cs)

Pasient_Fnr              ← FHIR Patient.identifier
Pasient_Fornavn          ← FHIR Patient.name
Pasient_Etternavn        ← FHIR Patient.name
Pasient_Fodselsdato      ← FHIR Patient.birthDate
Pasient_Kjonn            ← FHIR Patient.gender
Lege_HPR                 ← FHIR Practitioner.identifier
Lege_Fornavn             ← FHIR Practitioner.name
Lege_Etternavn           ← FHIR Practitioner.name
Virksomhet_Navn          ← FHIR Organization.name
Virksomhet_Orgnr         ← FHIR Organization.identifier (OID 4.101)
Virksomhet_HerId         ← FHIR Organization.identifier (OID 4.2)
Konsultasjon_Dato        ← FHIR Encounter.period.start
Diagnose_Kode            ← FHIR Condition.code.coding[0].code
Diagnose_Tekst           ← FHIR Condition.code.coding[0].display
Forer_Kjoretoygruppe     ← Legen velger (kjøretøykoder A–T, ikke gruppe 1/2/3)
Forer_ErSkikket          ← Legen velger (bool — for enkel)
Forer_Merknad            ← Legen fyller ut (fritekst)
Forer_Vilkar             ← Legen fyller ut (fritekst)
```

### Hva som mangler for et produksjonsklart skjema

En fullstendig implementering krever:

1. **Formål og førerkortgruppe** — 10 formålsalternativer + gruppe 1/2/3 (i stedet for kjøretøykoder A–T)
2. **Legens tilknytning** — 2 avkrysningsfelt + 1 fritekstfelt
3. **Identitetsbekreftelse** — 3 avkrysningsfelt
4. **Helsekategoriene 1–16** — 22 Ja/Nei-felt (inkl. underspørsmål), synstestdata (tall per øye)
5. **Konklusjon per gruppe** — 3 utfall × 4 grupper = 12 felt + 4 varighetstall
6. **Vilkår** — 6 faste avkrysninger + eksisterende fritekstfelt
7. **Progresjonsvurdering** — 1 Ja/Nei-felt

Total estimert utvidelse: fra 18 til ~70 felt i datamodellen.

### Merknader om digitalisering

- **Synstestdata** (spørsmål 1A) er i dag numeriske verdier (visus) — disse er ikke tilgjengelig som strukturerte FHIR-data i norske EPJ-er per 2026
- **Helsekategoriene 2–15** er legens kliniske vurdering og **kan ikke prefylles fra FHIR** — de tilhører legens faglige skjønn
- **Diagnose fra FHIR** (Condition) er en naturlig kilde for relevans til kategori 8 (hjerte/kar), 9 (diabetes) og 15 (andre), men legen bekrefter selv med Ja/Nei
- **Signatur** håndteres av Altinn (signering ved innsending) — ikke et eget skjemafelt

---

## Tilknyttede blanketter

| Blankett | ID | Formål |
|---|---|---|
| Helseattest førerett – syn | IS-2571 (2017) | Optiker/øyelege fyller ut synsvurdering separat |
| Egenerkl. om helse (søker) | — | Søker fyller ut selv, legen leser og bekrefter |


---

# 4. Pasientflyt

# Pasientflyt: Egenerklæring og legeattestprosessen — førerrett

**Dato:** 2026-06-16  
**Status:** Arkitekturforslag — utenfor PoC-scope, men nødvendig å adressere

---

## 1. Bakgrunn og problem

I dagens papirbaserte flyt:

1. Pasient bestiller time hos fastlege (for helseattest førerrett)
2. Pasient laster ned, skriver ut og fyller ut **egenerklæring om helse** (blankett NA-0201, Statens vegvesen)
3. Pasient medbringer utfylt papirskjema til konsultasjonen
4. Lege gjennomgår egenerklæringen og fyller ut **helseattest IS-2569**
5. Lege sender IS-2569 til Helsedirektoratet/Statens vegvesen

**Problemet:** Egenerklæringen er ikke digitalt tilgjengelig for legen i EPJ. Legen må manuelt taste inn informasjon fra papir, og pasienten risikerer å glemme skjemaet. Flyten er heller ikke sporbar eller arkivert digitalt.

**Målet:** Pasienten fyller ut egenerklæringen digitalt *før* konsultasjonen. Når legen åpner legeerklæringen i Altinn-appen, er pasientens egenerklæring allerede tilgjengelig som grunnlag.

---

## 2. Egenerklæringsskjemaet (NA-0201)

Blankett NA-0201 (Statens vegvesen, 2017) har to deler:

### Del 1 — Søknad om førerkort/kompetansebevis

| Felt | Beskrivelse |
|---|---|
| Etternavn, fornavn, mellomnavn | |
| Fødselsnummer (11 siffer) | |
| Adresse, postnummer, poststed | |
| Telefon, mobilnummer, e-post | |
| Ønsket målform (bokmål/nynorsk) | |
| Søknaden gjelder | Første gang / utvidelse / fornyelse / innbytte / tilbakelevering / kompetansebevis |
| Førerkortklasse ønsket | AM145/146/147, S, T, A1, A2, A, B1, B, B96, BE, C1, C1E, C, CE, D1, D1E, D, DE + kompetansebevis |
| Utenlandsk førerkort | Ja/Nei, utstedelsesland, klasse |

### Del 2 — Egenerklæring om helse (17 spørsmål, Ja/Nei)

| Nr | Spørsmål | Trigger |
|---|---|---|
| 1 | Nedsatt synsstyrke, behov for briller/linser? | Synsattest fra lege/optiker |
| 2 | Dobbeltsyn siste 3 måneder, problemer i mørke, nedsatt sidesyn? | Synsattest fra optiker/øyelege |
| 3 | Problemer med å orientere seg i trafikken? | Helseattest lege |
| 4 | Nevrologisk sykdom (nå eller tidligere)? | Helseattest lege |
| 5 | Besvimelse, krampe, nedsatt bevissthet siste 5 år? | Helseattest lege |
| 6 | Besvimelse/krampe siste 10 år, eller epilepsimedisiner? | Helseattest lege |
| 7 | Obstruktivt søvnapné syndrom eller annen søvnsykdom? | Helseattest lege |
| 8 | Hjerte-/karsykdom (nå eller tidligere)? | Helseattest lege |
| 9 | Diabetes? | Helseattest lege |
| 10 | Alvorlig psykisk lidelse eller psykisk svekkelse? | Helseattest lege |
| 11 | ADHD? | Helseattest lege |
| 12 | Legemidler som kan påvirke kjøringen? | Helseattest lege |
| 13 | Misbruk av alkohol/rusmidler siste 3 år? | Helseattest lege |
| 14 | Svekket lungefunksjon? | Helseattest lege |
| 15 | Alvorlig nyresvikt (nå eller tidligere)? | Helseattest lege |
| 16 | Nedsatt førlighet i arm eller ben? | Helseattest lege |
| 17 | Andre helsemessige forhold som kan svekke kjøreevnen? | Helseattest lege |

**Logikk:**
- Kun spm. 1 besvart «ja» → synsattest holder
- Spm. 2 besvart «ja» → optiker/øyelege synsattest
- Spm. 3–17 besvart «ja» (gruppe 1) → helseattest IS-2569 fra lege
- Tunge klasser (C/D og utrykning) → helseattest alltid påkrevd

---

## 3. Foreslått digital flyt

### Overordnet sekvens

```
Pasient                    helsenorge.no / Altinn      EPJ / Altinn-app (lege)
   │                              │                            │
   │── Bestiller time ──────────► │                            │
   │                              │◄─── EPJ oppretter ─────────┤
   │                              │     Dialogporten-dialog    │
   │◄─── Varsel (SMS/e-post) ─────│                            │
   │                              │                            │
   │── Åpner dialog ─────────────►│                            │
   │── Fyller ut egenerklæring ───►│                            │
   │── Signerer (ID-porten) ──────►│                            │
   │                              │── Lagrer som FHIR ────────►│
   │                              │   QuestionnaireResponse    │
   │                              │                            │
   │                              │           [Konsultasjon]   │
   │                              │                            │
   │                              │          ◄── SMART launch ─┤
   │                              │          ◄── Henter QuR ───┤
   │                              │          Prefiller IS-2569  │
   │                              │          Lege kompletterer  │
   │                              │          Sender inn ───────►│ Helsedir/Vegvesen
```

### Steg 1 — Timebestilling trigger dialog

Når pasienten bestiller time (via helsenorge.no booking eller EPJ-resepsjonen), oppretter EPJ-systemet en **Dialogporten-dialog** for pasienten:

```
POST https://dialogporten.altinn.no/api/v1/serviceowner/dialogs
{
  "serviceResource": "urn:altinn:resource:forer-egenerklaring",
  "party": "urn:altinn:person:identifier-no:01039012345",
  "status": "New",
  "dueAt": "2026-07-01T09:00:00Z",
  "title": [{ "value": "Egenerklæring om helse — førerrett", "languageCode": "nb" }],
  "body": [{ "value": "Du har bestilt time for helseattest. Fyll ut egenerklæringen før konsultasjonen.", "languageCode": "nb" }],
  "guiActions": [{
    "action": "open",
    "url": "https://altinn.no/forer-egenerklaring/...",
    "title": [{ "value": "Fyll ut egenerklæring", "languageCode": "nb" }],
    "priority": "Primary"
  }]
}
```

Dialogen vises i:
- **helsenorge.no** innboks (via Dialogporten-integrasjon)
- **Altinn innboks** (alternativt)
- SMS/e-post-varsel til pasienten

### Steg 2 — Pasient fyller ut egenerklæring

Pasienten åpner linken fra dialogen og fyller ut egenerklæringen i en **Altinn-app** (`forer-egenerklaring`):

- Autentiseres via ID-porten (nivå 3 — samme som Altinn generelt)
- Personopplysninger prefilles fra Folkeregisteret
- De 17 Ja/Nei-spørsmålene fylles ut
- Pasienten signerer digitalt
- Skjemaet lagres i Altinn og/eller skrives tilbake som FHIR QuestionnaireResponse

### Steg 3 — FHIR QuestionnaireResponse

Den utfylte egenerklæringen lagres som en **FHIR QuestionnaireResponse** knyttet til pasienten:

```json
{
  "resourceType": "QuestionnaireResponse",
  "questionnaire": "https://vegvesen.no/fhir/Questionnaire/egenerklaring-helse",
  "status": "completed",
  "subject": { "reference": "Patient/sophie-salt" },
  "authored": "2026-07-01T08:30:00+02:00",
  "item": [
    { "linkId": "spm1", "text": "Nedsatt synsstyrke?", "answer": [{ "valueBoolean": false }] },
    { "linkId": "spm9", "text": "Diabetes?", "answer": [{ "valueBoolean": true }] }
  ]
}
```

### Steg 4 — Legen henter egenerklæringen

Når legen starter SMART EHR Launch og Altinn-appen prefiller IS-2569, henter BFF-en også pasientens QuestionnaireResponse:

```csharp
// I FhirPrefillService.cs
var qrUrl = $"{ctx.FhirBaseUrl}/QuestionnaireResponse" +
    $"?subject=Patient/{patientId}" +
    $"&questionnaire=https://vegvesen.no/fhir/Questionnaire/egenerklaring-helse" +
    $"&status=completed&_sort=-authored&_count=1";
var egenerklaring = await TryGetFhirResource(client, qrUrl, "QuestionnaireResponse");
```

Relevante svar fra egenerklæringen kan:
- Vises som kontekst for legen (ikke redigerbart — pasientens eget svar)
- Trigge automatisk flagging (f.eks. «Pasienten har svart ja på spm. 9 — diabetes»)
- Foreslå hvilke helsekategorier i IS-2569 legen bør vurdere

---

## 4. Arkitektoniske valg

### Alternativ A — Altinn + Dialogporten (anbefalt)

| Komponent | Teknologi | Ansvar |
|---|---|---|
| Pasientskjema | Altinn-app `forer-egenerklaring` | Statens vegvesen / Helsedirektoratet |
| Dialogoppretting | Dialogporten API (fra EPJ via Maskinporten) | EPJ-leverandør |
| Pasientvisning | helsenorge.no (Dialogporten-integrasjon) eller Altinn | NHN / Digdir |
| Lagring | Altinn Storage + FHIR QuestionnaireResponse | Begge |
| Legehenting | FHIR API fra Altinn BFF | Vår app |

**Fordeler:** Bruker eksisterende nasjonal infrastruktur; helsenorge.no-visning er naturlig for pasienten; Altinn-arkivering gir sporbarhet.

**Utfordringer:** Krever at Dialogporten er tilgjengelig og at helsenorge.no viser dialogen; krever Maskinporten-autentisering fra EPJ for dialogoppretting.

### Alternativ B — Helsenorge.no native

Helsenorge.no har egne skjematjenester. Egenerklæringen opprettes som en oppgave/tjeneste direkte i helsenorge.no, uten Altinn-appen.

**Fordeler:** Integrert i pasientens vanlige helseportal.  
**Utfordringer:** Krever samarbeid med NHN/helsenorge.no-forvaltning; mindre fleksibel for gjenbruk i andre skjemaflyter.

### Anbefaling

**Alternativ A med Dialogporten** er riktig langsiktig arkitektur. Det gjenbruker eksisterende infrastruktur og gir pasienten en naturlig opplevelse via helsenorge.no. For en første iterasjon kan dialogen opprettes manuelt (EPJ-resepsjonen sender link direkte), uten automatisk integrasjon mot timebestillingssystemet.

---

## 5. Mapping: Egenerklæring → IS-2569

Pasientens svar på egenerklæringen korresponderer direkte med helsekategoriene legen vurderer:

| Egenerklæring (NA-0201) | IS-2569 helsekategori |
|---|---|
| Spm. 1–2: Syn | Kategori 1: Syn |
| Spm. 4: Nevrologisk | Kategori 6: Nevrologiske sykdommer |
| Spm. 5–6: Besvimelse/epilepsi | Kategori 5: Epilepsi / bevissthetsforstyrrelser |
| Spm. 7: Søvnapné | Kategori 11: Søvnsykdommer |
| Spm. 8: Hjerte-/kar | Kategori 3: Hjerte- og karsykdommer |
| Spm. 9: Diabetes | Kategori 4: Diabetes |
| Spm. 10: Psykisk lidelse | Kategori 8: Psykiske lidelser |
| Spm. 11: ADHD | Kategori 9: ADHD |
| Spm. 12: Legemidler | Kategori 13: Legemidler |
| Spm. 13: Rus | Kategori 14: Misbruk av rusmidler |
| Spm. 14: Lungefunksjon | Kategori 10: Lungesykdommer |
| Spm. 15: Nyresvikt | Kategori 12: Nyresykdommer |
| Spm. 16: Førlighet | Kategori 2: Bevegelse/førlighet |
| Spm. 17: Andre forhold | Legens skjønn |

**Viktig:** Pasientens egenerklæring er *ikke* medisinsk vurdering. Den vises som kontekst for legen, men legen foretar sin selvstendige kliniske vurdering i IS-2569.

---

## 6. Dataflyt og personvern

```
Pasient (fnr 01039012345)
    │
    ├─► Egenerklæring (NA-0201)
    │       Lagres i: Altinn + FHIR QuestionnaireResponse
    │       Tilgang: Pasienten selv + behandlende lege (via SMART-token)
    │
    └─► Samtykke til legen (innebygd i skjema):
            "Jeg gir legen fullmakt til å innhente nødvendige og relevante
             helseopplysninger fra spesialist og tidligere fastlege"
```

**Personvernkrav:**
- Egenerklæringen inneholder helseopplysninger — særskilt kategori etter GDPR art. 9
- Tilgangen må begrenses til behandlende lege i den aktuelle konsultasjonen
- FHIR QuestionnaireResponse bør ha `meta.security`-tagging og tilgangskontroll i EPJ
- Samtykket pasienten signerer i egenerklæringen dekker legens bruk — bør knyttes til den aktuelle konsultasjonen (Encounter-referanse)

---

## 7. Hva dette krever av nye komponenter

| Komponent | Ny? | Beskrivelse |
|---|---|---|
| Altinn-app `forer-egenerklaring` | **Ja** | Pasientens egenerklæringsskjema |
| Dialogporten-integrasjon i EPJ | **Ja** | EPJ oppretter dialog ved timebestilling |
| FHIR Questionnaire (NA-0201) | **Ja** | Formalisering av skjemaet som FHIR-ressurs |
| FHIR QuestionnaireResponse-støtte i BFF | **Ja** | Henting og visning for lege |
| helsenorge.no Dialogporten-visning | Eksisterer | NHN viser Dialogporten-dialogen |
| Altinn Autorisasjon for egenerklæring | **Ja** | Tilgangsstyring pasient → lege |

---

## 8. Scope og prioritering

| Fase | Innhold |
|---|---|
| **PoC (nå)** | Papirflyt — pasienten medbringer NA-0201 på papir. Ikke implementert digitalt. |
| **v1.0** | Pasient fyller ut egenerklæring digitalt i Altinn-app. Manuell link-distribusjon (ingen Dialogporten). Legen ser utfylt egenerklæring i Altinn BFF. |
| **v2.0** | Dialogporten-integrasjon: EPJ oppretter dialog automatisk ved timebestilling. Vises i helsenorge.no. |
| **v3.0** | Fullstendig FHIR QuestionnaireResponse-flyt: prefill IS-2569 fra egenerklæring. Samtykke og tilgangsstyring via Tillitsrammeverk. |

---

## 9. Referanser

| Kilde | URL |
|---|---|
| Egenerklæring NA-0201 (Statens vegvesen) | https://www.vegvesen.no/globalassets/forerkort/ta-forerkort/soknad-om-forerkort-og-kompetansebevis-egenerklaering-om-helse.pdf |
| Helseattest IS-2569 (Helsedirektoratet) | Se `docs/SKJEMA-IS2569.md` |
| Dialogporten dokumentasjon | https://docs.altinn.studio/en/dialogporten/ |
| helsenorge.no | https://www.helsenorge.no/ |
| FHIR Questionnaire (HL7) | https://hl7.org/fhir/R4/questionnaire.html |
| FHIR QuestionnaireResponse (HL7) | https://hl7.org/fhir/R4/questionnaireresponse.html |


---

# 5. Åpne beslutninger

# Åpne beslutninger og uavklarte designvalg

Dette dokumentet parkerer beslutninger som krever menneskelig avklaring — juridisk, organisatorisk eller arkitektonisk — og som ikke kan løses med kode alene. Hvert punkt inneholder bakgrunn, alternativer og hvem som beslutter.

Se [RISIKOREGISTER.md](RISIKOREGISTER.md) for en samlet, prioritert oversikt over hvilke av disse beslutningene som er mest tidskritiske, med forslag til eier og tiltak per punkt.

---

## C-1: Hvem er «parten» i Altinn-instansen?

**Problemstilling:**  
En Altinn-instans har alltid én «part» (party) som eier instansen. I dag starter legen instansen på vegne av seg selv. Spørsmålet er om parten skal være:

| Alternativ | Beskrivelse | Konsekvens |
|---|---|---|
| **A — Legen** | Legen er part og signatar | Enklest teknisk. Men instansen lever i legens «innboks» i Altinn, ikke pasientens. |
| **B — Pasienten** | Legen fyller ut på vegne av pasienten | Krever delegering eller samtykke. Mer korrekt juridisk (erklæringen gjelder pasienten). |
| **C — Legekontoret (org)** | Instansen tilhører virksomheten | Mulig ved bruk av `partyTypesAllowed.organisation`. Krever systemtilgang-autorisasjon. |

**Avhengigheter:** Valget påvirker autorisasjonsmodellen i `policy.xml`, rollekrav, og hvem som kan se instansen i Altinn-meldingsboksen.

**Beslutter:** Tjenesteeier (Digdir) i samråd med Statens vegvesen og Helsedirektoratet.

**Status:** Uavklart — PoC bruker alternativ A (legen som part).

---

## C-2: HelseID — når skal BFF-siden validere tokenet?

**Problemstilling:**  
I dag stoler BFF-en (ASP.NET Core) på access token fra SMART-mock uten å validere signaturen. I produksjon med HelseID må tokenet valideres. Spørsmålet er *når* dette skal innføres og *hva* som kreves:

- JWT Bearer-validering mot HelseID sitt JWKS-endepunkt
- Krav til claims: `pid` (fnr), `helseid://claims/hpr/hpr_number`, `assurance_level`
- NHN Tillitsrammeverk-claims for behandlingsformål (`CareRelationshipPurposeOfUseCode`, etc.)
- Klientregistrering på selvbetjening.test.nhn.no (HelseID test)

**Konkrete oppgaver som er utsatt:**
1. Registrer klient på `selvbetjening.test.nhn.no`
2. Legg til `AddJwtBearer` i `Program.cs` med HelseID JWKS-URL
3. Trekk ut `pid`-claim og verifiser mot FHIR `Practitioner.identifier` (fnr)
4. Valider at `assurance_level >= high` (kreves for helseopplysninger)
5. Logg `hpr_number` + `pid` i audit trail

**Referanse:** Se IMPLEMENTERING.md §14 (HelseID kom-i-gang) for detaljert veiledning.

**Beslutter:** Teknisk team + NHN avtaleprosess.

**Status:** Dokumentert, ikke implementert. Blokkert av klientregistrering.

---

## C-3: Mottaksarkitektur — hvem er tjenesteeier?

**Problemstilling:**  
Når legen sender inn erklæringen, hva sendes hvor — og hvem eier tjenesten?

### Altinn Events — slik mottaksarkitektur faktisk fungerer

Altinn 3 varsler mottakere via **Altinn Events** når noe sendes inn. Det er ikke Altinn som pusher data til SVV, men SVV som abonnerer på events og henter instansen selv. Hvert mottakssystem:
1. Abonnerer på events av typen `app.instance.process.completed` for riktig app/org
2. Henter instansdata via Altinn Storage API med Maskinporten-token
3. Distribuerer til riktig fagsystem eller sak/arkiv-system

**FINT Arkiv-mønsteret** (brukt bl.a. av Helsedirektoratet) viser en etablert tilnærming der et felles mottakslag router innkommende Altinn-instanser til riktig fagsystem basert på tjenestenøkkel/ressurstype. Dette er relevant som mal for et eventuelt felles mottakssystem for helseattester.

### To-lags mottaksmodell — konklusjon til SVV, full attest til EPJ

Den eksisterende digitale løsningen overfører **kun konklusjonen** (grønt/rødt per gruppe + vilkår) til SVV/førerkortregisteret — ikke hele IS-2569. Full attest skrives tilbake til EPJ. Dette er det realistiske målbildet:

| Lag | Innhold | Mottaker | Kanal |
|---|---|---|---|
| **Konklusjon** | Gruppe 1/2/3: skikket/ikke skikket + begrenset varighet + vilkår | Statens vegvesen (førerkortregisteret) | Altinn Events → SVVs mottakssystem |
| **Full attest** | Komplett IS-2569 + legens vurderinger | EPJ-journal | FHIR `DocumentReference` writeback med SMART access token |

**Teknisk løsning i PoC — to separate datatyper:**

```json
{
  "dataTypes": [
    {
      "id": "ForerLegeerklaering",
      "appLogic": { "classRef": "Altinn.App.Models.ForerLegeerklaeringModel" },
      "taskId": "Task_1"
    },
    {
      "id": "ForerKonklusjon",
      "appLogic": {
        "autoCreate": false,
        "classRef": "Altinn.App.Models.ForerKonklusjonModel"
      },
      "taskId": "Task_1",
      "minCount": 0
    }
  ]
}
```
*(Faktiske id-er og namespace i `src/App/config/applicationmetadata.json` — implementert 2026-06-19, commit `38057a4`.)*

`ForerKonklusjonModel` populeres automatisk i `FhirPrefillService.ProcessDataWrite` ved innsending (upsert via `IDataClient`), avledet fra den komplette modellen. Altinn Events varsler SVVs mottakssystem om at konklusjonen er klar; BFF skriver full attest til EPJ via `DocumentReference` (ikke implementert ennå, se VEIKART.md fase 2).

**Kjent forenkling i PoC-implementasjonen:** `ForerLegeerklaeringModel` har i dag ett enkelt `Forer_Kjoretoygruppe`-felt og én `Forer_ErSkikket`-boolean — legen vurderer altså kun *én* kjøretøygruppe per legeerklæring. `DeriveKonklusjon()` i `FhirPrefillService.cs` setter derfor resultatet på den ene gruppen som matcher, og lar de to andre `GruppeX_Resultat`-feltene stå tomme. De tre uavhengige gruppefeltene i `ForerKonklusjonModel` er altså forberedt for, men ikke fylt av, reelle per-gruppe-vurderinger — det krever at `ForerLegeerklaeringModel` utvides til å holde skikkethet per gruppe (jf. full IS-2569, C-5).

### Tjenesteeieralternativer

| Alternativ | Beskrivelse | Avhengigheter |
|---|---|---|
| **A — Digdir (nåværende)** | Digdir eier tjenesten, placeholder for PoC | Ingen nye avtaler. Men Digdir er ikke naturlig mottaker. |
| **B — Statens vegvesen** | SVV som tjenesteeier; abonnerer på Altinn Events | Krever avtale om Events-abonnement og mottakssystem hos SVV. |
| **C — Helsedirektoratet** | Hdir som nasjonal koordinator med FINT Arkiv-routing | Mulig felles mottakslag for helseattester på tvers av fagsystemer. |

**Avhengigheter:** Valget påvirker `applicationmetadata.json` (`org`-felt), `policy.xml` (tjenesteeier-regel), og Maskinporten-scope for mottakssystemets henting av instansdata.

**Beslutter:** Programleder + juridisk avdeling + Statens vegvesen.

**Status:** Teknisk retning avklart (to-lags modell: konklusjon til SVV via Events, full attest til EPJ). Tjenesteeier og SVVs mottakssystem gjenstår som avklaring.

---

## C-4: Rettslig grunnlag og DPIA

**Problemstilling:**  
Behandling av helseopplysninger i dette systemet krever avklaring av rettslig grunnlag og en formell personvernkonsekvensvurdering (DPIA/DPIA).

**Relevante hjemler (foreløpig vurdering):**
- Helsepersonelloven § 45 — plikt til å utstede attest/erklæring på anmodning
- Pasientjournalloven § 6 — behandling av helseopplysninger i forbindelse med helsehjelp
- GDPR art. 9 nr. 2 bokstav h — behandling nødvendig for medisinske formål av helsepersonell

**Åpne spørsmål:**
1. Er Digdir behandlingsansvarlig eller databehandler? Hvem er behandlingsansvarlig i siste instans (SVV? Hdir? Legekontoret?)?
2. Trenger Altinn-appen en egen behandlingsprotokoll (art. 30)?
3. Er det krav om DPIA (art. 35) gitt systematisk behandling av helseopplysninger i stor skala?
4. Hva er minimumsloggingskravet (art. 5 nr. 2 — ansvarlighetsprinsippet)?
5. Krav til databehandleravtale mellom Digdir og EPJ-leverandøren?

**Beslutter:** Personvernombud + juridisk rådgiver + evt. Datatilsynet (forhåndskonsultasjon).

**Status:** Uavklart. Se KRAVSPESIFIKASJON-v0.6.md §7 for utdyping.

---

## C-5: Fullstendig IS-2569 og pasientens egenerklæring (NA-0201)

**Problemstilling:**  
Nåværende datamodell (`ForerLegeerklaeringModel`) dekker kun en liten del av blankett IS-2569 (Helseattest for førerkort). Full implementering krever:

**Del A — Manglende felt i IS-2569:**
- Alle 17 helsekategorier med klinisk vurdering (synsfunksjon, hørsel, bevegelsesapparat, hjerte/kar, nevrologi, psykisk helse, kognitiv funksjon, søvn, diabetes, nyre, lever, kreft, øre/nese/hals, muskel/skjelett, rusmidler, medikamenter)
- Vilkår og begrensninger per kategori (kodeverk fra Statens vegvesen)
- Legens underskrift og dato
- Erklæring om at pasienten er informert

**Del B — Pasientens egenerklæring (NA-0201):**
- 17 ja/nei-spørsmål om helsetilstand
- Digital flyt via Dialogporten (se PASIENTFLYT.md for arkitekturforslag)
- Mapping mellom NA-0201-svar og IS-2569 helsekategorier
- FHIR QuestionnaireResponse for å overføre svar til legen

**Prioritering:**
- PoC: Kun grunndata (navn, fnr, HPR, org, vurdering) — ferdig
- v1.0: Full IS-2569 dekningsgrad — krever feltmapping-arbeid med Helsedirektoratet
- v2.0: Digital NA-0201 via Dialogporten — krever ny avtale og tjenesteutvikling

**Beslutter:** Produkteier + Helsedirektoratet (IS-2569 eier) + Statens vegvesen (NA-0201 eier).

**Status:** Datamodell og feltstruktur dokumentert i SKJEMA-IS2569.md. Implementering utsatt.

---

---

## C-6: Altinn Studio vs. Helsenorge — plattformvalg for IS-2569

**Problemstilling:**  
NHN har allerede en produksjonssatt løsning for legeerklæring IS-2569 bygget på Helsenorge-plattformen (se [NHN-DOKUMENTASJON.md](NHN-DOKUMENTASJON.md) og [SAMMENLIGNING-syk-inn.md](SAMMENLIGNING-syk-inn.md)). Digdir PoC-en viser at det *også* er mulig på Altinn Studio. Spørsmålet er hvilken plattform som er riktig for en eventuell produksjonssetting.

| Alternativ | Fordeler | Ulemper |
|---|---|---|
| **A — Altinn Studio (nåværende kurs)** | Signering, arkiv, XACML, ID-porten gratis; Digdir eier plattformen | Mottaksarkitektur til SVV uavklart; HelseID krever ekstraarbeid; NHN har gjort det samme |
| **B — Helsenorge-plattformen** | HelseID nativt; NA-0201 tilgjengelig; SVV-integrasjon eksisterer; NHN allerede i prod | Digdir er ikke eier; krever samarbeid med NHN; annen teknologistack |
| **C — Hybridmodell** | Altinn for signering/arkiv, Helsenorge for innbyggerflyt (NA-0201) | To integrasjoner, mer kompleksitet |

**Avhengigheter:** C-3 (mottaksarkitektur SVV), C-4 (behandlingsansvar).

**Ny innsikt (2026-06-17):** NHNs Slack-kanal `ext-utv-hn-forerrett` tilbyr samarbeid med NHN-teamet. Kontakt bør tas for å avklare om PoC-funnene kan bidra inn i eksisterende løsning fremfor å parallelt-utvikle en Altinn-variant.

**Beslutter:** Programleder + NHN + Statens vegvesen.

**Status:** Ny åpen beslutning — uavklart.

---

---

## C-7: DSOP + SMART on FHIR mot privat sektor (forsikring)

**Problemstilling:**  
Forsikringsbransjen er en stor og manuell konsument av legeerklæringer (ved tegning, skadesak og sakkyndig-oppdrag). Det finnes allerede en kanal mot Finans Norge via Helsenettet. DSOP-rammeverket (Digital Samhandling Offentlig–Privat) er brukt for bank/NAV-dataflyt. Spørsmålet er om Digdir bør initiere eller støtte arbeid med en SMART on FHIR-flyt mot forsikring innenfor dette rammeverket.

| Alternativ | Beskrivelse | Konsekvens |
|---|---|---|
| **A — Avvent** | Vent til offentlig sektor-pilotene er modne og tillitsankeret hos NHN er operasjonelt | Lavere risiko, ingen ny governance nå |
| **B — Utred** | Initier dialog med Finans Norge/Bits og NHN om DSOP-flyt for legeerklæringer | Kan sette Digdir i drivesetet for privatsektor-digitalisering |
| **C — Bidra inn** | Lever referansearkitektur fra `forer`-PoC som grunnlag for et privat-sektor-spor | Gjenbruk uten å eie løpet selv |

**Forutsetninger som må avklares uansett:**
- Samtykke og taushetsplikt: innbygger-/pasientstyrt flyt, dataminimering (GDPR art. 9)
- Legens sakkyndig-rolle: forsvarlighet, habilitet, objektivitet — SoF prefiller, legen vurderer
- Governance: hvem godkjenner apper mot privat mottaker (EPJ-leverandør, NHN tillitsanker)

**Avhengigheter:** C-6 (plattformvalg), NHN tillitsanker-modenhet, DSOP syke-/uføreforsikring-prosjekt.

**Beslutter:** Programleder + juridisk avdeling + Finans Norge / Bits.

**Status:** Ny åpen beslutning — uavklart. Se [KARTLEGGING-kandidater.md](KARTLEGGING-kandidater.md) G1/G2 for grunnlag.

---

## Beslutningslogg

| Dato | Beslutning | Besluttet av | Status |
|---|---|---|---|
| 2026-06-16 | PoC bruker legen som part (C-1 alt. A) | JSF | Midlertidig |
| 2026-06-16 | HelseID-validering utsettes til post-PoC (C-2) | JSF | Unblokket 2026-06-17 (klient registrert) |
| 2026-06-16 | Digdir som tjenesteeier-placeholder (C-3 alt. A) | JSF | Midlertidig |
| 2026-06-16 | DPIA-avklaring krever juridisk ressurs (C-4) | JSF | Åpen |
| 2026-06-16 | Full IS-2569 utsettes til v1.0 (C-5) | JSF | Planlagt |
| 2026-06-17 | Altinn Studio vs. Helsenorge — plattformvalg (C-6) | JSF | Ny åpen beslutning |
| 2026-06-17 | DSOP + SMART on FHIR mot forsikring/privat sektor (C-7) | JSF | Ny åpen beslutning |


---

# 6. Risikoregister

# Risikoregister — `forer-legeerklaering`

**Sist oppdatert:** 2026-08-10
**Bakgrunn:** Konsolidert etter kvalitetssikring 2026-06-19 (Del 3), som pekte på at risikoene i BESLUTNINGER.md, VEIKART.md og STRATEGI.md sto spredt uten samlet oversikt, eierskap og tiltak.

Dette dokumentet samler risikoene som allerede er beskrevet andre steder i dokumentasjonen, med eier og tiltak i én tabell. Det erstatter ikke BESLUTNINGER.md (som går i dybden på hver beslutning) eller VEIKART.md (tekniske tiltak) — det er et styringsverktøy for å følge opp dem.

**Skala:** Sannsynlighet og konsekvens vurdert Lav / Middels / Høy. «Risiko» = sannsynlighet × konsekvens, brukt til å prioritere rekkefølge, ikke en presis beregning.

---

| ID | Risiko | Kategori | Sannsynlighet | Konsekvens | Eier (forslag) | Tiltak | Status |
|---|---|---|---|---|---|---|---|
| R1 | Fastlege-EPJ-enes FHIR-/SMART-modenhet er ukjent — underlaget har til nå referert DIPS Arena (sykehus-EPJ), ikke de faktiske fastlege-EPJ-ene | Teknisk avhengighet | Høy | Høy — PoC-ens kjernepåstand («fungerer mot EPJ») er ikke verifisert mot en reell fastlege-EPJ | Digdir teknisk team + EPJ-løftet | Test mot minst én fastlege-EPJ FHIR-sandkasse (CGM/Infodoc/WebMed) før «det fungerer» hevdes bredere | Ikke startet — se [KRAVSPESIFIKASJON-v0.6.md §6.4](KRAVSPESIFIKASJON-v0.6.md) |
| R2 | Rettslig grunnlag, behandlingsansvar og Normen-vurdering for plattformleddet er uavklart | Juridisk / personvern | Høy | Høy — blokkerer enhver reell pilot med helseaktører; ingen DPIA finnes | Personvernombud + juridisk rådgiver | Bestill juridisk avklaring + innledende DPIA + Normen-vurdering *før* utviklingsoppstart med helseaktører | Uavklart — se [BESLUTNINGER.md C-4](BESLUTNINGER.md) |
| R3 | Mottaksarkitektur og tjenesteeierskap er ikke formelt avklart — teknisk retning (to-lags modell) er satt, men SVV har ikke bekreftet Events-abonnement | Organisatorisk | Middels | Høy — uten mottaker på den andre siden har konklusjonsdataene (`ForerKonklusjonModel`) ingen bruk | Programleder + Statens vegvesen | Avklar tjenesteeier; få SVV (eller Hdir) til å bekrefte abonnement på `app.instance.process.completed`-events | Teknisk retning avklart, organisatorisk avklaring gjenstår — se [BESLUTNINGER.md C-3](BESLUTNINGER.md) |
| R4 | Sikkerhetsgap: tokenvalidering, audit-logging og issuer-allowlist er ikke implementert | Sikkerhet / compliance | Høy (før fase 1) | Høy — uakseptabelt i produksjon; audit-logging er et Normen-/NHN-krav, ikke valgfritt | Teknisk team | Fase 1 i VEIKART.md: tokenvalidering, allowlist, audit-logging inn i definisjonen av «produksjonsklar» | Dokumentert, ikke implementert — se [VEIKART.md fase 1](VEIKART.md) |
| R5 | Lav adopsjon hos fastleger — konteksbytte ut av EPJ til Altinn er svakere UX enn EPJ-native løsninger | Endringsledelse / brukeradopsjon | Middels | Middels-Høy — teknisk fungerende løsning som ingen tar i bruk i klinisk praksis | Digdir + fastlegeorganisasjoner (Legeforeningen) | Brukertest med reelle fastleger; vurder om «grønt» parallell-case (KARTLEGGING D6) er bedre første pilot enn førerrett | Ikke startet |
| R6 | Initiativet oppfattes som konkurrerende med NHNs produksjonsløsning for IS-2569 på Helsenorge | Strategisk / posisjonering | Middels | Middels — politisk sårbarhet, dobbeltarbeid, samarbeidsvilje fra NHN | Programleder | Bruk firemodell-analysen (STRATEGI.md) som felles språk med NHN; kontakt NHN-teamet (Slack `ext-utv-hn-forerrett`) for skriftlig komplementaritet | Dialog ikke bekreftet gjennomført — se [BESLUTNINGER.md C-6](BESLUTNINGER.md) |
| R7 | Ingen automatiserte tester — regresjon kan innføres uten å bli fanget opp | Teknisk kvalitet | Høy | Middels — hindrer trygg videreutvikling og bredding | Teknisk team | VEIKART.md fase 3: e2e-røyktest + unit-tester (jf. `syk-inn`: 23 unit + 24 e2e) | Ikke startet — se [VEIKART.md fase 3](VEIKART.md) |
| R8 | Full OAuth-redirect-flyt (`ERR_TOO_MANY_REDIRECTS`) er ikke løst — kun `/smart/dev-login`-workaround er bevist | Teknisk | Middels | Middels — den reelle SMART-launch-flyten er ikke bevist ende-til-ende | Teknisk team | Diagnostiser redirect-loopen mot en ekte EPJ-testklient | Uløst — se [README.md «Kjente begrensninger»](../README.md) |

---

## Prioritert lukkerekkefølge

1. **R2 (rettslig grunnlag) og R3 (mottaksarkitektur)** — begge er forutsetninger uavhengig av hvilken leveransemodell som velges (jf. [STRATEGI.md](STRATEGI.md) «Fire leveransemodeller»). Ingen dialog med helseaktører bør starte før disse har en navngitt eier og et konkret neste steg.
2. **R1 (EPJ-modenhet) og R4 (sikkerhetsgap)** — tekniske forutsetninger for at PoC-en kan kalles produksjonsklar. Kan startes uten menneskelige avklaringer.
3. **R7 og R8** — kvalitets- og robusthetsarbeid, gjøres parallelt med 1–2.
4. **R5 og R6** — adopsjon og posisjonering. Krever at 1–2 er på plass først for å ha noe reelt å vise fastleger og NHN.

Se også [BESLUTNINGER.md](BESLUTNINGER.md) for beslutningsdetaljer og [VEIKART.md](VEIKART.md) for tekniske tiltak per fase.


---

# 7. Veikart

# Veikart — `forer-legeerklaering` og SMART on FHIR på Altinn

**Sist oppdatert:** 2026-06-18  
**Utgangspunkt:** PoC gjennomført og verifisert lokalt. Sammenligningsanalyse mot NAV `syk-inn` (prod) identifiserer fem konkrete gap før løsningen kan betraktes som produksjonsklar referansearkitektur.

Dette veikart dekker **Spor A (Førerrett PoC)** og **Spor B (generisk platform)**. Se [STRATEGI.md](STRATEGI.md) for nasjonal roadmap og samarbeidsmodell.

---

## Overordnet retning

Målet er ikke å reimplementere sykmelding. Målet er to ting:

1. Løfte `forer-legeerklaering` til **produksjonsklar referansearkitektur** (Spor A — fase 1–3)
2. Ekstrahere det generiske laget slik at andre etater og skjemaeiere kan bruke det som mal (Spor B — fase 4)

Strategien følger det NAV har bevist med `syk-inn`:
- Få én app til å virke skikkelig i produksjon
- Ekstraher det generiske laget til et bibliotek andre kan bruke

---

## Fase 1 — Produksjonsklar SMART-klient

*Forutsetning for ethvert produksjonssett. Kan gjøres uten avhengigheter mot ytre systemer.*

| Tiltak | Beskrivelse | Ref |
|---|---|---|
| **Tokenvalidering** | Valider access token-signatur server-side. I produksjon: sjekk mot EPJ-ens JWKS-endepunkt, verifiser `iss`, `aud`, `exp`. Bruk `Microsoft.IdentityModel.Tokens`. | `BESLUTNINGER.md` C-2 |
| **Refresh-håndtering** | `offline_access` etterspørres allerede — men token byttes ikke ut. Implementer bakgrunns-refresh i `FhirPrefillService` (sjekk `ExpiresAt` før hvert FHIR-kall, exchang refresh token om nødvendig). | `syk-inn`: `autoRefresh` |
| **Issuer-allowlist** | Fyll `SmartOnFhir:AllowedIssuerList` i appsettings og sett opp per-miljø konfigurasjon (dev/test/prod). Legg til kjente norske fastlege-EPJ-er: CGM Journal, Infodoc, WebMed, Pridok, Aspit System X. (DIPS Arena er sykehus-EPJ og ikke aktuell for denne bruksflyten — men relevant dersom Spor B generaliseres til spesialisthelsetjenesten.) | README «Konfig-gap» |
| **Distribuert sesjon** | Bytt `AddDistributedMemoryCache()` med Redis/Valkey. Kreves for HA (flere pod-er). FHIR-kontekst og token lagres allerede i sesjon — ingen kodeendring utover DI-konfig. | `syk-inn`: Valkey med 30-dagers TTL |
| **ERR_TOO_MANY_REDIRECTS** | Diagnostiser og løs redirect-loopen i full OAuth-flyt. `/dev-login`-workaround er kun lokalt. | README «Kjente begrensninger» |

**Estimat:** 2–3 uker med tilgang til et ekte fastlege-EPJ-testmiljø (f.eks. WebMed/CGM sandkasse).

---

## Fase 2 — Writeback til EPJ

*Lukker den viktigste funksjonelle mangelen. NAVs ADR01 fra `syk-inn` er en direkte oppskrift.*

| Tiltak | Beskrivelse |
|---|---|
| **DocumentReference** | Etter innsending: skriv en `DocumentReference` tilbake til EPJ-ens FHIR-server med PDF-referanse. Bruk SMART access token (BFF). |
| **QuestionnaireResponse** | Skriv strukturert skjemadata som `QuestionnaireResponse` koblet til en kanonisk `Questionnaire`-definisjon (servert fra Altinn-appen, URL `/{org}/{app}/fhir/R4/Questionnaire/V1`). |
| **Transaction Bundle med PUT** | Bruk klient-tildelt id (`DocumentReference.id = altinnInstanceId`) for idempotens. `GET`-sjekk før skriving for å unngå duplikater. |
| **Kanonisk Questionnaire** | Publiser `Questionnaire`-ressursen som beskriver IS-2569-feltene — gjenbrukbar av andre systemer som skal konsumere innsendingen. |

**Referanse:** `syk-inn/src/fhir-write-service.ts` + ADR01.  
**Estimat:** 1–2 uker etter fase 1 (krever gyldig access token med write-scope).

---

## Fase 3 — Testing

*Nødvendig regresjonsvern og demo-verdi. Ingen automatiserte tester finnes i dag.*

| Tiltak | Beskrivelse |
|---|---|
| **e2e røyktest** | Playwright-test som kjører full `dev-login` → prefill → utfylling → signering → verifiser at instans er arkivert i Altinn. Dekker det kritiske happy-path-løpet. |
| **Unit-test: FhirPrefillService** | Test med fixture-JSON for Patient/Practitioner/Encounter — verifiser at modellfelter prefylles korrekt, og at manglende ressurser håndteres uten unntak. |
| **Unit-test: SmartLaunchController** | Test state-mismatch-deteksjon, issuer-allowlist-logikk og PKCE-generering. |
| **Integrasjonstest: writeback** | Test mot HAPI FHIR (allerede i docker-compose) at DocumentReference og QuestionnaireResponse skrives korrekt. |

**Estimat:** 1–2 uker.

---

## Fase 4 — NuGet-pakke: `Digdir.SmartOnFhir`

*Gjøres etter fase 1–3, ikke i stedet for dem. NAV ekstraherte `@navikt/smart-on-fhir` etter at `syk-inn` var i produksjon — samme sekvens gjelder her.*

**Hvorfor:**  
Det finnes ingen SMART on FHIR-klient for .NET/Altinn i dag. En NuGet-pakke ville gjøre det trivielt for andre Altinn-apputviklere å legge til SMART-støtte — på samme måte som `@navikt/smart-on-fhir` gjør for Next.js-apper.

**Hva pakken bør inneholde:**

```
Digdir.SmartOnFhir/
├── SmartLaunchHandler        # EHR Launch: discovery, PKCE, state, redirect
├── SmartCallbackHandler      # Authorization code → token exchange
├── SmartTokenStore           # Abstraksjon over sesjon/cache (Redis-støtte)
├── FhirHttpClientFactory     # HttpClient med SMART token injisert automatisk
├── TokenValidator            # JWKS-validering av access token
├── SmartOptions              # Konfigurasjon: ClientId, AllowedIssuers, osv.
└── Extensions/
    └── AddSmartOnFhir()      # IServiceCollection-extension for enkel DI-konfig
```

**Hva som ikke skal inn:**  
Applikasjonslogikk (prefill-mapping, FHIR-ressursmodeller, skjemastruktur). Pakken er protokoll, ikke domene.

**Publisering:** NuGet.org + GitHub Packages. Versjonering via SemVer, changelog-drevet.

**Estimat:** 2–3 uker for v0.1 (etter at fase 1-koden er stabil nok å ekstrahere fra).

---

## Fase 5 — Full IS-2569 og HelseID

*Lengre horisont. Krever menneskelige avklaringer (se `BESLUTNINGER.md`).*

| Tiltak | Avhengighet |
|---|---|
| Implementer alle 17 helsekategorier i IS-2569 | C-5: menneskelig beslutning om omfang |
| HelseID som behandler-autentisering i Altinn-kontekst | C-2: klientregistrering hos NHN, juridisk avklaring |
| Party-modell: legekontor som innsender | C-1: organisasjonsstruktur i Altinn |
| Mottaksarkitektur (SVV/Hdir/EPJ) | C-3: avtaleverk mellom etater |
| DPIA og behandlingsgrunnlag | C-4: juridisk prosess |

---

## Prioritert rekkefølge

```
Fase 1: SMART-klient (tokenvalidering, refresh, allowlist, distribuert sesjon)
    ↓
Fase 2: Writeback (DocumentReference + QuestionnaireResponse)
    ↓
Fase 3: Testing (e2e + unit + integrasjon)
    ↓
Fase 4: NuGet-pakke Digdir.SmartOnFhir
    ↓
Fase 5: Full IS-2569, HelseID, mottaksarkitektur (etter menneskelige avklaringer)
```

Fase 1–3 er uavhengige av menneskelige beslutninger og kan startes nå.  
Fase 4 er avhengig av at fase 1 er stabil — ikke av fase 2–3.  
Fase 5 er avhengig av avklaringene i `BESLUTNINGER.md`.

---

## Nasjonal satsing (etter fase 4)

Den tekniske fasene 1–5 ovenfor er Spor A (Førerrett PoC). Nasjonal satsing er Spor B — se [STRATEGI.md](STRATEGI.md) for fullstendig beskrivelse.

```
Fase 1–3: Produksjonsklar PoC (Spor A)
    ↓
Fase 4: NuGet-pakke Digdir.SmartOnFhir (Spor B initiell)
    ↓
Nasjonal fase 3: Pilot med NAV / Helfo / Helsedirektoratet
    ↓
Nasjonal fase 4: SMART som standard integrasjonsmønster i Altinn
```


---

# 8. Sammenligning: forer vs. syk-inn vs. NHN Førerrett-App

# Sammenligning: `forer-legeerklaering` vs. `syk-inn` vs. NHN Førerrett-App

Tre SMART on FHIR-apper for norsk helsesektor, vurdert som arkitektur. I prinsippet løser alle det samme problemet: en behandler er innlogget i sin EPJ, EPJ starter en ekstern app via SMART App Launch, appen henter kontekst fra FHIR, behandleren fyller ut et helseskjema, og skjemaet sendes videre. Forskjellen ligger i modenhet, plattformvalg og hvilke valg som er tatt på hvert lag.

> **Viktig kontekst:** NHN Førerrett-App er produksjonsimplementasjonen av *nøyaktig det samme domenet* som `forer-legeerklaering` PoC-en dekker — legeerklæring IS-2569 for Statens vegvesen. Den er bygget av NHN på Helsenorge-plattformen og er allerede i drift.

---

## 1. Kort oppsummering

| | `forer-legeerklaering` | `syk-inn` | NHN Førerrett-App |
|---|---|---|---|
| Domene | Legeerklæring førerrett (IS-2569) | Sykmelding til NAV | Legeerklæring førerrett (IS-2569) |
| Eier | Digitaliseringsdirektoratet (Digdir) | NAV (navikt / team tsm) | NHN / Helsenorge |
| Plattform | Altinn Studio, ASP.NET Core / .NET 8 | Next.js 16 / React 19 / TypeScript | Helsenorge-plattformen |
| Erklært status | «PoC gjennomført» | Produksjon (nav.no) | Produksjon (Helsenorge/EPJ) |
| SMART-implementasjon | Håndskrevet i én controller | Egen pakke `@navikt/smart-on-fhir` | NHN-plattform (ikke offentlig kode) |
| Autentisering | Altinn/ID-porten (HelseID planlagt) | HelseID via Wonderwall | HelseID (obligatorisk) |
| IS-2569-dekning | ~4 av 17+ helsekategorier | N/A (sykmelding) | Komplett med betinget logikk |
| Egenerklæring (NA-0201) | Planlagt (PASIENTFLYT.md) | N/A | Implementert — legen ser den |
| Writeback til EPJ | Ikke implementert | Ja (DocumentReference + QR) | Ja (journalnotat + PDF i EPJ) |
| Innsending til mottaker | Altinn-arkiv | NAVs sykmeldingsflyt | Elektronisk til Statens vegvesen |
| Beslutningsstøtte | Ingen | Regelmotor (syk-inn-api) | Fullt implementert (Førerkortveileder) |
| Automatiserte tester | Ingen | 23 unit + 24 Playwright e2e | Ukjent (lukket kode) |
| «Smart register» | Ikke relevant | Ikke relevant | Obligatorisk legregistrering |

---

## 2. Hva appene faktisk gjør

### `forer-legeerklaering` (Digdir PoC)
EPJ → SMART EHR Launch → Altinn-app (som BFF) → prefyller skjema fra FHIR → lege kontrollerer → signering/arkiv via Altinn Platform. Kjernepoenget er å vise at **Altinn Studio kan brukes som skjemaplattform for helsedokumentasjon**, og at FHIR-prefill fungerer. Skjemaet har 23 komponenter som dekker en begrenset del av IS-2569: pasient, lege, virksomhet, konsultasjon, diagnose og fire lege-felt (kjøretøygruppe, skikket, vilkår, merknad).

### `syk-inn` (NAV)
EPJ → SMART Launch **eller** HelseID-innlogging direkte → dashboard → flertrinns sykmeldingsskjema → regelvalidering i `syk-inn-api` → publisering inn i NAVs sykmeldingsflyt → PDF + strukturert writeback til EPJ. Rikt skjema med diagnose/bidiagnose-søk (ICD-10/ICPC-2), flere perioder, tilbakedatering, arbeidsgiver via Aa-registeret, kladd, forleng og dupliser.

### NHN Førerrett-App (produksjon, samme domene som PoC)
EPJ → SMART EHR Launch → HelseID-autentisering → legen ser pasientens NA-0201 egenerklæring → strukturert IS-2569-vurdering med betinget logikk for alle helsekategorier → sanntids beslutningsstøtte fra Førerkortveileder → journalnotat i EPJ → tre PDF-versjoner (lege, innbygger, SVV) → elektronisk innsending til Statens vegvesen. Krever legregistrering i «Smart register».

---

## 3. SMART on FHIR — selve kjernen

### Felles, korrekte valg (alle tre)
Alle implementerer SMART App Launch IG og BFF-mønsteret «token forlater aldri nettleseren». Alle bruker server-side authorization code-flyt med `state`-parameter, `/.well-known/smart-configuration`-discovery, og `aud` = FHIR-base-URL (iss). NHNs egen implementasjonsguide bekrefter at dette er korrekte valg i norsk sektor.

NHNs guide spesifiserer at `state` skal ha **minimum 122-bit entropi** og bindes til brukersesjon. `forer` bruker `RandomNumberGenerator.GetBytes(32)` = 256 bit — tilfredsstiller dette.

### Der de skiller lag

**`forer` skriver SMART-klienten selv** i én controller — pedagogisk lesbart, men mangler tokenvalidering, refresh-håndtering, distribuert sesjon og en fylt issuer-allowlist (alle erkjent i egne docs).

**`syk-inn` har trukket SMART-klienten ut i `@navikt/smart-on-fhir`** med `autoRefresh`, `enableMultiLaunch`, token-validering, Valkey-sesjon og allowlist per miljø.

**NHN Førerrett-App** bruker NHNs egen plattform — kode ikke offentlig, men HelseID er obligatorisk og NHNs implementasjonsguide bekrefter HTTP Basic auth for konfidensielle klienter (`client_id:client_secret`).

---

## 4. Arkitektur og plattformvalg — det viktigste skillet

Dette er det viktigste funnet fra NHN-dokumentasjonen: alle tre apper gjør SMART EHR Launch, men de har valgt **tre forskjellige skjemaplattformer**:

| Plattform | App | Hva plattformen gir gratis |
|---|---|---|
| **Altinn Studio** | `forer-legeerklaering` | Signering (BPMN), arkiv, PDF, XACML-autorisasjon, ID-porten |
| **Helsenorge** | NHN Førerrett-App | HelseID, skjemakatalog, innbyggerportal, SVV-integrasjon, NA-0201-tilgang |
| **Egenutviklet (NAIS)** | `syk-inn` | Full kontroll, to launch-kontekster (FHIR + standalone), Valkey, NAV-regelmotor |

**Konsekvens for strategivalg:**

- Altinn Studio er sterk der helseskjemaer trenger formell signering, saksbehandling og arkiv i Altinn — f.eks. attester og skjemaer som skal til Digdir-eide tjenester.
- Helsenorge-plattformen er sterk der helseskjemaer skal integrere med innbyggerportalen, trenger HelseID, og skal til helsesektoren direkte (SVV, Hdir).
- For legeerklæring IS-2569 spesifikt har **NHN allerede valgt Helsenorge** og er i produksjon. Digdir PoC-en viser at det *også* er mulig på Altinn, men mottaksarkitekturen (hvem eier skjemaet hos SVV?) er uavklart (BESLUTNINGER.md C-3).

---

## 5. Writeback til EPJ

| | `forer` | `syk-inn` | NHN Førerrett-App |
|---|---|---|---|
| Writeback implementert | Nei | Ja | Ja |
| Format | — | DocumentReference + QuestionnaireResponse | Journalnotat + PDF |
| Idempotens | — | PUT med klient-tildelt id + GET-sjekk | Ukjent |
| Kanonisk Questionnaire | — | Publisert på `/fhir/R4/Questionnaire/V1` | Integrert med Helsenorge skjemakatalog |

NHNs tilnærming er interessant: de skriver journalnotat og PDF direkte i EPJ, i motsetning til `syk-inn` som skriver strukturerte FHIR-ressurser. For en legeerklæring (med signert PDF som juridisk dokument) kan NHNs tilnærming være mer hensiktsmessig enn `syk-inn`s QuestionnaireResponse-mønster.

---

## 6. Egenerklæring (NA-0201) — et unikt gap

NHN Førerrett-App gir legen tilgang til pasientens selvrapporterte helseerklæring (NA-0201) direkte i SMART-appen. Dette er beskrevet som en kjernefunksjon.

`forer` har dokumentert dette i [PASIENTFLYT.md](PASIENTFLYT.md) som et fremtidig arkitekturforslag (via Dialogporten og helsenorge.no), men det er ikke implementert. NHNs løsning viser at dette er teknisk gjennomførbart — og at det er en forventet del av flyten for en produksjonsklar løsning.

---

## 7. Autentisering og autorisasjon

**`forer`**: Altinn/ID-porten + XACML (`policy.xml`). HelseID planlagt men ikke kodet — nå unblokket (klientregistrering er gjennomført per 2026-06-17).

**`syk-inn`**: Wonderwall-sidecar injiserer HelseID access token. Pilotbruker-sjekk (`assertIsPilotUser(hpr)`) som gate-keeper. Shadow-validering av HelseID-on-FHIR-claim.

**NHN Førerrett-App**: HelseID obligatorisk. «Smart register» — legene må registreres i NHNs register før de får tilgang. Dette er et governance-krav som ikke finnes i de to andre appene.

---

## 8. Sikkerhet

| Tema | `forer` | `syk-inn` | NHN Førerrett-App |
|---|---|---|---|
| Token forlater nettleser | Nei (BFF) ✔ | Nei (BFF) ✔ | Nei (antatt) ✔ |
| PKCE + state | Ja, 256-bit ✔ | Ja (via pakke) ✔ | Ja (NHN-guide krever 122-bit+) ✔ |
| Tokenvalidering | **Nei** (erkjent gap) | Ja ✔ | Ja (HelseID) ✔ |
| Refresh token | Etterspørres, ikke håndtert | `autoRefresh` ✔ | Ukjent |
| Issuer-allowlist | **Tom** → nå unblokket | Kjente servere per miljø ✔ | HelseID only ✔ |
| Sesjonslagring | In-memory | Valkey (distribuert) ✔ | Ukjent |
| Audit-logging | Ikke implementert | OTel + Loki ✔ | NHN-guide krever det ✔ |
| Risikodokumentasjon | BESLUTNINGER.md (delvis) | Implisitt i NAIS-deploy | NHN-guide krever formell risikovurdering |

NHNs implementasjonsguide setter eksplisitte krav til audit-logging og risikovurdering — dette er sektorkrav som gjelder `forer` på vei mot produksjon, uavhengig av plattform.

---

## 9. IS-2569 og domenedekning

| | `forer` | NHN Førerrett-App |
|---|---|---|
| Prefill fra FHIR | Patient, Practitioner, Encounter, Organization, Condition ✔ | Ja |
| Betinget logikk | Ingen | Fullt implementert (alle helsekategorier) |
| Helsekategorier | 1 (diagnose) av 17+ | Alle |
| Egenerklæring (NA-0201) | Ikke implementert | Implementert |
| Kjøretøygrupper | Kodeverk implementert ✔ | Fullt implementert |
| Beslutningsstøtte | Ingen | Førerkortveileder integrert |
| Innsending SVV | Altinn-arkiv (uavklart mottaker) | Elektronisk direkte til SVV ✔ |

---

## 10. Testing, drift og dokumentasjon

| Tema | `forer` | `syk-inn` | NHN Førerrett-App |
|---|---|---|---|
| Automatiserte tester | Ingen | 23 unit + 24 Playwright e2e | Ukjent |
| Deployment | Helm/Altinn | NAIS, 2–4 replikaer | Helsenorge-plattform |
| Observability | Ikke implementert | OTel, Grafana Faro, Loki | Ukjent |
| Dokumentasjon (arkitektur) | ~13 500 ord, åpen | ADR-er, FHIR-docs, åpen | Confluence (begrenset tilgang) |
| Kontakt/community | GitHub | GitHub | Slack `ext-utv-hn-forerrett` |

---

## 11. Hva kan `forer-legeerklaering` lære av NHN-dokumentasjonen?

**Fra implementasjonsguiden (normsetting):**
- NHN bekrefter alle våre SMART-valg som korrekte (flyt, scopes, PKCE, HTTP Basic auth for konfidensielle klienter)
- Audit-logging og formell risikovurdering er sektorkrav, ikke valgfrie — bør inn i BESLUTNINGER.md og VEIKART.md
- «Smart register»-konseptet viser at governance rundt hvilke leger som kan bruke appen er et reelt krav, ikke bare en teknisk detalj

**Fra Førerrett-App (domenekunnskap):**
- Egenerklæring (NA-0201) er en forventet del av legeflyten — ikke bare fremtidig ønsket funksjonalitet
- Elektronisk innsending til SVV krever integrasjon utover Altinn-arkiv — mottaksarkitektur (BESLUTNINGER.md C-3) er kritisk vei
- Betinget logikk for alle 17+ helsekategorier er omfanget for en produksjonsklar løsning

**Strategisk spørsmål som bør inn i BESLUTNINGER.md:**
- Skal Digdir bygge videre på Altinn Studio (som dette PoC viser er mulig), eller bør løsningen bygges på eller i samarbeid med Helsenorge-plattformen der NHN allerede er i produksjon?

---

## 12. Konklusjon

De tre appene er ikke konkurrenter — de representerer tre ulike plattformvalg for det samme protokollaget (SMART on FHIR):

- **`forer-legeerklaering`** beviser at Altinn Studio kan brukes som skjemaplattform for helsedokumentasjon med FHIR-prefill. Det er en veldokumentert PoC med riktige arkitektoniske instinkter og klare erkjente gap.
- **`syk-inn`** er malen for en produksjonsklar SMART on FHIR-klient i .no-sektoren: trukket ut bibliotek, refresh, writeback, full testpyramide, observability.
- **NHN Førerrett-App** er produksjonsimplementasjonen av *nøyaktig det samme domenet* som PoC-en — på Helsenorge-plattformen med HelseID, NA-0201-integrasjon, betinget IS-2569-logikk og elektronisk SVV-innsending.

Det sentrale strategiske spørsmålet som `forer`-prosjektet nå bør besvare: **Er Altinn Studio den rette plattformen for legeerklæring IS-2569, eller skal Digdir bidra til / bygge på NHNs eksisterende produksjonsløsning?** Svaret avhenger av mottaksarkitektur (SVV, BESLUTNINGER.md C-3), DPIA (C-4), og hvilken part som er behandlingsansvarlig.

Uavhengig av plattformvalg gjelder veikartets fase 1–3 (tokenvalidering, refresh, writeback, tester) for begge alternativer.


---

# 9. NHN-dokumentasjon

# NHN-dokumentasjon — SMART App Launch + Førerrett-App

**Kilde:** Helsenorge Confluence (helsenorge.atlassian.net)  
**Hentet:** 2026-06-17  
**Sider:**
- [Implementasjonsguide SMART App Launch Framework](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/67469415/Implementasjonsguide+SMART+App+Launch+Framework) (oppdatert 7. mai 2025)
- [Smart-On-Fhir Førerrett-App](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/2846392337/Smart-On-Fhir+F+rerrett-App)

---

## 1. Implementasjonsguide SMART App Launch Framework

### Omfang og brukstilfelle

NHNs implementasjonsguide dekker **Use Case 4**: tredjepartsapplikasjoner for helsepersonell som startes fra EPJ eller portal. Det er nøyaktig dette brukstilfelle `forer-legeerklaering`-PoC-en implementerer.

> "SMART gir tredjepartsapplikasjoner autorisert tilgang til data i elektroniske pasientjournaler via en pålitelig og sikker autorisasjonsprotokoll"

### Autorisasjonsflyt (seksjon A–G)

Flyten samsvarer med SMART App Launch IG v2.2.0 og er den samme som PoC-en implementerer:

1. EPJ åpner app i integrert nettleser med `iss` (FHIR-endepunkt) og `launch` (unik kontekstidentifikator)
2. App henter SMART-metadata fra `/metadata/` eller `/.well-known/smart-configuration.json`
3. App redirecter til autorisasjonsendepunkt med `response_type=code`, `client_id`, `redirect_uri`, `scope`, `state`, `aud`
4. Autorisasjonsserver returnerer kode
5. App bytter kode mot access token via POST til token-endepunkt
6. App bruker Bearer-token i Authorization-header mot FHIR-APIet

Eksempel på autorisasjonsforespørsel:
```
GET https://ehr/authorize
  ?response_type=code
  &client_id=app-client-id
  &redirect_uri=https://app/after-auth
  &launch=xyz123
  &scope=launch+patient/Observation.read+patient/Patient.read+openid+fhirUser
  &state=98wrghuwuogerg97
  &aud=https://ehr/fhir
```

### Scopes — tre kategorier

| Kategori | Eksempler | Formål |
|---|---|---|
| Kliniske data | `patient/Observation.read`, `user/Observation.read`, `patient/*.read` | Tilgang til FHIR-ressurser for pasient eller bruker |
| Kontekstuell | `launch`, `launch/patient` | Motta kontekst fra EPJ / velg pasient ved frittstående start |
| Identitet | `openid`, `fhirUser` | OpenID Connect — returnerer `id_token` med brukerinformasjon |

### Sikkerhetskrav

| Krav | Detalj |
|---|---|
| Transport | TLS påkrevd for all kommunikasjon |
| `state`-parameter | Minimum 122-bit entropi; must binde til brukersesjon mot CSRF og session fixation |
| Response-headere | `Cache-Control: no-store` og `Pragma: no-cache` på token-responser |
| Klienttyper | Offentlige apper: ingen client secret, HTTPS redirect_uri-validering. Konfidensielle apper: HTTP Basic auth (`client_id:client_secret`) |
| Autorisasjonskoder | Kort levetid (typisk ~1 minutt) |
| Refresh tokens | Bundet til samme `client_id`, samme eller delmengde av original autorisasjon |
| Lagringssted | Tokens skal lagres i applikasjonsspesifikt lagringssted, ikke systemtilgjengelig storage |
| Revisjon | Audit-logging av tilgangsbeslutninger påkrevd |
| Risikostyring | Risikovurdering påkrevd før implementasjon |

### Governance

- Refererer til Normen (norsk rammeverk for informasjonssikkerhet i helse)
- Refererer til Direktoratet for e-helses arkitektur for datadeling og dokumentdeling
- Databehandleravtaler påkrevd der relevant
- Lenker til HelseAPI-implementasjonsguide

---

## 2. Smart-On-FHIR Førerrett-App (NHNs produksjonsapp)

Dette er **produksjonsimplementasjonen** av nøyaktig det `forer-legeerklaering`-PoC-en søker å bevise. NHN har altså allerede bygget og driftsatt en slik løsning for Statens vegvesen.

### Formål

> Genererer helsevurderingskonklusjoner basert på Norges «Førerkortveileder» gjennom registrert helseinformasjon.

Legen bruker appen til å fylle ut IS-2569 (helseattest for førerrett) med beslutningsstøtte, og appen sender konklusjonen elektronisk til Statens vegvesen.

### Kjernefunksjoner

- **Automatisk datafylling** — pasient- og legeinformasjon hentes fra EPJ via FHIR
- **Sykdomsspesifikke vurderingsområder** med betinget beslutningslogikk (f.eks. insulinbruk ved diabetes utløser tilleggssjekker)
- **Beslutningsstøtte i sanntid** basert på regulatoriske krav i Førerkortveiledere
- **Kontekstuell veiledning** med lenker til offisielle retningslinjer
- **Egenerklæring fra innbygger** — legen kan se pasientens selvrapporterte helseerklæring (tilsvarer NA-0201) der denne er fylt ut på Helsenorge

### Arbeidsflyt

1. Lege velger pasient i EPJ → starter appen via SMART EHR Launch
2. Gjennomgår pasientens selvrapporterte helseerklæring (om tilgjengelig)
3. Fyller ut strukturert helsevurdering på tvers av flere sykdomskategorier
4. System stiller betingede oppfølgingsspørsmål basert på svar
5. Genererer hovedkonklusjon med anbefalinger
6. Skaper dokumentasjon for arkivering og regulatorisk innsending

### Outputs — tre PDF-versjoner

| Dokument | Mottaker |
|---|---|
| Journalnotat | Arkiveres i EPJ |
| Legekopi (PDF) | Legen |
| Borgerkopi (PDF) | Innbygger via Helsenorge |
| Trafikkstasjonskopi (PDF) | Statens vegvesen |
| Elektronisk innsending | Statens vegvesen (automatisk) |

### Teknisk plattform

- **Autentisering:** HelseID (obligatorisk for integrerte EPJ-er)
- **Skjemakatalog:** Helsenorge-plattformen (ikke Altinn Studio)
- **Journalskriving:** Oppretter journaloppføringer og PDF-dokumenter i EPJ ved fullføring
- **Innsending:** Elektronisk overføring av godkjente konklusjoner til Statens vegvesen
- **«Smart register»:** Leger som bruker appen må registreres — plattformen sporer hvilke helsepersonell som har adoptert apper, og muliggjør tilpasning av plattformfunksjoner
- **Minimale FHIR-krav:** Tillater delvis implementasjon hos EPJ-leverandører

### Kontakt og tilgang

- Slack-kanal for utviklere: `ext-utv-hn-forerrett` på Helsenorge Slack
- Demo-tilgang krever direkte kontakt med Helsenorge-organisasjonen

### Merknad — Interaktor

**Interaktor** er nevnt i bransjedokumentasjon som en norsk app-plattform for å kjøre SMART on FHIR-apper integrert i
EPJ-systemer. Dette er en annen tilnærming enn Helsenorge-plattformen og Altinn Studio — en «shell» som EPJ-leverandør
integrerer én gang, og som deretter kan lansere godkjente SMART-apper for legens regning. Relevansen for
`forer`-prosjektet: Interaktor kan være et alternativt distribusjonskanal som reduserer avhengigheten av at hver
EPJ-leverandør selv implementerer full SMART App Launch-støtte. Bør undersøkes videre.

---

## 3. Nøkkelobservasjoner for `forer-legeerklaering`-prosjektet

### Bekreftelser

- NHNs guide bekrefter at vår SMART EHR Launch-implementasjon (flyt, scopes, PKCE, state, konfidensielt klient) er korrekt og i tråd med norsk sektornorm.
- `aud`-parameteren skal være FHIR-base-URL (iss) — slik vi allerede har implementert.
- HTTP Basic auth for konfidensielle klienter er riktig mønster.

### Viktigste gap mot NHNs produksjonsapp

| Område | `forer-legeerklaering` PoC | NHN Førerrett-App (prod) |
|---|---|---|
| Plattform | Altinn Studio | Helsenorge |
| Autentisering | Altinn/ID-porten (HelseID planlagt) | HelseID (obligatorisk) |
| IS-2569-dekning | ~4 av 17+ helsekategorier | Komplett med betinget logikk |
| Egenerklæring (NA-0201) | Planlagt (PASIENTFLYT.md) | Implementert — legen ser den |
| Writeback til EPJ | Ikke implementert | Journalnotat + PDF i EPJ |
| Innsending til SVV | Ikke implementert | Elektronisk automatisk |
| «Smart register» | Ikke relevant (Altinn) | Obligatorisk |
| Beslutningsstøtte | Ingen | Fullt implementert |

### Strategisk implikasjon

NHNs løsning og Digdirs PoC er **komplementære, ikke konkurrerende**:
- NHN bruker **Helsenorge** som skjemaplattform med native HelseID-integrasjon
- Digdir PoC bruker **Altinn Studio** som skjemaplattform med Altinn-infrastruktur (signering, arkiv, XACML)

Spørsmålet om hvilken plattform som er riktig for fremtidige helseskjemaer (Altinn vs. Helsenorge) er en åpen arkitekturavklaring som bør løftes i `BESLUTNINGER.md`.


---

# 10. Kartlegging av rapporteringsplikter

# Kartlegging av rapporteringsplikter for helsepersonell

Grunnlag for vurdering av forenklingspotensial – primær- og spesialisthelsetjenesten, offentlig og privat.
Versjon per juni 2026.

Tabellene dekker lovpålagte og avtalebaserte meldinger, rapporteringer og attester som helsepersonell og
virksomheter har overfor det offentlige **og** privat sektor. Hver rad er vurdert for om en
**SMART on FHIR**-applikasjon (app som startes i journalen, leser/skriver strukturerte data via FHIR) gir verdi.

> **Forbehold:** Listen er ikke uttømmende, og begreper/organisering endres jevnlig. Statens legemiddelverk er nå
> Direktoratet for medisinske produkter (DMP); Fylkesmannen heter Statsforvalteren; Reseptregisteret er erstattet av
> Legemiddelregisteret (LMR); Direktoratet for e-helse ble innlemmet i Helsedirektoratet i 2024. Rettslige grunnlag er
> forenklet – kontrollér mot gjeldende lov/forskrift før konkrete tiltak.

## Forklaring

| Begrep | Betydning |
|---|---|
| **Sektor** | Primær = fastlege/kommunal; Sekundær = spesialist/sykehus; Begge = gjelder begge |
| **Innsamling** | *Automatisk* = batch-uttrekk fra fagsystem; *Delvis* = noe manuelt; *Manuell* = skjema/portal/papir |
| **SoF-verdi** | Antatt verdi av en SMART on FHIR-app: **Høy** / Middels / Lav (begrunnet i eget kapittel) |

## Bærende prinsipp: generelt vs. profesjonsspesifikt

To typer plikter må skilles:

- **Generelle helsepersonell-plikter** (helsepersonelloven §§ 32–38) gjelder *alle* autoriserte yrkesgrupper likt –
  uavhengig av profesjon, sektor og offentlig/privat. Det er rad **D1** (barnevern), **D2** (sosial-/kommunal
  tjeneste), opplysningsplikt ved vold/kjønnslemlestelse, **C1** (varsel alvorlig hendelse) og **C4** (pasientskade).
  Disse gjentas derfor ikke per yrkesgruppe i del 2 – de ligger «over» tabellen.
- **Profesjonsspesifikke plikter** følger av hva slags helsehjelp som ytes (refusjonsordninger, attester, fagregistre,
  tvangslovgivning). Det er disse som skiller yrkesgruppene, og som dekkes i del 2 nedenfor.

---

# Del 1 — Alle helsepersonell og leger/virksomheter

## A. Sentrale / nasjonale helseregistre

Disse er i hovedsak *register-leveranser* – data høstes som batch fra fagsystem. SMART on FHIR (en interaktiv app i
behandlerens flyt) gir derfor mest verdi der kliniker fortsatt fyller ut en strukturert melding manuelt.

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| A1 | Norsk pasientregister (NPR) | Sekundær | Helsedirektoratet | Uttrekk fra EPJ/PAS | NPR-forskriften | Automatisk | Lav |
| A2 | Kommunalt pasient- og brukerregister (KPR), inkl. IPLOS | Primær | FHI | Fagsystem-uttrekk; KUHR daglig; IPLOS-registrering | KPR-forskriften | Delvis | Middels |
| A3 | Medisinsk fødselsregister (MFR) | Sekundær | FHI | Elektronisk melding | hpl §35; MFR-forskriften | Automatisk | Lav |
| A4 | Dødsmelding / Dødsårsaksregisteret (eDÅR) | Begge | FHI | Elektronisk dødsmelding | hpl §36; DÅR-forskriften | Automatisk | Lav |
| A5 | MSIS – smittsom sykdom (klinikermelding) | Begge | FHI + kommunelege | MSIS-skjema / elektronisk | smittevernloven §2-3; hpl §37 | Delvis | Middels |
| A6 | MSIS-labdatabasen | Sekundær | FHI | Elektronisk | MSIS-forskriften | Automatisk | Lav |
| A7 | Tuberkuloseregister | Begge | FHI + TB-koordinator | Skjema | Tuberkuloseforskriften | Manuell | Middels |
| A8 | SYSVAK – vaksinasjonsregister | Begge | FHI | Elektronisk fra EPJ/vaksinemodul | SYSVAK-forskriften | Automatisk | Lav |
| A9 | Legemiddelregisteret (LMR) | Begge | FHI | Elektronisk fra apotek | LMR-forskriften | Automatisk | Lav (plikt på apotek) |
| A10 | Kreftregisteret (klinisk melding) | Begge | Kreftregisteret | Elektronisk melding / KREMT | Kreftregisterforskriften | Delvis | **Høy** |
| A11 | Hjerte- og karregisteret (HKR) | Sekundær | FHI | Elektronisk / kvalitetsregistre | HKR-forskriften | Delvis | Middels |
| A12 | Abortregisteret | Sekundær | FHI (MFR) | Skjema / elektronisk | abortloven | Delvis | Lav |
| A13 | NOIS / NORM / RAVN (infeksjon/resistens) | Sekundær | FHI | Elektronisk / uttrekk | resp. forskrifter | Automatisk | Lav |
| A14 | Helsearkivregisteret | Begge | Norsk helsearkiv | Avlevering | helsearkivforskriften | Manuell | Lav |

## B. Medisinske kvalitetsregistre

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| B1 | Nasjonale medisinske kvalitetsregistre (~50+) | Begge | Det enkelte registeret | Egne innregistreringsportaler | forskrift om medisinske kvalitetsregistre §2-3 | Delvis | **Høy** |
| B2 | Årlig statusrapport (registernivå) | Begge | SKDE | Rapport | forskrift om nasjonale kvalitetsregistre | Manuell | Lav |

## C. Hendelser, skader og svikt

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| C1 | Varsel om alvorlig hendelse | Begge | Statens helsetilsyn + Ukom | melde.no / varselordning | sphlsl. §3-3a; helsetilsynsloven | Manuell | Middels |
| C2 | Bivirkningsmelding legemiddel | Begge | DMP / RELIS | melde.no | legemiddelforskriften; hpl | Manuell | **Høy** |
| C3 | Hendelse med medisinsk utstyr | Begge | DMP | melde.no | forskrift om medisinsk utstyr / MDR | Manuell | Middels |
| C4 | Pasientskade – tilleggsinfo til sak | Begge | Norsk pasientskadeerstatning (NPE) | NPE-portal / skjema | pasientskadeloven | Manuell | Middels |

## D. Meldeplikt til andre myndigheter

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| D1 | Melding/opplysninger til barnevernet | Begge | Kommunal barneverntjeneste | Skjema/brev/portal (varierer) | hpl §33; barnevernsloven | Manuell | Middels |
| D2 | Opplysninger til kommunal helse-/sosialtjeneste | Begge | Kommunen / NAV | Brev / skjema | hpl §32 | Manuell | Lav |
| D3 | Melding til politiet om unaturlig dødsfall | Begge | Politiet (+ Statsforvalter) | Telefon/personlig + skjema | hpl §36; forskrift om leges melding | Manuell | Lav |
| D4 | Førerkort: helseattest | Begge | Statens vegvesen (Statsforvalter ved tvil) | Papir/PDF båret av pasient | førerkortforskriften vedlegg 1 | Manuell | **Høy** (er pilot) |
| D5 | Førerkort: melding om manglende helsekrav (>6 mnd) | Begge | Statsforvalteren | Skjema/brev | hpl §34; førerkortforskriften | Manuell | **Høy** |
| D6 | Melding om arbeidsrelatert sykdom (skjema 154B) | Begge | Arbeidstilsynet (Havtil/Luftfartstilsynet) | PDF-skjema per post / eDialog / Altinn | arbeidsmiljøloven §5-3 | Manuell | **Høy** |
| D7 | Helseerklæring arbeidsdykking | Begge | Arbeidstilsynet | Egen attest | dykkeforskrift | Manuell | Middels |
| D8 | Tilrettelegging ved graviditet (arbeid) | Begge | Arbeidstilsynet / arbeidsgiver | Erklæring | aml. | Manuell | Middels |
| D9 | Sertifikatattester (sjøfart, dykk, luftfart, jernbane) | Begge | Sjøfartsdir., Luftfartstilsynet, SJT, Havtil | Egne ordninger per sektor | sektorlovgivning | Manuell | Middels |

## E. NAV / trygd / refusjon

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| E1 | Sykmelding | Begge | NAV | Elektronisk fra EPJ (lovpålagt) | folketrygdloven | Automatisk | Middels |
| E2 | Legeerklæringer/uttalelser (L-takster) | Begge | NAV | Elektronisk (lovpålagt) | folketrygdloven §21-4 vedlegg 1 | Delvis | **Høy** |
| E3 | Oppgjørskrav L-takster | Begge | Helfo / NAV økonomi | Elektronisk regning (EHF) | folketrygdloven kap. 22 | Automatisk | Lav |
| E4 | Refusjonskrav / direkteoppgjør (KUHR) | Begge | Helfo (→ KUHR → KPR) | Elektronisk fra EPJ | folketrygdloven; oppgjørsavtale | Automatisk | Lav |
| E5 | Praksisinformasjon / direkteoppgjørsavtale | Begge | Helfo | Praksisinformasjon-portal | folketrygdloven | Manuell | Lav |
| E6 | Oppfølgingsplan / dialogmeldinger | Primær | NAV / arbeidsgiver | Dialogmelding i EPJ | – | Automatisk | Lav |
| E7 | Blåreseptsøknad / individuell stønad + vedtak | Begge | Helfo | Tjenesteportalen / EPJ | blåreseptforskriften | Delvis | Middels |

## F. Attester til kommune, fylke, utdanning og dagligliv

Disse er stort sett **strukturerte attester kliniker fyller ut manuelt**, sendt via varierende kanaler – et tydelig
forenklingsområde.

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| F1 | TT-kort (tilrettelagt transport) | Primær | Fylkeskommune/kommune | Attest/skjema | Ingen felles nasjonal forskrift — kommunalt/fylkeskommunalt regelverk (varierer) | Manuell | **Høy** |
| F2 | HC-kort (parkering for forflytningshemmede) | Primær | Kommune | Attest/skjema | forskrift om parkering for forflytningshemmede | Manuell | **Høy** |
| F3 | Legeerklæring ved skolefravær | Primær | Skole/kommune | Attest | Ingen særskilt lovkrav om legeattest — skolens/kommunens fraværsreglement (jf. opplæringsloven) | Manuell | Middels |
| F4 | Sykmelding / nedsatt funksjonsevne for studenter | Primær | Lånekassen | Sykmelding/erklæring | forskrift om tildeling av utdanningsstøtte (sykdom/nedsatt funksjonsevne) | Manuell | **Høy** |
| F5 | Helsekort for gravide | Primær | Helsetjenesten (deles) | Digitalt helsekort (innføres 2026–27) | Ikke kartlagt i detalj — Helsedirektoratets retningslinjer for svangerskapsomsorg | Delvis | Lav (løses via NHN) |
| F6 | Div. attester (ammende mødre, treningssenter, legemidler til utenlandsreise m.m.) | Primær | Private/diverse aktører | Fritekst-attest | Ingen særskilt lovregulering — anmodning/avtale med mottaker | Manuell | Middels |

## G. Privat sektor – forsikring og andre

Privat sektor er en stor og i dag svært manuell konsument av legeerklæringer. Se eget kapittel om DSOP + SMART on FHIR.

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| G1 | Erklæring ved forsikringstegning | Begge | Forsikringsselskap | Via Helsenett (Finans Norge-flyt) / brev | samtykke; sakkyndig | Manuell | **Høy** |
| G2 | Erklæring til erstatnings-/skadesak | Begge | Forsikringsselskap | Via Helsenett / brev | samtykke; sakkyndig | Manuell | **Høy** |
| G3 | Sakkyndig-/behandlererklæring | Begge | Forsikring/justis | Brev / portal | habilitet, objektivitet, honorar per avtale | Manuell | Middels |
| G4 | Erklæring/attest til justissektoren (forfall, soningsdyktighet, prøveløslatelse m.m.) | Begge | Domstol / kriminalomsorg | Brev / attest | pasientens anmodning / pålegg | Manuell | Lav |

## H. Styring, statistikk og tilskudd

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| H1 | KOSTRA / kommunal aktivitetsrapportering | Primær | SSB | Altinn / SSB | forskrift om rapportering fra kommuner og fylkeskommuner (KOSTRA-forskriften) | Delvis | Lav |
| H2 | Helsestatistikk | Begge | SSB | SSB / Altinn | statistikkloven | Delvis | Lav |
| H3 | Tilskudds-/aktivitetsrapportering (ALIS m.m.) | Begge | Helsedir / Statsforvalter | Altinn / ulike portaler | Varierer per tilskuddsordning (Helsedir/Statsforvalter-regelverk) | Manuell | Lav |
| H4 | Strålebruk / strålevern | Begge | DSA | DSA-ordninger | strålevernforskriften (strålevernloven) | Manuell | Lav |

---

# Del 2 — Profesjonsspesifikke plikter

Utvidelse til tannleger/tannpleiere, fysioterapeuter, manuellterapeuter, kiropraktorer, psykologer,
psykiatere/psykisk helsevern, apotek/farmasøyter, jordmødre/helsesykepleiere, optikere, ergoterapeuter,
bioingeniører/laboratorier og radiografer. Mange av disse bruker EPJ og en stor andel er private.

> ID-er som starter med bokstav (A–H) viser til del 1 over. Nye rader her har profesjonsprefiks
> (TANN, FYS, KIR, PSY, PHV, APO, JOR, OPT, ERG, LAB, RAD).

## Oversikt per yrkesgruppe

| Yrkesgruppe | Sektor | Off./privat | EPJ | Tyngste profesjonsspesifikke plikter |
|---|---|---|---|---|
| Tannlege / tannpleier | Primær (fylkeskom. + privat) | Begge | Ja | Helfo-stønad, bivirkning dentale materialer, KPR tannhelse |
| Fysioterapeut | Primær | Begge | Ja | Helfo direkteoppgjør (KUHR), KPR, NAV-erklæring på bestilling |
| Manuellterapeut | Primær | Mest privat | Ja | Sykmelding, henvisning, Helfo |
| Kiropraktor | Primær | Mest privat | Ja | Sykmelding, henvisning, Helfo |
| Psykolog | Begge | Begge | Ja | NAV-erklæring, førerkort-meldeplikt, sakkyndigerklæring, NPR/BUP |
| Psykiater / psykisk helsevern | Sekundær | Mest offentlig | Ja | Tvangsvedtak til kontrollkommisjon, tvangsdata til NPR |
| Apotek / farmasøyt | – | Mest privat | Eget fagsystem | Blåresept-oppgjør, LMR, e-resept, SYSVAK |
| Jordmor / helsesykepleier | Primær | Mest offentlig | Ja | MFR, helsekort gravide, KPR helsestasjon, SYSVAK |
| Optiker | Primær | Privat | Ja | Førerkort syns-/helseattest og meldeplikt |
| Ergoterapeut | Primær | Mest offentlig | Ja | Hjelpemiddelsøknad til NAV, KPR |
| Bioingeniør / laboratorium | Sekundær | Begge | LIS | MSIS-lab, NOIS/NORM/RAVN, patologi til Kreftreg. |
| Radiograf / bildediagnostikk | Sekundær | Begge | RIS/PACS | Strålevern (DSA), privatfinansiert mammografi |

## Tannlege / tannpleier

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| TANN1 | Stønad til tannbehandling / direkteoppgjør | Helfo | Elektronisk refusjonskrav fra EPJ | ftrl §5-6, §5-6a, §5-25 | Automatisk | Lav |
| TANN2 | Egenandel / frikort | Helfo | Elektronisk (med oppgjør) | frikortordningen | Automatisk | Lav |
| TANN3 | Bivirkning av odontologiske materialer | Bivirkningsgruppen for odontologiske biomaterialer (NIOM/UiB) | Eget skjema (takst 6/10) | stønadsforskrift/hpl | Manuell | **Høy** |
| TANN4 | KPR – tannhelsetjeneste | FHI | Fagsystem-uttrekk (fra 2026) | KPR-forskriften | Delvis | Lav |

Delte plikter: NPR ved kjevekirurgi i sykehus (A1), forsikringsattester (G1–G2), generelle §§32–38-plikter.

## Fysioterapeut, manuellterapeut og kiropraktor

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| FYS1 | Direkteoppgjør / refusjonskrav (KUHR) | Helfo (→ KUHR → KPR) | Elektronisk fra EPJ | ftrl; oppgjørsavtale | Automatisk | Lav |
| FYS2 | KPR KUHR – fysioterapi | FHI | Via KUHR | KPR-forskriften | Automatisk | Lav |
| KIR1 | Sykmelding (kiropraktor/manuellterapeut, avgrenset varighet) | NAV | Elektronisk fra EPJ | folketrygdloven | Delvis | Middels |
| KIR2 | Henvisning til spesialist/bildediagnostikk | Spesialisthelsetjeneste | Henvisningsmelding i EPJ | – | Automatisk | Lav |

Delt plikt: «Legeerklæring ved arbeidsuførhet» kan skrives av psykolog, fysioterapeut, manuellterapeut og kiropraktor
når NAV særskilt ber om det (utvidelse av rad **E2**).

## Psykolog

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| PSY1 | NAV-erklæring på bestilling | NAV | Elektronisk / skjema | ftrl §21-4 | Delvis | **Høy** |
| PSY2 | Førerkort: melding om manglende helsekrav (bl.a. kognitiv funksjon) | Statsforvalteren | Skjema/brev | hpl §34; førerkortforskriften | Manuell | **Høy** |
| PSY3 | Sakkyndig-/behandlererklæring (forsikring, barnevern, justis) | Privat/offentlig | Brev / portal | samtykke; habilitet/objektivitet | Manuell | Middels |
| PSY4 | Aktivitetsdata psykisk helsevern / BUP | NPR (spesialist) / KPR (kommunal) | Uttrekk fra EPJ | NPR-/KPR-forskriften | Automatisk | Lav |

Delte plikter: barnevern (D1) er særlig sentral; varsel alvorlig hendelse (C1), pasientskade (C4).

## Psykiater / psykisk helsevern

Tvangslovgivningen gir et eget rapporteringslag som ikke finnes for andre grupper.

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| PHV1 | Vedtak om tvang (kap. 4/4A): registrering for samlet oversikt | Kontrollkommisjonen | Registrering i EPJ | psykisk helsevernforskriften | Delvis | Middels |
| PHV2 | Melding om vedtak om tvangsmidler / skjerming | Kontrollkommisjonen | Melding (snarest) | psykisk helsevernforskriften | Manuell | Middels |
| PHV3 | Tvangsdata nasjonalt | NPR (eget tvangsdatasett) | Uttrekk fra EPJ | NPR-forskriften | Automatisk | Lav |
| PHV4 | Melding om overføring / dom til tvungent vern | Pasient/pårørende; påtalemyndighet | Melding (forvaltningsloven §27) | phvl §§4-10, 5-4 | Manuell | Lav |

Delte plikter: alle legeplikter (MSIS A5, dødsmelding A4, bivirkning C2, m.fl.).

## Apotek / farmasøyt

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| APO1 | Blåresept – direkteoppgjør / refusjonskrav | Helfo | Elektronisk fra apoteksystem | ftrl §5-14; blåreseptforskriften | Automatisk | Lav |
| APO2 | Utleverte legemidler til Legemiddelregisteret | FHI | Elektronisk | LMR-forskriften | Automatisk | Lav |
| APO3 | E-resept / Reseptformidleren (ekspedering, oppslag) | NHN (Reseptformidleren) | Apoteksystem | reseptformidlerforskriften | Automatisk | Lav |
| APO4 | Vaksinering i apotek | FHI (SYSVAK) | Elektronisk | SYSVAK-forskriften | Delvis | Middels |
| APO5 | Bivirkningsmelding (farmasøyt kan melde) | DMP / RELIS | melde.no | legemiddelforskriften | Manuell | **Høy** |
| APO6 | Bransjestatistikk + medvirkning ved tilsyn | DMP / Statsforvalter | Diverse | apotekloven | Manuell | Lav |

Merknad: SoF-verdien er gjennomgående lav for apotek fordi de fleste pliktene er automatiserte oppgjør/uttrekk fra
apotekenes fagsystem (ikke EPJ med SMART-apper). Unntaket er bivirkningsmelding (APO5).

## Jordmor / helsesykepleier

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| JOR1 | Fødselsmelding (MFR) | FHI | Elektronisk | hpl §35; MFR-forskriften | Automatisk | Lav |
| JOR2 | Helsekort for gravide | Helsetjenesten (deles) | Digitalt helsekort (innføres 2026–27) | – | Delvis | Lav |
| JOR3 | KPR – helsestasjon og skolehelsetjeneste | FHI | Fagsystem-uttrekk (fra 09/2025) | KPR-forskriften | Delvis | Middels |
| JOR4 | Vaksinasjon (SYSVAK) | FHI | Elektronisk | SYSVAK-forskriften | Automatisk | Lav |

Delte plikter: abortmelding (A12), barnevern (D1).

## Optiker, ergoterapeut, laboratorium og radiograf

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| OPT1 | Førerkort: syns-/helseattest og meldeplikt | Statens vegvesen / Statsforvalter | Attest / melding | førerkortforskriften; hpl §34 | Manuell | **Høy** |
| ERG1 | Søknad om hjelpemidler / funksjonsvurdering | NAV Hjelpemiddelsentral | Skjema / portal | folketrygdloven kap. 10 | Manuell | Middels |
| ERG2 | KPR – kommunal ergoterapi | FHI | Fagsystem-uttrekk | KPR-forskriften | Delvis | Lav |
| LAB1 | Mikrobiologiske prøvesvar (MSIS-lab) | FHI | Elektronisk | MSIS-forskriften | Automatisk | Lav |
| LAB2 | NOIS / NORM / RAVN | FHI | Elektronisk / uttrekk | resp. forskrifter | Automatisk | Lav |
| LAB3 | Patologi-/celleprøvesvar til Kreftregisteret | Kreftregisteret | Elektronisk melding | Kreftregisterforskriften | Delvis | Middels |
| RAD1 | Strålebruk / strålevern | DSA | DSA-ordninger | strålevernforskriften | Manuell | Lav |
| RAD2 | Privatfinansiert bildediagnostikk (bl.a. mammografi) | DSA / Kreftreg. | Under utredning | (foreslått) | Manuell | Lav |

---

# Kanaler og plattformer (referanse)

| Kanal/plattform | Brukes til | Eier/leverandør |
|---|---|---|
| Melde.no | Bivirkninger, medisinsk utstyr, uønskede hendelser | DMP / Helsedir |
| Praksisinformasjon | Direkteoppgjør, praksisopplysninger | Helfo |
| KREMT | Klinisk melding til Kreftregisteret | Kreftregisteret |
| NISSY | Rekvirering av pasientreiser | Pasientreiser |
| Altinn | Arbeidstilsyn, SSB, tilskudd | Digdir |
| Kjernejournal | Oppslag/deling, ikke rapportering i seg selv | NHN / Helsedir |
| Interaktor | App-plattform for å kjøre SMART on FHIR-apper i EPJ | (leverandør) |
| Fiks-plattformen | Kommunal samhandling | KS |
| Norsk helsenett | Felles infrastruktur, meldingsutveksling, forsikringsflyt | NHN |

---

# SMART on FHIR – hvor gir det verdi?

**SMART on FHIR** lar en webapp startes inni journalsystemet (felles pålogging, delt pasientkontekst via OAuth2/OpenID
Connect), lese strukturerte data fra EPJ (`read`-scope) og skrive resultatet tilbake (`write`-scope), typisk som FHIR
`Questionnaire` → `QuestionnaireResponse`. I Norge er dette ikke teoretisk: **førerkort-attesten (D4) er den første
SMART on FHIR-piloten** – prosjekt «Digital førerrett» mellom Vegdirektoratet, Helsedirektoratet og Politiet,
realisert via EPJ-løftet, med nasjonal anbefaling i HITR 1225:2019.

**Verdien er størst når alle disse er oppfylt:**

1. Kliniker fyller i dag ut en **strukturert erklæring/attest manuelt** (ikke et rent register-uttrekk).
2. Mottaker er **ekstern** og kan motta et strukturert svar.
3. Mye av innholdet kan **forhåndsutfylles fra EPJ** (diagnoser, legemidler, funksjon) – sparer tasting og hever kvalitet.
4. **Høyt volum eller høy friksjon** i dag (egen portal, fritekst, papir).

**Lav verdi** der data allerede høstes automatisk som batch (NPR, KPR/KUHR, LMR, SYSVAK, MFR, NOIS/NORM/RAVN) eller
der oppgaven er sjelden/akutt (helsearkiv, politimelding ved unaturlig død). Apotek faller i tillegg utenfor
EPJ-/SMART-økosystemet (eget fagsystem) — forenkling der handler om oppgjørs- og e-reseptkjeden, ikke SMART-apper.

## Rader med høyest SMART on FHIR-verdi

### Fra del 1 (leger og alle helsepersonell)

| ID | Oppgave | Hvorfor høy verdi |
|---|---|---|
| D4/D5 | Førerkort-attest og -melding | Allerede SoF-pilot; mal for resten |
| D6 | Arbeidsrelatert sykdom (154B) | Papir/post i dag; kan forhåndsutfylles fra journal |
| B1 | Medisinske kvalitetsregistre | Største kilde til dobbeltregistrering vs. journal |
| A10 | Kreftregisteret (klinisk melding) | Strukturert melding, ekstern mottaker, dobbeltarbeid |
| C2 | Bivirkningsmelding | Kan forhåndsutfylles fra legemiddelliste; fritekst i dag |
| E2 | NAV-legeerklæringer | Strukturert `Questionnaire`-flyt; prototype mot NAV finnes |
| F1/F2/F4 | TT-kort, HC-kort, Lånekassen | Strukturerte attester, manuelle og spredt i dag |
| G1/G2 | Forsikringserklæringer | Stort privat volum; krever samtykke og sakkyndig-rolle |

### Fra del 2 (profesjonsspesifikke)

| ID | Oppgave | Hvorfor høy verdi |
|---|---|---|
| TANN3 | Bivirkning dentale materialer | Eget papir-/PDF-skjema; kan forhåndsutfylles fra tannlege-EPJ |
| PSY1 | NAV-erklæring (psykolog) | Strukturert erklæringsflyt, samme mønster som leger (E2) |
| PSY2 / OPT1 | Førerkort-meldeplikt (psykolog/optiker) | Utvider den eksisterende Førerrett-piloten til flere profesjoner — én app, flere brukergrupper |
| APO5 | Bivirkningsmelding fra apotek | Kan forhåndsutfylles fra utleverings-/legemiddeldata |

---

# DSOP + SMART on FHIR mot privat sektor

**Kort svar: ja, det bør med, og DSOP er et passende rammeverk.** Begrunnelse:

- **DSOP (Digital Samhandling Offentlig–Privat)** er en etablert modell der finansnæringen (Finans Norge/Bits) og
  offentlige etater digitaliserer dataflyt – f.eks. samtykkebasert lånesøknad og DSOP «Syke- og uføreforsikring», der
  forsikring og NAV utveksler opplysninger som i dag i stor grad går på papir/post.
- Forsikringsbransjen er allerede en **stor mottaker av legeerklæringer** (ved tegning, ved erstatning, og legen som
  sakkyndig). Det finnes allerede en kanal mot Finans Norge via Helsenettet.
- **Den tekniske brikken finnes:** SMART on FHIR i EPJ + felles tillitstjenester / tillitsanker hos Norsk helsenett.
  En forsikringsspesifikk (eller nasjonal) attest-app kan startes i journalen, hente **samtykkebaserte** opplysninger
  fra EPJ, og returnere en strukturert, signert `QuestionnaireResponse` til selskapet – samme mønster som Førerrett,
  men med privat mottaker.

**Forutsetninger og forbehold:**

- **Samtykke og taushetsplikt:** helseopplysninger er særlige kategorier (GDPR). Flyten må være innbygger-/
  pasientstyrt og dataminimerende – bare det erklæringen krever.
- **Legens sakkyndig-rolle:** krav til faglig forsvarlighet, habilitet og objektivitet. SoF kan *forhåndsutfylle*,
  men legen må fortsatt utøve selvstendig vurdering; honorar avtales per time med selskapet.
- **Governance:** hvem godkjenner appen (EPJ-leverandør, virksomhet, NHN), og hvordan sikres at bare rettmessige
  mottakere får tilgang. Tillitsankeret hos NHN er ment å løse dette.

**Anbefalt rekkefølge:** gjenbruk Førerrett-mønsteret (D4) → offentlige strukturerte attester (D6, E2, F1/F2/F4)
→ deretter DSOP-basert privat flyt for forsikring (G1/G2) når samtykke- og tillitstjenestene er modne.

Se [BESLUTNINGER.md](BESLUTNINGER.md) C-7 for strategisk avklaring.

---

# Tverrgående forenklingstemaer

| Tema | Hva vi ser | Mulig grep | Berører |
|---|---|---|---|
| Dobbeltregistrering vs. journal | Samme opplysning i EPJ og deretter manuelt i registre | Automatisk høsting fra strukturert EPJ; «registrer én gang» | B1, A2, A10, A11 |
| Mange portaler og innlogginger | melde.no, KREMT, Praksisinformasjon, NAV, Altinn, kommunale skjema | Felles inngang / SMART-apper i EPJ | C1–C3, D1–D9, F-rader |
| Flere mottakere for samme melding | MSIS til FHI + kommunelege; død → dødsmelding + politi | Meld én gang, distribuér automatisk | A5, A4+D3 |
| Manuelt der digitalt finnes | 154B på papir/post; attester båret av pasient | Strukturert digital innsending (SMART on FHIR) | D4, D6, F1–F4 |
| NAV-erklæringer – volum og fritekst | Stort volum, lite gjenbruk av journaldata — gjelder lege, psykolog, fysio | Strukturerte `Questionnaire`-felt og forhåndsutfylling | E1, E2, PSY1 |
| Privat sektor utenfor digital flyt | Forsikringserklæringer i stor grad manuelle | DSOP + SMART on FHIR med samtykke | G1, G2 |
| Én attest-app, mange profesjoner | Førerkort-meldeplikten gjelder lege, psykolog og optiker | Felles app, ulike brukergrupper — ikke tre separate apper | D4/D5, PSY2, OPT1 |
| Bivirkningsmeldinger er fragmentert | Legemiddel (C2/APO5) og dentale materialer (TANN3) går til ulike mottakere på ulike skjema | Felles strukturert bivirkningsflyt i melde.no | C2, APO5, TANN3 |
| Tvangsrapportering er lite digitalisert | Data skal allerede ligge i EPJ for kontrollkommisjonen | Automatisert uttrekk fremfor manuell melding | PHV1–PHV4 |
| Helfo-oppgjør er automatisert på tvers | Lege, tannlege, fysio og apotek bruker KUHR/blåresept | Gjenbruk fremfor parallell innsamling | E4, TANN1, FYS1, APO1 |
| Variabelutvalg revideres sjelden | Det som legges inn tas sjelden ut | Jevnlig kritisk revisjon av datasettene | Alle registre |

---

# Kilder

- FHI – Sentrale helseregistre; Helsedirektoratet – Helsedata og helseregistre
- FHI – MSIS-håndbok; KPR; Legemiddelregisteret
- Den norske legeforening – «Lovbestemte meldinger» og «Praktisk veileder for legers attestarbeid»
- NAV – samarbeidspartner (sykmelding, legeerklæringer); folketrygdloven §21-4 vedlegg 1
- Helfo – Lege; Helsedirektoratet – KUHR
- Helsedirektoratet – Førerkortveileder; Statsforvalteren – førerkort og helsekrav
- Arbeidstilsynet – Meldeplikta til legane (skjema 154B; aml. §5-3)
- Helsedirektoratet – «Anbefaling om bruk av SMART on FHIR» (HITR 1225:2019); EPJ-løftet; prosjekt Digital førerrett
- Bits/Finans Norge – DSOP; DSOP «Syke- og uføreforsikring»
- Norsk helsenett – tjenester, felles tillitstjenester / tillitsanker
- Helsedatastrategi 2025–2027 (FHI); rapporten «Nå snakker vi» (Helsedir/NAV/Dir. e-helse)
- Norsk Tannlegeforenings veileder; Den norske tannlegeforenings retningslinjer for bivirkningsmelding
- Norsk Psykologforening – fagetiske retningslinjer; NAV-samarbeidsveileder
- Norsk Fysioterapeutforbund; Norske Kiropraktorers Forening
- Apotekloven med forskrifter; NHN – Reseptformidleren og e-resept


---

# 11. Strategi

# Strategi — SMART on FHIR for Altinn Studio

**Sist oppdatert:** 2026-06-18  
**Eier:** Digitaliseringsdirektoratet (Digdir)

---

## Tre nøkkelbudskap

### 1 — SMART on FHIR fungerer i Altinn Studio
Demonstrert gjennom legeerklæring for førerrett: FHIR-prefill, SMART EHR Launch, BFF-mønster, Altinn-signering — kjernearkitekturen er verifisert i lokalt testmiljø mot SMART-mock og HAPI FHIR. Sikkerhetshardening (tokenvalidering, audit-logging) og verifikasjon mot reell fastlege-EPJ gjenstår.

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

**Suksesskriterier:**
- FHIR-prefill fra EPJ
- Legeerklæring IS-2569 utfylt og signert
- Innsending til mottaker
- Writeback til EPJ (DocumentReference)

**Status:** FHIR-prefill, signering og dev-innlogging er verifisert. Se [VEIKART.md](VEIKART.md) for teknisk gjenstående arbeid.

**Avgrensning:** Spor A er caset — ikke plattformen. Avsluttes når PoC er produksjonsklar.

---

### Spor B — Altinn Health Integration Framework

**Formål:** Gjøre Altinn Studio til en generisk SMART on FHIR-plattform — slik at tjenesteeiere kan bygge helseskjemaer uten å implementere SMART-flyten selv.

**Hva dette handler om:**
- Sykmelding
- Spesialistattest
- Refusjonskrav (Helfo)
- NAV-helseskjemaer
- Kommunale helsetjenesteskjemaer
- Spesialisthenvisning

**Leveranse:** NuGet-pakken `Digdir.SmartOnFhir` (se [VEIKART.md fase 4](VEIKART.md)) er kjernen i Spor B. Den gjør det trivielt for andre Altinn-apputviklere å legge til SMART-støtte.

**Avhengighet:** Spor B starter etter at Spor A har produsert et stabilt, produksjonsklar SMART-klient (fase 1–3).

---

## Fire leveransemodeller for nasjonal utbredelse

*Lagt til etter kvalitetssikring 2026-06-19.* Spørsmålet «hvem bygger, eier og drifter helseskjemaer nasjonalt» avgjøres ikke bare av Altinn vs. Helsenorge (C-6), men av fire realistiske leveransemodeller. De skiller seg langs fem dimensjoner: hvem **utvikler**, hvem **eier**, hvor **lages**, hvor **kjøres**, og hvor **lagres data**.

| Dimensjon | NAV-modellen | NHN/Helsenorge-modellen | EPJ-modellen | Altinn-modellen |
|---|---|---|---|---|
| Utvikler | Virksomheten selv | NHN, på vegne av tjenesteeier | EPJ-leverandøren | Tjenesteeier selv |
| Eier | Virksomheten | NHN drifter; tjenesteeier eier innhold | EPJ-leverandøren | Tjenesteeier |
| Lages | Egen stack/repo | Helsenorge-plattformen | Inne i EPJ-produktet | Altinn Studio |
| Kjøres | Egen infrastruktur | Helsenorge-plattformen | I/ved siden av EPJ | Altinn 3 |
| Data lagres | Eget lager | Helsenorge/NHN (innen Normen) | EPJ/leverandørens sky | Altinn Storage (i denne PoC-en: FHIR-data kun i minne, ikke persistert) |
| Levende eksempel | `syk-inn` (NAV) | NHN Førerrett-App | (varierer per leverandør) | `forer-legeerklaering` (denne PoC) |

**Hva som faktisk avgjør fart og utbredelse:** ikke skjemaet i seg selv, men hvor mange ganger *integrasjonslaget* (SMART Launch + FHIR + HelseID + signering + audit) må bygges på nytt. NAV-modellen krever *N* reimplementasjoner (én per virksomhet). EPJ-modellen krever *N × M* (leverandører × skjemaer). NHN-modellen bygger laget én gang, men alt køer bak NHNs kapasitet og roadmap. Altinn-modellen bygger laget én gang som en delt komponent (`Digdir.SmartOnFhir`), og tjenesteeierne bygger skjemaene selv — selvbetjent.

**Fordeler og ulemper, kort:**

- **NAV-modellen** — full kontroll og egen domenelogikk (jf. `syk-inn`), men skalerer ikke nasjonalt: forutsetter tung egen utviklingskapasitet de fleste tjenesteeiere (kommuner, tilsyn, mindre direktorater) ikke har.
- **NHN/Helsenorge-modellen** — HelseID nativt, i sektorens tillitsregime (Normen) og innbyggerkanal; men NHN blir en flaskehals, og tjenesteeiere utenfor helse (SVV, Lånekassen, Arbeidstilsynet) har ikke naturlig tilgang til Helsenorge.
- **EPJ-modellen** — dypest kliniske arbeidsflyt-UX, prefill/writeback er trivielt siden FHIR-data finnes nativt; men leverandørlås og N×M-fragmentering, og regelendringer må pushes konsistent gjennom alle leverandører.
- **Altinn-modellen** — selvbetjening for enhver tjenesteeier, tverrsektor, allerede DPG-registrert; men ikke nativt en helseplattform (Normen-spørsmålet må klareres, se [RISIKOREGISTER.md](RISIKOREGISTER.md)), HelseID er ikke innebygd, og legen bytter kontekst ut av EPJ.

**Syntese — modellene utelukker ikke hverandre.** Det sterkeste grepet er å **standardisere integrasjonslaget** og la *hostingmodellen* velges per case, ut fra tre variabler: (1) hvem er mottaker — helsemottaker eller tverrsektor-mottaker, (2) trengs innbyggerkanal (→ vanskelig å unngå Helsenorge), (3) hvor høy er den kliniske UX-terskelen. Posisjoneringen blir da: **Altinn-modellen er selvbetjenings-, tverrsektor- og DPG-laget**; **NHN-modellen er det helse-native laget med innbyggerkanal**; **EPJ-modellen gir best klinisk UX der den finnes**; **NAV-modellen passer virksomheter med tung egen utviklingskapasitet**. Gevinsten for nasjonen ligger i at de deler samme integrasjonsstandard — ikke i at én modell vinner.

---

## Samarbeidsmodell

For at dette skal bli en nasjonal standard kreves fire aktørgrupper med klare ansvarsområder.

### Digdir
Eier og leverer:
- Altinn Studio-komponenter og app-infrastruktur
- SMART-wrapper (`Digdir.SmartOnFhir` NuGet-pakke)
- Referanseimplementasjon (legeerklæring for førerrett)
- Altinn-integrasjon (signering, arkiv, PDF, instansmodell)

### NHN (Norsk helsenett)
Eier og leverer:
- HelseID (behandler-autentisering)
- HelseAPI og tillitsrammeverk
- Infrastruktur for sikker tilkobling mot EPJ-systemer

### EPJ-leverandører (DIPS, CGM, Infodoc, WebMed, m.fl.)
Eier og leverer:
- SMART EHR Launch fra EPJ
- FHIR API (Patient, Practitioner, Organization, Encounter)
- Klientregistrering i HelseID

### Tjenesteeiere (SVV, Helsedirektoratet, Helfo, NAV, kommuner)
Eier og leverer:
- Skjemadefinisjon og feltstruktur
- Forretningsregler og validering
- Mottaksarkitektur og videre behandling

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

Dette er den strategiske horisonten — utover den tekniske VEIKART.md.

### Fase 1 — Referanseimplementasjon (nå)
Førerrett PoC produksjonsklar: tokenvalidering, refresh, writeback, testing.  
*Leveranse: Spor A ferdig. Digdir alene.*

### Fase 2 — Generisk SMART-komponent
NuGet-pakke `Digdir.SmartOnFhir` ekstrahert og publisert.  
Dokumentasjon og integrasjonsguide for EPJ-leverandører.  
*Leveranse: Spor B initiell. Digdir + 1–2 EPJ-leverandører i pilot.*

### Fase 3 — Pilotsamarbeid
Pilot med minst én av: NAV, Helfo, Helsedirektoratet.  
HelseID-integrasjon aktivert (C-2 avklart).  
Gevinstmåling gjennomføres.  
*Leveranse: Andre skjematype på samme plattform.*

### Fase 4 — SMART som standard integrasjonsmønster
SMART on FHIR innarbeidet som anbefalt mønster i Altinn Studio for helseskjemaer.  
Felles nasjonal avklaring av mottaksarkitektur og juridisk grunnlag (C-3, C-4).  
*Leveranse: Nasjonal standard. Alle fire aktørgrupper involvert.*

---

## Strategisk risiko og posisjonering

**Risiko:** NHN har allerede en produksjonsløsning for førerrett (IS-2569) på Helsenorge-plattformen. Initiativet kan oppfattes som konkurrerende.

**Posisjonering:** Dette er ikke en konkurrent til NHN Førerrett-App. Initiativet handler om Altinn Studio som plattform — ikke om å reimplementere førerrett. Førerrett er caset for å bevise at mønsteret virker.

**Budskapet til NHN:**
> Vi bygger ikke en bedre førerrett-app. Vi bygger infrastrukturen som gjør at tjenesteeiere som ikke har Helsenorge-plattformen tilgjengelig, kan bruke Altinn Studio med FHIR-prefill — og at EPJ-leverandører bare trenger én standardintegrasjon.

Se [SAMMENLIGNING-syk-inn.md](SAMMENLIGNING-syk-inn.md) og [NHN-DOKUMENTASJON.md](NHN-DOKUMENTASJON.md) for full sammenligning og læringspunkter.

---

## Europeisk medvind — EHDS

**European Health Data Space** (forordning (EU) 2025/327) er i kraft og gjelder fra 2026, med gjennomføringsrettsakter innen mars 2027 og første primærbruk-utveksling fra 2029. FHIR er den sentrale tekniske standarden. Norge er EØS-bundet og vil bli omfattet.

En åpen, gjenbrukbar SMART on FHIR-komponent i Altinn Studio treffer EHDS-retningen presist. Det gir strategisk forankring utover nasjonale behov, og kobler initiativet til det europeiske arbeidet med helsedata-interoperabilitet.

**Altinn er allerede registrert som Digital Public Good (DPG).** En gjenbrukbar, åpen `Digdir.SmartOnFhir`-komponent kan forsterke denne posisjonen — særlig koblet mot norskutviklede DPG-er som **DHIS2** (UiO/HISP, flaggskip innen global helse). Forutsetning: helsedata-/Normen-spørsmålet (F2 i kvalitetssikringen) må lukkes.

---

## Åpne strategiske spørsmål

Se [BESLUTNINGER.md](BESLUTNINGER.md) for alle åpne beslutninger. De mest strategisk kritiske:

| ID | Spørsmål | Blokkerer |
|---|---|---|
| C-6 | Altinn Studio vs. Helsenorge-plattformen — rollefordeling | Fase 3 |
| C-3 | Mottaksarkitektur — hvem er tjenesteeier? | Fase 3 |
| C-4 | Rettslig grunnlag og DPIA | Fase 3 |
| C-7 | DSOP + SMART on FHIR mot privat sektor | Fase 4 |


