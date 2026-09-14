using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SmartFhir.Common.Session;

namespace SmartFhir.Common
{
    /// <summary>
    /// Leser SMART-token og FHIR-launchkontekst fra server-session, med IMemoryCache (keyet på
    /// session-ID) som fallback hvis session-cookien ikke følger med. Samme "1. session,
    /// 2. cache"-mønster som den opprinnelige FhirPrefillService brukte — flyttet hit 2026-09-09
    /// slik at nye apper ikke må implementere denne oppslagslogikken på nytt.
    /// </summary>
    public static class SmartSessionReader
    {
        public record Result(TokenData Token, FhirLaunchContext Context);

        public static async Task<Result> TryReadAsync(ISession session, IMemoryCache memoryCache, ILogger logger)
        {
            string tokenJson = null;
            string contextJson = null;

            // 1. Prøv session (krever session-middleware + cookie fra nettleseren)
            if (session != null)
            {
                await session.LoadAsync();
                tokenJson = session.GetString(SmartSessionKeys.TokenSessionKey);
                contextJson = session.GetString(SmartSessionKeys.FhirContextSessionKey);
                logger?.LogInformation(
                    "Session loaded — token present: {HasToken}, context present: {HasContext}",
                    !string.IsNullOrEmpty(tokenJson),
                    !string.IsNullOrEmpty(contextJson)
                );
            }

            // 2. Fall tilbake til IMemoryCache keyet på session-ID
            if ((string.IsNullOrEmpty(tokenJson) || string.IsNullOrEmpty(contextJson)) && session != null)
            {
                var cacheKey = SmartSessionKeys.CacheKeyPrefix + session.Id;
                if (memoryCache != null && memoryCache.TryGetValue(cacheKey, out CachedFhirData cached))
                {
                    logger?.LogInformation("Found FHIR context in memory cache (key: {Key})", cacheKey);
                    tokenJson = cached.TokenJson;
                    contextJson = cached.ContextJson;
                }
            }

            if (string.IsNullOrEmpty(tokenJson) || string.IsNullOrEmpty(contextJson))
                return null;

            var token = JsonSerializer.Deserialize<TokenData>(tokenJson);
            var context = JsonSerializer.Deserialize<FhirLaunchContext>(contextJson);
            return new Result(token, context);
        }
    }
}
