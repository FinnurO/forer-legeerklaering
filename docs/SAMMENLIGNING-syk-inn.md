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
