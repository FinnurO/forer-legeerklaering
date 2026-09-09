using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartFhir.Common;

namespace Altinn.App.Controllers
{
    /// <summary>
    /// SMART on FHIR launch/callback for ForerLegeerklaering. All faktisk OAuth2/PKCE/session-
    /// mekanikk ligger i SmartLaunchControllerBase (src/SmartFhir.Common) — denne klassen setter
    /// kun de få tingene som er spesifikke for dette caset: fallback client-id og hvilken
    /// test-pasient/-konsultasjon/-lege som brukes av test-prefill/dev-login, samt at vi her ber
    /// om Condition.read og skrive-scopes for DocumentReference (VEIKART.md fase 2-utforskning av
    /// writeback), som ikke er del av standard-scopene i basisklassen.
    ///
    /// Flyttet ut av den opprinnelige (monolittiske) SmartLaunchController 2026-09-09 — se
    /// SmartLaunchControllerBase for hele historikken/kommentarene rundt bugs funnet mot
    /// launch.smarthealthit.org (de er ikke duplisert her).
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

        protected override string DefaultClientId => "forer-legeerklaering-poc";
        protected override string DefaultTestPatientId => "sophie-salt";
        protected override string DefaultTestEncounterId => "enc-sophie-001";
        protected override string DefaultTestPractitionerPath => "Practitioner/lege-ola";

        protected override IEnumerable<string> GetScopes()
        {
            foreach (var scope in base.GetScopes())
                yield return scope;

            yield return "patient/Condition.read";
            yield return "patient/Observation.read";

            // VEIKART.md fase 2 (writeback til EPJ) — v1-stil (.write) og v2-stil (.c, create)
            // sendt begge, se hvilken(e) som faktisk innvilges i token-responsens "scope"-felt
            // (jf. "Sølv: scope-detektivarbeid" i HACKATHON-EHIN-2026.md).
            yield return "patient/DocumentReference.write";
            yield return "patient/DocumentReference.c";
        }
    }
}
