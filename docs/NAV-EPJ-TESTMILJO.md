# nav-epj som lokalt SMART on FHIR-testmiljø

**Status (2026-08-20): verifisert lokalt.** Full SMART EHR Launch-kjede (launch → authorize → token-exchange) kjørt ende-til-ende mot vår faktiske app, `forer-legeerklaering`, med en lokalt patchet klone av [`navikt/nav-epj`](https://github.com/navikt/nav-epj). Dette dokumentet samler alt en annen person trenger for å reprodusere oppsettet selv — inkludert seks reelle bugs som måtte rettes lokalt for at det skulle fungere i det hele tatt.

## Innhold

1. [Hva er nav-epj](#1-hva-er-nav-epj)
2. [Kan den hostede instansen brukes?](#2-kan-den-hostede-instansen-brukes)
3. [Forutsetninger](#3-forutsetninger)
4. [Steg-for-steg lokalt oppsett](#4-steg-for-steg-lokalt-oppsett)
5. [De seks bugene som måtte rettes](#5-de-seks-bugene-som-måtte-rettes)
6. [Kjente miljø-fallgruver](#6-kjente-miljø-fallgruver)
7. [Hvordan teste — automatisert](#7-hvordan-teste--automatisert)
8. [Hvordan teste — som et menneske, i en nettleser](#8-hvordan-teste--som-et-menneske-i-en-nettleser)
9. [Hva er bekreftet vs. ikke bekreftet](#9-hva-er-bekreftet-vs-ikke-bekreftet)
10. [Videre arbeid](#10-videre-arbeid)

---

## 1. Hva er nav-epj

Et test-EPJ bygget av NAV (team `helseopplysninger`) som simulerer en EHR: SMART on FHIR-launch + en FHIR-API, primært bygget for å teste **`syk-inn`** (NAVs egen sykmeldings-SMART-app — samme system vi allerede sammenligner oss med i [SAMMENLIGNING-syk-inn.md](SAMMENLIGNING-syk-inn.md)). Kotlin/Ktor-backend, React-frontend, Postgres + Valkey.

**Hvorfor dette er relevant for oss:** i motsetning til [launch.smarthealthit.org](https://launch.smarthealthit.org/) (amerikansk, Synthea-data), bruker nav-epj **korrekte norske OID-er** for identifikatorer — verifisert direkte i kildekoden:

| Ressurs | OID | Kildefil |
|---|---|---|
| Pasient (fødselsnummer) | `urn:oid:2.16.578.1.12.4.1.4.1` | `fhir/patient/PatientService.kt` |
| Practitioner (HPR-nummer) | `urn:oid:2.16.578.1.12.4.1.4.4` | `fhir/practitioner/PractitionerService.kt` |
| Organization (org.nummer) | `urn:oid:2.16.578.1.12.4.1.4.101` | `fhir/organization/OrganizationService.kt` |

Disse er identiske med det `FhirPrefillService.GetIdentifier` i vår egen app allerede forventer. Practitioner-ressursen bruker også den norske FHIR-profilen `http://hl7.no/fhir/StructureDefinition/no-basis-Practitioner`. Dette er nøyaktig gapet [HACKATHON-EHIN-2026.md §6](HACKATHON-EHIN-2026.md) pekte på: launch.smarthealthit.org tester protokollen, ikke de norske identifikatorene.

## 2. Kan den hostede instansen brukes?

**Nei.** `https://epj.ansatt.dev.nav.no` er reelt sett stengt for alle utenfor NAV:

```
GET /fhir/.well-known/smart-configuration  → 302 → ansatt.dev.nav.no/oauth2/login
GET /api/fhir/.well-known/smart-configuration → 302 → samme login
GET /auth/callback  → 302 → samme login
```

Selv SMART discovery-endepunktet (som per spec skal være åpent) sitter bak en NAV-ansatt-innlogging («Wonderwall»), og NAIS-konfigurasjonen (`.nais/nais-dev.yaml`) begrenser i tillegg innkommende trafikk til to andre NAV-interne apper (`syk-inn`, `zara`). Se forrige samtale i denne sesjonen for full utforskning — konklusjonen var uendret: **kjør det lokalt i stedet**, som dette dokumentet beskriver.

## 3. Forutsetninger

- **JDK 21.** Ikke nødvendigvis installert på forhånd — se §4.1 for en portabel (ingen-admin-rettigheter-nødvendig) fremgangsmåte.
- **Container-motor** (Docker eller Podman) for Postgres 17 + Valkey.
- **Git.**
- Vårt eget lokale Altinn-miljø må kjøre samtidig — se [HELHETLIG-FLYT.md](HELHETLIG-FLYT.md) for full oppskrift (Altinn localtest-containere + `forer-legeerklaering` på port 5005, nådd via loadbalanceren på `local.altinn.cloud:8000`).
- Node/Yarn er **ikke nødvendig** for testingen beskrevet her — vi bruker aldri nav-epj sitt eget React-frontend, kun dets REST-API og SMART-endepunkter direkte (se §8 for hvorfor dette fortsatt gir en fullverdig, menneske-testbar opplevelse).

## 4. Steg-for-steg lokalt oppsett

### 4.1 Klon og JDK

```bash
git clone https://github.com/navikt/nav-epj.git
```

Patchen i dette dokumentet er testet mot commit `078728bd081e4230fa17bea9ab19362c4010d405` (2026-08-20). Hvis nyere commits har endret de samme filene, kan `git apply` under kreve manuell konfliktløsning.

Hvis du ikke har JDK 21: en MSI-installer kan hange på en UAC-prompt i et ikke-interaktivt miljø. En portabel zip-distribusjon unngår dette helt:

```bash
curl -L -o temurin21.zip "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.12+8/OpenJDK21U-jdk_x64_windows_hotspot_21.0.12_8.zip"
# Pakk ut, og bruk deretter (per shell-sesjon, ikke persistent):
export JAVA_HOME="<utpakket-mappe>/jdk-21.0.12+8"
export PATH="$JAVA_HOME/bin:$PATH"
```

### 4.2 Containere (Postgres + Valkey)

`nav-epj` sin egen `docker-compose.yml` definerer disse to tjenestene:

```bash
docker run -d --name nav-epj-postgres -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=nav-epj -p 5434:5432 postgres:17
docker run -d --name nav-epj-valkey -p 6380:6379 valkey/valkey:8-alpine
```

(Bytt `docker` med `podman` hvis det er det du bruker — kommandoene er identiske.)

### 4.3 Anvend de lokale rettelsene

Se [`nav-epj-local-fixes.patch`](../local-dev/nav-epj-testing/nav-epj-local-fixes.patch) i dette repoet — inneholder alle seks rettelsene fra §5, inkludert konfigurasjonsendringene i §4.4. Anvend den fra roten av din `nav-epj`-klone:

```bash
cd nav-epj
git apply /sti/til/forer-legeerklaering/local-dev/nav-epj-testing/nav-epj-local-fixes.patch
```

Disse rettelsene er **ikke sendt til NAV** — se §5 for anbefaling om det.

### 4.4 Registrer vår app som SMART-klient

Patchen inkluderer allerede denne endringen i `src/main/resources/application-local.yaml`, men den er verdt å forstå: `nav-epj` krever at hver launch-app er forhåndsregistrert med eksakt matchende `redirectUris`/`launchUris` — det finnes ingen "Loose validation"-modus som hos launch.smarthealthit.org.

```yaml
smart:
  clients:
    - clientId: "forer-legeerklaering-poc"
      redirectUris: ["http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/callback"]
      launchUris: ["http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/launch"]
```

Patchen flytter også hele oppsettet fra port 8080 til **8090** (8080 er allerede i bruk av vår egen HAPI FHIR-mock, se [HELHETLIG-FLYT.md](HELHETLIG-FLYT.md)), og fra `localhost` til `[::1]`/`"::1"` for database/Valkey (se §6.1 for hvorfor).

### 4.5 Bygg og start

```bash
cd nav-epj
./gradlew.bat runLocal    # Windows
./gradlew runLocal         # macOS/Linux
```

Verifiser: `curl http://localhost:8090/fhir/.well-known/smart-configuration` skal gi `200` med en SMART discovery-dokument.

**Viktig:** i dev-modus (`-Dio.ktor.development=true`, satt av `runLocal`-tasken selv) kjører Flyway **`clean` før `migrate`** ved hver oppstart — all testdata i Postgres forsvinner ved hver restart. Valkey (som holder "aktiv pasient for innlogget behandler") er uansett kun i minnet og forsvinner ved container-restart. Se §7/§8 for hvordan du re-seeder testdata raskt.

I dev-modus er autentisering en **stub** som alltid logger deg inn som "Bjarte Legesen", HPR `111222333` — ingen ekte HelseID-innlogging er involvert eller nødvendig lokalt.

## 5. De seks bugene som måtte rettes

Uten disse rettelsene er `/fhir/launch`-endepunktet i `nav-epj`, slik det ligger på GitHub i dag, **helt ikke-funksjonelt — 100 % feilrate, for enhver klient**, ikke bare for oss.

| # | Fil | Symptom | Rotårsak |
|---|---|---|---|
| 1 | `fhir/EpjClient.kt` | `500` — `MismatchedInputException` ved ethvert internt kall som returnerer et `*Id`-felt | Den interne HTTP-klienten (`epjClient`, brukt av `PatientService`/`PractitionerService`/`EncounterService` osv. til å kalle EPJ-laget) manglet `KotlinModule`/`uuidModule` som hovedserverens `ContentNegotiation` registrerer |
| 2 | `plugins/Serialization.kt` | Selv med #1 rettet: `Uuid.parse` feilet med "53 tegn, forventet 32/36" | `uuidModule` sin egen deserializer kalte `Uuid.parse(p.toString())` — det gir parserens Java-objekt-`toString()`, ikke JSON-verdien. Riktig er `p.getString()` |
| 3 | `plugins/Serialization.kt` | `500` — `KotlinInvalidNullException` ved henting av aktiv konsultasjon | `kotlinx.datetime.LocalDateTime` hadde ingen registrert Jackson-(de)serializer noe sted. Serialisering "virket" tilfeldig via generisk bean-refleksjon (et ikke-standard JSON-objekt med et syntetisk `value$kotlinx_datetime`-felt), men deserialisering feilet alltid |
| 4 | `build.gradle.kts` + `gradle/libs.versions.toml` | `SerializationException: Serializer for class 'LaunchContext' is not found` — **dette er den mest kritiske bugen** | `ValkeyService` bruker `kotlinx.serialization.json.Json.encodeToString`, men Gradle-pluginet som genererer serializerne (`org.jetbrains.kotlin.plugin.serialization`) var aldri lagt til i build-oppsettet. `/fhir/launch` kunne derfor aldri fungere, for noen klient |
| 5 | `smart/api/SmartRouting.kt` | `502` fra en typisk nginx-oppsatt reverse proxy (stripper `://` fra ukodede query-parametre) ved launch-redirecten; `400 State mismatch` ved callback-redirecten | `iss`/`launch`/`code`/`state` ble limt inn i redirect-URL-er med ren strenginterpolasjon i stedet for URL-encoding. `state` er en base64-streng som ofte inneholder `+`/`=` — et ukodet `+` tolkes som mellomrom av standard query-parsing, noe som korrumperer state-verdien og trigger en falsk CSRF-avvisning hos mottakeren nesten hver gang |
| 6 | `epj/pasient/PasientRepository.kt` | `500` — fremmednøkkelbrudd på `pasient_helsepersonell` ved `POST /api/patient` med et allerede brukt fødselsnummer | `insertIgnore` på selve pasient-raden er ment å gjøre kallet idempotent på `fnr`, men koden setter deretter **ubetinget** inn en rad i koblingstabellen med den nygenererte (og ved konflikt aldri faktisk lagrede) pasient-ID-en |

Bug #1–#5 er alle rettet i [`nav-epj-local-fixes.patch`](../local-dev/nav-epj-testing/nav-epj-local-fixes.patch). Bug #6 er **ikke patchet** — [`seed-and-get-launch-url.ps1`](../local-dev/nav-epj-testing/seed-and-get-launch-url.ps1) unngår den i stedet ved å sjekke om testpasienten allerede finnes før den forsøker å opprette en ny.

**Anbefaling:** disse seks bugene er ikke sendt til NAV. Gitt at bug #4 gjør hele SMART-launch-funksjonaliteten ikke-funksjonell for alle, kan det være verdt å melde dem — f.eks. som GitHub issues på `navikt/nav-epj`, eller via samme kontaktflate som er brukt for andre NAV-relaterte spørsmål i dette prosjektet. Ikke gjort ennå — avventer beslutning.

## 6. Kjente miljø-fallgruver

Disse er ikke bugs i `nav-epj`, men lokale miljøproblemer som dukket opp under oppsettet — verdt å kjenne til før man antar noe er feil med selve appen.

### 6.1 `localhost` løser til IPv6, men containerens portmapping er kun IPv4 (eller omvendt)

Symptom: `Connection refused` fra Postgres/Valkey selv om containeren kjører og porten er mappet korrekt (`podman port` viser riktig mapping). Rotårsak: podman-machine/WSL2-miljøet mapper noen ganger porten kun på `[::1]` (IPv6-loopback), ikke `127.0.0.1`. `localhost` på Windows kan resolve til IPv4 først. **Løsning:** bruk `[::1]` explisitt i JDBC-URL-en og Valkey-hostnavnet (allerede gjort i patchen).

### 6.2 Portkollisjon med vår egen HAPI FHIR-mock

Vår egen [smart-mock](../local-dev/smart-mock/) sitt `SmartOnFhir:FhirBaseUrlOverride` peker på `http://localhost:8080/fhir` — samme standardport som `nav-epj` sin `application-local.yaml` opprinnelig bruker. **Løsning:** flyttet `nav-epj` til port 8090 (del av patchen).

### 6.3 Appens `isRealExternalIssuer`-heuristikk forutsetter https

`SmartLaunchController.Callback()` i vår egen app avgjør om `iss` er en "ekte ekstern server" ved å sjekke om den starter med `https://`. `nav-epj` sin lokale `iss` er `http://localhost:8090/fhir` — vanlig http. Uten tiltak vil appen feilaktig bruke `SmartOnFhir:FhirBaseUrlOverride` (som peker på vår EGEN HAPI FHIR-mock, ikke nav-epj) i stedet for den faktiske `iss`-verdien. **Løsning (kun for denne testen, satt via `dotnet user-secrets`, ikke committet):**

```bash
cd src/App
dotnet user-secrets set "SmartOnFhir:FhirBaseUrlOverride" "http://localhost:8090/fhir"
```

Dette er en reell, dokumentert begrensning i vår egen kode (ikke bare et testoppsett-hack) — en ekte EPJ i produksjon vil naturligvis bruke `https://`, men en lokal test-EPJ på ren http (som denne, eller som en fremtidig on-prem-installasjon bak en intern proxy) rammes av heuristikken. Verdt å vurdere en mer robust løsning senere (f.eks. en explisitt allowlist i stedet for et skjema-gjett).

### 6.4 Nginx stripper `iss`-parameteren når den ikke er url-kodet

Selv med bug #5 rettet i `nav-epj`: dette er en generell, allerede dokumentert fallgruve i vårt eget miljø, se [IMPLEMENTERING.md §7.5](IMPLEMENTERING.md). Altinn Local Test sin nginx-konfigurasjon fjerner query-parametre som inneholder et ukodet `://`. Appens `SmartOnFhir:DefaultIss`-fallback (`iss ??= _config["SmartOnFhir:DefaultIss"]`) finnes for nettopp dette scenarioet:

```bash
dotnet user-secrets set "SmartOnFhir:DefaultIss" "http://localhost:8090/fhir"
```

### 6.5 `podman-compose.yml` sin `HOST_DOMAIN=host.docker.internal` løser seg ikke korrekt

Uavhengig av `nav-epj`: `app-localtest` (vårt eget søsterrepo, se [HELHETLIG-FLYT.md](HELHETLIG-FLYT.md)) sin `podman-compose.yml` setter `HOST_DOMAIN=host.docker.internal` for loadbalanceren, men denne hostnavn-oppslagningen fungerte ikke i denne podman-machine-konfigurasjonen — nginx endte opp med å prøve å nå appen på en ugyldig APIPA-adresse (`169.254.1.2`), uavhengig av om det var restartet flere ganger. Bekreftet **ikke** løst av å restarte containerne eller hele podman-machinen. **Løsning (lokal, ikke committet i `app-localtest`):** sett `HOST_DOMAIN` til den faktiske, statiske WSL-vEthernet-adressen (`ipconfig` → "vEthernet (WSL (Hyper-V firewall))" → IPv4-adresse, typisk `172.30.80.1`) direkte i `podman-compose.yml`, i stedet for `host.docker.internal`. Dette er samme statiske verdi `docker-compose.yml` (søskenfilen, ment for ren Docker Desktop) allerede bruker — men den filen kunne ikke brukes direkte, siden den binder loadbalanceren til privilegert port 80, som rootless podman avviser.

## 7. Hvordan teste — automatisert

Forutsetter at hele miljøet kjører (§4 + [HELHETLIG-FLYT.md](HELHETLIG-FLYT.md) for vår egen app/containere) og at fallgruvene i §6.3/§6.4 er håndtert via `dotnet user-secrets`.

```powershell
.\local-dev\nav-epj-testing\seed-and-get-launch-url.ps1
```

Skriver ut en ferdig launch-URL. For en fullautomatisert curl-basert kjøring av selve launch→authorize→callback-kjeden (ingen nettleser involvert), se mønsteret i [`local-dev/smarthealthit-testing/test-smart-launch.ps1`](../local-dev/smarthealthit-testing/test-smart-launch.ps1) — samme prinsipp (følg `Location`-headere manuelt med en cookie-jar), men mot `nav-epj` sine endepunkter i stedet. Ikke bygget som et eget skript ennå siden `nav-epj` sin launch-URL-struktur (en enkel opak `launch`-ID, ikke et base64-array) er enklere og ikke krever noen ekvivalent til smarthealthit.org sin "Simulated Error"-meny.

**Forventet resultat:** alle steg gir `302`, og siste steg setter `AltinnStudioRuntime`-cookien — se [TESTGUIDE-SMARTHEALTHIT.md](TESTGUIDE-SMARTHEALTHIT.md) for hvordan man leser curl-headere for å bekrefte dette selv.

## 8. Hvordan teste — som et menneske, i en nettleser

`nav-epj` sitt eget React-frontend er **ikke bygget/kjørt** i dette oppsettet — det er ikke nødvendig for å teste selve SMART-launch-opplevelsen. Siden dev-modus bruker en autentiserings-stub (§4.5) som alltid logger deg inn automatisk, kan du gå rett til launch-URL-en i en vanlig nettleser:

1. Kjør `seed-and-get-launch-url.ps1` (§7) og kopiér URL-en den skriver ut.
2. Lim URL-en inn i adressefeltet i en nettleser (ikke i Claude Code sitt nettleserverktøy — det er sandboxet mot `local.altinn.cloud`, se [TESTGUIDE-SMARTHEALTHIT.md](TESTGUIDE-SMARTHEALTHIT.md)).
3. Trykk Enter. Du blir omdirigert flere ganger i rask rekkefølge (synlig i adressefeltet hvis du er observant): `nav-epj` → vår app sitt `/smart/launch` → `nav-epj` sitt `/oidc/authorize` → vår app sitt `/smart/callback` → til slutt `forer-legeerklaering`-appens forside, med en aktiv Altinn-sesjon.
4. Ingen innlogging, passord eller klikk er nødvendig underveis — hele kjeden er en ren redirect-sekvens.

Dette er den enkleste måten å demonstrere at hele flyten fungerer for en person som ikke ønsker å lese curl-output — pek en nettleser på én URL, se den lande i vår app.

**For å inspisere hva som skjer i FHIR-laget spesifikt** (f.eks. for en fagperson som vil se selve FHIR-ressursene, ikke bare sluttresultatet): `nav-epj` sine FHIR-endepunkter krever et gyldig Bearer-access-token (bekreftet: `401 Unauthorized` uten), så de kan ikke besøkes direkte i en nettleser uten videre. Bruk heller `curl` med tokenet appen selv mottok (synlig i `dotnet run`-konsollet i Development-modus), eller se §9 for hva som gjenstår å bekrefte her.

## 9. Hva er bekreftet vs. ikke bekreftet

**Bekreftet, med bevis:**
- Full SMART EHR Launch-kjede (launch → authorize → token-exchange) fungerer ende-til-ende mot vår faktiske app, gjentatt flere ganger etter alle rettelser.
- `nav-epj` sine FHIR-endepunkter krever Bearer-token (`401` uten) — korrekt sikkerhetsoppførsel.
- Norske identifikator-OID-er (fnr, HPR, orgnr) stemmer med det vår app allerede forventer (bekreftet i kildekoden, ikke bare ved kjøring).

**Ikke bekreftet:**
- At selve FHIR-prefillen i `FhirPrefillService` faktisk mapper dataene riktig inn i et Altinn-skjema. Dette krever at en ekte Altinn-instans opprettes (appens `IInstantiationProcessor`/prefill-hooks trigges av instansopprettelse, ikke av å bare laste appens forside) — en større, separat mekanisme som ikke er testet i denne runden.
- `request_invalid_token`/`request_expired_token`-type scenarioer (utløpt/ugyldig access token ved selve FHIR-ressurskallet) — ingen ekvivalent til smarthealthit.org sin "Simulated Error"-meny finnes i `nav-epj`.
- `private_key_jwt`-autentisering mot `nav-epj` — SMART discovery-dokumentet lister det som støttet (`token_endpoint_auth_methods_supported`), men `/oidc/token`-handleren i `SmartRouting.kt` har ingen kode-sti for `client_assertion` i det hele tatt, kun Basic-auth for `client_secret`. Ikke testet siden det uansett ville feilet.

## 10. Videre arbeid

- Vurder å melde bug #1–#6 til NAV (`helseopplysninger`-teamet) — se anbefaling i §5.
- Bekreft faktisk FHIR-prefill ved å gå gjennom en full Altinn-instansopprettelse (ikke bare launch+callback).
- Vurder en mer robust erstatning for `isRealExternalIssuer`-heuristikken i vår egen `SmartLaunchController.cs` (§6.3) — den fungerer i dag kun fordi vi visste å sette `FhirBaseUrlOverride` manuelt for denne testen.
- Vurder å bygge/kjøre `nav-epj` sitt React-frontend for en fullstendig admin-UI-opplevelse (pasientoppslag, konsultasjonshistorikk) — ikke nødvendig for SMART-launch-testing, men nyttig hvis man vil utforske EPJ-simulatoren mer grundig.
