# Innlogging: HelseID og ID-porten — hva vi har skrevet, og hva som er riktig

**Dato:** 2026-11-12
**Utløst av:** Johann påpekte at helsepersonell logger inn på Helseaktørportalen med HelseID, ikke ID-porten. Det er oppgitt av Johann og **ikke selvstendig verifisert her** — en delegert kilde fant ikke portalen (se §6).
**Kilder:** HelseID-dokumentasjonen på utviklerportal.nhn.no og nhn.no, Altinns dokumentasjon, og GitHub-repoet `Altinn/altinn-authentication`. De ble lest via delegerte oppslag 2026-11-12, ikke direkte av meg, og hver påstand under er merket etter hva som faktisk ble funnet.

---

## 1. Kort svar

- **Vi bruker HelseID riktig der vi faktisk bruker det:** system-til-system mot Helsenorge EksternAPI (`client_credentials`, ingen bruker involvert), og som tenkt tilleggsflyt for HPR-nummer.
- **Vi har skrevet en påstand som ikke holder:** at «HelseID autentiserer brukeren via ID-porten» og at ID-porten er HelseIDs «identitetsrot». HelseID er en føderasjon uten egen innlogging, og ID-porten er bare én av flere identitetsleverandører bak den, og ikke den foretrukne for helsepersonell.
- **Den påstanden bar en konklusjon som heller ikke holder:** at SSO mellom EPJ og Altinn «skjer naturlig» via felles fødselsnummer. Felles fødselsnummer lar oss *koble* to innlogginger til samme person; det er ikke SSO.
- **Vår Altinn-statusbeskrivelse er utdatert:** den sier at Altinn «kun» støtter ID-porten, Feide og UIDP. Altinns kode har siden september 2026 fått HelseID-relaterte endringer, men ekte HelseID-innlogging er ikke verifisert og vi fant ingen dokumentasjon eller bekreftelse for TT02 eller produksjon.
- **Vi har et eget hull:** appens Altinn-tilgangsregel er standardmalens, og sjekker ikke at brukeren er helsepersonell (§5).
- **Føring 2026-11-12: bruk av HelseID er avgjørende.** ID-porten for helsepersonell i portalmodus er derfor et midlertidig PoC-valg, ikke et sted vi kan lande. Det åpner et større spørsmål enn innloggingen, fordi Altinns instansmodell og lagring hviler på Party. Analysen er samlet i [ARKITEKTURVURDERING-SEKTORPLATTFORM.md](ARKITEKTURVURDERING-SEKTORPLATTFORM.md).

## 2. Hvem logger inn hvor

| Hvem | Hvor | Innlogging | Vurdering |
|---|---|---|---|
| Pasient | Altinn-skjema, Helsenorge | ID-porten. HelseID tillater uttrykkelig ikke innbyggerinnlogging, og peker på ID-porten for det | ✅ Riktig, og står i `PASIENTFLYT.md` |
| Lege / tannlege | EPJ | EPJ-ets egen pålogging, typisk via HelseID. Appen mottar identiteten som `fhirUser` etter SMART-launch | ✅ Appen validerer ikke selv hvem brukeren er, men får det fra EPJ-et |
| Lege / tannlege | Altinn, uten SMART («portalmodus») | I dag ID-porten, fordi det er det Altinn tilbyr | ⚠️ Ikke den innloggingen helsepersonell er vant til, og gir ikke HPR-nummer. Se §4 og §5 |
| Appen som system | Helsenorge EksternAPI | HelseID `client_credentials` med `private_key_jwt` og DPoP | ✅ Riktig bruk |
| Appen som system | HPR (oppslag på HPR-nummer) | Maskinporten | ✅ Slik planlagt |
| Utvikler | HelseIDs selvbetjening for test | Ikke verifisert hvilken innlogging som brukes (§6) | ❓ |

## 3. Hva som er verifisert om HelseID

- **Ingen egen innlogging.** HelseID er en identitetsføderasjon; brukeren velger en ekstern identitetsleverandør. Standardlisten er Buypass, Commfides, BankID, ID-porten (nivå 3 eller 4) og Helse Midt-Norge RHF. NHN sier at HelseID ikke har noen egen eID. Kilder: HelseIDs side om å styre innlogging, og HelseIDs spørsmål og svar.
- **ID-porten er et tillegg, ikke utgangspunktet.** Dokumentasjonen anbefaler Buypass, Commfides og BankID, og kaller ID-porten et «ekstra alternativ». Den sier også at ID-porten ikke er tilgjengelig på Helsenettet.
- **Berikede opplysninger.** HelseID legger til HPR-nummer (`helseid://claims/hpr/hpr_number`), `pid` (fødselsnummer), `security_level` (2, 3 eller 4), nettverk og organisasjonsopplysninger. NHN sier at oppslag mot HPR og PREG ikke er mulig via ID-porten.
- **Hvilken leverandør som ble brukt** ligger i `idp`-claimet (for eksempel `idporten-oidc`), og en klient kan sende brukeren rett til én leverandør med `acr_values`.
- **Ikke funnet:** noen uttrykkelig regel om at helsepersonell *ikke* kan logge inn via ID-porten, ingen omtale av Helseaktørportalen i HelseID-dokumentasjonen, og ingen beskrivelse av at en annen tjeneste (for eksempel en Altinn-app) kan gjenbruke en HelseID-sesjon.

## 4. Det vi har skrevet som må rettes

| Hvor | Påstand | Status |
|---|---|---|
| `KRAVSPESIFIKASJON-v0.6.md` §6.6 og tabell | «HelseID bruker ID-porten som rot-identitetstjeneste», med tilhørende diagram | ❌ Feil. Rettet |
| Samme sted | «SSO skjer naturlig gjennom den felles tillitskjeden» | ❌ Ikke belagt. Felles `pid` gir korrelasjon, ikke SSO. Rettet |
| `IMPLEMENTERING.md` §14 | Samme påstander, og at `pid` er «identisk» med det Altinn mottar | ❌ / ⚠️ Begge kan være fødselsnummer, men dokumentasjonen sier ikke at de er identiske. Rettet |
| `VEIKART.md` fase 1b, `DOBBEL-INNGANGSMODUS.md` §3.3 | Altinn støtter «kun ID-porten, Feide og UIDP»; kilde-lenken gir 404 | ⚠️ Utdatert. Rettet, se §5 |
| `site/flyt/` | «logge inn i Altinn som vanlig (ID-porten)» | ⚠️ Riktig for Altinn i dag, men sier ikke at helsepersonell ikke bruker det til vanlig. Presisert |
| `IMPLEMENTERING.md` §14 og `KRAVSPESIFIKASJON` | «Logg inn med ID-porten» på HelseIDs selvbetjening | ❓ Ikke verifisert. Merket |

## 5. Hva som gjelder for appen

**Altinn og HelseID (status 2026-11-12).**
- Altinns dokumentasjonsside om OIDC-leverandører (nå under `/nb/`, sist endret september 2025) lister bare FEIDE og UIDP som godkjente, og nevner ikke HelseID.
- I `Altinn/altinn-authentication` er det flettet endringer i september 2026: konfigurerbar kobling av identitetsleverandørens claims og `acr` (PR #2164), `private_key_jwt` (#2174 og #2175) og støtte for PAR, DPoP og streng validering for FAPI-leverandører (#2178). Hver funksjon slås på per leverandør. Beskrivelsen av #2178 sier at en ekte HelseID-innlogging ikke er verifisert av resultatene. Akseptansepunktet «verifiser faktisk HelseID-innlogging i AT22» var ikke avhuket da saken ble lukket.
- Repoet er arkivert og skrivebeskyttet siden 16. oktober 2026. Hvor arbeidet fortsetter, er ikke undersøkt.
- **Ikke funnet:** at HelseID er slått på i TT02 eller produksjon, hvordan en app melder seg på en bestemt leverandør, og noen offisiell dokumentasjon om HelseID som Altinn-innlogging.
- Konsekvens: det er ikke grunnlag for å si at «HelseID ikke kan brukes mot Altinn», og heller ikke for å si at det kan, før noen har prøvd det mot et testmiljø.

**Appens egen tilgangsregel er standardmalens.** `policy.xml` i førerrett-appen gir rettighetene til rollene Privatperson (PRIV) og Daglig leder (DAGL) med minimum autentiseringsnivå 2. Kjeveortopedisk-appen har samme roller. Partstypen er satt til «person». Det betyr at i portalmodus kan enhver person som kan logge inn i Altinn starte appen; ingenting sjekker at hun er helsepersonell, og nivå 2 er lavere enn nivå 3 eller 4, som er nivåene HelseIDs standard identitetsleverandører tilbyr. Dette står ikke i risikoregisteret fra før (R12 er lagt til).

**Helsenettet.** HelseID-dokumentasjonen sier at ID-porten ikke er tilgjengelig på Helsenettet. Om en lege på et lukket nett i det hele tatt kan nå en Altinn-app, og med hvilken innlogging, er ikke undersøkt. Portaloppdraget ([PORTALOPPDRAGET.md](PORTALOPPDRAGET.md)) krever at felles løsninger er tilgjengelige både fra Helsenettet og åpent internett.

**SSO.** Ingen kilde sier at Altinn kan gjenbruke en HelseID-sesjon fra EPJ-et. Det vi kan gjøre i dag er å koble to innlogginger til samme person via fødselsnummeret, noe som krever at legen logger inn to ganger: HelseID i appens tilleggsflyt og ID-porten i Altinn. Det er allerede beskrevet i `VEIKART.md` fase 1b og `RISIKOREGISTER.md` R10, men uten at det sto uttrykkelig at det er dobbel innlogging.

**SMART-launch.** Her kommer identiteten fra EPJ-et (`fhirUser`), ikke fra Altinn. Altinn-sesjonen er en egen sak som i dag løses med en testbruker i Development (R10).

## 6. Det som ikke er avklart

1. **Helseaktørportalen:** hvem eier den, hvilken innlogging den krever, og om den støtter SSO eller kontekstoverføring fra EPJ. Delegert oppslag fant ingen omtale; adressen det ble forsøkt mot ga DNS-feil. Må avklares av noen som kjenner portalen.
2. **Hvilke identitetsleverandører fastlegene faktisk bruker** i sine EPJ-er, og om noen av dem bare kan nå HelseID (og ikke ID-porten).
3. **Om og når HelseID blir en støttet Altinn-innlogging**, og hvordan en app skal melde seg på. Åpent strategisk spørsmål i `VEIKART.md` fase 1b. Etter føringen 2026-11-12 er spørsmålet ikke lenger *om* HelseID trengs, men *hvordan*: som leverandør i Altinn, som app-intern tilleggsflyt, eller i en egen helseinstallasjon ([ARKITEKTURVURDERING-SEKTORPLATTFORM.md](ARKITEKTURVURDERING-SEKTORPLATTFORM.md)).
4. **Hvilken innlogging HelseIDs selvbetjening bruker** for utviklere (ID-porten, Test-IdP eller HelseID test).
5. **Om en HelseID-innlogging i Altinn ville gi HPR-nummeret automatisk**, slik tilleggsflyten i dag må hente det.

## 7. Foreslåtte tiltak

| Tiltak | Hvor |
|---|---|
| Stram inn `policy.xml` og krev høyere autentiseringsnivå, og avklar hva som skal til for å kreve HPR | R12, `VEIKART.md` fase 1 |
| Undersøk om en HelseID-basert app kan kjøre uten Altinn Party, Dialogporten og arbeidsflate (liten PoC, målrettet analyse av Storage og instansmodell) | [ARKITEKTURVURDERING-SEKTORPLATTFORM.md](ARKITEKTURVURDERING-SEKTORPLATTFORM.md), `BESLUTNINGER.md` C-8, R13 |
| Skriv ned eksplisitt at portalmodus i dag betyr dobbel innlogging for helsepersonell | `DOBBEL-INNGANGSMODUS.md` |
| Avklar punktene i §6, spesielt 1 og 3, med NHN og Altinn Studio-teamet | `BESLUTNINGER.md` C-2 og C-6 |

## 8. Kilder

- HelseID, «Styre innlogging i HelseID» og «Spørsmål og svar», utviklerportal.nhn.no og nhn.no/tjenester/helseid.
- HelseID, «Claims» (teknisk referanse), utviklerportal.nhn.no.
- Altinn Docs, «OIDC providers» (`/nb/technology/architecture/capabilities/runtime/security/authentication/oidcproviders/`).
- `Altinn/altinn-authentication`: issues #2163 og #2176, PR #2164, #2174, #2175 og #2178.
