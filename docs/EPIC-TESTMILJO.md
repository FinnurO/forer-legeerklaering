# Epic on FHIR som testmiljø — relevant fordi Helseplattformen kjører Epic

**Status (2026-08-27): undersøkt og dokumentert, ikke satt opp ennå.** Kontoregistrering på fhir.epic.com må gjøres av et menneske (organisasjonstilknyttet, e-postverifisert) — se §2 for URL. Dette dokumentet er skrevet for å være klart til bruk så snart det er gjort.

## Innhold

1. [Hvorfor Epic er relevant for oss](#1-hvorfor-epic-er-relevant-for-oss)
2. [Registrering — dette må et menneske gjøre](#2-registrering--dette-må-et-menneske-gjøre)
3. [Sandkassen](#3-sandkassen)
4. [Teknisk oppsummering: SMART-flytene Epic støtter](#4-teknisk-oppsummering-smart-flytene-epic-støtter)
5. [To Epic-spesifikke avvik fra det vi allerede har testet](#5-to-epic-spesifikke-avvik-fra-det-vi-allerede-har-testet)
6. [Sjekkliste: når du har fått et client_id](#6-sjekkliste-når-du-har-fått-et-client_id)
7. [Veien til en reell Helseplattformen-integrasjon](#7-veien-til-en-reell-helseplattformen-integrasjon)
8. [Referanser](#8-referanser)

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

**Én registrering av appen gir automatisk to client_id-er samtidig** — ett for non-production (sandkasse) og ett for production. Du oppgir bl.a. én eller flere `redirect_uri`-er ved registrering. Ifølge dokumentasjonen kan det ta **opptil 30 minutter** før en nyregistrert app faktisk synkroniseres til sandkassen («rolling sync between the Build Apps page and the Sandbox»).

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

1. Vent til synkroniseringen mot sandkassen er fullført (opptil 30 minutter etter registrering, jf. §2).
2. Sett `SmartOnFhir:ClientId` til det utstedte **non-production** client_id-et via `dotnet user-secrets` i `src/App` (ikke i en committet appsettings-fil — samme mønster som tidligere runder).
3. Bruk sandkassens egen launch-simulator (tilgjengelig fra "Build Apps" etter innlogging) til å generere en launch-URL mot vår apps `/smart/launch`-endepunkt — samme prinsipp som [TESTGUIDE-SMARTHEALTHIT.md](TESTGUIDE-SMARTHEALTHIT.md), tilpasset Epics grensesnitt i stedet for smarthealthit.org sitt.
4. Kjør gjennom hele launch → authorize → callback-kjeden, samme fremgangsmåte (curl med cookie-jar, eller direkte i nettleser) som er dokumentert for de to andre testmiljøene.
5. Sjekk om §5.1 (Epic-Client-ID-header) faktisk er nødvendig i praksis, eller om sandkassen fungerer uten.
6. Hvis `private_key_jwt` skal testes: sett opp en offentlig JWKS-URL (§5.2) før forsøk på backend-services-flyten — statisk nøkkel vil ikke fungere.
7. Dokumenter resultatet i et nytt avsnitt her, samme mønster som IMPLEMENTERING.md §13 for launch.smarthealthit.org.

## 7. Veien til en reell Helseplattformen-integrasjon

Samme mønster som NHN (Helsenorge EksternAPI) og NAV (`nav-epj`): sandkasse-testing er rent teknisk og kan gjøres selvstendig, men **å gå live hos en ekte kunde er en organisatorisk henvendelse**, ikke noe som løses ved koding alene.

Epics prosess: appen må først merkes **«Ready for Production»** av utvikleren (etter sandkasse-verifisering), deretter må **Helseplattformen selv** («Epic community member») aktivt laste ned/be om appen via Epics «Showroom»-plattform (tidligere «App Orchard») og signere en «open.epic API Subscription Agreement». Dette er utelukkende kundedrevet — vi kan ikke selv trigge en produksjonsdistribusjon hos Helseplattformen, uansett hvor godt sandkasse-testingen går.

**Praktisk konsekvens:** sandkasse-verifiseringen (§6) beviser at protokollen fungerer mot en ekte Epic-instans — verdifullt i seg selv, og direkte sammenlignbart med det vi allerede har gjort mot launch.smarthealthit.org og nav-epj. Et faktisk samarbeid med Helseplattformen er et eget, senere skritt, trolig gjennom Helse Midt-Norge/Helseplattformen AS direkte snarere enn en kald henvendelse via Epics Showroom.

## 8. Referanser

- [fhir.epic.com](https://fhir.epic.com/) — Epic on FHIR, hovedside
- [fhir.epic.com/Developer/Index](https://fhir.epic.com/Developer/Index) — registrering
- [TESTGUIDE-SMARTHEALTHIT.md](TESTGUIDE-SMARTHEALTHIT.md) og [NAV-EPJ-TESTMILJO.md](NAV-EPJ-TESTMILJO.md) — de to andre testmiljøene, samme metodikk
- [IMPLEMENTERING.md §13](IMPLEMENTERING.md) — vår eksisterende `private_key_jwt`-implementasjon (`BuildClientAssertionJwt`), direkte gjenbrukbar
