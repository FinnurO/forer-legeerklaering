# HelseID EksternAPI — token-røyktest

Isolert konsoll-app som **kun** bekrefter at vi kan hente et gyldig access token fra HelseID
testmiljø for Helsenorge EksternAPI (Oppgave + Skjema), med klienten «Altinn Studio» registrert
i [selvbetjening.test.nhn.no](https://selvbetjening.test.nhn.no/clients/816692d0-71f2-4fe7-9bbf-a79a9a7feabe).

Ruller **ikke** noe kall mot selve Helsenorge-APIet (`eksternapi.helsenorge.no`) — det er neste steg,
etter at denne testen er grønn.

Bruker det offisielle [`HelseID.Library.ClientCredentials`](https://github.com/NorskHelsenett/HelseID.Library)
NuGet-pakket fra NHN (v1.1.3, støtter `net8.0` — samme rammeverk som `src/App`), som håndterer
`client_credentials`-flyten, `private_key_jwt`-klientautentisering og DPoP-token-binding for oss.
Vi trenger ikke å implementere JWT-signering eller DPoP-proof selv.

## Hvorfor et eget prosjekt og ikke inni `src/App`

Dette er en helt annen integrasjon enn `SmartLaunchController` (som gjør bruker-redirect
SMART EHR Launch mot legens EPJ). Helsenorge EksternAPI er maskin-til-maskin
(`client_credentials`, ingen brukerinnlogging). Holder de separate til vi vet om/hvordan dette
skal inn i hovedappen.

## Sikkerhet — privatnøkkelen skal ALDRI inn i repoet

Repoet er offentlig på GitHub. Privatnøkkelen (RSA 4096, JWK-format) fra HelseID selvbetjening
leses fra en **lokal fil utenfor repoet**, angitt via miljøvariabelen `HELSEID_JWK_PATH`. Den skal
aldri limes inn i kildekode, `appsettings*.json` eller en chat/commit-melding.

## Kjør

```powershell
$env:HELSEID_JWK_PATH = "C:\Users\jsf\.secrets\helseid-eksternapi-test.jwk.json"
cd local-dev\helseid-token-test
dotnet run
```

Forventet output ved suksess:
```
Ber om token fra https://helseid-sts.test.nhn.no for klient 4f1fc480-72d9-4e31-b099-69b84fd5ba6b ...
Scope: nhn:helsenorge.eksternapi/oppgave nhn:helsenorge.eksternapi/skjema

✅ Token mottatt.
   Utløper om: 300 sekunder
   Tildelt scope: nhn:helsenorge.eksternapi/oppgave nhn:helsenorge.eksternapi/skjema
```

Selve access-token-verdien skrives bevisst aldri til konsoll eller logg.

## Hvis privatnøkkelen ikke finnes lenger

NHN viser normalt privatnøkkelen **kun én gang**, ved opprettelse. Hvis du ikke har den lagret
fra tidligere, gå til klienten i selvbetjening.test.nhn.no → **Legg til ny autentiseringsnøkkel**
→ **Få generert et nøkkelpar**, last ned JWK-filen umiddelbart, og lagre den utenfor dette repoet
(f.eks. `C:\Users\jsf\.secrets\`).

## Status: bekreftet 2026-08-10 ✅

Token-utveksling mot ekte HelseID testmiljø (`helseid-sts.test.nhn.no`) fungerer med klienten
«Altinn Studio». Begge scopene (`oppgave`, `skjema`) ble innvilget uten `RejectedScope`. Access
token varer kun 60 sekunder i testmiljøet — kort levetid, husk å hente nytt token rett før bruk,
ikke cache lenge.

Loggen viste et `400` på første `POST /connect/token`, deretter `200` på andre forsøk — det er
**DPoP nonce-challenge**-mønsteret (HelseID avviser første forsøk uten nonce, klienten prøver på
nytt med nonce fra svaret). `HelseID.Library` håndterer dette helt automatisk. Dette er altså
forventet oppførsel, ikke en feil.

## Gjenstående åpne spørsmål (ikke løst av denne testen)

- **`orgnr_parent`** — token-utstedelsen krevde ikke noe organisasjonsnummer med dagens
  single-tenant-oppsett (`AddHelseIdClientCredentials` uten `.AddHelseIdMultiTenant()`). Det er
  uklart om `eksternapi.helsenorge.no` likevel krever `orgnr_parent` i selve API-kallet (ikke i
  token-forespørselen) — det finner vi ut når vi prøver et faktisk Oppgave/Skjema-kall.
- **DPoP i testmiljø** — [IMPLEMENTERING.md §14](../../docs/IMPLEMENTERING.md) sier DPoP ikke er
  nødvendig i testmiljøet, men denne testen viser at DPoP nonce-flyten faktisk trigges av
  `helseid-sts.test.nhn.no` for denne (EksternAPI-)klienten. §14s DPoP-utsagn gjaldt trolig kun
  den separate SMART-launch-klienten — bør presiseres i dokumentasjonen.
- **Selve Oppgave/Skjema-kallene** — denne testen bekrefter bare autentisering. Payload-format
  (FHIR `Task` via `POST .../oppgave/v1/Bundle`) er ikke utforsket eller implementert ennå.

## Kilder

- [HelseID.Library — GitHub](https://github.com/NorskHelsenett/HelseID.Library)
- [HelseID.Samples — ClientCredentials](https://github.com/NorskHelsenett/HelseID.Samples/tree/master/ClientCredentials)
- [Ekstern applikasjon kaller Helsenorge API i systemkontekst](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/1886191617/3.+Ekstern+applikasjon+kaller+Helsenorge+API+i+systemkontekst)
- [Oppgave API](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/2109734913)
