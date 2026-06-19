using System.Text.Json.Serialization;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Altinn.App.Models
{
    [XmlRoot(ElementName = "ForerKonklusjon")]
    public class ForerKonklusjonModel
    {
        [XmlElement("Pasient_Fnr", Order = 1)]
        [JsonProperty("Pasient_Fnr")]
        [JsonPropertyName("Pasient_Fnr")]
        public string Pasient_Fnr { get; set; }

        [XmlElement("Lege_HPR", Order = 2)]
        [JsonProperty("Lege_HPR")]
        [JsonPropertyName("Lege_HPR")]
        public string Lege_HPR { get; set; }

        // Konklusjon per gruppe: "skikket" | "ikke_skikket" | "begrenset" | ""
        [XmlElement("Gruppe1_Resultat", Order = 10)]
        [JsonProperty("Gruppe1_Resultat")]
        [JsonPropertyName("Gruppe1_Resultat")]
        public string Gruppe1_Resultat { get; set; }

        [XmlElement("Gruppe1_BegrensetAntallAar", Order = 11)]
        [JsonProperty("Gruppe1_BegrensetAntallAar")]
        [JsonPropertyName("Gruppe1_BegrensetAntallAar")]
        public int? Gruppe1_BegrensetAntallAar { get; set; }

        [XmlElement("Gruppe2_Resultat", Order = 20)]
        [JsonProperty("Gruppe2_Resultat")]
        [JsonPropertyName("Gruppe2_Resultat")]
        public string Gruppe2_Resultat { get; set; }

        [XmlElement("Gruppe2_BegrensetAntallAar", Order = 21)]
        [JsonProperty("Gruppe2_BegrensetAntallAar")]
        [JsonPropertyName("Gruppe2_BegrensetAntallAar")]
        public int? Gruppe2_BegrensetAntallAar { get; set; }

        [XmlElement("Gruppe3_Resultat", Order = 30)]
        [JsonProperty("Gruppe3_Resultat")]
        [JsonPropertyName("Gruppe3_Resultat")]
        public string Gruppe3_Resultat { get; set; }

        [XmlElement("Gruppe3_BegrensetAntallAar", Order = 31)]
        [JsonProperty("Gruppe3_BegrensetAntallAar")]
        [JsonPropertyName("Gruppe3_BegrensetAntallAar")]
        public int? Gruppe3_BegrensetAntallAar { get; set; }

        [XmlElement("Vilkar", Order = 40)]
        [JsonProperty("Vilkar")]
        [JsonPropertyName("Vilkar")]
        public string Vilkar { get; set; }

        [XmlElement("Merknad", Order = 41)]
        [JsonProperty("Merknad")]
        [JsonPropertyName("Merknad")]
        public string Merknad { get; set; }
    }
}
