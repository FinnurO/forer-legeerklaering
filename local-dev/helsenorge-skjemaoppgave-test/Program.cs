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
// Eksperiment: kan en FHIR Task med focus.type = "Questionnaire" (skjemaoppgave) peke ut til et
// eget skjema (f.eks. vårt localtest-skjema), i motsetning til focus.type = "Communication" (kun
// en "les info"-lenke, verifisert i local-dev/helsenorge-oppgave-test/)?
//
// Metodikk: samme empiriske "send → les konkret FHIR OperationOutcome-feilkode → juster → prøv
// igjen"-mønster som løste Communication-varianten. Se README.md for loggen av forsøk.
//
// Mottaker er "Høy Hai" (fnr 21814497167) — samme bekreftet digitalt aktive testperson som i
// helsenorge-oppgave-test.
//
// Kjør:
//   $env:HELSEID_JWK_PATH = "C:\Users\jsf\.secrets\helseid-eksternapi-test.jwk.json"
//   dotnet run
// ---------------------------------------------------------------------------------------------

const string ClientId = "4f1fc480-72d9-4e31-b099-69b84fd5ba6b"; // "Altinn Studio"-klienten
const string IssuerUri = "https://helseid-sts.test.nhn.no";
const string Scope = "nhn:helsenorge.eksternapi/oppgave";

const string EksternApiBaseUrl = "https://eksternapi.hn2.test.nhn.no";
const string OppgaveEndpoint = $"{EksternApiBaseUrl}/oppgave/v1/Task";

const string RequesterOrgnr = "310911186"; // LAV MODIG TIGER AS
const string RequesterOrgName = "LAV MODIG TIGER AS";

const string OwnerFnr = "21814497167"; // Høy Hai

// Forsøk 1 (http://local.altinn.cloud:8000/...) ga 400 — "URI scheme må være HTTPS" (kode 2174).
// Forsøk 2: isoler om resten av Questionnaire-oppsettet er riktig ved å bruke en ekte HTTPS-URL vi
// allerede vet er akseptert (samme repo-lenke som Communication-varianten brukte).
const string LocalTestSkjemaUrl = "https://github.com/FinnurO/forer-legeerklaering";

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

var taskIdentifierGuid = Guid.NewGuid();
var deadline = DateTimeOffset.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:sszzz");
var questionnaireId = "questionnaire-1";

// Forsøk 1: focus.type = "Questionnaire" med en contained Questionnaire-ressurs hvis .url peker
// til vårt localtest-skjema — analogt med hvordan Communication-varianten pekte til et nettsted
// via instantiatesUri, men her via en ekte FHIR Questionnaire.url siden focus nå refererer til en
// reell ressurstype med skjemainnhold, ikke bare en fritekst-info-side.
var task = new Dictionary<string, object?>
{
    ["resourceType"] = "Task",
    ["contained"] = new object[]
    {
        new Dictionary<string, object?>
        {
            ["resourceType"] = "Organization",
            ["id"] = "requester-1",
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
        new Dictionary<string, object?>
        {
            ["resourceType"] = "Questionnaire",
            ["id"] = questionnaireId,
            ["url"] = LocalTestSkjemaUrl,
            ["status"] = "active",
            ["title"] = "TEST — skjemaoppgave (localtest-lenke)",
        },
    },
    ["meta"] = new Dictionary<string, object?>
    {
        ["security"] = new object[]
        {
            new Dictionary<string, object?>
            {
                ["system"] = "urn:oid:2.16.578.1.12.4.1.1.7618",
                ["code"] = "3",
                ["display"] = "Helsehjelp",
            },
        },
    },
    ["identifier"] = new object[]
    {
        new Dictionary<string, object?>
        {
            ["system"] = "urn:ietf:rfc:3986",
            ["value"] = $"urn:uuid:{taskIdentifierGuid}",
        },
    },
    ["status"] = "ready",
    ["intent"] = "proposal",
    ["code"] = new Dictionary<string, object?> { ["text"] = "TEST — skjemaoppgave (Questionnaire-lenke)" },
    ["description"] =
        "TEST fra Digdir sin forer-legeerklaering PoC. Eksperiment: skjemaoppgave (focus.type=Questionnaire) "
        + "som peker til vårt eget localtest-skjema. Kan trygt ignoreres/kanselleres.",
    ["focus"] = new Dictionary<string, object?> { ["type"] = "Questionnaire", ["reference"] = $"#{questionnaireId}" },
    ["instantiatesUri"] = LocalTestSkjemaUrl,
    ["requester"] = new Dictionary<string, object?> { ["reference"] = "#requester-1", ["type"] = "Organization" },
    ["owner"] = new Dictionary<string, object?>
    {
        ["type"] = "Patient",
        ["identifier"] = new Dictionary<string, object?>
        {
            ["system"] = "urn:oid:2.16.578.1.12.4.1.4.1",
            ["value"] = OwnerFnr,
        },
    },
    ["restriction"] = new Dictionary<string, object?>
    {
        ["period"] = new Dictionary<string, object?> { ["end"] = deadline },
    },
};

var taskJson = JsonSerializer.Serialize(task, new JsonSerializerOptions { WriteIndented = true });

Console.WriteLine("2) Sender FHIR Task (focus.type=Questionnaire):");
Console.WriteLine(taskJson);
Console.WriteLine();

using var httpClient = new HttpClient();
var request = new HttpRequestMessage(HttpMethod.Post, OppgaveEndpoint)
{
    Content = new StringContent(taskJson, Encoding.UTF8, "application/fhir+json"),
};

var dPoPProof = await dPoPProofCreator.CreateDPoPProofForApiRequest(
    HttpMethod.Post,
    OppgaveEndpoint,
    accessTokenResponse
);
request.SetDPoPTokenAndProof(accessTokenResponse, dPoPProof);

Console.WriteLine($"3) POST {OppgaveEndpoint}");
var response = await httpClient.SendAsync(request);
var responseBody = await response.Content.ReadAsStringAsync();

Console.WriteLine($"   HTTP {(int)response.StatusCode} {response.StatusCode}");
Console.WriteLine();
Console.WriteLine(responseBody);

return response.IsSuccessStatusCode ? 0 : 1;
