# Epic on FHIR som testmiljø — relevant fordi Helseplattformen kjører Epic

**Status (2026-09-09): løst — Epic bekreftet et driftsproblem (synk-feil i testmiljøene deres) og LaunchPad-verktøyet gir nå et faktisk, ikke-tomt launch-token med riktig `iss` (R4).** App **"Legeerklæring førerrett (Digdir)"** er registrert og korrekt konfigurert. Det tomme launch-tokenet og feil FHIR-versjon (`DSTU2` i stedet for det registrerte `R4`) som ble reprodusert 2026-08-27/2026-09-08 — inkludert med Epics eget offisielle "SMART on FHIR test"-eksempelapp, som utelukket alt på vår side — er bekreftet borte etter Epics fiks. Neste steg er en full ende-til-ende-test av en faktisk `Launch` (ikke bare `Generate URL Only`) mot en kjørende lokal instans av appen. Se §9 for full diagnostikk/retest og §10 for e-postutvekslingen med Epic support.

## Innhold

1. [Hvorfor Epic er relevant for oss](#1-hvorfor-epic-er-relevant-for-oss)
2. [Registrering — dette må et menneske gjøre](#2-registrering--dette-må-et-menneske-gjøre)
3. [Sandkassen](#3-sandkassen)
4. [Teknisk oppsummering: SMART-flytene Epic støtter](#4-teknisk-oppsummering-smart-flytene-epic-støtter)
5. [To Epic-spesifikke avvik fra det vi allerede har testet](#5-to-epic-spesifikke-avvik-fra-det-vi-allerede-har-testet)
6. [Sjekkliste: når du har fått et client_id](#6-sjekkliste-når-du-har-fått-et-client_id)
7. [Veien til en reell Helseplattformen-integrasjon](#7-veien-til-en-reell-helseplattformen-integrasjon)
8. [Referanser](#8-referanser)
9. [Testlogg](#9-testlogg)
10. [Feilmelding sendt til Epic support](#10-feilmelding-sendt-til-epic-support)

---

## 1. Hvorfor Epic er relevant for oss

**Helseplattformen — Norges Epic-installasjon — er ikke bare sykehus.** Kontrakten (Helse Midt-Norge, inngått 2018, ca. 3,3 mrd. kr) dekker St. Olavs hospital, Helse Møre og Romsdal, Helse Nord-Trøndelag **og 38 kommuner** i regionen. Det betyr at kommunal helsetjeneste — inkludert fastleger — i Trøndelag/Møre og Romsdal reelt sett bruker Epic. En fastlege der som brukte `forer-legeerklaering`, ville møtt nøyaktig samme grensesnitt vi kan teste mot i sandkassen.

Dette er den tredje EPJ-simulatoren/testmiljøet vi undersøker i dette prosjektet, etter [launch.smarthealthit.org](TESTGUIDE-SMARTHEALTHIT.md) (generisk, amerikanske Synthea-data) og [nav-epj](NAV-EPJ-TESTMILJO.md) (Norway-tilpasset, men et NAV-internt testverktøy). Epic sin sandkasse er forskjellig fra begge: det er en **ekte leverandørs offisielle, produksjonslike testmiljø** — samme protokollimplementasjon en reell Helseplattformen-installasjon kjører, ikke en forenklet simulator.

## 2. Registrering — dette må et menneske gjøre

**Registrerings-URL: https://fhir.epic.com/Developer/Index**

Dette er «Sign Up to Access»-siden — bekreftet ved å inspisere den faktiske DOM-en (registreringsskjemaets `Submit`-knapp poster til denne URL-en selv).

**Hva skjemaet spør om** (organisasjonstilknyttet, ikke rent personlig):
- Fornavn, etternavn
- Firma-e-post, telefon, firmanavn, nettside
- Land, forretningsadresse, by, delstat/fylke, postnummer

**Prosess:** gratis, selvbetjent. E-postverifisering kreves («We have sent you an email... Follow the instructions in the email to verify your email address»). Etter verifisering får du tilgang til tre ting: **Testing Sandbox**, **Client Registration**, og **Documentation**.

**Én registrering av appen gir automatisk to client_id-er samtidig** — ett for non-production (sandkasse) og ett for production. Du oppgir bl.a. én eller flere `redirect_uri`-er ved registrering.

**Synkroniseringstid — to ulike tall i Epics egen dokumentasjon, det høyeste er det pålitelige:** OAuth 2.0-spesifikasjonen nevner «rolling sync between the Build Apps page and the Sandbox... up to 30 minutes». Developer Testing Guide sier derimot eksplisitt: **«The FHIR Developer Sandbox may take up to 1 hour to sync changes you make to technical settings for your app, such as endpoint URIs and selected APIs.»** Planlegg med **1 time**, ikke 30 minutter — bekreftet i praksis 2026-08-27 (se §9): et forsøk på å generere en launch-URL kort tid etter lagring ga et tomt launch-token og feil FHIR-versjon.

**Jeg kan ikke gjøre denne registreringen for deg** — å opprette kontoer er noe jeg aldri gjør på andres vegne, uavhengig av hvem det gjelder. Så snart du har et client_id (non-production), ta det videre herfra — se §6.

## 3. Sandkassen

**Base-URL:** `https://fhir.epic.com/interconnect-fhir-oauth/`

Sikret utelukkende med OAuth 2.0. Massiv FHIR-ressursflate (R4/STU3/DSTU2) — bl.a. `DocumentReference (Clinical Notes)` med **Create**-støtte i både R4 og STU3, direkte relevant for vår writeback-flyt.

### Testbrukere (behandler, for Standalone Launch)

| Navn | Brukernavn | Passord | Merknad |
|---|---|---|---|
| FHIR, USER | `FHIR` | `EpicFhir11!` | Ingen tilknyttet `PractitionerRole` |
| FHIRTWO, USER | `FHIRTWO` | `EpicFhir11!` | Har en tilknyttet `PractitionerRole`-ressurs |

### Testpasienter (utvalg — se full liste under «Sandbox Test Data» på fhir.epic.com)

| Pasient | FHIR-ID | MyChart-innlogging | Ressurser tilgjengelig |
|---|---|---|---|
| Camila Lopez | `erXuFYUfucBZaryVksYEcMg3` | `fhircamila` / `epicepic1` | DiagnosticReport, Goal, Medication(Order/Request/Statement), Observation (Labs), Procedure |
| Derrick Lin | `eq081-VQEgP8drUUqCWzHfw3` | `fhirderrick` / `epicepic1` | CarePlan, Condition, Goal, Medication*, Observation (Smoking History) |
| Elijah Davis | `egqBHVfQlt4Bw3XGXoxVxHg3` | (ingen) | AllergyIntolerance, Binary, Condition, DocumentReference, Medication* |
| Warren McGinnis | `e0w0LEDCYtfckT6N.CkJKCw3` | (ingen) | AllergyIntolerance, Binary, Condition, DiagnosticReport, DocumentReference, Observation (Labs/Vitals), Procedure |

For EHR Launch spesifikt (den flyten vi bruker) trenger man ikke MyChart-innlogging — pasient og behandler-kontekst velges av sandkassens egen launch-simulator (analogt med launch.smarthealthit.org sin "App Launch Options"-side), som man får tilgang til fra "Build Apps"-siden etter innlogging.

## 4. Teknisk oppsummering: SMART-flytene Epic støtter

Alle tre matcher det vi allerede har implementert og verifisert mot launch.smarthealthit.org.

### EHR Launch (det vi bruker i produksjon)

```
GET {redirect_uri}?iss={fhir_base}&launch={launch_token}
```

Discovery: `GET {iss}/metadata` (XML/FHIR-format) eller `GET {iss}/.well-known/smart-configuration` (JSON, støttet fra Epic august 2021+).

```
GET https://fhir.epic.com/interconnect-fhir-oauth/oauth2/authorize
    ?scope=launch&response_type=code
    &redirect_uri={redirect_uri}&client_id={client_id}
    &launch={launch_token}&state={state}
    &aud={fhir_base}
    &code_challenge={challenge}&code_challenge_method=S256   ← valgfritt, PKCE
```

Token-exchange: `POST {iss}/oauth2/token`, `grant_type=authorization_code` — identisk med det vi allerede sender.

**Merknader fra Epics egen dokumentasjon, verdt å kjenne til:**
- `aud`-parameteren er **påkrevd** siden Epic mai 2023 (vi sender den allerede).
- POST i stedet for GET til `/oauth2/authorize` støttes (fra Epic november 2020+) og anbefales for EHR-launches for å unngå URL-lengdebegrensninger — vi bruker i dag kun GET, fungerer men verdt å vurdere.
- `state` bør holdes kort (Epic anbefaler en ren UUID, ikke innbakt applikasjonstilstand) — lange `state`-verdier kombinert med JWT-baserte launch-tokens kan i sjeldne tilfeller sprenge webserverens max query-string-lengde.

### Standalone Launch

Samme mønster, men appen starter selv mot `/oauth2/authorize` uten `launch`-parameter, med `scope=openid` som minimum (påkrevd for sandkasse-testing).

### Backend Services (`private_key_jwt`) — allerede implementert hos oss

Epics eget eksempel matcher **nøyaktig** vår egen `BuildClientAssertionJwt`-implementasjon fra forrige runde (se [IMPLEMENTERING.md §13](IMPLEMENTERING.md)): `iss=sub=client_id`, `aud={token_endpoint}`, `jti`, kort levetid, signert med `RS384`.

```
POST https://fhir.epic.com/interconnect-fhir-oauth/oauth2/token
grant_type=client_credentials
&client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer
&client_assertion={vår signerte JWT}
```

**Viktig forskjell fra tidligere tester, se §5.**

## 5. To Epic-spesifikke avvik fra det vi allerede har testet

### 5.1 `Epic-Client-ID`-header kan være nødvendig på selve discovery-kallet

Epic støtter at FHIR-serverens endepunkt-URL (`iss`) overstyres per client_id. Uten en `Epic-Client-ID`-header i kallet mot `/metadata` eller `/.well-known/smart-configuration`, kan man få feil `authorize`/`token`-endepunkter tilbake. Vår `DiscoverSmartConfiguration()` i `SmartLaunchController.cs` sender i dag **ingen** ekstra headere — kun en ren `GET`. **Handling når vi tester:** legg til `client.DefaultRequestHeaders.Add("Epic-Client-ID", clientId)` på discovery-kallet, og bekreft at det ikke er nødvendig for sandkassen (det er mest relevant for spesifikt konfigurerte Epic-kunder) før man antar det er trygt å utelate i produksjon.

### 5.2 `private_key_jwt` mot Epic krever en ekte hostet JWKS (JKU), ikke en limt-inn statisk nøkkel

Forrige runde limte vi en statisk offentlig JWK inn i launch.smarthealthit.org sitt registreringsskjema — det holdt for å bevise at vår signerte JWT var strukturelt korrekt. **Epic tillater ikke lenger dette i sandkassen** («Vendor Services and Epic on FHIR websites no longer allow static key uploads for use in the sandbox» — gjeldende fra februar 2026 for nye apper). Vi må i stedet **verte en ekte, offentlig tilgjengelig JWKS-URL** som Epic kan hente den offentlige nøkkelen fra (en JSON Web Key Set URL, «JKU»). Dette er en reell, litt større jobb enn tidligere: appen vår må eksponere et `/.well-known/jwks.json`-lignende endepunkt (eller tilsvarende), tilgjengelig fra internett, ikke bare `localhost`.

## 6. Sjekkliste: når du har fått et client_id

1. Vent til synkroniseringen mot sandkassen er fullført (**opptil 1 time** etter lagring — se §2, ikke 30 minutter).
2. Sett `SmartOnFhir:ClientId` til det utstedte **non-production** client_id-et via `dotnet user-secrets` i `src/App` (ikke i en committet appsettings-fil — samme mønster som tidligere runder). Fjern eventuelle `FhirBaseUrlOverride`/`DefaultIss`-overstyringer fra tidligere EPJ-testrunder (nav-epj) — de peker feil for Epic.
3. **Finn launch-simulatoren** (bekreftet fungerende sti, ikke opplagt fra navigasjonen):
   - Logg inn på fhir.epic.com, gå til `https://fhir.epic.com/Documentation?docId=launching`.
   - Klikk fanen **«SMART on FHIR (OAuth 2.0)»**, deretter **«Try It»** rett under.
   - Et skjema åpnes: **«Choose an app to test with»** (velg vår app), **«Select a patient»**, **«Enter launch URL to receive the request to your app»** — bruk `http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/launch`.
   - Klikk **«Generate URL Only»** (ikke «Launch» — det prøver å navigere direkte, og `local.altinn.cloud` er ikke nåbar fra en vanlig nettleser utenfor det lokale miljøet/en sandkassemaskin uten VPN/hosts-oppsett). Den genererte URL-en vises i et modal-vindu.
4. Kjør gjennom hele launch → authorize → callback-kjeden med den genererte URL-en, samme fremgangsmåte (curl med cookie-jar, eller direkte i nettleser) som er dokumentert for de to andre testmiljøene.
5. Sjekk om §5.1 (Epic-Client-ID-header) faktisk er nødvendig i praksis, eller om sandkassen fungerer uten.
6. Hvis `private_key_jwt` skal testes: sett opp en offentlig JWKS-URL (§5.2) før forsøk på backend-services-flyten — statisk nøkkel vil ikke fungere.
7. Dokumenter resultatet i §9, samme mønster som IMPLEMENTERING.md §13 for launch.smarthealthit.org.

## 7. Veien til en reell Helseplattformen-integrasjon

Samme mønster som NHN (Helsenorge EksternAPI) og NAV (`nav-epj`): sandkasse-testing er rent teknisk og kan gjøres selvstendig, men **å gå live hos en ekte kunde er en organisatorisk henvendelse**, ikke noe som løses ved koding alene.

Epics prosess: appen må først merkes **«Ready for Production»** av utvikleren (etter sandkasse-verifisering), deretter må **Helseplattformen selv** («Epic community member») aktivt laste ned/be om appen via Epics «Showroom»-plattform (tidligere «App Orchard») og signere en «open.epic API Subscription Agreement». Dette er utelukkende kundedrevet — vi kan ikke selv trigge en produksjonsdistribusjon hos Helseplattformen, uansett hvor godt sandkasse-testingen går.

**Praktisk konsekvens:** sandkasse-verifiseringen (§6) beviser at protokollen fungerer mot en ekte Epic-instans — verdifullt i seg selv, og direkte sammenlignbart med det vi allerede har gjort mot launch.smarthealthit.org og nav-epj. Et faktisk samarbeid med Helseplattformen er et eget, senere skritt, trolig gjennom Helse Midt-Norge/Helseplattformen AS direkte snarere enn en kald henvendelse via Epics Showroom.

## 8. Referanser

- [fhir.epic.com](https://fhir.epic.com/) — Epic on FHIR, hovedside
- [fhir.epic.com/Developer/Index](https://fhir.epic.com/Developer/Index) — registrering
- [TESTGUIDE-SMARTHEALTHIT.md](TESTGUIDE-SMARTHEALTHIT.md) og [NAV-EPJ-TESTMILJO.md](NAV-EPJ-TESTMILJO.md) — de to andre testmiljøene, samme metodikk
- [IMPLEMENTERING.md §13](IMPLEMENTERING.md) — vår eksisterende `private_key_jwt`-implementasjon (`BuildClientAssertionJwt`), direkte gjenbrukbar

## 9. Testlogg

### 2026-08-27 — app registrert, første launch-forsøk

**Registrering fullført:** app **"Legeerklæring førerrett (Digdir)"** opprettet av Johann, med:
- Application Audience: Clinicians or Administrative Users
- Is Confidential Client: Nei (Public, for det første, enkleste testforsøket — samme fasede tilnærming som launch.smarthealthit.org: public → client_secret → private_key_jwt)
- SMART on FHIR Version: R4
- SMART Scope Version: v1
- FHIR ID Generation Scheme: 64-Character-Limited FHIR IDs for USCDI FHIR Resources
- Redirect URI: `http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/callback` (eksplisitt `http://`, feltet tillot å overstyre en `https://`-hint)
- Incoming APIs valgt ut fra faktisk kode i `FhirPrefillService.cs` (ikke gjettet fra scope-strengen): Patient.Read (R4), Practitioner.Read (Organizational Directory), PractitionerRole.Search (Organizational Directory), Encounter.Read (Patient Chart), Organization.Read (Organizational Directory), Condition.Search (Problems), DocumentReference.Create (Clinical Notes). Observation bevisst utelatt — `FillObservation` er ikke implementert ennå.
- Lagret med «Save & Ready for Sandbox».
- Non-production client_id satt lokalt via `dotnet user-secrets set "SmartOnFhir:ClientId" "..."` i `src/App` (ikke committet — se §6).

**Første forsøk på å generere en launch-URL** (via LaunchPad-verktøyet, se §6 steg 3), kort tid etter lagring:

```
http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/launch?iss=https%3A%2F%2Ffhir.epic.com%2Finterconnect-fhir-oauth%2Fapi%2FFHIR%2FDSTU2&launch=
```

**To avvik fra forventet:**
1. `launch=` er tomt — ingen faktisk launch-token ble generert.
2. `iss` peker på `DSTU2`, ikke `R4` som registrert.

**Vurdering (den gang):** antatt sandkasse-synkroniseringsforsinkelse (§2). **Denne teorien viste seg å være feil — se oppfølgingen 2026-09-08 under.**

### 2026-09-08 — rotårsak funnet: feil i Epics eget LaunchPad-verktøy, ikke noe på vår side

Gjentok forsøket >1 uke etter registrering (godt utenfor enhver rimelig synk-forsinkelse). Samme resultat: tomt `launch=`, `iss` fortsatt `DSTU2`.

**Systematisk feilsøking, i rekkefølge:**

1. **Bekreftet at appens R4-innstilling faktisk er lagret** — inspiserte radioknappene direkte i DOM-en (`PrimaryFHIRVersion`, verdi `R4`, `checked: true`). Ikke et lagringsproblem.
2. **Klikket «Save & Ready for Sandbox» på nytt** for å utelukke at en tidligere lagring var ufullstendig. Ingen endring i resultatet.
3. **Fanget selve nettverkskallet** LaunchPad-verktøyet gjør (`POST /Developer/GetLaunchUrl`) med en JS-interceptor for både request og response, i stedet for å gjette ut fra det synlige skjemaet:
   - **Request:** `{"launchUrl":"...","tokens":"dob=%DOB%&user=%SYSLOGIN%","eptId":"Z4529","wprId":"","appId":"60326","aesKey":"shhh","ssoMethod":"1"}` — bekreftet at riktig `appId` (vår app) faktisk ble sendt. `wprId` (et skjult felt, viste seg å være "Select a MyChart user" — irrelevant for en behandler-app, korrekt tomt).
   - **Response:** `{"Success":true,"Title":null,"Message":null,"Data":{"url":"...iss=...DSTU2&launch=","error":""}}` — serveren selv rapporterer suksess, ingen feilmelding, men leverer et tomt launch-token.
4. **Bekreftet at R4-discovery fungerer, men DSTU2-discovery ikke gjør det**, direkte mot sandkassen med `curl`:
   - `GET .../api/FHIR/R4/.well-known/smart-configuration` → `200 OK`
   - `GET .../api/FHIR/DSTU2/.well-known/smart-configuration` → **`404 Not Found`**
   - `GET .../api/FHIR/DSTU2/metadata` → `200 OK` (den eldre XML-baserte discovery-mekanismen virker for DSTU2, men ikke den nyere `.well-known`-en appen vår bruker)
   
   Dette forklarer *hvorfor* appen vår konkret feiler med «Could not retrieve SMART configuration from EPJ» når den mottar en DSTU2-`iss`: DSTU2 støtter rett og slett ikke discovery-mekanismen SMART App Launch IG (og vår kode) forutsetter — DSTU2 er eldre enn den spesifikasjonen.
5. **Avgjørende test: reproduserte identisk feil med Epics EGET offisielle eksempelapp** («SMART on FHIR test», appId `-121`, med sin egen standard launch-URL `https://fhir.epic.com/Test/Smart`) — samme tomme `launch=`, samme `DSTU2`. Dette utelukker *alt* på vår side (redirect-URI, API-liste, FHIR-versjon-innstilling, klienttype) som mulig årsak.
6. **Vurderte "HTTP Get LaunchPad" som alternativ** — forkastet: det er en helt annen, eldre SSO-mekanisme («HTTP GET with encrypted querystring»), ikke SMART on FHIR. Epics egen dokumentasjon sier eksplisitt: «Only use this method if SMART on FHIR is not an option. Epic recommends that new implementations use SMART on FHIR for SSO.» Å teste den ville ikke validert vår faktiske SMART-integrasjon.

**Konklusjon:** dette er en feil i Epics eget sandkasse-LaunchPad-verktøy — ikke noe i vår appregistrering, kode, eller sandkasse-synkronisering. Meldt til Epic support (`open@epic.com`) 2026-09-08 — se §10 for e-postens innhold. **Venter på svar fra Epic** før videre testing kan fortsette gjennom dette verktøyet.

**Mulige veier videre, ikke forsøkt ennå:**
- Vent på svar fra Epic support.
- Prøv igjen etter neste ukentlige sandkasse-refresh (søndag ca. 20:00 amerikansk sentraltid, se §3/§6).
- Vurder å bygge launch-URL-en manuelt (samme teknikk som for launch.smarthealthit.org og nav-epj: konstruer `iss`/`launch` selv) — trolig ikke mulig her siden `launch` er et EHR-generert, opakt token vi ikke kan forfalske selv, i motsetning til `iss`.

### 2026-09-09 — bekreftet løst etter Epics svar

Epic support svarte (se §10) at det var et driftsproblem — synkroniseringsproblemer i testmiljøene deres — og ba oss prøve på nytt. Gjentok nøyaktig samme forsøk som 2026-08-27/2026-09-08 (LaunchPad, app "Legeerklæring førerrett (Digdir)", samme launch-URL), med samme JS-interceptor på `POST /Developer/GetLaunchUrl` som avdekket feilen sist:

- **Request:** `{"launchUrl":"http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/launch",...,"appId":"60326",...}` — samme som før.
- **Response:** `{"Success":true,...,"Data":{"url":"...iss=https%3A%2F%2Ffhir.epic.com%2Finterconnect-fhir-oauth%2Fapi%2FFHIR%2FR4&launch=eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...","error":""}}`

**Begge avvikene fra 2026-08-27/2026-09-08 er borte:**
1. `launch=` er nå et faktisk, ikke-tomt JWT — dekodet payload viser `"epic.tokentype":"launch"`, `"client_id":"fe9e031b-b4f1-49ad-9a84-59f8e05475e5"` (vår registrerte non-production client_id — riktig app), 5 minutters levetid (`exp` − `iat` = 300s).
2. `iss` peker nå på `.../api/FHIR/R4`, ikke `DSTU2` — riktig, matcher appens registrerte FHIR-versjon.

**Konklusjon:** Epics driftsfeil er bekreftet rettet. LaunchPad-verktøyet fungerer nå som forventet for vår app. Neste steg er en full ende-til-ende-test (faktisk `Launch`, ikke bare `Generate URL Only`) mot en kjørende lokal instans av appen — ikke gjort i denne omgangen siden launch-tokenet er kortlevd (5 min) og må genereres på nytt rett før selve testen, i en nettleser som faktisk når `local.altinn.cloud:8000` (dvs. på Johanns egen maskin, ikke i dette hostede browser-panelet).

## 10. Feilmelding sendt til Epic support

Sendt til `open@epic.com` 2026-09-08 (Johann sitt eget navn/organisasjon og faktiske non-production client_id satt inn i den faktiske e-posten, utelatt her):

> **Subject:** SMART on FHIR LaunchPad generates empty launch token and wrong FHIR version — reproducible with Epic's own sample app
>
> We're testing a SMART on FHIR EHR Launch integration against the sandbox and have hit what looks like a bug in the "SMART on FHIR (OAuth 2.0)" LaunchPad tool (`fhir.epic.com/Documentation?docId=launching` → "Try It").
>
> **Steps to reproduce:** log in, go to the LaunchPad, choose an app (we tried both our own registered app and Epic's own built-in "SMART on FHIR test" sample app), select any patient, enter a launch URL, and click "Generate URL Only" (or "Launch" — same result either way).
>
> **Expected:** a real, one-time `launch` token, with `iss` pointing at the FHIR version registered for the app (our app is registered as R4).
>
> **Actual:** the generated URL always has an empty `launch=` parameter and `iss` always points at `.../api/FHIR/DSTU2`, regardless of the app's registered FHIR version. The underlying `POST /Developer/GetLaunchUrl` call returns `"Success":true` with no error message, and an empty `Data.url` launch token.
>
> **Key point:** we reproduced the exact same result using Epic's own built-in "SMART on FHIR test" app, not just our own — so this doesn't appear to be specific to our app's configuration.

**Svar fra Epic support (mottatt 2026-09-09):** bekreftet at det var et driftsproblem på deres side — synkroniseringsproblemer i testmiljøene deres (samme "opptil 1 time"-synk-mekanisme omtalt i §3, men her var selve synken feilet, ikke bare treg). Epic ba oss prøve på nytt.

**Neste steg:** gjenta launch-forsøket (§9, samme fremgangsmåte som 2026-08-27/2026-09-08) — se om `launch`-token og `iss` nå er korrekte (R4, ikke DSTU2, og et faktisk ikke-tomt token). Oppdater §9 med resultatet.
