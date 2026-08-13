# Testguide: SMART EHR Launch mot launch.smarthealthit.org

Denne guiden samler **alt** som trengs for at en annen person skal kunne gjenskape testene som er gjort mot [launch.smarthealthit.org](https://launch.smarthealthit.org/) — en offentlig, standardkompatibel SMART on FHIR-testserver driftet av SMART Health IT-prosjektet. Se [IMPLEMENTERING.md §13](IMPLEMENTERING.md) for selve funnene/resultatene; denne guiden er "hvordan gjøre det selv", ikke "hva vi fant".

## Innhold

1. [Har vi automatiske tester?](#1-har-vi-automatiske-tester)
2. [Forutsetninger](#2-forutsetninger)
3. [Hvordan launch.smarthealthit.org fungerer](#3-hvordan-launchsmarthealthitorg-fungerer)
4. [Secrets og nøkkelhåndtering](#4-secrets-og-nøkkelhåndtering)
5. [Reproduser testene selv](#5-reproduser-testene-selv)
6. [Kjente begrensninger](#6-kjente-begrensninger)

---

## 1. Har vi automatiske tester?

**Nei.** Dette bør sies rett ut: `.github/workflows/dotnet-test.yml` kjører `dotnet test src/App.sln` på hver push/PR mot `main`, men det finnes **ingen** faktiske testklasser i løsningen — `src/App/TestDummy.cs` er et tomt scaffold-artefakt fra Altinn Studio sin app-mal (`public class TestDummy { }`), ikke en ekte test. `dotnet test` kjører derfor 0 tester og "består" trivielt hver gang, uansett om koden fungerer eller ikke. Dette er en reell dekningsgap, ikke noe som er skjult — CI-en gir falsk trygghet i sin nåværende form.

To andre ting i repoet kan forveksles med automatiske tester, men er det ikke:

- [`local-dev/helseid-token-test/`](../local-dev/helseid-token-test/) og [`local-dev/helsenorge-oppgave-test/`](../local-dev/helsenorge-oppgave-test/) er **manuelle engangs-verifiseringsprogrammer** (konsoll-apper som kjøres med `dotnet run` og krever ekte NHN-testmiljø-tilgang + en lokal hemmelighet). De har ingen assertions/CI-integrasjon — de skriver bare resultatet til konsollet for et menneske å lese.
- Dette dokumentets [`local-dev/smarthealthit-testing/test-smart-launch.ps1`](../local-dev/smarthealthit-testing/test-smart-launch.ps1) er i samme kategori: et **repeterbart verifiseringsskript**, ikke en CI-test. Det avhenger av at hele det lokale miljøet (podman-containere + `dotnet run`) kjører og av en ekte, ekstern tjeneste (launch.smarthealthit.org) — begge egenskaper som gjør det uegnet for en vanlig CI-pipeline uten betydelig ekstra arbeid (containerisert testmiljø, mocking av den eksterne serveren, e.l.). Det er likevel en stor forbedring fra "gjør dette manuelt i nettleseren og les av resultatet selv" til "kjør ett skript, få et klart JA/NEI-svar".

**Anbefaling (ikke gjort ennå):** minst noen ekte enhetstester for de rene funksjonene som ikke krever nettverk eller Altinn-runtime — f.eks. `Base64UrlEncode`/`Base64UrlDecode`, `BuildClientAssertionJwt` sin JWT-struktur (uten å faktisk sende den noe sted), og `DeriveKonklusjon`-logikken i `FhirPrefillService`. Disse kunne vært ekte xUnit-tester i et nytt testprosjekt (`src/App.Tests/`) uten avhengighet til noe eksternt.

## 2. Forutsetninger

Alt under forutsetter at det lokale miljøet kjører — se [HELHETLIG-FLYT.md](HELHETLIG-FLYT.md) for full oppskrift. Kortversjon:

1. Altinn localtest-containerne kjører (`podman ps` skal vise `localtest`, `hapi-fhir`, `localtest-pdf3`, `localtest-loadbalancer` som "Up").
2. Loadbalanceren svarer: `curl http://local.altinn.cloud:8000/` skal gi `200`.
3. Appen selv kjører: `dotnet run` i `src/App`, lytter på `http://localhost:5005`. Verifiser: `curl http://localhost:5005/` skal gi `404` (forventet — det finnes ingen rot-endepunkt, men et svar i det hele tatt betyr at prosessen kjører).
4. `%WINDIR%\System32\drivers\etc\hosts` må ha `127.0.0.1 local.altinn.cloud`.
5. PowerShell-skriptene i denne guiden bruker `curl.exe` (ikke PowerShell-aliaset `curl` som peker til `Invoke-WebRequest`) — dette følger med Windows 10/11 som standard, ingen installasjon nødvendig.

## 3. Hvordan launch.smarthealthit.org fungerer

Launcheren er et React-grensesnitt med to faner:

### Fane 1: «App Launch Options»

- **Launch Type**: la stå på "Provider EHR Launch" (det er dette appen implementerer — SMART EHR Launch, ikke Standalone Launch).
- **Patient(s)** / **Provider(s)**: hvis disse står tomme (eller har flere kommaseparerte ID-er), viser launcheren en **interaktiv velger** midt i OAuth-flyten før den gir en autorisasjonskode. Dette er problemet som blokkerte automatisert testing tidligere i prosjektet — **løsningen er å alltid forhåndsvelge nøyaktig én pasient-ID og én behandler-ID**, se §5.
  - Provider-ID-er er faste og fremgår av launcherens UI (f.eks. `4832580` = "Dr. FLEX Test").
  - Pasient-ID-er er dynamiske Synthea-genererte UUID-er — hent en gyldig en med `GET https://launch.smarthealthit.org/v/r4/fhir/Patient?_count=1`.
- **Simulated Error**: tvinger en spesifikk feil på et gitt steg i flyten (se full liste i §5.3). Nyttig for å teste at appen feiler trygt.
- **App's Launch URL**: peker til appens `/smart/launch`-endepunkt. Lokalt: `http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/launch`.

### Fane 2: «Client Registration & Validation»

Bestemmer hvordan launcheren forventer at appen autentiserer seg ved token-utveksling:

| Client Type | Tilsvarer i appens kode | Config-nøkkel som må være satt |
|---|---|---|
| **Public** | Ingen autentisering, kun `code_verifier` (PKCE) | Ingen (både `ClientSecret` og `ClientAssertionPrivateKeyPath` tomme) |
| **Confidential Symmetric** | HTTP Basic-auth med `client_id:client_secret` | `SmartOnFhir:ClientSecret` |
| **Confidential Asymmetric** | `private_key_jwt` (RFC 7523) — signert JWT som `client_assertion` | `SmartOnFhir:ClientAssertionPrivateKeyPath` |

**Client Identity Validation**:
- **Loose** (brukt i alle våre tester): godtar en hvilken som helst `client_secret`, eller en hvilken som helst *strukturelt gyldig* JWT-assertion for Confidential Asymmetric. Den sjekker at feltene (`iss`/`sub`/`aud`/`exp` osv.) er til stede og konsistente, men verifiserer **ikke** signaturen kryptografisk mot en registrert nøkkel.
- **Strict**: ville krevd at vi registrerer den faktiske hemmeligheten/offentlige nøkkelen i launcheren, og gir en ekte test av at feil hemmelighet/signatur faktisk avvises. **Ikke testet i dette prosjektet** — se §6.

**Viktig fallgruve:** appens kode (`ExchangeCodeForToken`) prioriterer `private_key_jwt` over `client_secret` hvis *begge* er konfigurert samtidig. Skal du teste `client_secret` isolert, må `SmartOnFhir:ClientAssertionPrivateKeyPath` være tom/fjernet (og omvendt) — ellers tester du feil kodesti uten å merke det. `test-smart-launch.ps1` sjekker ikke dette for deg; du må selv sørge for at kun én av de to er satt før du kjører et gitt scenario. Appen må **restartes** etter enhver `dotnet user-secrets`-endring for at den skal ta effekt.

### `launch=`-parameterens skjema

Hele launch-konfigurasjonen (inkl. pasient, behandler, simulert feil, client type, validation mode) kodes som et JSON-array, base64url-enkodet, i `launch=`-query-parameteren. Reverse-engineert ved å endre innstillinger i UI-et og lese av den genererte lenken:

```
[launch_type, patient_id, provider_id, encounter_mode, 0, 0, 0, "", "", "", "", simulated_error, "", "", client_type, validation_mode, ""]
```

| Indeks | Betydning | Verdier |
|---|---|---|
| 0 | launch_type | `0` = provider-ehr |
| 1 | patient-ID | streng, tom = interaktiv velger |
| 2 | provider-ID | streng, tom = interaktiv velger |
| 3 | encounter-modus | `"AUTO"` eller `"MANUAL"` |
| 4–10 | (ikke i bruk i våre tester) | — |
| 11 | simulert feil | f.eks. `"token_invalid_token"`, tom streng = ingen |
| 14 | client type | `0` = Public, `1` = Confidential Symmetric, `2` = Confidential Asymmetric |
| 15 | validation mode | `1` = Loose (alle våre tester), `2` = Strict (ikke testet) |

Eksempel (Python, for manuell verifisering — skriptet i §5 gjør dette automatisk):

```python
import base64, json, urllib.parse
arr = [0, "<patient-id>", "4832580", "AUTO", 0,0,0, "","","","", "", "","", 2, 1, ""]
b64 = base64.urlsafe_b64encode(json.dumps(arr, separators=(',',':')).encode()).decode().rstrip('=')
print(f"http://local.altinn.cloud:8000/digdir/forer-legeerklaering/smart/launch?iss={urllib.parse.quote('https://launch.smarthealthit.org/v/r4/fhir', safe='')}&launch={b64}")
```

## 4. Secrets og nøkkelhåndtering

**Standing regel for dette prosjektet:** private nøkler og hemmeligheter limes **aldri** inn i en chat-samtale eller en commit. De lagres kun i lokale filer utenfor repoet, referert via `dotnet user-secrets` (som lagrer i `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` — utenfor ethvert repo) eller miljøvariabler.

### `client_secret` (Confidential Symmetric)

```bash
cd src/App
dotnet user-secrets init   # kun første gang — legger en UserSecretsId (en tilfeldig GUID, IKKE hemmelig) i App.csproj
dotnet user-secrets set "SmartOnFhir:ClientSecret" "en-hvilken-som-helst-verdi"
```

Verdien spiller ingen rolle i Loose-modus — poenget er at appen *sender* en Basic-auth-header, ikke hva som står i den.

### `private_key_jwt` (Confidential Asymmetric)

Generer et RSA-nøkkelpar og eksporter som JWK:

```powershell
.\local-dev\smarthealthit-testing\generate-client-assertion-jwk.ps1
```

Dette skriver den **private** JWK-en til `%USERPROFILE%\.local-secrets\forer-legeerklaering-smart-client-assertion-key.json` (utenfor repoet) og den **offentlige** JWK-en (kun `kty`/`use`/`alg`/`kid`/`n`/`e` — ingen `d`/`p`/`q`) til samme mappe som skriptet. Skriv deretter stien til den private filen inn i user-secrets (skriptet skriver ut den nøyaktige kommandoen på slutten):

```bash
dotnet user-secrets set "SmartOnFhir:ClientAssertionPrivateKeyPath" "C:\Users\<deg>\.local-secrets\forer-legeerklaering-smart-client-assertion-key.json"
```

Den offentlige JWK-en er trygg å dele — den trengs kun hvis du vil teste Strict-validering (registrer den i launcherens JWKS-felt, se §6).

### Restart appen etter enhver secrets-endring

`dotnet user-secrets` skriver til disk umiddelbart, men den kjørende `dotnet run`-prosessen har allerede lest konfigurasjonen ved oppstart. Stopp og start appen på nytt (`Ctrl+C`, så `dotnet run` igjen) etter hver `user-secrets set`/`remove`.

## 5. Reproduser testene selv

### 5.1 Happy path (public client)

```powershell
.\local-dev\smarthealthit-testing\test-smart-launch.ps1 -ClientType public
```

Forventet: alle tre steg (`launch`, `authorize`, `callback`) gir `302`, og skriptet rapporterer at `AltinnStudioRuntime`-cookien ble satt.

### 5.2 client_secret / private_key_jwt

Sett opp secrets som i §4 (kun **én** av dem om gangen, se fallgruven i §3), restart appen, kjør:

```powershell
.\local-dev\smarthealthit-testing\test-smart-launch.ps1 -ClientType secret
.\local-dev\smarthealthit-testing\test-smart-launch.ps1 -ClientType private_key_jwt
```

### 5.3 Simulated Error-scenarioer

Full liste over verdier launch.smarthealthit.org støtter (les av launcherens "Simulated Error"-nedtrekksmeny):

| Verdi | Hva den simulerer | Reprodusert? |
|---|---|---|
| `auth_invalid_client_id` | Ugyldig `client_id` ved autorisasjon | ✅ automatisk, se IMPLEMENTERING.md §13 |
| `auth_invalid_redirect_uri` | Ugyldig `redirect_uri` | ✅ automatisk |
| `auth_invalid_scope` | Ugyldig scope ved autorisasjon | ✅ automatisk |
| `auth_invalid_client_secret` | Feil `client_secret` ved token-utveksling | ⚠️ kjørt, men uten reell effekt mot Public client — se merknad under |
| `token_invalid_token` | Token-endepunktet returnerer et ugyldig token | ✅ automatisk (`502`, trygt) |
| `token_expired_registration_token` | Utløpt registreringstoken | ✅ automatisk (`502`, trygt) |
| `token_expired_refresh_token` | Utløpt refresh-token | ⚠️ ikke testet (appen bruker ikke refresh-flyten ennå) |
| `token_invalid_scope` | Token-endepunktet innvilger ugyldig scope | ✅ automatisk (lykkes — appen håndhever ikke scope, kjent/akseptert) |
| `request_invalid_token` | Ugyldig access token ved FHIR-ressurskall | ⚠️ ikke eksponert av denne testkjeden, se merknad under |
| `request_expired_token` | Utløpt access token ved FHIR-ressurskall | ⚠️ ikke eksponert av denne testkjeden, se merknad under |

Kjør et scenario:

```powershell
.\local-dev\smarthealthit-testing\test-smart-launch.ps1 -ClientType public -SimulatedError token_invalid_token
```

Sjekk: aldri en ASP.NET-utviklerfeilside/stacktrace, aldri en hemmelighet synlig i responsen, og en tydelig loggmelding i `dotnet run`-konsollet.

**Merknad om `auth_invalid_client_secret`:** for at denne skal teste noe reelt, må den kjøres med `-ClientType secret` og en app konfigurert med en *annen* verdi enn den launcheren forventer — men launcheren i Loose-modus godtar uansett en hvilken som helst verdi, så denne feilen kan i praksis ikke trigges uten Strict-validering (se §6). Kjørt med Public client gir den ingen effekt siden ingen hemmelighet sendes i det hele tatt.

**Merknad om `request_invalid_token`/`request_expired_token`:** disse rammer `FhirPrefillService` sine FHIR-kall, som skjer når appens forside lastes — *etter* at `test-smart-launch.ps1` sin siste `curl`-forespørsel (callback-redirecten) er ferdig. For å faktisk eksponere dette må du følge cookien videre inn til appens forside (`curl -b <cookie-jar> http://local.altinn.cloud:8000/digdir/forer-legeerklaering`) og sjekke om FHIR-prefillen feiler trygt. Ikke gjort i denne runden — se §6.

## 6. Kjente begrensninger

Det denne testrunden **beviser**: appen bygger og sender korrekte OAuth/RFC 7523-forespørsler, og feilresponser fra en ekte ekstern SMART-server håndteres uten uhåndterte exceptions eller lekkasje av hemmeligheter.

Det denne testrunden **ikke** beviser:

1. **Kryptografisk signaturverifisering av `private_key_jwt`** — kun strukturell (Loose) validering er testet. For en ekte test: bytt launcherens Client Identity Validation til Strict, lim inn den offentlige JWK-en (`smart-client-assertion-public-jwk.json`) i JWKS-feltet, og bekreft at en assertion signert med feil nøkkel faktisk avvises.
2. **`auth_invalid_client_secret` med en reell forventet verdi** — krever Strict-modus av samme grunn som over.
3. **`request_invalid_token`/`request_expired_token`** mot selve FHIR-prefill-steget (kun kodeinspisert, ikke reprodusert) — se merknad i §5.3.
4. **Appens egen tokenvalidering** — signatur, issuer, audience, utløpstid på et token som *ser* gyldig ut, er ikke implementert noe sted i appen ennå. Denne testrunden bekrefter kun at *feilresponser fra EPJ-en* håndteres trygt. Se [RISIKOREGISTER.md R4](RISIKOREGISTER.md).
