# Helsenorge Oppgave API — testkall

Sender en minimal FHIR `Task` ("Oppgave") til Helsenorge EksternAPI TEST02, som oppfølging av
[local-dev/helseid-token-test/](../helseid-token-test/) (som kun bekreftet token-utveksling).
Dette verktøyet tester **selve API-kallet**.

## Status: strukturelt verifisert 2026-08-11, blokkert på testdata

Alle tekniske lag er nå bekreftet fungerende, i denne rekkefølgen av feil vi løste underveis:

| # | Feil | Årsak | Løsning |
|---|---|---|---|
| 1 | `HTTP 401` — `EHSEC-110002` («Token is expired or invalid») | For lite spesifikk feilmelding — reell årsak var manglende organisasjonsnummer | La til `orgnr_parent` i token-forespørselen |
| 2 | `invalid_request` — «is set up for multi-tenancy, but is passing a single tenant organization structure» | Klienten «Altinn Studio» er registrert som **multi-tenant** i HelseID, ikke single-tenant | Byttet til `.AddHelseIdMultiTenant()` + `OrganizationNumbers { ParentOrganization, ChildOrganization }` (samme orgnr i begge, vi har ingen egen underenhet) |
| 3 | `400` — `2109`: «Finner ikke Task.focus» | `Task.focus` er reelt obligatorisk (dokumentasjonen markerte kun `focus.type` eksplisitt som «mandatory», ikke hele elementet) | La til `focus: { type: "Communication" }` (enklest oppgavetype — informasjonsoppgave) |
| 4 | `400` — `2147`: «Finner ikke Task.instantiatesUri» | For `focus.type = "Communication"` er `instantiatesUri` obligatorisk (peker til nettsted med informasjonen) | La til `instantiatesUri` (repo-URL) |
| 5 | `400` — `2118`: «Pasienten er ikke digitalt aktiv for tjeneste: OmradeHelsehjelp» | **Ikke en kodefeil** — testpersonen (Høy Hai, ekte Tenor-fnr) er ikke registrert som digitalt aktiv for tjenesteområdet "Helsehjelp" i Helsenorge TEST02 sitt testregister | **Uløst** — krever enten en annen testperson som er provisjonert som digitalt aktiv, eller å kontakte NHN/Helsenorge om aktivering |

**Konklusjon:** Hele den tekniske kjeden — HelseID multi-tenant client_credentials-autentisering,
DPoP, riktig FHIR `Task`-struktur — er verifisert korrekt mot en ekte NHN-tjeneste. Det gjenstående
hinderet er rent data-/provisjoneringsmessig, ikke noe som kan løses med mer kode.

## Faktisk fungerende Task-payload (strukturelt godkjent av API-et)

Se [Program.cs](Program.cs) for fullstendig, kjørbar kode. Kjernefeltene som kreves for en
`focus.type = "Communication"`-oppgave:

```json
{
  "resourceType": "Task",
  "contained": [{ "resourceType": "Organization", "id": "requester-1", "identifier": [...], "name": "..." }],
  "meta": { "security": [{ "system": "urn:oid:2.16.578.1.12.4.1.1.7618", "code": "3", "display": "Helsehjelp" }] },
  "identifier": [{ "system": "urn:ietf:rfc:3986", "value": "urn:uuid:..." }],
  "status": "ready",
  "intent": "proposal",
  "code": { "text": "..." },
  "description": "...",
  "focus": { "type": "Communication" },
  "instantiatesUri": "https://...",
  "requester": { "reference": "#requester-1", "type": "Organization" },
  "owner": { "type": "Patient", "identifier": { "system": "urn:oid:2.16.578.1.12.4.1.4.1", "value": "<fnr>" } },
  "restriction": { "period": { "end": "<ISO8601 deadline>" } }
}
```

## Kjør

Samme forutsetning som [helseid-token-test](../helseid-token-test/README.md) — privatnøkkel utenfor repoet:

```powershell
$env:HELSEID_JWK_PATH = "C:\Users\jsf\.secrets\helseid-eksternapi-test.jwk.json"
cd local-dev\helsenorge-oppgave-test
dotnet run
```

## Neste steg

**Oppdatert 2026-08-11 — ikke lenger et kodeproblem.** Testet med to uavhengige, Tenor-verifiserte
testpersoner (Høy Hai og Sart Maskin) — samme "ikke digitalt aktiv"-feil begge ganger. Samtidig er
citizen-portalen (`helsenorge.hn2.test.nhn.no` og «bakdør»-varianten `tjenester.hn2.test.nhn.no`)
IP-sperret uansett URL. Ifølge [Hvordan komme i gang](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/1348174733/Hvordan+komme+i+gang)
gis testmiljøtilgang og provisjonering av testpersoner kun etter formell leverandørkontakt med NHN
— ikke selvbetjent slik EksternAPI-token-autentiseringen er.

- **Konkret handling:** ta kontakt via `ext-utv-hn-forerrett`-Slack-kanalen eller
  `ide-ogbestillingsmottak@nhn.no` for å få (a) testpersoner provisjonert som digitalt aktive,
  og (b) ev. formell testmiljøtilgang til portalen. Se [BESLUTNINGER.md C-6](../../docs/BESLUTNINGER.md)
  og [RISIKOREGISTER.md R9](../../docs/RISIKOREGISTER.md).
- Når det er løst: verifiser at oppgaven faktisk dukker opp for testpersonen.
- Utforsk skjemaoppgave (`focus.type = "Questionnaire"`) og `Bundle`-varianten, som er det som
  faktisk trengs for NA-0201-egenerklæringen (se [PASIENTFLYT.md](../../docs/PASIENTFLYT.md)).

## Kilder

- [FHIR Task - Oppgave (fullstendig ressursspesifikasjon)](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/742948883)
- [Oppgave API (endepunkter og autorisasjon)](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/2109734913)
- [Testmiljøer og endepunkter](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/1552384092/Testmilj+er+og+endepunkter)
