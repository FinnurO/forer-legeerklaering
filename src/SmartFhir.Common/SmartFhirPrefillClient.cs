using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SmartFhir.Common.Session;

namespace SmartFhir.Common
{
    /// <summary>
    /// Henter og parser de FHIR-ressursene som er felles for alle caser (Patient, Practitioner
    /// [+PractitionerRole], Organization, Encounter, Condition) inn i en app-nøytral
    /// StandardFhirPrefillData. Flyttet ut av FhirPrefillService (ForerLegeerklaering) 2026-09-09
    /// slik at nye apper (f.eks. AppKjeveortopedisk) ikke må skrive denne FHIR-henting/parsing-
    /// logikken på nytt — kun mappe StandardFhirPrefillData over på sin egen datamodell.
    ///
    /// Caller er ansvarlig for å opprette en HttpClient med Authorization-header (Bearer
    /// access_token) satt — se hver apps egen FhirPrefillService for hvordan token hentes fra
    /// session/cache.
    /// </summary>
    public class SmartFhirPrefillClient
    {
        private readonly ILogger _logger;

        public SmartFhirPrefillClient(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Henter alt som er tilgjengelig av standardfelter for gitt launch-kontekst. Enkeltkall
        /// som feiler (nettverk, 404, manglende felt) logges som warning og hopper bare over de
        /// aktuelle feltene — resten av prefillen fullføres uansett (samme "best effort"-prinsipp
        /// som den opprinnelige FhirPrefillService).
        /// </summary>
        public async Task<StandardFhirPrefillData> FetchAsync(HttpClient client, FhirLaunchContext ctx)
        {
            var data = new StandardFhirPrefillData();

            await FillPatient(client, ctx, data);
            await FillPractitioner(client, ctx, data);
            await FillEncounterAndOrganization(client, ctx, data);
            await FillCondition(client, ctx, data);

            return data;
        }

        private async Task<JsonDocument> TryGetFhirResource(HttpClient client, string url, string resourceLabel)
        {
            try
            {
                var json = await client.GetStringAsync(url);
                return JsonDocument.Parse(json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch FHIR resource {Label} from {Url}", resourceLabel, url);
                return null;
            }
        }

        private async Task FillPatient(HttpClient client, FhirLaunchContext ctx, StandardFhirPrefillData data)
        {
            if (string.IsNullOrEmpty(ctx.PatientId))
                return;
            using var doc = await TryGetFhirResource(client, $"{ctx.FhirBaseUrl}/Patient/{ctx.PatientId}", "Patient");
            if (doc == null)
                return;
            var root = doc.RootElement;

            data.Pasient_Fnr = GetIdentifier(root, NorwegianHealthOids.Fnr);
            data.Pasient_Fodselsdato = root.TryGetProperty("birthDate", out var bd) ? bd.GetString() : null;
            data.Pasient_Kjonn = root.TryGetProperty("gender", out var g) ? g.GetString() : null;

            if (root.TryGetProperty("name", out var names) && names.GetArrayLength() > 0)
            {
                var name = names[0];
                data.Pasient_Fornavn =
                    name.TryGetProperty("given", out var given) && given.GetArrayLength() > 0
                        ? given[0].GetString()
                        : null;
                data.Pasient_Etternavn = name.TryGetProperty("family", out var family) ? family.GetString() : null;
            }

            if (root.TryGetProperty("address", out var addresses) && addresses.GetArrayLength() > 0)
                data.Pasient_Adresse = FormatAddress(addresses[0]);
        }

        private static string FormatAddress(JsonElement address)
        {
            var lines = new System.Collections.Generic.List<string>();

            if (address.TryGetProperty("line", out var lineArray))
                foreach (var line in lineArray.EnumerateArray())
                    if (line.GetString() is { Length: > 0 } l)
                        lines.Add(l);

            var postalCode = address.TryGetProperty("postalCode", out var pc) ? pc.GetString() : null;
            var city = address.TryGetProperty("city", out var c) ? c.GetString() : null;
            var postSted = string.Join(" ", new[] { postalCode, city }.Where(s => !string.IsNullOrEmpty(s)));
            if (!string.IsNullOrEmpty(postSted))
                lines.Add(postSted);

            return lines.Count > 0 ? string.Join(", ", lines) : null;
        }

        private async Task FillPractitioner(HttpClient client, FhirLaunchContext ctx, StandardFhirPrefillData data)
        {
            // Per SMART App Launch IG er fhirUser typisk en full URL, men noen servere (bl.a.
            // launch.smarthealthit.org) returnerer en relativ referanse ("Practitioner/<id>").
            if (string.IsNullOrEmpty(ctx.FhirUser))
                return;
            var fhirUserUrl = ctx.FhirUser.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? ctx.FhirUser
                : $"{ctx.FhirBaseUrl}/{ctx.FhirUser}";
            using var doc = await TryGetFhirResource(client, fhirUserUrl, "Practitioner");
            if (doc == null)
                return;
            var root = doc.RootElement;

            data.Henviser_Hpr = GetIdentifier(root, NorwegianHealthOids.Hpr);

            // Også prøv PractitionerRole for organisasjonstilknytning
            var practitionerId = root.TryGetProperty("id", out var pid) ? pid.GetString() : null;
            if (!string.IsNullOrEmpty(practitionerId))
                await FillPractitionerRole(client, ctx, practitionerId, data);

            if (root.TryGetProperty("name", out var names) && names.GetArrayLength() > 0)
            {
                var name = names[0];
                data.Henviser_Fornavn =
                    name.TryGetProperty("given", out var given) && given.GetArrayLength() > 0
                        ? given[0].GetString()
                        : null;
                data.Henviser_Etternavn = name.TryGetProperty("family", out var family) ? family.GetString() : null;
            }
        }

        private async Task FillPractitionerRole(
            HttpClient client,
            FhirLaunchContext ctx,
            string practitionerId,
            StandardFhirPrefillData data
        )
        {
            var url = $"{ctx.FhirBaseUrl}/PractitionerRole?practitioner={practitionerId}&_count=1";
            using var doc = await TryGetFhirResource(client, url, "PractitionerRole");
            if (doc == null)
                return;
            var root = doc.RootElement;

            if (!root.TryGetProperty("entry", out var entries) || entries.GetArrayLength() == 0)
                return;

            var role = entries[0].GetProperty("resource");
            if (
                role.TryGetProperty("organization", out var orgRef)
                && orgRef.TryGetProperty("reference", out var orgRefVal)
            )
            {
                var orgUrl = orgRefVal.GetString();
                if (!string.IsNullOrEmpty(orgUrl))
                {
                    if (!orgUrl.StartsWith("http"))
                        orgUrl = $"{ctx.FhirBaseUrl}/{orgUrl}";
                    await FillOrganization(client, orgUrl, data);
                }
            }
        }

        private async Task FillEncounterAndOrganization(
            HttpClient client,
            FhirLaunchContext ctx,
            StandardFhirPrefillData data
        )
        {
            if (string.IsNullOrEmpty(ctx.EncounterId))
                return;
            using var doc = await TryGetFhirResource(
                client,
                $"{ctx.FhirBaseUrl}/Encounter/{ctx.EncounterId}",
                "Encounter"
            );
            if (doc == null)
                return;
            var root = doc.RootElement;

            if (root.TryGetProperty("period", out var period) && period.TryGetProperty("start", out var start))
                data.Konsultasjon_Dato = start.GetString();

            // Fall back til Encounter.serviceProvider hvis PractitionerRole-oppslaget ikke traff
            if (
                string.IsNullOrEmpty(data.Virksomhet_Navn)
                && root.TryGetProperty("serviceProvider", out var sp)
                && sp.TryGetProperty("reference", out var orgRef)
            )
            {
                var orgUrl = orgRef.GetString();
                if (!orgUrl.StartsWith("http"))
                    orgUrl = $"{ctx.FhirBaseUrl}/{orgUrl}";
                await FillOrganization(client, orgUrl, data);
            }
        }

        private async Task FillOrganization(HttpClient client, string orgUrl, StandardFhirPrefillData data)
        {
            using var doc = await TryGetFhirResource(client, orgUrl, "Organization");
            if (doc == null)
                return;
            var root = doc.RootElement;

            data.Virksomhet_Orgnr = GetIdentifier(root, NorwegianHealthOids.Orgnr);
            data.Virksomhet_HerId = GetIdentifier(root, NorwegianHealthOids.HerId);
            data.Virksomhet_Navn = root.TryGetProperty("name", out var name) ? name.GetString() : null;
        }

        private async Task FillCondition(HttpClient client, FhirLaunchContext ctx, StandardFhirPrefillData data)
        {
            if (string.IsNullOrEmpty(ctx.PatientId))
                return;

            var url =
                $"{ctx.FhirBaseUrl}/Condition?patient={ctx.PatientId}&clinical-status=active&_sort=-recorded-date&_count=1";
            if (!string.IsNullOrEmpty(ctx.EncounterId))
                url += $"&encounter={ctx.EncounterId}";

            using var doc = await TryGetFhirResource(client, url, "Condition");
            if (doc == null)
                return;
            var root = doc.RootElement;

            if (!root.TryGetProperty("entry", out var entries) || entries.GetArrayLength() == 0)
                return;

            var condition = entries[0].GetProperty("resource");
            if (
                condition.TryGetProperty("code", out var code)
                && code.TryGetProperty("coding", out var codings)
                && codings.GetArrayLength() > 0
            )
            {
                var coding = codings[0];
                data.Diagnose_Kode = coding.TryGetProperty("code", out var c) ? c.GetString() : null;
                data.Diagnose_Tekst = coding.TryGetProperty("display", out var d) ? d.GetString() : null;
            }
        }

        private static string GetIdentifier(JsonElement resource, string system)
        {
            if (!resource.TryGetProperty("identifier", out var identifiers))
                return null;
            foreach (var id in identifiers.EnumerateArray())
            {
                if (
                    id.TryGetProperty("system", out var sys)
                    && sys.GetString() == system
                    && id.TryGetProperty("value", out var val)
                )
                    return val.GetString();
            }
            return null;
        }
    }
}
