using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Altinn.App.Core.Features;
using Altinn.App.Core.Internal.Data;
using Altinn.App.Models;
using Altinn.Platform.Storage.Interface.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartFhir.Common;

namespace Altinn.App.Services
{
    /// <summary>
    /// Pre-fills ForerLegeerklaeringModel with data from FHIR resources fetched from the EPJ
    /// using the SMART access token stored in server session.
    ///
    /// Selve FHIR-hentingen/parsingen (Patient/Practitioner/Organization/Encounter) ligger nå i
    /// SmartFhirPrefillClient (src/SmartFhir.Common) — denne klassen mapper kun det resultatet
    /// over på ForerLegeerklaeringModel sine feltnavn, og beholder det som er spesifikt for
    /// nettopp dette caset: Diagnose (Condition) og hele ForerKonklusjonModel-avledningen ved
    /// signering (End/DeriveKonklusjon), som kjeveortopedisk-caset ikke har noe tilsvarende av.
    /// </summary>
    public class FhirPrefillService : IDataProcessor, IProcessTaskEnd
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<FhirPrefillService> _logger;
        private readonly IDataClient _dataClient;
        private readonly SmartFhirPrefillClient _fhirClient;

        // Kjøretøygrupper som tilhører gruppe 1 (lette kjøretøy)
        private static readonly HashSet<string> Gruppe1Koder = new(StringComparer.OrdinalIgnoreCase)
        {
            "A",
            "A1",
            "A2",
            "AM",
            "B",
            "B1",
            "BE",
            "S",
            "T",
        };

        // Kjøretøygrupper som tilhører gruppe 2 (tunge kjøretøy)
        private static readonly HashSet<string> Gruppe2Koder = new(StringComparer.OrdinalIgnoreCase)
        {
            "C",
            "C1",
            "CE",
            "C1E",
        };

        // Kjøretøygrupper som tilhører gruppe 3 (persontransport/utrykking)
        private static readonly HashSet<string> Gruppe3Koder = new(StringComparer.OrdinalIgnoreCase)
        {
            "D",
            "D1",
            "DE",
            "D1E",
        };

        public FhirPrefillService(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IMemoryCache memoryCache,
            ILogger<FhirPrefillService> logger,
            IDataClient dataClient
        )
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _memoryCache = memoryCache;
            _logger = logger;
            _dataClient = dataClient;
            _fhirClient = new SmartFhirPrefillClient(logger);
        }

        public async Task ProcessDataRead(Instance instance, Guid? dataId, object data, string? language = null)
        {
            _logger.LogInformation("FhirPrefillService.ProcessDataRead called for instance {InstanceId}", instance?.Id);

            if (data is not ForerLegeerklaeringModel model)
            {
                _logger.LogInformation("Data is not ForerLegeerklaeringModel — skipping");
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
            model.Pasient_Fodselsdato = fhirData.Pasient_Fodselsdato;
            model.Pasient_Kjonn = fhirData.Pasient_Kjonn;

            model.Lege_HPR = fhirData.Henviser_Hpr;
            model.Lege_Fornavn = fhirData.Henviser_Fornavn;
            model.Lege_Etternavn = fhirData.Henviser_Etternavn;

            model.Virksomhet_Navn = fhirData.Virksomhet_Navn;
            model.Virksomhet_Orgnr = fhirData.Virksomhet_Orgnr;
            model.Virksomhet_HerId = fhirData.Virksomhet_HerId;

            model.Konsultasjon_Dato = fhirData.Konsultasjon_Dato;

            model.Diagnose_Kode = fhirData.Diagnose_Kode;
            model.Diagnose_Tekst = fhirData.Diagnose_Tekst;
        }

        // BUG (2026-08-12): avledningen av ForerKonklusjonModel lå tidligere her, i
        // IDataProcessor.ProcessDataWrite. Den kjører på HVER autolagring mens legen fyller ut
        // skjemaet — ikke bare når legen faktisk trykker "Signer og send inn". SVV-konklusjonen
        // ble dermed skrevet/oppdatert gjentatte ganger under utfylling, ikke bare ved innsending.
        // Flyttet til End() (IProcessTaskEnd) under, som kjører når Task_1 (signeringsoppgaven)
        // faktisk avsluttes. ProcessDataWrite er nå en no-op, men må fortsatt implementeres siden
        // IDataProcessor krever den.
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

        /// <summary>
        /// Kjører når en prosessoppgave avsluttes. For Task_1 (signering/innsending) avleder og
        /// lagrer denne ForerKonklusjonModel — først NÅ, ikke ved hver autolagring under utfylling.
        /// </summary>
        public async Task End(string taskId, Instance instance)
        {
            if (!string.Equals(taskId, "Task_1", StringComparison.OrdinalIgnoreCase))
                return;

            var dataElement = instance.Data?.Find(d =>
                d.DataType.Equals("ForerLegeerklaering", StringComparison.OrdinalIgnoreCase)
            );
            if (dataElement == null || !Guid.TryParse(dataElement.Id, out var dataGuid))
            {
                _logger.LogWarning(
                    "End: fant ikke ForerLegeerklaering-dataelement på instans {InstanceId}",
                    instance.Id
                );
                return;
            }

            // Parse instance owner and id from instance.Id ("owner/instanceGuid")
            var parts = instance.Id?.Split('/');
            if (
                parts == null
                || parts.Length != 2
                || !int.TryParse(parts[0], out var instanceOwnerPartyId)
                || !Guid.TryParse(parts[1], out var instanceGuid)
            )
            {
                _logger.LogWarning("End: could not parse instance id '{InstanceId}'", instance.Id);
                return;
            }

            var appName = instance.AppId.Split('/').Last();
            var formDataObj = await _dataClient.GetFormData(
                instanceGuid,
                typeof(ForerLegeerklaeringModel),
                instance.Org,
                appName,
                instanceOwnerPartyId,
                dataGuid
            );
            if (formDataObj is not ForerLegeerklaeringModel src)
            {
                _logger.LogWarning("End: ForerLegeerklaering-dataelement kunne ikke deserialiseres");
                return;
            }

            var konklusjon = DeriveKonklusjon(src);

            // Check if a ForerKonklusjon data element already exists on this instance
            var existingElements = instance.Data ?? new List<DataElement>();
            var existing = existingElements.Find(d =>
                d.DataType.Equals("ForerKonklusjon", StringComparison.OrdinalIgnoreCase)
            );

            if (existing != null && Guid.TryParse(existing.Id, out var existingDataGuid))
            {
                _logger.LogInformation("End: updating existing ForerKonklusjon element {Id}", existing.Id);
                await _dataClient.UpdateData(
                    konklusjon,
                    instanceGuid,
                    typeof(ForerKonklusjonModel),
                    instance.Org,
                    appName,
                    instanceOwnerPartyId,
                    existingDataGuid
                );
            }
            else
            {
                _logger.LogInformation("End: creating new ForerKonklusjon element on instance {Id}", instance.Id);
                await _dataClient.InsertFormData(
                    konklusjon,
                    instanceGuid,
                    typeof(ForerKonklusjonModel),
                    instance.Org,
                    appName,
                    instanceOwnerPartyId,
                    "ForerKonklusjon"
                );
            }
        }

        private static ForerKonklusjonModel DeriveKonklusjon(ForerLegeerklaeringModel src)
        {
            var resultat = src.Forer_ErSkikket == true ? "skikket" : "ikke_skikket";
            var gruppe = src.Forer_Kjoretoygruppe ?? "";

            return new ForerKonklusjonModel
            {
                Pasient_Fnr = src.Pasient_Fnr,
                Lege_HPR = src.Lege_HPR,
                Gruppe1_Resultat = Gruppe1Koder.Contains(gruppe) ? resultat : "",
                Gruppe2_Resultat = Gruppe2Koder.Contains(gruppe) ? resultat : "",
                Gruppe3_Resultat = Gruppe3Koder.Contains(gruppe) ? resultat : "",
                Vilkar = src.Forer_Vilkar,
                Merknad = src.Forer_Merknad,
            };
        }
    }
}
