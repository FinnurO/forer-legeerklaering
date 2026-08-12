# Pasientflyt: Egenerklæring og legeattestprosessen — førerrett

**Dato:** 2026-06-16 (oppdatert 2026-08-12)  
**Status:** Arkitekturforslag — utenfor PoC-scope, men nødvendig å adressere. Alternativ B sin autentisering er nå teknisk verifisert, se §4.

**Viktig presisering (2026-08-12):** Dette dokumentet beskriver **innbyggerens** del av flyten — pasienten som fyller ut egenerklæring (NA-0201). Dette er et vanlig **Altinn-skjema**, **ikke SMART on FHIR**. Kvitteringen havner i Altinn innboks (standard) og skal *i tillegg* havne i Helsenorge (se «DokumentAPI»-avsnittet under §4). Dette er en helt annen integrasjon enn **legens writeback til EPJ** (som *er* SMART on FHIR, se [IMPLEMENTERING.md §13](IMPLEMENTERING.md)) — de to må ikke blandes, selv om begge til slutt handler om samme legeerklæring.

---

## 1. Bakgrunn og problem

I dagens papirbaserte flyt:

1. Pasient bestiller time hos fastlege (for helseattest førerrett)
2. Pasient laster ned, skriver ut og fyller ut **egenerklæring om helse** (blankett NA-0201, Statens vegvesen)
3. Pasient medbringer utfylt papirskjema til konsultasjonen
4. Lege gjennomgår egenerklæringen og fyller ut **helseattest IS-2569**
5. Lege sender IS-2569 til Helsedirektoratet/Statens vegvesen

**Problemet:** Egenerklæringen er ikke digitalt tilgjengelig for legen i EPJ. Legen må manuelt taste inn informasjon fra papir, og pasienten risikerer å glemme skjemaet. Flyten er heller ikke sporbar eller arkivert digitalt.

**Målet:** Pasienten fyller ut egenerklæringen digitalt *før* konsultasjonen. Når legen åpner legeerklæringen i Altinn-appen, er pasientens egenerklæring allerede tilgjengelig som grunnlag.

---

## 2. Egenerklæringsskjemaet (NA-0201)

Blankett NA-0201 (Statens vegvesen, 2017) har to deler:

### Del 1 — Søknad om førerkort/kompetansebevis

| Felt | Beskrivelse |
|---|---|
| Etternavn, fornavn, mellomnavn | |
| Fødselsnummer (11 siffer) | |
| Adresse, postnummer, poststed | |
| Telefon, mobilnummer, e-post | |
| Ønsket målform (bokmål/nynorsk) | |
| Søknaden gjelder | Første gang / utvidelse / fornyelse / innbytte / tilbakelevering / kompetansebevis |
| Førerkortklasse ønsket | AM145/146/147, S, T, A1, A2, A, B1, B, B96, BE, C1, C1E, C, CE, D1, D1E, D, DE + kompetansebevis |
| Utenlandsk førerkort | Ja/Nei, utstedelsesland, klasse |

### Del 2 — Egenerklæring om helse (17 spørsmål, Ja/Nei)

| Nr | Spørsmål | Trigger |
|---|---|---|
| 1 | Nedsatt synsstyrke, behov for briller/linser? | Synsattest fra lege/optiker |
| 2 | Dobbeltsyn siste 3 måneder, problemer i mørke, nedsatt sidesyn? | Synsattest fra optiker/øyelege |
| 3 | Problemer med å orientere seg i trafikken? | Helseattest lege |
| 4 | Nevrologisk sykdom (nå eller tidligere)? | Helseattest lege |
| 5 | Besvimelse, krampe, nedsatt bevissthet siste 5 år? | Helseattest lege |
| 6 | Besvimelse/krampe siste 10 år, eller epilepsimedisiner? | Helseattest lege |
| 7 | Obstruktivt søvnapné syndrom eller annen søvnsykdom? | Helseattest lege |
| 8 | Hjerte-/karsykdom (nå eller tidligere)? | Helseattest lege |
| 9 | Diabetes? | Helseattest lege |
| 10 | Alvorlig psykisk lidelse eller psykisk svekkelse? | Helseattest lege |
| 11 | ADHD? | Helseattest lege |
| 12 | Legemidler som kan påvirke kjøringen? | Helseattest lege |
| 13 | Misbruk av alkohol/rusmidler siste 3 år? | Helseattest lege |
| 14 | Svekket lungefunksjon? | Helseattest lege |
| 15 | Alvorlig nyresvikt (nå eller tidligere)? | Helseattest lege |
| 16 | Nedsatt førlighet i arm eller ben? | Helseattest lege |
| 17 | Andre helsemessige forhold som kan svekke kjøreevnen? | Helseattest lege |

**Logikk:**
- Kun spm. 1 besvart «ja» → synsattest holder
- Spm. 2 besvart «ja» → optiker/øyelege synsattest
- Spm. 3–17 besvart «ja» (gruppe 1) → helseattest IS-2569 fra lege
- Tunge klasser (C/D og utrykning) → helseattest alltid påkrevd

---

## 3. Foreslått digital flyt

### Overordnet sekvens

```
Pasient                    helsenorge.no / Altinn      EPJ / Altinn-app (lege)
   │                              │                            │
   │── Bestiller time ──────────► │                            │
   │                              │◄─── EPJ oppretter ─────────┤
   │                              │     Dialogporten-dialog    │
   │◄─── Varsel (SMS/e-post) ─────│                            │
   │                              │                            │
   │── Åpner dialog ─────────────►│                            │
   │── Fyller ut egenerklæring ───►│                            │
   │── Signerer (ID-porten) ──────►│                            │
   │                              │── Lagrer som FHIR ────────►│
   │                              │   QuestionnaireResponse    │
   │                              │                            │
   │                              │           [Konsultasjon]   │
   │                              │                            │
   │                              │          ◄── SMART launch ─┤
   │                              │          ◄── Henter QuR ───┤
   │                              │          Prefiller IS-2569  │
   │                              │          Lege kompletterer  │
   │                              │          Sender inn ───────►│ Helsedir/Vegvesen
```

### Steg 1 — Timebestilling trigger dialog

Når pasienten bestiller time (via helsenorge.no booking eller EPJ-resepsjonen), oppretter EPJ-systemet en **Dialogporten-dialog** for pasienten:

```
POST https://dialogporten.altinn.no/api/v1/serviceowner/dialogs
{
  "serviceResource": "urn:altinn:resource:forer-egenerklaring",
  "party": "urn:altinn:person:identifier-no:01039012345",
  "status": "New",
  "dueAt": "2026-07-01T09:00:00Z",
  "title": [{ "value": "Egenerklæring om helse — førerrett", "languageCode": "nb" }],
  "body": [{ "value": "Du har bestilt time for helseattest. Fyll ut egenerklæringen før konsultasjonen.", "languageCode": "nb" }],
  "guiActions": [{
    "action": "open",
    "url": "https://altinn.no/forer-egenerklaring/...",
    "title": [{ "value": "Fyll ut egenerklæring", "languageCode": "nb" }],
    "priority": "Primary"
  }]
}
```

Dialogen vises i:
- **helsenorge.no** innboks (via Dialogporten-integrasjon)
- **Altinn innboks** (alternativt)
- SMS/e-post-varsel til pasienten

### Steg 2 — Pasient fyller ut egenerklæring

Pasienten åpner linken fra dialogen og fyller ut egenerklæringen i en **Altinn-app** (`forer-egenerklaring`):

- Autentiseres via ID-porten (nivå 3 — samme som Altinn generelt)
- Personopplysninger prefilles fra Folkeregisteret
- De 17 Ja/Nei-spørsmålene fylles ut
- Pasienten signerer digitalt
- Skjemaet lagres i Altinn og/eller skrives tilbake som FHIR QuestionnaireResponse

### Steg 3 — FHIR QuestionnaireResponse

Den utfylte egenerklæringen lagres som en **FHIR QuestionnaireResponse** knyttet til pasienten:

```json
{
  "resourceType": "QuestionnaireResponse",
  "questionnaire": "https://vegvesen.no/fhir/Questionnaire/egenerklaring-helse",
  "status": "completed",
  "subject": { "reference": "Patient/sophie-salt" },
  "authored": "2026-07-01T08:30:00+02:00",
  "item": [
    { "linkId": "spm1", "text": "Nedsatt synsstyrke?", "answer": [{ "valueBoolean": false }] },
    { "linkId": "spm9", "text": "Diabetes?", "answer": [{ "valueBoolean": true }] }
  ]
}
```

### Steg 4 — Legen henter egenerklæringen

Når legen starter SMART EHR Launch og Altinn-appen prefiller IS-2569, henter BFF-en også pasientens QuestionnaireResponse:

```csharp
// I FhirPrefillService.cs
var qrUrl = $"{ctx.FhirBaseUrl}/QuestionnaireResponse" +
    $"?subject=Patient/{patientId}" +
    $"&questionnaire=https://vegvesen.no/fhir/Questionnaire/egenerklaring-helse" +
    $"&status=completed&_sort=-authored&_count=1";
var egenerklaring = await TryGetFhirResource(client, qrUrl, "QuestionnaireResponse");
```

Relevante svar fra egenerklæringen kan:
- Vises som kontekst for legen (ikke redigerbart — pasientens eget svar)
- Trigge automatisk flagging (f.eks. «Pasienten har svart ja på spm. 9 — diabetes»)
- Foreslå hvilke helsekategorier i IS-2569 legen bør vurdere

---

## 4. Arkitektoniske valg

### Alternativ A — Altinn + Dialogporten (anbefalt)

| Komponent | Teknologi | Ansvar |
|---|---|---|
| Pasientskjema | Altinn-app `forer-egenerklaring` | Statens vegvesen / Helsedirektoratet |
| Dialogoppretting | Dialogporten API (fra EPJ via Maskinporten) | EPJ-leverandør |
| Pasientvisning | helsenorge.no (Dialogporten-integrasjon) eller Altinn | NHN / Digdir |
| Lagring | Altinn Storage + FHIR QuestionnaireResponse | Begge |
| Legehenting | FHIR API fra Altinn BFF | Vår app |

**Fordeler:** Bruker eksisterende nasjonal infrastruktur; helsenorge.no-visning er naturlig for pasienten; Altinn-arkivering gir sporbarhet.

**Utfordringer:** Krever at Dialogporten er tilgjengelig og at helsenorge.no viser dialogen; krever Maskinporten-autentisering fra EPJ for dialogoppretting.

### Alternativ B — Helsenorge EksternAPI (Oppgave + Skjema) — nå konkretisert og delvis verifisert

**Oppdatert 2026-08-10:** dette var tidligere en vag skisse ("Helsenorge.no har egne skjematjenester"). Nå vet vi konkret hvordan det fungerer og har verifisert autentiseringen. Se [IMPLEMENTERING.md §14.1](IMPLEMENTERING.md) for full teknisk detalj.

Helsenorge tilbyr et **maskin-til-maskin API** (`eksternapi.helsenorge.no`) med to relevante tjenester, hver med eget HelseID-scope:
- **Oppgave** (`nhn:helsenorge.eksternapi/oppgave`) — sender en oppgave (FHIR `Task`) til en innbygger, som varsles på helsenorge.no og må gjøre et aktivt valg for å åpne den.
- **Skjema** (`nhn:helsenorge.eksternapi/skjema`) — knyttet til selve skjemaet innbyggeren fyller ut.

Dette er trolig nøyaktig samme mekanisme NHNs egen produksjons-Førerrett-App bruker for å nå pasienten — [Helsenorge sin egen dokumentasjon](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/3262578689/Nye+IP+adresser+for+EksternAPI+og+F+rerrett) bekrefter at EksternAPI og Førerrett-appen deler infrastruktur.

**Autentisering (verifisert i test):** OAuth2 `client_credentials` + `private_key_jwt` + DPoP — ingen brukerredirect, siden det er systemet (Altinn-appen) som kaller Helsenorge, ikke pasienten som logger inn. Se [local-dev/helseid-token-test/](../local-dev/helseid-token-test/) for kjørbar bekreftelse.

**Ikke verifisert ennå:** selve Oppgave/Skjema-kallet (FHIR `Task` via `POST .../oppgave/v1/Bundle`), og om `orgnr_parent` må sendes med i API-kallet.

**Fordeler:** Integrert i pasientens vanlige helseportal; samme infrastruktur som NHNs egen løsning, altså sannsynligvis godt utprøvd og driftssikkert; unngår avhengighet av at Dialogporten viser dialogen på helsenorge.no (usikkert punkt i alternativ A).  
**Utfordringer:** Egen integrasjon å vedlikeholde ved siden av SMART-launch-integrasjonen mot legen; skjemastruktur/payload-format for Oppgave/Skjema er ikke utforsket; uklart om NA-0201 kan modelleres direkte i Skjema-tjenesten eller om det krever en egen avtale med NHN om skjemainnhold.

### Anbefaling

**Alternativ B (Helsenorge EksternAPI) bør utforskes videre før A velges endelig** — nå som autentiseringen er verifisert og vi vet at det er samme plattform NHN selv bruker for førerrett, er den tekniske usikkerheten redusert sammenlignet med da alternativ A ble anbefalt (2026-06-16). Alternativ A (Dialogporten) er fortsatt en gyldig arkitektur og gjenbruker eksisterende infrastruktur, men krever mer avklaring rundt hvordan Dialogporten faktisk vises på helsenorge.no. Neste steg: forsøk et faktisk Oppgave-kall (se IMPLEMENTERING.md §14.1 "ikke verifisert ennå") for å avgjøre hvilket alternativ som er raskest til en fungerende pasientflyt.

### DokumentAPI — kvittering til Helsenorge (funn 2026-08-12)

Presisert av Johann: når innbyggeren fyller ut egenerklæringen **i Altinn**, skal kvitteringen havne i Altinn innboks (standard) **og** i Helsenorge. Fant den relevante mekanismen: [Helsenorge DokumentAPI](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/1820623169) (`POST <BaseUrl>/dokumenter/api/v1/SaveDokument`) — «lagre dokument i innbyggers helsearkiv på Helsenorge... som informasjon til innbygger, eventuelt innbyggers kopi».

**To ting å være klar over før dette kan brukes:**
1. **Innholdstype-begrensning:** dokumentasjonen sier eksplisitt at API-et «pr. i dag kun» tilbyr lagring av «resultat fra Verktøy der det utføres egenkartlegging» (`innholdType = Egenkartlegging`, kode 6, eneste definerte verdi for eksterne systemer). En egenerklæring (NA-0201, pasienten svarer selv på 17 ja/nei-spørsmål om egen helse) er i praksis nettopp en egenkartlegging — dette skiller seg fra **legens** legeerklæring (IS-2569, en klinisk vurdering), som ikke ville kvalifisert under denne kategorien. Bør bekreftes med NHN at NA-0201-kvitteringen faktisk faller innenfor definisjonen, men den språklige treffsikkerheten er lovende.
2. **Autentisering er brukertilstedeværelse-basert:** DokumentAPI krever «en innlogget innbygger» med AksessToken via «sømløs pålogging» (Helsenorge som OpenID Connect-provider) — altså en helt annen autentiseringsmodell enn `client_credentials`-flyten vi allerede har verifisert for Oppgave/Skjema (IMPLEMENTERING.md §14.1). Å kalle DokumentAPI fra Altinn krever at Altinn-appen får en Helsenorge-OIDC-token for den *samme* innbyggeren som er innlogget i Altinn — en SSO/token-utvekslings-utfordring vi ikke har utforsket, analog til HelseID-pid-matchingen beskrevet for legen i §14.

**Testmiljøtilgang:** samme begrensning som Oppgave-API-et — «oppsett av API-klient i test-miljø(er) avtales som en del av kundeoppkoplingen» (se [RISIKOREGISTER.md R9](RISIKOREGISTER.md)).

---

## 5. Mapping: Egenerklæring → IS-2569

Pasientens svar på egenerklæringen korresponderer direkte med helsekategoriene legen vurderer:

| Egenerklæring (NA-0201) | IS-2569 helsekategori |
|---|---|
| Spm. 1–2: Syn | Kategori 1: Syn |
| Spm. 4: Nevrologisk | Kategori 6: Nevrologiske sykdommer |
| Spm. 5–6: Besvimelse/epilepsi | Kategori 5: Epilepsi / bevissthetsforstyrrelser |
| Spm. 7: Søvnapné | Kategori 11: Søvnsykdommer |
| Spm. 8: Hjerte-/kar | Kategori 3: Hjerte- og karsykdommer |
| Spm. 9: Diabetes | Kategori 4: Diabetes |
| Spm. 10: Psykisk lidelse | Kategori 8: Psykiske lidelser |
| Spm. 11: ADHD | Kategori 9: ADHD |
| Spm. 12: Legemidler | Kategori 13: Legemidler |
| Spm. 13: Rus | Kategori 14: Misbruk av rusmidler |
| Spm. 14: Lungefunksjon | Kategori 10: Lungesykdommer |
| Spm. 15: Nyresvikt | Kategori 12: Nyresykdommer |
| Spm. 16: Førlighet | Kategori 2: Bevegelse/førlighet |
| Spm. 17: Andre forhold | Legens skjønn |

**Viktig:** Pasientens egenerklæring er *ikke* medisinsk vurdering. Den vises som kontekst for legen, men legen foretar sin selvstendige kliniske vurdering i IS-2569.

---

## 6. Dataflyt og personvern

```
Pasient (fnr 01039012345)
    │
    ├─► Egenerklæring (NA-0201)
    │       Lagres i: Altinn + FHIR QuestionnaireResponse
    │       Tilgang: Pasienten selv + behandlende lege (via SMART-token)
    │
    └─► Samtykke til legen (innebygd i skjema):
            "Jeg gir legen fullmakt til å innhente nødvendige og relevante
             helseopplysninger fra spesialist og tidligere fastlege"
```

**Personvernkrav:**
- Egenerklæringen inneholder helseopplysninger — særskilt kategori etter GDPR art. 9
- Tilgangen må begrenses til behandlende lege i den aktuelle konsultasjonen
- FHIR QuestionnaireResponse bør ha `meta.security`-tagging og tilgangskontroll i EPJ
- Samtykket pasienten signerer i egenerklæringen dekker legens bruk — bør knyttes til den aktuelle konsultasjonen (Encounter-referanse)

---

## 7. Hva dette krever av nye komponenter

| Komponent | Ny? | Beskrivelse |
|---|---|---|
| Altinn-app `forer-egenerklaring` | **Ja** | Pasientens egenerklæringsskjema |
| Dialogporten-integrasjon i EPJ | **Ja** | EPJ oppretter dialog ved timebestilling |
| FHIR Questionnaire (NA-0201) | **Ja** | Formalisering av skjemaet som FHIR-ressurs |
| FHIR QuestionnaireResponse-støtte i BFF | **Ja** | Henting og visning for lege |
| helsenorge.no Dialogporten-visning | Eksisterer | NHN viser Dialogporten-dialogen |
| Altinn Autorisasjon for egenerklæring | **Ja** | Tilgangsstyring pasient → lege |

---

## 8. Scope og prioritering

| Fase | Innhold |
|---|---|
| **PoC (nå)** | Papirflyt — pasienten medbringer NA-0201 på papir. Ikke implementert digitalt. |
| **v1.0** | Pasient fyller ut egenerklæring digitalt i Altinn-app. Manuell link-distribusjon (ingen Dialogporten). Legen ser utfylt egenerklæring i Altinn BFF. |
| **v2.0** | Dialogporten-integrasjon: EPJ oppretter dialog automatisk ved timebestilling. Vises i helsenorge.no. |
| **v3.0** | Fullstendig FHIR QuestionnaireResponse-flyt: prefill IS-2569 fra egenerklæring. Samtykke og tilgangsstyring via Tillitsrammeverk. |

---

## 9. Referanser

| Kilde | URL |
|---|---|
| Egenerklæring NA-0201 (Statens vegvesen) | https://www.vegvesen.no/globalassets/forerkort/ta-forerkort/soknad-om-forerkort-og-kompetansebevis-egenerklaering-om-helse.pdf |
| Helseattest IS-2569 (Helsedirektoratet) | Se `docs/SKJEMA-IS2569.md` |
| Dialogporten dokumentasjon | https://docs.altinn.studio/en/dialogporten/ |
| helsenorge.no | https://www.helsenorge.no/ |
| FHIR Questionnaire (HL7) | https://hl7.org/fhir/R4/questionnaire.html |
| FHIR QuestionnaireResponse (HL7) | https://hl7.org/fhir/R4/questionnaireresponse.html |
