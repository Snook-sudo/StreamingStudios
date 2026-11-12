using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.StreamingStudios.Services;

public sealed class TmdbClient
{
    private static readonly Regex NoiseSuffix = new(
        @"\s+(Standard with Ads|Basic with Ads|with Ads|Amazon Channel|Apple TV Channel|Roku Premium Channel|Premium Channel)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TmdbClient> _logger;

    public TmdbClient(IHttpClientFactory httpClientFactory, ILogger<TmdbClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public static string Normalize(string name)
    {
        var n = name.Trim();
        string prev;
        do
        {
            prev = n;
            n = NoiseSuffix.Replace(n, string.Empty).Trim();
        }
        while (n != prev);
        return n;
    }

    /// <summary>Ritorna le piattaforme per un titolo, null se errore temporaneo.</summary>
    public async Task<List<string>?> GetProvidersAsync(
        bool isMovie, string tmdbId, string country, string apiKey,
        IReadOnlyCollection<string> categories, CancellationToken ct)
    {
        var kind = isMovie ? "movie" : "tv";
        var url = $"https://api.themoviedb.org/3/{kind}/{Uri.EscapeDataString(tmdbId)}/watch/providers";
        var bearer = apiKey.StartsWith("eyJ", StringComparison.Ordinal);
        if (!bearer)
        {
            url += "?api_key=" + Uri.EscapeDataString(apiKey);
        }

        for (var attempt = 0; attempt < 4; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (bearer)
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            var client = _httpClientFactory.CreateClient();
            using var resp = await client.SendAsync(req, ct).ConfigureAwait(false);

            if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var wait = resp.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2 * (attempt + 1));
                await Task.Delay(wait, ct).ConfigureAwait(false);
                continue;
            }

            if (resp.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<string>();
            }

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("Chiave API TMDB non valida (401).");
            }

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB {Kind}/{Id}: HTTP {Code}", kind, tmdbId, (int)resp.StatusCode);
                return null;
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

            var result = new List<string>();
            if (doc.RootElement.TryGetProperty("results", out var results)
                && results.TryGetProperty(country, out var c))
            {
                foreach (var cat in categories)
                {
                    if (!c.TryGetProperty(cat, out var arr) || arr.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var p in arr.EnumerateArray())
                    {
                        if (p.TryGetProperty("provider_name", out var pn) && pn.GetString() is { Length: > 0 } s)
                        {
                            var norm = Normalize(s);
                            if (norm.Length > 0 && !result.Contains(norm, StringComparer.OrdinalIgnoreCase))
                            {
                                result.Add(norm);
                            }
                        }
                    }
                }
            }

            return result;
        }

        return null;
    }
}
