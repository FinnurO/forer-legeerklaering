using System.Text;
using System.Text.Json;
using HelseId.Library;
using HelseId.Library.ClientCredentials;
using HelseId.Library.ClientCredentials.Interfaces;
using HelseId.Library.Configuration;
using HelseId.Library.ExtensionMethods;
using HelseId.Library.Interfaces.JwtTokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ---------------------------------------------------------------------------------------------
// Eksperiment: kan vi sende en "melding" til en innbygger som er en kopi av en innsending (f.eks.
// den signerte legeerklæringen), via Helsenorge sitt Skjema-API (nhn:helsenorge.eksternapi/skjema),
// som eksponerer FHIR DocumentReference (POST .../skjema/v1/DocumentReference)?
//
// Dette er en ANNEN API/løsningsområde enn Oppgave (Task) — bekreftet via Swagger
// (https://eksternapi.hn2.test.nhn.no/skjema/v1/swagger/v1/swagger.json), som viser
// paths: /v1/DocumentReference, /v1/DocumentReference/_search, /v1/DocumentReference/{id}.
// Responstypen heter "PostSkjemainstansResponse" — antyder at dette API-et konseptuelt handler om
// "skjemainstanser", ikke bare frie dokumenter. Ikke å forveksle med "DokumentAPI"
// (dokumenter/api/v1/SaveDokument, se PASIENTFLYT.md §4) — det er et tredje, separat API med en
// helt annen autentiseringsmodell (brukertilstedeværelse/OIDC, ikke client_credentials).
//
// Metodikk: samme empiriske "send → les konkret FHIR OperationOutcome-feilkode → juster → prøv
// igjen"-mønster som løste Oppgave/Task. Se README.md for loggen av forsøk.
//
// Mottaker er "Høy Hai" (fnr 21814497167) — samme bekreftet digitalt aktive testperson.
//
// Kjør:
//   $env:HELSEID_JWK_PATH = "C:\Users\jsf\.secrets\helseid-eksternapi-test.jwk.json"
//   dotnet run
// ---------------------------------------------------------------------------------------------

const string ClientId = "4f1fc480-72d9-4e31-b099-69b84fd5ba6b"; // "Altinn Studio"-klienten
const string IssuerUri = "https://helseid-sts.test.nhn.no";
const string Scope = "nhn:helsenorge.eksternapi/skjema";

const string EksternApiBaseUrl = "https://eksternapi.hn2.test.nhn.no";
const string DocumentReferenceEndpoint = $"{EksternApiBaseUrl}/skjema/v1/DocumentReference";

const string RequesterOrgnr = "310911186"; // LAV MODIG TIGER AS
const string RequesterOrgName = "LAV MODIG TIGER AS";

const string OwnerFnr = "21814497167"; // Høy Hai

// KRITISK FUNN (Confluence "Benyttede FHIR Ressurser i Skjemaløsningen", 2026-09-21): en
// DocumentReference ("kopi av innsending") kan IKKE sendes fritt — den må knyttes til en
// eksisterende skjemaoppgave (Task) via context.related, som refererer til Task.identifier.
// Dette er identifier-verdien fra Task-en opprettet av helsenorge-skjemaoppgave-test (HTTP 201,
// Task.id/identifier = cabf75ca-3675-492c-b800-d97686801887).
const string RelatedTaskIdentifierGuid = "cabf75ca-3675-492c-b800-d97686801887";

var jwkPath = Environment.GetEnvironmentVariable("HELSEID_JWK_PATH");
if (string.IsNullOrWhiteSpace(jwkPath))
{
    Console.Error.WriteLine("Miljøvariabelen HELSEID_JWK_PATH er ikke satt.");
    Console.Error.WriteLine(@"  $env:HELSEID_JWK_PATH = ""C:\Users\jsf\.secrets\helseid-eksternapi-test.jwk.json""");
    return 1;
}

if (!File.Exists(jwkPath))
{
    Console.Error.WriteLine($"Fant ingen fil på HELSEID_JWK_PATH: {jwkPath}");
    return 1;
}

var privateKeyJwk = File.ReadAllText(jwkPath);

var builder = Host.CreateApplicationBuilder(args);

var helseIdConfiguration = new HelseIdConfiguration
{
    ClientId = ClientId,
    Scope = Scope,
    IssuerUri = IssuerUri,
};

builder
    .Services.AddHelseIdClientCredentials(helseIdConfiguration)
    .AddHelseIdMultiTenant()
    .AddJwkForClientAuthentication(privateKeyJwk);

var host = builder.Build();

var flow = host.Services.GetRequiredService<IHelseIdClientCredentialsFlow>();
var dPoPProofCreator = host.Services.GetRequiredService<IDPoPProofCreatorForApiRequests>();

Console.WriteLine($"1) Henter token fra {IssuerUri} (scope: {Scope}, orgnr_parent: {RequesterOrgnr}) ...");

var organizationNumbers = new HelseId.Library.Models.DetailsFromClient.OrganizationNumbers
{
    ParentOrganization = RequesterOrgnr,
    ChildOrganization = RequesterOrgnr,
};
var tokenResponse = await flow.GetTokenResponseAsync(Scope, organizationNumbers);

if (!tokenResponse.IsSuccessful(out var accessTokenResponse))
{
    var error = tokenResponse.AsError();
    Console.Error.WriteLine("❌ Token-forespørsel feilet.");
    Console.Error.WriteLine($"   Error: {error.Error} — {error.ErrorDescription}");
    return 1;
}

Console.WriteLine(
    $"   ✅ Token mottatt (scope: {accessTokenResponse.Scope}, utløper om {accessTokenResponse.ExpiresIn}s)"
);
Console.WriteLine();

var docIdentifierGuid = Guid.NewGuid();

// Forsøk 3 (kode 119, "Fil signatur stemmer ikke for mimetype application/pdf") viste at
// innholdet faktisk valideres som ekte PDF (magic bytes %PDF), ikke bare MIME-typen deklarert.
// Gjenbruker derfor Helsenorges EGET eksempel-PDF fra "Benyttede FHIR Ressurser i
// Skjemaløsningen" (Confluence) — en ekte, minimal, gyldig PDF, ikke vårt eget testinnhold.
var testPdfBase64 = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "test-pdf-base64.txt")).Trim();

// Forsøk 1: minimal DocumentReference — status/type/subject/content/author, etter samme mønster
// som Task (contained Organization for author, identifier-basert Patient-referanse for subject).
var documentReference = new Dictionary<string, object?>
{
    ["resourceType"] = "DocumentReference",
    ["contained"] = new object[]
    {
        new Dictionary<string, object?>
        {
            ["resourceType"] = "Organization",
            ["id"] = "author-1",
            ["identifier"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["system"] = "urn:oid:2.16.578.1.12.4.1.4.101",
                    ["value"] = RequesterOrgnr,
                },
            },
            ["name"] = RequesterOrgName,
        },
    },
    ["identifier"] = new object[]
    {
        new Dictionary<string, object?>
        {
            ["system"] = "urn:ietf:rfc:3986",
            ["value"] = $"urn:uuid:{docIdentifierGuid}",
        },
    },
    ["status"] = "current",
    ["docStatus"] = "final", // Manglet i forsøk 1 — XML-eksemplet (Confluence) har begge status-feltene
    // LOINC 34108-1 "Outpatient Note" — samme kode som TestWriteback i SmartLaunchControllerBase
    // bruker mot EPJ-siden, gjenbrukt her som et plausibelt, generisk "notat"-kodeverdi.
    ["type"] = new Dictionary<string, object?>
    {
        ["coding"] = new object[]
        {
            new Dictionary<string, object?>
            {
                ["system"] = "http://loinc.org",
                ["code"] = "34108-1",
                ["display"] = "Outpatient Note",
            },
        },
    },
    ["subject"] = new Dictionary<string, object?>
    {
        ["type"] = "Patient",
        ["identifier"] = new Dictionary<string, object?>
        {
            ["system"] = "urn:oid:2.16.578.1.12.4.1.4.1",
            ["value"] = OwnerFnr,
        },
    },
    ["author"] = new object[]
    {
        new Dictionary<string, object?> { ["reference"] = "#author-1", ["type"] = "Organization" },
    },
    ["description"] = "TEST — kopi av innsending (Digdir forer-legeerklaering PoC). Kan trygt ignoreres/kanselleres.",
    ["content"] = new object[]
    {
        new Dictionary<string, object?>
        {
            ["attachment"] = new Dictionary<string, object?>
            {
                // "final" DocumentReference krever eksplisitt application/pdf (kode 1112) —
                // innholdet under er IKKE en ekte gyldig PDF, bare en test av valideringsregelen.
                ["contentType"] = "application/pdf",
                ["data"] = testPdfBase64,
                ["title"] = "TEST — kopi av innsending",
            },
        },
    },
    ["date"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
    // Obligatorisk knytning til skjemaoppgaven (Task) dette er en kopi av — se
    // "Benyttede FHIR Ressurser i Skjemaløsningen" (Confluence, 2026-09-21).
    ["context"] = new Dictionary<string, object?>
    {
        ["related"] = new object[]
        {
            new Dictionary<string, object?>
            {
                ["type"] = "Task",
                ["identifier"] = new Dictionary<string, object?>
                {
                    ["system"] = "urn:ietf:rfc:3986",
                    ["value"] = $"urn:uuid:{RelatedTaskIdentifierGuid}",
                },
            },
        },
    },
};

var docJson = JsonSerializer.Serialize(documentReference, new JsonSerializerOptions { WriteIndented = true });

Console.WriteLine("2) Sender FHIR DocumentReference:");
Console.WriteLine(docJson);
Console.WriteLine();

using var httpClient = new HttpClient();
var request = new HttpRequestMessage(HttpMethod.Post, DocumentReferenceEndpoint)
{
    Content = new StringContent(docJson, Encoding.UTF8, "application/fhir+json"),
};

var dPoPProof = await dPoPProofCreator.CreateDPoPProofForApiRequest(
    HttpMethod.Post,
    DocumentReferenceEndpoint,
    accessTokenResponse
);
request.SetDPoPTokenAndProof(accessTokenResponse, dPoPProof);

Console.WriteLine($"3) POST {DocumentReferenceEndpoint}");
var response = await httpClient.SendAsync(request);
var responseBody = await response.Content.ReadAsStringAsync();

Console.WriteLine($"   HTTP {(int)response.StatusCode} {response.StatusCode}");
Console.WriteLine();
Console.WriteLine(responseBody);

return response.IsSuccessStatusCode ? 0 : 1;
