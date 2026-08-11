# Risikoregister — `forer-legeerklaering`

**Sist oppdatert:** 2026-08-10
**Bakgrunn:** Konsolidert etter kvalitetssikring 2026-06-19 (Del 3), som pekte på at risikoene i BESLUTNINGER.md, VEIKART.md og STRATEGI.md sto spredt uten samlet oversikt, eierskap og tiltak.

Dette dokumentet samler risikoene som allerede er beskrevet andre steder i dokumentasjonen, med eier og tiltak i én tabell. Det erstatter ikke BESLUTNINGER.md (som går i dybden på hver beslutning) eller VEIKART.md (tekniske tiltak) — det er et styringsverktøy for å følge opp dem.

**Skala:** Sannsynlighet og konsekvens vurdert Lav / Middels / Høy. «Risiko» = sannsynlighet × konsekvens, brukt til å prioritere rekkefølge, ikke en presis beregning.

---

| ID | Risiko | Kategori | Sannsynlighet | Konsekvens | Eier (forslag) | Tiltak | Status |
|---|---|---|---|---|---|---|---|
| R1 | Fastlege-EPJ-enes FHIR-/SMART-modenhet er ukjent — underlaget har til nå referert DIPS Arena (sykehus-EPJ), ikke de faktiske fastlege-EPJ-ene | Teknisk avhengighet | Høy | Høy — PoC-ens kjernepåstand («fungerer mot EPJ») er ikke verifisert mot en reell fastlege-EPJ | Digdir teknisk team + EPJ-løftet | Test mot minst én fastlege-EPJ FHIR-sandkasse (CGM/Infodoc/WebMed) før «det fungerer» hevdes bredere | Ikke startet — se [KRAVSPESIFIKASJON-v0.6.md §6.4](KRAVSPESIFIKASJON-v0.6.md) |
| R2 | Rettslig grunnlag, behandlingsansvar og Normen-vurdering for plattformleddet er uavklart | Juridisk / personvern | Høy | Høy — blokkerer enhver reell pilot med helseaktører; ingen DPIA finnes | Personvernombud + juridisk rådgiver | Bestill juridisk avklaring + innledende DPIA + Normen-vurdering *før* utviklingsoppstart med helseaktører | Uavklart — se [BESLUTNINGER.md C-4](BESLUTNINGER.md) |
| R3 | Mottaksarkitektur og tjenesteeierskap er ikke formelt avklart — teknisk retning (to-lags modell) er satt, men SVV har ikke bekreftet Events-abonnement | Organisatorisk | Middels | Høy — uten mottaker på den andre siden har konklusjonsdataene (`ForerKonklusjonModel`) ingen bruk | Programleder + Statens vegvesen | Avklar tjenesteeier; få SVV (eller Hdir) til å bekrefte abonnement på `app.instance.process.completed`-events | Teknisk retning avklart, organisatorisk avklaring gjenstår — se [BESLUTNINGER.md C-3](BESLUTNINGER.md) |
| R4 | Sikkerhetsgap: tokenvalidering, audit-logging og issuer-allowlist er ikke implementert | Sikkerhet / compliance | Høy (før fase 1) | Høy — uakseptabelt i produksjon; audit-logging er et Normen-/NHN-krav, ikke valgfritt | Teknisk team | Fase 1 i VEIKART.md: tokenvalidering, allowlist, audit-logging inn i definisjonen av «produksjonsklar» | Dokumentert, ikke implementert — se [VEIKART.md fase 1](VEIKART.md) |
| R5 | Lav adopsjon hos fastleger — konteksbytte ut av EPJ til Altinn er svakere UX enn EPJ-native løsninger | Endringsledelse / brukeradopsjon | Middels | Middels-Høy — teknisk fungerende løsning som ingen tar i bruk i klinisk praksis | Digdir + fastlegeorganisasjoner (Legeforeningen) | Brukertest med reelle fastleger; vurder om «grønt» parallell-case (KARTLEGGING D6) er bedre første pilot enn førerrett | Ikke startet |
| R6 | Initiativet oppfattes som konkurrerende med NHNs produksjonsløsning for IS-2569 på Helsenorge | Strategisk / posisjonering | Middels | Middels — politisk sårbarhet, dobbeltarbeid, samarbeidsvilje fra NHN | Programleder | Bruk firemodell-analysen (STRATEGI.md) som felles språk med NHN; kontakt NHN-teamet (Slack `ext-utv-hn-forerrett`) for skriftlig komplementaritet | Dialog ikke bekreftet gjennomført — se [BESLUTNINGER.md C-6](BESLUTNINGER.md) |
| R7 | Ingen automatiserte tester — regresjon kan innføres uten å bli fanget opp | Teknisk kvalitet | Høy | Middels — hindrer trygg videreutvikling og bredding | Teknisk team | VEIKART.md fase 3: e2e-røyktest + unit-tester (jf. `syk-inn`: 23 unit + 24 e2e) | Ikke startet — se [VEIKART.md fase 3](VEIKART.md) |
| R8 | Full OAuth-redirect-flyt (`ERR_TOO_MANY_REDIRECTS`) er ikke løst — kun `/smart/dev-login`-workaround er bevist | Teknisk | Middels | Middels — den reelle SMART-launch-flyten er ikke bevist ende-til-ende | Teknisk team | Diagnostiser redirect-loopen mot en ekte EPJ-testklient | Uløst — se [README.md «Kjente begrensninger»](../README.md) |
| R9 | Helsenorge EksternAPI-autentisering er verifisert, men selve testmiljøtilgangen (portal + «digitalt aktive» testpersoner) krever formell NHN-leverandørkontakt — ikke selvbetjent | Organisatorisk / avhengighet | Lav (kjent prosess) | Middels — blokkerer videre verifisering av pasientsporet (PASIENTFLYT.md alt. B) inntil kontakt er tatt | Programleder | Ta kontakt via `ext-utv-hn-forerrett`-Slack eller `ide-ogbestillingsmottak@nhn.no` for testmiljøtilgang og provisjonering av testpersoner | Ikke startet — se [BESLUTNINGER.md C-6](BESLUTNINGER.md) og [IMPLEMENTERING.md §14.1](IMPLEMENTERING.md) |

---

## Prioritert lukkerekkefølge

1. **R2 (rettslig grunnlag) og R3 (mottaksarkitektur)** — begge er forutsetninger uavhengig av hvilken leveransemodell som velges (jf. [STRATEGI.md](STRATEGI.md) «Fire leveransemodeller»). Ingen dialog med helseaktører bør starte før disse har en navngitt eier og et konkret neste steg.
2. **R1 (EPJ-modenhet) og R4 (sikkerhetsgap)** — tekniske forutsetninger for at PoC-en kan kalles produksjonsklar. Kan startes uten menneskelige avklaringer.
3. **R7 og R8** — kvalitets- og robusthetsarbeid, gjøres parallelt med 1–2.
4. **R5 og R6** — adopsjon og posisjonering. Krever at 1–2 er på plass først for å ha noe reelt å vise fastleger og NHN.

Se også [BESLUTNINGER.md](BESLUTNINGER.md) for beslutningsdetaljer og [VEIKART.md](VEIKART.md) for tekniske tiltak per fase.
