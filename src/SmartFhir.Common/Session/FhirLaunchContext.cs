namespace SmartFhir.Common.Session
{
    /// <summary>
    /// FHIR-launchkontekst utledet fra token-responsen (SMART App Launch IG v2.2.0) — hvilken
    /// pasient/konsultasjon/bruker/FHIR-server denne instansen av skjemaet skal hente data fra.
    /// Lagres i session under SmartSessionKeys.FhirContextSessionKey.
    /// </summary>
    public class FhirLaunchContext
    {
        public string PatientId { get; set; }
        public string EncounterId { get; set; }
        public string FhirUser { get; set; }
        public string FhirBaseUrl { get; set; }
    }
}
