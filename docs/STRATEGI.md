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

## Åpne strategiske spørsmål

Se [BESLUTNINGER.md](BESLUTNINGER.md) for alle åpne beslutninger. De mest strategisk kritiske:

| ID | Spørsmål | Blokkerer |
|---|---|---|
| C-6 | Altinn Studio vs. Helsenorge-plattformen — rollefordeling | Fase 3 |
| C-3 | Mottaksarkitektur — hvem er tjenesteeier? | Fase 3 |
| C-4 | Rettslig grunnlag og DPIA | Fase 3 |
| C-7 | DSOP + SMART on FHIR mot privat sektor | Fase 4 |
