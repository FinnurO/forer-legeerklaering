# Arkitekturvurdering: Altinn Studio som gjenbrukbar plattform på tvers av sektorer

**Dato:** 2026-11-12
**Status:** Foreløpig arkitekturanalyse, grunnlag for videre utredning. Ikke en beslutning.
**Opphav:** Analysen i del B ble levert av Johann 2026-11-12 med føringen «bruk av HelseID er helt avgjørende». Forfatter er ikke oppgitt. Teksten er overtatt med lett redigering av formatering; innholdet er ikke endret. Del A er vår egen innplassering i prosjektet og del C er vår kontroll av kodepåstandene.
**Se også:** [INNLOGGING-HELSEID-IDPORTEN.md](INNLOGGING-HELSEID-IDPORTEN.md) (hva vi har skrevet om HelseID og ID-porten, og hva som er riktig), [DOBBEL-INNGANGSMODUS.md](DOBBEL-INNGANGSMODUS.md), [BESLUTNINGER.md](BESLUTNINGER.md) C-8, [RISIKOREGISTER.md](RISIKOREGISTER.md) R13.

---

# Del A — Hva dette betyr for prosjektet

## A.1 Føringen: HelseID er avgjørende

Det er nå en forutsetning at helsepersonell logger inn med HelseID, ikke ID-porten. Det endrer hvordan funnene i [INNLOGGING-HELSEID-IDPORTEN.md](INNLOGGING-HELSEID-IDPORTEN.md) skal leses:

- Valget «vente på HelseID i Altinn, eller akseptere ID-porten med strammere nivå» er ikke lenger like åpent. ID-porten for helsepersonell i portalmodus er et **midlertidig PoC-valg**, ikke en løsning vi kan lande på.
- Spørsmålet er heller **hvordan** HelseID kommer inn: som innloggingsleverandør i Altinns felles løsning, som app-intern tilleggsflyt (dagens plan i [VEIKART.md](VEIKART.md) fase 1b), eller i en egen helseinstallasjon som del B beskriver.
- SMART-launch er upåvirket. Der autentiserer EPJ-et legen (typisk med HelseID), og appen får identiteten som `fhirUser`.

## A.2 Hva analysen sier som er nytt for oss

1. **Problemet er større enn innloggingen.** Selv om Altinn får en HelseID-leverandør, hviler instansmodellen og lagringen på Altinns `Party`. En helsepersonell-identitet med HPR-nummer og virksomhetstilknytning er ikke det samme som en Altinn-part. Å bytte innlogging løser derfor bare første lag. Se del C for hva vi har kontrollert.
2. **Tre veier**, ikke to: generalisere Altinns felleskomponenter, kopiere separat, eller felles kodegrunnlag med separate installasjoner (del B §5). Analysen peker på den tredje.
3. **Autentisering, identitetskontekst og autorisasjon bør holdes adskilt.** Det er forklaringen på R12: i dag sjekker appens tilgangsregel bare Altinn-rolle og nivå, ikke hvem brukeren er i helsesektoren.
4. **Sektorgrensen er en sikkerhetsegenskap.** En HelseID-bruker skal ikke automatisk få Altinn-rettigheter, og omvendt (del B §3.2).

## A.3 Hvordan det passer med det vi har bygget

| Vår komponent | Plassering i analysen |
|---|---|
| PoC-en (førerrett, kjeveortopedisk) | Kjører som **Altinn-profil**: ID-porten, Altinn Party, Altinn Storage. Det er riktig for en demonstrasjon, men ikke en helseprofil |
| SMART-launch og BFF | Delvis uavhengig av Altinn-identiteten, siden identiteten kommer fra EPJ-et. Hindrer ikke en helseprofil |
| HelseID-tilleggsflyten (fase 1b) | En **omvei** rundt at Altinn ikke har HelseID: legen logger inn to ganger og koblingen skjer via fødselsnummer. Analysen peker på at dette ikke løser instans- og autorisasjonsmodellen |
| Helsenorge EksternAPI (Oppgave, Skjema, Brevtjeneste) | Er allerede en HelseID-basert helsesektorløsning, og er det nærmeste vi har en «sektorinstallasjon» som tar imot og leverer data. Relevant for C-6 og for mottak og arkiv i del B §7 |
| C-1 «hvem er parten» | Må leses på nytt: svaret er også et svar på hvem som kan eie en instans i en helseinstallasjon |
| R12 (tilgangsregel) | Symptom på at helsekonteksten ikke finnes i Altinns modell |

## A.4 Foreslått innplassering (ikke besluttet)

- **Nå, uten avklaring:** stram inn `policy.xml` (R12). Bygg PoC-en slik at helsespesifikk logikk (HPR, modus, mottak) ligger bak egne grensesnitt, ikke spredt gjennom Altinn-koden.
- **Neste:** målrettet teknisk analyse av Storage, instansmodellen og Apps Runtime (del B §15), og et lite PoC som kjører én app med HelseID uten Party, Dialogporten og arbeidsflaten (del B §12). Vi har allerede en app (`AppKjeveortopedisk`) som kan være kandidat, siden den ikke er tilknyttet førerrettsløpet.
- **Beslutning for Johann og Altinn Studio-teamet:** om dette skal følges som en egen retning. Lagt som [BESLUTNINGER.md](BESLUTNINGER.md) C-8.

---

# Del B — Analysen

*Overtatt slik den ble levert, med lett redigering.*

## B.1 Sammendrag

Altinn Studio og Apps Runtime har potensial til å være et gjenbrukbart fundament for utvikling og kjøring av digitale tjenester på tvers av offentlig sektor. I dagens arkitektur er løsningen imidlertid tett knyttet til Altinns egne felleskomponenter og domenemodeller.

Dette gjelder særlig:

- ID-porten og Altinns autentiseringsflyt
- Altinn Party og representasjonsmodellen
- Altinn Autorisasjon
- Storage og instansmodellen
- Dialogporten og Altinns arbeidsflate
- Register- og mottakerintegrasjoner

Dette gjør det krevende å ta i bruk sektoridentiteter som HelseID uten samtidig å måtte tilpasse eller generalisere store deler av Altinns eksisterende økosystem.

Det er særlig to mulige arkitekturretninger:

1. **Generalisere Altinns felleskomponenter:** utvikle mer generiske identitets-, autorisasjons-, instans- og lagringsmodeller som kan støtte flere sektorer.
2. **Gjenbruke teknologien, men separere domenene:** beholde Altinns eksisterende komponenter for Altinn, men etablere egne instanser eller sektorvarianter av nødvendige komponenter der andre sektorer har andre behov.

Den andre retningen fremstår som et interessant alternativ. I stedet for å gjøre Altinns domenemodell universell, kan man gjenbruke kode og teknologi, men la sektorene ha egne datamodeller, identitetsløsninger, autorisasjonsregler og arbeidsflater.

Målet bør være én felles utviklingsplattform og et gjenbrukbart teknologisk fundament, ikke nødvendigvis én felles kjørende infrastruktur eller én universell domenemodell.

**Foreløpig anbefaling:** undersøk om Altinn Studio og Apps Runtime kan videreutvikles som en felles applikasjonsplattform med tydelige integrasjonsgrenser, og om nødvendige komponenter som Storage og instanshåndtering kan gjenbrukes i separate sektorinstallasjoner.

## B.2 Bakgrunn og problemstilling

Altinn 3 representerer en betydelig modernisering av den norske løsningen for digitale offentlige tjenester. Plattformen har et bredt økosystem av felleskomponenter som sammen gir autentisering, autorisasjon, innsending, lagring, dialog og oppfølging. Denne integrasjonen er en styrke når tjenestene skal utvikles og kjøres innenfor Altinns eget økosystem.

Utfordringen oppstår når andre sektorer ønsker å bruke den samme skjemaplattformen, men allerede har egne identitetsløsninger, registre, autorisasjonsmodeller og arbeidsflater. Helsesektoren er et tydelig eksempel: HelseID, HPR-registeret, helsesektorens organisasjons- og tilgangskontekst og egne behov for arbeidsflyt og arkivering. SIKT, Forsvaret og andre sektorer kan ha egne krav og løsninger.

Det sentrale spørsmålet:

> Skal Altinn Studio primært være en plattform for å utvikle og kjøre Altinn-tjenester, eller skal det være en generell plattform for utvikling og kjøring av digitale offentlige tjenester, der Altinn er én av flere mulige integrasjonsprofiler?

## B.3 Identitet: HelseID og ID-porten

### B.3.1 To ulike identitetsdomener

ID-porten og HelseID er separate identitetsløsninger med ulike bruksområder og tillitskontekster. Begge kan tilby autentisering med høy sikkerhet, men samme sikkerhetsnivå betyr ikke at identitetene eller tillitsgrunnlaget er utbyttbare.

HelseID kan i tillegg inngå i en helsesektorkontekst med opplysninger som:

- helsepersonells profesjonelle identitet, for eksempel HPR-nummer
- tilknytning til virksomhet eller helseorganisasjon
- sektorens egne identitets- og tilgangsopplysninger

ID-porten inngår på sin side i Altinns etablerte identitets- og tjenestekontekst.

### B.3.2 Risiko ved å behandle identitetene likt

Hvis Altinn kun vurderer autentiseringsnivået, uten å bevare informasjon om hvilken identitetsleverandør og tillitskontekst som ligger bak, kan det oppstå uønsket sammenblanding. En HelseID-autentisert bruker skal ikke automatisk få tilgang til tjenester eller representasjonsforhold som forutsetter autentisering innenfor Altinns tillitsmodell.

Det er derfor viktig å skille mellom:

- hvem brukeren er
- hvordan brukeren er autentisert
- hvilken identitetsleverandør som har autentisert brukeren
- hvilket sikkerhetsnivå og hvilket tillitsgrunnlag som gjelder
- hvilken virksomhet eller rolle brukeren opptrer i
- hvilken autorisasjon brukeren har for den aktuelle handlingen

Autentisering, identitetskontekst og autorisasjon bør ikke blandes sammen til én generell vurdering.

### B.3.3 En mulig tilnærming

Man kan forsøke å generalisere Altinns identitetsmodell slik at den støtter flere identitetsleverandører, men det er ikke sikkert at dette er den beste første tilnærmingen. Et alternativ er å la Altinn-profilen beholde ID-porten og Altinns eksisterende identitetsmodell, mens en helseprofil bruker HelseID og helsesektorens egne identitets- og autorisasjonsmekanismer. Det reduserer behovet for å gjøre alle eksisterende Altinn-komponenter sektoragnostiske.

## B.4 Party-modellen og instansbegrepet

### B.4.1 Party er mer enn en identifikator

Altinns Party-modell brukes ikke bare til å identifisere en bruker eller virksomhet, men inngår også i:

- representasjon og partsforhold
- instanseierskap
- lagring og oppslag
- autorisasjon
- dialoger og arbeidsflate
- relasjoner til registre og organisasjoner

Party kan derfor ikke uten videre erstattes med en annen identifikator. I helsesektoren kan en bruker opptre som helsepersonell, med en profesjonell identitet og en bestemt organisatorisk kontekst. Det er ikke nødvendigvis det samme som å opptre på vegne av en Altinn Party.

### B.4.2 Kodeobservasjoner (kontrollert, se del C)

- `InstanceOwner` i Storage har `PartyId` som en sentral identifikator.
- Instans-ID-en er bygget opp rundt PartyId og en instans-GUID.
- Storage bruker PartyId i oppslag og håndtering av instanser.
- Eldre grensesnitt i app-biblioteket bruker `instanceOwnerPartyId` i flere operasjoner.

Dette viser at utfordringen ved å støtte andre sektorer ikke bare handler om å bytte ut ID-porten. Instansmodellen og lagringsmodellen har også semantiske avhengigheter til Altinns Party-modell.

### B.4.3 Konsekvens

En sektor som ønsker å bruke HelseID, men ikke Altinn Party, kan ikke nødvendigvis ta i bruk dagens instansmodell uendret. Man må enten:

1. generalisere instansmodellen slik at den støtter ulike typer instanseiere og identitetskontekster
2. etablere en egen sektorvariant av instansmodellen
3. gjenbruke deler av teknologien, men holde domenemodellene adskilt

Det siste alternativet er særlig interessant.

## B.5 Gjenbruk teknologi, men separer domenene

Man kan skille mellom gjenbruk av teknologiske byggeklosser og deling av domenemodeller.

### B.5.1 Tre mulige modeller

| Modell | Beskrivelse | Vurdering |
|---|---|---|
| Felles universell modell | Alle sektorer bruker samme Storage, instansmodell og identitetsmodell | Stor grad av felles drift, men risiko for domenekobling |
| Separate kodebaser | Hver sektor får egne kopier og videreutvikler dem separat | God separasjon, men risiko for teknisk divergens |
| Felles kodegrunnlag, separate installasjoner | Samme teknologiske fundament, men egne instanser, konfigurasjoner og domenemodeller | Interessant balanse mellom gjenbruk og isolasjon |

Den tredje modellen bør undersøkes nærmere.

### B.5.2 Eksempel: egen Storage-installasjon for helse

Helsesektoren kan få en egen installasjon av Storage og instanshåndtering, basert på Altinns eksisterende kode, med:

- egen instanseiermodell
- helseidentifikatorer og helsesektorens organisasjonskontekst
- eget datalager
- egen tilgangskontroll
- egen livssyklus for instanser
- egne integrasjoner mot arkiv og mottakssystemer

Altinn kan samtidig fortsette å bruke sin eksisterende Storage-installasjon og Party-modell. Det kan gi betydelig gjenbruk uten at helsesektorens identiteter, data og autorisasjonsregler blandes inn i Altinns domenemodell.

### B.5.3 Analogi med andre identitetsløsninger

Det kan være nyttig å undersøke erfaringer fra løsninger som Ansattporten og Passport, men det bør ikke legges til grunn uten egen teknisk verifisering at disse er realisert som direkte kopier av ID-porten. Det relevante prinsippet er uansett:

> Gjenbruk av teknologi krever ikke at man deler samme kjørende løsning, datamodell eller tillitsdomene.

## B.6 Altinn Studio og Apps Runtime

Studio Designer er utviklerverktøyet der applikasjoner modelleres og bygges. Apps Runtime er miljøet som faktisk kjører applikasjonene og håndterer blant annet instanser, datainnsamling, validering, prosess og arbeidsflyt, autentisering og autorisasjon, lagring og integrasjoner mot plattformtjenester.

Det er ikke tilstrekkelig å gjøre Studio Designer mer generisk dersom Apps Runtime fortsatt forutsetter Altinns identitets-, instans- og lagringsmodell.

### B.6.1 Konfigurerbare integrasjoner

`PlatformSettings.cs` viser at flere plattformendepunkter konfigureres gjennom innstillinger. Det er et positivt utgangspunkt, men man må skille mellom:

- at en tjenesteadresse kan konfigureres
- at en annen implementasjon faktisk kan brukes

Å endre URL til en annen Storage-tjeneste er ikke nok dersom API-kontrakten, tokenflyten, datamodellen eller autorisasjonsmekanismen fortsatt forutsetter Altinn.

### B.6.2 Events som mulig integrasjonsgrense

`EventsClient.cs` bruker som standard Party-baserte subjects, men har støtte for alternative subjects, blant annet knyttet til organisasjon, person og eksterne identifikatorer. Det er et positivt tegn på at deler av plattformen allerede har mekanismer for alternative identitetskontekster, og det bør undersøkes om Events kan brukes som koblingsmekanisme mot sektorens egne systemer.

## B.7 Dialogporten, arbeidsflate og innboks

Dialogporten og Altinns arbeidsflate er viktige deler av dagens Altinn-opplevelse, men trenger ikke være en forutsetning for at en skjemabasert applikasjon skal kunne utvikles og kjøres.

- **Dialogporten som valgfri integrasjon.** Studio-arkitekturens kontekstdiagram viser at Apps Runtime bruker Storage til å lagre instanser, og at Storage inngår i en synkroniseringskjede mot Dialogporten. Dialogporten fremstår dermed som en integrasjon rundt instansene, ikke selve skjemamotoren. Det må undersøkes om Apps Runtime, appkonfigurasjon, deploy og prosessflyt har direkte eller indirekte avhengigheter til den.
- **Innboks og arkiv trenger ikke eies av Altinn.** Andre sektorer har egne systemer for mottak, saksbehandling, arkivering, varsling og dialog med sluttbruker. Skjemaplattformen bør kunne levere data videre via API, webhook, hendelsesstrøm, saksbehandlings- eller arkivsystem, digital postkasse eller e-postvarsling. Det bør være mottakervirksomheten eller sektoren som bestemmer hvor innsendinger behandles og arkiveres.
- **Utkast er noe annet enn innboks.** En bruker kan ha behov for å lagre et uferdig skjema, fortsette senere, få en lenke til et påbegynt skjema og motta kvittering eller kopi etter innsending. Det krever ikke nødvendigvis Dialogporten eller Altinns arbeidsflate. Utkast kan håndteres av en egen Draft Service, og innsendte data kan leveres til sektorens egne mottaks- og arkivsystemer.

## B.8 Målarkitektur

En mulig målarkitektur er én felles Studio- og applikasjonsplattform med separate integrasjonsprofiler for ulike sektorer.

```text
                 Felles utviklingsplattform
              Altinn Studio + Apps Runtime
                           |
          +----------------+----------------+
          |                |                |
     Altinn-profil     Helse-profil    Andre sektorer
          |                |                |
      ID-porten          HelseID          Feide / andre
          |                |                |
    Altinn Party       Helseaktør       Sektoridentitet
          |                |                |
    Altinn Autor.     Sektorautor.      Egen autorisasjon
          |                |                |
   Altinn Storage     Helse Storage     Egen lagring
          |                |                |
   Dialogporten       Egen arbeidsflate  Egen arbeidsflate
          |                |                |
   Altinn arbeids-    Helsearkiv og      Sektorens mottak
      flate             mottak             og arkiv
```

Skissen viser et prinsipielt skille. Den innebærer ikke at alle komponenter må kopieres eller driftes separat; det avgjøres ut fra avhengigheter, sikkerhetsbehov og kostnader.

**Mulige felles byggeklosser:** Studio Designer, app-modell og metadata, bygg- og deploymekanismer, skjemakomponenter, valideringsmekanismer, prosess- og regelmotor, utviklerverktøy, teknologiske biblioteker og rammeverk.

**Det som bør kunne være sektorstyrt:** identitetsleverandør, identitets- og aktørmodell, organisasjons- og rollemodell, autorisasjon, instanseiermodell, Storage og livssyklus for instanser, utkastlagring, arbeidsflate og innboks, mottak og arkivering, varsling og distribusjon.

Et viktig prinsipp er at applikasjonsplattformen ikke bør eie sektorens identitets- eller autorisasjonsdomene.

## B.9 Foreløpige funn fra kodegjennomgangen

Analysen bygger på en avgrenset, read-only gjennomgang av `Altinn/altinn-studio`, `Altinn/app-lib-dotnet`, `Altinn/altinn-storage` og `Altinn/altinn-platform`. Det er en foreløpig arkitekturvurdering, ikke en uttømmende avhengighetsanalyse.

| Område | Foreløpig funn | Vurdering |
|---|---|---|
| Studio Designer | Har en tydelig egen rolle i arkitekturen | Relativt godt utgangspunkt for gjenbruk |
| Apps Runtime | Bruker flere Altinn-plattformtjenester | Krever frikobling |
| Identitet | Har Altinn-spesifikke token- og autentiseringsmekanismer | Krever abstraksjon eller alternative profiler |
| Party | Brukes sentralt i instans- og lagringsmodellen | Stor arkitektonisk utfordring |
| Storage | Har tydelig Party-kobling i instanseier og oppslag | Kandidat for sektorvariant |
| Autorisasjon | Integrert med Altinns tjenester og begreper | Krever alternativ implementasjon eller adapter |
| Dialogporten | Fremstår som en separat integrasjon rundt instansene | God kandidat for valgfri integrasjon |
| Events | Party er standard, men alternative subjects finnes | Mulig utgangspunkt for frikobling |
| Utkast og lagring | Må undersøkes nærmere mot instansmodellen | Viktig uavklart område |

Positivt: Altinn Studio skiller mellom Core, Runtime og Platform, noe som gir et mulig utgangspunkt for tydeligere integrasjonsgrenser. README-en beskriver prosjektet som DPG-verifisert og MIT-lisensiert, noe som er strategisk interessant for gjenbruk, men ikke i seg selv dokumentasjon på at implementasjonen er sektoragnostisk.

Ikke kartlagt: alle autentiserings- og tokenflyter, alle autorisasjonskall, utkast- og instanslivssyklus, direkte og indirekte Dialogporten-avhengigheter, prosess- og arbeidsflytavhengigheter, deploy- og konfigurasjonsmekanismer, og alle Party-referanser i Storage og Apps Runtime. Funnene bør derfor brukes til å prioritere videre analyse, ikke som grunnlag for en endelig estimering.

## B.10 Risiko og avveininger

- **Én felles universell modell:** flere abstraksjonslag og konfigurasjonsmuligheter, mer kompleks autorisasjonslogikk, risiko for at sektorbegreper påvirker hverandre og vanskeligere videreutvikling av eksisterende Altinn-tjenester.
- **Separate kopier:** teknisk divergens, duplisert vedlikehold, flere sikkerhetsoppdateringer, ulik funksjonalitet over tid og høyere kostnader. Ukontrollert forking bør unngås.
- **Felles kodegrunnlag, separate installasjoner:** felles kode der det er hensiktsmessig, separate deploymenter og datalagre, separate domenemodeller der behovene er ulike, klare integrasjonskontrakter, og mulighet for å videreutvikle felles tekniske komponenter uten å dele alle domenereglene. Det må likevel vurderes per komponent om den bør deles, konfigureres eller implementeres separat.

## B.11 Anbefalt retning

Det anbefales ikke å starte med et stort program for å gjøre hele Altinns felleskomponentøkosystem universelt. I stedet en trinnvis retning:

1. Behold Altinns eksisterende komponenter og modeller for Altinn-tjenester.
2. Definer tydelige grenser mellom Apps Runtime og Altinn-spesifikke tjenester.
3. Gjør integrasjoner som Dialogporten, Altinn Autorisasjon og Altinns arbeidsflate valgfrie der det er naturlig.
4. Undersøk om Storage og instanshåndtering kan gjenbrukes i separate sektorinstallasjoner.
5. La sektorene eie egne identitets-, aktør- og autorisasjonsmodeller.
6. Gjenbruk felles kode og teknologi der det gir reell gevinst, uten å tvinge frem felles domenemodeller.

## B.12 Forslag til proof of concept: én HelseID-basert applikasjon

Ta utgangspunkt i én eksisterende Altinn Studio-applikasjon og undersøk om den kan kjøre med en alternativ sektorprofil.

**Mål:** demonstrere at en applikasjon kan utvikles i Altinn Studio og kjøre uten å være avhengig av Altinns Party-modell, ID-porten, Dialogporten eller Altinns arbeidsflate.

**Omfang:**

1. Innlogging med HelseID.
2. Håndtering av profesjonell identitet og helsesektorkontekst uten Altinn Party.
3. Tilgang basert på helsesektorens autorisasjonsmodell.
4. Lagring av utkast uten å være avhengig av dagens Party-baserte instanseiermodell.
5. Innsending til en sektorstyrt mottaker.
6. Levering til sektorens eget arkiv eller saksbehandlingssystem.
7. Bruk uten Dialogporten og Altinns arbeidsflate.

**Hva skal måles:**

| Område | Spørsmål |
|---|---|
| Studio | Kan samme designer og app-modell brukes? |
| Apps Runtime | Hvor mange Altinn-spesifikke avhengigheter må erstattes? |
| Identitet | Kan HelseID integreres uten å endre Altinns eksisterende autentiseringsflyt? |
| Autorisasjon | Kan sektorens autorisasjon brukes gjennom en tydelig integrasjonsgrense? |
| Instanser | Kan instanseiermodellen byttes eller isoleres? |
| Storage | Hvor mye av eksisterende Storage-teknologi kan gjenbrukes? |
| Utkast | Kan utkast håndteres uavhengig av Altinns innboks og Party-modell? |
| Dialogporten | Kan applikasjonen fungere uten denne integrasjonen? |
| Mottak og arkiv | Kan innsending leveres til sektorens egne systemer? |
| Drift | Kan løsningen driftes som en separat sektorinstallasjon? |

**Suksesskriterier:**

- Samme Studio Designer kan brukes.
- Applikasjonen kan kjøre med HelseID.
- Helseidentitet og autorisasjon ikke må representeres som Altinn Party.
- Lagring og instanser kan håndteres uten å blande domenemodellene.
- Dialogporten og Altinns arbeidsflate ikke er obligatoriske.
- Det er mulig å gjenbruke vesentlige deler av teknologien uten å opprettholde en separat, fullstendig fork av hele plattformen.

PoC-en bør også dokumentere hvilke komponenter som må kopieres, hvilke som kan konfigureres og hvilke som bør gjøres generiske.

## B.13 Spørsmål som må avklares før en beslutning

**Arkitektur**

- Hvor går den reelle grensen mellom Apps Runtime og Altinns felleskomponenter?
- Hvilke avhengigheter er tekniske, og hvilke er semantiske?
- Kan vi etablere sektorprofiler uten å introdusere for mange abstraksjonslag?
- Hvilke komponenter bør være felles, og hvilke bør kunne installeres separat?

**Storage og instansmodell**

- Hvor omfattende er Party-avhengigheten i Storage?
- Kan dagens Storage-kode gjenbrukes med en annen instanseiermodell?
- Bør man lage en egen sektorvariant av Storage?
- Kan man skille lagringsmotor fra domenemodellen for instanser?
- Kan utkastlagring skilles fra den øvrige instanslivssyklusen?

**Identitet og autorisasjon**

- Kan autentiseringsleverandør og identitetskontekst abstraheres?
- Kan HelseID integreres uten å endre Altinns eksisterende tillitsmodell?
- Hvordan kan sektorens autorisasjon kobles til Apps Runtime?
- Hvordan sikres det at én sektors identiteter eller rettigheter ikke kan brukes i en annen sektors kontekst?

**Forvaltning og drift**

- Hvordan kan felles kode vedlikeholdes uten at sektorinstallasjoner divergerer?
- Hvilke komponenter må sikkerhetsoppdateres separat?
- Hvem skal eie og drifte sektorvariantene?
- Hvilke krav stilles til isolasjon av data, nøkler, tilgang og logger?

## B.14 Lederoppsummering

Altinn Studio er i dag tett koblet til ID-porten, Altinn Autorisasjon, Party-modellen, Dialogporten og Altinns arbeidsflate. Det gjør det vanskelig å bruke plattformen i sektorer som har egne identitets- og autorisasjonsløsninger, som HelseID. Vi bør ikke nødvendigvis forsøke å gjøre alle Altinns felleskomponenter universelle. Et alternativ er å gjenbruke teknologien og kodegrunnlaget, men etablere egne instanser og domenemodeller der sektorene har andre behov.

Forslag til formulering i et ledermøte:

> Vi bør undersøke om Altinn Studio og Apps Runtime kan bli et felles teknologisk fundament for utvikling av digitale tjenester på tvers av offentlig sektor.
>
> I stedet for å gjøre Altinns eksisterende felleskomponenter og domenemodeller universelle, kan vi gjenbruke kode og teknologi, men la sektorene ha egne instanser, identitetsløsninger, autorisasjonsmodeller og arbeidsflater der det er nødvendig.
>
> Altinn fortsetter med ID-porten, Party og Dialogporten. Helse kan bruke HelseID og helsesektorens egne modeller. Andre sektorer kan bruke sine egne løsninger.
>
> Vi får én felles utviklingsplattform, men unngår å tvinge alle sektorene inn i samme modell.

**Foreslått beslutning:** be om mandat til en avgrenset arkitekturvurdering og et proof of concept som undersøker (1) hvor tett Apps Runtime og Storage er koblet til Altinns Party- og instansmodell, (2) om felles kode kan brukes i separate sektorinstallasjoner, og (3) om én HelseID-basert applikasjon kan kjøre uten Altinn Party, Dialogporten og Altinns arbeidsflate. Målet er ikke å bygge en egen helseversjon av Altinn Studio, men å undersøke om én felles plattform kan støtte flere sektorer gjennom tydelige og isolerte integrasjonsprofiler.

## B.15 Konklusjon

Den sentrale arkitekturhypotesen:

> Altinn bør ikke nødvendigvis gjøre alle sine eksisterende felleskomponenter og domenemodeller sektoragnostiske. Det kan være mer hensiktsmessig å gjøre Studio og Apps Runtime til et gjenbrukbart teknologisk fundament, der sektorene kan bruke egne identitets-, autorisasjons-, instans- og lagringsmodeller gjennom separate integrasjoner eller installasjoner.

Dette kan være en mer trinnvis og mindre risikofylt vei mot en bredere Digital Public Good enn å generalisere hele Altinns eksisterende felleskomponentøkosystem. Den viktigste uavklarte problemstillingen er hvor mye av dagens Storage- og instansmodell som kan gjenbrukes uten å ta med seg Party-avhengigheten.

**Anbefalt neste steg:** gjennomfør en målrettet teknisk analyse av Storage, instansmodellen og Apps Runtime, og bruk en HelseID-basert PoC til å teste om felles kodegrunnlag og separate domenemodeller lar seg kombinere i praksis.

---

# Del C — Vår kontroll av kodepåstandene (2026-11-12)

Fem av filene analysen viser til (`InstanceOwner.cs`, `Instance.cs`, `SimpleInstance.cs`, `IInstance.cs` og `PlatformSettings.cs`) ble hentet fra `main` på GitHub og lest av oss samme dag. Resten ble ikke kontrollert.

| Påstand i analysen | Resultat |
|---|---|
| `InstanceOwner` i Storage har `PartyId` som sentral identifikator | ✅ Bekreftet. Klassen har `PartyId`, og i tillegg `PersonNumber`, `OrganisationNumber`, `Username` og `ExternalIdentifier`. Eieren kan altså også identifiseres uten Party-id, men `PartyId` er nøkkelen resten av modellen bruker |
| Instans-ID er bygget opp rundt PartyId og instans-GUID | ✅ Bekreftet. `Instance.Id` er dokumentert som `{instanceOwnerId}/{instanceGuid}`, og `SimpleInstance.Id` som `{instanceOwner.partyId}/{instanceGuid}`. `Instance.SetPlatformSelfLinks` parser `InstanceOwner.PartyId` som heltall |
| Eldre grensesnitt i app-biblioteket bruker `instanceOwnerPartyId` | ✅ Bekreftet i `IInstance` (blant annet `GetInstance`, `UpdateReadStatus`, `UpdateSubstatus`). Malen til `CreateInstance` sier at eieren kan oppgis med `partyId`, `personNumber` eller `organisationNumber` |
| Plattformendepunkter konfigureres i `PlatformSettings` | ✅ Bekreftet: Storage, Register, Profile, Authentication, Authorization, Events, PDF, Notification, Correspondence og AccessManagement har hvert sitt konfigurerbare endepunkt. Det sier ikke noe om hvorvidt en annen implementasjon kan stå bak |
| `EventsClient` har Party som standard og alternative subjects | ❌ Ikke kontrollert |
| Kontekstdiagrammet viser Storage → Dialogporten | ❌ Ikke kontrollert |
| README: DPG-verifisert og MIT-lisensiert | ❌ Ikke kontrollert |
| Ansattporten og Passport som analogi | ❌ Ikke kontrollert. Analysen sier selv at det ikke bør legges til grunn uten egen verifisering |
| Dialogporten-avhengigheter i Apps Runtime | ❌ Ikke kartlagt, også av analysen |

**Det kontrollen ikke avgjør:** om Party-avhengigheten faktisk hindrer en helse-installasjon. At nøkkelen er Party-basert viser at modellen ikke kan brukes uendret med HelseID alene, men det er ikke vist hvor mye som måtte endres, eller om en sektorinstallasjon av Storage med en annen eiermodell er praktisk gjennomførbar. Det er nettopp det PoC-en i B.12 skal avklare.
