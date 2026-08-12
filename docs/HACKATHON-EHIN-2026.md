# Norwegian FHIR Hackathon 2026 (EHiN pre-konferanse) — forberedelse og gap-analyse

**Dato:** 2026-08-11
**Arrangement:** [Norwegian FHIR Hackathon 2026](https://hl7norway.github.io/Norwegian-FHIR-Hackathon-2026/currentbuild/smart-track.html) — 9. november 2026, Rebel, Oslo. Del av EHiN-pre-konferansen. Digitalt forbesøk 2. november 09:00–11:00. Arrangeres av HL7 Norge i samarbeid med NHN, Helsedirektoratet, Bedredelt, NoMA, IHE Norge og EHiN. Gratis deltakelse.
**Relevant spor:** SMART on FHIR (track lead: Leo-Andreas Ervik, NAV)

---

## 1. Hvorfor dette er relevant for oss

Sporbeskrivelsen sier det rett ut: *«FHIR is the data. SMART is what makes an app safe to plug into someone else's journal system.»* — det er nøyaktig det `forer-legeerklaering`-PoC-en har demonstrert siden juni, og som ble **fullstendig ende-til-ende-verifisert mot en ekte SMART-server i dag** (se [IMPLEMENTERING.md §13](IMPLEMENTERING.md)).

Testmiljøet hackathon-sporet bruker er slående likt vårt eget:
- **HelseID** som nasjonal identitetsleverandør for helsepersonell — nøyaktig det [IMPLEMENTERING.md §14](IMPLEMENTERING.md) allerede planlegger, og som vi har praktisk erfaring med fra Helsenorge EksternAPI-arbeidet (§14.1).
- **Norske identifikatorer** (fødselsnummer, d-nummer, HPR-nummer, organisasjonsnummer) — nøyaktig de OID-ene `FhirPrefillService.cs` allerede mapper (`urn:oid:2.16.578.1.12.4.1.4.x`).
- **ICPC-2/ICD-10-koding** — vi bruker allerede ICD-10 i `FillCondition`.
- **SMART App Launch IG STU 2.2, FHIR R4, OAuth2 + PKCE** — identisk med det vi allerede har implementert og nettopp verifiserte.

**Vår posisjon er uvanlig sterk for dette arrangementet:** de fleste deltakere starter fra et skjelett-repo og bygger Bronse-nivå fra bunnen i løpet av dagen. Vi kommer med en **ekte, produksjonsformet Altinn Studio-app** som allerede er verifisert mot en ekte ekstern SMART-server. Det er en god historie å fortelle — og en mulighet til å teste PoC-en mot *enda et* uavhengig SMART-miljø.

---

## 2. Krav per nivå vs. vår status

### Bronse — obligatorisk

| Krav | Vår status |
|---|---|
| Launch app fra EPJ | ✅ Verifisert mot launch.smarthealthit.org 2026-08-11 |
| Autorisasjonskode-utveksling server-side | ✅ `ExchangeCodeForToken` i `SmartLaunchController.cs` — BFF-mønster, token forlater aldri nettleseren |
| Vis pasientnavn, alder, aktiv konsultasjon | ⚠️ Nesten — vi viser fødselsdato, ikke beregnet alder. Konsultasjonsdato vises. Trivielt å legge til alder |
| Forstå hvert steg i redirect-flyten | ✅ Nettopp gått gjennom dette i praksis — 7 bugs funnet og forstått, se IMPLEMENTERING.md §13 |

**Konklusjon:** Bronse er i praksis allerede oppnådd. Eneste konkrete mangel er en alder-beregning fra fødselsdato (< 30 min arbeid).

### Sølv — velg én

| Alternativ | Vår status |
|---|---|
| **Klinisk mini-app** (diagnoser/målinger med klinisk verdi — trender, sammendrag, flagg) | ⚠️ Delvis. `FillCondition` henter siste aktive diagnose, men **`Observation` er ikke implementert i det hele tatt** — scope `patient/Observation.read` etterspørres, men brukes aldri. Ingen trend/sammendrag/flagging-logikk finnes |
| **Scope-detektivarbeid** (be om smalere tilganger, dekod tokens, bevis hva som faktisk ble innvilget vs. forespurt) | ❌ Ikke gjort. Vi ber om ett fast, bredt scope-sett (14 scopes) og sjekker aldri hva som faktisk ble innvilget. Token-responsens `scope`-felt leses ikke ut og sammenlignes |
| **Redo launch med client_secret-autentisering** i stedet for public client | ⚠️ Delvis — `ExchangeCodeForToken` støtter allerede Basic-auth med client_secret hvis konfigurert (`if (!string.IsNullOrEmpty(clientSecret))`), men `ClientSecret` er tom streng i begge appsettings-filer i dag. Koden er der, men aldri faktisk testet med en ekte hemmelighet |

**Anbefaling:** «Redo launch med client_secret» er billigst å få demonstrert — koden finnes allerede, bare sett en verdi og verifiser Basic-auth-headeren faktisk sendes og godtas.

### Gull — velg én

| Alternativ | Vår status |
|---|---|
| **Writeback til EPJ** (dokumenter eller målinger) | ❌ Ikke implementert. Eksplisitt planlagt som [VEIKART.md fase 2](VEIKART.md) — `DocumentReference`-writeback er beskrevet i detalj, men ikke kodet |
| **`private_key_jwt` asymmetrisk klientautentisering ende-til-ende** | ❌ Ikke implementert for *denne* klienten (SMART EHR launch mot EPJ). **Men** vi har akkurat gjort nøyaktig dette for en annen klient (Helsenorge EksternAPI, se [IMPLEMENTERING.md §14.1](IMPLEMENTERING.md) og `local-dev/helseid-token-test/`) — samme mønster, samme `HelseID.Library`-erfaring, kan trolig gjenbrukes/tilpasses raskt |
| **Backend services SMART-flyt** (system-til-system, ingen bruker til stede) | ❌ Ikke implementert for SMART EHR-domenet. **Men** `client_credentials`-flyten vi bygde for Helsenorge EksternAPI er konseptuelt identisk (maskin-til-maskin, ingen brukerinnlogging) — se `local-dev/helsenorge-oppgave-test/` |
| **Sikkerhetstesting** (tukle med autorisasjonsparametere, gjenbruk koder, feil audience, utløpte tokens, be om ikke-autoriserte ressurser) | ❌ Ikke gjort systematisk. Dette ville trolig avdekke reelle hull — vi validerer i dag **ikke** token-signatur, issuer eller audience noe sted (jf. [RISIKOREGISTER.md R4](RISIKOREGISTER.md): tokenvalidering er ikke implementert) |

**Anbefaling:** Ingen Gull-oppgave er triviell, men **`private_key_jwt`** og **backend services-flyt** er de vi har mest overførbar kompetanse på fra denne sesjonens Helsenorge-arbeid. **Sikkerhetstesting** er den mest verdifulle å faktisk gjøre uavhengig av hackathon, siden den ville avdekke reelle produksjonsrisikoer vi allerede har flagget (R4) men ikke undersøkt konkret.

---

## 3. Konkret forberedelsesliste før 9. november

Prioritert etter innsats vs. verdi:

1. **Alder fra fødselsdato** (Bronse, trivielt) — vis beregnet alder i tillegg til fødselsdato.
2. **`Observation`-håndtering** (Sølv, moderat) — implementer `FillObservation` i `FhirPrefillService.cs` etter samme mønster som `FillCondition`. Gir også reell verdi til PoC-en uavhengig av hackathon, siden IS-2569 har flere helsekategorier som naturlig kobles til målinger (syn, blodtrykk osv.).
3. **Test client_secret-autentisering** (Sølv, lite) — sett en test-hemmelighet i `appsettings.Development.json` (via `dotnet user-secrets`, ikke i git — jf. tidligere sesjons funn om at `appsettings.Development.json` er sporet i git) og verifiser Basic-auth-flyten fungerer mot smarthealthit.org (som støtter både public og confidential clients).
4. **Scope-detektivarbeid** (Sølv, moderat) — logg/vis differansen mellom forespurt og innvilget scope fra token-responsen. Nyttig diagnostikk uavhengig av hackathon.
5. **Sikkerhetstesting-runde** (Gull, moderat–høy verdi) — bruk smarthealthit.org sin innebygde «Simulated Error»-funksjon (invalid client_id, invalid redirect_uri, expired token, osv. — se dropdown i launcheren) til å systematisk teste at appen feiler trygt. Direkte input til [RISIKOREGISTER.md R4](RISIKOREGISTER.md).
6. **`private_key_jwt` for SMART EHR-klienten** (Gull, høy innsats) — vurder om dette er verdt å gjøre før eller *på* selve hackathon-dagen, gitt at vi har fersk erfaring fra Helsenorge-arbeidet.
7. **Writeback-prototype** (Gull, høy innsats) — selv en minimal `DocumentReference`-POST mot smarthealthit.org sin FHIR-server (som støtter skriving) ville være et konkret første steg på [VEIKART.md fase 2](VEIKART.md).

**Ikke gjør før hackathon:** Ikke bruk tid på ting som uansett krever ekte norsk EPJ-tilgang (R1) eller NHN-kontakt (R9) — dette miljøet er amerikansk/Synthea-basert og løser ikke de avhengighetene.

---

## 4. Strategisk vinkel — hvorfor delta

- **Synlighet:** en fungerende Altinn Studio + SMART on FHIR-app er en sjelden kombinasjon i det norske FHIR-miljøet. Dette er en anledning til å vise fram [STRATEGI.md](STRATEGI.md) sin plattformvisjon for et fagpublikum.
- **Nettverk mot EPJ-leverandører:** hackathonet samler nettopp de aktørene som er relevante for [RISIKOREGISTER.md R1](RISIKOREGISTER.md) (fastlege-EPJ FHIR-modenhet er ukjent) og potensielt for HelseID-kontakter (C-2).
- **EHiN-tilknytning:** som pre-konferanse til EHiN gir dette en naturlig arena for å nevne PoC-en for et bredere publikum enn det rent tekniske hackathon-miljøet.
- **Gratis, lav risiko:** ingen kostnad, og selv et Bronse-nivå-resultat er allerede oppnådd — nedsiden ved å delta er minimal.
- **Trolig første sjanse til å teste mot norske FHIR-identifikatorer** — se punkt 6.

---

## 6. NAVs referansemateriale — og hvorfor det ikke løser R1 i forkant

Sporet lenker til to NAV-repoer (samme team som `syk-inn`, jf. [SAMMENLIGNING-syk-inn.md](SAMMENLIGNING-syk-inn.md)):

| Repo | Hva det er | Relevans for oss |
|---|---|---|
| [`navikt/smart-on-fhir`](https://github.com/navikt/smart-on-fhir) | Server-only SMART-bibliotek (`SmartClient` + `ReadyClient`) — samme rolle som vår `SmartLaunchController`/`FhirPrefillService` | Referansekode for en produksjonsrettet norsk SMART-klient. Ikke produksjonsklar selv (eksplisitt merket) |
| [`navikt/smart-on-fhir-validator`](https://github.com/navikt/smart-on-fhir-validator) | En **klient**-app ment å bli startet *fra* en EPJ, som validerer at EPJ-en korrekt tilbyr HPR-nummer og fødselsnummer/D-nummer med riktige OID-er | **Feil retning for oss** — vi trenger noe som simulerer en EPJ med norske data, ikke en klient til. Nyttig likevel som fasit på hvilke OID-sjekker en «compliant» norsk EPJ må bestå |

**Konklusjon:** Ingen av disse gir oss en EPJ-simulator med norske identifikatorer å teste mot *før* hackathonet. `launch.smarthealthit.org` (som vi allerede har verifisert mot) bruker amerikanske Synthea-data — det tester protokollen, ikke de norske OID-ene. Sporbeskrivelsen sier det medbrakte EPJ-testmiljøet på selve dagen har «Synthetic Norwegian patient data» (fnr, D-nummer, HPR-nummer, orgnr) — **dette er trolig første reelle mulighet vi får til å verifisere `GetIdentifier`-mappingen i `FhirPrefillService.cs` mot ekte norske OID-er i praksis**, ikke bare i kode.

**Forberedelse som faktisk kan gjøres i forkant:** les gjennom `smart-on-fhir-validator` sin valideringslogikk for OID-sjekkene (HPR: `2.16.578.1.12.4.1.4.4`, fnr: `2.16.578.1.12.4.1.4.1`, D-nummer: `2.16.578.1.12.4.1.4.2`) og bekreft at disse stemmer nøyaktig overens med det `FhirPrefillService.GetIdentifier` allerede bruker — de matcher for HPR og fnr, men **D-nummer er ikke håndtert i det hele tatt i dag** (kun fødselsnummer-OID-en sjekkes). Verdt å legge til før hackathonet, siden D-nummer er vanlig for pasienter uten norsk fødselsnummer.

---

## 7. Referanser

- [SMART on FHIR-sporet](https://hl7norway.github.io/Norwegian-FHIR-Hackathon-2026/currentbuild/smart-track.html)
- [IMPLEMENTERING.md §13](IMPLEMENTERING.md) — vår egen verifisering mot launch.smarthealthit.org, inkl. alle 7 bugs
- [IMPLEMENTERING.md §14 og §14.1](IMPLEMENTERING.md) — HelseID og Helsenorge EksternAPI-erfaring, direkte overførbar til Gull-nivå
- [VEIKART.md](VEIKART.md) fase 2 — writeback-planen som overlapper med Gull-alternativ 1
- [navikt/smart-on-fhir](https://github.com/navikt/smart-on-fhir) og [navikt/smart-on-fhir-validator](https://github.com/navikt/smart-on-fhir-validator) — NAVs referansekode og OID-valideringslogikk
- [RISIKOREGISTER.md](RISIKOREGISTER.md) R1 (EPJ-modenhet), R4 (sikkerhetsgap), R9 (NHN-kontakt)
