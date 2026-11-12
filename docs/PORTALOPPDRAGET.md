# Portaloppdraget (TB2023-17) — rapport om forbedret informasjonsutveksling mellom helsepersonell og myndigheter

**Kilde:** [Rapport fra Portaloppdraget med anbefalinger (PDF, Helsedirektoratet)](https://www.helsedirektoratet.no/rapporter/rapport-om-forbedret-informasjonsutveksling-mellom-helsepersonell-og-myndigheter/Rapport%20fra%20Portaloppdraget%20med%20anbefalinger_November%202023.pdf/_/attachment/inline/92538d09-4356-497d-9b39-d28bd9fa3682:6400840f7801415673ee116f2b7e9a1a6d0f3f74/Rapport%20fra%20Portaloppdraget%20med%20anbefalinger_November%202023.pdf)
**Tittel i PDF-en:** «Rapport om forbedret informasjonsutveksling mellom helsepersonell og myndigheter — Anbefalinger for å redusere rapporteringsbyrden», rapportnummer TB2023-17 Portal-oppdraget
**Dato og avsender:** november 2023. Skrevet av Direktoratet for e-helse (innlemmet i Helsedirektoratet i 2024) på oppdrag fra Helse- og omsorgsdepartementet. Prosjektet ble finansiert gjennom Stimulab-ordningen, og Digitaliseringsdirektoratet er oppført som å ha gitt midler.
**Lest og oppsummert:** 2026-11-12, hele rapporten (ca. 65 sider pluss vedlegg). Kapittelhenvisninger under er til rapportens egen nummerering.

**Forbehold:** rapporten er fra 2023. Hva som har skjedd med tiltakene siden, er ikke undersøkt her. Påstander om «dagens situasjon» i rapporten (for eksempel at bare én fastlege-EPJ støtter SMART on FHIR) er tilstanden i 2023 og må leses slik.

---

## 1. Hva rapporten er, og hva den ikke dekker

Oppdraget var å anbefale hvordan innrapportering og informasjonsutveksling fra helsepersonell til **myndigheter** bør håndteres fremover. Med «rapportering» menes både innrapportering til registre og utfylling av skjema, erklæringer og helseattester til andre enn pasienten selv (kap. 1.6). Det er altså nøyaktig den klassen skjema dette prosjektet bygger på.

**Avgrensning som er viktig for oss:** oppdragsteksten (kap. 1.1) utelater uttrykkelig informasjonsutveksling *mellom helseaktører* om ytelse av helsehjelp. Det betyr at en henvisning fra tannlege til kjeveortoped (`AppKjeveortopedisk`) i seg selv ligger utenfor rapportens omfang. Helfos vurdering av stønad er derimot en myndighetsvurdering. Rapportens konklusjoner kan derfor brukes som støtte for kjeveortopedisk-caset bare med dette forbeholdet.

Fokus er fastlegene, men rapporten sier selv at mye er relevant for annet helsepersonell.

## 2. Hovedfunn

| Funn | Tall og beskrivelse (kap. 1.2, 2) |
|---|---|
| Arbeidsbyrde | Median arbeidstid for fastleger er 55 timer per uke. 58 % opplever markant økt arbeidsbelastning, særlig knyttet til attester, erklæringer og sykmelding. Omtrent syv timer per uke går til administrativt arbeid, der kontakten med NAV er største andel. |
| Fragmentering | Mange portaler med ulik pålogging og ulikt brukergrensesnitt, liten grad av forhåndsutfylling, og dobbeltregistrering — også ved at opplysningene må føres manuelt tilbake i journalen. |
| Hvorfor portaler vinner | De er enkle for myndigheten å etablere: ofte ingen databehandleravtale og ingen avtale om utvikling i EPJ, og de kan endres raskt av virksomheten selv. |
| Leverandørsiden | 41 EPJ fra 29 leverandører, hvorav fastleger bruker 9. Leverandørene har for lite kapasitet mot summen av myndighetenes behov, og de opplever at myndighetene ikke er koordinerte eller tydelige på standarder. Kundene betaler lite for rapporteringsfunksjonalitet pålagt av myndighetene. |
| Endringstakt | NAV opplever at endringer i skjema som ligger i EPJ-ene kan ta flere år å få gjennomført hos alle leverandørene (kap. 4.1.4 og 5.1.1). |

## 3. De fem tiltaksområdene

| Område | Innhold | Anbefaling |
|---|---|---|
| 1 | Rapportering tettere integrert i fastlegens EPJ (tiltak 1A–1F) | Start med 1A, 1B, 1D og 1F. Vent med 1C og 1E. |
| 2 | Samskapingsarena for behov og løsninger | Videreføre arenaen fra Portaloppdraget |
| 3 | Nasjonal samordning, som verner fastlegen mot nye rapporteringsoppgaver | Utrede videre |
| 4 | Nasjonal prioritering av myndighetenes behov mot EPJ-leverandørene | Helsedirektoratet vurderer operasjonalisering |
| 5 | Regelverk som understøtter effektiv rapportering | Ingen nye tiltak nå; gjennomgang av registerforskriftene støttes |

Tiltakene i område 1:

| Tiltak | Innhold |
|---|---|
| 1A | Samleside med lenker til relevante portaler (lav kost, lav nytte) |
| 1B | Felles rammeverk for portaler: single sign-on, kontekstoverføring, sikkerhet og tilbakeskriving, inkludert HelseID |
| 1C | Én felles inngangsportal med HelseID-SSO (utsettes, venter på 1B) |
| **1D** | **Tilrettelegge for apper i EPJ — SMART on FHIR** |
| 1E | Felles rammeverk for API-integrasjon (ikke prioritert først) |
| 1F | Veileder for valg mellom portal, API og SMART on FHIR |

## 4. Tiltak 1D: SMART on FHIR (kap. 4.1.4, 4.1.7, 4.1.8)

Dette er den delen som treffer prosjektet tydeligst.

**Konklusjonen** er at SMART on FHIR-rammeverket bør innføres i alle fastlege-EPJ-er, som en tredje samhandlingsmekanisme ved siden av portaler og API. Den skal *sameksistere* med dem, ikke erstatte dem, fordi den ikke er beste løsning i alle tilfeller.

**Argumentene for** appmodellen er at:
- én app kan vedlikeholdes én gang og kjøres i alle EPJ-er som støtter rammeverket, noe som gir raskere og mer enhetlig utrulling fra sentralt hold;
- registereier eller tjenesteeier kan endre appen uten at EPJ-leverandøren må utvikle noe;
- appen kan hente standardisert informasjon (rapporten nevner uttrykkelig pasient, behandler og konsultasjon) og forhåndsutfylle skjemaet;
- FHIR-API-er kan også brukes til å skrive PDF eller strukturerte data tilbake til EPJ for journalføring;
- EPJ-leverandørene slipper å bygge og vedlikeholde brukergrensesnitt for hvert rapporteringsbehov, og de har selv sagt at de ikke ønsker å konkurrere på rapportering til myndigheter;
- en løsning med komplisert forretningslogikk og hyppige endringer er en god kandidat.

**Ulempene og uavklarte forhold** rapporten selv peker på:
- I 2023 hadde bare én fastlege-EPJ implementert støtte, og det hadde vist seg vanskelig å få flere til det.
- Appens brukergrensesnitt vil se annerledes ut enn EPJ-ets. Felles designelementer i portalrammeverket (1B) kan jevne ut forskjellene.
- Én leverandør ønsket at API alltid skulle tilbys i tillegg. Ulempen er at tjenesteeieren da igjen blir avhengig av leverandørens prioriteringer. Dette avgjøres fra case til case.
- Apper fra myndigheter eller registereiere er enklere å ta i bruk enn tredjepartsapper, som krever avklaringer om avtaler, ansvar, distribusjon og test.

**Anbefalt rekkefølge:**

| Horisont | Aktiviteter for 1D |
|---|---|
| Kort sikt (1 år) | Avklare **økosystemet for statlige apper** — ansvarsforhold, endringshåndtering, app-butikk, test og godkjenning. Gi en tydeligere anbefaling om bruk av SMART on FHIR og vurdere å normere Direktoratet for e-helses «Anbefaling om bruk av SMART on FHIR». Avklare finansiering og virkemidler mot EPJ-leverandørene. Identifisere en pilotapp for innrapportering. Standardisere og vurdere normering av **HelseAPI**. |
| Mellomlang sikt (2–4 år) | Etablere SMART on FHIR-støtte i resterende fastlege-EPJ-er. Teste pilotappen i den EPJ-en som allerede har støtte. Utvikle og ta i bruk statlige apper (utviklingen kan settes ut til næringslivet). Utvide HelseAPI til flere informasjonselementer. Avklare økosystemet for norske og utenlandske tredjepartsapper. |
| Lang sikt (4+ år) | Ta i bruk tredjepartsapper på en styrt måte. |

**Overgangsperioden (kap. 4.1.6):** fordi det tar tid å få SMART-støtte i alle EPJ-er, kan et innrapporteringsbehov i en periode være tilgjengelig både som portal (for EPJ-er uten støtte) og som SMART-app.

## 5. Hva som er relevant for andre deler av tiltaksområdene

- **Tilbakeskriving (1B, 1C):** rapporten foreslår at tilbakeskriving til EPJ kan baseres på meldingsstandarden *Dialogmelding* (eller en profil av den) og eksisterende meldingsinfrastruktur, og at det må vurderes om det bare skal være ustrukturert PDF eller også strukturert informasjon. Det som sendes tilbake bør være konsist, så journalen ikke fylles med unødvendig tekst.
- **NAV (kap. 5.1.1):** i det parallelle NAV-oppdraget er SMART on FHIR og webapplikasjoner/portaler de to mest aktuelle løsningene. Arbeidet bør starte med sykmeldingsløpet og legeerklæring ved arbeidsuførhet, og det trengs en plan for hvordan oppslutning fra EPJ-leverandørene kan sikres.
- **Kommunale skjema (kap. 4.6.4):** legeerklæring for HC-kort (parkering) er eksempel på at kommunene har ulik utforming og ulike krav, noe som hindrer automatisering og gjenbruk av EPJ-data.
- **Selvbetjening på Helsenorge (kap. 4.6.5):** en mer tillitsbasert tilnærming til attester, med økt bruk av egenmeldinger som innbygger kan fylle ut på Helsenorge.
- **Nasjonal skjemadatabase (kap. 4.6.3):** krever videre utredning og må vurderes sammen med skjemaløsningene i Helsenorge og i Altinn. Det er den eneste uttrykkelige omtalen av Altinn i rapporten.
- **MSIS (kap. 4.6.2):** kommuneoverlegen får i dag en papirkopi av meldingen og trenger mer informasjon fra MSIS til smittesporing. Rapporten anbefaler at dette løftes av FHI etter samlingen av registrene.
- **Interessenter (vedlegg A):** Statens vegvesen er oppført som behovseier, med erfaringsdeling via NHN knyttet til førerrett-prosjektet. Vedlegget nevner også at konklusjonen etter helseattest eller synsattest sendes elektronisk til førerkortregisteret.

## 6. Hvordan dette treffer `forer-legeerklaering`

| Vår påstand eller arbeid | Hva rapporten sier | Vurdering |
|---|---|---|
| SMART on FHIR er en gyldig tredje mekanisme ved siden av portal og API ([STRATEGI.md](STRATEGI.md)) | Kap. 4.1.7 konkluderer at den bør innføres i alle fastlege-EPJ-er og sameksistere med portaler og API | **Støtte.** Posisjonen er forankret i en offisiell anbefaling, ikke bare vår egen. |
| Dobbel inngangsmodus: samme app fra EPJ og via vanlig Altinn-pålogging ([DOBBEL-INNGANGSMODUS.md](DOBBEL-INNGANGSMODUS.md)) | Kap. 4.1.6: i en periode både portal og SMART-app for samme behov | **Delvis støtte.** Rapporten ser portal og app som to løsninger i en overgangsperiode. At *samme app* håndterer begge, er vår egen videreføring og ikke noe rapporten foreslår. |
| Appen eies av tjenesteeier og kan endres uten EPJ-leverandøren | Kap. 4.1.4 og 5.1.1: dette er hovedgevinsten, og NAVs erfaring er flere år per endring via EPJ | **Støtte.** |
| Prefill av pasient, behandler, organisasjon og konsultasjon ([SMARTFHIR-COMMON.md](SMARTFHIR-COMMON.md)) | Kap. 4.1.4 nevner nettopp pasient, behandler og konsultasjon | **Støtte.** Vi leser i tillegg organisasjon og diagnose. |
| Writeback som FHIR `DocumentReference` ([VEIKART.md fase 2](VEIKART.md)) | Kap. 4.1.2: tilbakeskriving kan baseres på Dialogmelding. Kap. 4.1.4: FHIR-API for å skrive PDF eller strukturerte data er også mulig | **Avvik å merke seg.** Rapporten peker først og fremst på Dialogmelding for portaler. Vår FHIR-vei er nevnt som mulighet for apper, men ikke utdypet. Valget bør gjøres bevisst. |
| HelseID og Normen ([BESLUTNINGER.md C-2](BESLUTNINGER.md)) | 1B og 1C forutsetter HelseID for SSO, og et tillitsrammeverk for tilgangsstyring | **Samsvar.** |
| Hvor skal statlige apper kjøre, og hvem godkjenner dem? ([BESLUTNINGER.md C-4, C-6](BESLUTNINGER.md)) | Kap. 4.1.4: økosystem for statlige apper (ansvar, endringshåndtering, app-butikk, test og godkjenning) er uavklart og er første aktivitet på kort sikt | **Åpent og relevant.** Altinn Studio som hosting- og utviklingsplattform er et mulig svar på deler av dette. Rapporten tar ikke stilling til plattform. |
| Pilotapp for innrapportering | Kap. 4.1.8: identifisere og teste én pilotapp i den EPJ-en som har støtte | **Aktuelt.** FHI har bekreftet at MSIS-klinikermelding kjører SMART on FHIR i minst ett EPJ ([KARTLEGGING-kandidater.md, note til A5](KARTLEGGING-kandidater.md)). Om det er pilotappen rapporten sikter til, er ikke avklart. |
| `NHN-DOKUMENTASJON.md §5`: HL7Norway/HelseAPI | Kap. 4.1.8 anbefaler å standardisere og vurdere normering av HelseAPI, og utvide den | **Styrker bidragsanbefalingen.** Se [NHN-DOKUMENTASJON.md §5](NHN-DOKUMENTASJON.md). |
| Digital egenerklæring NA-0201 via Helsenorge ([PASIENTFLYT.md](PASIENTFLYT.md)) | Kap. 4.6.5: attester og egenmeldinger som innbygger fyller ut på Helsenorge, som tillitsbasert tilnærming | **Samsvar i retning.** |
| Helse-template for flere skjema (STRATEGI.md Spor B) | Kap. 4.6.4: ulik kommunal utforming av for eksempel HC-kort-erklæringen hindrer gjenbruk av EPJ-data. Kap. 4.6.3: skjemadatabase vurderes sammen med Helsenorge og Altinn | **Relevant for kandidatvalg.** Skjema som er nasjonalt enhetlige, er lettere å automatisere enn kommunale varianter. |

## 7. Hvor rapporten og prosjektet ikke møtes

- **Rapporten behandler ikke Altinn som plattform.** Den sammenligner portal, API og SMART on FHIR, men sier ingenting om hvor SMART-apper bør hostes eller hvordan skjemaeier bør bygge dem.
- **Rapporten er ikke teknisk.** Den beskriver ingen launch-flyt, ingen scopes og ingen tokenvalidering. Alt i [IMPLEMENTERING.md](IMPLEMENTERING.md) går dypere enn den.
- **Helseaktør-til-helseaktør** (henvisninger) er uttrykkelig utenfor omfanget, jf. kap. 1.
- **Pasientsiden** (Helsenorge EksternAPI, oppgaver og kopi til innbygger) er ikke et tema, bortsett fra tiltaket om selvbetjening for attester.
- **Tidsforskyvning:** tilstanden i 2023 er ikke tilstanden i dag. Våre egne funn (MSIS i produksjon i et EPJ, NAVs `nav-epj`, Helsenorge EksternAPI verifisert mot testmiljø) viser at deler av det rapporten ser som fremtidig, er kommet lenger.

## 8. Mulige oppfølginger

1. **Bruk rapporten som referanse i dialogen med NHN og Helsedirektoratet.** Den gir et felles språk («pilotapp», «statlige apper», tiltak 1D/1F) og viser at prosjektets retning allerede er anbefalt.
2. **Svar på de uavklarte spørsmålene i 1D** med erfaringene fra dette prosjektet: ansvarsforhold, endringshåndtering, test og godkjenning, og hosting. Beslutning C-6 er stedet å begynne.
3. **Bidra til tiltak 1F (veileder for valg av integrasjonsløsning).** Dobbel inngangsmodus og erfaringene fra to ulike skjema er konkret materiale.
4. **Ta stilling til tilbakeskriving:** Dialogmelding eller FHIR `DocumentReference`. Legg valget inn i veikartets fase 2.
5. **Undersøk hva som har skjedd med tiltakene siden 2023** før noe av dette brukes utad.

---

## Referanser

- Direktoratet for e-helse. *Rapport om forbedret informasjonsutveksling mellom helsepersonell og myndigheter — Anbefalinger for å redusere rapporteringsbyrden.* November 2023. TB2023-17 Portal-oppdraget. Lenke øverst.
- Rapporten viser selv til *Anbefaling om bruk av SMART on FHIR* og *SMART on FHIR: Bruk og potensial* (Direktoratet for e-helse), *Situasjonsbeskrivelse og erfaringer med SMART on FHIR — EPJ-løftet*, og *Fastlegers arbeid med attester og erklæringer utenfor NAV området* (Helsedirektoratet, 2022). De er ikke lest her.
