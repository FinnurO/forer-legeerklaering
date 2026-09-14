using System.Text.Json.Serialization;

namespace SmartFhir.Common.Session
{
    /// <summary>
    /// Minimal deserialisering av OAuth2-tokenet slik det ligger lagret i session — kun
    /// access_token trengs her (FhirPrefillService bruker den som Bearer-token mot EPJ-en).
    ///
    /// BUG (2026-08-11, se SmartLaunchControllerBase.TokenResponse): tokenets serialisering til
    /// session er snake_case ("access_token"), ikke PascalCase — uten [JsonPropertyName] her
    /// deserialiseres AccessToken til null, og Bearer-headeren ender opp uten faktisk verdi,
    /// som gir 401 på alle FHIR-kall.
    /// </summary>
    public class TokenData
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
}
