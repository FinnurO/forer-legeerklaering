using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Altinn.App.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altinn.App.Controllers
{
    /// <summary>
    /// Handles SMART on FHIR EHR Launch flow (SMART App Launch IG v2.2.0).
    /// Step 1: EPJ redirects to /smart/launch?iss=...&launch=...
    /// Step 2: App redirects to EPJ auth server for authorization code.
    /// Step 3: EPJ auth server redirects to /smart/callback?code=...
    /// Step 4: App exchanges code for token (server-side, confidential client).
    /// Step 5: App stores token in server session and redirects to form.
    /// </summary>
    [AllowAnonymous]
    [ApiController]
    [Route("{org}/{app}/smart")]
    public class SmartLaunchController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<SmartLaunchController> _logger;
        private readonly IWebHostEnvironment _env;

        private const string StateSessionKey = "smart_state";
        private const string PkceSessionKey = "smart_pkce_verifier";
        private const string IssSessionKey = "smart_iss";
        private const string TokenSessionKey = FhirPrefillService.TokenSessionKey;
        private const string FhirContextSessionKey = FhirPrefillService.FhirContextSessionKey;

        public SmartLaunchController(
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            IMemoryCache memoryCache,
            ILogger<SmartLaunchController> logger,
            IWebHostEnvironment env
        )
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _memoryCache = memoryCache;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// Entry point for EHR Launch. EPJ redirects here with iss and launch parameters.
        /// </summary>
        [HttpGet("launch")]
        public async Task<IActionResult> Launch([FromQuery] string iss, [FromQuery] string launch)
        {
            // Fall back to configured defaults for local testing when nginx strips query params
            iss ??= _config["SmartOnFhir:DefaultIss"];
            launch ??= _config["SmartOnFhir:DefaultLaunch"];

            if (string.IsNullOrEmpty(iss) || string.IsNullOrEmpty(launch))
                return BadRequest("Missing required SMART launch parameters: iss, launch");

            // Validate iss against allowlist — fail-closed in production
            var allowedIssList = _config.GetSection("SmartOnFhir:AllowedIssuerList").Get<List<string>>() ?? new();
            var isAllowed = _env.IsDevelopment()
                ? (allowedIssList.Count == 0 || allowedIssList.Contains(iss))
                : (allowedIssList.Count > 0 && allowedIssList.Contains(iss));
            if (!isAllowed)
            {
                _logger.LogWarning("Rejected SMART launch from unlisted iss: {Iss}", iss);
                return Forbid();
            }

            // Discover SMART configuration from EPJ
            var smartConfig = await DiscoverSmartConfiguration(iss);
            if (smartConfig == null)
                return StatusCode(502, "Could not retrieve SMART configuration from EPJ");

            // Generate PKCE
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);

            // Generate state
            var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

            // Store in server session
            HttpContext.Session.SetString(StateSessionKey, state);
            HttpContext.Session.SetString(PkceSessionKey, codeVerifier);
            HttpContext.Session.SetString(IssSessionKey, iss);

            var clientId =
                _config["SmartOnFhir:ClientId"]
                ?? _config.GetSection("SmartOnFhir")["ClientId"]
                ?? "forer-legeerklaering-poc";
            var org = RouteData.Values["org"]?.ToString();
            var app = RouteData.Values["app"]?.ToString();
            var redirectUri = $"{Request.Scheme}://{Request.Host}/{org}/{app}/smart/callback";

            var scopes = string.Join(
                " ",
                new[]
                {
                    "openid",
                    "profile",
                    "fhirUser",
                    "launch",
                    "launch/patient",
                    "launch/encounter",
                    "offline_access",
                    "patient/Patient.read",
                    "patient/Encounter.read",
                    "patient/Condition.read",
                    "patient/Observation.read",
                    "user/Practitioner.read",
                    "user/Organization.read",
                    "user/PractitionerRole.read",
                    // VEIKART.md fase 2 (writeback til EPJ) — lagt til 2026-08-11 for å teste om
                    // skriving faktisk er mulig mot en ekte SMART-server. v1-stil (.write) og
                    // v2-stil (.c, create) sendt begge — se hvilken(e) som faktisk innvilges i
                    // token-responsens "scope"-felt (jf. "Sølv: scope-detektivarbeid" i
                    // HACKATHON-EHIN-2026.md).
                    "patient/DocumentReference.write",
                    "patient/DocumentReference.c",
                }
            );

            var authUrl = BuildAuthorizationUrl(
                smartConfig.AuthorizationEndpoint,
                clientId,
                redirectUri,
                scopes,
                state,
                codeChallenge,
                launch,
                iss
            );

            return Redirect(authUrl);
        }

        /// <summary>
        /// Test-only shortcut: bypasses OAuth and seeds session directly with mock FHIR context.
        /// Only active in Development environment and when SmartOnFhir:FhirBaseUrlOverride is configured.
        /// Usage: GET /{org}/{app}/smart/test-prefill
        /// </summary>
        [HttpGet("test-prefill")]
        public async Task<IActionResult> TestPrefill()
        {
            if (!_env.IsDevelopment())
                return NotFound();

            var fhirBase = _config["SmartOnFhir:FhirBaseUrlOverride"];
            if (string.IsNullOrEmpty(fhirBase))
                return BadRequest("SmartOnFhir:FhirBaseUrlOverride ikke konfigurert");

            // Nøkkelnavn må matche FhirPrefillService.TokenData sin [JsonPropertyName("access_token")].
            var mockToken = new { access_token = "mock-test-token" };
            var tokenJson = System.Text.Json.JsonSerializer.Serialize(mockToken);

            var fhirContext = new FhirLaunchContext
            {
                PatientId = "sophie-salt",
                EncounterId = "enc-sophie-001",
                FhirUser = $"{fhirBase}/Practitioner/lege-ola",
                FhirBaseUrl = fhirBase,
            };
            var contextJson = System.Text.Json.JsonSerializer.Serialize(fhirContext);

            // Store in session (works when cookie is forwarded by browser)
            await HttpContext.Session.LoadAsync();
            HttpContext.Session.SetString(TokenSessionKey, tokenJson);
            HttpContext.Session.SetString(FhirContextSessionKey, contextJson);

            // Also store in memory cache keyed by session ID (fallback if cookie is missing)
            var cacheKey = FhirPrefillService.CacheKeyPrefix + HttpContext.Session.Id;
            _memoryCache.Set(
                cacheKey,
                new FhirPrefillService.CachedFhirData { TokenJson = tokenJson, ContextJson = contextJson },
                TimeSpan.FromMinutes(30)
            );

            _logger.LogInformation(
                "TestPrefill: session ID={SessionId}, cache key={CacheKey}",
                HttpContext.Session.Id,
                cacheKey
            );

            var org = RouteData.Values["org"]?.ToString();
            var app = RouteData.Values["app"]?.ToString();
            return Redirect($"/{org}/{app}");
        }

        /// <summary>
        /// Dev-only: fetches a localtest JWT for the given userId/partyId, sets Altinn auth cookies,
        /// then runs test-prefill and redirects to the app — one click auto-login without the localtest UI.
        /// </summary>
        [HttpGet("dev-login")]
        public async Task<IActionResult> DevLogin(
            [FromQuery] int userId = 12345,
            [FromQuery] int partyId = 512345,
            [FromQuery] string patientId = "sophie-salt",
            [FromQuery] string encounterId = "enc-sophie-001"
        )
        {
            if (!_env.IsDevelopment())
                return NotFound();

            var loginOk = await EstablishLocaltestAltinnSessionAsync(userId, partyId);
            if (!loginOk)
                return StatusCode(502, $"Kunne ikke hente token fra localtest for userId={userId}");

            _logger.LogInformation(
                "DevLogin: userId={UserId} partyId={PartyId} patient={PatientId} encounter={EncounterId}",
                userId,
                partyId,
                patientId,
                encounterId
            );

            // Seed FHIR session with the selected patient/encounter
            var fhirBase = _config["SmartOnFhir:FhirBaseUrlOverride"];
            if (!string.IsNullOrEmpty(fhirBase))
            {
                // Nøkkelnavn må matche FhirPrefillService.TokenData sin [JsonPropertyName("access_token")].
                var mockTokenJson = System.Text.Json.JsonSerializer.Serialize(new { access_token = "mock-test-token" });
                var fhirContext = new FhirLaunchContext
                {
                    PatientId = patientId,
                    EncounterId = encounterId,
                    FhirUser = $"{fhirBase}/Practitioner/lege-ola",
                    FhirBaseUrl = fhirBase,
                };
                await HttpContext.Session.LoadAsync();
                HttpContext.Session.SetString(TokenSessionKey, mockTokenJson);
                HttpContext.Session.SetString(
                    FhirContextSessionKey,
                    System.Text.Json.JsonSerializer.Serialize(fhirContext)
                );
                _memoryCache.Set(
                    FhirPrefillService.CacheKeyPrefix + HttpContext.Session.Id,
                    new FhirPrefillService.CachedFhirData
                    {
                        TokenJson = mockTokenJson,
                        ContextJson = System.Text.Json.JsonSerializer.Serialize(fhirContext),
                    },
                    TimeSpan.FromMinutes(30)
                );
            }

            var org = RouteData.Values["org"]?.ToString();
            var app = RouteData.Values["app"]?.ToString();
            return Redirect($"/{org}/{app}");
        }

        /// <summary>
        /// Dev-only: prøver et faktisk writeback-kall (POST DocumentReference) mot EPJ-en fra den
        /// aktive SMART-sesjonen. VEIKART.md fase 2-utforskning — svarer på om skriving faktisk er
        /// mulig, ikke en produksjonsimplementasjon (ingen PDF, ingen ekte innhold, ingen idempotens
        /// via klient-tildelt id/PUT som VEIKART.md fase 2 spesifiserer for den ferdige løsningen).
        /// Kjør etter en vanlig launch (test-prefill, dev-login eller ekte /smart/launch).
        /// </summary>
        [HttpGet("test-writeback")]
        public async Task<IActionResult> TestWriteback()
        {
            if (!_env.IsDevelopment())
                return NotFound();

            await HttpContext.Session.LoadAsync();
            var tokenJson = HttpContext.Session.GetString(TokenSessionKey);
            var contextJson = HttpContext.Session.GetString(FhirContextSessionKey);
            if (string.IsNullOrEmpty(tokenJson) || string.IsNullOrEmpty(contextJson))
                return BadRequest("Ingen SMART-sesjon funnet i session — gjennomfør en launch først.");

            var token = JsonSerializer.Deserialize<TokenResponse>(tokenJson);
            var context = JsonSerializer.Deserialize<FhirLaunchContext>(contextJson);
            if (string.IsNullOrEmpty(token?.AccessToken) || string.IsNullOrEmpty(context?.PatientId))
                return BadRequest("Token eller pasientkontekst mangler i sesjonen.");

            var docRef = new Dictionary<string, object?>
            {
                ["resourceType"] = "DocumentReference",
                ["status"] = "current",
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
                ["subject"] = new Dictionary<string, object?> { ["reference"] = $"Patient/{context.PatientId}" },
                ["content"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["attachment"] = new Dictionary<string, object?>
                        {
                            ["contentType"] = "text/plain",
                            ["data"] = Convert.ToBase64String(
                                Encoding.UTF8.GetBytes(
                                    $"TEST writeback fra forer-legeerklaering PoC — {DateTimeOffset.UtcNow:O}"
                                )
                            ),
                            ["title"] = "Legeerklæring førerrett (TEST writeback — ikke ekte innhold)",
                        },
                    },
                },
            };

            var requestJson = JsonSerializer.Serialize(docRef);
            var writebackUrl = $"{context.FhirBaseUrl}/DocumentReference";
            var request = new HttpRequestMessage(HttpMethod.Post, writebackUrl)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/fhir+json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            var client = _httpClientFactory.CreateClient();
            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "TestWriteback: POST {Url} -> HTTP {Status} (innvilget scope: {Scope})",
                writebackUrl,
                (int)response.StatusCode,
                token.Scope
            );

            return Content(
                $"POST {writebackUrl}\nInnvilget scope: {token.Scope}\n\nHTTP {(int)response.StatusCode}\n\n{responseBody}",
                "text/plain"
            );
        }

        /// <summary>
        /// Dev-only: henter en localtest-JWT for gitt userId/partyId og setter Altinn-auth-cookies.
        /// Utledet fra DevLogin slik at Callback kan gjenbruke den samme innloggingsmekanikken (R10).
        /// </summary>
        private async Task<bool> EstablishLocaltestAltinnSessionAsync(int userId, int partyId)
        {
            var localtestBase =
                _config["PlatformSettings:ApiAuthenticationEndpoint"]?.Replace("/authentication/api/v1/", "")
                ?? "http://localhost:5101";

            var client = _httpClientFactory.CreateClient();
            var tokenResponse = await client.GetAsync(
                $"{localtestBase}/Home/GetTestUserToken/{userId}?authenticationLevel=2"
            );
            if (!tokenResponse.IsSuccessStatusCode)
                return false;

            var token = await tokenResponse.Content.ReadAsStringAsync();

            var runtimeCookieName = _config["AppSettings:RuntimeCookieName"] ?? "AltinnStudioRuntime";
            var partyCookieName = _config["AppSettings:AltinnPartyCookieName"] ?? "AltinnPartyId";
            var cookieOptions = new CookieOptions
            {
                Path = "/",
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
            };
            Response.Cookies.Append(runtimeCookieName, token, cookieOptions);
            Response.Cookies.Append(partyCookieName, partyId.ToString(), cookieOptions);
            return true;
        }

        /// <summary>
        /// OAuth2 callback. Exchanges authorization code for token server-side.
        /// </summary>
        [HttpGet("callback")]
        public async Task<IActionResult> Callback(
            [FromQuery] string code,
            [FromQuery] string state,
            [FromQuery] string error
        )
        {
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("SMART auth error: {Error}", error);
                return BadRequest($"Authorization error: {error}");
            }

            var sessionState = HttpContext.Session.GetString(StateSessionKey);
            if (state != sessionState)
                return BadRequest("State mismatch — possible CSRF");

            var iss = HttpContext.Session.GetString(IssSessionKey);
            var codeVerifier = HttpContext.Session.GetString(PkceSessionKey);
            var smartConfig = await DiscoverSmartConfiguration(iss);

            var org = RouteData.Values["org"]?.ToString();
            var app = RouteData.Values["app"]?.ToString();
            var redirectUri = $"{Request.Scheme}://{Request.Host}/{org}/{app}/smart/callback";
            var clientId = _config["SmartOnFhir:ClientId"];
            var clientSecret = _config["SmartOnFhir:ClientSecret"];

            var token = await ExchangeCodeForToken(
                smartConfig.TokenEndpoint,
                code,
                redirectUri,
                clientId,
                clientSecret,
                codeVerifier
            );

            if (token == null)
                return StatusCode(502, "Token exchange failed");

            // Fallback bekreftet nødvendig 2026-08-11 mot launch.smarthealthit.org: fhirUser kom ikke
            // som eget toppnivåfelt i token-responsen for denne launch-konfigurasjonen, kun som claim
            // i access_token-JWT-en. Dekodes uten signaturvalidering — brukes kun til en FHIR-referanse
            // for prefill, ikke til autorisasjonsbeslutninger, så det er innenfor akseptabel risiko her.
            if (string.IsNullOrEmpty(token.FhirUser))
            {
                token.FhirUser = TryExtractClaimFromJwt(token.AccessToken, "fhirUser");
                if (!string.IsNullOrEmpty(token.FhirUser))
                    _logger.LogInformation("fhirUser hentet fra access_token-claim (ikke toppnivåfelt)");
            }

            // Store token server-side — never expose to browser
            HttpContext.Session.SetString(TokenSessionKey, JsonSerializer.Serialize(token));

            // Store FHIR context for pre-fill
            // FhirBaseUrlOverride finnes for vår egen lokale SMART-mock (iss er en Docker-intern
            // http-adresse appen ikke kan nå direkte). Den skal IKKE brukes for ekte eksterne
            // SMART-servere (https), ellers prøver FhirPrefillService å hente pasientdata fra vår
            // egen HAPI FHIR-mock med en pasient-ID fra en helt annen server (404/tom prefill).
            // Bug funnet 2026-08-11 ved test mot launch.smarthealthit.org — se IMPLEMENTERING.md §13.
            var isRealExternalIssuer = iss?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true;
            var fhirBaseUrl = isRealExternalIssuer ? iss : (_config["SmartOnFhir:FhirBaseUrlOverride"] ?? iss);
            var fhirContext = new FhirLaunchContext
            {
                PatientId = token.Patient,
                EncounterId = token.Encounter,
                FhirUser = token.FhirUser,
                FhirBaseUrl = fhirBaseUrl,
            };
            HttpContext.Session.SetString(FhirContextSessionKey, JsonSerializer.Serialize(fhirContext));

            // Clear PKCE and state from session
            HttpContext.Session.Remove(StateSessionKey);
            HttpContext.Session.Remove(PkceSessionKey);

            // R10 (RISIKOREGISTER.md): en ekte SMART callback gir FHIR-kontekst, men ingen
            // Altinn-sesjon — Altinns generiske ID-porten-utfordring virker ikke fra denne inngangen
            // (se IMPLEMENTERING.md §13). Midlertidig demo-løsning: i Development, etabler en
            // localtest-testbrukersesjon automatisk hvis vi ikke allerede har en. Dette er IKKE en
            // produksjonsløsning — se VEIKART.md fase 1 for det egentlige forslaget (HelseID-identitet
            // → Altinn-sesjon).
            var runtimeCookieName = _config["AppSettings:RuntimeCookieName"] ?? "AltinnStudioRuntime";
            var hasAltinnSession = Request.Cookies.ContainsKey(runtimeCookieName);
            if (!hasAltinnSession && _env.IsDevelopment())
            {
                _logger.LogInformation(
                    "Callback: ingen Altinn-sesjon funnet — etablerer localtest-testbruker (kun dev)"
                );
                await EstablishLocaltestAltinnSessionAsync(userId: 12345, partyId: 512345);
            }

            return Redirect($"/{org}/{app}");
        }

        private async Task<SmartConfiguration> DiscoverSmartConfiguration(string iss)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var wellKnownUrl = $"{iss.TrimEnd('/')}/.well-known/smart-configuration";
                var response = await client.GetAsync(wellKnownUrl);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SmartConfiguration>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover SMART configuration from {Iss}", iss);
                return null;
            }
        }

        private async Task<TokenResponse> ExchangeCodeForToken(
            string tokenEndpoint,
            string code,
            string redirectUri,
            string clientId,
            string clientSecret,
            string codeVerifier
        )
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Confidential client: Basic auth with client_id:client_secret
                if (!string.IsNullOrEmpty(clientSecret))
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                }

                var body = new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = redirectUri,
                    ["client_id"] = clientId,
                    ["code_verifier"] = codeVerifier,
                };

                var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(body));
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TokenResponse>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token exchange failed at {Endpoint}", tokenEndpoint);
                return null;
            }
        }

        private static string BuildAuthorizationUrl(
            string authEndpoint,
            string clientId,
            string redirectUri,
            string scope,
            string state,
            string codeChallenge,
            string launch,
            string iss
        )
        {
            // aud must be the FHIR server base URL (iss), NOT the authorization endpoint
            // per SMART App Launch IG v2.2.0 §3.1
            var qs = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId ?? "forer-legeerklaering-poc",
                ["redirect_uri"] = redirectUri ?? "",
                ["scope"] = scope ?? "",
                ["state"] = state ?? "",
                ["aud"] = iss ?? "",
                ["launch"] = launch ?? "",
                ["code_challenge"] = codeChallenge ?? "",
                ["code_challenge_method"] = "S256",
            };
            var query = string.Join(
                "&",
                System.Linq.Enumerable.Select(
                    qs,
                    kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"
                )
            );
            return $"{authEndpoint}?{query}";
        }

        private static string GenerateCodeVerifier()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string GenerateCodeChallenge(string verifier)
        {
            var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// Dekoder en JWT-payload uten signaturvalidering og henter ut ett claim.
        /// KUN for prefill-formål (f.eks. fhirUser-fallback) — skal ALDRI brukes til
        /// autorisasjonsbeslutninger uten signatur-/utsteder-/utløpsvalidering.
        /// </summary>
        private static string TryExtractClaimFromJwt(string jwt, string claimName)
        {
            try
            {
                var parts = jwt?.Split('.');
                if (parts == null || parts.Length < 2)
                    return null;

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2:
                        payload += "==";
                        break;
                    case 3:
                        payload += "=";
                        break;
                }

                var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty(claimName, out var value) ? value.GetString() : null;
            }
            catch (Exception)
            {
                // access_token er ikke nødvendigvis en JWT (kan være et opakt token) — det er da
                // forventet at dette feiler, ikke en feilsituasjon som skal logges som warning/error.
                return null;
            }
        }

        private class SmartConfiguration
        {
            // BUG (2026-08-11, R8 i RISIKOREGISTER.md): PropertyNameCaseInsensitive løser kun
            // store/små bokstaver, ikke snake_case -> PascalCase. Uten disse attributtene
            // deserialiserer "authorization_endpoint"/"token_endpoint" til null, BuildAuthorizationUrl
            // bygger en tom/relativ URL, og Redirect() sender nettleseren tilbake til /smart/launch
            // — som årsaket ERR_TOO_MANY_REDIRECTS. Reprodusert og bekreftet mot launch.smarthealthit.org.
            [JsonPropertyName("authorization_endpoint")]
            public string AuthorizationEndpoint { get; set; }

            [JsonPropertyName("token_endpoint")]
            public string TokenEndpoint { get; set; }
        }

        private class TokenResponse
        {
            // Samme snake_case-bug som SmartConfiguration (OAuth2 token-respons, RFC 6749) —
            // rettet samtidig, siden ExchangeCodeForToken ellers ville feile likt etter at
            // autorisasjonsomdirigeringen er fikset.
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("token_type")]
            public string TokenType { get; set; }

            [JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; }

            // Serveren kan innvilge et smalere scope enn det som ble forespurt — sammenlign alltid
            // dette mot forespurt scope før man forutsetter en tilgang er innvilget (bl.a. relevant
            // for skrivetilgang, se TestWriteback og HACKATHON-EHIN-2026.md "scope-detektivarbeid").
            [JsonPropertyName("scope")]
            public string Scope { get; set; }

            public string Patient { get; set; }
            public string Encounter { get; set; }

            // Per SMART App Launch IG v2.2.0: fhirUser er et eget toppnivåfelt i tokenresponsen.
            // Noen EPJ-systemer returnerer det som JWT-claim i access_token i stedet — dekod da tokenet server-side.
            public string FhirUser { get; set; }
        }

        private class FhirLaunchContext
        {
            public string PatientId { get; set; }
            public string EncounterId { get; set; }
            public string FhirUser { get; set; }
            public string FhirBaseUrl { get; set; }
        }
    }
}
