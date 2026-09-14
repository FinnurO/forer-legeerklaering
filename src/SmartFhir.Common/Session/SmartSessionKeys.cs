namespace SmartFhir.Common.Session
{
    /// <summary>
    /// Nøkler brukt for å lagre SMART on FHIR-launch-tilstand i server-side session
    /// (og IMemoryCache som fallback). Delt mellom SmartLaunchControllerBase (skriver dem
    /// under launch/callback) og hver apps FhirPrefillService (leser dem ved prefill).
    /// </summary>
    public static class SmartSessionKeys
    {
        public const string TokenSessionKey = "smart_token";
        public const string FhirContextSessionKey = "smart_fhir_context";
        public const string CacheKeyPrefix = "smart_fhir_";

        // Kun brukt internt i SmartLaunchControllerBase mellom Launch() og Callback() — ikke
        // lest av noen FhirPrefillService, men samlet her for å holde alle session-nøkler ett sted.
        public const string StateSessionKey = "smart_state";
        public const string PkceSessionKey = "smart_pkce_verifier";
        public const string IssSessionKey = "smart_iss";
    }
}
