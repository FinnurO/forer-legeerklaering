# Helhetlig flyt: Pasient → Helsenorge → EPJ/Altinn → SVV

**Dato:** 2026-08-11
**Formål:** Binde sammen de fire aktørene som til nå har vært beskrevet i separate dokumenter — Altinn Studio-appen, Helsenorge.no, EPJ-systemet og Statens vegvesen (SVV) — i én sammenhengende beskrivelse. Ingen enkelt eksisterende dokument viser hele reisen; se «Kilder for hvert steg» nederst for hvor detaljene faktisk står.

---

## De fire aktørene

| Aktør | Rolle i flyten | Eies/driftes av |
|---|---|---|
| **Pasient (Helsenorge.no)** | Fyller ut egenerklæring om helse (NA-0201) før konsultasjon | Innbygger, via Helsenorge-plattformen (NHN) |
| **EPJ (fastlegesystem)** | Legens journalsystem — starter SMART-launch, tilbyr FHIR-data | Fastlegekontoret, EPJ-leverandør (CGM/Infodoc/WebMed) |
| **Altinn Studio-app** (`forer-legeerklaering`) | BFF som henter FHIR-data, prefiller IS-2569, styrer signering og innsending | Digdir (denne PoC-en) |
| **Statens vegvesen (SVV) / Helsedirektoratet** | Mottar konklusjonen om skikkethet for førerkort | Statens vegvesen (førerkortregisteret) |

---

## Helhetlig sekvens

```
Pasient (Helsenorge.no)      EPJ (fastlege)         Altinn Studio-app (BFF)      SVV / Helsedirektoratet
        │                          │                          │                          │
        │ 1. Fyller ut             │                          │                          │
        │ egenerklæring            │                          │                          │
        │ (Oppgave+Skjema,         │                          │                          │
        │ eller Dialogporten)      │                          │                          │
        │─────────────────────────►│ (synkes til EPJ,         │                          │
        │  [PLANLAGT — ikke        │  evt. hentes direkte     │                          │
        │  implementert i PoC]     │  av Altinn-appen)        │                          │
        │                          │                          │                          │
        │                          │ 2. Lege starter          │                          │
        │                          │ konsultasjon,             │                          │
        │                          │ SMART EHR Launch          │                          │
        │                          │─────────────────────────►│                          │
        │                          │  [VERIFISERT — SmartLaunchController]                │
        │                          │                          │                          │
        │                          │◄─────────────────────────│ 3. Henter FHIR-data       │
        │                          │  Patient, Practitioner,   │  (Patient/Practitioner/   │
        │                          │  Organization, Encounter,  │  Organization/Encounter/  │
        │                          │  Condition, evt.           │  Condition)               │
        │                          │  QuestionnaireResponse     │  [VERIFISERT — FhirPrefillService]
        │                          │  (egenerklæring)           │  [Egenerklæring-henting: PLANLAGT]
        │                          │                          │                          │
        │                          │                          │ 4. Prefiller IS-2569,     │
        │                          │                          │ legen kontrollerer,       │
        │                          │                          │ supplerer, signerer       │
        │                          │                          │ («Signer og send inn»)    │
        │                          │                          │  [VERIFISERT — process.bpmn Task_1]
        │                          │                          │                          │
        │                          │◄─────────────────────────│ 5a. Full attest skrives   │
        │                          │  DocumentReference         │  tilbake til EPJ          │
        │                          │  [PLANLAGT — VEIKART.md   │                          │
        │                          │  fase 2, ikke kodet]      │                          │
        │                          │                          │                          │
        │                          │                          │ 5b. Konklusjon (grønt/    │
        │                          │                          │ rødt per kjøretøygruppe) │
        │                          │                          │──────────────────────────►│
        │                          │                          │  Altinn Events →          │
        │                          │                          │  SVV henter selv via      │
        │                          │                          │  Maskinporten             │
        │                          │                          │  [MODELL VERIFISERT —     │
        │                          │                          │  ForerKonklusjonModel;    │
        │                          │                          │  SVV-abonnement PLANLAGT] │
```

**Steg 1** kan alternativt gjøres via to forskjellige spor — se [PASIENTFLYT.md](PASIENTFLYT.md) for begge:
- **Alternativ A:** Dialogporten-dialog vist på helsenorge.no, egenerklæring fylt ut i en egen Altinn-app.
- **Alternativ B:** Helsenorge EksternAPI (Oppgave + Skjema) — teknisk autentisering og selve API-strukturen er verifisert (se [IMPLEMENTERING.md §14.1](IMPLEMENTERING.md)), men selve skjemaoppgaven er ikke bygget, og videre testing er blokkert på formell NHN-kontakt (se [BESLUTNINGER.md C-6](BESLUTNINGER.md)).

---

## Status: hva er faktisk verifisert vs. planlagt

| # | Steg | Status | Kilde |
|---|---|---|---|
| 1 | Pasient fyller ut egenerklæring via Helsenorge | ❌ Ikke implementert — kun arkitekturforslag (to alternativer) | [PASIENTFLYT.md](PASIENTFLYT.md) |
| 2 | EPJ → Altinn: SMART EHR Launch | ✅ Verifisert (mock lokalt; `/smart/dev-login`-workaround pga. `ERR_TOO_MANY_REDIRECTS` i full flyt) | [SmartLaunchController.cs](../src/App/controllers/SmartLaunchController.cs), [RISIKOREGISTER.md R8](RISIKOREGISTER.md) |
| 3 | Altinn henter FHIR-data fra EPJ | ✅ Verifisert (mot lokal HAPI FHIR-mock, ikke reell fastlege-EPJ) | [FhirPrefillService.cs](../src/App/services/FhirPrefillService.cs), [RISIKOREGISTER.md R1](RISIKOREGISTER.md) |
| 3b | Altinn henter pasientens egenerklæring (QuestionnaireResponse) | ❌ Ikke implementert — avhenger av steg 1 | [PASIENTFLYT.md §3](PASIENTFLYT.md) |
| 4 | Lege fyller ut, signerer, sender inn | ✅ Verifisert («Signer og send inn», Task_1) | [process.bpmn](../src/App/config/process/process.bpmn) |
| 5a | Full attest skrives tilbake til EPJ | ❌ Ikke implementert | [VEIKART.md fase 2](VEIKART.md) |
| 5b | Konklusjon (grønt/rødt) → SVV via Altinn Events | ⚠️ Datamodell verifisert (`ForerKonklusjonModel`), selve Events-abonnementet hos SVV er ikke avtalt | [BESLUTNINGER.md C-3](BESLUTNINGER.md) |
| — | Helsenorge EksternAPI-autentisering (Oppgave/Skjema) | ✅ Verifisert mot ekte NHN-testmiljø, men videre arbeid blokkert på NHN-kontakt | [IMPLEMENTERING.md §14.1](IMPLEMENTERING.md), [RISIKOREGISTER.md R9](RISIKOREGISTER.md) |

**Lesbar oppsummering:** Den *midtre* delen av flyten (steg 2–4, lege ↔ Altinn-app) er den best verifiserte delen av hele PoC-en. Det som skjer *før* (pasientens egenerklæring) og *etter* (writeback til EPJ, faktisk mottak hos SVV) konsultasjonen er i stor grad arkitektur og datamodeller — ikke virkende integrasjoner.

---

## Kilder for hvert steg

| Steg i flyten | Detaljert beskrivelse |
|---|---|
| Pasient/Helsenorge | [PASIENTFLYT.md](PASIENTFLYT.md) — begge alternativer |
| Helsenorge EksternAPI teknisk | [IMPLEMENTERING.md §14.1](IMPLEMENTERING.md), [local-dev/helseid-token-test/](../local-dev/helseid-token-test/), [local-dev/helsenorge-oppgave-test/](../local-dev/helsenorge-oppgave-test/) |
| EPJ ↔ Altinn (SMART launch) | [KRAVSPESIFIKASJON-v0.6.md §4](KRAVSPESIFIKASJON-v0.6.md), [arkitektur-oversikt.svg](arkitektur-oversikt.svg), [smart-launch-sekvens.svg](smart-launch-sekvens.svg) |
| Signering | [IMPLEMENTERING.md](IMPLEMENTERING.md) — Altinn signing task-avsnittet |
| Altinn ↔ SVV (mottak) | [BESLUTNINGER.md C-3](BESLUTNINGER.md) — Altinn Events, FINT Arkiv-mønster, to-lags datamodell |
| Altinn ↔ EPJ (writeback) | [VEIKART.md fase 2](VEIKART.md) |
| Lokalt utviklingsmiljø (hvordan spinne opp alt) | [README.md «Kom i gang»](../README.md) |

---

## Hvordan spinne opp hele miljøet lokalt

Bekreftet fungerende 2026-08-11 (se README.md for fullstendige steg). Fire prosesser må kjøre samtidig:

| Komponent | Port | Kommando |
|---|---|---|
| Altinn Platform (localtest) + PDF + loadbalancer | 5101, 5300, 8000 | Podman/Podman Desktop, `app-localtest/docker-compose.yml` (separat repo, se README «Standarder og referanser») |
| HAPI FHIR (EPJ-mock) | 8080 | Del av samme compose-oppsett |
| SMART Auth Mock | 9090 | `cd local-dev/smart-mock && node server.js` |
| Altinn-appen (`forer-legeerklaering`) | 5005 | `cd src/App && dotnet run` |

**Kjent fallgruve:** Altinn localtest-plattformen (port 5101/8000) kan henge/ikke svare hvis den startes *før* Altinn-appen selv er oppe på port 5005 — den ser ut til å ha en avhengighet til appen ved oppstart av forespørsler. Start i denne rekkefølgen: containere → SMART mock → Altinn-appen, og gi containerne litt tid før du tester.

Etter at alt kjører: åpne `http://localhost:9090/epj` for EPJ-simulatoren (anbefalt startpunkt for demo), eller gå direkte til `http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/dev-login` for hurtigstart uten UI.
