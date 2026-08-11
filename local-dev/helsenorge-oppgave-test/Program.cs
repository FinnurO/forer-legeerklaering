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
// Sender én test-Oppgave (FHIR Task) til Helsenorge EksternAPI TEST02, for å verifisere at
// selve API-kallet fungerer — ikke bare token-utvekslingen (bekreftet i local-dev/helseid-token-test/).
//
// Mottaker er "Høy Hai" (fnr 21814497167) — en ekte Tenor-sjekket syntetisk testperson (ikke en
// reell person), oppgitt av Johann og verifisert med gyldig MOD11-kontrollsiffer.
//
// SIKKERHET: privatnøkkelen (JWK) leses fra en lokal fil utenfor repoet, se
// local-dev/helseid-token-test/README.md for fremgangsmåte. Samme miljøvariabel gjenbrukes her.
//
// Kjør:
//   $env:HELSEID_JWK_PATH = "C:\Users\jsf\.secrets\helseid-eksternapi-test.jwk.json"
//   dotnet run
// ---------------------------------------------------------------------------------------------

const string ClientId = "4f1fc480-72d9-4e31-b099-69b84fd5ba6b"; // "Altinn Studio"-klienten
const string IssuerUri = "https://helseid-sts.test.nhn.no";
const string Scope = "nhn:helsenorge.eksternapi/oppgave";

// TEST02 — "normalt det miljøet som benyttes ved oppkobling av nye eksterne integrasjoner"
// jf. Testmiljøer og endepunkter-dokumentasjonen.
const string EksternApiBaseUrl = "https://eksternapi.hn2.test.nhn.no";
const string OppgaveEndpoint = $"{EksternApiBaseUrl}/oppgave/v1/Task";

const string RequesterOrgnr = "310911186"; // LAV MODIG TIGER AS — hovedenhet for "Altinn Studio"-klienten
const string RequesterOrgName = "LAV MODIG TIGER AS";

const string OwnerFnr = "26908896636"; // Sart Maskin — Tenor-sjekket syntetisk testperson (forsøk 2, etter at Høy Hai ikke var digitalt aktiv)

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

// Klienten "Altinn Studio" er registrert som multi-tenant i HelseID — krever både parent- og
// child-organisasjon. Vi har ingen egen underenhet, så vi bruker samme orgnr for begge.
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

// Bygg en minimal, tydelig merket TEST-oppgave som FHIR Task.
var taskIdentifierGuid = Guid.NewGuid();
var deadline = DateTimeOffset.UtcNow.AddDays(30).ToString("yyyy-MM-ddTHH:mm:sszzz");

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
    },
    ["meta"] = new Dictionary<string, object?>
    {
        // Volven kodeverk 7618 — tjenesteområde. Kode 3 = "Helsehjelp".
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
    ["code"] = new Dictionary<string, object?> { ["text"] = "TEST — teknisk tilkoblingstest" },
    ["description"] =
        "TEST fra Digdir sin forer-legeerklaering PoC (SMART on FHIR + Altinn Studio). "
        + "Dette er en teknisk tilkoblingstest av Helsenorge EksternAPI, ikke en reell oppgave. "
        + "Kan trygt ignoreres/kanselleres.",
    // Enklest mulige oppgavetype for en tilkoblingstest: "Communication" = informasjonsoppgave,
    // krever ingen ekstern Questionnaire/Device-referanse — men instantiatesUri er obligatorisk
    // for denne typen (peker til nettstedet der informasjonen finnes).
    ["focus"] = new Dictionary<string, object?> { ["type"] = "Communication" },
    ["instantiatesUri"] = "https://github.com/FinnurO/forer-legeerklaering",
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

Console.WriteLine("2) Sender FHIR Task:");
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
