# Henvisning til kjeveortopedisk vurdering — Skjemastruktur og feltanalyse

## Blankett Helfo 05-06.10 (Helfo, endret 01.2026)

**Kilde:** [Henvisning til kjeveortopedisk vurdering (PDF)](https://www.helfo.no/skjema/Henvisning%20til%20kjeveortopedisk%20behandling-05-06.10-bokm%C3%A5l.pdf/_/attachment/inline/4b72a31f-7046-4b69-8384-2e1c218a1d52:9aef015d0999649a31177af9af939c1c2d152411/Henvisning%20til%20kjeveortopedisk%20vurdering-05-06.10-bokm%C3%A5l.pdf)
**Juridisk hjemmel:** Folketrygdloven — stønad til kjeveortopedisk behandling (tannregulering) etter takster fastsatt av Helse- og omsorgsdepartementet.
**Formål:** Henvisning fra tannlege/tannpleier til kjeveortoped, med selvstendig vurdering av om pasientens bittavvik kvalifiserer for stønad (gruppe a/b/c, ulik dekningsprosent).

**Nytt case, lagt til 2026-09-09.** Dette er det andre skjemaet i prosjektet (etter IS-2569/legeerklæring førerrett) — og et nyttig sannhetstest for «helse-template»-visjonen (STRATEGI.md Spor B): henviseren her er en **tannlege/tannpleier**, ikke en lege. Samme FHIR/HPR/SMART-mønster skal i prinsippet gjelde uendret, bare med en annen behandlerkategori.

**Presisert av Johann:** hvilken informasjon som faktisk kan hentes fra et tannlege-EPJ via FHIR er ikke avklart ennå. Skjemaet skal uansett kunne fylles ut **helt manuelt, uten EPJ-kontekst** — altså samme "dobbel inngangsmodus"-krav som er dokumentert for førerrett-caset i [DOBBEL-INNGANGSMODUS.md](DOBBEL-INNGANGSMODUS.md), bare at det her er en *forutsetning fra start* snarere enn noe lagt til senere.

---

## Feltoversikt

### 1a. Informasjon om pasienten

| Felt | Type | Mulig FHIR-kilde | Merknad |
|---|---|---|---|
| Etternavn, fornavn | Tekst | `Patient.name` | |
| Fødselsnummer | Tekst | `Patient.identifier` (OID `2.16.578.1.12.4.1.4.1`) | Samme OID som førerrett-caset |
| Adresse | Tekst (flerlinjer) | `Patient.address` | **Nytt felt** — adresse er ikke modellert i `ForerLegeerklaeringModel` fra før |

### 1b. Informasjon om henvisende behandler

| Felt | Type | Mulig FHIR-kilde | Merknad |
|---|---|---|---|
| Navn på henvisende tannlege/tannpleier | Tekst | `Practitioner.name` | |
| Henviserens HPR-nummer | Tekst | `Practitioner.identifier` (OID `2.16.578.1.12.4.1.4.4`) | Samme OID — HPR dekker alt autorisert helsepersonell, ikke bare leger |
| Dato og underskrift | Dato + signatur | — | Ikke FHIR-kilde; genereres ved signering (samme «signer og send inn»-mønster som førerrett) |
| *(implisitt)* Egenerklæring | — | — | Skjemaet har en fast erklæringstekst («Jeg har foretatt en selvstendig vurdering...») rett under 1b — bør trolig bli en obligatorisk bekreftelse i det digitale skjemaet, ikke bare trykt tekst |

**Åpent spørsmål:** kan vi skille tannlege fra tannpleier ut fra HPR-oppslag alene (autorisasjonskategori), eller må skjemaet spørre eksplisitt?

### 2. Utvidet stønad

| Felt | Type |
|---|---|
| Kryss av hvis pasienten har krav på utvidet stønad | Boolsk (checkbox) |

### 3. Hvilken bittanomali henvises det for? — hovedklassifisering

Tre grupper, med ulik dekningsprosent. Skjemaet håndhever ikke eksplisitt at kun én gruppe kan velges — det er trolig underforstått av regelverket (hver bittanomali-kode hører til én gruppe), men **ikke bekreftet**.

**Gruppe a — 100 %** (3 alternativer, ingen underpunkter)

| Kode | Beskrivelse |
|---|---|
| 8a1 | Leppe-kjeve-ganespalte |
| 8a2 | Medfødt og ervervet kraniofacial lidelse |
| 8a3 | Bittavvik som er så alvorlig at pasienten må ha ortognatisk-kirurgisk behandling |

**Gruppe b — 75 % / 90 % ved utvidet stønad** (10 alternativer, ingen underpunkter)

| Kode | Beskrivelse |
|---|---|
| 1 | Horisontalt overbitt, 9 mm eller mer |
| 2 | Enkeltsidig kryss- eller sakse-bitt (≥3 tannpar) med tvangsføring og/eller asymmetrier |
| 3 | Åpent bitt hvor det bare er okklusjonskontakt på molarene |
| 4 | Retinerte fortenner, hjørnetenner og premolarer med behov for aktiv fremføring |
| 5 | Underbitt som omfatter alle fire incisiver, med eller uten tvangsføring |
| 6 | Agenesi eller tanntap i fronten (fortenner og hjørnetenner) |
| 7 | Dypt bitt med buccal/palatinal påbitning av slimhinnen (≥2 tenner) |
| 8 | Dobbeltsidig saksebitt (≥2 tannpar på hver side) |
| 9 | Agenesi av ≥2 tenner i samme sidesegment (3. molarer unntatt) |
| 10 | Agenesi av enkelttenner i sidesegmentene (ved lukkede luker) og/eller hypoplastisk molar |

**Gruppe c — 40 % / 60 % ved utvidet stønad** (5 hovedkoder, de fleste med underpunkter a/b/(c))

| Kode | Beskrivelse | Underpunkter |
|---|---|---|
| 11 | Horisontalt overbitt, 6–9 mm | a) funksjonelle avvik · b) psykisk/sosial mestring · c) kombinert med c12 |
| 12 | Stor plassmangel i fronten (≥4 mm) med kontaktbrudd (≥2 mm) | a) funksjonelle avvik · b) psykisk/sosial mestring · c) kombinert med c11 eller c13 |
| 13 | Inverteringer i fronten (fortenner og hjørnetenner) | a) funksjonelle avvik · b) psykisk/sosial mestring · c) kombinert med c12 |
| 14 | Diastema mediale ≥3 mm, eller markert generelt plassoverskudd (**angis i mm** — tallfelt) | a) funksjonelle avvik · b) psykisk/sosial mestring |
| 15 | Åpent bitt som omfatter ≥3 tannpar | a) funksjonelle avvik · b) psykisk/sosial mestring |

### 4. Fyll ut hvis henvisningen gjelder annen tilstand

Alternativ til §3 — egen boks, egne koder. Hver refererer til et paragrafpunkt i regelverket (ikke gjengitt her — trenger avklaring av selve forskriftsteksten hvis vi skal bygge veiledningstekst i appen).

| Kode | Beskrivelse | Regelverksreferanse |
|---|---|---|
| 1 | Kjeveortopedisk behandling ved marginal periodontitt | punkt 6 b |
| 2 | Preprotetisk kjeveortopedisk behandling — tannskade ved godkjent yrkesskade | punkt 12 |
| 3 | Preprotetisk kjeveortopedisk behandling ved tannagenesi | punkt 7 c |
| 4 | Preprotetisk kjeveortopedisk behandling — tannskade ved ulykke, ikke godkjent yrkesskade | punkt 13 |

### 5. Merknader fra henvisende tannlege/tannpleier

Fritekst.

---

## Utkast til datamodell

**Status: utkast, ikke koblet til noen app ennå** — se «Åpent spørsmål: appstruktur» nederst. Følger samme stil som `ForerLegeerklaeringModel.cs` (flate felt, `Xxx_Yyy`-navngiving per gruppe, `[XmlElement]`/`[JsonProperty]`/`[JsonPropertyName]` for Altinn-kompatibilitet).

```csharp
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Altinn.App.Models
{
    [XmlRoot(ElementName = "KjeveortopediskHenvisning")]
    public class KjeveortopediskHenvisningModel
    {
        // --- Pasient (fra FHIR Patient) ---
        [XmlElement("Pasient_Fnr", Order = 1)]
        [JsonProperty("Pasient_Fnr")] [JsonPropertyName("Pasient_Fnr")]
        public string Pasient_Fnr { get; set; }

        [XmlElement("Pasient_Fornavn", Order = 2)]
        [JsonProperty("Pasient_Fornavn")] [JsonPropertyName("Pasient_Fornavn")]
        public string Pasient_Fornavn { get; set; }

        [XmlElement("Pasient_Etternavn", Order = 3)]
        [JsonProperty("Pasient_Etternavn")] [JsonPropertyName("Pasient_Etternavn")]
        public string Pasient_Etternavn { get; set; }

        [XmlElement("Pasient_Adresse", Order = 4)]
        [JsonProperty("Pasient_Adresse")] [JsonPropertyName("Pasient_Adresse")]
        public string Pasient_Adresse { get; set; }

        // --- Henviser: tannlege/tannpleier (fra FHIR Practitioner via fhirUser) ---
        [XmlElement("Henviser_HPR", Order = 10)]
        [JsonProperty("Henviser_HPR")] [JsonPropertyName("Henviser_HPR")]
        public string Henviser_HPR { get; set; }

        [XmlElement("Henviser_Fornavn", Order = 11)]
        [JsonProperty("Henviser_Fornavn")] [JsonPropertyName("Henviser_Fornavn")]
        public string Henviser_Fornavn { get; set; }

        [XmlElement("Henviser_Etternavn", Order = 12)]
        [JsonProperty("Henviser_Etternavn")] [JsonPropertyName("Henviser_Etternavn")]
        public string Henviser_Etternavn { get; set; }

        // --- Virksomhet (fra FHIR Organization via Encounter.serviceProvider) ---
        [XmlElement("Virksomhet_Navn", Order = 20)]
        [JsonProperty("Virksomhet_Navn")] [JsonPropertyName("Virksomhet_Navn")]
        public string Virksomhet_Navn { get; set; }

        [XmlElement("Virksomhet_Orgnr", Order = 21)]
        [JsonProperty("Virksomhet_Orgnr")] [JsonPropertyName("Virksomhet_Orgnr")]
        public string Virksomhet_Orgnr { get; set; }

        [XmlElement("Virksomhet_HerId", Order = 22)]
        [JsonProperty("Virksomhet_HerId")] [JsonPropertyName("Virksomhet_HerId")]
        public string Virksomhet_HerId { get; set; }

        // --- §2: Utvidet stønad ---
        [XmlElement("Henvisning_UtvidetStonad", Order = 30)]
        [JsonProperty("Henvisning_UtvidetStonad")] [JsonPropertyName("Henvisning_UtvidetStonad")]
        public bool? Henvisning_UtvidetStonad { get; set; }

        // --- §3 Gruppe a (100%) ---
        [XmlElement("GruppeA_Kode8a1", Order = 40)]
        [JsonProperty("GruppeA_Kode8a1")] [JsonPropertyName("GruppeA_Kode8a1")]
        public bool? GruppeA_Kode8a1 { get; set; }

        [XmlElement("GruppeA_Kode8a2", Order = 41)]
        [JsonProperty("GruppeA_Kode8a2")] [JsonPropertyName("GruppeA_Kode8a2")]
        public bool? GruppeA_Kode8a2 { get; set; }

        [XmlElement("GruppeA_Kode8a3", Order = 42)]
        [JsonProperty("GruppeA_Kode8a3")] [JsonPropertyName("GruppeA_Kode8a3")]
        public bool? GruppeA_Kode8a3 { get; set; }

        // --- §3 Gruppe b (75% / 90%) — Kode1..10 ---
        [XmlElement("GruppeB_Kode1", Order = 50)]
        [JsonProperty("GruppeB_Kode1")] [JsonPropertyName("GruppeB_Kode1")]
        public bool? GruppeB_Kode1 { get; set; }

        [XmlElement("GruppeB_Kode2", Order = 51)]
        [JsonProperty("GruppeB_Kode2")] [JsonPropertyName("GruppeB_Kode2")]
        public bool? GruppeB_Kode2 { get; set; }

        [XmlElement("GruppeB_Kode3", Order = 52)]
        [JsonProperty("GruppeB_Kode3")] [JsonPropertyName("GruppeB_Kode3")]
        public bool? GruppeB_Kode3 { get; set; }

        [XmlElement("GruppeB_Kode4", Order = 53)]
        [JsonProperty("GruppeB_Kode4")] [JsonPropertyName("GruppeB_Kode4")]
        public bool? GruppeB_Kode4 { get; set; }

        [XmlElement("GruppeB_Kode5", Order = 54)]
        [JsonProperty("GruppeB_Kode5")] [JsonPropertyName("GruppeB_Kode5")]
        public bool? GruppeB_Kode5 { get; set; }

        [XmlElement("GruppeB_Kode6", Order = 55)]
        [JsonProperty("GruppeB_Kode6")] [JsonPropertyName("GruppeB_Kode6")]
        public bool? GruppeB_Kode6 { get; set; }

        [XmlElement("GruppeB_Kode7", Order = 56)]
        [JsonProperty("GruppeB_Kode7")] [JsonPropertyName("GruppeB_Kode7")]
        public bool? GruppeB_Kode7 { get; set; }

        [XmlElement("GruppeB_Kode8", Order = 57)]
        [JsonProperty("GruppeB_Kode8")] [JsonPropertyName("GruppeB_Kode8")]
        public bool? GruppeB_Kode8 { get; set; }

        [XmlElement("GruppeB_Kode9", Order = 58)]
        [JsonProperty("GruppeB_Kode9")] [JsonPropertyName("GruppeB_Kode9")]
        public bool? GruppeB_Kode9 { get; set; }

        [XmlElement("GruppeB_Kode10", Order = 59)]
        [JsonProperty("GruppeB_Kode10")] [JsonPropertyName("GruppeB_Kode10")]
        public bool? GruppeB_Kode10 { get; set; }

        // --- §3 Gruppe c (40% / 60%) — Kode11..15, med underpunkter ---
        [XmlElement("GruppeC_Kode11", Order = 70)]
        [JsonProperty("GruppeC_Kode11")] [JsonPropertyName("GruppeC_Kode11")]
        public bool? GruppeC_Kode11 { get; set; }
        [XmlElement("GruppeC_Kode11_A", Order = 71)]
        [JsonProperty("GruppeC_Kode11_A")] [JsonPropertyName("GruppeC_Kode11_A")]
        public bool? GruppeC_Kode11_A { get; set; }
        [XmlElement("GruppeC_Kode11_B", Order = 72)]
        [JsonProperty("GruppeC_Kode11_B")] [JsonPropertyName("GruppeC_Kode11_B")]
        public bool? GruppeC_Kode11_B { get; set; }
        [XmlElement("GruppeC_Kode11_C", Order = 73)]
        [JsonProperty("GruppeC_Kode11_C")] [JsonPropertyName("GruppeC_Kode11_C")]
        public bool? GruppeC_Kode11_C { get; set; }

        [XmlElement("GruppeC_Kode12", Order = 80)]
        [JsonProperty("GruppeC_Kode12")] [JsonPropertyName("GruppeC_Kode12")]
        public bool? GruppeC_Kode12 { get; set; }
        [XmlElement("GruppeC_Kode12_A", Order = 81)]
        [JsonProperty("GruppeC_Kode12_A")] [JsonPropertyName("GruppeC_Kode12_A")]
        public bool? GruppeC_Kode12_A { get; set; }
        [XmlElement("GruppeC_Kode12_B", Order = 82)]
        [JsonProperty("GruppeC_Kode12_B")] [JsonPropertyName("GruppeC_Kode12_B")]
        public bool? GruppeC_Kode12_B { get; set; }
        [XmlElement("GruppeC_Kode12_C", Order = 83)]
        [JsonProperty("GruppeC_Kode12_C")] [JsonPropertyName("GruppeC_Kode12_C")]
        public bool? GruppeC_Kode12_C { get; set; }

        [XmlElement("GruppeC_Kode13", Order = 90)]
        [JsonProperty("GruppeC_Kode13")] [JsonPropertyName("GruppeC_Kode13")]
        public bool? GruppeC_Kode13 { get; set; }
        [XmlElement("GruppeC_Kode13_A", Order = 91)]
        [JsonProperty("GruppeC_Kode13_A")] [JsonPropertyName("GruppeC_Kode13_A")]
        public bool? GruppeC_Kode13_A { get; set; }
        [XmlElement("GruppeC_Kode13_B", Order = 92)]
        [JsonProperty("GruppeC_Kode13_B")] [JsonPropertyName("GruppeC_Kode13_B")]
        public bool? GruppeC_Kode13_B { get; set; }
        [XmlElement("GruppeC_Kode13_C", Order = 93)]
        [JsonProperty("GruppeC_Kode13_C")] [JsonPropertyName("GruppeC_Kode13_C")]
        public bool? GruppeC_Kode13_C { get; set; }

        [XmlElement("GruppeC_Kode14", Order = 100)]
        [JsonProperty("GruppeC_Kode14")] [JsonPropertyName("GruppeC_Kode14")]
        public bool? GruppeC_Kode14 { get; set; }
        [XmlElement("GruppeC_Kode14_PlassoverskuddMm", Order = 101)]
        [JsonProperty("GruppeC_Kode14_PlassoverskuddMm")] [JsonPropertyName("GruppeC_Kode14_PlassoverskuddMm")]
        public decimal? GruppeC_Kode14_PlassoverskuddMm { get; set; }
        [XmlElement("GruppeC_Kode14_A", Order = 102)]
        [JsonProperty("GruppeC_Kode14_A")] [JsonPropertyName("GruppeC_Kode14_A")]
        public bool? GruppeC_Kode14_A { get; set; }
        [XmlElement("GruppeC_Kode14_B", Order = 103)]
        [JsonProperty("GruppeC_Kode14_B")] [JsonPropertyName("GruppeC_Kode14_B")]
        public bool? GruppeC_Kode14_B { get; set; }

        [XmlElement("GruppeC_Kode15", Order = 110)]
        [JsonProperty("GruppeC_Kode15")] [JsonPropertyName("GruppeC_Kode15")]
        public bool? GruppeC_Kode15 { get; set; }
        [XmlElement("GruppeC_Kode15_A", Order = 111)]
        [JsonProperty("GruppeC_Kode15_A")] [JsonPropertyName("GruppeC_Kode15_A")]
        public bool? GruppeC_Kode15_A { get; set; }
        [XmlElement("GruppeC_Kode15_B", Order = 112)]
        [JsonProperty("GruppeC_Kode15_B")] [JsonPropertyName("GruppeC_Kode15_B")]
        public bool? GruppeC_Kode15_B { get; set; }

        // --- §4: Annen tilstand (alternativ til §3) ---
        [XmlElement("AnnenTilstand_Kode1", Order = 120)]
        [JsonProperty("AnnenTilstand_Kode1")] [JsonPropertyName("AnnenTilstand_Kode1")]
        public bool? AnnenTilstand_Kode1 { get; set; }
        [XmlElement("AnnenTilstand_Kode2", Order = 121)]
        [JsonProperty("AnnenTilstand_Kode2")] [JsonPropertyName("AnnenTilstand_Kode2")]
        public bool? AnnenTilstand_Kode2 { get; set; }
        [XmlElement("AnnenTilstand_Kode3", Order = 122)]
        [JsonProperty("AnnenTilstand_Kode3")] [JsonPropertyName("AnnenTilstand_Kode3")]
        public bool? AnnenTilstand_Kode3 { get; set; }
        [XmlElement("AnnenTilstand_Kode4", Order = 123)]
        [JsonProperty("AnnenTilstand_Kode4")] [JsonPropertyName("AnnenTilstand_Kode4")]
        public bool? AnnenTilstand_Kode4 { get; set; }

        // --- §5: Merknader ---
        [XmlElement("Henvisning_Merknad", Order = 130)]
        [JsonProperty("Henvisning_Merknad")] [JsonPropertyName("Henvisning_Merknad")]
        public string Henvisning_Merknad { get; set; }
    }
}
```

**Bevisst utelatt fra dette utkastet:**
- Egen boolsk «egenerklæring bekreftet»-flagg for erklæringsteksten under 1b — bør trolig legges til som et obligatorisk felt i selve Altinn-skjemaet, ikke bare i datamodellen.
- Enum/kodeverk-representasjon i stedet for individuelle boolske felt per kode — flate booleans matcher stilen i `ForerLegeerklaeringModel`, men en `List<string>`/kodeverk-tabell kunne vært mer kompakt. Ikke byttet ut uten videre avklaring, siden det påvirker layout-bindingen i Altinn Studio.
- Avledet «hvilken gruppe/dekningsprosent gjelder» — tilsvarende `ForerKonklusjonModel`-mønsteret fra førerrett-caset (se [BESLUTNINGER.md C-3](BESLUTNINGER.md)). Naturlig å legge til når selve forretningsregelen (kan flere grupper kombineres, eller er de gjensidig utelukkende?) er bekreftet.

---

## Åpent spørsmål: appstruktur

Dette blir det **andre** skjemaet i prosjektet. `src/App` er i dag én enkelt Altinn Studio-app (`forer-legeerklaering`) — det finnes ikke noe presedens i repoet for flere apper side om side. Før jeg går videre til selve Altinn Studio-skjemaet (layout, `applicationmetadata.json`, prosessdefinisjon), trengs et valg:

1. **Ny app-mappe i samme repo** (f.eks. `src/AppKjeveortopedisk`), egen `App.csproj`/datamodell/layout, delt `App.sln`.
2. **Eget repo** for dette caset.
3. Noe annet du har i tankene.

Altinn Studio-skjema (layout.json, layout-sets, applicationmetadata.json m.m.) er normalt noe Altinn Studio sitt eget designer-verktøy/CLI genererer — jeg vil helst vite hvor det skal bo før jeg begynner å håndskrive den strukturen, for å unngå å måtte flytte alt i etterkant.

## Åpent spørsmål: FHIR-tilgjengelighet fra tannlege-EPJ

Johann avklarer hvilke FHIR-ressurser et tannlege-EPJ faktisk eksponerer. Foreløpig antatt (basert på samme mønster som førerrett-caset): `Patient`, `Practitioner` (via `fhirUser`), `Organization` (via `Encounter.serviceProvider` eller tilsvarende). Selve den kliniske klassifiseringen i §3/§4 er høyst sannsynlig **ikke** noe et generisk tannlege-EPJ har strukturert FHIR-data for (dette er en Helfo-spesifikk vurdering, ikke en diagnosekode) — den delen av skjemaet må trolig alltid fylles ut manuelt av tannlegen/tannpleieren, uavhengig av EPJ-tilkobling.
