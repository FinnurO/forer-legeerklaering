# Sammenligning: `forer-legeerklaering` vs. `syk-inn`

To SMART on FHIR-apper for norsk helsesektor, lest i sin helhet og vurdert som arkitektur, ikke som to løsrevne innsendinger. I prinsippet løser de samme problem: en behandler er innlogget i sin EPJ, EPJ starter en ekstern app via SMART App Launch, appen henter kontekst fra FHIR, behandleren fyller ut et helseskjema, og skjemaet sendes videre. Forskjellen ligger i modenhet og i hvilke valg som er tatt på hvert lag.

---

## 1. Kort oppsummering

| | `forer-legeerklaering` | `syk-inn` |
|---|---|---|
| Domene | Legeerklæring førerrett (blankett IS-2569) | Sykmelding til NAV |
| Eier | Digitaliseringsdirektoratet (Digdir) | NAV (navikt / team tsm) |
| Plattform | Altinn Studio-app, ASP.NET Core / .NET 8 | Next.js 16 / React 19 / TypeScript |
| Erklært status | «PoC gjennomført» | Produksjon (kjører på `nav.no/samarbeidspartner/sykmelding`) |
| Kodeomfang (app) | ~1 controller + 1 service + 1 modell + Altinn-config | ~456 TS/TSX-filer, ~33 700 linjer |
| SMART-implementasjon | Håndskrevet i én controller | Egen publisert pakke `@navikt/smart-on-fhir@1.5.25` |
| Skriver tilbake til EPJ | Nei (dokumentert som «planlagt») | Ja (DocumentReference + QuestionnaireResponse) |
| Automatiserte tester | Ingen (kun `TestDummy.cs`) | 23 unit/integrasjon + 24 Playwright e2e-speser |
| Dokumentasjon | ~13 500 ord fordelt på 5 dokumenter | ADR-er, FHIR-docs, sekvensdiagrammer, fsh-profiler |

Begge er reelle, kompetente arbeider. De er bare på helt ulike steder i livsløpet: `forer` er en velbeskrevet *idé-validering* med ett tynt, men korrekt, vertikalt snitt; `syk-inn` er et *driftssatt produkt* der det vanskelige (refresh, writeback, regelmotor, to launch-kontekster, observability) er løst.

---

## 2. Hva appene faktisk gjør

### `forer-legeerklaering`
EPJ → SMART EHR Launch → Altinn-app (som BFF) → prefyller skjema fra FHIR → lege kontrollerer → signering/arkiv via Altinn Platform. Kjernepoenget er å vise at **Altinn Studio kan brukes som skjemaplattform for helsedokumentasjon**, og at FHIR-prefill fungerer. Skjemaet (`Side1.json`) har 23 komponenter: pasient-, lege-, virksomhets-, konsultasjons- og diagnosefelt prefylt fra FHIR, pluss fire felt legen fyller ut selv (kjøretøygruppe, skikket ja/nei, vilkår, merknad).

### `syk-inn`
EPJ → SMART Launch **eller** HelseID-innlogging direkte (kalt «standalone») → dashboard → flertrinns sykmeldingsskjema → regelvalidering i `syk-inn-api` (NAVs regelmotor) → publisering inn i NAVs vanlige sykmeldingsflyt → PDF + strukturert writeback til EPJ. Skjemaet er rikt: diagnose/bidiagnose med ICD-10/ICPC-2-søk, flere perioder med aktivitetstype og grad, tilbakedatering, arbeidsgiver via Aa-registeret, utdypende spørsmål, annen fraværsgrunn, behandlingsdager-variant, kladd (draft), forleng og dupliser.

---

## 3. SMART on FHIR — selve kjernen

Dette er det mest interessante sammenligningspunktet, siden begge implementerer SMART App Launch IG og BFF-mønsteret «token forlater aldri nettleseren».

### Felles, korrekte valg
Begge gjør server-side authorization code-flyt med PKCE (S256), `state` mot CSRF, well-known/`smart-configuration`-discovery, og lagrer token server-side. Begge bruker `aud` = FHIR-base (iss), riktige `launch/patient`-scopes, og samme nasjonale OID-er for identifikatorer (fnr `2.16.578.1.12.4.1.4.1`, HPR `2.16.578.1.12.4.1.4.4`, orgnr `2.16.578.1.12.4.1.4.101`). De treffer altså samme nasjonale infrastruktur og standard.

### Der de skiller lag

**`forer` skriver SMART-klienten selv** i `SmartLaunchController.cs`: PKCE-generering, state, discovery, token-exchange, og lagring i `HttpContext.Session` med `IMemoryCache` som fallback. Det er pedagogisk lesbart og spec-tro, men:

- **Ingen tokenvalidering.** BFF stoler på access token uten signaturkontroll (erkjent i `BESLUTNINGER.md` C-2).
- **Ingen refresh-håndtering** selv om `offline_access` etterspørres.
- **Issuer-allowlist er tom**, og logikken `fail-open i dev / fail-closed i prod` er riktig prinsipp, men allowlisten må fylles før produksjon (erkjent i README som «Konfig-gap»).
- **Sesjonen er ikke distribuert** — `AddDistributedMemoryCache()` er in-memory, så flere pod-er deler ikke sesjon. OK for PoC, ikke for HA.
- En kjent, uløst feil: full OAuth-redirect gir `ERR_TOO_MANY_REDIRECTS`, og workaround er `/smart/dev-login`-endepunktet som hopper over OAuth i utvikling.

**`syk-inn` har trukket SMART-klienten ut i en egen versjonert pakke** (`@navikt/smart-on-fhir`). Launch/callback-rutene blir tynne: de kaller `getSmartClient(...).launch()` / `.callback()` / `.ready()`. Klienten gir:

- **`autoRefresh`** (på i prod, av i lokal/e2e/demo),
- **`enableMultiLaunch`** — flere pasientkontekster i samme sesjon,
- **`.validate()`** på token i `ready-client.ts`,
- **token-lagring i Valkey** (distribuert, med 30 dagers TTL knyttet til refresh-token-levetid),
- en allowlist av kjente FHIR-servere per miljø (`getKnownProdFhirServers()` → WebMed med `client_secret_basic`).

Konsekvens: `syk-inn` skiller *protokoll* (gjenbrukbar pakke) fra *applikasjon*. Det er den modne varianten av nøyaktig det `forer` har skrevet for hånd. `forer`s implementasjon er ikke feil — den er bare på det stadiet før man ekstraherer den til et bibliotek og legger på refresh/validering/distribuert lagring.

---

## 4. Arkitektur og det viktigste designmønsteret

### `forer`: Altinn-rammeverket gjør tunge løft
Ved å være en Altinn-app arver `forer` gratis: autentisering (ID-porten/Altinn), lagring, PDF-generering, signering (BPMN `Task_1` med `signing`-task), arkiv og XACML-autorisasjon (`policy.xml`). Prefill skjer via `IDataProcessor.ProcessDataRead`, som er det idiomatiske Altinn-grepet. Hele appen er i praksis: «en SMART-controller + en data-processor + en modell + skjema-JSON». Det er en bevisst og smart strategi — la plattformen ta infrastruktur, fokuser PoC-en på integrasjonspunktet.

### `syk-inn`: GraphQL som kontekst-abstraksjon
Det arkitektoniske kronjuvelet er at **skjemaet ikke kjenner sin egen launch-kontekst**. Både FHIR-modus og HelseID-standalone-modus implementerer *samme* GraphQL-schema (`root.graphqls`: `behandler`, `pasient`, `konsultasjon`, `opprettSykmelding`, `draft`…), men med hver sin resolver-kontekst (`createFhirResolverContext` vs. HelseID-varianten) og hver sin auth. Skjemakomponentene snakker bare GraphQL og bryr seg ikke om hvor dataene kommer fra.

Rundt dette ligger et gjennomført, lagdelt oppsett: Redux for skjema-state, react-hook-form for input, en streng typekjede fra GraphQL-input via Zod-skjema til `syk-inn-api`-payload. Mock-motoren (`src/dev/mock-engine`) lar hele flyten kjøre uten ekte backend, og en innebygd `fhir-mock`- og `helseid-mock`-server simulerer EPJ og HelseID i dev/e2e.

**Vurdering:** `forer` outsourcer kompleksitet til Altinn; `syk-inn` eier kompleksiteten selv, men har bygget abstraksjonene som gjør at den ikke kollapser under egen vekt. Begge er forsvarlige — den ene optimaliserer for tid-til-PoC, den andre for langsiktig kontroll og to brukerkontekster.

---

## 5. Skriving tilbake til EPJ (writeback)

Her er gapet størst, og det er lærerikt.

`forer` lister writeback som **ikke implementert** (se `BESLUTNINGER.md` C-3 og `VEIKART.md` fase 2). Resultatet ender i Altinn storage hos Digdir, og mottaksarkitekturen er en åpen beslutning.

`syk-inn` har en **gjennomarbeidet ADR (ADR01)** og faktisk implementasjon (`fhir-write-service.ts`):

- Skriver en **standalone `QuestionnaireResponse`** med all strukturert sykmeldingsdata som `item[]`, og en **`DocumentReference`** med PDF-en, lenket til QR via `context.related`.
- Bruker en **transaction Bundle med PUT** og **klient-tildelt id** (`DocumentReference.id = sykmeldingId`) for sporbarhet og idempotens, med en `GET`-sjekk før skriving for å unngå duplikater.
- Publiserer en **offentlig `Questionnaire`-definisjon** på en kanonisk URL (`/fhir/R4/Questionnaire/V1`, servert `force-static`) som QR-en refererer til.
- ADR-en drøfter og *forkaster* eksplisitt alternativer (contained QR, base64-kodet QR, Bundle+Composition, egne Condition/Organization-ressurser, `no-basis-Sykmelding`, Observation) med FHIR-faglig begrunnelse.

`syk-inn` er i praksis en fungerende referanse for det `forer` har planlagt i fase 2 av VEIKART.md.

---

## 6. Autentisering og autorisasjon

**`forer`** har to lag som er holdt fra hverandre i kravspeken: behandlerens innlogging (Altinn/ID-porten i dag, HelseID planlagt) og FHIR-autorisasjonen (SMART access token). Autorisasjon for selve Altinn-instansen styres av XACML i `policy.xml` (roller PRIV/DAGL, auth-nivå 2/3). HelseID-validering er dokumentert i detalj (`IMPLEMENTERING.md §14`, `BESLUTNINGER.md` C-2) men ikke kodet — blokkert av klientregistrering hos NHN.

**`syk-inn`** kjører bak **Wonderwall** (NAVs sidecar-reverse-proxy) som logger inn alle stier unntatt `/fhir/**` via HelseID og injiserer access token som `Authorization`-header. I FHIR-modus brukes SMART-token; i standalone leses HPR-nummer fra HelseID-id-token (`helseid://claims/hpr/hpr_number`). Det finnes til og med en «shadow»-validering av HelseID-on-FHIR-claim (`shadowVerifyHelseIdOnFhir`) som logger uten å blokkere — en pen måte å rulle ut ny validering på.

`syk-inn` har altså løst HelseID-integrasjonen som `forer` har dokumentert men utsatt til fase 5.

---

## 7. Datamodell og kodeverk

Begge mapper de samme nasjonale identifikatorene fra FHIR (leter i `identifier[]` etter `urn:oid:<system>`):

- `forer`: én privat `GetIdentifier(resource, system)`-helper i `FhirPrefillService`.
- `syk-inn`: `userUrnToOidType`, `getHpr`, `getValidPatientIdent` (med fnr→dnr-prioritering), `getOrganisasjonsnummerFromFhir`.

Diagnosehåndteringen viser modenhetsforskjellen: `forer` tar første coding fra siste aktive Condition og lagrer kode + tekst rått. `syk-inn` bruker `@navikt/tsm-diagnoser` (ICD-10/ICPC-2/ICPC-2B-kodeverk), partisjonerer gyldige/ugyldige diagnoser, og kjører fuzzy-analyse (Fuse.js) som logger avvik mellom EPJ-ens diagnosetekst og det offisielle kodeverket.

Datamodellen i `forer` (`ForerLegeerklaeringModel`) er flat og dekker bevisst bare en liten del av IS-2569; `SKJEMA-IS2569.md` og `BESLUTNINGER.md` C-5 dokumenterer de manglende 17 helsekategoriene.

---

## 8. Sikkerhet

| Tema | `forer` | `syk-inn` |
|---|---|---|
| Token forlater nettleser | Nei (BFF) ✔ | Nei (BFF) ✔ |
| PKCE + state | Ja ✔ | Ja (via pakke) ✔ |
| Tokenvalidering (signatur) | **Nei** (erkjent gap) | Ja (`.validate()` + HelseID-claim) |
| Refresh token | Etterspørres, ikke håndtert | `autoRefresh` ✔ |
| Issuer-allowlist | **Tom** (dev fail-open) | Kjente servere per miljø ✔ |
| Sesjonslagring | In-memory (ikke distribuert) | Valkey (distribuert, TTL) ✔ |
| Cookies | HttpOnly, SameSite=Lax, Secure i prod ✔ | HttpOnly, distribuert sesjon-id ✔ |
| Audit/sporbarhet | Beskrevet, ikke implementert | OTel-spans + strukturert logging ✔ |

`forer` har riktige *instinkter* (BFF, PKCE, fail-closed-prinsipp, HttpOnly) og er ærlig om hva som mangler. Ingen av appene har et reelt sikkerhetsmessig feilgrep; forskjellen er «dokumentert som todo» vs. «implementert».

---

## 9. Testing og kvalitet

`forer`: ingen automatiserte tester (kun en tom `TestDummy.cs`; CI kjører `dotnet build`/`test` men det finnes ingen testprosjekt). Akseptabelt for PoC, men null regresjonsvern.

`syk-inn`: 23 unit/integrasjon-tester (vitest, inkl. testcontainers for Kafka/Postgres mot ekte `syk-inn-api`), 24 Playwright e2e-spesifikasjoner som dekker FHIR-, standalone- og multi-mode-flyt (launch, draft, dupliser, forleng, regelvalidering, tilbakedatering, chaosmonkey, multi-user), pluss `knip` (død kode), `eslint`, `oxfmt`, og en «debt»-rapport i CI.

---

## 10. Drift, deployment og observability

`forer`: Dockerfile (.NET 8 alpine, non-root) + Helm-chart for Altinn-plattformen. Lokal utvikling via Podman + HAPI FHIR + Node-basert SMART-mock + Altinn local-test.

`syk-inn`: NAIS-deploy (dev/demo/prod), 2–4 replikaer, Valkey-instans, liveness/readiness/prestop-prober, Prometheus-metrikker, OpenTelemetry auto-instrumentering + Grafana Faro (frontend-tracing), Loki-logging, Unleash for feature-toggles (f.eks. `SYK_INN_STRUCTURED_FHIR` som skrur QuestionnaireResponse av/på). PDF genereres med Typst.

---

## 11. Dokumentasjon

Her er `forer` faktisk *imponerende* — og på noen punkter rikere enn `syk-inn` som ren prosa. Fem dokumenter (~13 500 ord): kravspesifikasjon, implementeringsguide med fallgruver, beslutningslogg over åpne juridiske/arkitektoniske spørsmål, pasientflyt for digital egenerklæring, fullstendig IS-2569-feltstruktur, og tre SVG-diagrammer. Den er ærlig om begrensninger og skiller eksplisitt det som krever *menneskelig* avklaring fra det som er kode.

`syk-inn` dokumenterer tyngre på **arkitekturbeslutninger** (ADR01 er en lærebok i FHIR-ressursvalg), FHIR-ressurs-docs per type, fsh-profiler, og sekvensdiagrammer for SMART-launch.

**Begge team dokumenterer godt, men ulikt:** `forer` forklarer *beslutningsrommet og det uavklarte*; `syk-inn` forklarer *de tatte tekniske valgene og begrunner dem*.

---

## 12. Hva kan hver app lære av den andre?

**`forer` kan hente fra `syk-inn`:**
- Ekstraher SMART-flyten ut av controlleren til en testbar komponent/pakke (`Digdir.SmartOnFhir` — se `VEIKART.md` fase 4); legg på tokenvalidering, refresh og distribuert sesjon (Valkey/Redis).
- Fyll issuer-allowlist og gjør den fail-closed også i test mot ekte EPJ.
- Implementer DocumentReference/QuestionnaireResponse-writeback — ADR01 fra `syk-inn` er en ferdig oppskrift (se `VEIKART.md` fase 2).
- Legg til i det minste e2e-røyktester av launch→prefill→innsending (se `VEIKART.md` fase 3).

**`syk-inn` kan hente fra `forer`:**
- Den eksplisitte beslutningsloggen (`BESLUTNINGER.md`) for åpne juridiske/organisatoriske spørsmål er et mønster verdt å kopiere — DPIA, behandlingsansvar og rettslig grunnlag er like relevante for sykmelding.
- Vurder om Altinn-plattformens gratis-infrastruktur (signering, arkiv, PDF, XACML) kunne spart egenutviklet drift.

---

## 13. Konklusjon

De to appene er «i prinsippet like»: samme aktører (behandler i EPJ), samme protokoll (SMART App Launch IG + FHIR R4), samme nasjonale kodeverk, samme BFF-sikkerhetsmønster, og samme grunnform (launch → prefill → skjema → innsending).

Forskjellen er **ett vertikalt snitt på PoC-stadiet** mot **et driftssatt produkt**:

- `forer` har gjort de *riktige arkitektoniske valgene på papiret* og bevist integrasjonspunktet med minimal kode, ved å la Altinn bære infrastrukturen. Den er svak nøyaktig der den selv sier: tokenvalidering, refresh, allowlist, writeback, tester, distribuert sesjon.
- `syk-inn` har *bygget ut alt det `forer` har planlagt*, og lagt til to launch-kontekster (FHIR + HelseID) bak ett GraphQL-schema, ekte writeback med en gjennomtenkt FHIR-modell, regelmotor-integrasjon, full testpyramide og produksjons-observability.

Hvis målet er å vurdere *retning*, er `forer` et godt fundament med solid dokumentasjon av det uavklarte. Hvis målet er *referansearkitektur for SMART on FHIR i norsk helsesektor i produksjon*, er `syk-inn` fasiten — og kan brukes nesten direkte som mal for de fire-fem tingene `forer` har på sin todo-liste (jf. `VEIKART.md`).
