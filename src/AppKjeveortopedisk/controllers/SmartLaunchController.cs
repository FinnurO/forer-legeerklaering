using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartFhir.Common;

namespace Altinn.App.Controllers
{
    /// <summary>
    /// SMART on FHIR launch/callback for AppKjeveortopedisk. All faktisk OAuth2/PKCE/session-
    /// mekanikk ligger i SmartLaunchControllerBase (src/SmartFhir.Common) — se
    /// ForerLegeerklaering sin tilsvarende SmartLaunchController for det samme mønsteret.
    ///
    /// Testdataene (patient/encounter/practitioner-id) under er PLASSHOLDERE — det finnes ennå
    /// ikke noe reelt tannlege-EPJ-testmiljø koblet til dette caset. Oppdater disse så snart det
    /// er avklart hvilke FHIR-ressurser et faktisk tannlege-EPJ eksponerer (se
    /// docs/SKJEMA-KJEVEORTOPEDISK.md, «Åpent spørsmål: FHIR-tilgjengelighet fra tannlege-EPJ»).
    ///
    /// Scopene er holdt til akkurat det SmartFhirPrefillClient henter (standard-scopene i
    /// basisklassen) — ingen Condition.read/DocumentReference-scopes, siden §3/§4-klassifiseringen
    /// uansett ikke kommer fra FHIR (se samme dokument).
    /// </summary>
    public class SmartLaunchController : SmartLaunchControllerBase
    {
        public SmartLaunchController(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            IMemoryCache memoryCache,
            ILogger<SmartLaunchController> logger,
            IWebHostEnvironment env
        )
            : base(httpClientFactory, config, memoryCache, logger, env) { }

        protected override string DefaultClientId => "kjeveortopedisk-henvisning-poc";
        protected override string DefaultTestPatientId => "test-pasient-1";
        protected override string DefaultTestEncounterId => "enc-test-001";
        protected override string DefaultTestPractitionerPath => "Practitioner/tannlege-test";
    }
}
