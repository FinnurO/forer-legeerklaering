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

        // Seedet i local-dev/seed.ps1 (2026-09-09) — Tina Tannlege/Kaja Kviss/Sentrum Tannklinikk,
        // en egen navngitt tannlege-kontekst i stedet for den opprinnelige placeholderen
        // ("tannlege-test") som aldri fantes i HAPI FHIR-mocken. Overstyr med
        // ?patientId=...&encounterId=... på /smart/dev-login for å teste med andre
        // pasienter/behandlere seedet i samme skript.
        protected override string DefaultTestPatientId => "kaja-kviss";
        protected override string DefaultTestEncounterId => "enc-kaja-001";
        protected override string DefaultTestPractitionerPath => "Practitioner/tannlege-tina";

        // Matcher Tina Tannlege sin oppføring i wwwroot/testData.json — dette caset skal i
        // utgangspunktet kunne fylles ut helt uten SMART-kontekst (dobbel inngangsmodus), så det
        // naturlige "hvem logger inn"-standardvalget er henvisende tannlege selv, ikke den
        // generiske Ola Nordmann-testbrukeren fra ForerLegeerklaering.
        protected override int DefaultUserId => 22345;
        protected override int DefaultPartyId => 612345;
    }
}
