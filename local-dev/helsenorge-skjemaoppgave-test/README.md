# Helsenorge Oppgave API — skjemaoppgave (focus.type=Questionnaire)

Oppfølging av [local-dev/helsenorge-oppgave-test/](../helsenorge-oppgave-test/) (som verifiserte
`focus.type = "Communication"` ende-til-ende). Dette eksperimentet undersøker om en **skjemaoppgave**
kan peke ut til et eksternt skjema — konkret: kan lenken være vårt eget localtest-skjema?

## Status: mekanismen fungerer — men lenken må være HTTPS

**Konklusjon (2026-09-21):** en `focus.type = "Questionnaire"`-oppgave godtas strukturelt av
Helsenorge EksternAPI (`HTTP 201 Created`) på nøyaktig samme måte som `Communication`-varianten,
med `focus.reference` pekende til en `contained` `Questionnaire`-ressurs hvis `url`-felt er lenken.
**Men `Task.instantiatesUri` (og trolig `Questionnaire.url`) må bruke `https://`-skjema** —
`http://local.altinn.cloud:8000/...` avvises eksplisitt av Helsenorge sin egen validering, uavhengig
av om URL-en faktisk er nettverksmessig nåbar eller ikke.

**Betyr dette at vi kan sende en ekte lenke til localtest-skjemaet vårt?** Ikke direkte, nei —
localtest kjører med hensikt på ren HTTP (`local.altinn.cloud:8000`), og det er ikke noe vi bør endre
bare for dette eksperimentet. For en reell demo/pilot av skjemaoppgave-flyten mot vårt eget skjema,
trengs en av:
- En HTTPS-tunnel til den lokale appen (f.eks. ngrok / Cloudflare Tunnel) under selve demoen.
- Den faktiske deploy-URL-en til Altinn Studios TT02-testmiljø (`https://digdir.apps.tt02.altinn.no/...`
  el. lignende), hvis/når appen deployes dit.

Ingen av disse er utforsket i dette eksperimentet — kun selve API-mekanismens gyldighet.

## Loggen

| # | Forsøk | Resultat |
|---|---|---|
| 1 | `instantiatesUri = "http://local.altinn.cloud:8000/digdir/forer-legeerklaering/"` | `400` — kode 2174: «Ugyldig Task.InstantiatesUri. URI scheme må være HTTPS» |
| 2 | Samme struktur, men `instantiatesUri`/`Questionnaire.url` = `"https://github.com/FinnurO/forer-legeerklaering"` (en URL vi vet er en gyldig HTTPS-adresse) | **`HTTP 201 Created`** |

## Viktig funn fra Confluence-dokumentasjonen («Integrasjon mellom annen/ekstern skjemautfyller og
Helsenorge»), som ikke ble utforsket videre i dette eksperimentet men er relevant for en fremtidig
ekte integrasjon:

- En «ekstern skjemautfyller»-flyt (der innbygger fyller ut skjemaet på **vårt** nettsted, ikke
  Helsenorge sitt) forutsetter **sømløst SSO-uthopp**: innbyggeren skal ikke måtte logge inn på nytt
  når hun forlater Helsenorge og kommer til vårt skjema. Dette krever at vi bruker **Helsenorge sin
  egen OpenID Connect-provider** til å fastslå innbyggerens identitet på vår side — en helt egen,
  ikke-utforsket integrasjon (se «Sømløst uthopp — Helsenorge som OpenID Connect provider» i samme
  dokumentasjonsrom).
- Etter at innbygger har fylt ut skjemaet hos oss, skal vi levere en kopi tilbake til Helsenorge —
  nettopp det [local-dev/helsenorge-skjema-melding-test/](../helsenorge-skjema-melding-test/)
  verifiserer.

## Kjør

```powershell
$env:HELSEID_JWK_PATH = "C:\Users\jsf\.secrets\helseid-eksternapi-test.jwk.json"
cd local-dev\helsenorge-skjemaoppgave-test
dotnet run
```

## Neste steg

- Sett opp en HTTPS-tunnel til en kjørende localtest-instans og verifiser faktisk klikk-gjennom
  (åpnes lenken riktig, ender man opp på vårt skjema, uten dobbel innlogging).
- Utforsk «Sømløst uthopp» (Helsenorge som OIDC-provider) — nødvendig for at innbyggeren ikke skal
  måtte logge inn på nytt hos oss.
- `Bundle`-varianten (`POST .../oppgave/v1/Bundle`) er fortsatt ikke utforsket.

## Kilder

- [Integrasjon mellom annen/ekstern skjemautfyller og Helsenorge](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/1758822429/Integrasjon+mellom+annen+ekstern+skjemautfyller+og+Helsenorge)
- [FHIR Task - Oppgave](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/742948883)
