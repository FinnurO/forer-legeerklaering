# NHN-dokumentasjon — SMART App Launch + Førerrett-App

**Kilde:** Helsenorge Confluence (helsenorge.atlassian.net)  
**Hentet:** 2026-06-17  
**Sider:**
- [Implementasjonsguide SMART App Launch Framework](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/67469415/Implementasjonsguide+SMART+App+Launch+Framework) (oppdatert 7. mai 2025)
- [Smart-On-Fhir Førerrett-App](https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/2846392337/Smart-On-Fhir+F+rerrett-App)

---

## 1. Implementasjonsguide SMART App Launch Framework

### Omfang og brukstilfelle

NHNs implementasjonsguide dekker **Use Case 4**: tredjepartsapplikasjoner for helsepersonell som startes fra EPJ eller portal. Det er nøyaktig dette brukstilfelle `forer-legeerklaering`-PoC-en implementerer.

> "SMART gir tredjepartsapplikasjoner autorisert tilgang til data i elektroniske pasientjournaler via en pålitelig og sikker autorisasjonsprotokoll"

### Autorisasjonsflyt (seksjon A–G)

Flyten samsvarer med SMART App Launch IG v2.2.0 og er den samme som PoC-en implementerer:

1. EPJ åpner app i integrert nettleser med `iss` (FHIR-endepunkt) og `launch` (unik kontekstidentifikator)
2. App henter SMART-metadata fra `/metadata/` eller `/.well-known/smart-configuration.json`
3. App redirecter til autorisasjonsendepunkt med `response_type=code`, `client_id`, `redirect_uri`, `scope`, `state`, `aud`
4. Autorisasjonsserver returnerer kode
5. App bytter kode mot access token via POST til token-endepunkt
6. App bruker Bearer-token i Authorization-header mot FHIR-APIet

Eksempel på autorisasjonsforespørsel:
```
GET https://ehr/authorize
  ?response_type=code
  &client_id=app-client-id
  &redirect_uri=https://app/after-auth
  &launch=xyz123
  &scope=launch+patient/Observation.read+patient/Patient.read+openid+fhirUser
  &state=98wrghuwuogerg97
  &aud=https://ehr/fhir
```

### Scopes — tre kategorier

| Kategori | Eksempler | Formål |
|---|---|---|
| Kliniske data | `patient/Observation.read`, `user/Observation.read`, `patient/*.read` | Tilgang til FHIR-ressurser for pasient eller bruker |
| Kontekstuell | `launch`, `launch/patient` | Motta kontekst fra EPJ / velg pasient ved frittstående start |
| Identitet | `openid`, `fhirUser` | OpenID Connect — returnerer `id_token` med brukerinformasjon |

### Sikkerhetskrav

| Krav | Detalj |
|---|---|
| Transport | TLS påkrevd for all kommunikasjon |
| `state`-parameter | Minimum 122-bit entropi; must binde til brukersesjon mot CSRF og session fixation |
| Response-headere | `Cache-Control: no-store` og `Pragma: no-cache` på token-responser |
| Klienttyper | Offentlige apper: ingen client secret, HTTPS redirect_uri-validering. Konfidensielle apper: HTTP Basic auth (`client_id:client_secret`) |
| Autorisasjonskoder | Kort levetid (typisk ~1 minutt) |
| Refresh tokens | Bundet til samme `client_id`, samme eller delmengde av original autorisasjon |
| Lagringssted | Tokens skal lagres i applikasjonsspesifikt lagringssted, ikke systemtilgjengelig storage |
| Revisjon | Audit-logging av tilgangsbeslutninger påkrevd |
| Risikostyring | Risikovurdering påkrevd før implementasjon |

### Governance

- Refererer til Normen (norsk rammeverk for informasjonssikkerhet i helse)
- Refererer til Direktoratet for e-helses arkitektur for datadeling og dokumentdeling
- Databehandleravtaler påkrevd der relevant
- Lenker til HelseAPI-implementasjonsguide

---

## 2. Smart-On-FHIR Førerrett-App (NHNs produksjonsapp)

Dette er **produksjonsimplementasjonen** av nøyaktig det `forer-legeerklaering`-PoC-en søker å bevise. NHN har altså allerede bygget og driftsatt en slik løsning for Statens vegvesen.

### Formål

> Genererer helsevurderingskonklusjoner basert på Norges «Førerkortveileder» gjennom registrert helseinformasjon.

Legen bruker appen til å fylle ut IS-2569 (helseattest for førerrett) med beslutningsstøtte, og appen sender konklusjonen elektronisk til Statens vegvesen.

### Kjernefunksjoner

- **Automatisk datafylling** — pasient- og legeinformasjon hentes fra EPJ via FHIR
- **Sykdomsspesifikke vurderingsområder** med betinget beslutningslogikk (f.eks. insulinbruk ved diabetes utløser tilleggssjekker)
- **Beslutningsstøtte i sanntid** basert på regulatoriske krav i Førerkortveiledere
- **Kontekstuell veiledning** med lenker til offisielle retningslinjer
- **Egenerklæring fra innbygger** — legen kan se pasientens selvrapporterte helseerklæring (tilsvarer NA-0201) der denne er fylt ut på Helsenorge

### Arbeidsflyt

1. Lege velger pasient i EPJ → starter appen via SMART EHR Launch
2. Gjennomgår pasientens selvrapporterte helseerklæring (om tilgjengelig)
3. Fyller ut strukturert helsevurdering på tvers av flere sykdomskategorier
4. System stiller betingede oppfølgingsspørsmål basert på svar
5. Genererer hovedkonklusjon med anbefalinger
6. Skaper dokumentasjon for arkivering og regulatorisk innsending

### Outputs — tre PDF-versjoner

| Dokument | Mottaker |
|---|---|
| Journalnotat | Arkiveres i EPJ |
| Legekopi (PDF) | Legen |
| Borgerkopi (PDF) | Innbygger via Helsenorge |
| Trafikkstasjonskopi (PDF) | Statens vegvesen |
| Elektronisk innsending | Statens vegvesen (automatisk) |

### Teknisk plattform

- **Autentisering:** HelseID (obligatorisk for integrerte EPJ-er)
- **Skjemakatalog:** Helsenorge-plattformen (ikke Altinn Studio)
- **Journalskriving:** Oppretter journaloppføringer og PDF-dokumenter i EPJ ved fullføring
- **Innsending:** Elektronisk overføring av godkjente konklusjoner til Statens vegvesen
- **«Smart register»:** Leger som bruker appen må registreres — plattformen sporer hvilke helsepersonell som har adoptert apper, og muliggjør tilpasning av plattformfunksjoner
- **Minimale FHIR-krav:** Tillater delvis implementasjon hos EPJ-leverandører

### Kontakt og tilgang

- Slack-kanal for utviklere: `ext-utv-hn-forerrett` på Helsenorge Slack
- Demo-tilgang krever direkte kontakt med Helsenorge-organisasjonen

---

## 3. Nøkkelobservasjoner for `forer-legeerklaering`-prosjektet

### Bekreftelser

- NHNs guide bekrefter at vår SMART EHR Launch-implementasjon (flyt, scopes, PKCE, state, konfidensielt klient) er korrekt og i tråd med norsk sektornorm.
- `aud`-parameteren skal være FHIR-base-URL (iss) — slik vi allerede har implementert.
- HTTP Basic auth for konfidensielle klienter er riktig mønster.

### Viktigste gap mot NHNs produksjonsapp

| Område | `forer-legeerklaering` PoC | NHN Førerrett-App (prod) |
|---|---|---|
| Plattform | Altinn Studio | Helsenorge |
| Autentisering | Altinn/ID-porten (HelseID planlagt) | HelseID (obligatorisk) |
| IS-2569-dekning | ~4 av 17+ helsekategorier | Komplett med betinget logikk |
| Egenerklæring (NA-0201) | Planlagt (PASIENTFLYT.md) | Implementert — legen ser den |
| Writeback til EPJ | Ikke implementert | Journalnotat + PDF i EPJ |
| Innsending til SVV | Ikke implementert | Elektronisk automatisk |
| «Smart register» | Ikke relevant (Altinn) | Obligatorisk |
| Beslutningsstøtte | Ingen | Fullt implementert |

### Strategisk implikasjon

NHNs løsning og Digdirs PoC er **komplementære, ikke konkurrerende**:
- NHN bruker **Helsenorge** som skjemaplattform med native HelseID-integrasjon
- Digdir PoC bruker **Altinn Studio** som skjemaplattform med Altinn-infrastruktur (signering, arkiv, XACML)

Spørsmålet om hvilken plattform som er riktig for fremtidige helseskjemaer (Altinn vs. Helsenorge) er en åpen arkitekturavklaring som bør løftes i `BESLUTNINGER.md`.
