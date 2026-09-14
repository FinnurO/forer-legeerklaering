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
        [JsonProperty("Pasient_Fnr")]
        [JsonPropertyName("Pasient_Fnr")]
        public string Pasient_Fnr { get; set; }

        [XmlElement("Pasient_Fornavn", Order = 2)]
        [JsonProperty("Pasient_Fornavn")]
        [JsonPropertyName("Pasient_Fornavn")]
        public string Pasient_Fornavn { get; set; }

        [XmlElement("Pasient_Etternavn", Order = 3)]
        [JsonProperty("Pasient_Etternavn")]
        [JsonPropertyName("Pasient_Etternavn")]
        public string Pasient_Etternavn { get; set; }

        [XmlElement("Pasient_Adresse", Order = 4)]
        [JsonProperty("Pasient_Adresse")]
        [JsonPropertyName("Pasient_Adresse")]
        public string Pasient_Adresse { get; set; }

        // --- Henviser: tannlege/tannpleier (fra FHIR Practitioner via fhirUser) ---
        [XmlElement("Henviser_HPR", Order = 10)]
        [JsonProperty("Henviser_HPR")]
        [JsonPropertyName("Henviser_HPR")]
        public string Henviser_HPR { get; set; }

        [XmlElement("Henviser_Fornavn", Order = 11)]
        [JsonProperty("Henviser_Fornavn")]
        [JsonPropertyName("Henviser_Fornavn")]
        public string Henviser_Fornavn { get; set; }

        [XmlElement("Henviser_Etternavn", Order = 12)]
        [JsonProperty("Henviser_Etternavn")]
        [JsonPropertyName("Henviser_Etternavn")]
        public string Henviser_Etternavn { get; set; }

        // --- Virksomhet (fra FHIR Organization via Encounter.serviceProvider) ---
        [XmlElement("Virksomhet_Navn", Order = 20)]
        [JsonProperty("Virksomhet_Navn")]
        [JsonPropertyName("Virksomhet_Navn")]
        public string Virksomhet_Navn { get; set; }

        [XmlElement("Virksomhet_Orgnr", Order = 21)]
        [JsonProperty("Virksomhet_Orgnr")]
        [JsonPropertyName("Virksomhet_Orgnr")]
        public string Virksomhet_Orgnr { get; set; }

        [XmlElement("Virksomhet_HerId", Order = 22)]
        [JsonProperty("Virksomhet_HerId")]
        [JsonPropertyName("Virksomhet_HerId")]
        public string Virksomhet_HerId { get; set; }

        // --- §2: Utvidet stønad ---
        [XmlElement("Henvisning_UtvidetStonad", Order = 30)]
        [JsonProperty("Henvisning_UtvidetStonad")]
        [JsonPropertyName("Henvisning_UtvidetStonad")]
        public bool? Henvisning_UtvidetStonad { get; set; }

        // --- §3 Gruppe a (100%) ---
        [XmlElement("GruppeA_Kode8a1", Order = 40)]
        [JsonProperty("GruppeA_Kode8a1")]
        [JsonPropertyName("GruppeA_Kode8a1")]
        public bool? GruppeA_Kode8a1 { get; set; }

        [XmlElement("GruppeA_Kode8a2", Order = 41)]
        [JsonProperty("GruppeA_Kode8a2")]
        [JsonPropertyName("GruppeA_Kode8a2")]
        public bool? GruppeA_Kode8a2 { get; set; }

        [XmlElement("GruppeA_Kode8a3", Order = 42)]
        [JsonProperty("GruppeA_Kode8a3")]
        [JsonPropertyName("GruppeA_Kode8a3")]
        public bool? GruppeA_Kode8a3 { get; set; }

        // --- §3 Gruppe b (75% / 90%) — Kode1..10 ---
        [XmlElement("GruppeB_Kode1", Order = 50)]
        [JsonProperty("GruppeB_Kode1")]
        [JsonPropertyName("GruppeB_Kode1")]
        public bool? GruppeB_Kode1 { get; set; }

        [XmlElement("GruppeB_Kode2", Order = 51)]
        [JsonProperty("GruppeB_Kode2")]
        [JsonPropertyName("GruppeB_Kode2")]
        public bool? GruppeB_Kode2 { get; set; }

        [XmlElement("GruppeB_Kode3", Order = 52)]
        [JsonProperty("GruppeB_Kode3")]
        [JsonPropertyName("GruppeB_Kode3")]
        public bool? GruppeB_Kode3 { get; set; }

        [XmlElement("GruppeB_Kode4", Order = 53)]
        [JsonProperty("GruppeB_Kode4")]
        [JsonPropertyName("GruppeB_Kode4")]
        public bool? GruppeB_Kode4 { get; set; }

        [XmlElement("GruppeB_Kode5", Order = 54)]
        [JsonProperty("GruppeB_Kode5")]
        [JsonPropertyName("GruppeB_Kode5")]
        public bool? GruppeB_Kode5 { get; set; }

        [XmlElement("GruppeB_Kode6", Order = 55)]
        [JsonProperty("GruppeB_Kode6")]
        [JsonPropertyName("GruppeB_Kode6")]
        public bool? GruppeB_Kode6 { get; set; }

        [XmlElement("GruppeB_Kode7", Order = 56)]
        [JsonProperty("GruppeB_Kode7")]
        [JsonPropertyName("GruppeB_Kode7")]
        public bool? GruppeB_Kode7 { get; set; }

        [XmlElement("GruppeB_Kode8", Order = 57)]
        [JsonProperty("GruppeB_Kode8")]
        [JsonPropertyName("GruppeB_Kode8")]
        public bool? GruppeB_Kode8 { get; set; }

        [XmlElement("GruppeB_Kode9", Order = 58)]
        [JsonProperty("GruppeB_Kode9")]
        [JsonPropertyName("GruppeB_Kode9")]
        public bool? GruppeB_Kode9 { get; set; }

        [XmlElement("GruppeB_Kode10", Order = 59)]
        [JsonProperty("GruppeB_Kode10")]
        [JsonPropertyName("GruppeB_Kode10")]
        public bool? GruppeB_Kode10 { get; set; }

        // --- §3 Gruppe c (40% / 60%) — Kode11..15, med underpunkter ---
        [XmlElement("GruppeC_Kode11", Order = 70)]
        [JsonProperty("GruppeC_Kode11")]
        [JsonPropertyName("GruppeC_Kode11")]
        public bool? GruppeC_Kode11 { get; set; }

        [XmlElement("GruppeC_Kode11_A", Order = 71)]
        [JsonProperty("GruppeC_Kode11_A")]
        [JsonPropertyName("GruppeC_Kode11_A")]
        public bool? GruppeC_Kode11_A { get; set; }

        [XmlElement("GruppeC_Kode11_B", Order = 72)]
        [JsonProperty("GruppeC_Kode11_B")]
        [JsonPropertyName("GruppeC_Kode11_B")]
        public bool? GruppeC_Kode11_B { get; set; }

        [XmlElement("GruppeC_Kode11_C", Order = 73)]
        [JsonProperty("GruppeC_Kode11_C")]
        [JsonPropertyName("GruppeC_Kode11_C")]
        public bool? GruppeC_Kode11_C { get; set; }

        [XmlElement("GruppeC_Kode12", Order = 80)]
        [JsonProperty("GruppeC_Kode12")]
        [JsonPropertyName("GruppeC_Kode12")]
        public bool? GruppeC_Kode12 { get; set; }

        [XmlElement("GruppeC_Kode12_A", Order = 81)]
        [JsonProperty("GruppeC_Kode12_A")]
        [JsonPropertyName("GruppeC_Kode12_A")]
        public bool? GruppeC_Kode12_A { get; set; }

        [XmlElement("GruppeC_Kode12_B", Order = 82)]
        [JsonProperty("GruppeC_Kode12_B")]
        [JsonPropertyName("GruppeC_Kode12_B")]
        public bool? GruppeC_Kode12_B { get; set; }

        [XmlElement("GruppeC_Kode12_C", Order = 83)]
        [JsonProperty("GruppeC_Kode12_C")]
        [JsonPropertyName("GruppeC_Kode12_C")]
        public bool? GruppeC_Kode12_C { get; set; }

        [XmlElement("GruppeC_Kode13", Order = 90)]
        [JsonProperty("GruppeC_Kode13")]
        [JsonPropertyName("GruppeC_Kode13")]
        public bool? GruppeC_Kode13 { get; set; }

        [XmlElement("GruppeC_Kode13_A", Order = 91)]
        [JsonProperty("GruppeC_Kode13_A")]
        [JsonPropertyName("GruppeC_Kode13_A")]
        public bool? GruppeC_Kode13_A { get; set; }

        [XmlElement("GruppeC_Kode13_B", Order = 92)]
        [JsonProperty("GruppeC_Kode13_B")]
        [JsonPropertyName("GruppeC_Kode13_B")]
        public bool? GruppeC_Kode13_B { get; set; }

        [XmlElement("GruppeC_Kode13_C", Order = 93)]
        [JsonProperty("GruppeC_Kode13_C")]
        [JsonPropertyName("GruppeC_Kode13_C")]
        public bool? GruppeC_Kode13_C { get; set; }

        [XmlElement("GruppeC_Kode14", Order = 100)]
        [JsonProperty("GruppeC_Kode14")]
        [JsonPropertyName("GruppeC_Kode14")]
        public bool? GruppeC_Kode14 { get; set; }

        [XmlElement("GruppeC_Kode14_PlassoverskuddMm", Order = 101)]
        [JsonProperty("GruppeC_Kode14_PlassoverskuddMm")]
        [JsonPropertyName("GruppeC_Kode14_PlassoverskuddMm")]
        public decimal? GruppeC_Kode14_PlassoverskuddMm { get; set; }

        [XmlElement("GruppeC_Kode14_A", Order = 102)]
        [JsonProperty("GruppeC_Kode14_A")]
        [JsonPropertyName("GruppeC_Kode14_A")]
        public bool? GruppeC_Kode14_A { get; set; }

        [XmlElement("GruppeC_Kode14_B", Order = 103)]
        [JsonProperty("GruppeC_Kode14_B")]
        [JsonPropertyName("GruppeC_Kode14_B")]
        public bool? GruppeC_Kode14_B { get; set; }

        [XmlElement("GruppeC_Kode15", Order = 110)]
        [JsonProperty("GruppeC_Kode15")]
        [JsonPropertyName("GruppeC_Kode15")]
        public bool? GruppeC_Kode15 { get; set; }

        [XmlElement("GruppeC_Kode15_A", Order = 111)]
        [JsonProperty("GruppeC_Kode15_A")]
        [JsonPropertyName("GruppeC_Kode15_A")]
        public bool? GruppeC_Kode15_A { get; set; }

        [XmlElement("GruppeC_Kode15_B", Order = 112)]
        [JsonProperty("GruppeC_Kode15_B")]
        [JsonPropertyName("GruppeC_Kode15_B")]
        public bool? GruppeC_Kode15_B { get; set; }

        // --- §4: Annen tilstand (alternativ til §3) ---
        [XmlElement("AnnenTilstand_Kode1", Order = 120)]
        [JsonProperty("AnnenTilstand_Kode1")]
        [JsonPropertyName("AnnenTilstand_Kode1")]
        public bool? AnnenTilstand_Kode1 { get; set; }

        [XmlElement("AnnenTilstand_Kode2", Order = 121)]
        [JsonProperty("AnnenTilstand_Kode2")]
        [JsonPropertyName("AnnenTilstand_Kode2")]
        public bool? AnnenTilstand_Kode2 { get; set; }

        [XmlElement("AnnenTilstand_Kode3", Order = 122)]
        [JsonProperty("AnnenTilstand_Kode3")]
        [JsonPropertyName("AnnenTilstand_Kode3")]
        public bool? AnnenTilstand_Kode3 { get; set; }

        [XmlElement("AnnenTilstand_Kode4", Order = 123)]
        [JsonProperty("AnnenTilstand_Kode4")]
        [JsonPropertyName("AnnenTilstand_Kode4")]
        public bool? AnnenTilstand_Kode4 { get; set; }

        // --- §5: Merknader ---
        [XmlElement("Henvisning_Merknad", Order = 130)]
        [JsonProperty("Henvisning_Merknad")]
        [JsonPropertyName("Henvisning_Merknad")]
        public string Henvisning_Merknad { get; set; }
    }
}
