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

            // Ryddet 2026-09-09: droppet Observation.read (aldri valgt som Incoming API ved
            // appregistrering, se docs/EPIC-TESTMILJO.md §9 2026-08-27) og v2-stil
            // DocumentReference.c (appen er registrert med "SMART Scope Version: v1", som ikke
            // har noe eget ".c"/create-scope). Denne opprydningen var IKKE rotårsaken til
            // "OAuth2 Error: Something went wrong trying to authorize the client" på selve
            // /oauth2/authorize — feilen vedvarte identisk med denne snevrere scope-listen også
            // (se §9 2026-09-09). Beholdt likevel siden den nå matcher registreringen nøyaktig.
            yield return "patient/DocumentReference.write";
        }
    }
}
