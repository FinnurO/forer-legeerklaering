# Kartlegging — SMART on FHIR kandidater i norsk helsesektor

**Utarbeidet:** 2026-06-17  
**Kilder:** Direktoratet for e-helse HITR 1225:2019, NHN implementasjonsguide (2025), HL7 Norge, NAV GitHub, offentlig tilgjengelig dokumentasjon

---

## 1. Nasjonalt rammeverk og styringsgrunnlag

### Direktoratet for e-helse — Anbefaling SMART on FHIR (HITR 1225:2019)

Utgitt januar 2019, oppdatert merknad september 2024. Klassifisert som **Veiledning** (laveste normative nivå — under retningslinjer, anbefalte standarder og obligatoriske standarder).

> "Direktoratet for e-helse anser SMART on FHIR som et av de mest lovende nye rammeverkene for applikasjonsintegrasjon i EPJ som er tilgjengelig i dag, og anbefaler leverandører og andre aktører å ta i bruk dette rammeverket."

Dokumentet nevner eksplisitt to pilotprosjekter som grunnlag for anbefalingen:
- **Velferdsteknologiprogrammet**
- **Førerrettsprosjektet** — nøyaktig det domenet `forer-legeerklaering` PoC-en dekker

Sju betraktninger/forbehold i anbefalingen er fortsatt relevante for alle nye prosjekter:

| Nr | Betraktning | Status for `forer` |
|---|---|---|
| 1 | Fokuser på noen sentrale FHIR-ressurser først | ✔ Patient, Practitioner, Encounter, Condition |
| 2 | Godkjenning/sertifisering av apper er uavklart for norsk marked | Åpen (BESLUTNINGER.md C-6) |
| 3 | Tilpass til norske/nordiske FHIR-profiler (ikke bare US Argonaut) | Delvis — norske OID-er, men ikke no-basis-profiler fullt ut |
| 4 | Gjenbruk FHIR-grensesnitt utover SMART | Potensial via HelseAPI |
| 5 | Tilpass SMART til norsk grunnmur inkl. HelseID | HelseID-klient nå registrert (2026-06-17) |
| 6 | Del dokumentasjon og retningslinjer med andre prosjekter | ✔ Åpen kildekode, åpen dokumentasjon |
| 7 | Direktoratet vil vurdere nasjonale retningslinjer høyere i normskalaen | Under utarbeidelse (2024-merknad) |

### NHN Implementasjonsguide SMART App Launch Framework (2025)

Operasjonaliserer punktene over med konkrete krav: TLS, minimum 122-bit state-entropi, HTTP Basic auth for konfidensielle klienter, audit-logging, formell risikovurdering. Se [NHN-DOKUMENTASJON.md](NHN-DOKUMENTASJON.md).

### HL7 Norge

Peker til Direktorat-anbefalingen. Identifiserer behov for norsk arkitektur for SMART on FHIR, med Førerrettsprosjektet som piloten som skal produsere implementasjonsveiledning med nivå-inndeling (FHIR-ressurser/profiler).

---

## 2. EPJ-systemer som SMART App Launch-vertskap

For at SMART-apper skal fungere i klinisk hverdag, må EPJ-systemet støtte SMART App Launch som **launch host** — dvs. starte apper, overføre pasientkontekst og eksponere FHIR-API.

| EPJ-system | Markedssegment | SMART-støtte | Merknad |
|---|---|---|---|
| **DIPS Arena** | Sykehus (dominerende) | Ukjent / under utvikling | Brukes av de fleste norske helseforetak. FHIR-grensesnitt under utvikling. Høy strategisk prioritet. |
| **WebMed EPJ** | Fastleger / spesialister | Bekreftet pilotdeltaker | Navngitt i `syk-inn`-kodebasen som kjent FHIR-server (`client_secret_basic`). Deltok i Førerrettsprosjektet. |
| **CGM Journey** | Fastleger / spesialister | Ukjent | CGM er stor EPJ-leverandør for norske fastleger. |
| **Infodoc Plenario** | Fastleger | Ukjent | Betydelig markedsandel blant fastleger. |
| **Visma Flyt Helse** | Kommunal helse/omsorg | Ukjent | Brukes i kommunale tjenester — relevant for velferdsteknologi. |
| **Acos EPJ** | Kommunal pleie og omsorg | Ukjent | — |

**Nøkkelutfordring:** SMART-støtte i norske EPJ-systemer er fragmentert og drives av enkeltprosjekter (slik HITR 1225:2019 forutsier). Syk-inn og Førerrettsprosjektet er de to prosjektene som har drevet faktisk EPJ-integrasjon. Det finnes ingen nasjonal sertifiseringsordning eller app-butikk for norske EPJ-er.

---

## 3. Eksisterende SMART on FHIR-apper i Norge

### Produksjon

| App | Domene | Eier | Plattform | EPJ-integrasjon |
|---|---|---|---|---|
| **NHN Førerrett-App** | Legeerklæring IS-2569 → SVV | NHN | Helsenorge | WebMed (bekreftet), DIPS (antatt) |
| **NAV `syk-inn`** | Sykmelding → NAV | NAV | NAIS (egenutviklet) | WebMed (bekreftet), DIPS (under arbeid) |

### PoC / pilot

| App | Domene | Eier | Status |
|---|---|---|---|
| **Digdir `forer-legeerklaering`** | Legeerklæring IS-2569 → Altinn | Digdir | Funksjonell lokal PoC |

### Infrastruktur og biblioteker

| Komponent | Eier | Formål |
|---|---|---|
| `@navikt/smart-on-fhir` | NAV | Gjenbrukbar SMART-klient for Next.js/TypeScript |
| NAV SMART on FHIR validator | NAV | Samsvarskontroll for EPJ-leverandører |
| NAV SMART on FHIR example | NAV | Referanseimplementasjon for EPJ-leverandører |
| NHN SMART App Launch implementasjonsguide | NHN | Krav og veiledning for EPJ-leverandører og app-utviklere |

---

## 4. Kandidater for nye SMART on FHIR-apper

Kriteriene for en god SMART on FHIR-kandidat (fra HITR 1225:2019 og erfaring):
1. **Datakilde i EPJ** — legen trenger pasientdata fra journalen for å fylle ut
2. **Mottaker utenfor EPJ** — skjemaet sendes til en offentlig etat eller annen aktør
3. **Komplekst skjema** — tilstrekkelig kompleksitet til at prefill gir reell verdi
4. **Eksisterende papir/manuell flyt** — tydelig effektiviseringsgevinst

### Kategori A: Legeerklæringer og attester til offentlige etater

| Skjema | Mottaker | Hjemmel | FHIR-ressurser | SMART-status |
|---|---|---|---|---|
| **Helseattest førerrett IS-2569** | Statens vegvesen | Vegtrafikkloven | Patient, Condition, Encounter | ✔ I produksjon (NHN) / PoC (Digdir) |
| **Sykmelding (NAV 08-07.04)** | NAV | Folketrygdloven | Patient, Condition, Encounter, Practitioner | ✔ I produksjon (syk-inn) |
| **Helseattest for sjøfolk** | Sjøfartsdirektoratet (NMA) | Skipssikkerhetsloven | Patient, Condition | Kandidat — høy prioritet |
| **Helseattest for flymannskap (JAR-FCL)** | Luftfartstilsynet (CAA) | Luftfartsloven | Patient, Condition | Kandidat — EU-regulert (EASA) |
| **Helseattest for dykkere** | Arbeidstilsynet | Dykkerforskriften | Patient, Condition | Kandidat |
| **Legeerklæring arbeidsavklaringspenger (AAP)** | NAV | Folketrygdloven kap. 11 | Patient, Condition, Encounter | Kandidat — volum |
| **Legeerklæring uføretrygd** | NAV | Folketrygdloven kap. 12 | Patient, Condition | Kandidat — volum |
| **Legeerklæring yrkesskade** | NAV | Yrkesskadeforsikringsloven | Patient, Condition, Procedure | Kandidat |
| **Legeattest for tvangsinnleggelse** | Kommunen / domstol | Psykisk helsevernloven | Patient, Condition | Sensitiv — høy kompleksitet |

### Kategori B: Henvisninger og epikriser

| Skjema | Mottaker | FHIR-ressurser | SMART-status |
|---|---|---|---|
| **Elektronisk henvisning** | Spesialisthelsetjenesten | Patient, ServiceRequest, Condition | Delvis — eksisterer via Helsenettet/EDI men ikke SMART |
| **Epikrise** | Fastlege / annen behandler | Patient, Encounter, Condition, Procedure | FHIR-profil finnes (no-basis-Epikrise) — ingen SMART-app kjent |
| **Røntgen-/lab-rekvisisjon** | Røntgen / laboratorium | Patient, ServiceRequest | Vurdert — eksisterende e-rekvisisjon dekker behovet |

### Kategori C: Klinisk beslutningsstøtte

Nevnt eksplisitt i HITR 1225:2019 som en av de mest naturlige bruks­casene:

| App-type | Eksempel | FHIR-ressurser | Merknad |
|---|---|---|---|
| **Vekstkurver** | Visualisering av barnets vekst mot normkurver | Observation, Patient | Nevnt i Direktoratets anbefaling |
| **Risikokalkulatorer** | Kardiovaskulær risiko (NORRISK), CHA₂DS₂-VASc, FRAX | Observation, Condition, MedicationStatement | Høy verdi, relativt enkelt å implementere |
| **Diagnoseverktøy** | KOLS-staging, Wells-skår, GFR-kalkulatorer | Observation, Condition | Read-only — ingen writeback nødvendig |
| **KOLS/astma miljødataintegrasjon** | Korrelasjon pollenmålinger/luftkvalitet mot symptomer | Observation + ekstern API | Nevnt i Direktoratets anbefaling |

### Kategori D: Velferdsteknologi og kommunal helse

Nevnt i Direktoratets anbefaling (Velferdsteknologiprogrammet som pilotprosjekt):

| Brukstilfelle | FHIR-ressurser | Status |
|---|---|---|
| Trygghetspakke-monitorering i EPJ | Observation, Device | Under utredning — kommunale EPJ-er mangler SMART-støtte |
| Koordineringsplan (individuell plan) | CarePlan, Patient | Kompleks aktørstruktur |
| Hjemmetjeneste-dokumentasjon | Encounter, Observation | Kommunalt EPJ-marked fragmentert |

### Kategori E: Forskning og registre

| Brukstilfelle | Aktør | SMART-støtte |
|---|---|---|
| Kreftregisteret — innmelding direkte fra EPJ | Kreftregisteret | Ingen kjent SMART-implementasjon |
| Norsk pasientregister (NPR) — aktivitetsdata | Helsedirektoratet | Eksisterende EDI/XML-flyt |
| Medisinske kvalitetsregistre | Varierer (NorCAN, NorGast, etc.) | Ingen kjent SMART-tilnærming |
| Kliniske studier — eCRF-integrasjon | Forskningsmiljøer | Internasjonalt: ResearchKit, REDCap — ingen norsk standard |

---

## 5. Prioriteringsvurdering

Basert på kombinasjonen av: **datakilde i EPJ** × **klar mottaker** × **eksisterende papirflyt** × **volum/frekvens** × **ingen eksisterende digital løsning**:

### Høy prioritet (anbefalt neste kandidat)

| App | Begrunnelse |
|---|---|
| **Helseattest for sjøfolk** | ~60 000 norske sjøfolk, obligatorisk annen hvert år, mottaker er Sjøfartsdirektoratet med etablert API (NMA). Lignende domene som IS-2569 — minimal ny arkitektur. |
| **Helseattest for flymannskap** | Regulert av EASA (EU), potensielt pan-europeisk gjenbruk. Mottaker er Luftfartstilsynet. |
| **Kliniske risikokalkulatorer** | Lavest implementasjonskompleksitet — read-only FHIR, ingen writeback, ingen mottaker-integrasjon. Rask vei til synlig verdi i EPJ (jf. betraktning 1 i HITR 1225:2019). |

### Middels prioritet

| App | Blokkerende avhengigheter |
|---|---|
| **AAP-legeerklæring** | Avhenger av NAV API-tilgang og databehandleravtale |
| **Elektronisk henvisning** | Konkurrerer med eksisterende Helsenettet-infrastruktur |
| **Uføretrygd-legeerklæring** | NAV har allerede ressurser til å bygge dette (jf. syk-inn) |

### Lavere prioritet (høy kompleksitet eller blokkert)

| App | Årsak |
|---|---|
| **Epikrise** | Eksisterer via Helsenettet EDI — SMART ville kreve full EPJ-omlegging |
| **Tvangsinnleggelse** | Juridisk/etisk kompleksitet, sensitiv data, domstolsinvolvering |
| **Kreftregisteret** | Egen infrastruktur, ikke erstattet av SMART |

---

## 6. EPJ-leverandørstrategi — hva som trengs

For at kandidatappene i avsnitt 4 skal realiseres, kreves:

1. **FHIR-endepunkt i EPJ** — minst Patient, Practitioner, Organization, Encounter, Condition
2. **SMART App Launch-støtte** — `.well-known/smart-configuration`, autorisasjonsendepunkt, token-endepunkt
3. **Avtaleverk** — EPJ-leverandør ↔ app-leverandør ↔ helsevirksomhet (ingen nasjonal standard for dette per 2026)
4. **Norske FHIR-profiler** — HL7 Norge no-basis-profiler (Patient, Practitioner, Organization, Encounter)
5. **HelseID-integrasjon** — NHN tillitsrammeverk-claims for behandlingsformål

**Manglende ledd:** Det finnes ingen norsk «app store» eller sertifiseringsordning for SMART-apper på norske EPJ-er. Dette er eksplisitt uavklart i HITR 1225:2019 (betraktning 2) og er fortsatt åpent i 2026.

---

## 7. Internasjonale referanser

| Ressurs | Relevans |
|---|---|
| **SMART Health IT App Gallery** (smarthealthit.org) | Internasjonal katalog over SMART-apper — referanseimplementasjoner |
| **Epic App Orchard** | USAs største EPJ har ~1 000+ SMART-apper. Viser hva som er mulig når plattformen er moden. |
| **HL7 SMART App Launch IG v2.2.0** | Gjeldende standard — det vi implementerer |
| **IPA (International Patient Access)** | HL7 FHIR-profil for internasjonal pasienttilgang — relevant for norsk e-helse grunnmur |
| **Cerner/Oracle Health** og **Azure Health Data Services** | Tilbyr SMART-kompatible FHIR-endepunkter — potensielt relevant for norske sykehus |

---

## 8. Konklusjon

Norge er i en tidlig, men strategisk viktig fase for SMART on FHIR-adopsjonen:

- **To produksjonsapper** er i drift (syk-inn og NHN Førerrett-App), begge i legens EPJ-kontekst
- **Direktoratet for e-helse har anbefalt** rammeverket siden 2019, med Førerrettsprosjektet som eksplisitt pilot
- **EPJ-leverandørstøtte er den kritiske flaskehalsen** — uten FHIR-API og SMART App Launch-støtte i EPJ-ene kan ingen av kandidatappene realiseres
- **De mest modne kandidatene** for neste implementasjon er helseattest for sjøfolk og flymannskap (samme domene og arkitektur som IS-2569, klare mottakere) og kliniske risikokalkulatorer (read-only, minimal kompleksitet)
- **Digdir `forer-legeerklaering`** sin viktigste strategiske verdi fremover er å bidra til referansearkitektur og åpen dokumentasjon som hjelper EPJ-leverandørene med å bygge SMART-støtte — i tråd med betraktning 6 i HITR 1225:2019

---

## Referanser

- [HITR 1225:2019 — Anbefaling om bruk av SMART on FHIR](https://www.helsedirektoratet.no/faglige-rad/anbefaling-om-bruk-av-smart-on-fhir) (Helsedirektoratet, jan 2019, merknad sep 2024)
- [NHN Implementasjonsguide SMART App Launch Framework](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/67469415/) (NHN, mai 2025)
- [NHN Smart-On-Fhir Førerrett-App](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/2846392337/) (NHN)
- [HL7 Norge — SMART on FHIR best practice](https://hl7norway.github.io/best-practice/docs/IG-og-dokumentasjon/smart.html)
- [NAV `syk-inn`](https://github.com/navikt/syk-inn) (NAV, produksjon)
- [HL7 FHIR SMART App Launch IG v2.2.0](https://hl7.org/fhir/smart-app-launch/)
