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
// Eksperiment: den faktiske "Brevtjenesten"-scenarioen Johann beskrev 2026-09-21 —
// https://www.nhn.no/tjenester/helsenorge/sende-brev-skjema-og-andre-oppgaver
//
// EN HELSEAKTØR (f.eks. fastlege via vår Altinn-app) sender en henvisning til EN ANNEN
// HELSEAKTØR (f.eks. en spesialist) via en helt annen kanal (EDI/HER-ID, utenfor Helsenorge), og
// ønsker SAMTIDIG å sende en lesbar KOPI av henvisningen til PASIENTEN via Helsenorge.
//
// Dette er IKKE samme scenario som "ekstern skjemautfyller sender ferdig utfylt skjema til
// Helsenorge" (helsenorge-skjema-melding-test/) — der var det Helsenorge selv som opprinnelig ba
// om skjemaet (en skjemaoppgave, Questionnaire). Her er det VI (helseaktøren) som initierer alt:
// både selve henvisningen (extern, ikke en del av dette) og "her er en kopi til deg"-varselet.
//
// Hypotese, basert på FHIR Task-dokumentasjonen (Task.basedOn kan referere til en "ServiceRequest"
// = Henvisning, se https://helsenorge.atlassian.net/wiki/spaces/HELSENORGE/pages/742948883):
//   1. Send en Communication-type Task (samme mekanisme som helsenorge-oppgave-test/, allerede
//      verifisert HTTP 201) — MEN med Task.basedOn satt til henvisningens ServiceRequest-identifier,
//      slik at oppgaven eksplisitt sier "dette gjelder henvisning X" til pasienten.
//   2. Send en DocumentReference (samme mekanisme som helsenorge-skjema-melding-test/, allerede
//      verifisert HTTP 200) med context.related pekende til DENNE Task-en OG direkte til
//      henvisningen — altså den generelle "knytt et dokument til en oppgave/ressurs"-mekanismen,
//      ikke spesifikk for skjemaoppgaver.
//
// Mottaker er "Høy Hai" (fnr 21814497167) — samme bekreftet digitalt aktive testperson.
//
// Kjør:
//   $env:HELSEID_JWK_PATH = "C:\Users\jsf\.secrets\helseid-eksternapi-test.jwk.json"
//   dotnet run
// ---------------------------------------------------------------------------------------------

const string ClientId = "4f1fc480-72d9-4e31-b099-69b84fd5ba6b"; // "Altinn Studio"-klienten
const string IssuerUri = "https://helseid-sts.test.nhn.no";

const string EksternApiBaseUrl = "https://eksternapi.hn2.test.nhn.no";
const string OppgaveEndpoint = $"{EksternApiBaseUrl}/oppgave/v1/Task";
const string DocumentReferenceEndpoint = $"{EksternApiBaseUrl}/skjema/v1/DocumentReference";

const string RequesterOrgnr = "310911186"; // LAV MODIG TIGER AS
const string RequesterOrgName = "LAV MODIG TIGER AS";

const string OwnerFnr = "21814497167"; // Høy Hai

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
var organizationNumbers = new HelseId.Library.Models.DetailsFromClient.OrganizationNumbers
{
    ParentOrganization = RequesterOrgnr,
    ChildOrganization = RequesterOrgnr,
};

using var httpClient = new HttpClient();

// ---------------------------------------------------------------------------------------------
// Steg 1: fingert henvisning (ServiceRequest) — ikke sendt noe sted, kun en identifier som Task
// og DocumentReference kan referere til, for å simulere "henvisningen som allerede er sendt til
// spesialisten via EDI/HER-ID".
// ---------------------------------------------------------------------------------------------
var henvisningIdentifierGuid = Guid.NewGuid();
Console.WriteLine($"Simulert henvisning (ServiceRequest), identifier: urn:uuid:{henvisningIdentifierGuid}");
Console.WriteLine("(Sendes ikke noe sted i dette eksperimentet — kun brukt som referanse i Task.basedOn.)");
Console.WriteLine();

// ---------------------------------------------------------------------------------------------
// Steg 2: send Communication-Task til pasienten, MED Task.basedOn -> henvisningen.
// ---------------------------------------------------------------------------------------------
const string OppgaveScope = "nhn:helsenorge.eksternapi/oppgave";
Console.WriteLine($"1) Henter token for scope: {OppgaveScope} ...");

var oppgaveBuilder = Host.CreateApplicationBuilder();
oppgaveBuilder
    .Services.AddHelseIdClientCredentials(
        new HelseIdConfiguration
        {
            ClientId = ClientId,
            Scope = OppgaveScope,
            IssuerUri = IssuerUri,
        }
    )
    .AddHelseIdMultiTenant()
    .AddJwkForClientAuthentication(privateKeyJwk);
var oppgaveHost = oppgaveBuilder.Build();
var oppgaveFlow = oppgaveHost.Services.GetRequiredService<IHelseIdClientCredentialsFlow>();
var oppgaveDpop = oppgaveHost.Services.GetRequiredService<IDPoPProofCreatorForApiRequests>();
var oppgaveTokenResponse = await oppgaveFlow.GetTokenResponseAsync(OppgaveScope, organizationNumbers);
if (!oppgaveTokenResponse.IsSuccessful(out var oppgaveToken))
{
    var error = oppgaveTokenResponse.AsError();
    Console.Error.WriteLine($"❌ Token-forespørsel feilet: {error.Error} — {error.ErrorDescription}");
    return 1;
}
Console.WriteLine("   ✅ Token mottatt");
Console.WriteLine();

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
    // Task.basedOn -> henvisningen (ServiceRequest) dette varselet/kopien gjelder.
    ["basedOn"] = new object[]
    {
        new Dictionary<string, object?>
        {
            ["type"] = "ServiceRequest",
            ["identifier"] = new Dictionary<string, object?>
            {
                ["system"] = "urn:ietf:rfc:3986",
                ["value"] = $"urn:uuid:{henvisningIdentifierGuid}",
            },
            ["display"] = "Henvisning",
        },
    },
    ["status"] = "ready",
    ["intent"] = "proposal",
    ["code"] = new Dictionary<string, object?> { ["text"] = "TEST — kopi av henvisning sendt til spesialist" },
    ["description"] =
        "TEST fra Digdir sin forer-legeerklaering PoC. Din henvisning er sendt til spesialist. "
        + "Her er en kopi til deg, til orientering. Kan trygt ignoreres/kanselleres.",
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
Console.WriteLine($"2) POST {OppgaveEndpoint} (Communication-Task med basedOn->ServiceRequest):");
Console.WriteLine(taskJson);
Console.WriteLine();

var taskRequest = new HttpRequestMessage(HttpMethod.Post, OppgaveEndpoint)
{
    Content = new StringContent(taskJson, Encoding.UTF8, "application/fhir+json"),
};
var taskProof = await oppgaveDpop.CreateDPoPProofForApiRequest(HttpMethod.Post, OppgaveEndpoint, oppgaveToken);
taskRequest.SetDPoPTokenAndProof(oppgaveToken, taskProof);
var taskResponse = await httpClient.SendAsync(taskRequest);
var taskResponseBody = await taskResponse.Content.ReadAsStringAsync();

Console.WriteLine($"   HTTP {(int)taskResponse.StatusCode} {taskResponse.StatusCode}");
Console.WriteLine(taskResponseBody);
Console.WriteLine();

if (!taskResponse.IsSuccessStatusCode)
{
    Console.Error.WriteLine("❌ Task-opprettelse feilet — avbryter før DocumentReference-forsøket.");
    return 1;
}

// ---------------------------------------------------------------------------------------------
// Steg 3: send DocumentReference (kopi av henvisningen som PDF) knyttet til DENNE Task-en OG
// direkte til henvisningen.
// ---------------------------------------------------------------------------------------------
const string SkjemaScope = "nhn:helsenorge.eksternapi/skjema";
Console.WriteLine($"3) Henter token for scope: {SkjemaScope} ...");

var skjemaBuilder = Host.CreateApplicationBuilder();
skjemaBuilder
    .Services.AddHelseIdClientCredentials(
        new HelseIdConfiguration
        {
            ClientId = ClientId,
            Scope = SkjemaScope,
            IssuerUri = IssuerUri,
        }
    )
    .AddHelseIdMultiTenant()
    .AddJwkForClientAuthentication(privateKeyJwk);
var skjemaHost = skjemaBuilder.Build();
var skjemaFlow = skjemaHost.Services.GetRequiredService<IHelseIdClientCredentialsFlow>();
var skjemaDpop = skjemaHost.Services.GetRequiredService<IDPoPProofCreatorForApiRequests>();
var skjemaTokenResponse = await skjemaFlow.GetTokenResponseAsync(SkjemaScope, organizationNumbers);
if (!skjemaTokenResponse.IsSuccessful(out var skjemaToken))
{
    var error = skjemaTokenResponse.AsError();
    Console.Error.WriteLine($"❌ Token-forespørsel feilet: {error.Error} — {error.ErrorDescription}");
    return 1;
}
Console.WriteLine("   ✅ Token mottatt");
Console.WriteLine();

var testPdfBase64 = File.ReadAllText(
        Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "helsenorge-skjema-melding-test",
            "test-pdf-base64.txt"
        )
    )
    .Trim();

var docIdentifierGuid = Guid.NewGuid();
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
    ["docStatus"] = "final",
    ["type"] = new Dictionary<string, object?>
    {
        ["coding"] = new object[]
        {
            new Dictionary<string, object?>
            {
                ["system"] = "http://loinc.org",
                ["code"] = "57833-6",
                ["display"] = "Referral note",
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
    ["description"] =
        "TEST — kopi av henvisning sendt til spesialist (Digdir forer-legeerklaering PoC). Kan trygt ignoreres/kanselleres.",
    ["content"] = new object[]
    {
        new Dictionary<string, object?>
        {
            ["attachment"] = new Dictionary<string, object?>
            {
                ["contentType"] = "application/pdf",
                ["data"] = testPdfBase64,
                ["title"] = "TEST — kopi av henvisning",
            },
        },
    },
    ["date"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
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
                    ["value"] = $"urn:uuid:{taskIdentifierGuid}",
                },
            },
            // I tillegg til Task-knytningen: en direkte referanse til den samme henvisningen som
            // Task.basedOn pekte til, for å teste om DocumentReference.context.related kan ha mer
            // enn én oppføring (Task OG ServiceRequest).
            new Dictionary<string, object?>
            {
                ["type"] = "ServiceRequest",
                ["identifier"] = new Dictionary<string, object?>
                {
                    ["system"] = "urn:ietf:rfc:3986",
                    ["value"] = $"urn:uuid:{henvisningIdentifierGuid}",
                },
            },
        },
    },
};

var docJson = JsonSerializer.Serialize(documentReference, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine($"4) POST {DocumentReferenceEndpoint} (kopi av henvisning, knyttet til Task OG ServiceRequest):");
Console.WriteLine(docJson);
Console.WriteLine();

var docRequest = new HttpRequestMessage(HttpMethod.Post, DocumentReferenceEndpoint)
{
    Content = new StringContent(docJson, Encoding.UTF8, "application/fhir+json"),
};
var docProof = await skjemaDpop.CreateDPoPProofForApiRequest(HttpMethod.Post, DocumentReferenceEndpoint, skjemaToken);
docRequest.SetDPoPTokenAndProof(skjemaToken, docProof);
var docResponse = await httpClient.SendAsync(docRequest);
var docResponseBody = await docResponse.Content.ReadAsStringAsync();

Console.WriteLine($"   HTTP {(int)docResponse.StatusCode} {docResponse.StatusCode}");
Console.WriteLine(docResponseBody);

return taskResponse.IsSuccessStatusCode && docResponse.IsSuccessStatusCode ? 0 : 1;
