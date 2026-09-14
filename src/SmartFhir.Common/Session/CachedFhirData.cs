namespace SmartFhir.Common.Session
{
    /// <summary>
    /// Fallback-lagring av token+kontekst i IMemoryCache, keyet på session-ID — brukes når
    /// session-cookien av en eller annen grunn ikke følger med tilbake til appen (se
    /// FhirPrefillService/SmartLaunchControllerBase for begrunnelse).
    /// </summary>
    public class CachedFhirData
    {
        public string TokenJson { get; set; }
        public string ContextJson { get; set; }
    }
}
