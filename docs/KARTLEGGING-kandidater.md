# Kartlegging av rapporteringsplikter for helsepersonell

Grunnlag for vurdering av forenklingspotensial – primær- og spesialisthelsetjenesten, offentlig og privat.
Versjon per juni 2026.

Tabellene dekker lovpålagte og avtalebaserte meldinger, rapporteringer og attester som helsepersonell og
virksomheter har overfor det offentlige **og** privat sektor. Hver rad er vurdert for om en
**SMART on FHIR**-applikasjon (app som startes i journalen, leser/skriver strukturerte data via FHIR) gir verdi.

> **Forbehold:** Listen er ikke uttømmende, og begreper/organisering endres jevnlig. Statens legemiddelverk er nå
> Direktoratet for medisinske produkter (DMP); Fylkesmannen heter Statsforvalteren; Reseptregisteret er erstattet av
> Legemiddelregisteret (LMR); Direktoratet for e-helse ble innlemmet i Helsedirektoratet i 2024. Rettslige grunnlag er
> forenklet – kontrollér mot gjeldende lov/forskrift før konkrete tiltak.

## Forklaring

| Begrep | Betydning |
|---|---|
| **Sektor** | Primær = fastlege/kommunal; Sekundær = spesialist/sykehus; Begge = gjelder begge |
| **Innsamling** | *Automatisk* = batch-uttrekk fra fagsystem; *Delvis* = noe manuelt; *Manuell* = skjema/portal/papir |
| **SoF-verdi** | Antatt verdi av en SMART on FHIR-app: **Høy** / Middels / Lav (begrunnet i eget kapittel) |

## Bærende prinsipp: generelt vs. profesjonsspesifikt

To typer plikter må skilles:

- **Generelle helsepersonell-plikter** (helsepersonelloven §§ 32–38) gjelder *alle* autoriserte yrkesgrupper likt –
  uavhengig av profesjon, sektor og offentlig/privat. Det er rad **D1** (barnevern), **D2** (sosial-/kommunal
  tjeneste), opplysningsplikt ved vold/kjønnslemlestelse, **C1** (varsel alvorlig hendelse) og **C4** (pasientskade).
  Disse gjentas derfor ikke per yrkesgruppe i del 2 – de ligger «over» tabellen.
- **Profesjonsspesifikke plikter** følger av hva slags helsehjelp som ytes (refusjonsordninger, attester, fagregistre,
  tvangslovgivning). Det er disse som skiller yrkesgruppene, og som dekkes i del 2 nedenfor.

---

# Del 1 — Alle helsepersonell og leger/virksomheter

## A. Sentrale / nasjonale helseregistre

Disse er i hovedsak *register-leveranser* – data høstes som batch fra fagsystem. SMART on FHIR (en interaktiv app i
behandlerens flyt) gir derfor mest verdi der kliniker fortsatt fyller ut en strukturert melding manuelt.

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| A1 | Norsk pasientregister (NPR) | Sekundær | Helsedirektoratet | Uttrekk fra EPJ/PAS | NPR-forskriften | Automatisk | Lav |
| A2 | Kommunalt pasient- og brukerregister (KPR), inkl. IPLOS | Primær | FHI | Fagsystem-uttrekk; KUHR daglig; IPLOS-registrering | KPR-forskriften | Delvis | Middels |
| A3 | Medisinsk fødselsregister (MFR) | Sekundær | FHI | Elektronisk melding | hpl §35; MFR-forskriften | Automatisk | Lav |
| A4 | Dødsmelding / Dødsårsaksregisteret (eDÅR) | Begge | FHI | Elektronisk dødsmelding | hpl §36; DÅR-forskriften | Automatisk | Lav |
| A5 | MSIS – smittsom sykdom (klinikermelding) | Begge | FHI + kommunelege | MSIS-skjema / elektronisk | smittevernloven §2-3; hpl §37 | Delvis | Middels |
| A6 | MSIS-labdatabasen | Sekundær | FHI | Elektronisk | MSIS-forskriften | Automatisk | Lav |
| A7 | Tuberkuloseregister | Begge | FHI + TB-koordinator | Skjema | Tuberkuloseforskriften | Manuell | Middels |
| A8 | SYSVAK – vaksinasjonsregister | Begge | FHI | Elektronisk fra EPJ/vaksinemodul | SYSVAK-forskriften | Automatisk | Lav |
| A9 | Legemiddelregisteret (LMR) | Begge | FHI | Elektronisk fra apotek | LMR-forskriften | Automatisk | Lav (plikt på apotek) |
| A10 | Kreftregisteret (klinisk melding) | Begge | Kreftregisteret | Elektronisk melding / KREMT | Kreftregisterforskriften | Delvis | **Høy** |
| A11 | Hjerte- og karregisteret (HKR) | Sekundær | FHI | Elektronisk / kvalitetsregistre | HKR-forskriften | Delvis | Middels |
| A12 | Abortregisteret | Sekundær | FHI (MFR) | Skjema / elektronisk | abortloven | Delvis | Lav |
| A13 | NOIS / NORM / RAVN (infeksjon/resistens) | Sekundær | FHI | Elektronisk / uttrekk | resp. forskrifter | Automatisk | Lav |
| A14 | Helsearkivregisteret | Begge | Norsk helsearkiv | Avlevering | helsearkivforskriften | Manuell | Lav |

## B. Medisinske kvalitetsregistre

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| B1 | Nasjonale medisinske kvalitetsregistre (~50+) | Begge | Det enkelte registeret | Egne innregistreringsportaler | forskrift om medisinske kvalitetsregistre §2-3 | Delvis | **Høy** |
| B2 | Årlig statusrapport (registernivå) | Begge | SKDE | Rapport | forskrift om nasjonale kvalitetsregistre | Manuell | Lav |

## C. Hendelser, skader og svikt

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| C1 | Varsel om alvorlig hendelse | Begge | Statens helsetilsyn + Ukom | melde.no / varselordning | sphlsl. §3-3a; helsetilsynsloven | Manuell | Middels |
| C2 | Bivirkningsmelding legemiddel | Begge | DMP / RELIS | melde.no | legemiddelforskriften; hpl | Manuell | **Høy** |
| C3 | Hendelse med medisinsk utstyr | Begge | DMP | melde.no | forskrift om medisinsk utstyr / MDR | Manuell | Middels |
| C4 | Pasientskade – tilleggsinfo til sak | Begge | Norsk pasientskadeerstatning (NPE) | NPE-portal / skjema | pasientskadeloven | Manuell | Middels |

## D. Meldeplikt til andre myndigheter

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| D1 | Melding/opplysninger til barnevernet | Begge | Kommunal barneverntjeneste | Skjema/brev/portal (varierer) | hpl §33; barnevernsloven | Manuell | Middels |
| D2 | Opplysninger til kommunal helse-/sosialtjeneste | Begge | Kommunen / NAV | Brev / skjema | hpl §32 | Manuell | Lav |
| D3 | Melding til politiet om unaturlig dødsfall | Begge | Politiet (+ Statsforvalter) | Telefon/personlig + skjema | hpl §36; forskrift om leges melding | Manuell | Lav |
| D4 | Førerkort: helseattest | Begge | Statens vegvesen (Statsforvalter ved tvil) | Papir/PDF båret av pasient | førerkortforskriften vedlegg 1 | Manuell | **Høy** (er pilot) |
| D5 | Førerkort: melding om manglende helsekrav (>6 mnd) | Begge | Statsforvalteren | Skjema/brev | hpl §34; førerkortforskriften | Manuell | **Høy** |
| D6 | Melding om arbeidsrelatert sykdom (skjema 154B) | Begge | Arbeidstilsynet (Havtil/Luftfartstilsynet) | PDF-skjema per post / eDialog / Altinn | arbeidsmiljøloven §5-3 | Manuell | **Høy** |
| D7 | Helseerklæring arbeidsdykking | Begge | Arbeidstilsynet | Egen attest | dykkeforskrift | Manuell | Middels |
| D8 | Tilrettelegging ved graviditet (arbeid) | Begge | Arbeidstilsynet / arbeidsgiver | Erklæring | aml. | Manuell | Middels |
| D9 | Sertifikatattester (sjøfart, dykk, luftfart, jernbane) | Begge | Sjøfartsdir., Luftfartstilsynet, SJT, Havtil | Egne ordninger per sektor | sektorlovgivning | Manuell | Middels |

## E. NAV / trygd / refusjon

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| E1 | Sykmelding | Begge | NAV | Elektronisk fra EPJ (lovpålagt) | folketrygdloven | Automatisk | Middels |
| E2 | Legeerklæringer/uttalelser (L-takster) | Begge | NAV | Elektronisk (lovpålagt) | folketrygdloven §21-4 vedlegg 1 | Delvis | **Høy** |
| E3 | Oppgjørskrav L-takster | Begge | Helfo / NAV økonomi | Elektronisk regning (EHF) | folketrygdloven kap. 22 | Automatisk | Lav |
| E4 | Refusjonskrav / direkteoppgjør (KUHR) | Begge | Helfo (→ KUHR → KPR) | Elektronisk fra EPJ | folketrygdloven; oppgjørsavtale | Automatisk | Lav |
| E5 | Praksisinformasjon / direkteoppgjørsavtale | Begge | Helfo | Praksisinformasjon-portal | folketrygdloven | Manuell | Lav |
| E6 | Oppfølgingsplan / dialogmeldinger | Primær | NAV / arbeidsgiver | Dialogmelding i EPJ | – | Automatisk | Lav |
| E7 | Blåreseptsøknad / individuell stønad + vedtak | Begge | Helfo | Tjenesteportalen / EPJ | blåreseptforskriften | Delvis | Middels |

## F. Attester til kommune, fylke, utdanning og dagligliv

Disse er stort sett **strukturerte attester kliniker fyller ut manuelt**, sendt via varierende kanaler – et tydelig
forenklingsområde.

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| F1 | TT-kort (tilrettelagt transport) | Primær | Fylkeskommune/kommune | Attest/skjema | Manuell | **Høy** |
| F2 | HC-kort (parkering for forflytningshemmede) | Primær | Kommune | Attest/skjema | Manuell | **Høy** |
| F3 | Legeerklæring ved skolefravær | Primær | Skole/kommune | Attest | Manuell | Middels |
| F4 | Sykmelding / nedsatt funksjonsevne for studenter | Primær | Lånekassen | Sykmelding/erklæring | Manuell | **Høy** |
| F5 | Helsekort for gravide | Primær | Helsetjenesten (deles) | Digitalt helsekort (innføres 2026–27) | Delvis | Lav (løses via NHN) |
| F6 | Div. attester (ammende mødre, treningssenter, legemidler til utenlandsreise m.m.) | Primær | Private/diverse aktører | Fritekst-attest | Manuell | Middels |

## G. Privat sektor – forsikring og andre

Privat sektor er en stor og i dag svært manuell konsument av legeerklæringer. Se eget kapittel om DSOP + SMART on FHIR.

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Grunnlag/rolle | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|---|
| G1 | Erklæring ved forsikringstegning | Begge | Forsikringsselskap | Via Helsenett (Finans Norge-flyt) / brev | samtykke; sakkyndig | Manuell | **Høy** |
| G2 | Erklæring til erstatnings-/skadesak | Begge | Forsikringsselskap | Via Helsenett / brev | samtykke; sakkyndig | Manuell | **Høy** |
| G3 | Sakkyndig-/behandlererklæring | Begge | Forsikring/justis | Brev / portal | habilitet, objektivitet, honorar per avtale | Manuell | Middels |
| G4 | Erklæring/attest til justissektoren (forfall, soningsdyktighet, prøveløslatelse m.m.) | Begge | Domstol / kriminalomsorg | Brev / attest | pasientens anmodning / pålegg | Manuell | Lav |

## H. Styring, statistikk og tilskudd

| ID | Oppgave / melding | Sektor | Mottaker | Kanal i dag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| H1 | KOSTRA / kommunal aktivitetsrapportering | Primær | SSB | Altinn / SSB | Delvis | Lav |
| H2 | Helsestatistikk | Begge | SSB | SSB / Altinn | Delvis | Lav |
| H3 | Tilskudds-/aktivitetsrapportering (ALIS m.m.) | Begge | Helsedir / Statsforvalter | Altinn / ulike portaler | Manuell | Lav |
| H4 | Strålebruk / strålevern | Begge | DSA | DSA-ordninger | Manuell | Lav |

---

# Del 2 — Profesjonsspesifikke plikter

Utvidelse til tannleger/tannpleiere, fysioterapeuter, manuellterapeuter, kiropraktorer, psykologer,
psykiatere/psykisk helsevern, apotek/farmasøyter, jordmødre/helsesykepleiere, optikere, ergoterapeuter,
bioingeniører/laboratorier og radiografer. Mange av disse bruker EPJ og en stor andel er private.

> ID-er som starter med bokstav (A–H) viser til del 1 over. Nye rader her har profesjonsprefiks
> (TANN, FYS, KIR, PSY, PHV, APO, JOR, OPT, ERG, LAB, RAD).

## Oversikt per yrkesgruppe

| Yrkesgruppe | Sektor | Off./privat | EPJ | Tyngste profesjonsspesifikke plikter |
|---|---|---|---|---|
| Tannlege / tannpleier | Primær (fylkeskom. + privat) | Begge | Ja | Helfo-stønad, bivirkning dentale materialer, KPR tannhelse |
| Fysioterapeut | Primær | Begge | Ja | Helfo direkteoppgjør (KUHR), KPR, NAV-erklæring på bestilling |
| Manuellterapeut | Primær | Mest privat | Ja | Sykmelding, henvisning, Helfo |
| Kiropraktor | Primær | Mest privat | Ja | Sykmelding, henvisning, Helfo |
| Psykolog | Begge | Begge | Ja | NAV-erklæring, førerkort-meldeplikt, sakkyndigerklæring, NPR/BUP |
| Psykiater / psykisk helsevern | Sekundær | Mest offentlig | Ja | Tvangsvedtak til kontrollkommisjon, tvangsdata til NPR |
| Apotek / farmasøyt | – | Mest privat | Eget fagsystem | Blåresept-oppgjør, LMR, e-resept, SYSVAK |
| Jordmor / helsesykepleier | Primær | Mest offentlig | Ja | MFR, helsekort gravide, KPR helsestasjon, SYSVAK |
| Optiker | Primær | Privat | Ja | Førerkort syns-/helseattest og meldeplikt |
| Ergoterapeut | Primær | Mest offentlig | Ja | Hjelpemiddelsøknad til NAV, KPR |
| Bioingeniør / laboratorium | Sekundær | Begge | LIS | MSIS-lab, NOIS/NORM/RAVN, patologi til Kreftreg. |
| Radiograf / bildediagnostikk | Sekundær | Begge | RIS/PACS | Strålevern (DSA), privatfinansiert mammografi |

## Tannlege / tannpleier

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| TANN1 | Stønad til tannbehandling / direkteoppgjør | Helfo | Elektronisk refusjonskrav fra EPJ | ftrl §5-6, §5-6a, §5-25 | Automatisk | Lav |
| TANN2 | Egenandel / frikort | Helfo | Elektronisk (med oppgjør) | frikortordningen | Automatisk | Lav |
| TANN3 | Bivirkning av odontologiske materialer | Bivirkningsgruppen for odontologiske biomaterialer (NIOM/UiB) | Eget skjema (takst 6/10) | stønadsforskrift/hpl | Manuell | **Høy** |
| TANN4 | KPR – tannhelsetjeneste | FHI | Fagsystem-uttrekk (fra 2026) | KPR-forskriften | Delvis | Lav |

Delte plikter: NPR ved kjevekirurgi i sykehus (A1), forsikringsattester (G1–G2), generelle §§32–38-plikter.

## Fysioterapeut, manuellterapeut og kiropraktor

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| FYS1 | Direkteoppgjør / refusjonskrav (KUHR) | Helfo (→ KUHR → KPR) | Elektronisk fra EPJ | ftrl; oppgjørsavtale | Automatisk | Lav |
| FYS2 | KPR KUHR – fysioterapi | FHI | Via KUHR | KPR-forskriften | Automatisk | Lav |
| KIR1 | Sykmelding (kiropraktor/manuellterapeut, avgrenset varighet) | NAV | Elektronisk fra EPJ | folketrygdloven | Delvis | Middels |
| KIR2 | Henvisning til spesialist/bildediagnostikk | Spesialisthelsetjeneste | Henvisningsmelding i EPJ | – | Automatisk | Lav |

Delt plikt: «Legeerklæring ved arbeidsuførhet» kan skrives av psykolog, fysioterapeut, manuellterapeut og kiropraktor
når NAV særskilt ber om det (utvidelse av rad **E2**).

## Psykolog

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| PSY1 | NAV-erklæring på bestilling | NAV | Elektronisk / skjema | ftrl §21-4 | Delvis | **Høy** |
| PSY2 | Førerkort: melding om manglende helsekrav (bl.a. kognitiv funksjon) | Statsforvalteren | Skjema/brev | hpl §34; førerkortforskriften | Manuell | **Høy** |
| PSY3 | Sakkyndig-/behandlererklæring (forsikring, barnevern, justis) | Privat/offentlig | Brev / portal | samtykke; habilitet/objektivitet | Manuell | Middels |
| PSY4 | Aktivitetsdata psykisk helsevern / BUP | NPR (spesialist) / KPR (kommunal) | Uttrekk fra EPJ | NPR-/KPR-forskriften | Automatisk | Lav |

Delte plikter: barnevern (D1) er særlig sentral; varsel alvorlig hendelse (C1), pasientskade (C4).

## Psykiater / psykisk helsevern

Tvangslovgivningen gir et eget rapporteringslag som ikke finnes for andre grupper.

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| PHV1 | Vedtak om tvang (kap. 4/4A): registrering for samlet oversikt | Kontrollkommisjonen | Registrering i EPJ | psykisk helsevernforskriften | Delvis | Middels |
| PHV2 | Melding om vedtak om tvangsmidler / skjerming | Kontrollkommisjonen | Melding (snarest) | psykisk helsevernforskriften | Manuell | Middels |
| PHV3 | Tvangsdata nasjonalt | NPR (eget tvangsdatasett) | Uttrekk fra EPJ | NPR-forskriften | Automatisk | Lav |
| PHV4 | Melding om overføring / dom til tvungent vern | Pasient/pårørende; påtalemyndighet | Melding (forvaltningsloven §27) | phvl §§4-10, 5-4 | Manuell | Lav |

Delte plikter: alle legeplikter (MSIS A5, dødsmelding A4, bivirkning C2, m.fl.).

## Apotek / farmasøyt

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| APO1 | Blåresept – direkteoppgjør / refusjonskrav | Helfo | Elektronisk fra apoteksystem | ftrl §5-14; blåreseptforskriften | Automatisk | Lav |
| APO2 | Utleverte legemidler til Legemiddelregisteret | FHI | Elektronisk | LMR-forskriften | Automatisk | Lav |
| APO3 | E-resept / Reseptformidleren (ekspedering, oppslag) | NHN (Reseptformidleren) | Apoteksystem | reseptformidlerforskriften | Automatisk | Lav |
| APO4 | Vaksinering i apotek | FHI (SYSVAK) | Elektronisk | SYSVAK-forskriften | Delvis | Middels |
| APO5 | Bivirkningsmelding (farmasøyt kan melde) | DMP / RELIS | melde.no | legemiddelforskriften | Manuell | **Høy** |
| APO6 | Bransjestatistikk + medvirkning ved tilsyn | DMP / Statsforvalter | Diverse | apotekloven | Manuell | Lav |

Merknad: SoF-verdien er gjennomgående lav for apotek fordi de fleste pliktene er automatiserte oppgjør/uttrekk fra
apotekenes fagsystem (ikke EPJ med SMART-apper). Unntaket er bivirkningsmelding (APO5).

## Jordmor / helsesykepleier

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| JOR1 | Fødselsmelding (MFR) | FHI | Elektronisk | hpl §35; MFR-forskriften | Automatisk | Lav |
| JOR2 | Helsekort for gravide | Helsetjenesten (deles) | Digitalt helsekort (innføres 2026–27) | – | Delvis | Lav |
| JOR3 | KPR – helsestasjon og skolehelsetjeneste | FHI | Fagsystem-uttrekk (fra 09/2025) | KPR-forskriften | Delvis | Middels |
| JOR4 | Vaksinasjon (SYSVAK) | FHI | Elektronisk | SYSVAK-forskriften | Automatisk | Lav |

Delte plikter: abortmelding (A12), barnevern (D1).

## Optiker, ergoterapeut, laboratorium og radiograf

| ID | Oppgave / melding | Mottaker | Kanal i dag | Rettslig grunnlag | Innsamling | SoF-verdi |
|---|---|---|---|---|---|---|
| OPT1 | Førerkort: syns-/helseattest og meldeplikt | Statens vegvesen / Statsforvalter | Attest / melding | førerkortforskriften; hpl §34 | Manuell | **Høy** |
| ERG1 | Søknad om hjelpemidler / funksjonsvurdering | NAV Hjelpemiddelsentral | Skjema / portal | folketrygdloven kap. 10 | Manuell | Middels |
| ERG2 | KPR – kommunal ergoterapi | FHI | Fagsystem-uttrekk | KPR-forskriften | Delvis | Lav |
| LAB1 | Mikrobiologiske prøvesvar (MSIS-lab) | FHI | Elektronisk | MSIS-forskriften | Automatisk | Lav |
| LAB2 | NOIS / NORM / RAVN | FHI | Elektronisk / uttrekk | resp. forskrifter | Automatisk | Lav |
| LAB3 | Patologi-/celleprøvesvar til Kreftregisteret | Kreftregisteret | Elektronisk melding | Kreftregisterforskriften | Delvis | Middels |
| RAD1 | Strålebruk / strålevern | DSA | DSA-ordninger | strålevernforskriften | Manuell | Lav |
| RAD2 | Privatfinansiert bildediagnostikk (bl.a. mammografi) | DSA / Kreftreg. | Under utredning | (foreslått) | Manuell | Lav |

---

# Kanaler og plattformer (referanse)

| Kanal/plattform | Brukes til | Eier/leverandør |
|---|---|---|
| Melde.no | Bivirkninger, medisinsk utstyr, uønskede hendelser | DMP / Helsedir |
| Praksisinformasjon | Direkteoppgjør, praksisopplysninger | Helfo |
| KREMT | Klinisk melding til Kreftregisteret | Kreftregisteret |
| NISSY | Rekvirering av pasientreiser | Pasientreiser |
| Altinn | Arbeidstilsyn, SSB, tilskudd | Digdir |
| Kjernejournal | Oppslag/deling, ikke rapportering i seg selv | NHN / Helsedir |
| Interaktor | App-plattform for å kjøre SMART on FHIR-apper i EPJ | (leverandør) |
| Fiks-plattformen | Kommunal samhandling | KS |
| Norsk helsenett | Felles infrastruktur, meldingsutveksling, forsikringsflyt | NHN |

---

# SMART on FHIR – hvor gir det verdi?

**SMART on FHIR** lar en webapp startes inni journalsystemet (felles pålogging, delt pasientkontekst via OAuth2/OpenID
Connect), lese strukturerte data fra EPJ (`read`-scope) og skrive resultatet tilbake (`write`-scope), typisk som FHIR
`Questionnaire` → `QuestionnaireResponse`. I Norge er dette ikke teoretisk: **førerkort-attesten (D4) er den første
SMART on FHIR-piloten** – prosjekt «Digital førerrett» mellom Vegdirektoratet, Helsedirektoratet og Politiet,
realisert via EPJ-løftet, med nasjonal anbefaling i HITR 1225:2019.

**Verdien er størst når alle disse er oppfylt:**

1. Kliniker fyller i dag ut en **strukturert erklæring/attest manuelt** (ikke et rent register-uttrekk).
2. Mottaker er **ekstern** og kan motta et strukturert svar.
3. Mye av innholdet kan **forhåndsutfylles fra EPJ** (diagnoser, legemidler, funksjon) – sparer tasting og hever kvalitet.
4. **Høyt volum eller høy friksjon** i dag (egen portal, fritekst, papir).

**Lav verdi** der data allerede høstes automatisk som batch (NPR, KPR/KUHR, LMR, SYSVAK, MFR, NOIS/NORM/RAVN) eller
der oppgaven er sjelden/akutt (helsearkiv, politimelding ved unaturlig død). Apotek faller i tillegg utenfor
EPJ-/SMART-økosystemet (eget fagsystem) — forenkling der handler om oppgjørs- og e-reseptkjeden, ikke SMART-apper.

## Rader med høyest SMART on FHIR-verdi

### Fra del 1 (leger og alle helsepersonell)

| ID | Oppgave | Hvorfor høy verdi |
|---|---|---|
| D4/D5 | Førerkort-attest og -melding | Allerede SoF-pilot; mal for resten |
| D6 | Arbeidsrelatert sykdom (154B) | Papir/post i dag; kan forhåndsutfylles fra journal |
| B1 | Medisinske kvalitetsregistre | Største kilde til dobbeltregistrering vs. journal |
| A10 | Kreftregisteret (klinisk melding) | Strukturert melding, ekstern mottaker, dobbeltarbeid |
| C2 | Bivirkningsmelding | Kan forhåndsutfylles fra legemiddelliste; fritekst i dag |
| E2 | NAV-legeerklæringer | Strukturert `Questionnaire`-flyt; prototype mot NAV finnes |
| F1/F2/F4 | TT-kort, HC-kort, Lånekassen | Strukturerte attester, manuelle og spredt i dag |
| G1/G2 | Forsikringserklæringer | Stort privat volum; krever samtykke og sakkyndig-rolle |

### Fra del 2 (profesjonsspesifikke)

| ID | Oppgave | Hvorfor høy verdi |
|---|---|---|
| TANN3 | Bivirkning dentale materialer | Eget papir-/PDF-skjema; kan forhåndsutfylles fra tannlege-EPJ |
| PSY1 | NAV-erklæring (psykolog) | Strukturert erklæringsflyt, samme mønster som leger (E2) |
| PSY2 / OPT1 | Førerkort-meldeplikt (psykolog/optiker) | Utvider den eksisterende Førerrett-piloten til flere profesjoner — én app, flere brukergrupper |
| APO5 | Bivirkningsmelding fra apotek | Kan forhåndsutfylles fra utleverings-/legemiddeldata |

---

# DSOP + SMART on FHIR mot privat sektor

**Kort svar: ja, det bør med, og DSOP er et passende rammeverk.** Begrunnelse:

- **DSOP (Digital Samhandling Offentlig–Privat)** er en etablert modell der finansnæringen (Finans Norge/Bits) og
  offentlige etater digitaliserer dataflyt – f.eks. samtykkebasert lånesøknad og DSOP «Syke- og uføreforsikring», der
  forsikring og NAV utveksler opplysninger som i dag i stor grad går på papir/post.
- Forsikringsbransjen er allerede en **stor mottaker av legeerklæringer** (ved tegning, ved erstatning, og legen som
  sakkyndig). Det finnes allerede en kanal mot Finans Norge via Helsenettet.
- **Den tekniske brikken finnes:** SMART on FHIR i EPJ + felles tillitstjenester / tillitsanker hos Norsk helsenett.
  En forsikringsspesifikk (eller nasjonal) attest-app kan startes i journalen, hente **samtykkebaserte** opplysninger
  fra EPJ, og returnere en strukturert, signert `QuestionnaireResponse` til selskapet – samme mønster som Førerrett,
  men med privat mottaker.

**Forutsetninger og forbehold:**

- **Samtykke og taushetsplikt:** helseopplysninger er særlige kategorier (GDPR). Flyten må være innbygger-/
  pasientstyrt og dataminimerende – bare det erklæringen krever.
- **Legens sakkyndig-rolle:** krav til faglig forsvarlighet, habilitet og objektivitet. SoF kan *forhåndsutfylle*,
  men legen må fortsatt utøve selvstendig vurdering; honorar avtales per time med selskapet.
- **Governance:** hvem godkjenner appen (EPJ-leverandør, virksomhet, NHN), og hvordan sikres at bare rettmessige
  mottakere får tilgang. Tillitsankeret hos NHN er ment å løse dette.

**Anbefalt rekkefølge:** gjenbruk Førerrett-mønsteret (D4) → offentlige strukturerte attester (D6, E2, F1/F2/F4)
→ deretter DSOP-basert privat flyt for forsikring (G1/G2) når samtykke- og tillitstjenestene er modne.

Se [BESLUTNINGER.md](BESLUTNINGER.md) C-7 for strategisk avklaring.

---

# Tverrgående forenklingstemaer

| Tema | Hva vi ser | Mulig grep | Berører |
|---|---|---|---|
| Dobbeltregistrering vs. journal | Samme opplysning i EPJ og deretter manuelt i registre | Automatisk høsting fra strukturert EPJ; «registrer én gang» | B1, A2, A10, A11 |
| Mange portaler og innlogginger | melde.no, KREMT, Praksisinformasjon, NAV, Altinn, kommunale skjema | Felles inngang / SMART-apper i EPJ | C1–C3, D1–D9, F-rader |
| Flere mottakere for samme melding | MSIS til FHI + kommunelege; død → dødsmelding + politi | Meld én gang, distribuér automatisk | A5, A4+D3 |
| Manuelt der digitalt finnes | 154B på papir/post; attester båret av pasient | Strukturert digital innsending (SMART on FHIR) | D4, D6, F1–F4 |
| NAV-erklæringer – volum og fritekst | Stort volum, lite gjenbruk av journaldata — gjelder lege, psykolog, fysio | Strukturerte `Questionnaire`-felt og forhåndsutfylling | E1, E2, PSY1 |
| Privat sektor utenfor digital flyt | Forsikringserklæringer i stor grad manuelle | DSOP + SMART on FHIR med samtykke | G1, G2 |
| Én attest-app, mange profesjoner | Førerkort-meldeplikten gjelder lege, psykolog og optiker | Felles app, ulike brukergrupper — ikke tre separate apper | D4/D5, PSY2, OPT1 |
| Bivirkningsmeldinger er fragmentert | Legemiddel (C2/APO5) og dentale materialer (TANN3) går til ulike mottakere på ulike skjema | Felles strukturert bivirkningsflyt i melde.no | C2, APO5, TANN3 |
| Tvangsrapportering er lite digitalisert | Data skal allerede ligge i EPJ for kontrollkommisjonen | Automatisert uttrekk fremfor manuell melding | PHV1–PHV4 |
| Helfo-oppgjør er automatisert på tvers | Lege, tannlege, fysio og apotek bruker KUHR/blåresept | Gjenbruk fremfor parallell innsamling | E4, TANN1, FYS1, APO1 |
| Variabelutvalg revideres sjelden | Det som legges inn tas sjelden ut | Jevnlig kritisk revisjon av datasettene | Alle registre |

---

# Kilder

- FHI – Sentrale helseregistre; Helsedirektoratet – Helsedata og helseregistre
- FHI – MSIS-håndbok; KPR; Legemiddelregisteret
- Den norske legeforening – «Lovbestemte meldinger» og «Praktisk veileder for legers attestarbeid»
- NAV – samarbeidspartner (sykmelding, legeerklæringer); folketrygdloven §21-4 vedlegg 1
- Helfo – Lege; Helsedirektoratet – KUHR
- Helsedirektoratet – Førerkortveileder; Statsforvalteren – førerkort og helsekrav
- Arbeidstilsynet – Meldeplikta til legane (skjema 154B; aml. §5-3)
- Helsedirektoratet – «Anbefaling om bruk av SMART on FHIR» (HITR 1225:2019); EPJ-løftet; prosjekt Digital førerrett
- Bits/Finans Norge – DSOP; DSOP «Syke- og uføreforsikring»
- Norsk helsenett – tjenester, felles tillitstjenester / tillitsanker
- Helsedatastrategi 2025–2027 (FHI); rapporten «Nå snakker vi» (Helsedir/NAV/Dir. e-helse)
- Norsk Tannlegeforenings veileder; Den norske tannlegeforenings retningslinjer for bivirkningsmelding
- Norsk Psykologforening – fagetiske retningslinjer; NAV-samarbeidsveileder
- Norsk Fysioterapeutforbund; Norske Kiropraktorers Forening
- Apotekloven med forskrifter; NHN – Reseptformidleren og e-resept
