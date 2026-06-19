# Åpne beslutninger og uavklarte designvalg

Dette dokumentet parkerer beslutninger som krever menneskelig avklaring — juridisk, organisatorisk eller arkitektonisk — og som ikke kan løses med kode alene. Hvert punkt inneholder bakgrunn, alternativer og hvem som beslutter.

---

## C-1: Hvem er «parten» i Altinn-instansen?

**Problemstilling:**  
En Altinn-instans har alltid én «part» (party) som eier instansen. I dag starter legen instansen på vegne av seg selv. Spørsmålet er om parten skal være:

| Alternativ | Beskrivelse | Konsekvens |
|---|---|---|
| **A — Legen** | Legen er part og signatar | Enklest teknisk. Men instansen lever i legens «innboks» i Altinn, ikke pasientens. |
| **B — Pasienten** | Legen fyller ut på vegne av pasienten | Krever delegering eller samtykke. Mer korrekt juridisk (erklæringen gjelder pasienten). |
| **C — Legekontoret (org)** | Instansen tilhører virksomheten | Mulig ved bruk av `partyTypesAllowed.organisation`. Krever systemtilgang-autorisasjon. |

**Avhengigheter:** Valget påvirker autorisasjonsmodellen i `policy.xml`, rollekrav, og hvem som kan se instansen i Altinn-meldingsboksen.

**Beslutter:** Tjenesteeier (Digdir) i samråd med Statens vegvesen og Helsedirektoratet.

**Status:** Uavklart — PoC bruker alternativ A (legen som part).

---

## C-2: HelseID — når skal BFF-siden validere tokenet?

**Problemstilling:**  
I dag stoler BFF-en (ASP.NET Core) på access token fra SMART-mock uten å validere signaturen. I produksjon med HelseID må tokenet valideres. Spørsmålet er *når* dette skal innføres og *hva* som kreves:

- JWT Bearer-validering mot HelseID sitt JWKS-endepunkt
- Krav til claims: `pid` (fnr), `helseid://claims/hpr/hpr_number`, `assurance_level`
- NHN Tillitsrammeverk-claims for behandlingsformål (`CareRelationshipPurposeOfUseCode`, etc.)
- Klientregistrering på selvbetjening.test.nhn.no (HelseID test)

**Konkrete oppgaver som er utsatt:**
1. Registrer klient på `selvbetjening.test.nhn.no`
2. Legg til `AddJwtBearer` i `Program.cs` med HelseID JWKS-URL
3. Trekk ut `pid`-claim og verifiser mot FHIR `Practitioner.identifier` (fnr)
4. Valider at `assurance_level >= high` (kreves for helseopplysninger)
5. Logg `hpr_number` + `pid` i audit trail

**Referanse:** Se IMPLEMENTERING.md §14 (HelseID kom-i-gang) for detaljert veiledning.

**Beslutter:** Teknisk team + NHN avtaleprosess.

**Status:** Dokumentert, ikke implementert. Blokkert av klientregistrering.

---

## C-3: Mottaksarkitektur — hvem er tjenesteeier?

**Problemstilling:**  
Når legen sender inn erklæringen, hva sendes hvor — og hvem eier tjenesten?

### Altinn Events — slik mottaksarkitektur faktisk fungerer

Altinn 3 varsler mottakere via **Altinn Events** når noe sendes inn. Det er ikke Altinn som pusher data til SVV, men SVV som abonnerer på events og henter instansen selv. Hvert mottakssystem:
1. Abonnerer på events av typen `app.instance.process.completed` for riktig app/org
2. Henter instansdata via Altinn Storage API med Maskinporten-token
3. Distribuerer til riktig fagsystem eller sak/arkiv-system

**FINT Arkiv-mønsteret** (brukt bl.a. av Helsedirektoratet) viser en etablert tilnærming der et felles mottakslag router innkommende Altinn-instanser til riktig fagsystem basert på tjenestenøkkel/ressurstype. Dette er relevant som mal for et eventuelt felles mottakssystem for helseattester.

### To-lags mottaksmodell — konklusjon til SVV, full attest til EPJ

Den eksisterende digitale løsningen overfører **kun konklusjonen** (grønt/rødt per gruppe + vilkår) til SVV/førerkortregisteret — ikke hele IS-2569. Full attest skrives tilbake til EPJ. Dette er det realistiske målbildet:

| Lag | Innhold | Mottaker | Kanal |
|---|---|---|---|
| **Konklusjon** | Gruppe 1/2/3: skikket/ikke skikket + begrenset varighet + vilkår | Statens vegvesen (førerkortregisteret) | Altinn Events → SVVs mottakssystem |
| **Full attest** | Komplett IS-2569 + legens vurderinger | EPJ-journal | FHIR `DocumentReference` writeback med SMART access token |

**Teknisk løsning i PoC — to separate datatyper:**

```json
{
  "dataTypes": [
    {
      "id": "forer-legeerklaering",
      "description": "Komplett IS-2569 — skrives tilbake til EPJ via DocumentReference",
      "appLogic": { "classRef": "ForerLegeerklaeringModel" },
      "taskId": "Task_1"
    },
    {
      "id": "forer-konklusjon",
      "description": "Konklusjon til SVV — grønt/rødt per gruppe + vilkår",
      "appLogic": { "classRef": "ForerKonklusjonModel" },
      "taskId": "Task_1"
    }
  ]
}
```

`ForerKonklusjonModel` populeres automatisk i `IDataProcessor.ProcessDataWrite` ved innsending, avledet fra den komplette modellen. Altinn Events varsler SVVs mottakssystem om at konklusjonen er klar; BFF skriver full attest til EPJ via `DocumentReference`.

### Tjenesteeieralternativer

| Alternativ | Beskrivelse | Avhengigheter |
|---|---|---|
| **A — Digdir (nåværende)** | Digdir eier tjenesten, placeholder for PoC | Ingen nye avtaler. Men Digdir er ikke naturlig mottaker. |
| **B — Statens vegvesen** | SVV som tjenesteeier; abonnerer på Altinn Events | Krever avtale om Events-abonnement og mottakssystem hos SVV. |
| **C — Helsedirektoratet** | Hdir som nasjonal koordinator med FINT Arkiv-routing | Mulig felles mottakslag for helseattester på tvers av fagsystemer. |

**Avhengigheter:** Valget påvirker `applicationmetadata.json` (`org`-felt), `policy.xml` (tjenesteeier-regel), og Maskinporten-scope for mottakssystemets henting av instansdata.

**Beslutter:** Programleder + juridisk avdeling + Statens vegvesen.

**Status:** Teknisk retning avklart (to-lags modell: konklusjon til SVV via Events, full attest til EPJ). Tjenesteeier og SVVs mottakssystem gjenstår som avklaring.

---

## C-4: Rettslig grunnlag og DPIA

**Problemstilling:**  
Behandling av helseopplysninger i dette systemet krever avklaring av rettslig grunnlag og en formell personvernkonsekvensvurdering (DPIA/DPIA).

**Relevante hjemler (foreløpig vurdering):**
- Helsepersonelloven § 45 — plikt til å utstede attest/erklæring på anmodning
- Pasientjournalloven § 6 — behandling av helseopplysninger i forbindelse med helsehjelp
- GDPR art. 9 nr. 2 bokstav h — behandling nødvendig for medisinske formål av helsepersonell

**Åpne spørsmål:**
1. Er Digdir behandlingsansvarlig eller databehandler? Hvem er behandlingsansvarlig i siste instans (SVV? Hdir? Legekontoret?)?
2. Trenger Altinn-appen en egen behandlingsprotokoll (art. 30)?
3. Er det krav om DPIA (art. 35) gitt systematisk behandling av helseopplysninger i stor skala?
4. Hva er minimumsloggingskravet (art. 5 nr. 2 — ansvarlighetsprinsippet)?
5. Krav til databehandleravtale mellom Digdir og EPJ-leverandøren?

**Beslutter:** Personvernombud + juridisk rådgiver + evt. Datatilsynet (forhåndskonsultasjon).

**Status:** Uavklart. Se KRAVSPESIFIKASJON-v0.6.md §7 for utdyping.

---

## C-5: Fullstendig IS-2569 og pasientens egenerklæring (NA-0201)

**Problemstilling:**  
Nåværende datamodell (`ForerLegeerklaeringModel`) dekker kun en liten del av blankett IS-2569 (Helseattest for førerkort). Full implementering krever:

**Del A — Manglende felt i IS-2569:**
- Alle 17 helsekategorier med klinisk vurdering (synsfunksjon, hørsel, bevegelsesapparat, hjerte/kar, nevrologi, psykisk helse, kognitiv funksjon, søvn, diabetes, nyre, lever, kreft, øre/nese/hals, muskel/skjelett, rusmidler, medikamenter)
- Vilkår og begrensninger per kategori (kodeverk fra Statens vegvesen)
- Legens underskrift og dato
- Erklæring om at pasienten er informert

**Del B — Pasientens egenerklæring (NA-0201):**
- 17 ja/nei-spørsmål om helsetilstand
- Digital flyt via Dialogporten (se PASIENTFLYT.md for arkitekturforslag)
- Mapping mellom NA-0201-svar og IS-2569 helsekategorier
- FHIR QuestionnaireResponse for å overføre svar til legen

**Prioritering:**
- PoC: Kun grunndata (navn, fnr, HPR, org, vurdering) — ferdig
- v1.0: Full IS-2569 dekningsgrad — krever feltmapping-arbeid med Helsedirektoratet
- v2.0: Digital NA-0201 via Dialogporten — krever ny avtale og tjenesteutvikling

**Beslutter:** Produkteier + Helsedirektoratet (IS-2569 eier) + Statens vegvesen (NA-0201 eier).

**Status:** Datamodell og feltstruktur dokumentert i SKJEMA-IS2569.md. Implementering utsatt.

---

---

## C-6: Altinn Studio vs. Helsenorge — plattformvalg for IS-2569

**Problemstilling:**  
NHN har allerede en produksjonssatt løsning for legeerklæring IS-2569 bygget på Helsenorge-plattformen (se [NHN-DOKUMENTASJON.md](NHN-DOKUMENTASJON.md) og [SAMMENLIGNING-syk-inn.md](SAMMENLIGNING-syk-inn.md)). Digdir PoC-en viser at det *også* er mulig på Altinn Studio. Spørsmålet er hvilken plattform som er riktig for en eventuell produksjonssetting.

| Alternativ | Fordeler | Ulemper |
|---|---|---|
| **A — Altinn Studio (nåværende kurs)** | Signering, arkiv, XACML, ID-porten gratis; Digdir eier plattformen | Mottaksarkitektur til SVV uavklart; HelseID krever ekstraarbeid; NHN har gjort det samme |
| **B — Helsenorge-plattformen** | HelseID nativt; NA-0201 tilgjengelig; SVV-integrasjon eksisterer; NHN allerede i prod | Digdir er ikke eier; krever samarbeid med NHN; annen teknologistack |
| **C — Hybridmodell** | Altinn for signering/arkiv, Helsenorge for innbyggerflyt (NA-0201) | To integrasjoner, mer kompleksitet |

**Avhengigheter:** C-3 (mottaksarkitektur SVV), C-4 (behandlingsansvar).

**Ny innsikt (2026-06-17):** NHNs Slack-kanal `ext-utv-hn-forerrett` tilbyr samarbeid med NHN-teamet. Kontakt bør tas for å avklare om PoC-funnene kan bidra inn i eksisterende løsning fremfor å parallelt-utvikle en Altinn-variant.

**Beslutter:** Programleder + NHN + Statens vegvesen.

**Status:** Ny åpen beslutning — uavklart.

---

---

## C-7: DSOP + SMART on FHIR mot privat sektor (forsikring)

**Problemstilling:**  
Forsikringsbransjen er en stor og manuell konsument av legeerklæringer (ved tegning, skadesak og sakkyndig-oppdrag). Det finnes allerede en kanal mot Finans Norge via Helsenettet. DSOP-rammeverket (Digital Samhandling Offentlig–Privat) er brukt for bank/NAV-dataflyt. Spørsmålet er om Digdir bør initiere eller støtte arbeid med en SMART on FHIR-flyt mot forsikring innenfor dette rammeverket.

| Alternativ | Beskrivelse | Konsekvens |
|---|---|---|
| **A — Avvent** | Vent til offentlig sektor-pilotene er modne og tillitsankeret hos NHN er operasjonelt | Lavere risiko, ingen ny governance nå |
| **B — Utred** | Initier dialog med Finans Norge/Bits og NHN om DSOP-flyt for legeerklæringer | Kan sette Digdir i drivesetet for privatsektor-digitalisering |
| **C — Bidra inn** | Lever referansearkitektur fra `forer`-PoC som grunnlag for et privat-sektor-spor | Gjenbruk uten å eie løpet selv |

**Forutsetninger som må avklares uansett:**
- Samtykke og taushetsplikt: innbygger-/pasientstyrt flyt, dataminimering (GDPR art. 9)
- Legens sakkyndig-rolle: forsvarlighet, habilitet, objektivitet — SoF prefiller, legen vurderer
- Governance: hvem godkjenner apper mot privat mottaker (EPJ-leverandør, NHN tillitsanker)

**Avhengigheter:** C-6 (plattformvalg), NHN tillitsanker-modenhet, DSOP syke-/uføreforsikring-prosjekt.

**Beslutter:** Programleder + juridisk avdeling + Finans Norge / Bits.

**Status:** Ny åpen beslutning — uavklart. Se [KARTLEGGING-kandidater.md](KARTLEGGING-kandidater.md) G1/G2 for grunnlag.

---

## Beslutningslogg

| Dato | Beslutning | Besluttet av | Status |
|---|---|---|---|
| 2026-06-16 | PoC bruker legen som part (C-1 alt. A) | JSF | Midlertidig |
| 2026-06-16 | HelseID-validering utsettes til post-PoC (C-2) | JSF | Unblokket 2026-06-17 (klient registrert) |
| 2026-06-16 | Digdir som tjenesteeier-placeholder (C-3 alt. A) | JSF | Midlertidig |
| 2026-06-16 | DPIA-avklaring krever juridisk ressurs (C-4) | JSF | Åpen |
| 2026-06-16 | Full IS-2569 utsettes til v1.0 (C-5) | JSF | Planlagt |
| 2026-06-17 | Altinn Studio vs. Helsenorge — plattformvalg (C-6) | JSF | Ny åpen beslutning |
| 2026-06-17 | DSOP + SMART on FHIR mot forsikring/privat sektor (C-7) | JSF | Ny åpen beslutning |
