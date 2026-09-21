# Helsenorge Oppgave API — testkall

Sender en minimal FHIR `Task` ("Oppgave") til Helsenorge EksternAPI TEST02, som oppfølging av
[local-dev/helseid-token-test/](../helseid-token-test/) (som kun bekreftet token-utveksling).
Dette verktøyet tester **selve API-kallet**.

## Status: fullstendig ende-til-ende verifisert 2026-09-18

**Oppdatering 2026-09-18 — blokkeren fra 2026-08-11 er løst.** NHN åpnet IP-sperren på
citizen-portalen (`tjenester.hn2.test.nhn.no`). Johann logget selv inn på ekte, produksjonslik
helsenorge.no som vanlig innbygger, og det åpnet muligheten for å teste «bakdør»-varianten
(`?pnr=<fnr>`) mot **TEST02** for en av de to testpersonene som tidligere feilet
(`21814497167`, Høy Hai). Høy Hai hadde aldri fullført samtykke-flyten («Hvordan vil du bruke
Helsenorge?») siden portalen inntil nå var IP-sperret for oss — det var trolig selve årsaken til
«ikke digitalt aktiv»-feilen, ikke en permanent provisjoneringsbeslutning hos NHN. Etter å ha
fullført samtykket (nivå «Full») direkte på portalen for Høy Hai, ga nøyaktig samme `POST
.../oppgave/v1/Task`-kall som tidligere feilet nå:

```
HTTP 201 Created
{"resourceType":"Task","id":"f25e035b-ec7e-4258-b92d-ae2a79c53bb0", ...}
```

**Bekreftet i selve portalen** (`GET /proxy/oppgaveinternal/innbygger/oppgave/v2/aktive` og
UI på `/oppgaver`): oppgaven vises for Høy Hai som «TEST — teknisk tilkoblingstest», avsender
«LAV MODIG TIGER AS», status «Ikke startet» / «Ulest», frist 18.10.2026 — nøyaktig payloaden
`Program.cs` sender. Dette er første gang hele kjeden — HelseID-autentisering, FHIR
`Task`-strukturen, EksternAPI-kallet, **og selve leveransen til en innbyggers Helsenorge-innboks**
— er bekreftet fungerende ende-til-ende, ikke bare strukturelt.

`OwnerFnr` er derfor endret til Høy Hai (`21814497167`) igjen, siden hun nå er den bekreftet
fungerende testpersonen. Sart Maskin (`26908896636`) er ikke retestet — anta samme «ikke
digitalt aktiv»-status til hun også har fullført samtykke-flyten på portalen minst én gang.

**Visuelt bekreftet 2026-09-21** (i tillegg til API-responsen over): skjermbilde av Høy Hai sin
faktiske `/oppgaver`-side viser oppgavekortet «TEST — teknisk tilkoblingstest», status «Ikke
startet», merket «Ulest», avsender «LAV MODIG TIGER AS», frist «Om 30 dager (18.10.2026)» — altså
et fullt visuelt samsvar med det API-et rapporterte, ikke bare en antatt kobling mellom de to.

---

<details>
<summary>Historikk: status før 2026-09-18 (strukturelt verifisert, blokkert på testdata)</summary>

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

</details>

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

**Oppdatert 2026-09-21.** Skjemaoppgave (`focus.type = "Questionnaire"`) er nå også verifisert —
se [local-dev/helsenorge-skjemaoppgave-test/](../helsenorge-skjemaoppgave-test/) (krever HTTPS-lenke)
og [local-dev/helsenorge-skjema-melding-test/](../helsenorge-skjema-melding-test/) (kopi av
innsending tilbake til innbygger, via et eget API — `DocumentReference`, ikke `Task`). Gjenstående
arbeid:

- `Bundle`-varianten (`POST .../oppgave/v1/Bundle`) er fortsatt ikke utforsket.
- «Sømløst uthopp» (Helsenorge som OIDC-provider for vår egen skjemautfylling) — nødvendig for at
  innbygger ikke skal måtte logge inn på nytt hos oss, se `helsenorge-skjemaoppgave-test/README.md`.
- Retest Sart Maskin (`26908896636`) ved å fullføre samtykke-flyten for henne også på portalen,
  for å bekrefte at «Full»-samtykke er det som faktisk avgjør «digitalt aktiv»-status (ikke noe
  unikt med Høy Hai).
- Fortsatt uklart om dette funnet (IP-sperren løst, «digitalt aktiv» løst ved samtykke) også
  gjelder DokumentAPI (se [PASIENTFLYT.md §4](../../docs/PASIENTFLYT.md), «DokumentAPI»-avsnittet)
  — den har en annen autentiseringsmodell (brukertilstedeværelse/OIDC, ikke `client_credentials`)
  og er ikke retestet.

## Kilder

- [FHIR Task - Oppgave (fullstendig ressursspesifikasjon)](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/742948883)
- [Oppgave API (endepunkter og autorisasjon)](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/2109734913)
- [Testmiljøer og endepunkter](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/1552384092/Testmilj+er+og+endepunkter)
