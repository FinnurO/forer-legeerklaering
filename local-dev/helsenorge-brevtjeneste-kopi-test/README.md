# Helsenorge Brevtjeneste — kopi av henvisning sendt til en annen helseaktør

Presisering fra Johann (2026-09-21), med referanse til NHNs egen produktside:
[Sende brev, skjema og andre oppgaver](https://www.nhn.no/tjenester/helsenorge/sende-brev-skjema-og-andre-oppgaver).

Det egentlige scenarioet: **en helseaktør (f.eks. fastlege, via vår Altinn-app) sender en
henvisning til en annen helseaktør (f.eks. en spesialist) via en helt annen kanal (EDI/HER-ID,
utenfor Helsenorge) — og vil samtidig sende en lesbar kopi til pasienten via Helsenorge.**

Dette er **et annet** scenario enn [helsenorge-skjema-melding-test/](../helsenorge-skjema-melding-test/),
som dekket «ekstern skjemautfyller sender ferdig utfylt skjema *til Helsenorge*» (der Helsenorge selv
opprinnelig ba om skjemaet). Her er det **vi** som initierer alt — både selve henvisningen (som skjer
utenfor dette eksperimentet) og «her er en kopi til deg»-varselet.

## Status: strukturelt verifisert ende-til-ende 2026-09-21

Hypotese basert på [FHIR Task - Oppgave](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/742948883)-dokumentasjonen
(`Task.basedOn` kan referere til en `ServiceRequest` = Henvisning): send en `Communication`-Task
(allerede kjent mekanisme, se [helsenorge-oppgave-test/](../helsenorge-oppgave-test/)) med
`Task.basedOn` satt til henvisningens identifier, og en `DocumentReference` (kjent mekanisme, se
[helsenorge-skjema-melding-test/](../helsenorge-skjema-melding-test/)) med `context.related`
pekende til **både** denne Task-en og direkte til henvisningen.

**Resultat — begge kall lyktes på første forsøk, ingen nye feilkoder:**

1. `POST .../oppgave/v1/Task` (Communication, med `basedOn` → `ServiceRequest`-identifier) →
   `HTTP 201 Created`.
2. `POST .../skjema/v1/DocumentReference` (kopi av henvisningen som PDF, `context.related` med
   **to** oppføringer — én av type `Task`, én av type `ServiceRequest`) → `HTTP 200 OK`.

**Konklusjon:** «Brevtjenesten» slik Johann beskrev den — kopi til pasient av noe som ble sendt til
en annen helseaktør, eksplisitt knyttet til selve henvisningen — er nå strukturelt bekreftet
gjennomførbar med de samme to API-ene (Oppgave og Skjema) vi allerede har verifisert, uten behov for
noe tredje API. `Task.basedOn` og en `DocumentReference.context.related`-liste med mer enn én
oppføring var begge uverifiserte antagelser før dette eksperimentet — begge fungerte som forventet.

## Kjør

```powershell
$env:HELSEID_JWK_PATH = "C:\Users\jsf\.secrets\helseid-eksternapi-test.jwk.json"
cd local-dev\helsenorge-brevtjeneste-kopi-test
dotnet run
```

## Ikke utforsket

- Faktisk visning på portalen (samme IP-tilgangsbegrensning som i `helsenorge-skjema-melding-test/`
  — ikke retestet her).
- Om `type`-koden (LOINC 57833-6, «Referral note») er den koden Helsenorge/mottakerne faktisk
  forventer for en henvisningskopi.
- Selve henvisnings-sendingen til spesialisten (EDI/HER-ID) — utenfor scope for dette eksperimentet,
  som kun tester «kopien til pasienten»-halvparten.

## Kilder

- [Sende brev, skjema og andre oppgaver (NHN)](https://www.nhn.no/tjenester/helsenorge/sende-brev-skjema-og-andre-oppgaver)
- [FHIR Task - Oppgave](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/742948883) (`Task.basedOn`)
