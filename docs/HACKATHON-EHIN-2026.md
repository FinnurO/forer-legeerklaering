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

**Oppdatert 2026-08-13:** Bronse er i praksis oppnådd. På Sølv har vi krysset av **to av tre alternativer** (scope-detektivarbeid, client_secret-autentisering). På Gull har vi krysset av **to av fire alternativer** (writeback til EPJ, `private_key_jwt`) og gjort en fullstendig sikkerhetstestrunde (alle 9 «Simulated Error»-varianter reprodusert, ikke bare 3) — alt mot et amerikansk testmiljø. Det gjenstår å bekrefte at det samme fungerer mot hackathonets norske EPJ-testmiljø på selve dagen, men det tekniske mønsteret er bevist for begge Gull-alternativene og repeterbart via [`local-dev/smarthealthit-testing/`](../local-dev/smarthealthit-testing/) (se [TESTGUIDE-SMARTHEALTHIT.md](TESTGUIDE-SMARTHEALTHIT.md)). Vi går inn i dette arrangementet med mer enn det som kreves alt bestått.

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

**Status (2026-08-13): to av tre alternativer er oppnådd.**

| Alternativ | Vår status |
|---|---|
| **Klinisk mini-app** (diagnoser/målinger med klinisk verdi — trender, sammendrag, flagg) | ⚠️ Delvis. `FillCondition` henter siste aktive diagnose, men **`Observation` er ikke implementert i det hele tatt** — scope `patient/Observation.read` etterspørres, men brukes aldri. Ingen trend/sammendrag/flagging-logikk finnes |
| **Scope-detektivarbeid** (be om smalere tilganger, dekod tokens, bevis hva som faktisk ble innvilget vs. forespurt) | ✅ **2 av 3 deler gjort 2026-08-11–12.** «Dekod tokens»: `TryExtractClaimFromJwt` dekoder `access_token`-JWT-en for `fhirUser`-claimet (fant vi trengte dette da toppnivåfeltet manglet, se §13 funn #7). «Bevis innvilget vs. forespurt»: `TokenResponse.Scope` + `/smart/test-writeback` viser innvilget scope explisitt — bekreftet at begge skrivescopene ble innvilget uten innsnevring. **Gjenstår:** selve eksperimentet med å *be om* et smalere scope-sett og se hva som skjer |
| **Redo launch med client_secret-autentisering** i stedet for public client | ✅ **Gjort 2026-08-13.** `ExchangeCodeForToken` sin Basic-auth-vei kjørt ende-til-ende mot launch.smarthealthit.org (Confidential Symmetric, Loose-validering) via `dotnet user-secrets` + [`test-smart-launch.ps1`](../local-dev/smarthealthit-testing/test-smart-launch.ps1) — `302` med Altinn-sesjon etablert. Se [IMPLEMENTERING.md §13](IMPLEMENTERING.md) og [TESTGUIDE-SMARTHEALTHIT.md](TESTGUIDE-SMARTHEALTHIT.md) |

**Anbefaling (oppdatert 2026-08-13):** To av tre Sølv-alternativer er krysset av. Gjenstående arbeid for et tredje: eksplisitt teste en smalere scope-forespørsel (10 minutter), eller `Observation`-håndtering (moderat innsats, se §3).

### Gull — velg én

**Status (2026-08-13): to av fire alternativer er oppnådd, sikkerhetstesting fullført.**

| Alternativ | Vår status |
|---|---|
| **Writeback til EPJ** (dokumenter eller målinger) | ✅ Skrivemekanikk bevist 2026-08-11 — `POST DocumentReference` mot launch.smarthealthit.org ga `HTTP 201 Created`, ingen innsnevring av skrivescope. Timing-bug funnet og rettet 2026-08-12 (avledningslogikk flyttet fra `ProcessDataWrite` til `IProcessTaskEnd.End()`, se §13). Gjenstår: ekte innhold (PDF), idempotens (PUT + klient-id), `QuestionnaireResponse`. Se [VEIKART.md fase 2](VEIKART.md), [IMPLEMENTERING.md §13](IMPLEMENTERING.md) |
| **`private_key_jwt` asymmetrisk klientautentisering ende-til-ende** | ✅ **Gjort 2026-08-13.** Implementert i `ExchangeCodeForToken`/`BuildClientAssertionJwt` (RFC 7523, RS384) for *denne* klienten (SMART EHR launch, ikke bare Helsenorge EksternAPI-erfaringen fra §14.1). RSA-nøkkelpar generert lokalt (se [`generate-client-assertion-jwk.ps1`](../local-dev/smarthealthit-testing/generate-client-assertion-jwk.ps1)), kjørt ende-til-ende mot launch.smarthealthit.org (Confidential Asymmetric, Loose-validering) — `302` med Altinn-sesjon etablert. **Ikke testet:** faktisk kryptografisk signaturverifisering (Strict-modus/registrert JWKS) — kun strukturell validering. Se [IMPLEMENTERING.md §13](IMPLEMENTERING.md) og [TESTGUIDE-SMARTHEALTHIT.md](TESTGUIDE-SMARTHEALTHIT.md) |
| **Backend services SMART-flyt** (system-til-system, ingen bruker til stede) | ❌ Ikke implementert for SMART EHR-domenet. **Men** `client_credentials`-flyten vi bygde for Helsenorge EksternAPI er konseptuelt identisk (maskin-til-maskin, ingen brukerinnlogging) — se `local-dev/helsenorge-oppgave-test/` |
| **Sikkerhetstesting** (tukle med autorisasjonsparametere, gjenbruk koder, feil audience, utløpte tokens, be om ikke-autoriserte ressurser) | ✅ **Fullført 2026-08-13.** Alle 9 «Simulated Error»-varianter fra launch.smarthealthit.org reprodusert automatisert med [`test-smart-launch.ps1`](../local-dev/smarthealthit-testing/test-smart-launch.ps1) — løsningen på den tidligere blokkeringen («krever interaktiv innlogging») var å forhåndsvelge pasient+behandler i launch-payloaden, som hopper over launcherens velger helt. 2 robusthetsbugs funnet og rettet i `SmartLaunchController.Callback()` (manglende null-sjekk på `smartConfig`, manglende sjekk på tomt `access_token`). Alle varianter feiler trygt der de faktisk trigger en feil (`502`, ingen stacktrace, ingen hemmeligheter) — se [IMPLEMENTERING.md §13](IMPLEMENTERING.md) for full resultattabell inkl. hvilke varianter som viste seg å ikke ha noen effekt mot en Public client. **Fortsatt ikke gjort:** ekte tokenvalidering (signatur/issuer/audience) er fortsatt ikke implementert (jf. [RISIKOREGISTER.md R4](RISIKOREGISTER.md)) — denne runden testet kun feilhåndtering, ikke selve valideringsgapet; heller ikke `request_invalid_token`/`request_expired_token` mot selve FHIR-prefill-steget |

**Anbefaling (oppdatert 2026-08-13):** To Gull-alternativer er krysset av (writeback, `private_key_jwt`) — mer enn nok til å demonstrere Gull-nivå. Sikkerhetstesting er fullført som en selvstendig verdifull øvelse utover selve hackathon-kravet. **Gjenstående, om vi vil ha et tredje Gull-alternativ:** backend services-flyt, hvor `client_credentials`-mønsteret fra Helsenorge-arbeidet er direkte overførbart.

---

## 3. Konkret forberedelsesliste før 9. november

**Oppdatert 2026-08-13 — fem av sju punkter er nå gjort:**

| # | Punkt | Nivå | Status |
|---|---|---|---|
| 1 | Alder fra fødselsdato | Bronse | ❌ Gjenstår (trivielt) |
| 2 | `Observation`-håndtering | Sølv | ❌ Gjenstår (moderat innsats) |
| 3 | ~~Test client_secret-autentisering~~ | Sølv | ✅ **Gjort 2026-08-13** — se §2 |
| 4 | ~~Scope-detektivarbeid~~ | Sølv | ✅ **Gjort 2026-08-11–12** — se §2 |
| 5 | ~~Sikkerhetstesting-runde~~ | Gull | ✅ **Fullført 2026-08-13** — se §2 |
| 6 | ~~`private_key_jwt` for SMART EHR-klienten~~ | Gull | ✅ **Gjort 2026-08-13** — se §2 |
| 7 | ~~Writeback-prototype~~ | Gull | ✅ **Gjort 2026-08-11** — se §2 |

**Gjenstående, prioritert etter innsats vs. verdi:**

1. **Alder fra fødselsdato** (Bronse, trivielt) — vis beregnet alder i tillegg til fødselsdato.
2. **`Observation`-håndtering** (Sølv, moderat) — implementer `FillObservation` i `FhirPrefillService.cs` etter samme mønster som `FillCondition`. Gir også reell verdi til PoC-en uavhengig av hackathon, siden IS-2569 har flere helsekategorier som naturlig kobles til målinger (syn, blodtrykk osv.).

**Ikke gjør før hackathon:** Ikke bruk tid på ting som uansett krever ekte norsk EPJ-tilgang (R1) eller NHN-kontakt (R9) — dette miljøet er amerikansk/Synthea-basert og løser ikke de avhengighetene. Backend services-flyten (Gull-alternativ 3) er også lav prioritet nå — vi har allerede to Gull-alternativer krysset av.

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
- [TESTGUIDE-SMARTHEALTHIT.md](TESTGUIDE-SMARTHEALTHIT.md) — hvordan reprodusere alle testene selv (client_secret, private_key_jwt, alle 9 Simulated Error-varianter)
- [IMPLEMENTERING.md §14 og §14.1](IMPLEMENTERING.md) — HelseID og Helsenorge EksternAPI-erfaring, direkte overførbar til Gull-nivå
- [VEIKART.md](VEIKART.md) fase 2 — writeback-planen som overlapper med Gull-alternativ 1
- [navikt/smart-on-fhir](https://github.com/navikt/smart-on-fhir) og [navikt/smart-on-fhir-validator](https://github.com/navikt/smart-on-fhir-validator) — NAVs referansekode og OID-valideringslogikk
- [RISIKOREGISTER.md](RISIKOREGISTER.md) R1 (EPJ-modenhet), R4 (sikkerhetsgap), R9 (NHN-kontakt)
