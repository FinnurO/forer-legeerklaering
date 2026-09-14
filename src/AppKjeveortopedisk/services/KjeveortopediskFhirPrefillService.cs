using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Altinn.App.Core.Features;
using Altinn.App.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartFhir.Common;

namespace Altinn.App.Services
{
    /// <summary>
    /// Pre-fills KjeveortopediskHenvisningModel with the standard FHIR fields (Patient,
    /// Practitioner, Organization) fetched from the EPJ using the SMART access token stored in
    /// server session — samme mønster som ForerLegeerklaering sin FhirPrefillService, men uten
    /// noen Condition-/konklusjon-avledning, siden dette skjemaet ikke har noe tilsvarende.
    ///
    /// §3/§4 (bittanomali-klassifiseringen) er BEVISST IKKE fylt ut herfra — det er en
    /// Helfo-spesifikk vurdering tannlegen/tannpleieren selv gjør, ikke noe som finnes som
    /// strukturert FHIR-data i noe EPJ (se docs/SKJEMA-KJEVEORTOPEDISK.md). Skjemaet må derfor
    /// alltid kunne fylles ut helt manuelt for disse feltene, uavhengig av EPJ-tilkobling — jf.
    /// «dobbel inngangsmodus»-kravet som gjelder for hele prosjektet.
    /// </summary>
    public class KjeveortopediskFhirPrefillService : IDataProcessor
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<KjeveortopediskFhirPrefillService> _logger;
        private readonly SmartFhirPrefillClient _fhirClient;

        public KjeveortopediskFhirPrefillService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache memoryCache,
            ILogger<KjeveortopediskFhirPrefillService> logger
        )
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _memoryCache = memoryCache;
            _logger = logger;
            _fhirClient = new SmartFhirPrefillClient(logger);
        }

        public async Task ProcessDataRead(Instance instance, Guid? dataId, object data, string? language = null)
        {
            _logger.LogInformation(
                "KjeveortopediskFhirPrefillService.ProcessDataRead called for instance {InstanceId}",
                instance?.Id
            );

            if (data is not KjeveortopediskHenvisningModel model)
            {
                _logger.LogInformation("Data is not KjeveortopediskHenvisningModel — skipping");
                return;
            }

            var session = _httpContextAccessor.HttpContext?.Session;
            var read = await SmartSessionReader.TryReadAsync(session, _memoryCache, _logger);
            if (read == null)
            {
                _logger.LogInformation("No SMART context found in session or cache — skipping FHIR pre-fill");
                return;
            }

            _logger.LogInformation(
                "Starting FHIR pre-fill: patient={Patient}, encounter={Encounter}",
                read.Context?.PatientId,
                read.Context?.EncounterId
            );

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                read.Token.AccessToken
            );

            var fhirData = await _fhirClient.FetchAsync(client, read.Context);

            model.Pasient_Fnr = fhirData.Pasient_Fnr;
            model.Pasient_Fornavn = fhirData.Pasient_Fornavn;
            model.Pasient_Etternavn = fhirData.Pasient_Etternavn;
            model.Pasient_Adresse = fhirData.Pasient_Adresse;

            model.Henviser_HPR = fhirData.Henviser_Hpr;
            model.Henviser_Fornavn = fhirData.Henviser_Fornavn;
            model.Henviser_Etternavn = fhirData.Henviser_Etternavn;

            model.Virksomhet_Navn = fhirData.Virksomhet_Navn;
            model.Virksomhet_Orgnr = fhirData.Virksomhet_Orgnr;
            model.Virksomhet_HerId = fhirData.Virksomhet_HerId;

            // §2-§5 (utvidet stønad, bittanomali-klassifisering, merknad) settes IKKE her —
            // se klassekommentar. Fylles ut manuelt av tannlegen/tannpleieren i skjemaet.
        }

        public Task ProcessDataWrite(
            Instance instance,
            Guid? dataId,
            object data,
            object? previousData,
            string? language = null
        )
        {
            return Task.CompletedTask;
        }
    }
}
