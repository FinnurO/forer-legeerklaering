# SmartFhir.Common — delt SMART on FHIR-launch/-prefill for flere Altinn-apper i dette repoet

**Lagt til 2026-09-09**, sammen med det andre caset i repoet ([SKJEMA-KJEVEORTOPEDISK.md](SKJEMA-KJEVEORTOPEDISK.md) / `src/AppKjeveortopedisk`). Første gang repoet faktisk har to Altinn Studio-apper side om side — se «Åpent spørsmål: appstruktur» i det dokumentet for hvorfor de bor i samme repo.

## Hvorfor nå, og hvorfor ikke en NuGet-pakke ennå

[VEIKART.md, Fase 4](VEIKART.md) beskriver en fremtidig NuGet-pakke `Digdir.SmartOnFhir`, uttrykkelig **etter** at mønsteret er bevist i produksjon (*"NAV ekstraherte `@navikt/smart-on-fhir` etter at `syk-inn` var i produksjon — samme sekvens gjelder her"*). `SmartFhir.Common` er **ikke** den pakken — det er et internt `ProjectReference`-klassebibliotek i samme repo, ikke noe publisert/versjonert utenfor det. Det mangler bevisst det fase 4 lister som pakkens fulle omfang:

- `TokenValidator` (JWKS-validering av access token) — finnes ikke.
- `SmartTokenStore` med Redis-støtte — vi bruker fortsatt kun session + `IMemoryCache`, uendret fra før.
- `SmartOptions`/`AddSmartOnFhir()` som ferdig DI-extension — hver app registrerer fortsatt tjenestene manuelt i `Program.cs`.
- Konvensjonsmønsteret for «dobbel inngangsmodus» (se [DOBBEL-INNGANGSMODUS.md](DOBBEL-INNGANGSMODUS.md)) — ikke del av dette biblioteket ennå; hver app må fortsatt løse det selv.

Grunnen til at vi likevel gjør denne mindre utskillingen nå, foran fase 4, er at den ble **fremtvunget av en reell andre forbruker** (kjeveortopedisk-caset), ikke gjettet på forhånd — den samme rekkefølgen VEIKART.md etterlyser (bevis mønsteret med to reelle brukssteder, ekstraher deretter det som faktisk er felles), bare i miniatyr og internt i repoet. Når/hvis dette skal bli den faktiske `Digdir.SmartOnFhir`-pakken i fase 4, er `SmartFhir.Common` et konkret utgangspunkt — ikke et blankt ark.

## Hva som faktisk ble flyttet

Den opprinnelige `SmartLaunchController.cs` og `FhirPrefillService.cs` i `src/App` viste seg, ved gjennomgang, å **allerede være ~100 % app-nøytrale** — `SmartLaunchController` hadde ingen referanse til `ForerLegeerklaeringModel` i det hele tatt, og FHIR-hentingen/-parsingen i `FhirPrefillService` var kun koblet til modellen i selve feltildelingen. Utskillingen var derfor i praksis en ren omplassering av eksisterende, allerede testet kode — ikke en omskriving.

| Flyttet til `SmartFhir.Common` | Ble værende app-spesifikt |
|---|---|
| `SmartLaunchControllerBase` — hele OAuth2/PKCE/discovery/token-exchange/session-mekanikken | Fallback-`ClientId`, testdata (patient/encounter/practitioner-id) for `test-prefill`/`dev-login`, hvilke scopes appen ber om — satt via `protected override`-hooks i hver apps tynne `SmartLaunchController` |
| `SmartFhirPrefillClient` — henter/parser Patient, Practitioner (+PractitionerRole), Organization, Encounter, Condition inn i en nøytral `StandardFhirPrefillData` | Mapping fra `StandardFhirPrefillData` til appens egen datamodell (feltnavn varierer: `Lege_HPR` i ForerLegeerklaering vs. `Henviser_HPR` i kjeveortopedisk) |
| `SmartSessionReader` — leser token+kontekst fra session/`IMemoryCache` | Alt som ikke er FHIR-prefill: `ForerKonklusjonModel`-avledningen (`End`/`DeriveKonklusjon`) i ForerLegeerklaering finnes ikke i kjeveortopedisk-caset og ligger fortsatt kun der |
| `NorwegianHealthOids` — Fnr/HPR/Orgnr/HerId-OID-ene | — |
| `FhirLaunchContext`/`TokenData`/`CachedFhirData`/`SmartSessionKeys` — session-DTO-er og nøkler | — |

**Merk om navngiving:** `StandardFhirPrefillData` bruker `Henviser_*` (ikke `Lege_*`) som feltprefiks for behandleren, fordi HPR-nummeret dekker all autorisert helsepersonell — bekreftet på tvers av lege (ForerLegeerklaering) og tannlege/tannpleier (kjeveortopedisk). Hver app mapper selv videre til sitt eget feltnavn.

**Nytt felt lagt til i samme slag:** `Pasient_Adresse` (fra `Patient.address`) — kjeveortopedisk-caset trengte det (ForerLegeerklaering gjorde ikke), men det er generisk nok til å være med i standardsettet for senere caser også.

## Hvordan en ny app i repoet bruker biblioteket

1. `<ProjectReference Include="..\SmartFhir.Common\SmartFhir.Common.csproj" />` i appens `.csproj`.
2. Egen `SmartLaunchController : SmartLaunchControllerBase` — implementer `DefaultClientId`, `DefaultTestPatientId`, `DefaultTestEncounterId`, `DefaultTestPractitionerPath`; overstyr `GetScopes()` kun hvis appen trenger andre FHIR-scopes enn standardsettet.
3. Egen `FhirPrefillService : IDataProcessor` — kall `SmartSessionReader.TryReadAsync(...)` og `new SmartFhirPrefillClient(logger).FetchAsync(...)`, map `StandardFhirPrefillData` til appens egen modell. Alt som IKKE er et av standardfeltene (f.eks. en klinisk klassifisering som ikke finnes strukturert i noe EPJ) fylles bevisst ikke ut herfra — se hver apps egen prefill-tjeneste for begrunnelse.

## Bevisste avgrensninger (per 2026-09-09)

- Ingen automatiserte tester for noen av delene — samme status som resten av repoet (se [TESTGUIDE-SMARTHEALTHIT.md](TESTGUIDE-SMARTHEALTHIT.md)).
- Ingen `AddSmartOnFhir()`-DI-extension — `Program.cs` i hver app registrerer `IHttpClientFactory`/`IMemoryCache`/session manuelt, uendret oppsett fra før utskillingen.
- Testdataene i `AppKjeveortopedisk` sin `SmartLaunchController` (patient-/encounter-/practitioner-id) er placeholder — det finnes ennå ikke noe tannlege-EPJ-testmiljø koblet til dette caset (se SKJEMA-KJEVEORTOPEDISK.md).
