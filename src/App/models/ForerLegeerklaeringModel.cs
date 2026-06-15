using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Altinn.App.Models
{
    [XmlRoot(ElementName = "ForerLegeerklaering")]
    public class ForerLegeerklaeringModel
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

        [XmlElement("Pasient_Fodselsdato", Order = 4)]
        [JsonProperty("Pasient_Fodselsdato")]
        [JsonPropertyName("Pasient_Fodselsdato")]
        public string Pasient_Fodselsdato { get; set; }

        [XmlElement("Pasient_Kjonn", Order = 5)]
        [JsonProperty("Pasient_Kjonn")]
        [JsonPropertyName("Pasient_Kjonn")]
        public string Pasient_Kjonn { get; set; }

        // --- Lege (fra FHIR Practitioner via fhirUser) ---
        [XmlElement("Lege_HPR", Order = 10)]
        [JsonProperty("Lege_HPR")]
        [JsonPropertyName("Lege_HPR")]
        public string Lege_HPR { get; set; }

        [XmlElement("Lege_Fornavn", Order = 11)]
        [JsonProperty("Lege_Fornavn")]
        [JsonPropertyName("Lege_Fornavn")]
        public string Lege_Fornavn { get; set; }

        [XmlElement("Lege_Etternavn", Order = 12)]
        [JsonProperty("Lege_Etternavn")]
        [JsonPropertyName("Lege_Etternavn")]
        public string Lege_Etternavn { get; set; }

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

        // --- Konsultasjon (fra FHIR Encounter) ---
        [XmlElement("Konsultasjon_Dato", Order = 30)]
        [JsonProperty("Konsultasjon_Dato")]
        [JsonPropertyName("Konsultasjon_Dato")]
        public string Konsultasjon_Dato { get; set; }

        // --- Diagnose (fra FHIR Condition) ---
        [XmlElement("Diagnose_Kode", Order = 40)]
        [JsonProperty("Diagnose_Kode")]
        [JsonPropertyName("Diagnose_Kode")]
        public string Diagnose_Kode { get; set; }

        [XmlElement("Diagnose_Tekst", Order = 41)]
        [JsonProperty("Diagnose_Tekst")]
        [JsonPropertyName("Diagnose_Tekst")]
        public string Diagnose_Tekst { get; set; }

        // --- Legeerklæring førerrett (fylles ut av legen) ---
        [XmlElement("Forer_Kjoretoygruppe", Order = 50)]
        [JsonProperty("Forer_Kjoretoygruppe")]
        [JsonPropertyName("Forer_Kjoretoygruppe")]
        public string Forer_Kjoretoygruppe { get; set; }

        [XmlElement("Forer_ErSkikket", Order = 51)]
        [JsonProperty("Forer_ErSkikket")]
        [JsonPropertyName("Forer_ErSkikket")]
        public bool? Forer_ErSkikket { get; set; }

        [XmlElement("Forer_Merknad", Order = 52)]
        [JsonProperty("Forer_Merknad")]
        [JsonPropertyName("Forer_Merknad")]
        public string Forer_Merknad { get; set; }

        [XmlElement("Forer_Vilkar", Order = 53)]
        [JsonProperty("Forer_Vilkar")]
        [JsonPropertyName("Forer_Vilkar")]
        public string Forer_Vilkar { get; set; }
    }
}
