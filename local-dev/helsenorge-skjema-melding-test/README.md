# Helsenorge Skjema API — kopi av innsending til innbygger (DocumentReference)

Eksperiment: kan vi sende en «melding» til en innbygger som er en kopi av en innsending (f.eks. den
signerte legeerklæringen)? Svaret ligger i et **annet** API/løsningsområde enn Oppgave (Task):
**Skjema-API-et** (scope `nhn:helsenorge.eksternapi/skjema`), som eksponerer FHIR `DocumentReference`
(`POST .../skjema/v1/DocumentReference`) — bekreftet via Swagger
(`https://eksternapi.hn2.test.nhn.no/skjema/v1/swagger/v1/swagger.json`).

**Ikke å forveksle med:**
- **DokumentAPI** (`dokumenter/api/v1/SaveDokument`, se [PASIENTFLYT.md §4](../../docs/PASIENTFLYT.md)) —
  et helt annet, tredje API med brukertilstedeværelse/OIDC-autentisering (ikke `client_credentials`),
  og en innholdstype-begrensning til «Egenkartlegging».
- **Oppgave-API-et** (Task) — dette Skjema-API-et brukes for selve *innholdet* (dokumentet), ikke
  for å varsle innbyggeren om at noe er tilgjengelig (det gjør Task).

## Status: ende-til-ende verifisert 2026-09-21 — men KUN som svar på en eksisterende skjemaoppgave

**Viktigste arkitektoniske funn:** dette er **ikke** en fristående «send en melding»-mekanisme. En
`DocumentReference` kan bare sendes som **oppfølging av en skjemaoppgave** (`Task`) Helsenorge selv
har sendt til innbyggeren — den må inneholde `context.related` med en referanse til
`Task.identifier`. Bekreftet ordrett i Confluence («Benyttede FHIR Ressurser i Skjemaløsningen»,
XML-eksempel): *«Context gir knytning mellom PDF dokumentet og skjemaoppgaven (Task)»*. Uten en
relatert `Task` finnes det (per denne dokumentasjonen) ingen støttet vei til å sende en vilkårlig
«her er en kopi av noe du sendte inn»-melding til en innbygger på Helsenorge.

Dette betyr at den fulle flyten for «send kopi av en innsending til pasienten» i praksis er:
1. Send en skjemaoppgave (`Task`, `focus.type=Questionnaire`) — se
   [local-dev/helsenorge-skjemaoppgave-test/](../helsenorge-skjemaoppgave-test/).
2. (Ikke utforsket) Innbygger fyller ut skjemaet — hos oss (krever sømløst OIDC-uthopp) eller på
   Helsenorge selv, avhengig av løsning.
3. **Vi sender kopien tilbake** som en `DocumentReference` med `context.related` → samme `Task`.
   Dette er hva denne testen verifiserer.

## Loggen — fire feil, i rekkefølge

| # | Feil | Årsak | Løsning |
|---|---|---|---|
| 1 | `400` — kode 102: «Context kan ikke være null eller tom» | `DocumentReference.context.related` manglet — obligatorisk knytning til en `Task.identifier` | La til `context.related[0] = {type: "Task", identifier: {system: "urn:ietf:rfc:3986", value: "urn:uuid:<Task.identifier>"}}`, pekende til `Task`-en fra `helsenorge-skjemaoppgave-test` |
| 2 | `400` — kode 1111: «Don't know how to handle documentReference status» | Confluence-eksemplet har **både** `status` og `docStatus` — vi hadde kun `status` | La til `docStatus: "final"` |
| 3 | `400` — kode 1112: «A final DocumentReference requires that a pdf is attached» | `content.attachment.contentType` var `"text/plain"`, ikke tillatt for en «final» dokumentreferanse | Endret til `"application/pdf"` |
| 4 | `500` — kode 119 (`MELD-SENT-000119`): «Fil signatur stemmer ikke for mimetype application/pdf. Fil signatur: 84,69,83,84, Valid signatur: 37,80,68,70» | Innholdet **valideres som ekte binærinnhold**, ikke bare den deklarerte MIME-typen — våre testbytes («TEST…») begynte ikke med PDF-magic-bytes (`%PDF` = 37,80,68,70) | Gjenbrukte Helsenorges **eget eksempel-PDF** fra Confluence-dokumentasjonen (`test-pdf-base64.txt`) — en ekte, minimal, gyldig PDF |

**Resultat etter forsøk 4:** `HTTP 200 OK`, med full ressurs ekkoet tilbake inkl.
`"id":"87b7c09f-eff6-4f33-b2a0-314f4ac46bcf"` og `"meta":{"versionId":"1"}` — et ekte, lagret
dokument, ikke bare en validert-og-forkastet forespørsel.

## Kjør

```powershell
$env:HELSEID_JWK_PATH = "C:\Users\jsf\.secrets\helseid-eksternapi-test.jwk.json"
cd local-dev\helsenorge-skjema-melding-test
dotnet run
```

`RelatedTaskIdentifierGuid` i `Program.cs` peker på en spesifikk, allerede opprettet `Task` — bytt
denne til identifier-verdien fra en fersk kjøring av `helsenorge-skjemaoppgave-test` for å teste mot
en ny oppgave.

## Neste steg

- Verifiser visuelt i portalens `/dokumenter`-side at dokumentet faktisk vises for innbyggeren
  (ikke gjort ennå — se begrensning under).
- Utforsk `GET .../skjema/v1/DocumentReference/_search` (bekreftet i Confluence-dokumentasjonen)
  for å bekrefte at dokumentet faktisk kan slås opp igjen via `subject.identifier` + `related.identifier`.
- Vurder om `type`-koden (LOINC 34108-1, gjenbrukt fra `TestWriteback` i `SmartLaunchControllerBase`)
  er den rette koden for en legeerklæring/henvisning-kopi, eller om Helsenorge har egne forventede
  kodeverk for dette.

## Begrensning observert under arbeidet — IP-tilgangen til citizen-portalen er ikke stabilt åpen

I motsetning til forrige runde (se [helsenorge-oppgave-test/README.md](../helsenorge-oppgave-test/README.md)),
fikk vi denne gangen «Vi beklager! Siden kan ikke vises, da vi ikke gjenkjenner ip-adressen din» ved
forsøk på å logge inn i portalen fra samme nettleser-miljø — med en annen (IPv6) adresse enn sist.
IP-åpningen er altså trolig **spesifikk per klient-IP**, ikke en generell fjerning av sperren, og kan
derfor slå ut igjen ved neste forsøk fra et annet nettverk. API-nivå-verifiseringen over (curl/dotnet)
er upåvirket av dette, siden den går via en annen nettverksvei enn nettleserpanelet.

## Kilder

- [Integrasjon mellom annen/ekstern skjemautfyller og Helsenorge](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/1758822429/Integrasjon+mellom+annen+ekstern+skjemautfyller+og+Helsenorge)
- [Benyttede FHIR Ressurser i Skjemaløsningen](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/1008369665/Benyttede+FHIR+Ressurser+i+Skjemal+sningen) (XML-eksempler, inkl. eksempel-PDF-en gjenbrukt i `test-pdf-base64.txt`)
