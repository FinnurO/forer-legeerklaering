using HelseId.Library;
using HelseId.Library.ClientCredentials;
using HelseId.Library.ClientCredentials.Interfaces;
using HelseId.Library.Configuration;
using HelseId.Library.ExtensionMethods;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// ---------------------------------------------------------------------------------------------
// Engangs-røyktest: bekreft at vi kan hente et access token fra HelseID testmiljø for
// Helsenorge EksternAPI (Oppgave + Skjema), med den «Altinn Studio»-klienten som er registrert
// i selvbetjening.test.nhn.no.
//
// Kaller IKKE selve Helsenorge-APIet — bare token-utvekslingen mot HelseID.
//
// SIKKERHET: privatnøkkelen (JWK) leses fra en LOKAL FIL utenfor dette repoet, angitt via
// miljøvariabelen HELSEID_JWK_PATH. Den skal ALDRI legges i kildekoden, appsettings, eller
// committes — dette repoet er offentlig på GitHub.
//
// Kjør:
//   $env:HELSEID_JWK_PATH = "C:\Users\jsf\.secrets\helseid-eksternapi-test.jwk.json"
//   dotnet run
// ---------------------------------------------------------------------------------------------

const string ClientId = "4f1fc480-72d9-4e31-b099-69b84fd5ba6b"; // "Altinn Studio"-klienten, ikke sensitiv
const string IssuerUri = "https://helseid-sts.test.nhn.no";
const string Scope = "nhn:helsenorge.eksternapi/oppgave nhn:helsenorge.eksternapi/skjema";

var jwkPath = Environment.GetEnvironmentVariable("HELSEID_JWK_PATH");
if (string.IsNullOrWhiteSpace(jwkPath))
{
    Console.Error.WriteLine("Miljøvariabelen HELSEID_JWK_PATH er ikke satt.");
    Console.Error.WriteLine(
        "Sett den til filstien der du har lagret den private JWK-nøkkelen fra "
            + "selvbetjening.test.nhn.no (utenfor dette repoet), f.eks.:"
    );
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

builder.Services.AddHelseIdClientCredentials(helseIdConfiguration).AddJwkForClientAuthentication(privateKeyJwk);

var host = builder.Build();

var flow = host.Services.GetRequiredService<IHelseIdClientCredentialsFlow>();

Console.WriteLine($"Ber om token fra {IssuerUri} for klient {ClientId} ...");
Console.WriteLine($"Scope: {Scope}");
Console.WriteLine();

var tokenResponse = await flow.GetTokenResponseAsync();

if (tokenResponse.IsSuccessful(out var accessTokenResponse))
{
    Console.WriteLine("✅ Token mottatt.");
    Console.WriteLine($"   Utløper om: {accessTokenResponse.ExpiresIn} sekunder");
    Console.WriteLine($"   Tildelt scope: {accessTokenResponse.Scope}");
    if (!string.IsNullOrEmpty(accessTokenResponse.RejectedScope))
        Console.WriteLine($"   ⚠️ Avvist scope: {accessTokenResponse.RejectedScope}");
    // Selve access token-verdien skrives bevisst IKKE til konsollen/logg.
    return 0;
}
else
{
    var error = tokenResponse.AsError();
    Console.Error.WriteLine("❌ Token-forespørsel feilet.");
    Console.Error.WriteLine($"   Error: {error.Error}");
    Console.Error.WriteLine($"   Description: {error.ErrorDescription}");
    return 1;
}
