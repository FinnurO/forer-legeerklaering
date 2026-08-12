# Dobbel inngangsmodus: SMART on FHIR vs. normal Altinn-pålogging

**Dato:** 2026-08-12 (oppdatert 2026-08-13 — arkitektur for §3.2–3.4 besluttet, se [VEIKART.md fase 1b](VEIKART.md))
**Bakgrunn:** Ikke alle EPJ-leverandører har, eller kommer til å, implementere SMART on FHIR. Legen må derfor kunne bruke appen selv om EPJ-en ikke støtter SMART-launch — ved å logge inn i Altinn direkte («normal portalpålogging», ID-porten), på samme måte som enhver annen Altinn-tjeneste.

**Kjernepåstanden dette analyserer:** at en Altinn Studio-app kan håndtere *begge* behov i samme app er en reell differensiator mot alternativene (en frittstående SMART-app, eller en EPJ-innebygd løsning, forutsetter begge SMART-launch og fungerer ikke uten). Dette bør bli en eksplisitt del av «helse-template»-visjonen i [STRATEGI.md](STRATEGI.md) Spor B.

---

## 1. De to inngangene

| | A — SMART EHR Launch | B — Normal Altinn-pålogging |
|---|---|---|
| **Forutsetning** | EPJ støtter SMART App Launch | Ingen — fungerer for enhver Altinn-bruker |
| **Hvordan legen kommer inn** | EPJ åpner appen med `iss`/`launch`-parametere | Legen finner tjenesten i Altinn (portal/tjenesteoversikt) og logger inn med ID-porten som normalt |
| **Pasientkontekst** | Kjent fra launch (patient-ID, encounter-ID) | Ukjent — må oppgis manuelt |
| **FHIR-prefill** | Automatisk (`FhirPrefillService.ProcessDataRead`) | Ikke mulig — ingen FHIR-tilkobling finnes |
| **Status i dag** | ✅ Fullstendig verifisert (IMPLEMENTERING.md §13) | ⚠️ Fungerer trolig som utilsiktet bivirkning — aldri designet eller testet bevisst |

---

## 2. Hva fungerer allerede i dag (verifisert 2026-08-12)

Testet direkte: hentet en helt normal Altinn-testbruker-JWT fra localtest (samme mekanisme som `/smart/dev-login` bruker internt, men uten å gå via noen SMART-kode i det hele tatt), satte den som cookie, og lastet appens rot-URL. Resultat: `HTTP 200`, SPA-skallet laster uten feil.

`FhirPrefillService.ProcessDataRead` er allerede skrevet defensivt:
```csharp
if (string.IsNullOrEmpty(tokenJson) || string.IsNullOrEmpty(contextJson))
{
    _logger.LogInformation("No SMART context found in session or cache — skipping FHIR pre-fill");
    return;
}
```
Ingen exception, ingen krasj — skjemaet vises rett og slett tomt, klart for manuell utfylling.

**Konklusjon:** Den grunnleggende dobbelmodusen fungerer allerede — men kun som en tilfeldig bivirkning av defensiv koding, ikke som en bevisst designet og testet funksjon. Det er forskjellen mellom «krasjer ikke» og «er en god opplevelse for legen».

---

## 3. Hva som mangler for at dette skal være en bevisst, veldesignet dobbelmodus

### 3.1 Eksplisitt modus-signalisering til legen

I dag: ingen indikasjon om *hvorfor* feltene er tomme. Legen bør se en tydelig melding — «Ingen pasientdata funnet automatisk, fyll ut manuelt» vs. «Data hentet fra journalsystemet — kontroller og suppler».

**Trengs:** et modus-felt i datamodellen (f.eks. skjult `Prefill_Kilde: "SMART" | "Manuell"`), satt tidlig i prosessen, og betinget visning i Altinn-layouten (støttes allerede av rammeverket — betinget synlighet basert på feltverdier er en standard Altinn-komponent).

### 3.2 Manuell pasientidentifikasjon i normal-modus

**Oppdatert 2026-08-13 — løst konkret.** I SMART-modus kommer pasient-ID fra launch-konteksten. I normal-modus må legen selv oppgi hvem erklæringen gjelder. Altinn Studio har en innebygd komponent for nettopp dette: **`PersonLookup`** («Finn person») — søk i Folkeregisteret, verifisert i offisiell Altinn Studio-komponentdokumentasjon. `OrganisationLookup` er tilsvarende for virksomhet/orgnr, relevant hvis legekontoret må identifiseres separat. Se [VEIKART.md fase 1b](VEIKART.md) — kan bygges nå, krever ingen nye klientregistreringer.

### 3.3 Delvis autofylling av legens egne opplysninger, uavhengig av SMART

Viktig innsikt: Altinn vet allerede hvem som er innlogget (ID-porten-fødselsnummer) — **også uten SMART-kontekst**. Det betyr at legens navn/fnr i prinsippet kan hentes fra Altinn-identiteten i *begge* moduser. Kun pasient- og konsultasjonsdata er avhengig av SMART.

**Oppdatert 2026-08-13 — arkitektur besluttet, ikke bygget ennå.** HPR-nummer er ikke tilgjengelig fra en vanlig ID-porten-pålogging. Undersøkt to veier:

1. **Kunne HelseID vært Altinns egen innloggingsmetode** (i stedet for ID-porten), slik at `hpr_number`-claimet kom automatisk? **Nei, ikke uten videre** — Altinn Platform sin native innlogging støtter i dag kun ID-porten, Feide og UIDP som godkjente identitetsleverandører ([kilde](https://docs.altinn.studio/technology/architecture/capabilities/runtime/security/authentication/oidcproviders/)). Å legge til HelseID der er en plattformendring Altinn Studio-teamet selv må gjøre — ikke noe denne appen kan løse alene. Bekreftet av Johann: «Det finnes ikke støtte for HelseID pålogging mot Altinn Studio apper... men det er mulig med OAuth og har blitt gjort med Feide.»
2. **Løsningen: en app-intern tilleggsflyt.** Legen logger inn i Altinn helt normalt (ID-porten, uendret), og appen tilbyr en *egen* HelseID-autorisasjonsflyt inni skjemaet (samme mønster som `SmartLaunchController`, bare trigget av legen selv). Etter callback: hent `hpr_number`-claim, slå opp i [HPR Offentlig API](https://utviklerportal.nhn.no/informasjonstjenester/helsepersonellregisteret) (`GET /v1/personer/{hpr_number}`, Maskinporten-sikret, scope `nhn:hpr/basic` — selvbetjent, ingen Helsedirektoratet-godkjenning nødvendig siden vi kun trenger oppslag på HPR-nummer, ikke fnr) for å bekrefte navn og autorisert rolle.

Se [VEIKART.md fase 1b](VEIKART.md) for full tiltaksliste. **Status:** venter på to klientregistreringer (HelseID authorization_code-klient, Maskinporten `nhn:hpr/basic`-klient) — ikke igangsatt.

**Bekreftet presedens (2026-08-13):** Johann fant et konkret eksempel fra en annen, produksjonssatt Altinn Studio-app — «Apotekdrift» (DMP/tidl. Statens legemiddelverk, apotektillatelser) — som gjør nøyaktig denne typen HPR-validering i dag. Bekrefter:
- **Konfigurasjonsmønster:** `{AppNavn}-MaskinportenSettings` i `appsettings.Production.json`, med `HprApiEndpoint` (samme URL vi fant: `https://api.offentlig.hpr.nhn.no/`) og `Authority` (`https://maskinporten.no/`).
- **`Utdannelse`-feltet er også relevant** — HPR-oppslaget returnerer ikke bare navn og HPR-nummer, men også autorisasjonstype/utdannelse. For oss betyr det at bekreftelsen bør sjekke at personen faktisk er autorisert som **lege** spesifikt, ikke bare at HPR-nummeret finnes og er gyldig for en eller annen helsepersonellkategori.
- **Valideringsmønster:** Apotekdrift bruker en `ValidateHprNumber(...)`-metode knyttet til en spesifikk side/felt i skjemaet (`Page.FilialstatusInfoStedligLeder`) med selector-uttrykk som peker til nøyaktige datamodell-felt (`m => m.Meldingsdel...hprNummer`) — dette er trolig et **side-/felt-nivå valideringsmønster** (validering skjer når legen når et bestemt steg, med feilmelding på spesifikke felt), ikke nødvendigvis en hard blokkering ved instansiering (`IInstantiationValidator`). Verdt å vurdere dette mønsteret som et alternativ til/supplement for §3.4 sin `IInstantiationValidator`-idé — kan gi bedre brukeropplevelse (feilmelding i konteksten der den oppstår, fremfor å nekte oppstart av hele tjenesten).

### 3.4 Tilgangsstyring — hvem har lov til å starte tjenesten normalt?

I SMART-modus er det implisitt bare en lege som kommer inn (via EPJ-ens egen autentisering, og på sikt HelseID). I normal Altinn-modus kan i prinsippet **enhver Altinn-bruker** starte tjenesten i dag — `applicationmetadata.json` sin `partyTypesAllowed: {person: true}` har ingen ytterligere sjekk på hvem denne personen er.

Akseptabelt for en PoC. For produksjon bør en `IInstantiationValidator` (Altinn.App.Core-grensesnitt — bekreftet å finnes i vår installerte versjon 8.6.4 via refleksjon) sjekke at personen faktisk er autorisert helsepersonell — **samme HPR-bekreftelse som §3.3 løser dette også**, siden en vellykket HPR-oppslags-bekreftelse i praksis *er* autorisasjonssjekken. Ingen separat mekanisme nødvendig utover det som allerede er planlagt der.

### 3.5 Riktig sted å oppdage modus tidlig: instansieringshooken

Fant `IInstantiationProcessor.DataCreation(Instance instance, object data, Dictionary<string,string> prefill)` (verifisert via refleksjon mot installert `Altinn.App.Core` 8.6.4) — kjører ved selve instansopprettelsen, **før** `ProcessDataRead`. Dette er et bedre sted å sette modus-indikatoren (§3.1) enn å vente til `ProcessDataRead`, siden den kjører uansett hvilken vei brukeren kom inn, og gir et tidligere og mer pålitelig signal.

---

## 4. Hva dette betyr for «helse-template»-visjonen (Spor B)

[STRATEGI.md](STRATEGI.md) og [VEIKART.md](VEIKART.md) fase 4 omtaler i dag `Digdir.SmartOnFhir` som en NuGet-pakke for selve SMART-protokollen alene. Basert på denne analysen bør malen dekke **mer enn SMART-protokollen** — selve dobbelmodus-mønsteret bør være et førsteklasses konsept i malen, ikke en bivirkning:

- En felles `IDataProcessor`-baseklasse/mal som håndterer «har vi SMART-kontekst eller ikke» defensivt og eksplisitt (det vi allerede har implisitt, gjort gjenbrukbart).
- Et konvensjonsmønster for modus-indikatorfelt i datamodellen (§3.1) og tilhørende layout-mønster for betinget visning.
- Retningslinjer/eksempelkode for `IInstantiationValidator` (§3.4) — autorisasjonssjekk for helsepersonell.
- Dokumentasjon til appteam om hvilke felt som *alltid* krever manuell fallback (pasientdata) vs. hvilke som kan hentes fra Altinn-identiteten uavhengig av SMART (legens navn, §3.3).

**Dette er trolig et sterkere differensieringsargument for Altinn-modellen enn det STRATEGI.md sin «Fire leveransemodeller»-analyse fanger opp eksplisitt i dag** — verdt å legge til der: NAV-modellen, NHN-modellen og EPJ-modellen har alle egne, dedikerte apper som *forutsetter* sin respektive integrasjon. Altinn-modellen er den eneste som kan tilby samme skjema uavhengig av om EPJ-en støtter SMART eller ikke.

---

## 5. Anbefalt neste steg

**Status 2026-08-13:** flyttet til [VEIKART.md fase 1b](VEIKART.md) som konkret tiltaksliste med avhengigheter. Oppsummert:

1. ✅ **Undersøkt HPR-oppslags-API og `PersonLookup`/`OrganisationLookup`-komponenter** — se §3.2–3.3. Arkitektur besluttet.
2. ✅ **Oppdatert STRATEGI.md og VEIKART.md fase 4** med dobbelmodus som eksplisitt del av helse-template-visjonen.
3. ⬜ **Modus-indikatorfelt + betinget UI-melding** i `ForerLegeerklaeringModel` — kan bygges nå, ingen avhengigheter.
4. ⬜ **`PersonLookup`-komponent** i layout, synlig kun i portal-modus — kan bygges nå.
5. ⬜ **HelseID-tilleggsflyt + HPR-bekreftelse** — **venter** på at Johann registrerer en HelseID authorization_code-klient og en Maskinporten `nhn:hpr/basic`-klient (besluttet 2026-08-13: bygging avventes til klientene finnes).
6. ⬜ **`IInstantiationValidator`** for tilgangsstyring — avhenger av #5 (bruker samme HPR-bekreftelse).

---

## 6. Referanser

- [IMPLEMENTERING.md §13](IMPLEMENTERING.md) — SMART-launch-verifisering (inngang A)
- [BESLUTNINGER.md C-1](BESLUTNINGER.md) — «hvem er parten» — relevant for §3.4
- [STRATEGI.md](STRATEGI.md) «Fire leveransemodeller» og Spor B
- [VEIKART.md](VEIKART.md) fase 4 — `Digdir.SmartOnFhir`-pakken
- [IMPLEMENTERING.md §14](IMPLEMENTERING.md) — HelseID, relevant for HPR-oppslag (§3.3)
