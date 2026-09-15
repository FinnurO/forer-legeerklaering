# NHN-dokumentasjon — SMART App Launch + Førerrett-App

**Kilde:** Helsenorge Confluence (helsenorge.atlassian.net)  
**Hentet:** 2026-06-17 (§1, §3, §4), 2026-09-15 (§2 og HL7Norway/HelseAPI-vurderingen i §5)  
**Sider:**
- [Implementasjonsguide SMART App Launch Framework](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/67469415/Implementasjonsguide+SMART+App+Launch+Framework) (oppdatert 7. mai 2025)
- [Implementasjonsguide HelseAPI](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/67239937/Implementasjonsguide+HelseAPI) (oppdatert 10. januar 2020)
- [Smart-On-Fhir Førerrett-App](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/2846392337/Smart-On-Fhir+F+rerrett-App)
- [HL7Norway/HelseAPI](https://github.com/HL7Norway/HelseAPI) (GitHub — profilkildekoden bak HelseAPI-guiden)

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

## 2. Implementasjonsguide HelseAPI

### Formål og status

Guiden skal *"tydeliggjøre premisser for teknisk tilrettelegging av EPJ for FHIR"* — rettet mot teknisk personell, testere og prosjektledere hos EPJ-leverandører. Den utelukker eksplisitt drift/overvåkingsrutiner fra sitt omfang.

**Sist oppdatert 10. januar 2020** — over fem år eldre enn SMART App Launch-guiden i §1 (mai 2025), og forfatter er oppgitt som slettet bruker. Dette er et **stille** dokument, ikke et aktivt vedlikeholdt et, i motsetning til §1.

### FHIR-profiler nevnt i guiden

- HelseAPI Address
- HelseAPI Encounter
- HelseAPI Organization
- HelseAPI Patient
- HelseAPI Practitioner
- NO basis HumanName

**Merk avviket mot GitHub-repoet** (se §5): `HL7Norway/HelseAPI` sin `StructureDefinition/`-mappe inneholder i dag kun Address, DocumentReference, Patient og Practitioner — ingen Encounter eller Organization, men til gjengjeld en DocumentReference-profil som ikke er nevnt i Confluence-guiden. Guiden (2020) og kildekoden (siste push mai 2024) har med andre ord driftet fra hverandre — endnu et tegn på at HelseAPI-initiativet aldri har hatt en samlet, aktivt vedlikeholdt kilde.

### Kobling til SMART App Launch-guiden

Guidene er søsterdokumenter i samme Confluence-rom, og §1 lenker eksplisitt til denne (se «Governance»-punktet der). Men SMART-guiden (mai 2025) **bruker ikke** noen av HelseAPI-profilene i sine egne eksempler — den er generisk FHIR. Kobling mellom de to guidene finnes altså kun som en lenke, ikke som et innholdsmessig avhengighetsforhold.

### Relevans for `forer-legeerklaering`

Ingen av de seks profilene dekker noe vi mangler i dag — vi bruker allerede `Patient`/`Practitioner`/`Organization`/`Encounter` fra `no-basis` direkte (se `NorwegianHealthOids` og `SmartFhirPrefillClient` i `src/SmartFhir.Common`). Verdien av guiden er bekreftende, ikke retningsgivende: den viser at NHN/HL7 Norway så for seg akkurat denne typen profilering allerede i 2019–2020, men at arbeidet stanset opp før det ble noe mer enn et Confluence-dokument og et delvis GitHub-repo. Se §5 for vurdering av om det er noe å bygge videre på.

---

## 3. Smart-On-FHIR Førerrett-App (NHNs produksjonsapp)

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

## 4. Nøkkelobservasjoner for `forer-legeerklaering`-prosjektet

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

## 5. Vurdering: `HL7Norway/HelseAPI` — bygge videre på, eller bidra til?

**Spørsmål stilt 2026-09-15:** er GitHub-repoet bak HelseAPI-profilene (§2) noe `forer-legeerklaering` kan bygge på eller bidra til?

### Hva repoet er

[HL7Norway/HelseAPI](https://github.com/HL7Norway/HelseAPI) — opprettet 26.02.2019, ingen lisens, 6 stjerner/4 forks/5 åpne issues, **siste push 19.05.2024** (over to år gammelt regnet fra i dag). READMEen beskriver formålet som å utvikle standardiserte, åpne FHIR-profiler for norsk helsevesen, bygget på Argonaut/US Core + IPS/IPA-prinsipper oppå `no-basis` R4.

**Det påfallende funnet:** READMEen sier eksplisitt at *"the first use of HelseAPI profiles will be Førerrett-prosjektet"* — med SMART App Launch mot fastlege-EPJ for fornyelse av førerkort, og lenker til en avisartikkel om et tidlig digitaliseringsforsøk fra 2017. **Dette er nøyaktig samme brukstilfelle som `forer-legeerklaering` selv løser** — bare sett fra HL7 Norway-siden, sju år tidligere, og aldri fullført. Kontaktperson oppgitt: Espen Stranger Seland (HL7 Norway / Vali).

### Innhold

- `StructureDefinition/`: `no-helseapi-Address`, `-DocumentReference`, `-Patient`, `-Practitioner`
- `ValueSet/`: documentreference-category, documentreference-type
- `CodeSystem/`: documentreference-category

`no-helseapi-Practitioner` krever `Practitioner.identifier` (min=1) med en navngitt slice **"HPR"** (`mustSupport=true`) — **identisk konvensjon** med det `forer-legeerklaering` allerede bruker (`NorwegianHealthOids.Hpr` i `src/SmartFhir.Common`), bekreftet uavhengig på tvers av to prosjekter sju år fra hverandre. `no-helseapi-DocumentReference` (mustSupport på `identifier` og påkrevd, bundet `type`) er relevant for eventuell fremtidig writeback-funksjonalitet (VEIKART.md fase 2).

### Er det aktivt?

Nei. Fem åpne issues, ingen lukket siden 2020 bortsett fra diskusjon:
- **#15** («Børste støv av HelseAPI og få lagt den ut som en FHIR IG», feb. 2024, 7 kommentarer) — den eneste reelt innholdsrike tråden, se under.
- #16 (åpen PR, ikke merget), #13 (2020), #11 (2020), #9 (2019) — alle stille.

**Issue #15 sitt innhold er selve verdien i repoet akkurat nå.** Ekte HL7 Norway/Helsenorge-folk (rockphotog, kennethmyhra, thomiz, losolio, oaassv) diskuterer der — uten å konkludere — nøyaktig det samme spørsmålet `forer-legeerklaering` selv har måttet ta stilling til: **EHR-launched (scenario 4) vs. standalone/portal-launched (scenario 3, f.eks. fra Helseaktørportalen)** — det dette prosjektet kaller «dobbel inngangsmodus» (se [DOBBEL-INNGANGSMODUS.md](DOBBEL-INNGANGSMODUS.md)). `kennethmyhra` uttrykker usikkerhet om Helseaktørportalen i det hele tatt er SMART-kompatibel. `losolio` foreslår mot slutten en generalisering til «portal-smart-apper», som ligger tett opptil dette prosjektets eget STRATEGI.md-spor B («Altinn Health Integration Framework»), med referanse til Helsedirektoratets «portaloppdraget»-rapport. `kennethmyhra` lenker også en «fornorsket versjon» av en SMART App Launch-IG skrevet for scenario 4 spesifikt ([Confluence, bekreftet offentlig lesbar](https://helsenorge.atlassian.net/l/cp/J1efuXyY)) — innholdsmessig overlappende med §1 i dette dokumentet.

### Vurdering

**Ja, dette er troverdig prior art** som `forer-legeerklaering` bør forholde seg til — samme brukstilfelle, samme HPR-slice-konvensjon, og en stalled fagdiskusjon som allerede har identifisert de samme arkitektoniske spørsmålene dette prosjektet har måttet løse selv. **Nei, det er ikke noe å «bygge på» direkte i kodeteknisk forstand** — repoet er dormant (ingen push siden mai 2024), det finnes ingen publisert/versjonert FHIR Implementation Guide (kun løse `StructureDefinition`-filer og en stanset issue-diskusjon om nettopp å publisere en), og §2 over viser at selv Confluence-dokumentasjonen og GitHub-kildekoden har driftet fra hverandre.

**Anbefalt engasjementsvei — bidra, ikke bygg på:**
1. Kommentere på issue #15 med `forer-legeerklaering` som konkret, virkende bevis på at mønsteret de diskuterte i 2024 faktisk fungerer i praksis (EHR-launched SMART on FHIR, HPR-slice-konvensjon, testet mot flere EPJ-er) — dette er reell fremdrift på akkurat det tråden etterlyste («få lagt den ut som en FHIR IG»).
2. Vurdere å formelt style `ForerLegeerklaeringModel`/`KjeveortopediskHenvisningModel` sin FHIR-mapping mot `no-helseapi-Patient`/`-Practitioner`/`-DocumentReference` der de allerede overlapper (kravet om HPR-slice er jo identisk) — gir prosjektet legitimitet som *«implementerer en named, om enn stalled, norsk fagstandard»* fremfor en ad-hoc-tolkning av `no-basis`.
3. Ta direkte kontakt med Espen Stranger Seland og/eller `kennethmyhra` — de er navngitte, aktive fagpersoner i norsk HL7-miljø, og en henvendelse med en fungerende referanseimplementasjon er noe kvalitativt annet enn en kommentar på et gammelt issue.

Ingen av disse er kodeendringer i seg selv — de er forankrings-/kommunikasjonstiltak. Se også [VEIKART.md, Fase 4](VEIKART.md) for hvor en eventuell formell profilretting bør inn i den fremtidige `Digdir.SmartOnFhir`-pakken.
